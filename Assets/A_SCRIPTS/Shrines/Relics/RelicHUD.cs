using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Muestra en pantalla las reliquias que vas juntando.
// Cada slot tiene la imagen de la reliquia que se llena de abajo hacia arriba (1/3, 2/3, 3/3)
// y 3 puntitos de progreso. Al conseguir un fragmento, la imagen vuela desde el centro de la
// pantalla hasta su slot (espera a que se cierre el panel del cofre).
// Pasando el mouse por encima de una reliquia se abre un cartel con qué es y qué hace.
// Cada reliquia tiene un idle propio (flota y se mece distinto).
// Se crea desde el menú: Tools > Botes > Crear HUD de reliquias.
public class RelicHUD : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public SO_RelicData relic;
        public RectTransform root;
        [Tooltip("La imagen apagada de fondo (silueta)")]
        public Image silhouette;
        [Tooltip("La imagen a color. Image Type = Filled, Vertical, Bottom")]
        public Image fill;
        public Image[] pips;
    }

    [SerializeField] private List<Slot> _slots = new List<Slot>();

    [Header("Comportamiento")]
    [Tooltip("El slot aparece recién cuando conseguís el primer fragmento de esa reliquia")]
    [SerializeField] private bool _hideUntilFirstFragment = true;
    [SerializeField] private Color _pipOn = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color _pipOff = new Color(0f, 0f, 0f, 0.45f);

    [Header("Idle")]
    [Tooltip("Cuánto sube y baja, en píxeles")]
    [SerializeField] private float _idleBob = 6f;
    [Tooltip("Cuánto se mueve a los costados, en píxeles")]
    [SerializeField] private float _idleSway = 3f;
    [Tooltip("Cuánto se inclina, en grados")]
    [SerializeField] private float _idleTilt = 6f;
    [SerializeField] private float _idleSpeed = 1f;

    [Header("Cartel al pasar el mouse")]
    [SerializeField] private Color _tooltipBg = new Color(0.08f, 0.09f, 0.16f, 0.94f);
    [SerializeField] private Color _tooltipBorder = new Color(0.85f, 0.65f, 0.3f, 1f);
    [SerializeField] private float _tooltipWidth = 340f;

    [Header("Vuelo al slot")]
    [SerializeField] private float _flySize = 220f;
    [SerializeField] private float _popTime = 0.35f;
    [SerializeField] private float _holdTime = 0.35f;
    [SerializeField] private float _flyTime = 0.5f;

    private RelicInventory _inventory;
    private readonly Queue<(SO_RelicData relic, int count)> _pending = new Queue<(SO_RelicData, int)>();
    private bool _playing;

    // idle
    private struct IdleParams { public float phase, bobFreq, swayFreq, tiltFreq, bobMul, swayMul, tiltMul; }
    private IdleParams[] _idle;
    private Vector2[] _baseFill, _baseSil;

    // cartel
    private RectTransform _tooltip;
    private TextMeshProUGUI _ttTitle, _ttDesc, _ttProgress;
    private int _hovered = -1;
    private float _ttAnim;

    private void Start()
    {
        SetupSlots();
        _inventory = RelicInventory.Instance != null ? RelicInventory.Instance : FindFirstObjectByType<RelicInventory>();
        if (_inventory != null)
        {
            _inventory.OnFragmentAdded += OnFragmentAdded;
            _inventory.OnReset += OnInventoryReset;
        }
        else Debug.LogWarning("[RelicHUD] No hay RelicInventory en la escena.");
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnFragmentAdded -= OnFragmentAdded;
            _inventory.OnReset -= OnInventoryReset;
        }
    }

    private void OnInventoryReset()
    {
        _pending.Clear();
        _hovered = -1;
        RefreshAll();
    }

    private void OnFragmentAdded(SO_RelicData relic, int count)
    {
        _pending.Enqueue((relic, count));
    }

    private void Update()
    {
        UpdateIdle();
        UpdateTooltip();

        if (_playing || _pending.Count == 0) return;
        // esperar a que se cierre el panel del cofre para que se vea el vuelo
        var panel = ShrineRewardUI.Instance;
        if (panel != null && panel.IsOpen) return;

        var next = _pending.Dequeue();
        StartCoroutine(PlayGain(next.relic, next.count));
    }

    public void RefreshAll()
    {
        foreach (var s in _slots)
        {
            if (s == null || s.root == null) continue;
            int count = _inventory != null && s.relic != null ? _inventory.GetCount(s.relic) : 0;
            Apply(s, count);
            SetVisible(s, !_hideUntilFirstFragment || count > 0);
        }
    }

    private void Apply(Slot s, int count)
    {
        float k = Mathf.Clamp01(count / (float)RelicInventory.FragmentsToComplete);
        if (s.relic != null && s.relic.icon != null)
        {
            if (s.fill != null) s.fill.sprite = s.relic.icon;
            if (s.silhouette != null) s.silhouette.sprite = s.relic.icon;
        }
        if (s.fill != null) s.fill.fillAmount = k;
        if (s.pips != null)
            for (int i = 0; i < s.pips.Length; i++)
                if (s.pips[i] != null) s.pips[i].color = i < count ? _pipOn : _pipOff;
    }

    // Oculta con alpha (no con SetActive) para que cada slot mantenga su lugar fijo en pantalla
    private static void SetVisible(Slot s, bool visible)
    {
        var cg = s.root.GetComponent<CanvasGroup>();
        if (cg == null) cg = s.root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible; // solo se puede hoverear si está visible
        cg.interactable = false;
    }

    private static bool IsHidden(Slot s)
    {
        var cg = s.root.GetComponent<CanvasGroup>();
        return cg != null && cg.alpha <= 0.01f;
    }

    // ------------------------------------------------------------ setup

    private void SetupSlots()
    {
        _idle = new IdleParams[_slots.Count];
        _baseFill = new Vector2[_slots.Count];
        _baseSil = new Vector2[_slots.Count];

        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s == null || s.root == null) continue;

            // sin fondo negro, pero que siga recibiendo el mouse
            var bg = s.root.GetComponent<Image>();
            if (bg == null) bg = s.root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;

            var hover = s.root.GetComponent<RelicSlotHover>();
            if (hover == null) hover = s.root.gameObject.AddComponent<RelicSlotHover>();
            hover.hud = this;
            hover.index = i;

            if (s.fill != null) _baseFill[i] = s.fill.rectTransform.anchoredPosition;
            if (s.silhouette != null) _baseSil[i] = s.silhouette.rectTransform.anchoredPosition;

            // cada reliquia con su propio ritmo (determinístico por índice)
            var r = new System.Random(1234 + i * 977);
            _idle[i] = new IdleParams
            {
                phase = (float)r.NextDouble() * 10f,
                bobFreq = 1.6f + (float)r.NextDouble() * 0.9f,
                swayFreq = 0.7f + (float)r.NextDouble() * 0.6f,
                tiltFreq = 1.1f + (float)r.NextDouble() * 0.8f,
                bobMul = 0.75f + (float)r.NextDouble() * 0.5f,
                swayMul = 0.6f + (float)r.NextDouble() * 0.8f,
                tiltMul = (r.NextDouble() < 0.5 ? -1f : 1f) * (0.6f + (float)r.NextDouble() * 0.7f)
            };
        }
    }

    private void UpdateIdle()
    {
        if (_idle == null) return;
        float t = Time.unscaledTime * _idleSpeed;
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s == null || s.root == null) continue;
            var p = _idle[i];
            float tt = t + p.phase;
            Vector2 off = new Vector2(
                Mathf.Sin(tt * p.swayFreq) * _idleSway * p.swayMul,
                Mathf.Sin(tt * p.bobFreq) * _idleBob * p.bobMul);
            float tilt = Mathf.Sin(tt * p.tiltFreq + 1.3f) * _idleTilt * p.tiltMul;
            var rot = Quaternion.Euler(0f, 0f, tilt);

            if (s.fill != null)
            {
                s.fill.rectTransform.anchoredPosition = _baseFill[i] + off;
                s.fill.rectTransform.localRotation = rot;
            }
            if (s.silhouette != null)
            {
                s.silhouette.rectTransform.anchoredPosition = _baseSil[i] + off;
                s.silhouette.rectTransform.localRotation = rot;
            }
        }
    }

    // ------------------------------------------------------------ cartel

    public void OnSlotHover(int index, bool enter)
    {
        if (enter)
        {
            var s = _slots[index];
            if (s == null || s.relic == null || IsHidden(s)) return;
            _hovered = index;
            ShowTooltip(s);
        }
        else if (_hovered == index)
        {
            _hovered = -1;
        }
    }

    private void ShowTooltip(Slot s)
    {
        if (_tooltip == null) BuildTooltip();
        int count = _inventory != null ? _inventory.GetCount(s.relic) : 0;
        int need = RelicInventory.FragmentsToComplete;

        _ttTitle.text = $"<color=#{ColorUtility.ToHtmlStringRGB(s.relic.color)}>{s.relic.displayName}</color>";
        string desc = string.IsNullOrEmpty(s.relic.description) ? "" : $"<i>{s.relic.description}</i>\n\n";
        string fx = RelicEffects.Instance != null ? RelicEffects.Instance.DescribeAll(s.relic, count) : "";
        _ttDesc.text = desc + fx;
        _ttProgress.text = count >= need ? "Completa" : $"Fragmentos: {count}/{need}";

        _tooltip.gameObject.SetActive(true);
        _tooltip.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltip);

        // debajo del slot, alineado a su borde derecho (el HUD está arriba a la derecha)
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        Vector3[] corners = new Vector3[4];
        s.root.GetWorldCorners(corners); // 0 abajo-izq, 3 abajo-der
        _tooltip.position = corners[3] + Vector3.down * 26f * canvas.scaleFactor;

        // que no se salga de la pantalla por la izquierda
        _tooltip.GetWorldCorners(corners);
        if (corners[0].x < 8f) _tooltip.position += Vector3.right * (8f - corners[0].x);

        _ttAnim = 0f;
    }

    private void UpdateTooltip()
    {
        if (_tooltip == null) return;
        if (_hovered < 0)
        {
            if (_tooltip.gameObject.activeSelf) _tooltip.gameObject.SetActive(false);
            return;
        }
        // pop al aparecer
        _ttAnim = Mathf.Min(1f, _ttAnim + Time.unscaledDeltaTime / 0.15f);
        _tooltip.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(_ttAnim));
    }

    private void BuildTooltip()
    {
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var go = new GameObject("RelicTooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        _tooltip = (RectTransform)go.transform;
        _tooltip.SetParent(canvas.transform, false);
        _tooltip.pivot = new Vector2(1f, 1f);
        _tooltip.anchorMin = _tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        _tooltip.sizeDelta = new Vector2(_tooltipWidth, 0f);

        var bg = go.GetComponent<Image>();
        bg.color = _tooltipBg;
        bg.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = _tooltipBorder;
        outline.effectDistance = new Vector2(3f, -3f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _ttTitle = MakeTooltipText("Title", 30f, FontStyles.Bold);
        _ttDesc = MakeTooltipText("Description", 22f, FontStyles.Normal);
        _ttProgress = MakeTooltipText("Progress", 20f, FontStyles.Italic);
        _ttProgress.color = _tooltipBorder;

        go.SetActive(false);
    }

    private TextMeshProUGUI MakeTooltipText(string n, float size, FontStyles style)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(_tooltip, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        return t;
    }

    // ------------------------------------------------------------ util

    private Slot Find(SO_RelicData relic)
    {
        foreach (var s in _slots) if (s != null && s.relic == relic) return s;
        return null;
    }

    private IEnumerator PlayGain(SO_RelicData relic, int count)
    {
        _playing = true;
        var slot = Find(relic);
        if (slot == null || slot.root == null)
        {
            Debug.LogWarning($"[RelicHUD] No hay slot para '{(relic != null ? relic.name : "null")}'.");
            _playing = false;
            yield break;
        }

        bool wasHidden = IsHidden(slot);
        if (wasHidden) Apply(slot, count - 1); // arranca vacío y se llena al llegar

        // imagen que vuela desde el centro de la pantalla
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var flyGo = new GameObject("RelicFly", typeof(RectTransform), typeof(Image));
        var fly = (RectTransform)flyGo.transform;
        fly.SetParent(canvas.transform, false);
        fly.SetAsLastSibling();
        var img = flyGo.GetComponent<Image>();
        img.sprite = relic.icon;
        img.preserveAspect = true;
        img.raycastTarget = false;
        fly.anchorMin = fly.anchorMax = new Vector2(0.5f, 0.5f);
        fly.anchoredPosition = Vector2.zero;
        fly.sizeDelta = Vector2.one * _flySize;

        // pop
        yield return Tween(_popTime, k =>
        {
            float s = Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(k));
            fly.localScale = Vector3.one * s;
            fly.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -25f);
        });
        yield return Wait(_holdTime);

        // vuelo al slot (en curva)
        Vector3 from = fly.position;
        Vector3 fromScale = fly.localScale;
        Vector2 fromSize = fly.sizeDelta;
        Vector2 toSize = slot.root.rect.size;
        yield return Tween(_flyTime, k =>
        {
            float e = k * k * (3f - 2f * k);
            Vector3 to = slot.root.position;
            Vector3 mid = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 120f * canvas.scaleFactor;
            Vector3 a = Vector3.Lerp(from, mid, e), b = Vector3.Lerp(mid, to, e);
            fly.position = Vector3.Lerp(a, b, e);
            fly.sizeDelta = Vector2.Lerp(fromSize, toSize, e);
            fly.localScale = Vector3.Lerp(fromScale, Vector3.one, e);
            fly.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * 20f);
        });
        Destroy(flyGo);

        // llega: aparece el slot, se llena y salta
        SetVisible(slot, true);
        Apply(slot, count);
        bool complete = count >= RelicInventory.FragmentsToComplete;
        float punch = complete ? 1.45f : 1.25f;
        yield return Tween(complete ? 0.45f : 0.3f, k =>
        {
            float p = Mathf.Sin(k * Mathf.PI);
            slot.root.localScale = Vector3.one * (1f + (punch - 1f) * p);
            slot.root.localRotation = Quaternion.Euler(0f, 0f, complete ? Mathf.Sin(k * Mathf.PI * 4f) * 12f * (1f - k) : 0f);
        });
        slot.root.localScale = Vector3.one;
        slot.root.localRotation = Quaternion.identity;

        _playing = false;
    }

    private static float EaseOutBack(float k) => 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);

    private static IEnumerator Wait(float t)
    {
        float e = 0f;
        while (e < t) { e += Time.unscaledDeltaTime; yield return null; }
    }

    private static IEnumerator Tween(float duration, Action<float> step)
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

#if UNITY_EDITOR
    // Lo usa el menú del Editor para armar los slots
    public void EditorSetSlots(List<Slot> slots) => _slots = slots;
#endif
}
