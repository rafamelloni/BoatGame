using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// UI de prueba con botones reales, generada por codigo (no necesita prefabs
// armados a mano). Arranca con plata "infinita" (ver _startingCurrency) para
// poder clickear todo sin trabarse mientras no este la economia real.
//
// Uso: poner este script en un GameObject vacio de la escena, asignarle
// el RT_SkillTree y los arrays de nodos de prueba. Si no le asignas Canvas
// ni contenedores, los crea solo al arrancar.
public class SkillTreeUI : MonoBehaviour
{
    [Header("Arbol")]
    [SerializeField] private RT_SkillTree _tree;

    [Header("Nodos de prueba")]
    [SerializeField] private SO_NormalNode[] _normalNodes;
    [SerializeField] private SO_SpecialNode[] _specialNodes;

    [Header("Moneda (testing)")]
    [SerializeField] private int _startingCurrency = 999999;

    [Header("UI (opcional, se genera sola si falta)")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _normalContainer;
    [SerializeField] private Transform _specialContainer;
    [SerializeField] private Text _currencyLabel;

    private readonly Dictionary<SO_NormalNode, Button> _normalButtons = new();
    private readonly Dictionary<SO_NormalNode, Text> _normalLabels = new();
    private readonly Dictionary<SO_SpecialNode, Button> _specialButtons = new();
    private readonly Dictionary<SO_SpecialNode, Text> _specialLabels = new();

    private void Awake()
    {
        EnsureUI();
        BuildButtons();
        _tree.SetCurrency(_startingCurrency);
    }

    private void OnEnable()
    {
        _tree.OnNormalNodeLeveled += HandleNormalLeveled;
        _tree.OnSpecialNodeUnlocked += HandleSpecialUnlocked;
        _tree.OnCurrencyChanged += HandleCurrencyChanged;
    }

    private void OnDisable()
    {
        _tree.OnNormalNodeLeveled -= HandleNormalLeveled;
        _tree.OnSpecialNodeUnlocked -= HandleSpecialUnlocked;
        _tree.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void HandleNormalLeveled(SO_NormalNode node, int level) => RefreshAll();
    private void HandleSpecialUnlocked(SO_SpecialNode special) => RefreshAll();
    private void HandleCurrencyChanged(int amount) => RefreshAll();

    // ---------- Construccion de la UI ----------

    private void EnsureUI()
    {
        if (_canvas == null)
        {
            var canvasGO = new GameObject("SkillTreeCanvas_AUTO");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem_AUTO");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        if (_currencyLabel == null)
            _currencyLabel = CreateLabel(_canvas.transform, "CurrencyLabel_AUTO", new Vector2(20f, -20f), "Plata: 0");

        if (_normalContainer == null)
            _normalContainer = CreateContainer(_canvas.transform, "NormalNodes_AUTO", new Vector2(20f, -60f));

        if (_specialContainer == null)
            _specialContainer = CreateContainer(_canvas.transform, "SpecialNodes_AUTO", new Vector2(320f, -60f));
    }

    private Transform CreateContainer(Transform parent, string name, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(280f, 400f);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return go.transform;
    }

    private Text CreateLabel(Transform parent, string name, Vector2 anchoredPos, string text)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(280f, 30f);

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.color = Color.white;
        return txt;
    }

    private void BuildButtons()
    {
        if (_normalNodes != null)
        {
            foreach (var node in _normalNodes)
            {
                if (node == null) continue;
                CreateNormalButton(node);
            }
        }

        if (_specialNodes != null)
        {
            foreach (var special in _specialNodes)
            {
                if (special == null) continue;
                CreateSpecialButton(special);
            }
        }

        RefreshAll();
    }

    private void CreateNormalButton(SO_NormalNode node)
    {
        var go = new GameObject($"Btn_{node.displayName}", typeof(RectTransform));
        go.transform.SetParent(_normalContainer, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 44f);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f);

        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            bool ok = _tree.TryLevelUp(node);
            if (!ok)
                Debug.Log($"[SkillTreeUI] No se pudo subir {node.displayName} (maximo o sin plata)");
        });

        var label = CreateChildLabel(go.transform);

        _normalButtons[node] = button;
        _normalLabels[node] = label;
    }

    private void CreateSpecialButton(SO_SpecialNode special)
    {
        var go = new GameObject($"Btn_{special.displayName}", typeof(RectTransform));
        go.transform.SetParent(_specialContainer, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 44f);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.5f, 0.1f, 0.1f);

        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            bool ok = _tree.UnlockSpecial(special);
            if (!ok)
                Debug.Log($"[SkillTreeUI] {special.displayName} todavia no cumple los requisitos");
        });

        var label = CreateChildLabel(go.transform);

        _specialButtons[special] = button;
        _specialLabels[special] = label;
    }

    private Text CreateChildLabel(Transform parent)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 15;
        txt.color = Color.white;
        return txt;
    }

    // ---------- Refresco ----------

    private void RefreshAll()
    {
        if (_currencyLabel != null)
            _currencyLabel.text = $"Plata: {_tree.Currency}";

        foreach (var kvp in _normalLabels)
        {
            SO_NormalNode node = kvp.Key;
            int level = _tree.GetLevel(node);
            kvp.Value.text = $"{node.displayName}\n{level}/{node.maxLevel}  (costo {node.cost})";
            _normalButtons[node].interactable = _tree.CanLevelUp(node) && _tree.CanAfford(node);
        }

        foreach (var kvp in _specialLabels)
        {
            SO_SpecialNode special = kvp.Key;
            bool unlocked = _tree.IsSpecialUnlocked(special);
            bool unlockable = _tree.IsSpecialUnlockable(special);
            kvp.Value.text = unlocked
                ? $"{special.displayName}\nDESBLOQUEADO"
                : $"{special.displayName}\n{(unlockable ? "listo!" : "bloqueado")}";
            _specialButtons[special].interactable = !unlocked && unlockable;
        }
    }
}
