using System.Collections.Generic;
using UnityEngine;

// Pool para los numeros de daño flotante. Mismo patron que ParticlePool
// (singleton + Queue), pero ahora cada popup avisa cuando termina (su vida
// se extiende si sigue acumulando daño).
//
// Se suscribe a EnemyHealth.OnAnyEnemyDamagedInfo, asi funciona para
// cualquier enemigo que use EnemyHealth sin tocar sus prefabs.
//
// Acumulación: si le volvés a pegar al mismo enemigo (con el mismo tipo de
// daño) dentro de _mergeWindow, el número existente suma en vez de salir otro.
// El fuego (burn / ground fire) acumula aparte de las balas.
public class DamageNumberPool : MonoBehaviour
{
    public static DamageNumberPool Instance { get; private set; }

    [Header("Prefab (Canvas World Space + Text + DamageNumberPopup)")]
    [SerializeField] private GameObject _damageNumberPrefab;
    [SerializeField] private int _poolSize = 20;

    [Header("Timing")]
    [Tooltip("Cuánto vive cada número desde el ÚLTIMO golpe que sumó.")]
    [SerializeField] private float _lifetime = 0.8f;

    [Header("Acumulación")]
    [Tooltip("Si el siguiente golpe al mismo enemigo llega antes de esto (seg), se suma al número existente.")]
    [SerializeField] private float _mergeWindow = 0.5f;
    [Tooltip("Tope: pasado este tiempo desde que apareció, deja de sumar y el próximo golpe saca un número nuevo.")]
    [SerializeField] private float _maxMergeTime = 1.5f;

    [Header("Posicion")]
    [Tooltip("Desplazamiento desde la posicion del enemigo donde aparece el numero.")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Jitter horizontal random para que golpes seguidos no se pisen encima.")]
    [SerializeField] private float _randomJitter = 0.3f;

    [Header("Color por daño (relativo al daño base del cañón)")]
    [Tooltip("De acá sale el daño de referencia (damage del SO). Si queda vacío usa Fallback Reference Damage.")]
    [SerializeField] private SO_CannonData _cannonData;
    [SerializeField] private float _fallbackReferenceDamage = 35f;
    [Tooltip("Cuántas veces el daño base corresponde al final del gradiente (rojo).")]
    [SerializeField] private float _maxRatio = 6f;
    [Tooltip("Izquierda = daño base o menos, derecha = Max Ratio veces el daño base.")]
    [SerializeField] private Gradient _damageGradient = DefaultGradient();

    [Header("Tamaño por daño")]
    [Tooltip("Escala cuando el daño llega a Max Ratio (a daño base es 1).")]
    [SerializeField] private float _maxSizeMultiplier = 1.6f;

    [Header("Crítico")]
    [SerializeField] private Color _critColor = new Color(1f, 0.25f, 0.85f);
    [SerializeField] private float _critSizeMultiplier = 1.2f;
    [SerializeField] private string _critSuffix = "!";
    [SerializeField] private float _critOvershoot = 1.7f;

    [Header("Fuego (burn / ground fire)")]
    [SerializeField] private Color _fireColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private float _fireSizeMultiplier = 0.75f;

    [Header("Punch")]
    [Tooltip("Punch extra de escala cuando el golpe mata al enemigo.")]
    [SerializeField] private float _killPunch = 1.4f;
    [Tooltip("Punch de escala cada vez que el número suma un golpe.")]
    [SerializeField] private float _mergePunch = 1.2f;

    private readonly Queue<DamageNumberPopup> _pool = new();
    private readonly Dictionary<(EnemyHealth, DamageKind), DamageNumberPopup> _active = new();

    // ---- getters para el popup
    public float Lifetime => _lifetime;
    public string CritSuffix => _critSuffix;
    public float CritOvershoot => _critOvershoot;
    public float KillPunch => _killPunch;
    public float MergePunch => _mergePunch;
    private float ReferenceDamage => (_cannonData != null && _cannonData.damage > 0f) ? _cannonData.damage : Mathf.Max(1f, _fallbackReferenceDamage);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < _poolSize; i++)
            _pool.Enqueue(CreatePopup());
    }

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDamagedInfo += HandleEnemyDamaged;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDamagedInfo -= HandleEnemyDamaged;
    }

    private DamageNumberPopup CreatePopup()
    {
        GameObject obj = Instantiate(_damageNumberPrefab, transform);
        obj.SetActive(false);
        var popup = obj.GetComponent<DamageNumberPopup>();
        popup.Finished += Release;
        return popup;
    }

    private void HandleEnemyDamaged(DamageInfo info)
    {
        if (_damageNumberPrefab == null || info.target == null) return;

        var key = (info.target, info.kind);
        if (_active.TryGetValue(key, out DamageNumberPopup existing) &&
            existing != null && existing.CanMerge(Time.time, _mergeWindow, _maxMergeTime))
        {
            existing.Add(info);
            return;
        }

        DamageNumberPopup popup = _pool.Count > 0 ? _pool.Dequeue() : CreatePopup();
        Vector3 jitter = new Vector3(Random.Range(-_randomJitter, _randomJitter), 0f, 0f);
        popup.gameObject.SetActive(true);
        popup.Begin(this, info.target.transform, _spawnOffset + jitter, info);
        _active[key] = popup;
    }

    private void Release(DamageNumberPopup popup)
    {
        popup.gameObject.SetActive(false);
        _pool.Enqueue(popup);

        // sacarlo del diccionario solo si sigue siendo el número activo de ese enemigo
        (EnemyHealth, DamageKind) found = default;
        bool has = false;
        foreach (var kv in _active)
            if (kv.Value == popup) { found = kv.Key; has = true; break; }
        if (has) _active.Remove(found);
    }

    // ---- estilo
    public Color GetColor(float total, DamageKind kind, bool crit)
    {
        if (crit) return _critColor;
        if (kind == DamageKind.Fire) return _fireColor;
        float ratio = total / ReferenceDamage;
        return _damageGradient.Evaluate(Mathf.Clamp01(ratio / Mathf.Max(1f, _maxRatio)));
    }

    public float GetSize(float total, DamageKind kind, bool crit)
    {
        if (kind == DamageKind.Fire) return _fireSizeMultiplier;
        float ratio = total / ReferenceDamage;
        float size = Mathf.Lerp(1f, _maxSizeMultiplier, Mathf.InverseLerp(1f, Mathf.Max(1.01f, _maxRatio), ratio));
        return crit ? size * _critSizeMultiplier : size;
    }

    private static Gradient DefaultGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.167f),                  // daño base
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.33f),     // x2
                new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.58f),    // x3.5
                new GradientColorKey(new Color(1f, 0.15f, 0.1f), 1f)        // x6
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}
