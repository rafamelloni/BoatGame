using UnityEngine;

// Proyectil grande y lento del HeavyRangedEnemy.
// Usa el mismo sistema de pool que tus balas (BulletsBase + BulletFactory + ObjectPool).
//  - Sale en línea recta hacia donde estaba el player al disparar y NO corrige.
//  - Daña al player al tocarlo (RT_PlayerHealth), y se destruye contra islas.
//  - Limpia la estela al volver al pool y al relanzarse (sin rayas de un lado al otro del mapa).
// Necesita un Collider en modo Trigger (lo arma la herramienta del menú).
public class HeavyProjectile : BulletsBase
{
    [SerializeField] private TrailRenderer _trail;
    [Tooltip("Lo que se escala con el tamaño. Vacío = el propio objeto.")]
    [SerializeField] private Transform _visual;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private string _islandTag = "Islas";

    private Vector3 _dir;
    private float _speed;
    private float _damage;
    private float _life;
    private GameObject _impactVfx;
    private bool _alive;

    public struct TrailSettings
    {
        public bool apply;
        public float time;
        public float widthMultiplier;
        public Gradient color;
    }

    private void Awake()
    {
        if (_trail == null) _trail = GetComponentInChildren<TrailRenderer>(true);
        if (_visual == null) _visual = transform;
    }

    public void Launch(Vector3 position, Vector3 direction, float speed, float damage, float size,
                       float lifetime, GameObject impactVfx, TrailSettings trail)
    {
        // posicionar ANTES de que la estela emita, así no dibuja una raya desde donde murió
        if (_trail != null) { _trail.emitting = false; _trail.Clear(); }

        direction.y = 0f;
        _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(_dir, Vector3.up));
        _visual.localScale = Vector3.one * size;

        _speed = speed;
        _damage = damage;
        _life = lifetime;
        _impactVfx = impactVfx;
        _alive = true;

        if (_trail != null)
        {
            if (trail.apply)
            {
                _trail.time = trail.time;
                _trail.widthMultiplier = trail.widthMultiplier * size;
                if (trail.color != null) _trail.colorGradient = trail.color;
            }
            _trail.Clear();
            _trail.emitting = true;
        }
    }

    private void Update()
    {
        if (!_alive) return;
        transform.position += _dir * (_speed * Time.deltaTime);
        _life -= Time.deltaTime;
        if (_life <= 0f) Despawn(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_alive) return;

        if (other.CompareTag(_playerTag))
        {
            var health = other.GetComponent<RT_PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<RT_PlayerHealth>();
            if (health != null) health.TakeDamage(_damage);
            Despawn(true);
        }
        else if (other.CompareTag(_islandTag))
        {
            Despawn(true);
        }
    }

    private void Despawn(bool impact)
    {
        if (!_alive) return;
        _alive = false;
        if (impact && _impactVfx != null && ParticlePool.Instance != null)
            ParticlePool.Instance.GetParticle(_impactVfx, transform.position);

        if (Pool != null) Pool.Return(this);
        else TurnOff();
    }

    public override void TurnOff()
    {
        _alive = false;
        if (_trail != null) { _trail.emitting = false; _trail.Clear(); }
        gameObject.SetActive(false);
    }
}
