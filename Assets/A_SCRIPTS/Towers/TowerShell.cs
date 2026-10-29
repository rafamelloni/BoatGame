using UnityEngine;

// Bala de la TorreMortero: sale en parábola y cae exactamente en el punto marcado.
// Mientras vuela, en el agua se ve un círculo de aviso que se va llenando:
// cuando está lleno, cae. Al caer daña al jugador si está dentro del radio.
public class TowerShell : MonoBehaviour
{
    private Vector3 _from, _to;
    private float _flightTime, _arcHeight, _damage, _radius, _splashSize;
    private float _t;
    private bool _alive;
    private GameObject _landingVfx;
    private Transform _player;
    private TowerWarningCircle _warning;
    private TrailRenderer _trail;

    public bool IsFree => !gameObject.activeSelf;

    private void Awake()
    {
        _trail = GetComponentInChildren<TrailRenderer>(true);
        // la bala no choca con nada: solo cae en el punto
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
    }

    public void Launch(Vector3 from, Vector3 to, float flightTime, float arcHeight, float damage, float radius,
                       float size, Transform player, TowerWarningCircle warning, GameObject landingVfx, float splashSize)
    {
        _from = from;
        _to = to;
        _flightTime = Mathf.Max(0.2f, flightTime);
        _arcHeight = arcHeight;
        _damage = damage;
        _radius = radius;
        _player = player;
        _warning = warning;
        _landingVfx = landingVfx;
        _splashSize = splashSize;
        _t = 0f;
        _alive = true;

        if (_trail != null) { _trail.emitting = false; _trail.Clear(); }
        transform.position = from;
        transform.localScale = Vector3.one * size;
        gameObject.SetActive(true);
        if (_trail != null) { _trail.Clear(); _trail.emitting = true; }

        if (_warning != null) _warning.Show(to, radius);
    }

    private void Update()
    {
        if (!_alive) return;

        _t += Time.deltaTime / _flightTime;
        float k = Mathf.Clamp01(_t);
        Vector3 prev = transform.position;
        transform.position = Vector3.Lerp(_from, _to, k) + Vector3.up * (4f * _arcHeight * k * (1f - k));
        Vector3 vel = transform.position - prev;
        if (vel.sqrMagnitude > 0.000001f) transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);

        if (_warning != null) _warning.SetProgress(k);
        if (k >= 1f) Land();
    }

    private void Land()
    {
        _alive = false;

        if (_player != null)
        {
            Vector3 d = _player.position - _to;
            d.y = 0f;
            if (d.magnitude <= _radius)
            {
                var health = _player.GetComponentInParent<RT_PlayerHealth>();
                if (health != null) health.TakeDamage(_damage);
            }
        }

        if (_landingVfx != null && ParticlePool.Instance != null)
            ParticlePool.Instance.GetParticle(_landingVfx, _to);
        WaterImpacts.Spawn(_to, _splashSize);

        // el aviso hace su flash de impacto y se apaga solo
        if (_warning != null) { _warning.PlayImpact(); _warning = null; }
        Despawn();
    }

    public void Despawn()
    {
        _alive = false;
        if (_warning != null) { _warning.Hide(); _warning = null; }
        if (_trail != null) { _trail.emitting = false; _trail.Clear(); }
        gameObject.SetActive(false);
    }
}
