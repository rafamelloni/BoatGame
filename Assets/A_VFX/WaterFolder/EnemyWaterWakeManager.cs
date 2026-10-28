using System.Collections.Generic;
using UnityEngine;

// Pinta las estelas de todos los enemigos en una textura vista desde arriba que sigue al jugador,
// y el agua (WaterToon.hlsl, EnemyWake_Mask) la lee para dibujar la espuma.
// No tiene límite de enemigos ni de largo: cada píxel se apaga solo con el tiempo,
// aunque el enemigo que lo dejó ya se haya desactivado.
// Ponelo en un GameObject de la escena para ajustar los valores globales.
// Si no está, se crea solo con los valores por defecto.
[DefaultExecutionOrder(100)]
public class EnemyWaterWakeManager : MonoBehaviour
{
    public const int MaxStampsPerPass = 64; // tiene que coincidir con MAX_STAMPS de EnemyWakeStamp.shader

    private static readonly int WakeTexID = Shader.PropertyToID("_EnemyWakeTex");
    private static readonly int WakeGlobalAreaID = Shader.PropertyToID("_EnemyWakeArea");
    private static readonly int WakeParamsID = Shader.PropertyToID("_EnemyWakeParams");

    private static readonly int AreaID = Shader.PropertyToID("_WakeArea");
    private static readonly int PrevAreaID = Shader.PropertyToID("_WakePrevArea");
    private static readonly int DeltaID = Shader.PropertyToID("_WakeDeltaTime");
    private static readonly int CountID = Shader.PropertyToID("_StampCount");
    private static readonly int StampAID = Shader.PropertyToID("_StampA");
    private static readonly int StampBID = Shader.PropertyToID("_StampB");
    private static readonly int StampCID = Shader.PropertyToID("_StampC");

    public static EnemyWaterWakeManager Instance { get; private set; }
    private static bool _quitting;

    [Header("Zona que cubre (sigue al jugador)")]
    [Tooltip("A quién sigue la zona. Si queda vacío busca el objeto con Movement (y si no, la cámara).")]
    [SerializeField] private Transform _follow;
    [Tooltip("Lado del cuadrado alrededor del jugador donde se dibujan estelas, en metros. Más grande = más área pero menos detalle.")]
    [SerializeField, Min(10f)] private float _areaSize = 90f;
    [Tooltip("Resolución de la textura de estelas. Más alto = bordes más finos (y más costo).")]
    [SerializeField] private int _resolution = 512;

    [Header("Espuma (vale para todas las estelas)")]
    [Tooltip("Tamaño de las manchas de espuma (más alto = manchas más chicas)")]
    [SerializeField] private float _noiseScale = 2.6f;
    [Tooltip("Velocidad a la que se mueven las manchas")]
    [SerializeField] private float _noiseSpeed = 0.8f;
    [Tooltip("Cómo desaparece. 0 = se afina de forma pareja hasta ser una línea. 1 = además se deshace en manchas.")]
    [SerializeField, Range(0f, 1f)] private float _dissolve = 0.15f;
    [Tooltip("Borde de la estela. Bajo = borde deshilachado con manchas; alto = borde limpio.")]
    [SerializeField, Range(1f, 40f)] private float _edgeSharpness = 8f;

    [Header("Debug")]
    [Tooltip("Muestra la textura de estelas en una esquina de la pantalla")]
    [SerializeField] private bool _showDebugTexture = false;
    [Tooltip("Shader de las estelas. Si queda vacío lo busca solo (Hidden/Botes/EnemyWakeStamp).")]
    [SerializeField] private Shader _stampShader;

    private struct Stamp { public Vector4 a, b, c; }

    private readonly List<Stamp> _pending = new List<Stamp>();
    private readonly Vector4[] _bufA = new Vector4[MaxStampsPerPass];
    private readonly Vector4[] _bufB = new Vector4[MaxStampsPerPass];
    private readonly Vector4[] _bufC = new Vector4[MaxStampsPerPass];

    private RenderTexture _rtA, _rtB;
    private Material _mat;
    private Vector2 _prevCenter;
    private bool _hasPrev;
    private bool _failed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        _quitting = false;
        Application.quitting -= OnQuit;
        Application.quitting += OnQuit;
    }

    private static void OnQuit() => _quitting = true;

    // Devuelve el manager, y si no hay ninguno en la escena lo crea.
    public static EnemyWaterWakeManager Get()
    {
        if (Instance != null || _quitting) return Instance;
        Instance = FindFirstObjectByType<EnemyWaterWakeManager>();
        if (Instance == null)
            Instance = new GameObject("EnemyWaterWakeManager").AddComponent<EnemyWaterWakeManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_mat != null) Destroy(_mat);
    }

    private void OnDisable()
    {
        Release();
        _pending.Clear();
        Shader.SetGlobalVector(WakeGlobalAreaID, Vector4.zero); // apaga la lectura en el agua
    }

    // Lo llama cada EnemyWaterWake: un tramo desde donde estaba el frame anterior hasta donde está ahora.
    public void AddStamp(Vector2 from, Vector2 to, float radius, float lifetime, float strength, float hold, float endWidth)
    {
        float shape = 1f / Mathf.Max(1f - Mathf.Clamp01(endWidth) * 0.9f, 0.1f);
        _pending.Add(new Stamp
        {
            a = new Vector4(from.x, from.y, to.x, to.y),
            b = new Vector4(Mathf.Max(radius, 0.01f), 1f / Mathf.Max(lifetime, 0.01f), Mathf.Clamp01(strength), shape),
            c = new Vector4(Mathf.Clamp(hold, 0f, 0.95f), 0f, 0f, 0f)
        });
    }

    // Borra todas las estelas de golpe (por ejemplo al resetear la escena)
    public void ClearAll()
    {
        _pending.Clear();
        ClearRT(_rtA);
        ClearRT(_rtB);
    }

    private void LateUpdate()
    {
        if (!EnsureResources()) { _pending.Clear(); return; }

        float dt = Time.deltaTime;
        float size = _areaSize;
        float texel = size / _resolution;

        // centro pegado a la grilla de píxeles, así al desplazarse no se emborrona
        Vector2 target = GetFollowXZ();
        Vector2 center = new Vector2(Mathf.Round(target.x / texel) * texel, Mathf.Round(target.y / texel) * texel);
        if (!_hasPrev) { _prevCenter = center; _hasPrev = true; }

        if (dt > 0f) // en pausa queda como está
        {
            int passes = Mathf.Max(1, Mathf.CeilToInt(_pending.Count / (float)MaxStampsPerPass));
            for (int p = 0; p < passes; p++)
            {
                int start = p * MaxStampsPerPass;
                int n = Mathf.Min(MaxStampsPerPass, _pending.Count - start);
                for (int i = 0; i < MaxStampsPerPass; i++)
                {
                    if (i < n)
                    {
                        var s = _pending[start + i];
                        _bufA[i] = s.a; _bufB[i] = s.b; _bufC[i] = s.c;
                    }
                    else
                    {
                        _bufA[i] = Vector4.zero; _bufB[i] = Vector4.zero; _bufC[i] = Vector4.zero;
                    }
                }

                bool first = p == 0;
                _mat.SetVector(AreaID, new Vector4(center.x, center.y, size, 0f));
                _mat.SetVector(PrevAreaID, first ? new Vector4(_prevCenter.x, _prevCenter.y, size, 0f) : new Vector4(center.x, center.y, size, 0f));
                _mat.SetFloat(DeltaID, first ? dt : 0f);
                _mat.SetFloat(CountID, Mathf.Max(n, 0));
                // siempre el array entero: Unity fija el tamaño en la primera llamada
                _mat.SetVectorArray(StampAID, _bufA);
                _mat.SetVectorArray(StampBID, _bufB);
                _mat.SetVectorArray(StampCID, _bufC);

                Graphics.Blit(_rtA, _rtB, _mat);
                (_rtA, _rtB) = (_rtB, _rtA);
            }
            _prevCenter = center;
        }
        _pending.Clear();

        Shader.SetGlobalTexture(WakeTexID, _rtA);
        Shader.SetGlobalVector(WakeGlobalAreaID, new Vector4(_prevCenter.x, _prevCenter.y, size, 1f));
        Shader.SetGlobalVector(WakeParamsID, new Vector4(_noiseScale, _noiseSpeed, _dissolve, _edgeSharpness));
    }

    private Vector2 GetFollowXZ()
    {
        if (_follow == null)
        {
            var mv = FindFirstObjectByType<Movement>();
            if (mv != null) _follow = mv.transform;
        }
        Vector3 p = _follow != null ? _follow.position
                  : Camera.main != null ? Camera.main.transform.position
                  : Vector3.zero;
        return new Vector2(p.x, p.z);
    }

    private bool EnsureResources()
    {
        if (_failed) return false;

        if (_mat == null)
        {
            if (_stampShader == null) _stampShader = Shader.Find("Hidden/Botes/EnemyWakeStamp");
            if (_stampShader == null || !_stampShader.isSupported)
            {
                Debug.LogWarning("[EnemyWaterWakeManager] No encuentro el shader Hidden/Botes/EnemyWakeStamp (tiene que estar en una carpeta Resources). Sin estelas de enemigos.");
                _failed = true;
                return false;
            }
            _mat = new Material(_stampShader) { hideFlags = HideFlags.HideAndDontSave };
        }

        _resolution = Mathf.Clamp(_resolution, 64, 2048);
        if (_rtA == null || _rtA.width != _resolution)
        {
            RenderTextureFormat fmt = RenderTextureFormat.ARGBHalf;
            if (!SystemInfo.SupportsRenderTextureFormat(fmt)) fmt = RenderTextureFormat.ARGBFloat;
            if (!SystemInfo.SupportsRenderTextureFormat(fmt))
            {
                Debug.LogWarning("[EnemyWaterWakeManager] La plataforma no soporta texturas half/float. Sin estelas de enemigos.");
                _failed = true;
                return false;
            }
            Release();
            _rtA = CreateRT(fmt);
            _rtB = CreateRT(fmt);
            _hasPrev = false;
        }
        return true;
    }

    private RenderTexture CreateRT(RenderTextureFormat fmt)
    {
        var rt = new RenderTexture(_resolution, _resolution, 0, fmt, RenderTextureReadWrite.Linear)
        {
            name = "EnemyWakeRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.HideAndDontSave
        };
        rt.Create();
        ClearRT(rt);
        return rt;
    }

    private static void ClearRT(RenderTexture rt)
    {
        if (rt == null) return;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = prev;
    }

    private void Release()
    {
        if (_rtA != null) { _rtA.Release(); Destroy(_rtA); _rtA = null; }
        if (_rtB != null) { _rtB.Release(); Destroy(_rtB); _rtB = null; }
        _hasPrev = false;
    }

    private void OnGUI()
    {
        if (!_showDebugTexture || _rtA == null) return;
        GUI.DrawTexture(new Rect(10, Screen.height - 266, 256, 256), _rtA, ScaleMode.ScaleToFit, false);
    }
}
