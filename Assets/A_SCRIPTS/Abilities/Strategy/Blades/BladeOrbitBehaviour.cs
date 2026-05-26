using System.Collections.Generic;
using UnityEngine;

// Poné este script en el prefab de cada blade.
// BladesStrategy lo inicializa automáticamente.
public class BladeOrbitBehaviour : MonoBehaviour
{
    private float _damage;
    private float _damageCooldown;
    private Dictionary<GameObject, float> _hitTimers = new();
    private LayerMask _enemyLayers;

    [SerializeField] private float rotationSpeed = 720f;

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    public void Init(float damage, float damageCooldown, LayerMask enemyLayers)
    {
        _damage = damage;
        _damageCooldown = damageCooldown;
        _enemyLayers = enemyLayers;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((_enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

        if (_hitTimers.TryGetValue(other.gameObject, out float lastHit))
            if (Time.time - lastHit < _damageCooldown) return;

        var health = other.GetComponentInParent<EnemyHealth>();
        if (health == null) return;

        health.TakeDamage(_damage);
        _hitTimers[other.gameObject] = Time.time;
    }

    private void OnDisable()
    {
        _hitTimers.Clear();
    }
}