using UnityEngine;

// Hace aparecer un poco de la espuma del agua debajo del crosshair.
// La posición sigue al cursor con retraso (lerp), así la espuma "llega" detrás del mouse,
// y al moverse va dejando una estela de la misma espuma que se apaga sola.
// No usa turbulencia: no agrega brillo ni tinte, solo espuma (la dibuja WaterToon.hlsl).
// Ponelo en cualquier GameObject de la escena.
public class CrosshairWaterWake : MonoBehaviour
{
    private static readonly int CursorFoamID = Shader.PropertyToID("_CursorFoam");
    private static readonly int CursorFoamParamsID = Shader.PropertyToID("_CursorFoamParams");
    private static readonly int CursorFoamTrailID = Shader.PropertyToID("_CursorFoamTrail");

    public const int MaxTrail = 12; // tiene que coincidir con MAX_CURSOR_TRAIL de WaterToon.hlsl

    [Header("Referencias (opcionales)")]
    [Tooltip("Si queda vacío usa Camera.main")]
    [SerializeField] private Camera _cam;
    [Tooltip("Para saber la altura del agua. Si queda vacío busca el objeto con Movement.")]
    [SerializeField] private Transform _player;

    [Header("Seguimiento")]
    [Tooltip("Qué tan rápido alcanza al cursor. Más bajo = más retraso.")]
    [SerializeField] private float _followSpeed = 6f;

    [Header("Espuma")]
    [Tooltip("Radio de la espuma debajo del cursor, en metros")]
    [SerializeField, Min(0.05f)] private float _radius = 1.6f;
    [Tooltip("Cuánta espuma aparece (0..1). Bajo = solo unas manchitas.")]
    [SerializeField, Range(0f, 1f)] private float _amount = 0.45f;
    [Tooltip("Espuma extra mientras movés el mouse (0 = siempre igual)")]
    [SerializeField, Range(0f, 1f)] private float _extraWhenMoving = 0.15f;
    [Tooltip("Tamaño de las manchas de espuma (más alto = manchas más chicas)")]
    [SerializeField] private float _noiseScale = 2.2f;
    [Tooltip("Velocidad a la que se mueven las manchas")]
    [SerializeField] private float _noiseSpeed = 0.8f;
    [Tooltip("Cuánto tarda en aparecer/desaparecer (cuando el cursor entra o sale del agua)")]
    [SerializeField] private float _fadeSpeed = 4f;

    [Header("Estela")]
    [Tooltip("Ancho de la estela: radio de cada punto, en metros")]
    [SerializeField, Min(0.05f)] private float _trailWidth = 1.4f;
    [Tooltip("Grosor de la estela al final, relativo al ancho (1 = no se afina, 0.3 = termina finita)")]
    [SerializeField, Range(0.05f, 1f)] private float _trailEndWidth = 0.6f;
    [Tooltip("Qué parte del ancho tiene espuma pareja (0 = solo en el centro, 0.9 = casi todo el ancho lleno)")]
    [SerializeField, Range(0f, 0.95f)] private float _trailFill = 0.35f;
    [Tooltip("Cada cuántos metros se deja un punto de estela (más bajo = estela más continua)")]
    [SerializeField, Min(0.05f)] private float _trailSpacing = 0.35f;
    [Tooltip("Segundos que tarda en desaparecer la estela (más alto = estela más larga)")]
    [SerializeField, Min(0.05f)] private float _trailLifetime = 1.4f;
    [Tooltip("Cuánta espuma tiene cada punto de estela al nacer (0..1)")]
    [SerializeField, Range(0f, 1f)] private float _trailStrength = 0.75f;
    [Tooltip("Fracción de la vida en la que la estela se mantiene firme antes de empezar a apagarse")]
    [SerializeField, Range(0f, 0.9f)] private float _trailHold = 0.35f;

    private struct TrailPoint { public Vector2 pos; public float age; public float strength; }
    private readonly TrailPoint[] _trail = new TrailPoint[MaxTrail];
    private readonly Vector4[] _trailBuffer = new Vector4[MaxTrail];
    private int _trailNext;
    private float _distAcc;

    private Vector3 _smoothPos;
    private bool _hasPos;
    private float _intensity;
    private float _moveBoost;

    private void OnDisable()
    {
        _hasPos = false;
        _intensity = 0f;
        for (int i = 0; i < MaxTrail; i++) _trail[i].strength = 0f;
        Shader.SetGlobalVector(CursorFoamID, Vector4.zero);
        UploadTrail(dt: 0f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // en pausa queda como está

        bool onWater = TryGetCursorOnWater(out Vector3 target);

        if (onWater)
        {
            if (!_hasPos) { _smoothPos = target; _hasPos = true; }
            Vector3 prev = _smoothPos;
            _smoothPos = Vector3.Lerp(_smoothPos, target, 1f - Mathf.Exp(-_followSpeed * dt));

            // un poquito más de espuma mientras se mueve
            float moved = (_smoothPos - prev).magnitude;
            float speed = moved / dt;

            // estela: un punto cada _trailSpacing metros recorridos
            _distAcc += moved;
            if (_distAcc >= _trailSpacing)
            {
                _distAcc = 0f;
                _trail[_trailNext] = new TrailPoint
                {
                    pos = new Vector2(_smoothPos.x, _smoothPos.z),
                    age = 0f,
                    strength = _trailStrength
                };
                _trailNext = (_trailNext + 1) % MaxTrail;
            }
            float targetBoost = Mathf.Clamp01(speed / 20f) * _extraWhenMoving;
            _moveBoost = Mathf.Lerp(_moveBoost, targetBoost, 1f - Mathf.Exp(-6f * dt));
        }

        float targetIntensity = onWater ? _amount + _moveBoost : 0f;
        _intensity = Mathf.MoveTowards(_intensity, targetIntensity, _fadeSpeed * dt);

        Shader.SetGlobalVector(CursorFoamID, new Vector4(_smoothPos.x, _smoothPos.z, _radius, _hasPos ? _intensity : 0f));
        UploadTrail(dt);
    }

    private void UploadTrail(float dt)
    {
        float life = Mathf.Max(_trailLifetime, 0.01f);
        for (int i = 0; i < MaxTrail; i++)
        {
            ref TrailPoint p = ref _trail[i];
            p.age += dt;
            // se mantiene firme un rato y después se apaga
            float k = 1f - Mathf.Clamp01((p.age / life - _trailHold) / Mathf.Max(1f - _trailHold, 0.01f)); // 1 -> 0
            float w = p.strength * k;
            // se achica un poco mientras se apaga
            float r = _trailWidth * Mathf.Lerp(_trailEndWidth, 1f, k);
            _trailBuffer[i] = w > 0.001f ? new Vector4(p.pos.x, p.pos.y, r, w) : Vector4.zero;
        }
        // siempre el array entero: Unity fija el tamaño en la primera llamada
        Shader.SetGlobalVectorArray(CursorFoamTrailID, _trailBuffer);
        Shader.SetGlobalVector(CursorFoamParamsID, new Vector4(_noiseScale, _noiseSpeed, MaxTrail, _trailFill));
    }

    // Para chequear en la Scene view que la estela se está generando
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < MaxTrail; i++)
        {
            Vector4 b = _trailBuffer[i];
            if (b.w <= 0f) continue;
            Gizmos.DrawWireSphere(new Vector3(b.x, _smoothPos.y, b.y), b.z * b.w);
        }
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(_smoothPos, _radius);
    }

    private bool TryGetCursorOnWater(out Vector3 point)
    {
        point = default;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || !_cam.isActiveAndEnabled) return false;

        if (_player == null)
        {
            var mv = FindFirstObjectByType<Movement>();
            if (mv != null) _player = mv.transform;
        }

        // mismo plano que usa CannonAimManager (altura del barco); la espuma solo usa XZ
        float y = _player != null ? _player.position.y : 0f;
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
        if (!plane.Raycast(ray, out float d)) return false;
        point = ray.GetPoint(d);
        return true;
    }
}
