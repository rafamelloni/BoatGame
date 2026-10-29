using System;
using System.Collections;
using UnityEngine;

public class CannonStrategy : IAbilityStrategy, IcooldownAbilities
{
    private readonly SO_CannonData _baseData;
    public readonly RT_CannonData _rtData;
    private BulletFactory _cannonBullet;
    private readonly ShipHardpoints hardpoints;
    private readonly CoroutineRunner runner;
    // Cargador: cada disparo gasta 1 bala; al vaciarse recarga durante _rtData.cooldown
    private int _currentAmmo;
    private bool _reloading = false;
    private float _reloadEndTime = 0f;
    private float _nextShotTime = 0f;
    followPlayer _camera;
    CannonRecoil _recoilLeft;
    CannonRecoil _recoilRight;
    private RT_PlayerUpgrades _playerUpgrades;
    private PlayerUpgrades _islandUpgrades;
    private int _shotsSinceCharge = 0;
    private Transform _shipRoot;

    public event Action<float> OnCooldownStarted;      // arranca la recarga (la UI radial la muestra)
    public event Action<int, int> OnAmmoChanged;       // (actual, máximo)
    public event Action OnReloaded;                     // terminó una recarga (reliquias)

    // Reliquias: multiplica el tiempo de UNA recarga; se consulta una vez al empezar a recargar
    public Func<float> ReloadTimeMultiplier;
    // Reliquias: mientras está en true, disparar no gasta balas
    public bool InfiniteAmmo;
    private float _currentReloadDuration;

    public float CooldownDuration => _reloading ? _currentReloadDuration : _rtData.cooldown;
    public float RemainingCooldown => _reloading ? Mathf.Max(0f, _reloadEndTime - Time.time) : 0f;
    public bool IsOnCooldown => _reloading;
    public bool IsReloading => _reloading;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _rtData.maxAmmo;
    public bool IsUnlocked { get; private set; } = false;
    public void SetUnlocked(bool unlocked) => IsUnlocked = unlocked;

    public CannonStrategy(SO_CannonData data, ShipHardpoints hardpoints, CoroutineRunner runner,
        BulletFactory cannonBullet, followPlayer camera, CannonRecoil recoilR, CannonRecoil recoilL, RT_PlayerUpgrades playerUpgrades, PlayerUpgrades islandUpgrades)
    {
        _playerUpgrades = playerUpgrades;
        _baseData = data;
        _rtData = new RT_CannonData(_baseData);
        this.hardpoints = hardpoints;
        this.runner = runner;
        _cannonBullet = cannonBullet;
        _camera = camera;
        _recoilLeft = recoilL;
        _recoilRight = recoilR;
        _islandUpgrades = islandUpgrades;
        _currentAmmo = _rtData.maxAmmo;
    }

    // Llamar todos los frames (AbilityController.Update) para que la recarga termine sola
    public void Tick()
    {
        if (_reloading && Time.time >= _reloadEndTime)
        {
            _reloading = false;
            _currentAmmo = _rtData.maxAmmo;
            OnAmmoChanged?.Invoke(_currentAmmo, _rtData.maxAmmo);
            OnReloaded?.Invoke();
        }
    }

    public void TryExecute()
    {
        Tick();
        if (_reloading) return;
        if (Time.time < _nextShotTime) return;
        if (_currentAmmo <= 0) { StartReload(); return; }

        if (!InfiniteAmmo) _currentAmmo--;
        _nextShotTime = Time.time + _rtData.timeBetweenShots;
        OnAmmoChanged?.Invoke(_currentAmmo, _rtData.maxAmmo);
        runner.StartCoroutine(FireBurst());

        if (_currentAmmo <= 0)
        {
            if (_playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.Broadside))
                runner.StartCoroutine(FireBroadside());
            StartReload();
        }
    }

    private void StartReload()
    {
        _reloading = true;
        float mult = ReloadTimeMultiplier != null ? Mathf.Max(0.05f, ReloadTimeMultiplier()) : 1f;
        _currentReloadDuration = _rtData.cooldown * mult;
        _reloadEndTime = Time.time + _currentReloadDuration;
        OnCooldownStarted?.Invoke(_currentReloadDuration);
    }

    // Mejoras de munición: suben el máximo y, si no está recargando, también la actual
    public void AddAmmo(int amount)
    {
        if (amount == 0) return;
        _rtData.maxAmmo = Mathf.Max(1, _rtData.maxAmmo + amount);
        if (!_reloading) _currentAmmo = Mathf.Clamp(_currentAmmo + amount, 0, _rtData.maxAmmo);
        OnAmmoChanged?.Invoke(_currentAmmo, _rtData.maxAmmo);
    }

    // Devuelve la bala del disparo recién hecho (reliquia Ron: "Barril sin fondo")
    public void RefundShot()
    {
        if (_reloading) return;
        _currentAmmo = Mathf.Min(_currentAmmo + 1, _rtData.maxAmmo);
        OnAmmoChanged?.Invoke(_currentAmmo, _rtData.maxAmmo);
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < _rtData.shotsPerBurst; i++)
        {
            // Recalculado en CADA disparo de la ráfaga, así cada bala sale
            // hacia donde está el mouse en ese instante y no hacia el punto
            // original del click que arrancó la ráfaga.
            Vector3 targetPoint = GetMouseWorldPoint();
            Vector3 shipPos = hardpoints.transform.position;
            // Usamos el "right" del barco aplanado (sin inclinación). Antes se usaba
            // transform.right tal cual: con el barco inclinado ese vector apunta un poco
            // hacia arriba/abajo y el Reflect levantaba el target espejado varios metros.
            // Con un target muy por encima de la bala, CalculateLaunchVelocity caía en el
            // fallback de 45° a velocidad máxima y esa bala salía volando re lejos.
            Vector3 flatRight = hardpoints.transform.right;
            flatRight.y = 0f;
            flatRight.Normalize();

            Vector3 toTarget = targetPoint - shipPos;
            toTarget.y = 0f;
            Vector3 mirroredTarget = shipPos + Vector3.Reflect(toTarget, flatRight);
            mirroredTarget.y = targetPoint.y;

            // Detectar de qué lado está el target
            float side = Vector3.Dot(toTarget, flatRight);

            Vector3 rightTarget = side >= 0f ? targetPoint : mirroredTarget;
            Vector3 leftTarget = side >= 0f ? mirroredTarget : targetPoint;

            bool isCharged = DetermineIfCharged();

            foreach (Transform p in hardpoints.rightShootPoints)
            {
                FireFromPoint(p, 1f, rightTarget, isCharged);
                _camera.ApplyCannonImpact();
                _recoilRight.Fire(_rtData.timeBetweenShots);
                _recoilLeft.Fire(_rtData.timeBetweenShots);
                hardpoints._cannonSmokeShootR.Play();
                FireMuzzle(p);
            }
            foreach (Transform p in hardpoints.leftShootPoints)
            {
                FireFromPoint(p, -1f, leftTarget, isCharged);
                hardpoints._cannonSmokeShootL.Play();
                FireMuzzle(p);
            }
            if (i < _rtData.shotsPerBurst - 1)
                yield return new WaitForSeconds(_rtData.timeBetweenShots);
        }
    }

    // Andanada: al vaciar el cargador, cada costado suelta una fila de balas
    // perpendicular al casco. Usa FireFromPoint, así que hereda Split, Ricochet,
    // fuego, etc.
    private IEnumerator FireBroadside()
    {
        yield return new WaitForSeconds(_rtData.broadsideDelay);

        int count = Mathf.Max(1, _rtData.broadsideShotsPerSide);
        float start = -(count - 1) * 0.5f * _rtData.broadsideSpacing;

        Transform rightPoint = FirstPoint(hardpoints.rightShootPoints);
        Transform leftPoint = FirstPoint(hardpoints.leftShootPoints);

        for (int i = 0; i < count; i++)
        {
            // Se recalcula en cada bala porque el barco se sigue moviendo
            Vector3 shipPos = hardpoints.transform.position;
            Vector3 flatRight = hardpoints.transform.right;
            flatRight.y = 0f;
            flatRight.Normalize();
            Vector3 flatForward = Vector3.Cross(flatRight, Vector3.up);

            Vector3 along = flatForward * (start + i * _rtData.broadsideSpacing);
            Vector3 rightTarget = shipPos + flatRight * _rtData.broadsideDistance + along;
            Vector3 leftTarget = shipPos - flatRight * _rtData.broadsideDistance + along;

            if (rightPoint != null)
            {
                FireFromPoint(rightPoint, 1f, rightTarget, false);
                FireMuzzle(rightPoint);
                hardpoints._cannonSmokeShootR.Play();
            }
            if (leftPoint != null)
            {
                FireFromPoint(leftPoint, -1f, leftTarget, false);
                FireMuzzle(leftPoint);
                hardpoints._cannonSmokeShootL.Play();
            }
            _camera.ApplyCannonImpact();

            if (i < count - 1 && _rtData.broadsideInterval > 0f)
                yield return new WaitForSeconds(_rtData.broadsideInterval);
        }
    }

    private Transform FirstPoint(Transform[] points)
    {
        if (points == null) return null;
        foreach (Transform p in points)
            if (p != null) return p;
        return null;
    }

    // Cañones extra (tier 2+): cada uno tiene su propio CannonMuzzle con humo y recoil.
    // Los cañones originales no tienen CannonMuzzle, así que acá no hacen nada.
    private void FireMuzzle(Transform point)
    {
        var muzzle = point.GetComponentInParent<CannonMuzzle>();
        if (muzzle != null) muzzle.Fire(_rtData.timeBetweenShots);
    }

    // Decide UNA vez por ráfaga si este disparo es el cargado - así los dos
    // cañones (derecho e izquierdo) tiran la bala grande en el mismo volley,
    // en vez de que el contador se reparta entre ambos por separado.
    private bool DetermineIfCharged()
    {
        bool hasChargedShot = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.ChargedShot);
        if (!hasChargedShot || _rtData.chargedShotInterval <= 0) return false;

        _shotsSinceCharge++;
        if (_shotsSinceCharge >= _rtData.chargedShotInterval)
        {
            _shotsSinceCharge = 0;
            return true;
        }
        return false;
    }

    private Vector3 GetMouseWorldPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, hardpoints.transform.position.y, 0f));
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return Vector3.zero;
    }

    private void FireFromPoint(Transform point, float side, Vector3 targetPoint, bool isCharged)
    {
        var b = _cannonBullet.Create();
        var cb = b.GetComponent<CannonBullet>();
        if (cb != null)
        {
            cb.OnSplit -= SpawnSplitBullet;
            cb.OnSplit += SpawnSplitBullet;
            cb.Setup(point, _rtData, side, _playerUpgrades, _islandUpgrades, targetPoint, isCharged, GetStableShootPosition(point));
        }
    }

    // El barco se inclina (bank al girar + balanceo de las olas) y los fire points
    // bajan con él; a veces quedaban casi al nivel del agua y la bala tocaba el
    // Floor apenas se activaba el collider. Acá calculamos dónde estaría el fire
    // point si el barco estuviera derecho (solo con su rotación en Y), así la bala
    // siempre sale a la misma altura relativa al barco.
    private Vector3 GetStableShootPosition(Transform point)
    {
        if (_shipRoot == null)
        {
            var movement = hardpoints.GetComponentInParent<Movement>();
            _shipRoot = movement != null ? movement.transform : hardpoints.transform;
        }

        Vector3 local = _shipRoot.InverseTransformPoint(point.position);
        Vector3 scaledLocal = Vector3.Scale(local, _shipRoot.lossyScale);
        Quaternion yawOnly = Quaternion.Euler(0f, _shipRoot.eulerAngles.y, 0f);
        return _shipRoot.position + yawOnly * scaledLocal;
    }

    // Bala suelta disparada desde cualquier punto (reliquia Metralla)
    public void FireExtraBullet(Vector3 position, Vector3 velocity, float damageMultiplier)
    {
        var b = _cannonBullet.Create();
        var cb = b.GetComponent<CannonBullet>();
        if (cb == null) return;
        cb.SetupSplit(position, velocity, _rtData, _playerUpgrades, false);
        cb.MarkShrapnel(damageMultiplier);
    }

    private void SpawnSplitBullet(Vector3 position, Vector3 direction, float damageMultiplier, bool isCharged)
    {
        var b = _cannonBullet.Create();
        var cb = b.GetComponent<CannonBullet>();
        if (cb != null)
        {
            float originalDamage = _rtData.damage;
            _rtData.damage *= damageMultiplier;
            cb.SetupSplit(position, direction, _rtData, _playerUpgrades, isCharged);
            _rtData.damage = originalDamage;
        }
    }

    public void ResetUpgrades()
    {
        _rtData.damage = _baseData.damage;
        _rtData.cooldown = _baseData.cooldown;
        _rtData.timeBetweenShots = _baseData.timeBetweenShots;
        _rtData.shotsPerBurst = _baseData.shotsPerBurst;
        _rtData.explosionRadius = _baseData.explosionRadius;
        _rtData.burnDamagePerTick = _baseData.burnDamagePerTick;
        _rtData.burnDuration = _baseData.burnDuration;
        _rtData.maxAmmo = _baseData.maxAmmo;
        _rtData.launchSpeed = _baseData.launchSpeed;
        _rtData.bulletSpeed = _baseData.bulletSpeed;
        _rtData.verticalArc = _baseData.verticalArc;
        _rtData.fireDamageMultiplier = 1f;
        InfiniteAmmo = false;
        _shotsSinceCharge = 0;
        _currentAmmo = _rtData.maxAmmo;
        _reloading = false;
        _reloadEndTime = 0f;
        _nextShotTime = 0f;
        OnAmmoChanged?.Invoke(_currentAmmo, _rtData.maxAmmo);
    }
}