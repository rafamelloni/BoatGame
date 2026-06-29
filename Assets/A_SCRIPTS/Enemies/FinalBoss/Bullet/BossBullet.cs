using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] RT_PlayerHealth _healthPlayer;
    [SerializeField] GameObject explosionVFX;

    private Vector3 _direction;

    public void Launch(Vector3 direction)
    {
        _direction = direction.normalized;
        _direction.y = 0f;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;
    }

    public void setup(RT_PlayerHealth healthPlayer)
    {
        _healthPlayer = healthPlayer;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        Vector3 explosionPoint = other.ClosestPoint(transform.position);

        _healthPlayer.TakeDamage(20f);
        ParticlePool.Instance.GetParticle(explosionVFX, explosionPoint);

        Destroy(gameObject);
    }
} 