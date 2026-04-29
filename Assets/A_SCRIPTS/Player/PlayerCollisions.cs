using UnityEngine;

public class PlayerCollisions : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public string enemyShipTag = "ShipEnemy";
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float damageWhenBumpingEnemies = 15f;
    [SerializeField] private Movement _movement;

    RT_PlayerHealth _health;
    Rigidbody _rb;

    private void Awake()
    {
        _health = GetComponent<RT_PlayerHealth>();
        _rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(enemyTag))
        {
             collision.gameObject.GetComponent<EnemyHealth>()?.TakeDamage(9999);
            _health.TakeDamage(damageWhenBumpingEnemies);

            
        }

        if (collision.gameObject.CompareTag(enemyShipTag))
        {
            Vector3 knockbackDir = transform.position - collision.transform.position;
            knockbackDir.y = 0f;
            knockbackDir.Normalize();
            _movement.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);
            _health.TakeDamage(damageWhenBumpingEnemies);
        }

    }
}