using UnityEngine;
using UnityEngine.UI;

// Barra de vida flotante para enemigos grandes (ShipEnemy, SpikeBall, ...).
// Ponelo en el prefab del enemigo (mismo objeto que EnemyHealth o un hijo).
// También sirve para las torres (SpecialTower): lee la vida de cualquier IHealthBarSource.
// Arma sola su Canvas en World Space, sigue al enemigo y mira a cámara.
//
// Game feel:
//  - La vida real baja de golpe (rojo sólido)
//  - Lo que se perdió queda marcado con menos alpha ("trail")
//  - Después de un ratito el trail se drena hasta la vida real a velocidad fija
//  - En cada golpe: flash blanco + punch de escala + un temblorcito
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealth _health;

    [Header("Posición / tamaño")]
    [Tooltip("Desde el centro del enemigo, en metros (Y = altura).")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 4f, 0f);
    [Tooltip("Ancho de la barra en metros")]
    [SerializeField] private float _worldWidth = 3f;
    [Tooltip("Alto de la barra en metros")]
    [SerializeField] private float _worldHeight = 0.32f;

    [Header("Colores")]
    [SerializeField] private Color _fillColor = new Color(0.93f, 0.11f, 0.14f, 1f);
    [Tooltip("Lo que se acaba de perder: mismo rojo con menos alpha")]
    [SerializeField] private Color _trailColor = new Color(0.93f, 0.11f, 0.14f, 0.4f);
    [Tooltip("Fondo (lo que ya se perdió del todo)")]
    [SerializeField] private Color _backgroundColor = new Color(0.08f, 0.02f, 0.02f, 0.75f);
    [SerializeField] private Color _lineColor = Color.black;
    [SerializeField] private Color _outlineColor = Color.black;
    [SerializeField] private Color _flashColor = Color.white;

    [Header("Separadores")]
    [Tooltip("Si es > 0, una raya cada esta cantidad de vida (más vida = más rayas). Si es 0 usa Segments.")]
    [SerializeField] private float _healthPerSegment = 0f;
    [Tooltip("Cantidad de tramos fija (rayas = tramos - 1) cuando Health Per Segment es 0")]
    [SerializeField] private int _segments = 10;
    [Tooltip("Grosor de cada raya separadora, en metros")]
    [SerializeField] private float _lineWidth = 0.03f;

    [Header("Outline")]
    [Tooltip("Grosor del borde negro alrededor de la barra, en metros")]
    [SerializeField] private float _outlineWidth = 0.05f;

    [Header("Trail (lo que bajó)")]
    [Tooltip("Cuánto espera el trail antes de empezar a bajar")]
    [SerializeField] private float _trailDelay = 0.35f;
    [Tooltip("Velocidad a la que baja el trail (fracción de barra por segundo)")]
    [SerializeField] private float _trailSpeed = 0.6f;

    [Header("Golpe")]
    [SerializeField] private float _flashDuration = 0.07f;
    [SerializeField] private float _punchScale = 1.1f;
    [SerializeField] private float _punchDuration = 0.15f;
    [Tooltip("Temblor en metros")]
    [SerializeField] private float _shakeAmount = 0.06f;

    [Header("Visibilidad")]
    [Tooltip("Si está prendido, la barra aparece recién después del primer golpe")]
    [SerializeField] private bool _hideUntilHit = false;
    [SerializeField] private int _sortingOrder = 50;

    [Header("Render encima de todo")]
    [Tooltip("Opcional. Si queda vacío usa 'Botes/UI Overlay' (A_VFX/UI/Resources/UIOverlay.shader).")]
    [SerializeField] private Shader _overlayShader;

    private static Material _overlayMaterial;

    private const float PixelsPerMeter = 100f;

    private IHealthBarSource _source;
    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _fill;
    private RectTransform _trail;
    private Image _fillImage;
    private RectTransform _linesParent;
    private Camera _cam;

    private float _shown = 1f;       // vida mostrada (sólido)
    private float _trailValue = 1f;  // trail
    private float _trailWait;
    private float _flashTimer;
    private float _punchTimer;
    private bool _wasHit;
    private float _builtMaxHealth = -1f;

    private void Awake()
    {
        if (_health != null) _source = _health;
        if (_source == null) _source = GetComponentInParent<IHealthBarSource>();
        if (_source == null) _source = GetComponentInChildren<IHealthBarSource>();
        if (_source == null)
            Debug.LogWarning($"[EnemyHealthBar] {name}: no encontré EnemyHealth (ni otra fuente de vida).", this);
        Build();
    }

    private void OnEnable()
    {
        // al reaparecer (pool / Revive) arranca llena
        _shown = _trailValue = 1f;
        _wasHit = false;
        _flashTimer = _punchTimer = 0f;
        if (_canvas != null) _canvas.gameObject.SetActive(!_hideUntilHit);
        ApplyBars();
    }

    private void OnDisable()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_canvas != null) Destroy(_canvas.gameObject);
    }

    private void LateUpdate()
    {
        if (_source == null || !_source.IsInitialized || _canvas == null) return;

        // rayas según la vida máxima (por si cambia con el tier)
        float max = _source.GetMaxHealt();
        if (!Mathf.Approximately(max, _builtMaxHealth)) BuildLines(max);

        float target = Mathf.Clamp01(_source.GetHealthNormalized());

        if (target < _shown - 0.0001f)
        {
            // golpe: el sólido baja de una, el trail se queda y espera
            _shown = target;
            _trailWait = _trailDelay;
            _flashTimer = _flashDuration;
            _punchTimer = _punchDuration;
            if (!_wasHit)
            {
                _wasHit = true;
                if (_hideUntilHit) _canvas.gameObject.SetActive(true);
            }
        }
        else if (target > _shown + 0.0001f)
        {
            // curación / reset: sube de una
            _shown = target;
            if (_trailValue < _shown) _trailValue = _shown;
        }

        float dt = Time.deltaTime;
        if (_trailValue > _shown)
        {
            if (_trailWait > 0f) _trailWait -= dt;
            else _trailValue = Mathf.MoveTowards(_trailValue, _shown, _trailSpeed * dt);
        }

        ApplyBars();

        // flash
        if (_flashTimer > 0f)
        {
            _flashTimer -= dt;
            _fillImage.color = _flashColor;
        }
        else _fillImage.color = _fillColor;

        // posición, billboard, punch y temblor
        if (_cam == null) _cam = Camera.main;
        Vector3 pos = transform.position + _worldOffset;
        float scale = 1f;
        if (_punchTimer > 0f)
        {
            _punchTimer -= dt;
            float p = Mathf.Clamp01(_punchTimer / _punchDuration);
            scale = 1f + (_punchScale - 1f) * p;
            if (_cam != null)
                pos += (_cam.transform.right * Random.Range(-1f, 1f) + _cam.transform.up * Random.Range(-1f, 1f)) * (_shakeAmount * p);
        }

        _canvas.transform.position = pos;
        if (_cam != null) _canvas.transform.rotation = _cam.transform.rotation;
        _canvas.transform.localScale = Vector3.one * (scale / PixelsPerMeter);
    }

    private void ApplyBars()
    {
        if (_fill == null) return;
        _fill.anchorMax = new Vector2(_shown, 1f);
        _trail.anchorMax = new Vector2(Mathf.Max(_trailValue, _shown), 1f);
    }

    // ---------- construcción ----------
    private void Build()
    {
        var canvasGo = new GameObject($"{name}_HealthBar", typeof(RectTransform), typeof(Canvas));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = _sortingOrder;

        _root = (RectTransform)canvasGo.transform;
        _root.sizeDelta = new Vector2(_worldWidth, _worldHeight) * PixelsPerMeter;
        _root.localScale = Vector3.one / PixelsPerMeter;

        EnsureOverlayMaterial();

        // borde negro: un rect detrás de todo que sobresale _outlineWidth por cada lado
        var outline = CreateRect("Outline", _root, _outlineColor, Vector2.zero, Vector2.one).rectTransform;
        float o = _outlineWidth * PixelsPerMeter;
        outline.offsetMin = new Vector2(-o, -o);
        outline.offsetMax = new Vector2(o, o);

        CreateRect("Background", _root, _backgroundColor, Vector2.zero, Vector2.one);
        _trail = CreateRect("Trail", _root, _trailColor, Vector2.zero, Vector2.one).rectTransform;
        _fillImage = CreateRect("Fill", _root, _fillColor, Vector2.zero, Vector2.one);
        _fill = _fillImage.rectTransform;

        var lines = new GameObject("Lines", typeof(RectTransform));
        _linesParent = (RectTransform)lines.transform;
        _linesParent.SetParent(_root, false);
        Stretch(_linesParent, Vector2.zero, Vector2.one);

        canvasGo.SetActive(false);
    }

    private void BuildLines(float maxHealth)
    {
        _builtMaxHealth = maxHealth;
        for (int i = _linesParent.childCount - 1; i >= 0; i--)
            Destroy(_linesParent.GetChild(i).gameObject);

        int segments = _healthPerSegment > 0f
            ? Mathf.Clamp(Mathf.CeilToInt(maxHealth / _healthPerSegment), 1, 40)
            : Mathf.Max(1, _segments);

        // rayas a la misma distancia, de alto completo, mismo grosor
        float halfPx = _lineWidth * PixelsPerMeter * 0.5f;
        for (int i = 1; i < segments; i++)
        {
            float x = (float)i / segments;
            var line = CreateRect($"Line{i}", _linesParent, _lineColor, new Vector2(x, 0f), new Vector2(x, 1f)).rectTransform;
            line.offsetMin = new Vector2(-halfPx, 0f);
            line.offsetMax = new Vector2(halfPx, 0f);
        }
    }

    private void EnsureOverlayMaterial()
    {
        if (_overlayMaterial != null) return;
        Shader shader = _overlayShader != null ? _overlayShader : Shader.Find("Botes/UI Overlay");
        if (shader == null)
        {
            Debug.LogWarning("[EnemyHealthBar] No encontré el shader 'Botes/UI Overlay': la barra se va a poder tapar.");
            return;
        }
        _overlayMaterial = new Material(shader) { name = "HealthBar (Overlay)" };
        // después de todo lo transparente (partículas = 3000), antes de los damage numbers (4000)
        _overlayMaterial.renderQueue = 3500;
    }

    private static Image CreateRect(string n, RectTransform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        Stretch(r, anchorMin, anchorMax);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (_overlayMaterial != null) img.material = _overlayMaterial;
        return img;
    }

    private static void Stretch(RectTransform r, Vector2 anchorMin, Vector2 anchorMax)
    {
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.pivot = new Vector2(0.5f, 0.5f);
    }
}
