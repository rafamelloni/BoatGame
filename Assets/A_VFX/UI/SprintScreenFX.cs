using UnityEngine;
using UnityEngine.UI;

// Efecto de pantalla al sprintear (y más fuerte en el dash):
//  - Líneas de velocidad estilo anime que "hierven" (shader procedural, toon)
//  - Viñeta suave en los bordes
//  - La cámara se aleja un poco mientras sprinteás
//  - Al arrancar el sprint/dash hay un "golpe" (burst) extra que después se asienta
//
// Ponelo en cualquier GameObject de la escena (por ejemplo el barco o la cámara).
// Busca solo Movement, DashMovement y followPlayer si no los asignás.
// No usa post-process: anda bien en WebGL.
public class SprintScreenFX : MonoBehaviour
{
    [Header("Referencias (opcionales)")]
    [SerializeField] private Movement _movement;
    [SerializeField] private DashMovement _dash;
    [SerializeField] private followPlayer _camera;
    [Tooltip("Opcional. Si queda vacío usa 'Botes/UI SpeedLines' (A_VFX/UI/Resources).")]
    [SerializeField] private Shader _shader;

    [Header("Intensidad")]
    [Tooltip("Qué tan rápido entra el efecto")]
    [SerializeField] private float _fadeInSpeed = 5f;
    [Tooltip("Qué tan rápido se va el efecto")]
    [SerializeField] private float _fadeOutSpeed = 2.5f;
    [Tooltip("Intensidad extra durante el dash")]
    [SerializeField] private float _dashIntensity = 1.4f;
    [Tooltip("Golpe extra al arrancar (se suma y decae)")]
    [SerializeField] private float _burstAmount = 0.6f;
    [SerializeField] private float _burstDecay = 3f;

    [Header("Líneas")]
    [SerializeField] private Color _lineColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private float _lineCount = 90f;
    [Range(0f, 1f)]
    [SerializeField] private float _density = 0.35f;
    [Tooltip("Cuántas veces por segundo cambian las líneas (toon 'boil')")]
    [SerializeField] private float _flicker = 12f;
    [Tooltip("Desde qué distancia del centro empiezan (0 = centro, 1 = borde de arriba)")]
    [SerializeField] private float _innerRadius = 0.45f;
    [Range(0.05f, 1f)]
    [SerializeField] private float _lineWidth = 0.35f;

    [Header("Viñeta")]
    [SerializeField] private Color _vignetteColor = new Color(0.02f, 0.08f, 0.12f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float _vignetteStrength = 0.35f;

    [Header("Cámara")]
    [Tooltip("Cuánto se aleja la cámara al sprintear (metros)")]
    [SerializeField] private float _zoomOut = 2.5f;
    [Tooltip("Alejamiento extra en el golpe inicial")]
    [SerializeField] private float _burstZoom = 1.5f;

    [Header("UI")]
    [Tooltip("Orden del canvas. Negativo = debajo del HUD.")]
    [SerializeField] private int _sortingOrder = -50;

    private Material _mat;
    private RawImage _image;
    private float _intensity;
    private float _burst;
    private bool _wasActive;
    private bool _wasDashing;

    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int AspectID = Shader.PropertyToID("_Aspect");

    private void Start()
    {
        if (_movement == null) _movement = FindFirstObjectByType<Movement>();
        if (_dash == null && _movement != null) _dash = _movement.GetComponent<DashMovement>();
        if (_camera == null) _camera = FindFirstObjectByType<followPlayer>();
        Build();
    }

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }

    private void Update()
    {
        if (_mat == null) return;

        bool sprinting = _movement != null && _movement.enabled && _movement.IsSprinting;
        bool dashing = _dash != null && _dash.IsDashing;
        bool active = sprinting || dashing;

        // golpe al arrancar
        if (active && !_wasActive) _burst = 1f;
        if (dashing && !_wasDashing) _burst = 1.3f;
        _wasActive = active;
        _wasDashing = dashing;

        float target = dashing ? _dashIntensity : (sprinting ? 1f : 0f);
        float speed = target > _intensity ? _fadeInSpeed : _fadeOutSpeed;
        _intensity = Mathf.MoveTowards(_intensity, target, speed * Time.deltaTime);
        _burst = Mathf.MoveTowards(_burst, 0f, _burstDecay * Time.deltaTime);

        float shown = _intensity + _burst * _burstAmount * Mathf.Clamp01(_intensity + 0.3f);

        _mat.SetFloat(IntensityID, shown);
        _mat.SetFloat(AspectID, Screen.height > 0 ? (float)Screen.width / Screen.height : 1.777f);
        _image.enabled = shown > 0.001f;

        if (_camera != null)
            _camera.extraZoom = Mathf.Min(_intensity, 1.2f) * _zoomOut + _burst * _burstZoom * Mathf.Clamp01(_intensity);
    }

    private void Build()
    {
        Shader shader = _shader != null ? _shader : Shader.Find("Botes/UI SpeedLines");
        if (shader == null)
        {
            Debug.LogWarning("[SprintScreenFX] No encontré el shader 'Botes/UI SpeedLines'.");
            return;
        }

        _mat = new Material(shader) { name = "SprintSpeedLines (runtime)" };
        _mat.SetColor("_LineColor", _lineColor);
        _mat.SetFloat("_LineCount", _lineCount);
        _mat.SetFloat("_Density", _density);
        _mat.SetFloat("_Flicker", _flicker);
        _mat.SetFloat("_InnerRadius", _innerRadius);
        _mat.SetFloat("_LineWidth", _lineWidth);
        _mat.SetColor("_VignetteColor", _vignetteColor);
        _mat.SetFloat("_VignetteStrength", _vignetteStrength);
        _mat.SetFloat(IntensityID, 0f);

        var canvasGo = new GameObject("SprintFXCanvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _sortingOrder;

        var imgGo = new GameObject("SpeedLines", typeof(RectTransform), typeof(RawImage));
        imgGo.transform.SetParent(canvasGo.transform, false);
        var r = (RectTransform)imgGo.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;

        _image = imgGo.GetComponent<RawImage>();
        _image.material = _mat;
        _image.raycastTarget = false;
        _image.enabled = false;
    }

    // Para probar los valores en vivo desde el Inspector
    private void OnValidate()
    {
        if (_mat == null) return;
        _mat.SetColor("_LineColor", _lineColor);
        _mat.SetFloat("_LineCount", _lineCount);
        _mat.SetFloat("_Density", _density);
        _mat.SetFloat("_Flicker", _flicker);
        _mat.SetFloat("_InnerRadius", _innerRadius);
        _mat.SetFloat("_LineWidth", _lineWidth);
        _mat.SetColor("_VignetteColor", _vignetteColor);
        _mat.SetFloat("_VignetteStrength", _vignetteStrength);
    }
}
