using System;
using UnityEngine;

public class RafaEnemy : Enemy
{
    [Header("RafaEnemy")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float steerSpeed = 3f; // qué tan rápido curva

    private Transform _player;
    private Vector3 _currentDir;

    public event Action<RafaEnemy> OnDead;

    protected override void Awake()
    {
        base.Awake();
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _currentDir = transform.forward;
        _enemyHealth.Revive();
    }

    private void Update()
    {
        if (_player == null) return;

        Vector3 desiredDir = _player.position - transform.position;
        desiredDir.y = 0f;
        desiredDir.Normalize();

        // Interpola la dirección actual hacia la deseada  curva suave
        _currentDir = Vector3.Slerp(_currentDir, desiredDir, steerSpeed * Time.deltaTime);
        _currentDir.y = 0f;

        transform.position += _currentDir * speed * Time.deltaTime;

        if (_currentDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_currentDir);
            targetRot *= Quaternion.Euler(0f, -90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
       

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<RT_PlayerHealth>().TakeDamage(10f);
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, collision.contacts[0].point);
            _enemyHealth.TakeDamage(999f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        if (other.gameObject.CompareTag("Enemy"))
        {
            other.gameObject.GetComponent<EnemyHealth>().TakeDamage(999f);
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, explosionPoint);
        }
    }
}