using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Panel que aparece al completar un templo (estilo "Treasure Found!" de Vampire Survivors),
// pero con el cofre en 3D: una cámara aparte lo renderiza a una RenderTexture que se muestra en el panel.
//  - Pausa el juego mientras está abierto
//  - ABRIR: el cofre salta/gira/se abre, da una reliquia al azar según rareza (RelicInventory) y la muestra
//    (el borde del panel se pinta del color de la rareza)
//  - CONTINUAR: cierra y reanuda
// El panel se crea en tu Canvas desde el menú: Tools > Botes > Crear panel de templo
// (queda en la escena como UI normal para que lo retoques con tus PNGs).
// Si no hay panel en la escena, se arma uno solo en runtime como respaldo.
// Para usar tu cofre: asigná el prefab en "Chest Prefab".
public class ShrineRewardUI : MonoBehaviour
{
    public static ShrineRewardUI Instance { get; private set; }

    [Header("Cofre 3D")]
    [Tooltip("Tu cofre. Si queda vacío se usa un cubo.")]
    [SerializeField] private GameObject _chestPrefab;
    [SerializeField] private Vector3 _chestOffset = Vector3.zero;
    [SerializeField] private float _chestScale = 1f;
    [Tooltip("Se llama cuando termina el salto, justo antes de mostrar el fragmento (ej. abrir la tapa)")]
    [SerializeField] private UnityEvent _onChestOpened;

    [Header("Textos")]
    [Tooltip("Solo se usa al crear el panel; después editá el texto directo en la escena")]
    [SerializeField] private string _title = "¡Templo completado!";
    [SerializeField] private string _openText = "ABRIR";
    [SerializeField] private string _continueText = "CONTINUAR";

    [Header("Colores (después lo reemplazás con tus PNGs)")]
    [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color _panelColor = new Color(0.27f, 0.29f, 0.48f, 1f);
    [SerializeField] private Color _borderColor = new Color(0.85f, 0.65f, 0.3f, 1f);
    [SerializeField] private Color _buttonColor = new Color(0.2f, 0.25f, 0.85f, 1f);

    [Header("Render 3D")]
    [SerializeField] private int _renderSize = 512;
    [Tooltip("Lugar del mundo donde se arma el cofre de preview (lejos de todo)")]
    [SerializeField] private Vector3 _previewWorldPos = new Vector3(0f, -5000f, 0f);

    [Header("Referencias del panel (las llena Tools > Botes > Crear panel de templo)")]
    [Tooltip("Objeto que se prende/apaga (contiene el fondo oscuro y el panel)")]
    [SerializeField] private GameObject _root;
    [Tooltip("Lo que hace el pop-in de escala")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private RawImage _chestImage;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _fragmentText;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _buttonLabel;
    [Tooltip("Opcionales: flechitas que se mueven al costado del botón")]
    [SerializeField] private RectTransform _arrowL, _arrowR;

    public event System.Action OnClosed;
    public bool IsOpen => _isOpen;

    private Vector2 _arrowLBase, _arrowRBase;
    private Image _borderImage;
    private Color _borderBaseColor;

    // 3D
    private GameObject _rig;
    private Camera _previewCam;
    private RenderTexture _rt;
    private Transform _pivot;
    private Transform _chest;

    private float _prevTimeScale = 1f;
    private bool _isOpen;
    private bool _opened;
    private Coroutine _anim;

    // ---------------------------------------------------------------- API

    public static void ShowFor(ChargeShrine shrine)
    {
        if (Instance == null)
        {
            var existing = FindFirstObjectByType<ShrineRewardUI>();
            if (existing != null) Instance = existing;
            else new GameObject("ShrineRewardUI").AddComponent<ShrineRewardUI>();
        }
        Instance.Show();
    }

    public void Show()
    {
        Build();
        if (_isOpen) return;
        _isOpen = true;
        _opened = false;

        _prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        _fragmentText.gameObject.SetActive(false);
        _progressText.gameObject.SetActive(false);
        if (_borderImage != null) _borderImage.color = _borderBaseColor;
        _buttonLabel.text = _openText;
        _button.interactable = true;

        _chest.localPosition = _chestOffset;
        _chest.localRotation = Quaternion.identity;
        _chest.localScale = Vector3.one * _chestScale;

        _rig.SetActive(true);
        _root.SetActive(true);
        _root.transform.SetAsLastSibling(); // arriba del resto de la UI
        EnsureEventSystem();

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(PopIn());
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        if (_anim != null) StopCoroutine(_anim);
        _root.SetActive(false);
        _rig.SetActive(false);
        Time.timeScale = _prevTimeScale;
        OnClosed?.Invoke();
    }

    // ---------------------------------------------------------------- vida

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        // el panel de la escena puede quedar prendido mientras lo editás: al arrancar se oculta
        if (_root != null) _root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_rt != null) { _rt.Release(); Destroy(_rt); }
        if (_rig != null) Destroy(_rig);
    }

    private void Start()
    {
        if (!_isOpen && _root != null) _root.SetActive(false);
    }

    private void LateUpdate()
    {
        // si algún otro script prende todo el Canvas, el panel igual queda oculto hasta que corresponda
        if (!_isOpen && _root != null && _root.activeSelf) _root.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen) return;
        float t = Time.unscaledTime;

        // cofre flotando y girando despacio mientras esperás
        if (!_opened && _anim == null)
        {
            _chest.localPosition = _chestOffset + Vector3.up * (Mathf.Sin(t * 2f) * 0.06f);
            _chest.Rotate(Vector3.up, 35f * Time.unscaledDeltaTime, Space.World);
        }

        // flechitas a los costados del botón
        float s = Mathf.Sin(t * 6f) * 6f;
        if (_arrowL != null) _arrowL.anchoredPosition = _arrowLBase + new Vector2(s, 0f);
        if (_arrowR != null) _arrowR.anchoredPosition = _arrowRBase - new Vector2(s, 0f);
    }

    private void OnButton()
    {
        if (!_opened)
        {
            _button.interactable = false;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(OpenSequence());
        }
        else Close();
    }

    // ---------------------------------------------------------------- animaciones (tiempo sin escala)

    private IEnumerator PopIn()
    {
        float d = 0.25f, e = 0f;
        while (e < d)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / d);
            float back = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f); // easeOutBack
            _panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, back);
            yield return null;
        }
        _panel.localScale = Vector3.one;
        _anim = null;
    }

    private IEnumerator OpenSequence()
    {
        Vector3 basePos = _chestOffset;
        Vector3 baseScale = Vector3.one * _chestScale;
        Quaternion startRot = _chest.localRotation;

        // 1) se agacha (anticipación)
        yield return Tween(0.15f, k =>
        {
            float sq = Mathf.Sin(k * Mathf.PI * 0.5f);
            _chest.localScale = Vector3.Scale(baseScale, new Vector3(1f + 0.25f * sq, 1f - 0.3f * sq, 1f + 0.25f * sq));
        });

        // 2) salta girando
        yield return Tween(0.45f, k =>
        {
            float h = 4f * k * (1f - k); // parábola 0 -> 1 -> 0
            _chest.localPosition = basePos + Vector3.up * (h * 0.9f);
            _chest.localRotation = startRot * Quaternion.Euler(0f, 540f * EaseOut(k), 0f);
            float stretch = 1f - Mathf.Abs(0.5f - k) * 2f; // estirado en el aire
            _chest.localScale = Vector3.Scale(baseScale, new Vector3(1f - 0.12f * stretch, 1f + 0.18f * stretch, 1f - 0.12f * stretch));
        });

        // 3) cae y rebota
        yield return Tween(0.12f, k =>
        {
            float sq = Mathf.Sin(k * Mathf.PI);
            _chest.localScale = Vector3.Scale(baseScale, new Vector3(1f + 0.3f * sq, 1f - 0.35f * sq, 1f + 0.3f * sq));
        });
        _chest.localScale = baseScale;
        _chest.localPosition = basePos;

        _onChestOpened?.Invoke();

        // 4) reliquia
        GiveRelic();
        _opened = true;

        _fragmentText.gameObject.SetActive(true);
        _progressText.gameObject.SetActive(true);
        yield return Tween(0.25f, k =>
        {
            float s = Mathf.LerpUnclamped(0.5f, 1f, 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f));
            _fragmentText.rectTransform.localScale = Vector3.one * s;
            _progressText.rectTransform.localScale = Vector3.one * s;
        });

        _buttonLabel.text = _continueText;
        _button.interactable = true;
        _anim = null;
    }

    private void GiveRelic()
    {
        var inv = RelicInventory.Instance != null ? RelicInventory.Instance : FindFirstObjectByType<RelicInventory>();
        if (inv != null && inv.TryGiveRandom(out SO_RelicData relic, out int count))
        {
            Color rc = SO_RelicData.RarityColor(relic.rarity);
            string rarityHex = ColorUtility.ToHtmlStringRGB(rc);
            string nameHex = ColorUtility.ToHtmlStringRGB(relic.NameColor);
            string stack = count > 1 ? $" <size=70%>x{count}</size>" : "";
            _fragmentText.text = $"<size=60%><color=#{rarityHex}>{SO_RelicData.RarityLabel(relic.rarity).ToUpper()}</color></size>\n<color=#{nameHex}>{relic.displayName}</color>{stack}";
            string fx = RelicEffects.Instance != null ? RelicEffects.Instance.Describe(relic, count) : "";
            _progressText.text = $"<size=75%>{fx}</size>";
            if (_borderImage != null) _borderImage.color = rc;
        }
        else
        {
            _fragmentText.text = inv == null ? "Reliquia\n(falta RelicInventory en la escena)" : "Ya tenés\ntodas las reliquias";
            _progressText.text = "";
        }
    }

    private static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

    private static IEnumerator Tween(float duration, System.Action<float> step)
    {
        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            step(Mathf.Clamp01(e / duration));
            yield return null;
        }
        step(1f);
    }

    // ---------------------------------------------------------------- construcción

    private bool _built;

    private void Build()
    {
        if (_built) return;
        _built = true;

        // respaldo: si no se creó el panel desde el menú, se arma uno en un Canvas propio
        if (_root == null)
        {
            var canvasGo = new GameObject("ShrineRewardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            BuildPanelInto((RectTransform)canvasGo.transform);
        }

        Build3D();
        _chestImage.texture = _rt;

        _button.onClick.RemoveListener(OnButton);
        _button.onClick.AddListener(OnButton);
        if (_arrowL != null) _arrowLBase = _arrowL.anchoredPosition;
        if (_arrowR != null) _arrowRBase = _arrowR.anchoredPosition;
        _borderImage = _panel != null ? _panel.GetComponent<Image>() : null;
        if (_borderImage != null) _borderBaseColor = _borderImage.color;
        // el texto de lo que hace la reliquia puede ocupar 2 renglones
        #pragma warning disable 0618
        _progressText.enableWordWrapping = true;
#pragma warning restore 0618
        _progressText.rectTransform.sizeDelta = new Vector2(_progressText.rectTransform.sizeDelta.x, Mathf.Max(_progressText.rectTransform.sizeDelta.y, 60f));
        _root.SetActive(false);
    }

    private void Build3D()
    {
        _rt = new RenderTexture(_renderSize, _renderSize, 24, RenderTextureFormat.ARGB32) { name = "ShrineChestRT", antiAliasing = 4 };
        _rt.Create();

        _rig = new GameObject("ShrineRewardPreview");
        _rig.transform.position = _previewWorldPos;

        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(_rig.transform, false);

        if (_chestPrefab != null)
        {
            _chest = Instantiate(_chestPrefab, _pivot).transform;
        }
        else
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<Collider>());
            cube.name = "ChestPlaceholder";
            cube.transform.SetParent(_pivot, false);
            var r = cube.GetComponent<Renderer>();
            r.material.color = new Color(0.85f, 0.55f, 0.2f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _chest = cube.transform;
        }
        foreach (var col in _chest.GetComponentsInChildren<Collider>()) col.enabled = false;

        var camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(_rig.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.4f, -3.4f);
        camGo.transform.LookAt(_rig.transform.position + Vector3.up * 0.2f);
        _previewCam = camGo.AddComponent<Camera>();
        _previewCam.clearFlags = CameraClearFlags.SolidColor;
        _previewCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _previewCam.fieldOfView = 30f;
        _previewCam.nearClipPlane = 0.1f;
        _previewCam.farClipPlane = 30f;
        _previewCam.targetTexture = _rt;
        _previewCam.depth = -50;

        _rig.SetActive(false);
    }

    // Arma la jerarquía del panel adentro de "parent" (un Canvas o un hijo de uno) y llena las referencias.
    // La usa el menú del Editor y también el respaldo en runtime. Devuelve el objeto raíz.
    public GameObject BuildPanelInto(RectTransform parent)
    {
        var rootGo = new GameObject("ShrineRewardPanel", typeof(RectTransform));
        rootGo.transform.SetParent(parent, false);
        var root = (RectTransform)rootGo.transform;
        Stretch(root);
        _root = rootGo;

        // fondo oscurecido (bloquea clicks al juego)
        var dim = MakeImage("Dim", root, _dimColor);
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);

        // panel con borde
        var border = MakeImage("Panel", root, _borderColor);
        _panel = border.rectTransform;
        _panel.sizeDelta = new Vector2(560f, 700f);
        var inner = MakeImage("Inner", _panel, _panelColor);
        Stretch(inner.rectTransform, 10f);

        _titleText = MakeText("Title", _panel, _title, 46f, new Vector2(0f, 290f), new Vector2(520f, 80f));

        // cofre 3D (la RenderTexture se asigna en runtime)
        var imgGo = new GameObject("Chest3D", typeof(RectTransform), typeof(RawImage));
        imgGo.transform.SetParent(_panel, false);
        _chestImage = imgGo.GetComponent<RawImage>();
        _chestImage.raycastTarget = false;
        var ir = _chestImage.rectTransform;
        ir.sizeDelta = new Vector2(400f, 400f);
        ir.anchoredPosition = new Vector2(0f, 40f);

        _fragmentText = MakeText("Fragment", _panel, "RARE\nReliquia", 36f, new Vector2(0f, -165f), new Vector2(520f, 100f));
        _progressText = MakeText("Progress", _panel, "Lo que hace", 30f, new Vector2(0f, -232f), new Vector2(500f, 60f));

        // botón
        var btnImg = MakeImage("Button", _panel, _buttonColor);
        btnImg.raycastTarget = true;
        var br = btnImg.rectTransform;
        br.sizeDelta = new Vector2(260f, 76f);
        br.anchoredPosition = new Vector2(0f, -300f);
        var outline = btnImg.gameObject.AddComponent<Outline>();
        outline.effectColor = _borderColor;
        outline.effectDistance = new Vector2(4f, -4f);
        _button = btnImg.gameObject.AddComponent<Button>();
        _button.targetGraphic = btnImg;
        _buttonLabel = MakeText("Label", br, _openText, 40f, Vector2.zero, br.sizeDelta);

        var al = MakeText("ArrowL", _panel, ">", 36f, new Vector2(-175f, -300f), new Vector2(40f, 40f));
        var ar = MakeText("ArrowR", _panel, "<", 36f, new Vector2(175f, -300f), new Vector2(40f, 40f));
        al.color = _borderColor;
        ar.color = _borderColor;
        _arrowL = al.rectTransform;
        _arrowR = ar.rectTransform;

        return rootGo;
    }

    private static Image MakeImage(string n, Transform parent, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string n, Transform parent, string text, float size, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        var r = t.rectTransform;
        r.sizeDelta = box;
        r.anchoredPosition = pos;
        return t;
    }

    private static void Stretch(RectTransform r, float inset = 0f)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
