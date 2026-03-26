using UnityEngine;
using static UnityEngine.ParticleSystem;

public class BarrelExplosion : BulletsBase
{

    [SerializeField] float rayDistance = 5f;
    [SerializeField] LayerMask floorLayer;


    private RT_MortarData _rtData;
    private bool _hasSpawned = false;
    private GameObject _circleInstance;
    private Transform _pos;
    private TrailRenderer _trail;
    private Rigidbody _rb;

    [Header("Explosion")]
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private LayerMask _damageLayers;

    private Vector3 _lastExplosionPoint;
    private bool _showLastExplosion;

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
    public void Setup( Transform pos, RT_MortarData rtData)
    {
        _pos = pos;
        _rtData = rtData;


        Launch();
    }

    void Launch()
    {
        if (_hasSpawned) return;

        transform.position = _pos.position;
        transform.rotation = Quaternion.identity;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.AddForce(Vector3.down * _rtData.fallingSpeed, ForceMode.Acceleration);


        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, floorLayer))
        {
            _circleInstance = Instantiate(
                _rtData.circleVFX,
                hit.point,
                Quaternion.identity
            );
            _hasSpawned = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Enemy"))
        {
            Explode(explosionPoint);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            _hasSpawned = false;

        }

        if (other.gameObject.CompareTag("Floor"))
        {
            Vector3 explosionPoint0 = other.ClosestPoint(transform.position);
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint0);
            print("floor");
            _hasSpawned = false;

        }

        if (_circleInstance != null)
            Destroy(_circleInstance);
        Pool.Return(this);
        _hasSpawned = false;
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
