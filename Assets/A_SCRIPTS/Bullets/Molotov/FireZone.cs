using UnityEngine;

public class FireZone : MonoBehaviour
{
    private float _duration;
    private float _damagePerSecond;
    private float _radius;
    private LayerMask _damageLayers;
    private float _timer = 0f;
    private float _damageTickInterval = 0.5f;
    private float _damageTickTimer = 0f;

    public void Init(float duration, float damagePerSecond, float radius, LayerMask damageLayers)
    {
        _duration = duration;
        _damagePerSecond = damagePerSecond;
        _radius = radius;
        _damageLayers = damageLayers;

        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            ps.Play();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        _damageTickTimer += Time.deltaTime;

        if (_damageTickTimer >= _damageTickInterval)
        {
            _damageTickTimer = 0f;
            DamageTick();
        }

        if (_timer >= _duration)
            Destroy(gameObject);
    }

    private void DamageTick()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _damageLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(_damagePerSecond * _damageTickInterval);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}