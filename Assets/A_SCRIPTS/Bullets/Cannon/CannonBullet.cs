using System;
using System.Collections;
using UnityEngine;

public class CannonBullet : BulletsBase
{
    private Rigidbody _rb;
    private TrailRenderer _trail;
    private Transform _pointShoot;
    private RT_CannonData _rtData;
    float _side;
    private CannonBulletImpactIndicator _impactIndicator;

    [Header("Visual")]
    [SerializeField] private Transform _visualTransform;
    [Tooltip("TEMPORAL: destildar para probar sin el indicador de impacto")]
    [SerializeField] private bool _showImpactIndicator = true;

    [Header("Fuego (meteorito)")]
    [Tooltip("Sistema de partículas raíz del fuego (los hijos se manejan solos)")]
    [SerializeField] private ParticleSystem _fireVfx;
    private Vector3 _fireBaseScale = Vector3.one;

    [Header("Explosion")]
    [SerializeField] private LayerMask _damageLayers;
    [SerializeField] Collider _collider;
    [SerializeField] private LayerMask _enemyLayers;

    [Header("Split")]
    [Tooltip("En qué parte del vuelo se divide (0.5 = a mitad de camino)")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float _splitAtFlight = 0.5f;
    [SerializeField] private float _spreadAngle = 30f;
    [SerializeField] private float _splitDamageMultiplier = 0.5f;

    [Header("Ricochet")]
    [SerializeField] private int _maxRicochetBounces = 1;
    private int _ricochetCount = 0;

    [Header("Debug")]
    public bool debugRicochet = false;
    public bool debugSplit = false;

    private bool _isSplitBullet = false;
    private bool _isCharged = false;
    private RT_PlayerUpgrades _playerUpgrades;
    private PlayerUpgrades _islandUpgrades;
    private Collider _ignoredCollider;
    private Vector3 _visualBaseScale = Vector3.one;

    public event Action<Vector3, Vector3, float, bool> OnSplit;

    // Reliquias: (centro, enemigos que mató esa explosión)
    public static event Action<Vector3, int> OnBulletExploded;
    // Reliquias: la bala cayó al agua sin pegarle a nada (punto, capas que daña, vfx de explosión)
    public static event Action<Vector3, LayerMask, GameObject> OnBulletHitWater;

    private int _pierced = 0;
    private float _fireDamageMult = 1f;
    // Balitas de Metralla: daño propio y no disparan otras reliquias (ni minas, ni más metralla, ni Pleno)
    private bool _isShrapnel = false;
    private float _shrapnelDamageMult = 1f;

    public void MarkShrapnel(float damageMultiplier)
    {
        _isShrapnel = true;
        _shrapnelDamageMult = damageMultiplier;
    }

    private Vector3 _targetPoint;
    private Vector3 _spawnPosition;
    private float _flightTime;
    private Vector3 _landingPoint;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>();
        _impactIndicator = GetComponent<CannonBulletImpactIndicator>();
        if (_visualTransform != null)
            _visualBaseScale = _visualTransform.localScale;
        if (_fireVfx != null)
            _fireBaseScale = _fireVfx.transform.localScale;
    }

    public override void TurnOff()
    {
        StopAllCoroutines();
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _collider.enabled = false;
        StopFire();
        if (_impactIndicator != null)
            _impactIndicator.ResetIndicator();
        _trail.Clear();
        _ricochetCount = 0;
        _pierced = 0;
        _fireDamageMult = 1f;
        _isShrapnel = false;
        _shrapnelDamageMult = 1f;
        _ignoredCollider = null;
        _isSplitBullet = false;
        _isCharged = false;
        if (_visualTransform != null) _visualTransform.localScale = _visualBaseScale;
        OnSplit = null;
        gameObject.SetActive(false);
    }

    public void Setup(Transform point, RT_CannonData rtData, float side, RT_PlayerUpgrades playerUpgrades, PlayerUpgrades islandUpgrades, Vector3 targetPoint, bool isCharged = false, Vector3? spawnPosition = null)
    {
        _pointShoot = point;
        _spawnPosition = spawnPosition ?? point.position;
        _rtData = rtData;
        _side = side;
        _playerUpgrades = playerUpgrades;
        _islandUpgrades = islandUpgrades;
        _isSplitBullet = false;
        _isCharged = isCharged;
        _targetPoint = targetPoint;
        _fireDamageMult = rtData.fireDamageMultiplier;
        Launch();
    }

    public void SetupSplit(Vector3 position, Vector3 velocity, RT_CannonData rtData, RT_PlayerUpgrades playerUpgrades, bool isCharged = false)
    {
        _rtData = rtData;
        _playerUpgrades = playerUpgrades;
        _isSplitBullet = true;
        _isCharged = isCharged;
        _fireDamageMult = rtData.fireDamageMultiplier;
        if (_visualTransform != null)
            _visualTransform.localScale = _isCharged ? _visualBaseScale * _rtData.chargedBulletScale : _visualBaseScale;

        transform.position = position;
        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(velocity);
        _rb.angularVelocity = Vector3.zero;
        _rb.position = position;
        _rb.useGravity = true;
        _rb.linearVelocity = velocity;
        StartFire();

        StartCoroutine(ActivateColliderBulelt());
    }

    private void Launch()
    {
        transform.position = _spawnPosition;
        transform.rotation = _pointShoot.rotation;
        if (_visualTransform != null)
            _visualTransform.localScale = _isCharged ? _visualBaseScale * _rtData.chargedBulletScale : _visualBaseScale;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = _spawnPosition;
        _rb.rotation = _pointShoot.rotation;
        _rb.useGravity = true;

        Vector3 startVelocity = CalculateLaunchVelocity(_spawnPosition, _targetPoint);
        _rb.linearVelocity = startVelocity;
        CacheLanding(startVelocity);
        StartFire();

        if (_impactIndicator != null && _showImpactIndicator)
            _impactIndicator.SetPosition(_targetPoint);

        StartCoroutine(ActivateColliderBulelt());

        bool hasSplit = debugSplit || (_playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.DoubleShot));
        if (hasSplit && !_isSplitBullet)
            StartCoroutine(SplitCoroutine());
    }

    private Vector3 CalculateLaunchVelocity(Vector3 origin, Vector3 target)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = toTargetXZ.magnitude;
        float yOffset = toTarget.y;
        float maxSpeed = _rtData.launchSpeed;

        if (distance < 0.1f)
            return Vector3.up * maxSpeed;

        // Ángulo "deseado": lo más derecho posible, escalado por verticalArc
        // igual que antes (1 = arco normal, menos = más derecho y rápido).
        float baseAngleDeg = Mathf.Clamp(distance * 1.5f, 30f, 65f);
        float desiredAngleDeg = Mathf.Clamp(baseAngleDeg * Mathf.Max(0f, _rtData.verticalArc), 3f, 80f);
        float desiredAngle = desiredAngleDeg * Mathf.Deg2Rad;

        // Ángulo MÍNIMO necesario para que, con la velocidad máxima disponible
        // (_rtData.launchSpeed), la bala llegue exactamente al punto clickeado.
        // Si el ángulo "derecho" ya alcanza, se usa tal cual (sale bien recta).
        // Si el click está muy lejos y con ese ángulo no llegaría, se sube
        // automáticamente lo justo y necesario para que sí llegue.
        float angle = desiredAngle;
        if (maxSpeed > 0.01f)
        {
            float A = (gravity * distance * distance) / (2f * maxSpeed * maxSpeed);
            float discriminant = distance * distance - 4f * A * (yOffset + A);

            if (discriminant < 0f)
            {
                // Ni siquiera al ángulo de máximo alcance (45°) llega con esta
                // velocidad: el click está directamente fuera de rango. Tiramos
                // al ángulo de máximo alcance para acercarnos lo más posible
                // (si necesitás que llegue de verdad, subí launchSpeed).
                angle = Mathf.Max(desiredAngle, 45f * Mathf.Deg2Rad);
            }
            else
            {
                float sqrtD = Mathf.Sqrt(discriminant);
                float tLow = (distance - sqrtD) / (2f * A);
                float minRequiredAngle = Mathf.Max(0.01f, Mathf.Atan(tLow));
                angle = Mathf.Max(desiredAngle, minRequiredAngle);
            }
        }

        float cosAngle = Mathf.Cos(angle);
        float sinAngle = Mathf.Sin(angle);

        float denom = 2f * cosAngle * cosAngle * (distance * Mathf.Tan(angle) - yOffset);
        if (denom <= 0f)
        {
            float fallbackAngle = 45f * Mathf.Deg2Rad;
            Vector3 fallbackDir = toTargetXZ.normalized * Mathf.Cos(fallbackAngle) + Vector3.up * Mathf.Sin(fallbackAngle);
            return fallbackDir * maxSpeed;
        }

        float v = Mathf.Sqrt((gravity * distance * distance) / denom);
        v = Mathf.Clamp(v, 5f, maxSpeed);

        return toTargetXZ.normalized * v * cosAngle + Vector3.up * v * sinAngle;
    }

    // Calcula cuánto tarda la bala en caer y dónde cae realmente
    // (si el click está fuera de rango, es el punto real de caída, no el click).
    private void CacheLanding(Vector3 v0)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float dy = _spawnPosition.y - _targetPoint.y;
        float disc = v0.y * v0.y + 2f * g * dy;
        _flightTime = disc > 0f ? (v0.y + Mathf.Sqrt(disc)) / g : 0f;

        Vector3 flatVel = new Vector3(v0.x, 0f, v0.z);
        _landingPoint = _spawnPosition + flatVel * _flightTime;
        _landingPoint.y = _targetPoint.y;
    }

    private IEnumerator SplitCoroutine()
    {
        float splitTime = _flightTime * _splitAtFlight;
        yield return new WaitForSeconds(splitTime);

        if (!gameObject.activeInHierarchy) yield break;

        float remaining = Mathf.Max(0.05f, _flightTime - splitTime);
        Vector3 splitPos = transform.position;
        Vector3 toLanding = _landingPoint - splitPos;
        Vector3 toLandingFlat = new Vector3(toLanding.x, 0f, toLanding.z);
        float landingY = _landingPoint.y;

        bool wasCharged = _isCharged;
        var cachedOnSplit = OnSplit;
        OnSplit = null;
        Pool.Return(this);

        float[] angles = { -_spreadAngle, 0f, _spreadAngle };
        foreach (float angle in angles)
        {
            Vector3 childTarget = splitPos + Quaternion.Euler(0f, angle, 0f) * toLandingFlat;
            childTarget.y = landingY;

            // Velocidad exacta para llegar a childTarget en "remaining" segundos con gravedad
            Vector3 velocity = (childTarget - splitPos - 0.5f * Physics.gravity * remaining * remaining) / remaining;
            cachedOnSplit?.Invoke(splitPos, velocity, _splitDamageMultiplier, wasCharged);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == _ignoredCollider) return;

        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Enemy") || other.CompareTag("ShipEnemy") || other.CompareTag("DashBoss") || other.CompareTag("MortarBoss") || other.CompareTag("Chest")) // agregado "Chest"
        {
            // Bala fantasma: explota en el enemigo y sigue de largo
            if (!_isShrapnel && _pierced < RelicRuntime.PierceCount)
            {
                _pierced++;
                _ignoredCollider = other;
                Explode(explosionPoint);
                SpawnExplosionVFX(explosionPoint);
                return;
            }

            bool hasRicochet = debugRicochet || (_playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.Ricochet));

            if (_ricochetCount < _maxRicochetBounces && hasRicochet)
            {
                _ricochetCount++;
                Ricochet(other);
                return;
            }

            Explode(explosionPoint);
            SpawnExplosionVFX(explosionPoint);
            Pool.Return(this);
        }

        if (other.CompareTag("Floor"))
        {
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint);
            WaterImpacts.Spawn(explosionPoint, _isCharged ? 1.6f : 1f);
            if (!_isShrapnel) OnBulletHitWater?.Invoke(explosionPoint, _damageLayers, _rtData.explosionVFX);
            Pool.Return(this);
        }
    }

    private void Ricochet(Collider hitCollider)
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, 10f, _enemyLayers);
        Collider bestTarget = null;
        float bestDist = Mathf.Infinity;

        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] == hitCollider) continue;
            float dist = Vector3.Distance(transform.position, nearby[i].transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = nearby[i];
            }
        }

        Explode(hitCollider.ClosestPoint(transform.position));
        SpawnExplosionVFX(hitCollider.ClosestPoint(transform.position));

        if (bestTarget != null)
        {
            _ignoredCollider = hitCollider;
            Vector3 dir = (bestTarget.transform.position - transform.position).normalized;
            dir.y = 0f;
            transform.position += dir * 0.5f;
            _rb.useGravity = false;
            _rb.linearVelocity = dir * _rtData.bulletSpeed;
            StartCoroutine(DropBullet());
        }
        else
        {
            Pool.Return(this);
        }
    }

    private void SpawnExplosionVFX(Vector3 position)
    {
        GameObject vfx = ParticlePool.Instance.GetParticle(_rtData.explosionVFX, position);
        if (vfx != null)
            vfx.transform.localScale = _isCharged ? Vector3.one * _rtData.chargedVfxScale : Vector3.one;
    }

    private void Explode(Vector3 center)
    {
        float baseDamage = _rtData.damage;
        if (_isCharged) baseDamage *= _rtData.chargedDamageMultiplier;
        baseDamage *= _fireDamageMult;
        if (_isShrapnel) baseDamage *= _shrapnelDamageMult;
        bool isCrit = _islandUpgrades != null && UnityEngine.Random.value < _islandUpgrades.Stats.critChance;
        float damage = isCrit ? baseDamage * _islandUpgrades.Stats.critMultiplier : baseDamage;

        float radius = _isCharged ? _rtData.explosionRadius * _rtData.chargedExplosionMultiplier : _rtData.explosionRadius;
        bool hasBurn = !_isShrapnel && _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.BurnShot);

        Collider[] hits = Physics.OverlapSphere(center, radius, _damageLayers);
        int kills = 0;
        bool prevSuppress = RelicRuntime.SuppressProcs;
        if (_isShrapnel) RelicRuntime.SuppressProcs = true; // lo que mate una balita no genera más balitas
        DamageContext.Set(isCrit, DamageKind.Normal); // para los damage numbers
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                var comp = damageable as Component;
                bool wasAlive = comp != null && comp.gameObject.activeInHierarchy;
                damageable.TakeDamage(damage); // <-- damage, no _rtData.damage
                if (wasAlive && !comp.gameObject.activeInHierarchy) kills++;
                if (hasBurn)
                    ApplyBurn(damageable);
            }
        }
        DamageContext.Clear();
        RelicRuntime.SuppressProcs = prevSuppress;
        if (!_isShrapnel) OnBulletExploded?.Invoke(center, kills);

        bool hasGroundFire = _playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.GroundFire);
        if (hasGroundFire)
            SpawnGroundFire(center);
    }

    private void ApplyBurn(IDamageable damageable)
    {
        Component targetComponent = damageable as Component;
        if (targetComponent == null) return;

        // Si el mismo golpe que aplic� el burn ya mat�/desactiv� al enemigo
        // (pool, animaci�n de muerte, etc.), no tiene sentido prenderlo fuego.
        if (!targetComponent.gameObject.activeInHierarchy) return;

        BurnStatus burn = targetComponent.GetComponent<BurnStatus>();
        if (burn == null)
            burn = targetComponent.gameObject.AddComponent<BurnStatus>();

        burn.ApplyBurn(damageable, _rtData.burnDamagePerTick, _rtData.burnTickInterval, _rtData.burnDuration, _rtData.burnVfxPrefab, _rtData.burnVfxOffset);
    }

    private void SpawnGroundFire(Vector3 center)
    {
        if (_rtData.groundFireZonePrefab == null) return;

        FireZone fireZone = Instantiate(_rtData.groundFireZonePrefab, center, Quaternion.identity).GetComponent<FireZone>();
        if (fireZone != null)
            fireZone.Init(_rtData.groundFireDuration, _rtData.groundFireDamagePerSecond, _rtData.groundFireRadius, _damageLayers);
    }

    // Se limpia y reinicia en cada disparo: como la bala viene del pool, si no
    // se limpia aparece una línea de fuego desde donde murió hasta el cañón.
    private void StartFire()
    {
        if (_fireVfx == null) return;
        float scale = (_isCharged && _rtData != null) ? _rtData.chargedBulletScale : 1f;
        _fireVfx.transform.localScale = _fireBaseScale * scale;
        _fireVfx.Clear(true);
        _fireVfx.Play(true);
    }

    private void StopFire()
    {
        if (_fireVfx == null) return;
        _fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private IEnumerator ActivateColliderBulelt()
    {
        yield return new WaitForSeconds(0.1f);
        _collider.enabled = true;
    }

    private IEnumerator DropBullet()
    {
        yield return new WaitForSeconds(_rtData.dropDelay);
        _rb.useGravity = true;
    }
}