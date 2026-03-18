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
        if (collision.gameObject.CompareTag("Floor"))
        {
            ContactPoint contact = collision.contacts[0];
            _particlePool.GetParticle(_explosionVFX, contact.point);
            print(_particlePool);

            //Instantiate(
            //    _explosionVfx,
            //    contact.point,
            //     Quaternion.LookRotation(contact.normal)
            //);


            Pool.Return(this);
        }
    }

}
