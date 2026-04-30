using UnityEngine;
using static UnityEngine.ParticleSystem;

public class HeavyIslandBullet : BulletsBase
{
    [Header("Data")]
    [SerializeField] private float _speed;
    [SerializeField] private float _lifeTime = 5f;
    private CannonBulletImpactIndicator _impactIndicator;
    private RT_CannonData _rtData;
    private TrailRenderer _trail;
    private Transform _shootPoint;
    private Vector3 velocity;

    private float _currentTime;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>();
        _impactIndicator = GetComponent<CannonBulletImpactIndicator>();
    }
    public void Setup(Transform shootPoint, float speed, RT_CannonData rtData)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = shootPoint.position;
        _rb.rotation = shootPoint.rotation;
        transform.position = shootPoint.position;
        transform.rotation = shootPoint.rotation;
        _rtData = rtData;
        Vector3 velocity = shootPoint.forward * speed;
        _rb.linearVelocity = velocity;

        if (_impactIndicator != null)
            _impactIndicator.Init(shootPoint.position, velocity);
    }
    private void Update()
    {
        _currentTime -= Time.deltaTime;
        if (_currentTime <= 0f)
        {
            Pool.Return(this);
            TurnOff();
        }
    }

    public override void TurnOff()
    {
        gameObject.SetActive(false);
        if (_impactIndicator != null)
            _impactIndicator.ResetIndicator();
        _trail.Clear();
    }
    private void OnEnable()
    {
        _currentTime = _lifeTime;
    }


    private void OnTriggerEnter(Collider other)
    {
        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.gameObject.CompareTag("Player"))
        {
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            other.gameObject.GetComponent<RT_PlayerHealth>().TakeDamage(20f);

        }
        if (other.gameObject.CompareTag("Floor"))
        {
            Vector3 explosionPoint0 = other.ClosestPoint(transform.position);
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint0);

        }


        Pool.Return(this);
    }
}