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

    [Header("Explosion")]
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private LayerMask _damageLayers;
    [SerializeField] Collider _collider;
    [SerializeField] private LayerMask _enemyLayers;

    [Header("Split")]
    [SerializeField] private float _splitDelay = 1f;
    [SerializeField] private float _spreadAngle = 30f;
    [SerializeField] private float _splitDamageMultiplier = 0.5f;

    [Header("Ricochet")]
    [SerializeField] private int _maxRicochetBounces = 1;
    private int _ricochetCount = 0;

    [Header("Debug")]
    public bool debugRicochet = false;
    public bool debugSplit = false;

    private bool _isSplitBullet = false;
    private RT_PlayerUpgrades _playerUpgrades;
    private Collider _ignoredCollider;

    public event Action<Vector3, Vector3, float> OnSplit;

    private Vector3 _targetPoint;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>();
        _impactIndicator = GetComponent<CannonBulletImpactIndicator>();
        transform.localScale = Vector3.one;
    }

    public override void TurnOff()
    {
        StopAllCoroutines();
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _collider.enabled = false;
        if (_impactIndicator != null)
            _impactIndicator.ResetIndicator();
        _trail.Clear();
        _ricochetCount = 0;
        _ignoredCollider = null;
        _isSplitBullet = false;
        OnSplit = null;
        gameObject.SetActive(false);
    }

    public void Setup(Transform point, RT_CannonData rtData, float side, RT_PlayerUpgrades playerUpgrades, Vector3 targetPoint)
    {
        _pointShoot = point;
        _rtData = rtData;
        _side = side;
        _playerUpgrades = playerUpgrades;
        _isSplitBullet = false;
        _targetPoint = targetPoint;
        Launch();
    }

    public void SetupSplit(Vector3 position, Vector3 direction, RT_CannonData rtData, RT_PlayerUpgrades playerUpgrades)
    {
        _rtData = rtData;
        _playerUpgrades = playerUpgrades;
        _isSplitBullet = true;

        transform.position = position;
        transform.rotation = Quaternion.LookRotation(direction);
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = position;
        _rb.useGravity = false;
        _rb.linearVelocity = direction * rtData.bulletSpeed;

        StartCoroutine(ActivateColliderBulelt());
        StartCoroutine(DropBullet());
    }

    private void Launch()
    {
        transform.position = _pointShoot.position;
        transform.rotation = _pointShoot.rotation;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = _pointShoot.position;
        _rb.rotation = _pointShoot.rotation;
        _rb.useGravity = true;

        Vector3 startVelocity = CalculateLaunchVelocity(_pointShoot.position, _targetPoint);
        _rb.linearVelocity = startVelocity;

        if (_impactIndicator != null)
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
        float v = _rtData.launchSpeed;

        if (distance < 0.1f)
            return Vector3.up * v;

        float angle = Mathf.Clamp(distance * 1.5f, 30f, 65f) * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angle);
        float sinAngle = Mathf.Sin(angle);

        float denom = 2f * cosAngle * cosAngle * (distance * Mathf.Tan(angle) - yOffset);
        if (denom <= 0f)
        {
            float fallbackAngle = 45f * Mathf.Deg2Rad;
            Vector3 fallbackDir = toTargetXZ.normalized * Mathf.Cos(fallbackAngle) + Vector3.up * Mathf.Sin(fallbackAngle);
            return fallbackDir * v;
        }

        v = Mathf.Sqrt((gravity * distance * distance) / denom);
        v = Mathf.Clamp(v, 5f, _rtData.launchSpeed);

        return toTargetXZ.normalized * v * cosAngle + Vector3.up * v * sinAngle;
    }

    private IEnumerator SplitCoroutine()
    {
        yield return new WaitForSeconds(_splitDelay);

        if (!gameObject.activeInHierarchy) yield break;

        Vector3 currentDir = _rb.linearVelocity.normalized;
        currentDir.y = 0f;
        currentDir.Normalize();

        Vector3 splitPos = transform.position;
        var cachedOnSplit = OnSplit;
        OnSplit = null;
        Pool.Return(this);

        float[] angles = { -_spreadAngle, 0f, _spreadAngle };
        foreach (float angle in angles)
        {
            Vector3 splitDir = Quaternion.Euler(0f, angle, 0f) * currentDir;
            cachedOnSplit?.Invoke(splitPos, splitDir, _splitDamageMultiplier);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == _ignoredCollider) return;

        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Enemy") || other.CompareTag("ShipEnemy") || other.CompareTag("DashBoss") || other.CompareTag("MortarBoss"))
        {
            bool hasRicochet = debugRicochet || (_playerUpgrades != null && _playerUpgrades.HasAbility(SpecialAbilityType.Ricochet));

            if (_ricochetCount < _maxRicochetBounces && hasRicochet)
            {
                _ricochetCount++;
                Ricochet(other);
                return;
            }

            Explode(explosionPoint);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            Pool.Return(this);
        }

        if (other.CompareTag("Floor"))
        {
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint);
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
        ParticlePool.Instance.GetParticle(_rtData.explosionVFX, hitCollider.ClosestPoint(transform.position));

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

    private void Explode(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, _explosionRadius, _damageLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(_rtData.damage);
        }
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