using System.Collections.Generic;
using UnityEngine;

// Enemigo mortero: se acerca hasta cierta distancia, se frena y te tira balas en parábola.
// La bala sale a 45° (la altura del arco se calcula sola según la distancia) y cae
// justo donde marca el círculo de aviso. Reusa TowerShell y TowerWarningCircle de la torre.
// Hereda de Enemy (EnemyHealth + SO_EnemyData) y avisa OnDead para volver al pool.
public class MortarEnemy : Enemy
{
    public event System.Action<MortarEnemy> OnDead;

    private enum State { Chase, Attack }

    [Header("Movimiento")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _acceleration = 5f;
    [Tooltip("Grados por segundo")]
    [SerializeField] private float _rotationSpeed = 140f;
    [Tooltip("Corrección si el modelo no mira hacia su +Z (ej. 90 o -90)")]
    [SerializeField] private float _modelYawOffset = 0f;

    [Header("Distancias")]
    [Tooltip("Se frena y empieza a disparar a esta distancia")]
    [SerializeField] private float _attackDistance = 22f;
    [Tooltip("Si te alejás más que esto vuelve a perseguirte (mayor que Attack Distance)")]
    [SerializeField] private float _reengageDistance = 30f;

    [Header("Disparo")]
    [Tooltip("De dónde sale la bala. Vacío = 2 m arriba del enemigo")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _fireInterval = 2.5f;
    [SerializeField] private float _firstShotDelay = 1f;
    [Tooltip("Velocidad horizontal de la bala (m/s). Más alto = cae más rápido")]
    [SerializeField, Min(1f)] private float _horizontalSpeed = 22f;
    [Tooltip("Tiempo mínimo de vuelo, para que siempre se pueda esquivar aunque estés cerca")]
    [SerializeField, Min(0.2f)] private float _minFlightTime = 0.5f;
    [Tooltip("Cuánto se adelanta a hacia dónde vas. 0 = donde estás, 1 = donde vas a estar")]
    [SerializeField, Range(0f, 1.5f)] private float _lead = 0.7f;
    [Tooltip("Error de puntería en metros")]
    [SerializeField, Min(0f)] private float _aimSpread = 1f;
    [SerializeField] private ParticleSystem _muzzleVfx;

    [Header("Impacto")]
    [SerializeField, Min(0f)] private float _damage = 20f;
    [Tooltip("Radio de daño (y tamaño del círculo de aviso)")]
    [SerializeField, Min(0.2f)] private float _blastRadius = 2.5f;
    [Tooltip("Partícula al caer (tiene que estar en el ParticlePool). Opcional")]
    [SerializeField] private GameObject _landingVfx;
    [SerializeField] private float _splashSize = 1.4f;

    [Header("Visual de la bala")]
    [Tooltip("Prefab con TowerShell. Vacío = esfera simple")]
    [SerializeField] private TowerShell _shellPrefab;
    [SerializeField, Min(0.05f)] private float _shellSize = 0.8f;
    [Tooltip("Prefab con TowerWarningCircle. Vacío = aviso por defecto")]
    [SerializeField] private TowerWarningCircle _warningPrefab;
    [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.18f, 1f);
    [SerializeField] private Color _warningFillColor = new Color(1f, 0.42f, 0.2f, 0.55f);

    [Header("Spawn")]
    [Tooltip("Altura (Y) a la que queda al spawnear. Solo afecta a este enemigo, no al resto del spawner")]
    [SerializeField] private float _spawnHeight = 5.2f;

    [Header("Vida (si falta el SO)")]
    [SerializeField] private float _fallbackMaxHealth = 150f;

    // pools compartidos entre todos los MortarEnemy (las balas siguen volando aunque el que disparó muera)
    private static readonly List<TowerShell> _shells = new();
    private static readonly List<TowerWarningCircle> _warnings = new();

    private Transform _player;
    private Rigidbody _playerRb;
    private State _state;
    private float _currentSpeed;
    private float _nextFireTime;
    private float _facingYaw;
    private FakeWaveMovement _wave;

    protected override void Awake()
    {
        if (_enemyHealth == null) _enemyHealth = GetComponent<EnemyHealth>();
        if (baseData == null)
        {
            Debug.LogWarning("[MortarEnemy] Falta Base Data (SO_EnemyData): uso vida por defecto.", this);
            baseData = ScriptableObject.CreateInstance<SO_EnemyData>();
            baseData.maxHealth = _fallbackMaxHealth;
        }
        base.Awake();
        _wave = GetComponent<FakeWaveMovement>();
        if (_enemyHealth != null) _enemyHealth.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null) _enemyHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath() => OnDead?.Invoke(this);

    // Lo llama la EnemyFactory al sacarlo del pool
    public void SetPlayer(Transform player)
    {
        // la factory ya lo posicionÃ³ con el Y Offset general del spawner: acÃ¡ lo corrijo a su altura
        Vector3 p = transform.position;
        p.y = _spawnHeight;
        transform.position = p;

        SetPlayerRefs(player);
        if (_enemyHealth != null) _enemyHealth.Revive();
        ResetState();
    }

    private void SetPlayerRefs(Transform player)
    {
        _player = player;
        _playerRb = player != null ? player.GetComponentInParent<Rigidbody>() : null;
    }

    private void OnEnable() => ResetState();

    private void ResetState()
    {
        _state = State.Chase;
        _currentSpeed = 0f;
        _nextFireTime = 0f;
        _facingYaw = transform.eulerAngles.y - _modelYawOffset;
    }

    private void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            SetPlayerRefs(p.transform);
        }

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // estado con histéresis para que no titubee en el borde
        if (_state == State.Chase && dist <= _attackDistance)
        {
            _state = State.Attack;
            _nextFireTime = Time.time + _firstShotDelay;
        }
        else if (_state == State.Attack && dist > Mathf.Max(_reengageDistance, _attackDistance + 0.5f))
        {
            _state = State.Chase;
        }

        // mirar al player (solo Y)
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            float targetYaw = Quaternion.LookRotation(toPlayer.normalized, Vector3.up).eulerAngles.y;
            _facingYaw = Mathf.MoveTowardsAngle(_facingYaw, targetYaw, _rotationSpeed * Time.deltaTime);
            float yaw = _facingYaw + _modelYawOffset;
            if (_wave != null) _wave.SetYaw(yaw);
            else transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // perseguir en Chase, frenar en Attack
        float targetSpeed = _state == State.Chase ? _moveSpeed : 0f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
        if (_currentSpeed > 0.001f && dist > 0.01f)
            transform.position += toPlayer.normalized * (_currentSpeed * Time.deltaTime);

        if (_state == State.Attack && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireInterval;
            Fire();
        }
    }

    private void Fire()
    {
        Vector3 from = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 2f;

        // primero estimo el tiempo de vuelo con la distancia actual para adelantar la puntería
        Vector3 target = _player.position;
        float flightTime = GetFlightTime(from, target);
        if (_playerRb != null && _lead > 0f)
        {
            Vector3 v = _playerRb.linearVelocity;
            v.y = 0f;
            target += v * (flightTime * _lead);
        }
        if (_aimSpread > 0f)
        {
            Vector2 r = Random.insideUnitCircle * _aimSpread;
            target += new Vector3(r.x, 0f, r.y);
        }
        target.y = _player.position.y; // altura del agua

        // 45°: con la parábola de TowerShell la pendiente de salida es 4h/D, así que h = D/4
        Vector3 flat = target - from;
        flat.y = 0f;
        float horizontalDist = flat.magnitude;
        float arcHeight = horizontalDist * 0.25f;
        flightTime = GetFlightTime(from, target);

        GetShell().Launch(from, target, flightTime, arcHeight, _damage, _blastRadius, _shellSize,
                          _player, GetWarning(), _landingVfx, _splashSize);

        if (_muzzleVfx != null) _muzzleVfx.Play(true);
    }

    private float GetFlightTime(Vector3 from, Vector3 to)
    {
        Vector3 flat = to - from;
        flat.y = 0f;
        return Mathf.Max(_minFlightTime, flat.magnitude / _horizontalSpeed);
    }

    // ---------- pools ----------

    private TowerShell GetShell()
    {
        _shells.RemoveAll(s => s == null);
        foreach (var s in _shells) if (s.IsFree) return s;
        TowerShell shell = _shellPrefab != null ? Instantiate(_shellPrefab) : CreateFallbackShell();
        shell.gameObject.SetActive(false);
        _shells.Add(shell);
        return shell;
    }

    private TowerWarningCircle GetWarning()
    {
        _warnings.RemoveAll(w => w == null);
        foreach (var w in _warnings) if (w.IsFree) return w;
        TowerWarningCircle warn;
        if (_warningPrefab != null) warn = Instantiate(_warningPrefab);
        else
        {
            var root = new GameObject("MortarEnemyWarning");
            warn = root.AddComponent<TowerWarningCircle>();
            warn.BuildDefault(_warningColor, _warningFillColor);
        }
        warn.gameObject.SetActive(false);
        _warnings.Add(warn);
        return warn;
    }

    private TowerShell CreateFallbackShell()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "MortarEnemyShell";
        var rend = go.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.AddComponent<TowerShell>();
    }

    // Para resetear la escena: apaga las balas y avisos que queden volando
    public static void DespawnAllShells()
    {
        foreach (var s in _shells) if (s != null && !s.IsFree) s.Despawn();
        foreach (var w in _warnings) if (w != null) w.Hide();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _attackDistance);
        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _reengageDistance);
    }
}