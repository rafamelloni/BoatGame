using UnityEngine;

public class CannonBulletIsland : BulletsBase
{

    private Rigidbody _rb;
    private TrailRenderer _trail;
    private Transform _pointShoot;
    private RT_CannonData _rtData;
    float _side;
    private CannonBulletImpactIndicator _impactIndicator;
    private bool _aimed;

  


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
        _aimed = false;
        _pointShoot = point;
        _rtData = rtData;
        _side = side;
        Launch();
    }

    // Tira la bala con arco para que caiga en el punto indicado
    public void SetupAimed(Transform point, RT_CannonData rtData, Vector3 target, float speed)
    {
        _aimed = true;
        _pointShoot = point;
        _rtData = rtData;

        transform.position = point.position;
        transform.rotation = point.rotation;
        _rb.position = point.position;
        _rb.rotation = point.rotation;
        _rb.angularVelocity = Vector3.zero;

        Vector3 toTarget = target - point.position;
        Vector3 flat = toTarget;
        flat.y = 0f;
        float flightTime = Mathf.Max(flat.magnitude / Mathf.Max(speed, 0.01f), 0.2f);

        Vector3 startVelocity = (toTarget - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;
        _rb.linearVelocity = startVelocity;

        if (_impactIndicator != null)
            _impactIndicator.Init(point.position, startVelocity);
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
        // Las balas apuntadas solo terminan en el jugador o en el agua:
        // así no se cortan contra el barco que las disparó ni contra otros enemigos
        if (_aimed && !other.CompareTag("Player") && !other.CompareTag("Floor")) return;

        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Player"))
        {
            Explode(explosionPoint);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            other.gameObject.GetComponent<RT_PlayerHealth>().TakeDamage(20f);
        }
        if (other.CompareTag("Floor"))
        {
            Vector3 explosionPoint0 = other.ClosestPoint(transform.position);
            ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, explosionPoint0);
            WaterImpacts.Spawn(explosionPoint0);

        }



        Pool.Return(this);
    }

    private void Explode(Vector3 center)
    {
      

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
