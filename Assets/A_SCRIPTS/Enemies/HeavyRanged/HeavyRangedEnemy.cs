using System.Collections.Generic;
using UnityEngine;

// Enemigo de largo alcance: se acerca, se frena a cierta distancia y dispara
// un proyectil enorme y lento (HeavyProjectile) cada X segundos.
//  - Siempre mira al player (rotación suave solo en Y), moviéndose o quieto.
//  - Si el player se aleja más de Re-engage Distance, vuelve a acercarse.
//  - Integrado como el resto: hereda de Enemy (EnemyHealth + SO_EnemyData), avisa OnDead
//    para volver al pool de la EnemyFactory, y funciona con EnemyEmerge y las fases.
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
    [SerializeField] private ParticleSystem _muzzleVfx;

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

        // siempre mirando al player (solo Y)
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer.normalized, Vector3.up) * Quaternion.Euler(0f, _modelYawOffset, 0f);
            Quaternion current = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            transform.rotation = Quaternion.RotateTowards(current, target, _rotationSpeed * Time.deltaTime);
        }

        // moverse hacia el player en Chase, frenar en Attack
        float targetSpeed = _state == State.Chase ? _moveSpeed : 0f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
        if (_currentSpeed > 0.001f && dist > 0.01f)
            transform.position += toPlayer.normalized * (_currentSpeed * Time.deltaTime);

        if (_state == State.Attack && Time.time >= _nextFireTime && IsFacing(toPlayer))
        {
            Fire();
            _nextFireTime = Time.time + _fireInterval;
        }
    }

    private bool IsFacing(Vector3 toPlayer)
    {
        Vector3 forward = Quaternion.Euler(0f, transform.eulerAngles.y - _modelYawOffset, 0f) * Vector3.forward;
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
        bullet.Launch(origin, dir, _projectileSpeed, _projectileDamage, _projectileSize, _projectileLifetime, _impactVfx, trail);

        if (_muzzleVfx != null) _muzzleVfx.Play(true);
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
