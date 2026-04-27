using UnityEngine;

public class PlayerCollisions : MonoBehaviour
{
    public string enemyTag = "Enemy";
    RT_PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<RT_PlayerHealth>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(enemyTag))
        {
            collision.gameObject.GetComponent<EnemyHealth>()?.TakeDamage(9999);
            _health.TakeDamage(20f);
        }
    }
}
