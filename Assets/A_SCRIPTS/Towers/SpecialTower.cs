using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Base de las torres que te cruzás en el mar y te atacan (TorreEspiral, TorreMortero).
// - Tiene su propia vida (IDamageable): tus balas le pegan si la torre tiene tag "Enemy" y layer EnemyLayer
//   (el menú Tools > Botes > Crear torres ya la arma así).
// - Cuando te acercás a menos de Activation Range empieza a atacar: se infla un poco (aviso) y dispara.
// - Al morir larga un montón de monedas como el cofre (CoinSpawner.SpawnFromChest).
// - Se resetea con el resto del mundo cuando morís (SceneResetter llama a ResetAllTowers).
public abstract class SpecialTower : MonoBehaviour, IDamageable
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
    [Tooltip("Opcional: Image (tipo Filled) que muestra la vida")]
    [SerializeField] private Image _healthFill;
    [Tooltip("Opcional: se prende recién cuando recibe el primer golpe")]
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

    protected virtual void Awake()
    {
        _hitFlash = GetComponentInChildren<HitFlash>();
        _colliders = GetComponentsInChildren<Collider>(true);
        if (_pulseTarget == null && _visualRoot != null) _pulseTarget = _visualRoot.transform;
        if (_pulseTarget != null) _pulseBaseScale = _pulseTarget.localScale;
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

        SetAlive(false);
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
        if (_healthBarRoot != null) _healthBarRoot.SetActive(false);
        UpdateBar();
        SetAlive(true);
        OnTowerReset();
    }

    private void SetAlive(bool alive)
    {
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
