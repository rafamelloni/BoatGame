using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Va en el prefab del numero de daño (un Canvas en World Space con un
// TextMeshProUGUI adentro, armado a mano). Lo maneja DamageNumberPool.
//
// - Pop con rebote al aparecer (más rebote si es crítico o si mató)
// - Sigue al enemigo mientras acumula daño; cada golpe que suma hace un "punch"
// - Color y tamaño según el daño acumulado (los define el pool)
// - Sólido la mayor parte del tiempo; al final se achica rápido
// - Se dibuja encima de todo (shader TMP Overlay)
public class DamageNumberPopup : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI _text;
    [Tooltip("Lo que rota para mirar a camara. Si lo dejas vacio, usa el transform del texto.")]
    [SerializeField] private Transform _billboardTarget;

    [Header("Pop (al aparecer)")]
    [SerializeField] private float _popDuration = 0.15f;
    [Tooltip("Cuánto se pasa de tamaño antes de rebotar (1 = sin rebote)")]
    [SerializeField] private float _popOvershoot = 1.4f;
    [Tooltip("Duración del punch cuando suma un golpe")]
    [SerializeField] private float _punchDuration = 0.12f;

    [Header("Subida")]
    [Tooltip("Cuánto sube en total (frena al final)")]
    [SerializeField] private float _riseDistance = 0.9f;
    [Tooltip("En cuánto tiempo hace la subida completa")]
    [SerializeField] private float _riseTime = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float _sidewaysRandomness = 0.4f;

    [Header("Salida")]
    [Tooltip("Parte de la vida en la que se ve sólido. El resto es la salida.")]
    [Range(0.3f, 0.95f)]
    [SerializeField] private float _solidFraction = 0.7f;
    [Tooltip("Si está prendido, en la salida también se achica (más toon). Si no, solo baja el alpha.")]
    [SerializeField] private bool _shrinkOnExit = true;

    [Header("Render encima de todo")]
    [Tooltip("Arrastrá 'TMP_SDF Overlay' (o 'TMP_SDF-Mobile Overlay' si tu fuente usa el Mobile). " +
             "Si lo dejás vacío lo busca por nombre, pero en el build (WebGL) puede no estar incluido.")]
    [SerializeField] private Shader _overlayShader;
    [SerializeField] private int _canvasSortingOrder = 100;

    public event Action<DamageNumberPopup> Finished;

    private static readonly Dictionary<Material, Material> _overlayCache = new Dictionary<Material, Material>();

    private DamageNumberPool _pool;
    private Transform _follow;
    private Vector3 _offset;
    private Vector3 _anchor;
    private Vector3 _moveDir;
    private Vector3 _targetScale; // la escala que configuraste a mano en el prefab (ej 0.01)
    private Camera _cam;

    private float _total;
    private bool _anyCrit;
    private bool _closed;          // mató al enemigo: no suma más
    private DamageKind _kind;
    private float _startTime;
    private float _lastHitTime;

    private float _age;            // desde el último golpe (define la salida)
    private float _spawnAge;       // desde que apareció (pop + subida)
    private float _overshoot;
    private float _sizeMul = 1f;
    private float _punchTimer;
    private float _punchAmount = 1f;
    private float _lifetime = 0.8f;
    private bool _running;

    private void Awake()
    {
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();
        if (_billboardTarget == null && _text != null) _billboardTarget = _text.transform;
        _targetScale = transform.localScale;
        SetupRenderOnTop();
    }

    private void OnEnable()
    {
        if (_cam == null) _cam = Camera.main;
        transform.localScale = Vector3.zero;
    }

    private void OnDisable()
    {
        _running = false;
    }

    // ---- API del pool
    public void Begin(DamageNumberPool pool, Transform follow, Vector3 offset, DamageInfo info)
    {
        _pool = pool;
        _follow = follow;
        _offset = offset;
        _anchor = (follow != null ? follow.position : info.position) + offset;

        _kind = info.kind;
        _total = info.damage;
        _anyCrit = info.isCrit;
        _closed = info.killed;
        _startTime = _lastHitTime = Time.time;
        _lifetime = pool != null ? pool.Lifetime : 0.8f;

        _age = 0f;
        _spawnAge = 0f;
        _punchTimer = 0f;
        _overshoot = _anyCrit && pool != null ? pool.CritOvershoot : _popOvershoot;
        if (info.killed && pool != null) _overshoot *= 1.15f;

        float sideways = UnityEngine.Random.Range(-_sidewaysRandomness, _sidewaysRandomness);
        _moveDir = new Vector3(sideways, 1f, 0f).normalized;

        RefreshVisual();
        transform.position = _anchor;
        transform.localScale = Vector3.zero;
        _running = true;
    }

    public bool CanMerge(float now, float window, float maxTime)
    {
        return _running && !_closed &&
               now - _lastHitTime <= window &&
               now - _startTime <= maxTime;
    }

    public void Add(DamageInfo info)
    {
        _total += info.damage;
        _anyCrit |= info.isCrit;
        _lastHitTime = Time.time;
        _age = 0f; // vuelve a estar sólido

        float punch = _pool != null ? _pool.MergePunch : 1.2f;
        if (info.killed)
        {
            _closed = true;
            punch = _pool != null ? _pool.KillPunch : 1.4f;
        }
        Punch(punch);
        RefreshVisual();
    }

    // compatibilidad con el uso viejo
    public void Show(float damage)
    {
        _total = damage;
        RefreshVisual();
    }

    // ---- animación
    private void Update()
    {
        if (!_running) return;

        float dt = Time.deltaTime;
        _age += dt;
        _spawnAge += dt;

        if (_age >= _lifetime)
        {
            _running = false;
            Finished?.Invoke(this);
            return;
        }

        // seguir al enemigo mientras exista
        if (_follow != null && _follow.gameObject.activeInHierarchy)
            _anchor = _follow.position + _offset;

        // subida frenando
        float rise01 = 1f - Mathf.Pow(1f - Mathf.Clamp01(_spawnAge / Mathf.Max(0.01f, _riseTime)), 3f);
        transform.position = _anchor + _moveDir * (_riseDistance * rise01);

        // escala: tamaño por daño * pop inicial * punch * salida
        float scale = _sizeMul;
        if (_spawnAge < _popDuration)
            scale *= EaseOutBack(_spawnAge / _popDuration, _overshoot);

        if (_punchTimer > 0f)
        {
            _punchTimer -= dt;
            float p = Mathf.Clamp01(_punchTimer / _punchDuration);
            scale *= 1f + (_punchAmount - 1f) * p;
        }

        float life01 = _age / _lifetime;
        float exit01 = life01 > _solidFraction ? Mathf.Clamp01((life01 - _solidFraction) / (1f - _solidFraction)) : 0f;
        if (_shrinkOnExit) scale *= 1f - exit01 * exit01;

        transform.localScale = _targetScale * scale;

        if (_cam != null && _billboardTarget != null)
            _billboardTarget.rotation = _cam.transform.rotation;

        if (_text != null)
        {
            Color c = _text.color;
            c.a = 1f - exit01;
            _text.color = c;
        }
    }

    private void Punch(float amount)
    {
        _punchAmount = amount;
        _punchTimer = _punchDuration;
    }

    private void RefreshVisual()
    {
        if (_text == null) return;
        string suffix = _anyCrit && _pool != null ? _pool.CritSuffix : "";
        _text.text = Mathf.RoundToInt(_total) + suffix;

        Color c = _pool != null ? _pool.GetColor(_total, _kind, _anyCrit) : Color.white;
        c.a = 1f;
        _text.color = c;
        _sizeMul = _pool != null ? _pool.GetSize(_total, _kind, _anyCrit) : 1f;
    }

    // 0 -> pasa de 1 (overshoot) -> 1
    private static float EaseOutBack(float t, float overshoot)
    {
        t = Mathf.Clamp01(t);
        float s = overshoot > 1f ? (overshoot - 1f) * 8f + 0.9f : 0f; // aprox: 1.1 -> 1.7, 1.4 -> 4.1 (pico ≈ overshoot)
        t -= 1f;
        return t * t * ((s + 1f) * t + s) + 1f;
    }

    private void SetupRenderOnTop()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = _canvasSortingOrder;
        }

        if (_text == null) return;
        Material src = _text.fontSharedMaterial;
        if (src == null) return;

        if (!_overlayCache.TryGetValue(src, out Material overlay) || overlay == null)
        {
            Shader shader = _overlayShader;
            if (shader == null)
            {
                bool mobile = src.shader != null && src.shader.name.Contains("Mobile");
                shader = Shader.Find(mobile ? "TextMeshPro/Mobile/Distance Field Overlay" : "TextMeshPro/Distance Field Overlay");
            }
            if (shader == null)
            {
                Debug.LogWarning("[DamageNumberPopup] No encontré el shader TMP Overlay: asignalo en el campo Overlay Shader del prefab.");
                return;
            }

            overlay = new Material(src) { name = src.name + " (Overlay)", shader = shader };
            overlay.renderQueue = 4000; // Overlay: después de todo lo transparente (partículas)
            _overlayCache[src] = overlay;
        }
        _text.fontSharedMaterial = overlay;
    }
}
