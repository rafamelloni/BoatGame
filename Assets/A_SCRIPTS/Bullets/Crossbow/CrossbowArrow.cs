using UnityEngine;

public class CrossbowArrow : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private GameObject _explosionVfx;
    private GameObject _waterVfx;
    private bool _initialized = false;

    public void Init(Vector3 direction, float speed, float damage, GameObject explosionVfx, GameObject waterVfx)
    {
        _direction = direction;
        _speed = speed;
        _damage = damage;
        _explosionVfx = explosionVfx;
        _waterVfx = waterVfx;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag("Enemy") || other.CompareTag("ShipEnemy") ||
            other.CompareTag("DashBoss") || other.CompareTag("MortarBoss"))
        {
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            MortarBossHealth Mortar = other.GetComponent<MortarBossHealth>();

            if (Mortar != null)
                Mortar.TakeDamage(_damage);

            if (enemy != null)
                enemy.TakeDamage(_damage);

            if (_explosionVfx != null)
                ParticlePool.Instance.GetParticle(_explosionVfx, hitPoint);

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Floor"))
        {
            if (_waterVfx != null)
                ParticlePool.Instance.GetParticle(_waterVfx, hitPoint);

            Destroy(gameObject);
        }
    }
}