using System.Collections.Generic;
using UnityEngine;

// Poné este script en el prefab del proyectil del burst.
// Se autodestruye al llegar a la distancia maxima.
public class BladeBurstProjectile : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _maxDistance;
    private float _damageCooldown;

    private Vector3 _direction;
    private Vector3 _startPosition;
    private Dictionary<GameObject, float> _hitTimers = new();
    private LayerMask _enemyLayers;

    public void Init(Vector3 direction, float damage, float speed, float maxDistance, float damageCooldown, LayerMask enemyLayers)
    {
        Debug.Log($"[BurstProjectile] Init - enemyLayers: {enemyLayers.value}");

        _direction = direction.normalized;
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
        _damageCooldown = damageCooldown;
        _enemyLayers = enemyLayers;
        _startPosition = transform.position;
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(_direction);

        if (Vector3.Distance(transform.position, _startPosition) >= _maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {

        var health = other.GetComponentInParent<EnemyHealth>();
        if (health == null) return;

        health.TakeDamage(_damage);
        _hitTimers[other.gameObject] = Time.time;
    }
}