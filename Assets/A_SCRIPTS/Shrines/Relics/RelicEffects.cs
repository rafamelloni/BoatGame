using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ancla, Timon y Ron quedan solo para no romper los assets viejos: ya no hacen nada ni salen en los templos.
public enum RelicType
{
    None, Ancla, Timon, Ron,
    // Common
    PolvoraFina, RelojArena, BalasRepuesto, MechaLarga, Pluma, Salvavidas, Catalejo,
    // Rare
    Antorcha, Garfio, UltimoAliento, Pleno, Mina,
    // Super Rare
    Metralla, Holandes, BalaFantasma, CargadorSinFondo
}

// Banderas que leen otros scripts (CannonBullet) sin depender de RelicEffects.
public static class RelicRuntime
{
    // Cuántos enemigos atraviesa cada bala antes de comportarse normal (Bala fantasma)
    public static int PierceCount;
    // true mientras explota una balita de Metralla o una mina: esas kills no generan más Metralla
    public static bool SuppressProcs;
}

// Lo que hacen las reliquias (solo duran la run).
// Todo lo que suma lo anota para poder deshacerlo exacto al reiniciar la run.
// Va en el mismo objeto que RelicInventory (si no está, RelicInventory lo agrega solo).
public class RelicEffects : MonoBehaviour
{
    public static RelicEffects Instance { get; private set; }

    public static bool IsLegacy(RelicType t) =>
        t == RelicType.None || t == RelicType.Ancla || t == RelicType.Timon || t == RelicType.Ron;

    // ------------------------------------------------------------ valores

    [Header("Common (se apilan: cada copia suma de nuevo)")]
    [SerializeField, Range(0f, 1f)] private float _damagePct = 0.10f;           // Pólvora fina
    [SerializeField, Range(0f, 0.5f)] private float _reloadPct = 0.10f;         // Reloj de arena
    [SerializeField] private int _extraAmmo = 1;                                 // Balas de repuesto
    [SerializeField, Range(0f, 1f)] private float _explosionPct = 0.10f;        // Mecha larga
    [SerializeField, Range(0f, 1f)] private float _bulletSpeedPct = 0.10f;      // Pluma de gaviota
    [SerializeField, Range(0f, 1f)] private float _maxHealthPct = 0.10f;        // Salvavidas
    [SerializeField, Range(0f, 1f)] private float _critChance = 0.05f;          // Catalejo

    [Header("Antorcha — después de recargar")]
    [SerializeField] private float _freshDamageMult = 1.5f;
    [SerializeField] private float _freshDuration = 1f;

    [Header("Garfio — cada kill acelera la próxima recarga")]
    [SerializeField, Range(0f, 0.3f)] private float _killReloadBonus = 0.08f;
    [SerializeField, Range(0f, 0.9f)] private float _killReloadMax = 0.5f;

    [Header("Último aliento")]
    [SerializeField, Range(0f, 1f)] private float _lastBreathThreshold = 0.3f;
    [Tooltip("Multiplica el tiempo de recarga (0.5 = el doble de rápido)")]
    [SerializeField, Range(0.1f, 1f)] private float _lastBreathReloadMul = 0.5f;

    [Header("Dados cargados (Pleno)")]
    [SerializeField] private int _plenoKills = 3;

    [Header("Mina de pólvora")]
    [SerializeField] private float _mineLifetime = 5f;
    [SerializeField] private float _mineTriggerRadius = 2.5f;
    [SerializeField] private float _mineExplosionRadius = 3.5f;
    [Tooltip("Daño de la mina = daño del cañón x esto")]
    [SerializeField] private float _mineDamageMul = 0.8f;
    [SerializeField] private int _maxMines = 8;

    [Header("Metralla")]
    [SerializeField] private int _shrapnelCount = 3;
    [Tooltip("Daño de cada balita = daño del cañón x esto")]
    [SerializeField] private float _shrapnelDamageMul = 0.25f;
    [SerializeField] private float _shrapnelDistance = 5f;
    [SerializeField] private float _shrapnelFlightTime = 0.5f;
    [Tooltip("Tope de balitas por segundo, para que una cadena de kills no explote todo")]
    [SerializeField] private float _maxShrapnelPerSecond = 12f;

    [Header("Linterna del Holandés")]
    [SerializeField, Range(0.1f, 1f)] private float _reviveHealthPct = 0.5f;

    [Header("Bala fantasma")]
    [SerializeField] private int _pierceCount = 1;

    [Header("Barril sin fondo")]
    [SerializeField] private int _bottomlessKills = 15;
    [SerializeField] private float _bottomlessDuration = 5f;

    // ------------------------------------------------------------ estado

    private RT_PlayerStats _stats;
    private RT_PlayerStats _critStats;
    private RT_PlayerHealth _health;
    private CannonStrategy _cannon;
    private RelicInventory _inventory;

    private readonly List<System.Action> _undo = new List<System.Action>();
    private readonly HashSet<RelicType> _active = new HashSet<RelicType>();
    private readonly List<PowderMine> _mines = new List<PowderMine>();

    private bool _cannonHooked, _healthHooked;
    private int _garfioKills;
    private int _bottomlessCounter;
    private bool _reviveUsed;
    private float _shrapnelBudget;
    private Coroutine _freshRoutine, _bottomlessRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        _inventory = GetComponent<RelicInventory>();
        if (_inventory == null) _inventory = FindFirstObjectByType<RelicInventory>();
        if (_inventory != null)
        {
            _inventory.OnRelicAdded += OnRelicAdded;
            _inventory.OnReset += RevertAll;
        }
        EnemyHealth.OnAnyEnemyDamagedInfo += OnEnemyDamaged;
        CannonBullet.OnBulletExploded += OnBulletExploded;
        CannonBullet.OnBulletHitWater += OnBulletHitWater;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_inventory != null)
        {
            _inventory.OnRelicAdded -= OnRelicAdded;
            _inventory.OnReset -= RevertAll;
        }
        EnemyHealth.OnAnyEnemyDamagedInfo -= OnEnemyDamaged;
        CannonBullet.OnBulletExploded -= OnBulletExploded;
        CannonBullet.OnBulletHitWater -= OnBulletHitWater;
        RelicRuntime.PierceCount = 0;
    }

    private void Update()
    {
        _shrapnelBudget = Mathf.Min(_maxShrapnelPerSecond, _shrapnelBudget + _maxShrapnelPerSecond * Time.deltaTime);
    }

    // ------------------------------------------------------------ aplicar

    private void OnRelicAdded(SO_RelicData relic, int count)
    {
        if (relic == null) return;
        FindRefs();

        switch (relic.effectType)
        {
            // Common
            case RelicType.PolvoraFina: AddCannonDamagePct(_damagePct); break;
            case RelicType.RelojArena: AddReloadPct(_reloadPct); break;
            case RelicType.BalasRepuesto: AddAmmo(_extraAmmo); break;
            case RelicType.MechaLarga: AddExplosionPct(_explosionPct); break;
            case RelicType.Pluma: AddBulletSpeedPct(_bulletSpeedPct); break;
            case RelicType.Salvavidas: AddMaxHealthPct(_maxHealthPct); break;
            case RelicType.Catalejo: AddCrit(_critChance); break;

            // Rare / Super Rare: se prenden y los manejan los eventos de abajo
            case RelicType.Antorcha:
            case RelicType.Garfio:
            case RelicType.UltimoAliento:
                HookCannon();
                Enable(relic.effectType);
                break;
            case RelicType.Holandes:
                HookHealth();
                Enable(relic.effectType);
                break;
            case RelicType.BalaFantasma:
                RelicRuntime.PierceCount += _pierceCount;
                int p = _pierceCount;
                _undo.Add(() => RelicRuntime.PierceCount = Mathf.Max(0, RelicRuntime.PierceCount - p));
                Enable(relic.effectType);
                break;
            case RelicType.Pleno:
            case RelicType.Mina:
            case RelicType.Metralla:
            case RelicType.CargadorSinFondo:
                Enable(relic.effectType);
                break;
        }
    }

    private void Enable(RelicType t)
    {
        if (_active.Add(t)) _undo.Add(() => _active.Remove(t));
    }

    private bool Has(RelicType t) => _active.Contains(t);

    // --- stats

    private void AddCannonDamagePct(float pct)
    {
        if (_cannon == null) return;
        var data = _cannon._rtData;
        float d = data.damage * pct;
        data.damage += d;
        _undo.Add(() => data.damage -= d);
    }

    private void AddReloadPct(float pct)
    {
        if (_cannon == null) return;
        var data = _cannon._rtData;
        float c = data.cooldown * pct;
        data.cooldown -= c;
        _undo.Add(() => data.cooldown += c);
    }

    private void AddAmmo(int amount)
    {
        if (_cannon == null) return;
        var cannon = _cannon;
        cannon.AddAmmo(amount);
        _undo.Add(() => cannon.AddAmmo(-amount));
    }

    private void AddExplosionPct(float pct)
    {
        if (_cannon == null) return;
        var data = _cannon._rtData;
        float r = data.explosionRadius * pct;
        data.explosionRadius += r;
        _undo.Add(() => data.explosionRadius -= r);
    }

    private void AddBulletSpeedPct(float pct)
    {
        if (_cannon == null) return;
        var data = _cannon._rtData;
        float m = 1f + pct;
        data.launchSpeed *= m;
        data.bulletSpeed *= m;
        data.verticalArc /= m; // arco más plano = la bala llega antes
        _undo.Add(() => { data.launchSpeed /= m; data.bulletSpeed /= m; data.verticalArc *= m; });
    }

    private void AddMaxHealthPct(float pct)
    {
        if (_stats == null) return;
        float add = _stats.maxHealth * pct;
        _stats.maxHealth += add;
        _stats.currentHealth = Mathf.Min(_stats.currentHealth + add, _stats.maxHealth); // te cura lo que sumó
        if (_health != null) _health.RefreshUI();
        var st = _stats;
        _undo.Add(() => { st.maxHealth -= add; st.currentHealth = Mathf.Min(st.currentHealth, st.maxHealth); });
    }

    private void AddCrit(float add)
    {
        if (_critStats == null) return;
        var st = _critStats;
        st.critChance += add;
        _undo.Add(() => st.critChance -= add);
    }

    // --- enganches

    private void HookCannon()
    {
        if (_cannonHooked || _cannon == null) return;
        _cannonHooked = true;
        var cannon = _cannon;
        cannon.ReloadTimeMultiplier = ConsumeReloadMultiplier;
        cannon.OnReloaded += HandleReloaded;
        _undo.Add(() =>
        {
            cannon.ReloadTimeMultiplier = null;
            cannon.OnReloaded -= HandleReloaded;
            _cannonHooked = false;
        });
    }

    private void HookHealth()
    {
        if (_healthHooked || _health == null) return;
        _healthHooked = true;
        var health = _health;
        health.IncomingDamageModifier += ModifyIncomingDamage;
        _undo.Add(() =>
        {
            health.IncomingDamageModifier -= ModifyIncomingDamage;
            _healthHooked = false;
        });
    }

    // Lo llama el cañón UNA vez al empezar cada recarga
    private float ConsumeReloadMultiplier()
    {
        float m = 1f;
        if (Has(RelicType.Garfio))
        {
            m *= 1f - Mathf.Min(_killReloadMax, _garfioKills * _killReloadBonus);
            _garfioKills = 0; // se resetea al recargar
        }
        if (Has(RelicType.UltimoAliento) && _stats != null && _stats.maxHealth > 0f
            && _stats.currentHealth / _stats.maxHealth < _lastBreathThreshold)
            m *= _lastBreathReloadMul;
        return m;
    }

    private void HandleReloaded()
    {
        if (Has(RelicType.Antorcha) && _cannon != null)
        {
            if (_freshRoutine != null) StopCoroutine(_freshRoutine);
            _freshRoutine = StartCoroutine(FreshClip(_cannon._rtData));
        }
    }

    private IEnumerator FreshClip(RT_CannonData data)
    {
        data.fireDamageMultiplier = _freshDamageMult;
        yield return new WaitForSeconds(_freshDuration);
        data.fireDamageMultiplier = 1f;
        _freshRoutine = null;
    }

    // Linterna del Holandés: el golpe que te mataría te deja con vida (una vez por run)
    private float ModifyIncomingDamage(float amount)
    {
        if (!Has(RelicType.Holandes) || _reviveUsed || _stats == null) return amount;
        if (amount < _stats.currentHealth) return amount;

        _reviveUsed = true;
        float target = _stats.maxHealth * _reviveHealthPct;
        if (_health != null) _health.Heal(Mathf.Max(0f, target - _stats.currentHealth));
        else _stats.currentHealth = target;
        return 0f;
    }

    // ------------------------------------------------------------ eventos del juego

    private void OnEnemyDamaged(DamageInfo info)
    {
        if (!info.killed || _active.Count == 0) return;
        FindRefs();

        if (Has(RelicType.Garfio)) _garfioKills++;

        if (Has(RelicType.CargadorSinFondo) && _bottomlessRoutine == null)
        {
            _bottomlessCounter++;
            if (_bottomlessCounter >= _bottomlessKills)
            {
                _bottomlessCounter = 0;
                _bottomlessRoutine = StartCoroutine(Bottomless());
            }
        }

        // solo las kills "de verdad" (bala, fuego...) largan balitas; las de balitas o minas no encadenan
        if (Has(RelicType.Metralla) && !RelicRuntime.SuppressProcs) SpawnShrapnel(info.position);
    }

    private IEnumerator Bottomless()
    {
        if (_cannon != null) _cannon.InfiniteAmmo = true;
        yield return new WaitForSeconds(_bottomlessDuration);
        if (_cannon != null) _cannon.InfiniteAmmo = false;
        _bottomlessRoutine = null;
    }

    private void SpawnShrapnel(Vector3 pos)
    {
        if (_cannon == null || _shrapnelCount <= 0) return;
        float g = Physics.gravity.y;
        float t = Mathf.Max(0.1f, _shrapnelFlightTime);
        float offset = Random.Range(0f, 360f);
        Vector3 start = pos + Vector3.up * 1.2f;

        for (int i = 0; i < _shrapnelCount; i++)
        {
            if (_shrapnelBudget < 1f) return;
            _shrapnelBudget -= 1f;

            float a = (offset + i * 360f / _shrapnelCount) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 target = pos + dir * _shrapnelDistance;
            // velocidad exacta para caer en target en t segundos
            Vector3 v = (target - start - 0.5f * new Vector3(0f, g, 0f) * t * t) / t;
            _cannon.FireExtraBullet(start, v, _shrapnelDamageMul);
        }
    }

    private void OnBulletExploded(Vector3 center, int kills)
    {
        if (Has(RelicType.Pleno) && kills >= _plenoKills && _cannon != null)
            _cannon.RefundShot();
    }

    private void OnBulletHitWater(Vector3 point, LayerMask damageLayers, GameObject explosionVfx)
    {
        if (!Has(RelicType.Mina) || _cannon == null) return;

        _mines.RemoveAll(m => m == null);
        if (_mines.Count >= _maxMines && _mines.Count > 0)
        {
            Destroy(_mines[0].gameObject);
            _mines.RemoveAt(0);
        }

        float damage = _cannon._rtData.damage * _mineDamageMul;
        _mines.Add(PowderMine.Spawn(point, _mineLifetime, _mineTriggerRadius, _mineExplosionRadius, damage, damageLayers, explosionVfx));
    }

    // ------------------------------------------------------------ reset

    private void RevertAll()
    {
        for (int i = _undo.Count - 1; i >= 0; i--)
        {
            try { _undo[i](); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
        _undo.Clear();
        _active.Clear();

        StopAllCoroutines();
        _freshRoutine = null;
        _bottomlessRoutine = null;
        if (_cannon != null)
        {
            _cannon.InfiniteAmmo = false;
            _cannon._rtData.fireDamageMultiplier = 1f;
        }

        foreach (var m in _mines) if (m != null) Destroy(m.gameObject);
        _mines.Clear();

        RelicRuntime.PierceCount = 0;
        RelicRuntime.SuppressProcs = false;
        _garfioKills = 0;
        _bottomlessCounter = 0;
        _reviveUsed = false;
        _cannonHooked = _healthHooked = false;

        // la run nueva puede crear otro cañón/jugador: volver a buscarlos
        _cannon = null; _stats = null; _critStats = null; _health = null;
    }

    private void FindRefs()
    {
        if (_stats == null)
        {
            var move = FindFirstObjectByType<Movement>();
            if (move != null) _stats = move.GetComponent<RT_PlayerStats>();
            if (_stats == null) _stats = FindFirstObjectByType<RT_PlayerStats>();
        }
        if (_health == null && _stats != null) _health = _stats.GetComponent<RT_PlayerHealth>();
        if (_health == null) _health = FindFirstObjectByType<RT_PlayerHealth>();
        if (_critStats == null)
        {
            // el crítico de la bala se lee de PlayerUpgrades.Stats
            var pu = FindFirstObjectByType<PlayerUpgrades>();
            _critStats = pu != null && pu.Stats != null ? pu.Stats : _stats;
        }
        if (_cannon == null)
        {
            var ac = FindFirstObjectByType<AbilityController>();
            if (ac != null) _cannon = ac.CannonAbility;
        }
    }

    // ------------------------------------------------------------ textos

    // Qué hace la reliquia (panel del cofre y cartel del HUD). Para las Common muestra el total apilado.
    public string Describe(SO_RelicData relic, int count)
    {
        if (relic == null) return "";
        int n = Mathf.Max(1, count);
        switch (relic.effectType)
        {
            case RelicType.PolvoraFina: return $"+{Pct(_damagePct * n)} daño del cañón";
            case RelicType.RelojArena: return $"-{Pct(_reloadPct * n)} tiempo de recarga";
            case RelicType.BalasRepuesto: return $"+{_extraAmmo * n} {(_extraAmmo * n == 1 ? "bala" : "balas")} en el cargador";
            case RelicType.MechaLarga: return $"+{Pct(_explosionPct * n)} radio de explosión";
            case RelicType.Pluma: return $"+{Pct(_bulletSpeedPct * n)} velocidad de bala";
            case RelicType.Salvavidas: return $"+{Pct(_maxHealthPct * n)} vida máxima";
            case RelicType.Catalejo: return $"+{Pct(_critChance * n)} chance de crítico";
            case RelicType.Antorcha: return $"Durante {_freshDuration:0.#}s después de recargar, tus balas pegan +{Pct(_freshDamageMult - 1f)}";
            case RelicType.Garfio: return $"Cada kill hace la próxima recarga {Pct(_killReloadBonus)} más rápida (hasta {Pct(_killReloadMax)}). Se reinicia al recargar";
            case RelicType.UltimoAliento: return $"Con menos de {Pct(_lastBreathThreshold)} de vida, recargás {1f / _lastBreathReloadMul:0.#}x más rápido";
            case RelicType.Pleno: return $"Si una bala mata a {_plenoKills} o más enemigos, te devuelve la bala";
            case RelicType.Mina: return "Las balas que caen al agua dejan una mina flotando que explota si pasa un enemigo";
            case RelicType.Metralla: return $"Los enemigos que matás explotan en {_shrapnelCount} balitas";
            case RelicType.Holandes: return $"La primera vez que te hundirían, volvés con {Pct(_reviveHealthPct)} de vida";
            case RelicType.BalaFantasma: return _pierceCount == 1 ? "Tus balas atraviesan al primer enemigo" : $"Tus balas atraviesan {_pierceCount} enemigos";
            case RelicType.CargadorSinFondo: return $"Cada {_bottomlessKills} kills, {_bottomlessDuration:0.#}s de munición infinita";
            default: return "";
        }
    }

    [System.Obsolete("Usá Describe")] public string DescribeGain(SO_RelicData relic, int count) => Describe(relic, count);
    [System.Obsolete("Usá Describe")] public string DescribeAll(SO_RelicData relic, int count) => Describe(relic, count);

    private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
}
