using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Secuencia al darle Play en el menú principal:
//  1. La UI del menú se va deslizando hacia la izquierda.
//  2. El barco deja de pasear y se va derecho hacia la derecha de la pantalla.
//  3. Desde la izquierda llueven balas de mortero (con el mismo aviso en el agua que la torre mortero)
//     que caen alrededor y detrás del barco.
//  4. Cuando el barco sale de pantalla, avisa (onFinished) y MainMenuController carga el juego.
public class MenuPlaySequence : MonoBehaviour
{
    [Header("Debug: apagá de a una para encontrar qué traba")]
    [SerializeField] private bool _enableUiSlide = true;
    [SerializeField] private bool _enableBoatEscape = true;
    [SerializeField] private bool _enableBarrage = true;
    [SerializeField] private bool _enableImpactVfx = true;
    [SerializeField] private bool _enableCameraShake = true;

    [Header("Referencias (vacías = se buscan solas)")]
    [SerializeField] private RectTransform _menuPanel;
    [SerializeField] private MenuBoatAutopilot _boat;
    [SerializeField] private Camera _camera;

    [Header("UI")]
    [SerializeField] private float _uiSlideDuration = 0.45f;
    [Tooltip("Cuánto retrocede la UI antes de salir disparada (en % del ancho de pantalla)")]
    [SerializeField] private float _uiAnticipation = 0.03f;

    [Header("Fin")]
    [Tooltip("Viewport X a partir del cual el barco cuenta como afuera de la pantalla")]
    [SerializeField] private float _exitViewportX = 1.08f;
    [Tooltip("Por si algo falla, a los X segundos carga igual")]
    [SerializeField] private float _maxDuration = 10f;

    [Header("Bombardeo")]
    [Tooltip("Tiempo entre balas (min, max)")]
    [SerializeField] private Vector2 _shellInterval = new Vector2(0.06f, 0.16f);
    [Tooltip("Proporción de balas que caen cerca del barco (el resto se reparte por la pantalla)")]
    [SerializeField, Range(0f, 1f)] private float _chaseRatio = 0.65f;
    [Tooltip("Radio alrededor del barco donde caen las balas que lo persiguen")]
    [SerializeField] private float _chaseRadius = 9f;
    [Tooltip("Distancia mínima al barco (para que no le caigan encima)")]
    [SerializeField] private float _minDistanceToBoat = 3.5f;
    [Tooltip("Cuánto más atrás (hacia la izquierda) del barco tienden a caer")]
    [SerializeField] private float _behindBias = 5f;
    [SerializeField] private float _flightTime = 1.1f;
    [SerializeField] private float _arcHeight = 12f;
    [SerializeField] private float _blastRadius = 2.4f;
    [SerializeField] private float _shellSize = 0.9f;
    [SerializeField] private float _splashSize = 1.6f;

    [Header("Visual")]
    [Tooltip("Partícula al caer (la misma de la torre mortero). Opcional.")]
    [SerializeField] private GameObject _impactVfx;
    [SerializeField] private float _impactVfxLifetime = 3f;
    [SerializeField] private Color _shellColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.18f, 1f);
    [SerializeField] private Color _warningFillColor = new Color(1f, 0.42f, 0.2f, 0.55f);
    [SerializeField] private Color _trailColor = new Color(1f, 0.85f, 0.6f, 0.8f);

    [Header("Rendimiento")]
    [Tooltip("Balas que se crean de antemano al cargar la escena (así no se crean en medio de la secuencia)")]
    [SerializeField] private int _prewarmShells = 16;
    [Tooltip("Explosiones que se crean de antemano y se reciclan")]
    [SerializeField] private int _prewarmVfx = 12;
    [Tooltip("Máximo de balas en el aire a la vez")]
    [SerializeField] private int _maxShellsInAir = 14;

    [Header("Temblor de cámara al caer")]
    [SerializeField] private float _shakeStrength = 0.25f;
    [SerializeField] private float _shakeDuration = 0.25f;

    // VFX de impacto de la TorreMortero (para autocompletar en el editor)
    private const string DefaultImpactVfxGuid = "5a82c2388a1c65b44900743f9eb3c37b";

    private readonly List<TowerShell> _shells = new List<TowerShell>();
    private readonly List<TowerWarningCircle> _warnings = new List<TowerWarningCircle>();
    private readonly List<ParticleSystem[]> _vfxPool = new List<ParticleSystem[]>();
    private readonly List<GameObject> _vfxObjects = new List<GameObject>();
    private readonly List<float> _vfxFreeAt = new List<float>();
    private Material _shellMat, _trailMat;
    private bool _running;
    private Vector3 _camBasePos;
    private float _shakeTimer;

    private void Reset() => AutoFillVfx();

    private void Awake()
    {
        if (_menuPanel == null) _menuPanel = GetComponent<RectTransform>();
        if (_boat == null) _boat = FindFirstObjectByType<MenuBoatAutopilot>();
        if (_camera == null) _camera = Camera.main;
        AutoFillVfx();
    }

    // Todo lo pesado (crear balas, avisos, texturas del aviso, explosiones) se hace al cargar
    // el menú, no en el momento en que tocás Play.
    private void Start()
    {
        for (int i = 0; i < _prewarmShells; i++)
        {
            _shells.Add(CreateShell());
            _warnings.Add(CreateWarning());
        }
        if (_impactVfx != null)
            for (int i = 0; i < _prewarmVfx; i++) CreateVfx();
    }

    private void AutoFillVfx()
    {
#if UNITY_EDITOR
        if (_impactVfx == null)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(DefaultImpactVfxGuid);
            if (!string.IsNullOrEmpty(path))
                _impactVfx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif
    }

    public void Play(Action onFinished)
    {
        if (_running) return;
        _running = true;

        // botones muertos apenas tocás Play
        if (_menuPanel != null)
            foreach (var b in _menuPanel.GetComponentsInChildren<Selectable>()) b.interactable = false;

        if (_enableUiSlide) StartCoroutine(SlideUiOut());
        else if (_menuPanel != null) _menuPanel.anchoredPosition += Vector2.left * 5000f;
        if (_boat != null && _enableBoatEscape) _boat.Escape();
        if (_camera != null) _camBasePos = _camera.transform.position;
        if (_enableBarrage) StartCoroutine(Barrage());
        StartCoroutine(WaitForExit(onFinished));
    }

    // ---------------------------------------------------------------- UI

    private IEnumerator SlideUiOut()
    {
        if (_menuPanel == null) yield break;

        var canvas = _menuPanel.GetComponentInParent<Canvas>();
        float width = canvas != null ? ((RectTransform)canvas.rootCanvas.transform).rect.width : Screen.width;

        Vector2 start = _menuPanel.anchoredPosition;
        Vector2 anticipation = start + Vector2.right * width * _uiAnticipation;
        Vector2 end = start + Vector2.left * (width + _menuPanel.rect.width + 200f);

        // pequeño amague hacia la derecha y después sale disparada a la izquierda
        float t = 0f, d1 = _uiSlideDuration * 0.25f;
        while (t < d1)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d1);
            _menuPanel.anchoredPosition = Vector2.Lerp(start, anticipation, 1f - (1f - k) * (1f - k));
            yield return null;
        }

        t = 0f;
        float d2 = _uiSlideDuration * 0.75f;
        while (t < d2)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d2);
            _menuPanel.anchoredPosition = Vector2.Lerp(anticipation, end, k * k * k); // ease in
            yield return null;
        }

        // No se apaga: en este mismo objeto corren MainMenuController y esta secuencia
    }

    // ---------------------------------------------------------------- final

    private IEnumerator WaitForExit(Action onFinished)
    {
        float elapsed = 0f;
        while (elapsed < _maxDuration)
        {
            elapsed += Time.deltaTime;
            if (_boat != null && _boat.ViewportX > _exitViewportX) break;
            yield return null;
        }
        onFinished?.Invoke();
    }

    // ---------------------------------------------------------------- bombardeo

    private IEnumerator Barrage()
    {
        while (true)
        {
            FireShell();
            yield return new WaitForSeconds(UnityEngine.Random.Range(_shellInterval.x, _shellInterval.y));
        }
    }

    private void FireShell()
    {
        if (_camera == null) return;

        int inAir = 0;
        foreach (var s in _shells) if (s != null && !s.IsFree) inAir++;
        if (inAir >= _maxShellsInAir) return;

        float waterY = _boat != null ? _boat.transform.position.y : 0f;
        Vector3 target;

        if (_boat != null && UnityEngine.Random.value < _chaseRatio)
        {
            // alrededor del barco, más hacia atrás (izquierda de la pantalla), nunca encima
            Vector3 boatPos = _boat.transform.position;
            Vector3 left = -_camera.transform.right;
            left.y = 0f;
            left.Normalize();
            target = boatPos;
            for (int i = 0; i < 8; i++)
            {
                Vector2 r = UnityEngine.Random.insideUnitCircle * _chaseRadius;
                target = boatPos + new Vector3(r.x, 0f, r.y) + left * UnityEngine.Random.Range(0f, _behindBias);
                Vector3 d = target - boatPos;
                d.y = 0f;
                if (d.magnitude >= _minDistanceToBoat) break;
            }
        }
        else
        {
            // cualquier lado de la pantalla, más cargado hacia la izquierda
            float x = Mathf.Pow(UnityEngine.Random.value, 1.6f) * 0.9f + 0.03f;
            target = ViewportToWater(new Vector2(x, UnityEngine.Random.Range(0.08f, 0.92f)), waterY);
        }
        target.y = waterY;

        // salen desde afuera de la pantalla, por la izquierda
        Vector3 from = ViewportToWater(new Vector2(-0.25f, UnityEngine.Random.Range(0.1f, 0.9f)), waterY);
        from.y = waterY + _arcHeight * 0.6f;

        var warning = GetWarning();
        GetShell().Launch(from, target, _flightTime, _arcHeight, 0f, _blastRadius, _shellSize,
                          null, warning, null, _splashSize);
        StartCoroutine(ImpactAfter(target, _flightTime));
    }

    private IEnumerator ImpactAfter(Vector3 point, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_enableImpactVfx) PlayVfx(point);
        if (_enableCameraShake) _shakeTimer = _shakeDuration;
    }

    private void LateUpdate()
    {
        if (!_running || _camera == null) return;
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float s = Mathf.Max(0f, _shakeTimer / _shakeDuration) * _shakeStrength;
            _camera.transform.position = _camBasePos + new Vector3(UnityEngine.Random.Range(-s, s), 0f, UnityEngine.Random.Range(-s, s));
        }
        else
        {
            _camera.transform.position = _camBasePos;
        }
    }

    private Vector3 ViewportToWater(Vector2 viewport, float waterY)
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        Plane plane = new Plane(Vector3.up, new Vector3(0f, waterY, 0f));
        return plane.Raycast(ray, out float dist) ? ray.GetPoint(dist) : new Vector3(0f, waterY, 0f);
    }

    // ---------------------------------------------------------------- pools (igual que MortarTower)

    private TowerShell GetShell()
    {
        foreach (var s in _shells) if (s != null && s.IsFree) return s;
        var shell = CreateShell();
        _shells.Add(shell);
        return shell;
    }

    private TowerWarningCircle GetWarning()
    {
        foreach (var w in _warnings) if (w != null && w.IsFree) return w;
        var warn = CreateWarning();
        _warnings.Add(warn);
        return warn;
    }

    private TowerWarningCircle CreateWarning()
    {
        var root = new GameObject("MenuMortarWarning");
        var warn = root.AddComponent<TowerWarningCircle>();
        warn.BuildDefault(_warningColor, _warningFillColor);
        root.SetActive(false);
        return warn;
    }

    // Explosiones recicladas: antes se instanciaba y destruía una por bala
    private int CreateVfx()
    {
        var go = Instantiate(_impactVfx);
        go.SetActive(false);
        _vfxObjects.Add(go);
        _vfxPool.Add(go.GetComponentsInChildren<ParticleSystem>(true));
        _vfxFreeAt.Add(0f);
        return _vfxObjects.Count - 1;
    }

    private void PlayVfx(Vector3 point)
    {
        if (_impactVfx == null) return;

        int index = -1;
        for (int i = 0; i < _vfxObjects.Count; i++)
            if (_vfxObjects[i] != null && Time.time >= _vfxFreeAt[i]) { index = i; break; }
        if (index < 0) return; // todas ocupadas: esta bala cae sin explosión, solo con la espuma

        var go = _vfxObjects[index];
        go.SetActive(false);
        go.transform.SetPositionAndRotation(point, Quaternion.identity);
        go.SetActive(true);
        foreach (var ps in _vfxPool[index])
        {
            ps.Clear(true);
            ps.Play(true);
        }
        _vfxFreeAt[index] = Time.time + _impactVfxLifetime;
    }

    private TowerShell CreateShell()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "MenuMortarShell";
        var rend = go.GetComponent<Renderer>();
        if (_shellMat == null)
        {
            _shellMat = new Material(rend.sharedMaterial);
            if (_shellMat.HasProperty("_BaseColor")) _shellMat.SetColor("_BaseColor", _shellColor);
            if (_shellMat.HasProperty("_Color")) _shellMat.SetColor("_Color", _shellColor);
        }
        rend.sharedMaterial = _shellMat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var trail = go.AddComponent<TrailRenderer>();
        if (_trailMat == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _trailMat = new Material(sh);
        }
        trail.sharedMaterial = _trailMat;
        trail.time = 0.35f;
        trail.minVertexDistance = 0.1f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 0f));
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(_trailColor, 0f), new GradientColorKey(new Color(0.35f, 0.35f, 0.38f), 1f) },
                  new[] { new GradientAlphaKey(_trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        // la bala no necesita física
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var shell = go.AddComponent<TowerShell>();
        go.SetActive(false);
        return shell;
    }

    private void OnDestroy()
    {
        if (_shellMat != null) Destroy(_shellMat);
        if (_trailMat != null) Destroy(_trailMat);
    }
}
