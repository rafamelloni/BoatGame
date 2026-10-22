using UnityEngine;

// Indicador de hacia dónde va a dashear el SpikeBallEnemy.
// Ponelo en el prefab del Spike (mismo objeto que SpikeBallEnemy).
//  - Aparece solo mientras carga (WindUp): franja finita sobre el agua, del Spike
//    hasta donde va a quedar frenado. Se llena de atrás hacia adelante con la carga.
//  - Al arrancar el dash: flash y se desvanece rápido (queda quieto donde estaba).
//  - Long dash: más largo y con doble barra al final.
public class SpikeDashTelegraph : MonoBehaviour
{
    [SerializeField] private SpikeBallEnemy _spike;
    [Tooltip("Opcional. Si queda vacío usa 'Botes/DashTelegraph' (A_VFX/UI/Resources).")]
    [SerializeField] private Shader _shader;

    [Header("Forma")]
    [Tooltip("Ancho de la franja (metros)")]
    [SerializeField] private float _width = 2f;
    [Tooltip("Altura sobre el agua (para que las olas no la tapen)")]
    [SerializeField] private float _hoverOverWater = 0.35f;
    [Tooltip("Si no encuentra el agua (tag Floor) abajo, la pone esto por debajo del Spike")]
    [SerializeField] private float _fallbackDrop = 1f;

    [Header("Look")]
    [SerializeField] private Color _color = new Color(1f, 0.35f, 0.25f, 1f);
    [Range(0f, 1f)] [SerializeField] private float _edgeAlpha = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float _fillAlpha = 0.12f;
    [Range(0f, 1f)] [SerializeField] private float _chevronAlpha = 0.45f;

    [Header("Tiempos")]
    [SerializeField] private float _fadeIn = 0.08f;
    [SerializeField] private float _fadeOut = 0.25f;

    private GameObject _quad;
    private Material _mat;
    private float _alpha;
    private float _flash;
    private bool _wasWindingUp;
    private bool _locked;           // ya salió el dash: se queda quieto y se apaga
    private float _waterY;
    private bool _hasWaterY;

    private static readonly int ProgressID = Shader.PropertyToID("_Progress");
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");
    private static readonly int FlashID = Shader.PropertyToID("_Flash");
    private static readonly int LengthID = Shader.PropertyToID("_Length");
    private static readonly int WidthID = Shader.PropertyToID("_Width");
    private static readonly int EndMarkerID = Shader.PropertyToID("_EndMarker");

    private void Awake()
    {
        if (_spike == null) _spike = GetComponent<SpikeBallEnemy>();
        Build();
    }

    private void OnEnable()
    {
        _alpha = 0f; _flash = 0f; _locked = false; _wasWindingUp = false; _hasWaterY = false;
        if (_quad != null) _quad.SetActive(false);
    }

    private void OnDisable()
    {
        if (_quad != null) _quad.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_quad != null) Destroy(_quad);
        if (_mat != null) Destroy(_mat);
    }

    private void LateUpdate()
    {
        if (_spike == null || _mat == null) return;
        float dt = Time.deltaTime;

        bool winding = _spike.isActiveAndEnabled && _spike.IsWindingUp;

        if (winding)
        {
            if (!_wasWindingUp) { _locked = false; _hasWaterY = false; _flash = 0f; }
            _alpha = Mathf.MoveTowards(_alpha, 1f, dt / Mathf.Max(0.001f, _fadeIn));
            PlaceAlongPrediction();
            _mat.SetFloat(ProgressID, _spike.WindUpProgress01);
            _mat.SetFloat(EndMarkerID, _spike.NextAttackIsLong ? 1f : 0f);
        }
        else if (_wasWindingUp)
        {
            // terminó la carga: si salió el dash, flash; en cualquier caso se apaga en el lugar
            _locked = true;
            if (_spike.IsDashing) _flash = 1f;
            _mat.SetFloat(ProgressID, 1f);
        }

        if (!winding)
        {
            _alpha = Mathf.MoveTowards(_alpha, 0f, dt / Mathf.Max(0.001f, _fadeOut));
            _flash = Mathf.MoveTowards(_flash, 0f, dt / Mathf.Max(0.001f, _fadeOut * 0.6f));
        }
        _wasWindingUp = winding;

        bool visible = _alpha > 0.001f;
        if (_quad.activeSelf != visible) _quad.SetActive(visible);
        _mat.SetFloat(AlphaID, _alpha);
        _mat.SetFloat(FlashID, _flash);
    }

    private void PlaceAlongPrediction()
    {
        if (_locked) return;
        if (!_hasWaterY) { _waterY = FindWaterY(); _hasWaterY = true; }

        Vector3 dir = _spike.PredictedDashDirection;
        float length = Mathf.Max(1f, _spike.PredictedDashLength);

        Vector3 start = transform.position;
        start.y = _waterY;

        _quad.transform.SetPositionAndRotation(start, Quaternion.LookRotation(dir, Vector3.up));
        _quad.transform.localScale = new Vector3(_width, 1f, length);
        _mat.SetFloat(LengthID, length);
        _mat.SetFloat(WidthID, _width);
    }

    private float FindWaterY()
    {
        Vector3 origin = transform.position + Vector3.up * 5f;
        var hits = Physics.RaycastAll(origin, Vector3.down, 60f, ~0, QueryTriggerInteraction.Collide);
        float best = float.NegativeInfinity;
        foreach (var h in hits)
            if (h.collider.CompareTag("Floor") && h.point.y > best) best = h.point.y;
        if (best > float.NegativeInfinity) return best + _hoverOverWater;
        return transform.position.y - _fallbackDrop;
    }

    private void Build()
    {
        Shader shader = _shader != null ? _shader : Shader.Find("Botes/DashTelegraph");
        if (shader == null)
        {
            Debug.LogWarning("[SpikeDashTelegraph] No encontré el shader 'Botes/DashTelegraph'.", this);
            return;
        }
        _mat = new Material(shader) { name = "SpikeDashTelegraph (runtime)" };
        _mat.SetColor("_Color", _color);
        _mat.SetFloat("_EdgeAlpha", _edgeAlpha);
        _mat.SetFloat("_FillAlpha", _fillAlpha);
        _mat.SetFloat("_ChevronAlpha", _chevronAlpha);

        // quad plano: x = ancho centrado, z = 0..1 hacia adelante (se escala al largo)
        var mesh = new Mesh { name = "TelegraphQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 0f, 1f), new Vector3(0.5f, 0f, 1f)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // objeto suelto (no hijo) para que no herede la inclinación ni el giro del Spike
        _quad = new GameObject($"{name}_DashTelegraph", typeof(MeshFilter), typeof(MeshRenderer));
        _quad.GetComponent<MeshFilter>().sharedMesh = mesh;
        var mr = _quad.GetComponent<MeshRenderer>();
        mr.sharedMaterial = _mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        _quad.SetActive(false);
    }

    private void OnValidate()
    {
        if (_mat == null) return;
        _mat.SetColor("_Color", _color);
        _mat.SetFloat("_EdgeAlpha", _edgeAlpha);
        _mat.SetFloat("_FillAlpha", _fillAlpha);
        _mat.SetFloat("_ChevronAlpha", _chevronAlpha);
    }
}
