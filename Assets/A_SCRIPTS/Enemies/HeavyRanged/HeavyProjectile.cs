using UnityEngine;

// Proyectil grande y lento del HeavyRangedEnemy.
// Usa el mismo sistema de pool que tus balas (BulletsBase + BulletFactory + ObjectPool).
//  - Sale en línea recta hacia donde estaba el player al disparar y NO corrige.
//  - Daña al player al tocarlo (RT_PlayerHealth), y se destruye contra islas.
//  - Limpia la estela al volver al pool y al relanzarse (sin rayas de un lado al otro del mapa).
//  - LaunchArc: en parábola hasta el punto donde estaba el player; al caer daña en un radio.
// Necesita un Collider en modo Trigger (lo arma la herramienta del menú).
public class HeavyProjectile : BulletsBase
{
    [SerializeField] private TrailRenderer _trail;
    [Tooltip("Lo que se escala con el tamaño. Vacío = el propio objeto.")]
    [SerializeField] private Transform _visual;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private string _islandTag = "Islas";

    [Header("Humo detrás de la bala (para que se lea aunque vaya lenta)")]
    [SerializeField] private bool _smoke = true;
    [Tooltip("Bocanadas por metro recorrido")]
    [SerializeField] private float _smokePerMeter = 2.5f;
    [SerializeField] private float _smokeLifetime = 1.6f;
    [Tooltip("Tamaño de cada bocanada relativo al tamaño de la bala")]
    [SerializeField] private Vector2 _smokeSize = new Vector2(0.35f, 0.6f);
    [SerializeField] private Color _smokeColorStart = new Color(1f, 0.75f, 0.35f, 0.9f);
    [SerializeField] private Color _smokeColorEnd = new Color(0.35f, 0.3f, 0.3f, 0f);
    [Tooltip("Material del humo (vacío = el mismo de la estela)")]
    [SerializeField] private Material _smokeMaterial;

    private ParticleSystem _smokePS;
    private float _size = 1f;

    private Vector3 _dir;
    private float _speed;
    private float _damage;
    private float _life;
    private GameObject _impactVfx;
    private bool _alive;

    // parábola
    private bool _arc;
    private Vector3 _arcFrom, _arcTo;
    private float _arcHeight, _arcT, _arcDuration, _landingRadius;
    private GameObject _landingVfx;

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
        if (_smoke) BuildSmoke();
    }

    private void BuildSmoke()
    {
        var go = new GameObject("Smoke");
        go.transform.SetParent(transform, false);
        _smokePS = go.AddComponent<ParticleSystem>();
        _smokePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _smokePS.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(_smokeLifetime * 0.7f, _smokeLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 120;

        var em = _smokePS.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = _smokePerMeter;

        var shape = _smokePS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var sol = _smokePS.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.3f)));

        var col = _smokePS.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(_smokeColorStart, 0f), new GradientColorKey(_smokeColorEnd, 1f) },
            new[] { new GradientAlphaKey(_smokeColorStart.a, 0f), new GradientAlphaKey(_smokeColorStart.a * 0.6f, 0.4f), new GradientAlphaKey(_smokeColorEnd.a, 1f) });
        col.color = g;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        var mat = _smokeMaterial != null ? _smokeMaterial : (_trail != null ? _trail.sharedMaterial : null);
        if (mat != null) rend.sharedMaterial = mat;
    }

    private void RestartSmoke()
    {
        if (_smokePS == null) return;
        _smokePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = _smokePS.main;
        main.startSize = new ParticleSystem.MinMaxCurve(_smokeSize.x * _size, _smokeSize.y * _size);
        _smokePS.Play(true);
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
        _size = size;

        _speed = speed;
        _damage = damage;
        _life = lifetime;
        _impactVfx = impactVfx;
        _alive = true;
        _arc = false;

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
        RestartSmoke(); // después de posicionar: así no tira humo desde donde estaba
    }

    // Parábola: sale de "from" y cae exactamente en "to". speed = velocidad horizontal (m/s)
    public void LaunchArc(Vector3 from, Vector3 to, float speed, float arcHeight, float damage, float size,
                          float landingRadius, GameObject landingVfx, GameObject impactVfx, TrailSettings trail)
    {
        Vector3 flat = to - from; flat.y = 0f;
        Launch(from, flat, speed, damage, size, 999f, impactVfx, trail);
        _arc = true;
        _arcFrom = from;
        _arcTo = to;
        _arcHeight = arcHeight;
        _arcT = 0f;
        _arcDuration = Mathf.Max(0.3f, flat.magnitude / Mathf.Max(speed, 0.01f));
        _landingRadius = landingRadius;
        _landingVfx = landingVfx;
    }

    private void Update()
    {
        if (!_alive) return;

        if (_arc)
        {
            _arcT += Time.deltaTime / _arcDuration;
            float k = Mathf.Clamp01(_arcT);
            Vector3 prev = transform.position;
            Vector3 pos = Vector3.Lerp(_arcFrom, _arcTo, k) + Vector3.up * (4f * _arcHeight * k * (1f - k));
            transform.position = pos;
            Vector3 vel = pos - prev;
            if (vel.sqrMagnitude > 0.000001f) transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
            if (k >= 1f) Land();
            return;
        }

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
        else if (!_arc && other.CompareTag(_islandTag)) // en parábola pasa por arriba de las islas
        {
            Despawn(true);
        }
    }

    // cae: daña en un radio y salpica
    private void Land()
    {
        if (!_alive) return;
        var hits = Physics.OverlapSphere(transform.position, _landingRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (!h.CompareTag(_playerTag)) continue;
            var health = h.GetComponent<RT_PlayerHealth>();
            if (health == null) health = h.GetComponentInParent<RT_PlayerHealth>();
            if (health != null) { health.TakeDamage(_damage); break; }
        }
        if (_landingVfx != null && ParticlePool.Instance != null)
            ParticlePool.Instance.GetParticle(_landingVfx, transform.position);
        _alive = false;
        if (Pool != null) Pool.Return(this);
        else TurnOff();
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
        if (_smokePS != null) _smokePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }
}
