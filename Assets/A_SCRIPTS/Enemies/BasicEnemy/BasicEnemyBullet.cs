using UnityEngine;
using static UnityEngine.ParticleSystem;

public class BasicEnemyBullet : BulletsBase
{

    private CannonBulletImpactIndicator _impactIndicator;
    private Rigidbody _rb;
    private float _currentTime;
        private TrailRenderer _trail;


    RT_EnemyStats _rtData;

    private void Awake()
    {
        _impactIndicator = GetComponent<CannonBulletImpactIndicator>();
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    public void SetTarget(Vector3 targetPosition, RT_EnemyStats datart, Transform shootPoint)
    {
        _rtData = datart;
        _currentTime = _rtData.lifeTime;
        transform.position = shootPoint.position;
        transform.rotation = shootPoint.rotation;
        _rb.position = shootPoint.position;

        Vector3 dir = targetPosition - transform.position;
        float distance = new Vector3(dir.x, 0f, dir.z).magnitude;
        float height = dir.y;

        float g = Mathf.Abs(Physics.gravity.y);
        float v = _rtData.bulletSpeed;
        float v2 = v * v;

        float discriminant = v2 * v2 - g * (g * distance * distance + 2f * height * v2);

        float dynamicArc;
        if (discriminant < 0f)
        {
            dynamicArc = _rtData.bulletVerticalArc;
        }
        else
        {
            float angle = Mathf.Atan2(v2 - Mathf.Sqrt(discriminant), g * distance);
            dynamicArc = Mathf.Tan(angle);
        }

        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z).normalized;
        Vector3 shootDir = (flatDir + Vector3.up * dynamicArc).normalized;
        _rb.linearVelocity = shootDir * _rtData.bulletSpeed;

        if (_impactIndicator != null)
            _impactIndicator.Init(transform.position, _rb.linearVelocity);
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
        _currentTime = _rtData != null ? _rtData.lifeTime : float.MaxValue;
        if (_rb != null)
            _rb.linearVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (_rb.linearVelocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity);

        _currentTime -= Time.deltaTime;
        if (_currentTime <= 0f)
        {
            Debug.Log($"Timer expired: {_currentTime}");
            Pool.Return(this);
        }
            
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Player"))
        {
            other.GetComponent<RT_PlayerHealth>()?.TakeDamage(_rtData.damage);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            Pool.Return(this);
            return;
        }

        if (other.CompareTag("Floor"))
        {
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint);
            Pool.Return(this);
            return;
        }

        if (other.CompareTag("Enemy")) return;

        Pool.Return(this);
    }



    private void Explode(Vector3 center)
    {
        //_lastExplosionPoint = center;
        //_showLastExplosion = true;

        //Collider[] hits = Physics.OverlapSphere(center, _explosionRadius, _damageLayers);

        //for (int i = 0; i < hits.Length; i++)
        //{
        //    IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();

        //    if (damageable != null)
        //    {
        //        damageable.TakeDamage(_rtData.damage);
        //    }
        //}
    }
}