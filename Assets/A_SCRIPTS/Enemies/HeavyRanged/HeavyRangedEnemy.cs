using System.Collections.Generic;
using UnityEngine;

// Enemigo de largo alcance: se acerca, se frena a cierta distancia y dispara
// un proyectil enorme y lento (HeavyProjectile) cada X segundos.
//  - Siempre mira al player (rotación suave solo en Y), moviéndose o quieto.
//  - Si el player se aleja más de Re-engage Distance, vuelve a acercarse.
//  - Integrado como el resto: hereda de Enemy (EnemyHealth + SO_EnemyData), avisa OnDead
//    para volver al pool de la EnemyFactory, y funciona con EnemyEmerge y las fases.
//
// Game feel:
//  - Se mece con las olas (bob + inclinación), cada uno con su fase.
//  - Antes de disparar se inclina para atrás (carga) y dispara en parábola.
//  - El disparo lo empuja para atrás (knockback) con la trompa para abajo; cuando termina
//    el knockback la suelta y queda hamacándose (resorte poco amortiguado: forma de medialuna).
//  - VFX opcionales: al cargar, al disparar (prefab o ParticleSystem hijo) y al caer la bala.
public class HeavyRangedEnemy : Enemy
{
    public event System.Action<HeavyRangedEnemy> OnDead;

    private enum State { Chase, Attack }

    [Header("Movimiento")]
    [SerializeField] private float _moveSpeed = 4f;
    [Tooltip("Qué tan rápido acelera / frena")]
    [SerializeField] private float _acceleration = 4f;
    [Tooltip("Grados por segundo")]
    [SerializeField] private float _rotationSpeed = 120f;
    [Tooltip("Corrección si el modelo no mira hacia su +Z (ej. 90 o -90)")]
    [SerializeField] private float _modelYawOffset = 0f;

    [Header("Distancias")]
    [Tooltip("Se frena y empieza a disparar a esta distancia del player")]
    [SerializeField] private float _attackDistance = 25f;
    [Tooltip("Si el player se aleja más que esto, vuelve a perseguirlo (tiene que ser mayor que Attack Distance)")]
    [SerializeField] private float _reengageDistance = 32f;

    [Header("Disparo")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private HeavyProjectile _projectilePrefab;
    [Tooltip("Segundos entre disparos")]
    [SerializeField] private float _fireInterval = 3f;
    [Tooltip("Espera antes del primer disparo al frenar")]
    [SerializeField] private float _firstShotDelay = 0.8f;
    [Tooltip("Solo dispara si está mirando al player dentro de este ángulo")]
    [SerializeField] private float _maxAimAngle = 15f;
    [Tooltip("ParticleSystem hijo que se reproduce al disparar (opcional)")]
    [SerializeField] private ParticleSystem _muzzleVfx;

    [Header("Parábola")]
    [Tooltip("Si está apagado dispara recto como antes")]
    [SerializeField] private bool _useArc = true;
    [Tooltip("Altura máxima del arco, en metros")]
    [SerializeField] private float _arcHeight = 7f;
    [Tooltip("Radio de daño donde cae la bala")]
    [SerializeField] private float _landingRadius = 2.5f;

    [Header("Olas (idle)")]
    [SerializeField] private float _waveBob = 0.15f;
    [SerializeField] private float _waveBobFrequency = 1.2f;
    [Tooltip("Grados de inclinación por las olas")]
    [SerializeField] private float _waveTilt = 4f;
    [SerializeField] private float _waveTiltFrequency = 0.9f;

    [Header("Disparo — carga")]
    [Tooltip("Cuánto tarda en inclinarse para atrás antes de disparar")]
    [SerializeField] private float _windupTime = 0.5f;
    [Tooltip("Grados que se inclina para atrás al cargar")]
    [SerializeField] private float _windupLean = 16f;

    [Header("Disparo — knockback")]
    [Tooltip("Cuánto dura el empujón hacia atrás al disparar")]
    [SerializeField] private float _knockbackTime = 0.4f;
    [Tooltip("Metros que retrocede por el disparo")]
    [SerializeField] private float _knockbackDistance = 1.5f;
    [Tooltip("Grados que baja la trompa durante el knockback")]
    [SerializeField] private float _noseDownAngle = 15f;
    [Tooltip("Qué tan rápido baja la trompa al disparar")]
    [SerializeField] private float _noseDownSpeed = 18f;
    [Tooltip("Hundida en el agua al disparar (metros/seg)")]
    [SerializeField] private float _recoilSinkImpulse = 1.5f;

    [Header("Sway después del knockback")]
    [Tooltip("Empujón extra al soltar la trompa (más = hamaca más grande)")]
    [SerializeField] private float _swayKick = 60f;
    [Tooltip("Rigidez del resorte (más alto = hamaca más rápido)")]
    [SerializeField] private float _springStiffness = 45f;
    [Tooltip("Amortiguación (más bajo = se hamaca más veces antes de quedarse quieto)")]
    [SerializeField] private float _springDamping = 3.5f;

    [Header("VFX del disparo (opcionales)")]
    [Tooltip("ParticleSystem hijo que se reproduce al empezar a cargar")]
    [SerializeField] private ParticleSystem _windupVfx;
    [Tooltip("Prefab que se instancia (pool) en el FirePoint al disparar")]
    [SerializeField] private GameObject _muzzleVfxPrefab;
    [Tooltip("Prefab al caer la bala en el agua (si es nulo usa Impact Vfx)")]
    [SerializeField] private GameObject _landingVfx;

    [Header("Proyectil")]
    [Tooltip("Escala del proyectil (las balas normales son ~0.8)")]
    [SerializeField] private float _projectileSize = 2.5f;
    [Tooltip("Metros por segundo (lento a propósito: tiene que poder esquivarse)")]
    [SerializeField] private float _projectileSpeed = 6f;
    [SerializeField] private float _projectileDamage = 25f;
    [Tooltip("Segundos hasta que desaparece si no pega en nada")]
    [SerializeField] private float _projectileLifetime = 8f;
    [SerializeField] private GameObject _impactVfx;

    [Header("Estela del proyectil")]
    [Tooltip("Si está apagado, usa lo que tenga el TrailRenderer del prefab del proyectil")]
    [SerializeField] private bool _overrideTrail = true;
    [SerializeField] private float _trailTime = 1.2f;
    [Tooltip("Ancho de la estela relativo al tamaño del proyectil")]
    [SerializeField] private float _trailWidth = 0.8f;
    [SerializeField] private Gradient _trailColor = DefaultTrailGradient();

    [Header("Vida (si falta el SO)")]
    [SerializeField] private float _fallbackMaxHealth = 200f;

    // un BulletFactory compartido por prefab de proyectil (mismo pool que usan tus balas)
    private static readonly Dictionary<HeavyProjectile, BulletFactory> _factories = new();

    private Transform _player;
    private State _state;
    private float _currentSpeed;
    private float _nextFireTime;

    // feel
    private float _facingYaw;        // hacia dónde mira (sin el offset del modelo)
    private float _wavePhase;
    private float _windupTimer = -1f; // >= 0 mientras carga
    private float _pitch, _pitchVel;  // inclinación adelante/atrás (negativo = para atrás)
    private float _sink, _sinkVel;    // hundida vertical
    private float _knockbackTimer = -1f; // >= 0 mientras dura el knockback
    private Vector3 _knockbackDir;
    private Vector3 _appliedOffset;   // lo que sumamos a la posición el frame anterior

    protected override void Awake()
    {
        if (_enemyHealth == null) _enemyHealth = GetComponent<EnemyHealth>();
        if (baseData == null)
        {
            Debug.LogWarning("[HeavyRangedEnemy] Falta Base Data (SO_EnemyData): uso vida por defecto.", this);
            baseData = ScriptableObject.CreateInstance<SO_EnemyData>();
            baseData.maxHealth = _fallbackMaxHealth;
        }
        base.Awake();
        if (_enemyHealth != null) _enemyHealth.OnDeath += HandleDeath;
        if (_firePoint == null) _firePoint = transform;

        // si el modelo trae un FakeWaveMovement, lo reemplaza este feel (si no, pelean por la rotación)
        var fake = GetComponent<FakeWaveMovement>();
        if (fake != null) fake.enabled = false;
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null) _enemyHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath() => OnDead?.Invoke(this);

    // Lo llama la EnemyFactory al sacarlo del pool
    public void SetPlayer(Transform player)
    {
        _player = player;
        if (_enemyHealth != null) _enemyHealth.Revive();
        ResetState();
    }

    private void OnEnable() => ResetState();

    private void ResetState()
    {
        _state = State.Chase;
        _currentSpeed = 0f;
        _nextFireTime = 0f;
        _windupTimer = -1f;
        _pitch = _pitchVel = _sink = _sinkVel = 0f;
        _knockbackTimer = -1f;
        _appliedOffset = Vector3.zero; // la factory ya lo posicionó
        _facingYaw = transform.eulerAngles.y - _modelYawOffset;
        _wavePhase = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            _player = p.transform;
        }

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // estado con histéresis: no titubea en el borde de la distancia
        if (_state == State.Chase && dist <= _attackDistance)
        {
            _state = State.Attack;
            _nextFireTime = Time.time + _firstShotDelay;
        }
        else if (_state == State.Attack && dist > Mathf.Max(_reengageDistance, _attackDistance + 0.5f))
        {
            _state = State.Chase;
        }

        // siempre mirando al player (solo Y). La rotación final (con olas y retroceso) se arma en LateUpdate
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            float targetYaw = Quaternion.LookRotation(toPlayer.normalized, Vector3.up).eulerAngles.y;
            _facingYaw = Mathf.MoveTowardsAngle(_facingYaw, targetYaw, _rotationSpeed * Time.deltaTime);
        }

        // moverse hacia el player en Chase, frenar en Attack
        float targetSpeed = _state == State.Chase ? _moveSpeed : 0f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
        if (_currentSpeed > 0.001f && dist > 0.01f)
            transform.position += toPlayer.normalized * (_currentSpeed * Time.deltaTime);

        // knockback del disparo: retrocede frenando (velocidad que cae a 0 = recorre _knockbackDistance)
        if (_knockbackTimer >= 0f)
        {
            float dur = Mathf.Max(_knockbackTime, 0.01f);
            float k = 1f - Mathf.Clamp01(_knockbackTimer / dur);         // 0 -> 1
            float speed = 2f * _knockbackDistance / dur * (1f - k);
            transform.position += _knockbackDir * (speed * Time.deltaTime);
            _knockbackTimer += Time.deltaTime;
            if (_knockbackTimer >= dur)
            {
                // termina el knockback: suelta la trompa y arranca la hamaca
                _knockbackTimer = -1f;
                _pitchVel -= _swayKick;
            }
        }

        // carga: se inclina para atrás y al terminar dispara
        if (_windupTimer >= 0f)
        {
            _windupTimer -= Time.deltaTime;
            if (_windupTimer < 0f)
            {
                Fire();
                _nextFireTime = Time.time + _fireInterval;
            }
        }
        else if (_state == State.Attack && Time.time >= _nextFireTime && IsFacing(toPlayer))
        {
            _windupTimer = Mathf.Max(0f, _windupTime);
            if (_windupVfx != null) _windupVfx.Play(true);
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (_knockbackTimer >= 0f)
        {
            // durante el knockback la trompa va para abajo y se queda ahí (sin hamaca)
            _pitch = Mathf.Lerp(_pitch, _noseDownAngle, 1f - Mathf.Exp(-_noseDownSpeed * dt));
            _pitchVel = 0f;
        }
        else
        {
            // carga: para atrás. Después del knockback: vuelve a 0 con resorte poco amortiguado (sway)
            float pitchTarget = _windupTimer >= 0f ? -_windupLean : 0f;
            Spring(ref _pitch, ref _pitchVel, pitchTarget, dt);
        }
        Spring(ref _sink, ref _sinkVel, 0f, dt);

        // olas
        float t = Time.time + _wavePhase;
        float waveX = Mathf.Sin(t * _waveTiltFrequency) * _waveTilt;
        float waveZ = Mathf.Cos(t * _waveTiltFrequency * 0.8f) * _waveTilt;
        float bob = Mathf.Sin(t * _waveBobFrequency) * _waveBob;

        // rotación: mirar al player + inclinación relativa a hacia dónde mira + offset del modelo
        transform.rotation = Quaternion.Euler(0f, _facingYaw, 0f)
                           * Quaternion.Euler(_pitch + waveX, 0f, waveZ)
                           * Quaternion.Euler(0f, _modelYawOffset, 0f);

        // posición: se suma como offset (así no pelea con el movimiento ni con EnemyEmerge)
        Vector3 offset = Vector3.up * (bob + _sink);
        transform.position += offset - _appliedOffset;
        _appliedOffset = offset;
    }

    private void Spring(ref float x, ref float v, float target, float dt)
    {
        v += (_springStiffness * (target - x) - _springDamping * v) * dt;
        x += v * dt;
    }

    private bool IsFacing(Vector3 toPlayer)
    {
        Vector3 forward = Quaternion.Euler(0f, _facingYaw, 0f) * Vector3.forward;
        return Vector3.Angle(forward, toPlayer) <= _maxAimAngle;
    }

    private void Fire()
    {
        if (_projectilePrefab == null)
        {
            Debug.LogWarning("[HeavyRangedEnemy] No hay Projectile Prefab asignado.", this);
            return;
        }

        var bullet = GetFactory(_projectilePrefab).Create() as HeavyProjectile;
        if (bullet == null) return;

        // apunta a donde está el player AHORA; después no corrige
        Vector3 origin = _firePoint.position;
        Vector3 dir = _player.position - origin;
        dir.y = 0f;

        var trail = new HeavyProjectile.TrailSettings
        {
            apply = _overrideTrail,
            time = _trailTime,
            widthMultiplier = _trailWidth,
            color = _trailColor
        };
        if (_useArc)
        {
            Vector3 target = _player.position;
            target.y = transform.position.y - _appliedOffset.y; // cae a la altura del agua
            bullet.LaunchArc(origin, target, _projectileSpeed, _arcHeight, _projectileDamage, _projectileSize,
                             _landingRadius, _landingVfx != null ? _landingVfx : _impactVfx, _impactVfx, trail);
        }
        else
        {
            bullet.Launch(origin, dir, _projectileSpeed, _projectileDamage, _projectileSize, _projectileLifetime, _impactVfx, trail);
        }

        // knockback: retrocede con la trompa para abajo; al terminar arranca el sway
        _knockbackDir = Quaternion.Euler(0f, _facingYaw, 0f) * Vector3.back;
        _knockbackTimer = 0f;
        _sinkVel -= _recoilSinkImpulse;

        if (_muzzleVfx != null) _muzzleVfx.Play(true);
        if (_muzzleVfxPrefab != null && ParticlePool.Instance != null)
        {
            var vfx = ParticlePool.Instance.GetParticle(_muzzleVfxPrefab, _firePoint.position);
            if (vfx != null) vfx.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward, Vector3.up);
        }
    }

    private static BulletFactory GetFactory(HeavyProjectile prefab)
    {
        if (_factories.TryGetValue(prefab, out var f) && f != null) return f;
        var go = new GameObject($"HeavyProjectilePool_{prefab.name}");
        f = go.AddComponent<BulletFactory>(); // Awake arma el pool vacío; crece cuando hace falta
        f.prefab = prefab;
        _factories[prefab] = f;
        return f;
    }

    private static Gradient DefaultTrailGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f), new GradientColorKey(new Color(0.6f, 0.1f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackDistance);
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, _reengageDistance);
    }
}
