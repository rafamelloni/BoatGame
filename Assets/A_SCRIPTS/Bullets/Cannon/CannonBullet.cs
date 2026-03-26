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

    private Vector3 _lastExplosionPoint;
    private bool _showLastExplosion;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>();
        _impactIndicator = GetComponent<CannonBulletImpactIndicator>();
    }

    public override void TurnOff()
    {
        gameObject.SetActive(false);
        if (_impactIndicator != null)
            _impactIndicator.ResetIndicator();
        _trail.Clear();
    }

    public void Setup(Transform point, RT_CannonData rtData, float side)
    {
        _pointShoot = point;
        _rtData = rtData;
        _side = side;
        Launch();
    }

    private void Launch()
    {

        //When it fires

        transform.position = _pointShoot.position;
        transform.rotation = _pointShoot.rotation;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = _pointShoot.position;
        _rb.rotation = _pointShoot.rotation;

        Vector3 dir = _pointShoot.right * _side;
        dir.y = 0f;
        dir.Normalize();

        Vector3 shootDir = (dir + Vector3.up * _rtData.verticalArc).normalized;

        Vector3 startVelocity = shootDir * _rtData.bulletSpeed;
        _rb.linearVelocity = startVelocity;

        if (_impactIndicator != null)
            _impactIndicator.Init(_pointShoot.position, startVelocity);

    }
    private void OnTriggerEnter(Collider other)
    {
        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Enemy"))
        {
            Explode(explosionPoint);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
        }
        if (other.CompareTag("Floor"))
        {
            Vector3 explosionPoint0 = other.ClosestPoint(transform.position);
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint0);

        }
       
        

        Pool.Return(this);
    }

    private void Explode(Vector3 center)
    {
        _lastExplosionPoint = center;
        _showLastExplosion = true;

        Collider[] hits = Physics.OverlapSphere(center, _explosionRadius, _damageLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(_rtData.damage);
            }
        }
    }
}
