using UnityEngine;

public class BarrelExplosion : BulletsBase
{
    [SerializeField] float rayDistance = 5f;
    [SerializeField] LayerMask floorLayer;

    private RT_MortarData _rtData;
    private bool _hasSpawned = false;
    private Transform _pos;
    private TrailRenderer _trail;
    private Rigidbody _rb;

    [Header("Explosion")]
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private LayerMask _damageLayers;

    [Header("Indicator")]
    [SerializeField] private CannonBulletImpactIndicator _indicator;

    private Vector3 _spawnOffset;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        _rb = GetComponent<Rigidbody>();
    }

    public override void TurnOff()
    {
        gameObject.SetActive(false);
        _trail.Clear();
    }

    public void Setup(Transform pos, RT_MortarData rtData, Vector3 targetPosition, Vector3 spawnOffset = default)
    {
        _pos = pos;
        _rtData = rtData;
        _spawnOffset = spawnOffset;
        Launch(targetPosition);
    }

    void Launch(Vector3 targetPosition)
    {
        if (_hasSpawned) return;

        transform.position = _pos.position;
        transform.rotation = Quaternion.identity;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.AddForce(Vector3.down * _rtData.fallingSpeed, ForceMode.Acceleration);

        if (_indicator != null)
            _indicator.SetPosition(targetPosition);

        _hasSpawned = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Enemy") || other.CompareTag("ShipEnemy"))
        {
            Explode(explosionPoint);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            _hasSpawned = false;
        }

        if (other.gameObject.CompareTag("Floor"))
        {
            Vector3 explosionPoint0 = other.ClosestPoint(transform.position);
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint0);
            _hasSpawned = false;
        }

        if (_indicator != null)
            _indicator.ResetIndicator();

        Pool.Return(this);
        _hasSpawned = false;
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
}