using UnityEngine;

public class CannonBulletBoss : BulletsBase
{
    private Rigidbody _rb;
    private TrailRenderer _trail;
    private RT_CannonData _rtData;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _trail = GetComponent<TrailRenderer>();
    }

    public override void TurnOff()
    {
        gameObject.SetActive(false);
        if (_trail != null) _trail.Clear();
    }

    public void Setup(Transform point, RT_CannonData rtData, Vector3 direction)
    {
        _rtData = rtData;
        Launch(point, direction);
    }

    private void Launch(Transform point, Vector3 direction)
    {
        transform.position = point.position;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = point.position;

        direction.y = 0f;
        direction.Normalize();

        Vector3 shootDir = (direction + Vector3.up * _rtData.verticalArc).normalized;
        _rb.linearVelocity = shootDir * _rtData.bulletSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 explosionPoint = other.ClosestPoint(transform.position);
            ParticlePool.Instance.GetParticle(_rtData.explosionVFX, explosionPoint);
            other.GetComponent<RT_PlayerHealth>().TakeDamage(20f);
            Pool.Return(this);
        }

        //if (other.CompareTag("Floor"))
        //{
        //    Vector3 splashPoint = other.ClosestPoint(transform.position);
        //    ParticlePool.Instance.GetParticle(_rtData.waterSplashVFX, splashPoint);
        //    Pool.Return(this);
        //}
    }
}