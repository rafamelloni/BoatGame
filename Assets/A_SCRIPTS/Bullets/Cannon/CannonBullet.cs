using UnityEngine;

public class CannonBullet : BulletsBase
{
    private ParticlePool _particlePool;
    private GameObject _explosionVFX;
    private Rigidbody _rb;
    private TrailRenderer _trail;
    private Transform _pointShoot;
    private RT_CannonData _rtData;
    float _side;

    [Header("Explosion")]
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private LayerMask _damageLayers;

    private Vector3 _lastExplosionPoint;
    private bool _showLastExplosion;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>();
    }

    public override void TurnOff()
    {
        gameObject.SetActive(false);
        _trail.Clear();
    }

    public void Setup(ParticlePool poolParticle, Transform point, RT_CannonData rtData, float side, GameObject explosionVFX)
    {
        _explosionVFX = explosionVFX;
        _particlePool = poolParticle;
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
        _rb.linearVelocity = shootDir * _rtData.bulletSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 explosionPoint = contact.point;

            Explode(explosionPoint);

            if (_particlePool != null && _explosionVFX != null)
            {
                _particlePool.GetParticle(_explosionVFX, explosionPoint);
            }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (Application.isPlaying && _showLastExplosion)
        {
            Gizmos.DrawWireSphere(_lastExplosionPoint, _explosionRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
    }

}
