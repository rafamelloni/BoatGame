using System.Collections.Generic;
using UnityEngine;

// Poné este script en el prefab de cada blade.
// BladesStrategy lo inicializa automáticamente.
public class BladeOrbitBehaviour : MonoBehaviour
{
    private float _damage;
    private float _damageCooldown;
    private Dictionary<GameObject, float> _hitTimers = new();

    public void Init(float damage, float damageCooldown)
    {
        _damage = damage;
        _damageCooldown = damageCooldown;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_hitTimers.TryGetValue(other.gameObject, out float lastHit))
            if (Time.time - lastHit < _damageCooldown) return;

        //var health = other.GetComponentInParent<Health>();
      // if (health == null) return;

       //health.TakeDamage(_damage);
        _hitTimers[other.gameObject] = Time.time;
    }

    private void OnDisable()
    {
        _hitTimers.Clear();
    }
}