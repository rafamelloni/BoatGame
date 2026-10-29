using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Base de las torres que te cruzás en el mar y te atacan (por ahora solo TorreMortero).
// Si cambiás el modelo: click en los tres puntitos del componente > "Ordenar referencias".
// - Tiene su propia vida (IDamageable): tus balas le pegan si la torre tiene tag "Enemy" y layer EnemyLayer
//   (el menú Tools > Botes > Crear torres ya la arma así).
// - Usa la misma barra de vida que los enemigos (EnemyHealthBar en el root) y el mismo HitFlash.
// - Cuando te acercás a menos de Activation Range empieza a atacar: se infla un poco (aviso) y dispara.
// - Al morir larga un montón de monedas como el cofre (CoinSpawner.SpawnFromChest),
//   tiembla y se hunde en el agua mientras salen partículas.
// - Se resetea con el resto del mundo cuando morís (SceneResetter llama a ResetAllTowers).
public abstract class SpecialTower : MonoBehaviour, IDamageable, IHealthBarSource
{
    private static readonly List<SpecialTower> _all = new List<SpecialTower>();

    public static void ResetAllTowers()
    {
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            if (_all[i] == null) { _all.RemoveAt(i); continue; }
            _all[i].ResetTower();
        }
    }

    [Header("Vida")]
    [SerializeField, Min(1f)] private float _maxHealth = 400f;
    [Tooltip("Opcional (viejo): Image (tipo Filled) que muestra la vida. Lo normal es usar EnemyHealthBar en el root")]
    [SerializeField] private Image _healthFill;
    [Tooltip("Opcional (viejo): se prende recién cuando recibe el primer golpe")]
    [SerializeField] private GameObject _healthBarRoot;

    [Header("Detección")]
    [Tooltip("Distancia a la que te detecta y empieza a atacar (metros)")]
    [SerializeField, Min(1f)] private float _activationRange = 28f;
    [Tooltip("Segundos desde que entrás en rango hasta el primer ataque")]
    [SerializeField, Min(0f)] private float _firstAttackDelay = 1f;
    [Tooltip("Segundos entre un ataque y el siguiente")]
    [SerializeField, Min(0.1f)] private float _attackCooldown = 3.5f;

    [Header("Cabeza (opcional)")]
    [Tooltip("Parte que gira mirando al jugador")]
    [SerializeField] private Transform _head;
    [SerializeField] private float _headTurnSpeed = 4f;
    [Tooltip("Si el modelo no mira hacia +Z, corregilo acá (ej. 0,90,0)")]
    [SerializeField] private Vector3 _headRotationOffset;

    [Header("Aviso antes de atacar")]
    [Tooltip("Segundos que se infla antes de atacar (para que el jugador lo vea venir)")]
    [SerializeField, Min(0f)] private float _windup = 0.6f;
    [Tooltip("Lo que se infla. Vacío = Visual Root")]
    [SerializeField] private Transform _pulseTarget;
    [SerializeField] private float _pulseScale = 1.15f;

    [Header("Muerte y monedas (como el cofre)")]
    [Tooltip("Vacío = lo busca en la escena")]
    [SerializeField] private CoinSpawner _coinSpawner;
    [SerializeField, Min(0)] private int _minCoins = 15;
    [SerializeField, Min(0)] private int _maxCoins = 25;
    [SerializeField] private float _minHorizontalSpeed = 2.5f;
    [SerializeField] private float _maxHorizontalSpeed = 6f;
    [SerializeField] private float _minVerticalSpeed = 6f;
    [SerializeField] private float _maxVerticalSpeed = 10f;
    [SerializeField] private float _launchPower = 1.3f;
    [Tooltip("Partícula de explosión (tiene que estar en la lista del ParticlePool)")]
    [SerializeField] private GameObject _deathVfx;
    [Tooltip("Tamaño del anillo de espuma en el agua al morir")]
    [SerializeField] private float _deathSplashSize = 2.5f;

    [Header("Muerte: se hunde")]
    [Tooltip("Cuánto tarda en hundirse del todo")]
    [SerializeField, Min(0.1f)] private float _sinkDuration = 2.2f;
    [Tooltip("Segundos que solo tiembla antes de empezar a bajar")]
    [SerializeField, Min(0f)] private float _shakeOnlyTime = 0.35f;
    [Tooltip("Cuántos metros baja (que quede tapada por el agua)")]
    [SerializeField, Min(0f)] private float _sinkDepth = 8f;
    [Tooltip("Temblor de costado, en metros")]
    [SerializeField, Min(0f)] private float _shakeAmount = 0.25f;
    [Tooltip("Bamboleo en grados mientras tiembla")]
    [SerializeField, Min(0f)] private float _wobbleAngle = 6f;
    [Tooltip("Cuánto se inclina mientras se hunde, en grados")]
    [SerializeField] private float _sinkTilt = 12f;
    [Tooltip("Partícula que sale alrededor mientras se hunde (tiene que estar en el ParticlePool). Opcional.")]
    [SerializeField] private GameObject _sinkVfx;
    [Tooltip("Cada cuánto sale una partícula / anillo de espuma mientras se hunde")]
    [SerializeField, Min(0.03f)] private float _sinkFxInterval = 0.18f;
    [SerializeField, Min(0f)] private float _sinkFxRadius = 1.6f;
    [SerializeField, Min(0f)] private float _sinkSplashSize = 1.4f;

    [Header("Visual")]
    [Tooltip("Modelo de la torre: se apaga al morir")]
    [SerializeField] private GameObject _visualRoot;
    [Tooltip("Opcional: ruinas que se prenden al morir")]
    [SerializeField] private GameObject _destroyedVisual;

    protected Transform Player { get; private set; }
    protected Rigidbody PlayerRb { get; private set; }
    protected bool IsDead => _dead;

    private float _health;
    private bool _dead;
    private bool _attacking;
    private bool _wasInRange;
    private float _nextAttack;
    private HitFlash _hitFlash;
    private Collider[] _colliders;
    private Vector3 _pulseBaseScale;
    private EnemyHealthBar _bar;
    private Vector3 _visualBasePos;
    private Quaternion _visualBaseRot;

    // IHealthBarSource (para EnemyHealthBar)
    public bool IsInitialized => true;
    public float GetMaxHealt() => _maxHealth;
    public float GetHealthNormalized() => Mathf.Clamp01(_health / _maxHealth);

    protected virtual void Awake()
    {
        EnsureEmissionForFlash();
        _hitFlash = GetComponentInChildren<HitFlash>();
        _bar = GetComponentInChildren<EnemyHealthBar>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        if (_pulseTarget == null && _visualRoot != null) _pulseTarget = _visualRoot.transform;
        if (_pulseTarget != null) _pulseBaseScale = _pulseTarget.localScale;
        if (_visualRoot != null)
        {
            _visualBasePos = _visualRoot.transform.localPosition;
            _visualBaseRot = _visualRoot.transform.localRotation;
        }
        _health = _maxHealth;
        if (_healthBarRoot != null) _healthBarRoot.SetActive(false);
        if (_destroyedVisual != null) _destroyedVisual.SetActive(false);
        UpdateBar();
    }

    protected virtual void OnEnable()
    {
        if (!_all.Contains(this)) _all.Add(this);
    }

    protected virtual void OnDestroy()
    {
        _all.Remove(this);
    }

    private void Update()
    {
        if (_dead) return;
        if (!FindPlayer()) return;

        Vector3 toPlayer = Player.position - transform.position;
        toPlayer.y = 0f;
        bool inRange = toPlayer.magnitude <= _activationRange;

        if (inRange && !_wasInRange)
            _nextAttack = Mathf.Max(_nextAttack, Time.time + _firstAttackDelay);
        _wasInRange = inRange;
        if (!inRange) return;

        if (_head != null && toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(toPlayer.normalized, Vector3.up) * Quaternion.Euler(_headRotationOffset);
            _head.rotation = Quaternion.Slerp(_head.rotation, target, 1f - Mathf.Exp(-_headTurnSpeed * Time.deltaTime));
        }

        if (!_attacking && Time.time >= _nextAttack)
            StartCoroutine(AttackRoutine());
    }

    private bool FindPlayer()
    {
        if (Player != null) return true;
        var go = GameObject.FindWithTag("Player");
        if (go == null) return false;
        Player = go.transform;
        PlayerRb = go.GetComponentInParent<Rigidbody>();
        return true;
    }

    private IEnumerator AttackRoutine()
    {
        _attacking = true;

        // aviso: se infla y vuelve
        if (_windup > 0f && _pulseTarget != null)
        {
            float t = 0f;
            while (t < _windup)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / _windup) * Mathf.PI);
                _pulseTarget.localScale = _pulseBaseScale * Mathf.Lerp(1f, _pulseScale, k);
                yield return null;
            }
            _pulseTarget.localScale = _pulseBaseScale;
        }
        else if (_windup > 0f)
        {
            yield return new WaitForSeconds(_windup);
        }

        yield return StartCoroutine(Attack());

        _nextAttack = Time.time + _attackCooldown;
        _attacking = false;
    }

    // El ataque de cada torre
    protected abstract IEnumerator Attack();

    // Para que cada torre limpie sus proyectiles al resetear
    protected virtual void OnTowerReset() { }

    public void TakeDamage(float damage)
    {
        if (_dead) return;
        _health -= damage;
        if (_healthBarRoot != null && !_healthBarRoot.activeSelf) _healthBarRoot.SetActive(true);
        UpdateBar();
        _hitFlash?.Flash();
        if (_health <= 0f) Die();
    }

    private void UpdateBar()
    {
        if (_healthFill != null) _healthFill.fillAmount = Mathf.Clamp01(_health / _maxHealth);
    }

    private void Die()
    {
        _dead = true;
        StopAllCoroutines();
        _attacking = false;
        if (_pulseTarget != null) _pulseTarget.localScale = _pulseBaseScale;

        Vector3 pos = transform.position;

        if (_coinSpawner == null) _coinSpawner = FindFirstObjectByType<CoinSpawner>();
        if (_coinSpawner != null)
        {
            int count = Random.Range(_minCoins, _maxCoins + 1);
            _coinSpawner.SpawnFromChest(pos, count, _minHorizontalSpeed, _maxHorizontalSpeed,
                                        _minVerticalSpeed, _maxVerticalSpeed, _launchPower);
        }
        else Debug.LogWarning($"[SpecialTower] {name}: no hay CoinSpawner en la escena, no salen monedas.", this);

        if (_deathVfx != null && ParticlePool.Instance != null)
            ParticlePool.Instance.GetParticle(_deathVfx, pos);
        WaterImpacts.Spawn(pos, _deathSplashSize);

        // ya no se le puede pegar ni se ve la barra; el modelo se hunde y recién ahí se apaga
        if (_bar != null) _bar.enabled = false;
        if (_healthBarRoot != null) _healthBarRoot.SetActive(false);
        foreach (var c in _colliders) if (c != null) c.enabled = false;
        StartCoroutine(SinkRoutine(pos));
    }

    // El HitFlash brilla con la emisión del material. Si el modelo nuevo tiene materiales sin emisión
    // (por ejemplo los que vienen dentro del FBX), se la prende en copias en runtime; no toca los assets.
    private void EnsureEmissionForFlash()
    {
        var root = _visualRoot != null ? _visualRoot.transform : transform;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            bool needs = false;
            foreach (var m in r.sharedMaterials)
                if (m != null && m.HasProperty("_EmissionColor") && !m.IsKeywordEnabled("_EMISSION")) needs = true;
            if (!needs) continue;
            foreach (var m in r.materials)
            {
                if (m == null || !m.HasProperty("_EmissionColor")) continue;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private IEnumerator SinkRoutine(Vector3 pos)
    {
        Transform v = _visualRoot != null ? _visualRoot.transform : null;
        if (v == null) { SetAlive(false); yield break; }

        // altura del agua: la del barco (misma idea que el mortero)
        float waterY = Player != null ? Player.position.y : pos.y;
        float t = 0f, nextFx = 0f;
        float sinkTime = Mathf.Max(0.05f, _sinkDuration - _shakeOnlyTime);

        while (t < _sinkDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _sinkDuration);
            float sink = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - _shakeOnlyTime) / sinkTime));
            float fade = 1f - k * 0.6f; // tiembla menos a medida que se hunde

            Vector3 jitter = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * (_shakeAmount * fade);
            v.localPosition = _visualBasePos + jitter + Vector3.down * (_sinkDepth * sink);
            v.localRotation = _visualBaseRot * Quaternion.Euler(
                Mathf.Sin(t * 25f) * _wobbleAngle * fade,
                0f,
                Mathf.Cos(t * 21f) * _wobbleAngle * fade + _sinkTilt * sink);

            // partículas y espuma alrededor de la base
            if (t >= nextFx)
            {
                nextFx = t + _sinkFxInterval;
                Vector2 r = Random.insideUnitCircle.normalized * _sinkFxRadius;
                Vector3 p = new Vector3(pos.x + r.x, waterY, pos.z + r.y);
                if (_sinkVfx != null && ParticlePool.Instance != null) ParticlePool.Instance.GetParticle(_sinkVfx, p);
                WaterImpacts.Spawn(p, _sinkSplashSize * Random.Range(0.8f, 1.2f));
            }
            yield return null;
        }

        // último golpe de agua al terminar de hundirse
        Vector3 end = new Vector3(pos.x, waterY, pos.z);
        if (_sinkVfx != null && ParticlePool.Instance != null) ParticlePool.Instance.GetParticle(_sinkVfx, end);
        WaterImpacts.Spawn(end, _deathSplashSize);

        SetAlive(false);
        v.localPosition = _visualBasePos;
        v.localRotation = _visualBaseRot;
    }

    public void ResetTower()
    {
        StopAllCoroutines();
        _dead = false;
        _attacking = false;
        _wasInRange = false;
        _nextAttack = 0f;
        _health = _maxHealth;
        if (_pulseTarget != null) _pulseTarget.localScale = _pulseBaseScale;
        if (_visualRoot != null)
        {
            _visualRoot.transform.localPosition = _visualBasePos;
            _visualRoot.transform.localRotation = _visualBaseRot;
        }
        if (_healthBarRoot != null) _healthBarRoot.SetActive(false);
        UpdateBar();
        SetAlive(true);
        OnTowerReset();
    }

    private void SetAlive(bool alive)
    {
        // la barra se apaga al morir y vuelve llena al resetear (igual que un enemigo que vuelve del pool)
        if (_bar != null) _bar.enabled = alive;
        if (_visualRoot != null) _visualRoot.SetActive(alive);
        if (_destroyedVisual != null) _destroyedVisual.SetActive(!alive);
        if (!alive && _healthBarRoot != null) _healthBarRoot.SetActive(false);
        foreach (var c in _colliders)
            if (c != null) c.enabled = alive;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _activationRange);
    }
}
