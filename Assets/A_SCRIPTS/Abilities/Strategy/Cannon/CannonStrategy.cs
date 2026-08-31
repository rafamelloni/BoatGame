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
    private float nextFireTime = 0f;
    followPlayer _camera;
    CannonRecoil _recoilLeft;
    CannonRecoil _recoilRight;
    private RT_PlayerUpgrades _playerUpgrades;
    private PlayerUpgrades _islandUpgrades;
    private int _shotsSinceCharge = 0;

    public event Action<float> OnCooldownStarted;
    public float CooldownDuration => _rtData.cooldown;
    public float RemainingCooldown => Mathf.Max(0f, nextFireTime - Time.time);
    public bool IsOnCooldown => Time.time < nextFireTime;
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
    }

    public void TryExecute()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + _rtData.cooldown;
        OnCooldownStarted?.Invoke(_rtData.cooldown);
        Vector3 targetPoint = GetMouseWorldPoint();
        runner.StartCoroutine(FireBurst(targetPoint));
    }

    private IEnumerator FireBurst(Vector3 targetPoint)
    {
        Vector3 shipPos = hardpoints.transform.position;
        Vector3 toTarget = targetPoint - shipPos;
        Vector3 mirroredTarget = shipPos + Vector3.Reflect(toTarget, hardpoints.transform.right);

        // Detectar de qué lado está el target
        float side = Vector3.Dot(toTarget, hardpoints.transform.right);

        Vector3 rightTarget = side >= 0f ? targetPoint : mirroredTarget;
        Vector3 leftTarget = side >= 0f ? mirroredTarget : targetPoint;

        for (int i = 0; i < _rtData.shotsPerBurst; i++)
        {
            bool isCharged = DetermineIfCharged();

            foreach (Transform p in hardpoints.rightShootPoints)
            {
                FireFromPoint(p, 1f, rightTarget, isCharged);
                _camera.ApplyCannonImpact();
                _recoilRight.Fire(_rtData.timeBetweenShots);
                _recoilLeft.Fire(_rtData.timeBetweenShots);
                hardpoints._cannonSmokeShootR.Play();
            }
            foreach (Transform p in hardpoints.leftShootPoints)
            {
                FireFromPoint(p, -1f, leftTarget, isCharged);
                hardpoints._cannonSmokeShootL.Play();
            }
            if (i < _rtData.shotsPerBurst - 1)
                yield return new WaitForSeconds(_rtData.timeBetweenShots);
        }
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
            cb.Setup(point, _rtData, side, _playerUpgrades, _islandUpgrades, targetPoint, isCharged);
        }
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
        _shotsSinceCharge = 0;
        nextFireTime = 0f;
    }
}