using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerCollisions : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public string IslandTag = "Islas";
    public string enemyShipTag = "ShipEnemy";
    public string bossShipTag = "DashBoss";
    [SerializeField] ParticleSystem SmokeParticles;

    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float damageWhenBumpingEnemies = 15f;
    [SerializeField] private float damageWhenBumpingBoss = 25f;
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


        if (collision.gameObject.CompareTag(enemyShipTag) || collision.gameObject.CompareTag("MortarBoss"))
        {
           
            Vector3 knockbackDir = transform.position - collision.transform.position;
            knockbackDir.y = 0f;
            knockbackDir.Normalize();
            _movement.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);
            _health.TakeDamage(damageWhenBumpingEnemies);
        }

        if (collision.gameObject.CompareTag(IslandTag))
        {
            ContactPoint contact = collision.contacts[0];

            ParticleSystem particles = Instantiate(
                SmokeParticles,
                contact.point,                          //  punto exacto
                Quaternion.LookRotation(contact.normal) //  orientado a la superficie
            );

            Destroy(particles.gameObject, 2f); // limpiar


            Vector3 knockbackDir = transform.position - collision.transform.position;
            knockbackDir.y = 0f;
            knockbackDir.Normalize();
            _movement.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);
        }

        if (collision.gameObject.CompareTag(bossShipTag))
        {

            //collision.gameObject.GetComponent<EnemyHealth>()?.TakeDamage(9999);
            _health.TakeDamage(damageWhenBumpingBoss);


        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(enemyTag))
        {
            other.gameObject.GetComponent<EnemyHealth>()?.TakeDamage(9999);
            _health.TakeDamage(damageWhenBumpingEnemies);
        }
    }
}