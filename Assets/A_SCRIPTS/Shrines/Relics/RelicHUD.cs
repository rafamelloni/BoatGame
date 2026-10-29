using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Muestra en pantalla las reliquias que vas consiguiendo, en una columna a la izquierda.
// Van en el orden en que las conseguiste: la primera arriba y las nuevas abajo.
// Si no entran, arma otra columna a la derecha.
// Cada slot tiene un círculo del color de la rareza, el ícono y "x2", "x3"... si la apilaste.
// Al conseguir una, la imagen vuela desde el centro de la pantalla hasta su slot
// (espera a que se cierre el panel del cofre).
// Pasando el mouse por encima se abre un cartel con qué es y qué hace.
// Los slots se crean en runtime: el objeto solo necesita estar en el Canvas (se ubica solo a la izquierda).
// Se crea desde el menú: Tools > Botes > Crear HUD de reliquias.
public class RelicHUD : MonoBehaviour
{
    private class Slot
    {
        public SO_RelicData relic;
        public RectTransform root;
        public Image disc, ring, icon;
        public TextMeshProUGUI countText;
        public CanvasGroup group;
        public Vector2 iconBase;
        public Vector2 target;
        public float phase, bobFreq, swayFreq, tiltFreq, bobMul, swayMul, tiltMul;
    }

    [Header("Posición")]
    [Tooltip("Al arrancar ubica el HUD a la izquierda de la pantalla (destildalo si lo querés mover a mano)")]
    [SerializeField] private bool _anchorLeft = true;
    [Tooltip("Distancia al borde izquierdo (x) y al borde de arriba (y)")]
    [SerializeField] private Vector2 _screenMargin = new Vector2(30f, 140f);

    [Header("Columna")]
    [SerializeField] private float _slotSize = 72f;
    [SerializeField] private float _spacing = 10f;
    [Tooltip("Reliquias por columna; si hay más, arma otra columna a la derecha")]
    [SerializeField] private int _perColumn = 8;
    [Tooltip("Qué tan rápido se corren las reliquias cuando entra una nueva")]
    [SerializeField] private float _slideSpeed = 12f;

    [Header("Marco por rareza")]
    [SerializeField, Range(0f, 1f)] private float _discAlpha = 0.35f;
    [SerializeField] private float _ringThickness = 0.09f;

    [Header("Idle")]
    [SerializeField] private float _idleBob = 5f;
    [SerializeField] private float _idleSway = 2f;
    [SerializeField] private float _idleTilt = 6f;
    [SerializeField] private float _idleSpeed = 1f;

    [Header("Cartel al pasar el mouse")]
    [SerializeField] private Color _tooltipBg = new Color(0.08f, 0.09f, 0.16f, 0.94f);
    [SerializeField] private Color _tooltipBorder = new Color(0.85f, 0.65f, 0.3f, 1f);
    [SerializeField] private float _tooltipWidth = 360f;

    [Header("Vuelo al slot")]
    [SerializeField] private float _flySize = 220f;
    [SerializeField] private float _popTime = 0.35f;
    [SerializeField] private float _holdTime = 0.35f;
    [SerializeField] private float _flyTime = 0.5f;

    private readonly List<Slot> _runtimeSlots = new List<Slot>();
    private RelicInventory _inventory;
    private readonly Queue<(SO_RelicData relic, int count)> _pending = new Queue<(SO_RelicData, int)>();
    private bool _playing;
    private GameObject _flyGo;

    private RectTransform _tooltip;
    private Image _tooltipImage;
    private Outline _tooltipOutline;
    private TextMeshProUGUI _ttTitle, _ttRarity, _ttDesc;
    private int _hovered = -1;
    private float _ttAnim;

    private static Sprite _discSprite, _ringSprite;
    private RectTransform Rect => (RectTransform)transform;

    private void Start()
    {
        // Los slots fijos del HUD viejo (fragmentos) ya no se usan: se borran y se arman en runtime
        foreach (var lg in GetComponents<LayoutGroup>()) lg.enabled = false;
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);

        if (_anchorLeft)
        {
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0f, 1f);
            Rect.anchoredPosition = new Vector2(_screenMargin.x, -_screenMargin.y);
        }

        _inventory = RelicInventory.Instance != null ? RelicInventory.Instance : FindFirstObjectByType<RelicInventory>();
        if (_inventory != null)
        {
            _inventory.OnRelicAdded += OnRelicAdded;
            _inventory.OnReset += OnInventoryReset;
            foreach (var r in _inventory.Owned) { var s = CreateSlot(r); SetCount(s, _inventory.GetCount(r)); SetVisible(s, true); }
        }
        else Debug.LogWarning("[RelicHUD] No hay RelicInventory en la escena.");
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnRelicAdded -= OnRelicAdded;
            _inventory.OnReset -= OnInventoryReset;
        }
    }

    private void OnInventoryReset()
    {
        StopAllCoroutines();
        if (_flyGo != null) Destroy(_flyGo);
        _playing = false;
        _pending.Clear();
        _hovered = -1;
        foreach (var s in _runtimeSlots) if (s.root != null) Destroy(s.root.gameObject);
        _runtimeSlots.Clear();
    }

    private void OnRelicAdded(SO_RelicData relic, int count) => _pending.Enqueue((relic, count));

    private void Update()
    {
        UpdateSlide();
        UpdateIdle();
        UpdateTooltip();

        if (_playing || _pending.Count == 0) return;
        // esperar a que se cierre el panel del cofre para que se vea el vuelo
        var panel = ShrineRewardUI.Instance;
        if (panel != null && panel.IsOpen) return;

        var next = _pending.Dequeue();
        StartCoroutine(PlayGain(next.relic, next.count));
    }

    // ------------------------------------------------------------ slots

    private Slot Find(SO_RelicData relic)
    {
        foreach (var s in _runtimeSlots) if (s.relic == relic) return s;
        return null;
    }

    // Posición de la reliquia "index", relativa a la esquina de arriba a la izquierda del HUD.
    // Orden de llegada de arriba hacia abajo; cuando se llena la columna sigue en la de al lado.
    private Vector2 SlotPosition(int index, int total)
    {
        int perCol = Mathf.Max(1, _perColumn);
        int col = index / perCol;
        int row = index % perCol;
        float step = _slotSize + _spacing;
        float x = col * (step + 8f) + _slotSize * 0.5f;
        float y = row * step + _slotSize * 0.5f;
        return new Vector2(x, -y);
    }

    private void Relayout()
    {
        for (int i = 0; i < _runtimeSlots.Count; i++)
            _runtimeSlots[i].target = SlotPosition(i, _runtimeSlots.Count);
    }

    private void UpdateSlide()
    {
        float k = 1f - Mathf.Exp(-_slideSpeed * Time.unscaledDeltaTime);
        foreach (var s in _runtimeSlots)
            if (s.root != null) s.root.anchoredPosition = Vector2.Lerp(s.root.anchoredPosition, s.target, k);
    }

    private Slot CreateSlot(SO_RelicData relic)
    {
        EnsureSprites();
        int index = _runtimeSlots.Count;
        var go = new GameObject("Relic_" + relic.name.Replace("SO_Relic_", ""), typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var root = (RectTransform)go.transform;
        root.SetParent(transform, false);
        root.anchorMin = root.anchorMax = Rect.pivot; // posiciones relativas a la esquina del HUD
        root.sizeDelta = Vector2.one * _slotSize;
        root.anchoredPosition = SlotPosition(index, index + 1);

        var hit = go.GetComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f); // invisible, solo para detectar el mouse
        hit.raycastTarget = true;

        Color rc = SO_RelicData.RarityColor(relic.rarity);
        var s = new Slot
        {
            relic = relic,
            root = root,
            group = go.GetComponent<CanvasGroup>(),
            disc = MakeImage("Disc", root, _discSprite, new Color(rc.r, rc.g, rc.b, _discAlpha), 0f),
            ring = MakeImage("Ring", root, _ringSprite, rc, 0f),
            icon = MakeImage("Icon", root, relic.icon, Color.white, _slotSize * 0.14f),
        };
        s.iconBase = s.icon.rectTransform.anchoredPosition;

        var tGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        var tr = (RectTransform)tGo.transform;
        tr.SetParent(root, false);
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(1f, 0f);
        tr.anchoredPosition = new Vector2(6f, -6f);
        tr.sizeDelta = new Vector2(60f, 30f);
        s.countText = tGo.GetComponent<TextMeshProUGUI>();
        s.countText.fontSize = 24f;
        s.countText.fontStyle = FontStyles.Bold;
        s.countText.alignment = TextAlignmentOptions.BottomRight;
        s.countText.color = Color.white;
        s.countText.outlineWidth = 0.25f;
        s.countText.outlineColor = new Color32(20, 18, 30, 255);
        s.countText.raycastTarget = false;
        s.countText.text = "";

        var hover = go.AddComponent<RelicSlotHover>();
        hover.hud = this;
        hover.index = index;

        // cada reliquia con su propio ritmo
        var r = new System.Random(1234 + index * 977);
        s.phase = (float)r.NextDouble() * 10f;
        s.bobFreq = 1.6f + (float)r.NextDouble() * 0.9f;
        s.swayFreq = 0.7f + (float)r.NextDouble() * 0.6f;
        s.tiltFreq = 1.1f + (float)r.NextDouble() * 0.8f;
        s.bobMul = 0.75f + (float)r.NextDouble() * 0.5f;
        s.swayMul = 0.6f + (float)r.NextDouble() * 0.8f;
        s.tiltMul = (r.NextDouble() < 0.5 ? -1f : 1f) * (0.6f + (float)r.NextDouble() * 0.7f);

        SetVisible(s, false);
        _runtimeSlots.Add(s);
        Relayout();
        s.root.anchoredPosition = s.target;
        return s;
    }

    private static void SetCount(Slot s, int count) => s.countText.text = count > 1 ? "x" + count : "";

    private static void SetVisible(Slot s, bool visible)
    {
        s.group.alpha = visible ? 1f : 0f;
        s.group.blocksRaycasts = visible;
        s.group.interactable = false;
    }

    private Image MakeImage(string n, RectTransform parent, Sprite sprite, Color c, float inset)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = c;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    // Círculo y aro generados por código (así no depende de ningún PNG)
    private void EnsureSprites()
    {
        if (_discSprite == null) _discSprite = MakeCircleSprite(0f);
        if (_ringSprite == null) _ringSprite = MakeCircleSprite(_ringThickness);
    }

    private static Sprite MakeCircleSprite(float ring)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f, outer = c - 1f, inner = ring > 0f ? outer - ring * size : -1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(outer - d + 0.5f);
                if (inner > 0f) a *= Mathf.Clamp01(d - inner + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void UpdateIdle()
    {
        float t = Time.unscaledTime * _idleSpeed;
        foreach (var s in _runtimeSlots)
        {
            if (s.icon == null) continue;
            float tt = t + s.phase;
            Vector2 off = new Vector2(Mathf.Sin(tt * s.swayFreq) * _idleSway * s.swayMul,
                                      Mathf.Sin(tt * s.bobFreq) * _idleBob * s.bobMul);
            s.icon.rectTransform.anchoredPosition = s.iconBase + off;
            s.icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(tt * s.tiltFreq + 1.3f) * _idleTilt * s.tiltMul);
        }
    }

    // ------------------------------------------------------------ cartel

    public void OnSlotHover(int index, bool enter)
    {
        if (enter)
        {
            if (index < 0 || index >= _runtimeSlots.Count) return;
            var s = _runtimeSlots[index];
            if (s.relic == null || s.group.alpha <= 0.01f) return;
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
        int count = _inventory != null ? _inventory.GetCount(s.relic) : 1;
        Color rc = SO_RelicData.RarityColor(s.relic.rarity);

        string stack = count > 1 ? $"  <size=75%>x{count}</size>" : "";
        _ttTitle.text = $"<color=#{ColorUtility.ToHtmlStringRGB(s.relic.NameColor)}>{s.relic.displayName}</color>{stack}";
        _ttRarity.text = SO_RelicData.RarityLabel(s.relic.rarity).ToUpper();
        _ttRarity.color = rc;
        string fx = RelicEffects.Instance != null ? RelicEffects.Instance.Describe(s.relic, count) : "";
        string flavor = string.IsNullOrEmpty(s.relic.description) ? "" : $"\n\n<color=#8A8FA8><i>{s.relic.description}</i></color>";
        _ttDesc.text = fx + flavor;
        _tooltipOutline.effectColor = rc;

        _tooltip.gameObject.SetActive(true);
        _tooltip.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltip);

        // a la derecha del slot, alineado con su borde de arriba (el HUD está a la izquierda)
        _tooltip.pivot = new Vector2(0f, 1f);
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        Vector3[] corners = new Vector3[4];
        s.root.GetWorldCorners(corners); // 2 arriba-der
        _tooltip.position = corners[2] + Vector3.right * 16f * canvas.scaleFactor;

        // que no se salga de la pantalla por abajo
        _tooltip.GetWorldCorners(corners);
        if (corners[0].y < 8f) _tooltip.position += Vector3.up * (8f - corners[0].y);

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

        _tooltipImage = go.GetComponent<Image>();
        _tooltipImage.color = _tooltipBg;
        _tooltipImage.raycastTarget = false;
        _tooltipOutline = go.AddComponent<Outline>();
        _tooltipOutline.effectColor = _tooltipBorder;
        _tooltipOutline.effectDistance = new Vector2(3f, -3f);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _ttTitle = MakeTooltipText("Title", 30f, FontStyles.Bold);
        _ttRarity = MakeTooltipText("Rarity", 18f, FontStyles.Bold);
        _ttDesc = MakeTooltipText("Description", 22f, FontStyles.Normal);

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

    // ------------------------------------------------------------ animación

    private IEnumerator PlayGain(SO_RelicData relic, int count)
    {
        _playing = true;
        var slot = Find(relic);
        if (slot == null) slot = CreateSlot(relic); // nueva: arranca invisible y aparece cuando llega

        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var flyGo = _flyGo = new GameObject("RelicFly", typeof(RectTransform), typeof(Image));
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
            fly.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(k));
            fly.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -25f);
        });
        yield return Wait(_holdTime);

        // vuelo al slot (en curva)
        Vector3 from = fly.position;
        Vector3 fromScale = fly.localScale;
        Vector2 fromSize = fly.sizeDelta;
        Vector2 toSize = slot.icon.rectTransform.rect.size;
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

        // llega: aparece el slot, actualiza el contador y salta (más fuerte si es Super Rare)
        SetVisible(slot, true);
        SetCount(slot, count);
        bool big = relic.rarity == RelicRarity.SuperRare;
        float punch = big ? 1.5f : relic.rarity == RelicRarity.Rare ? 1.35f : 1.25f;
        yield return Tween(big ? 0.5f : 0.3f, k =>
        {
            float p = Mathf.Sin(k * Mathf.PI);
            slot.root.localScale = Vector3.one * (1f + (punch - 1f) * p);
            slot.root.localRotation = Quaternion.Euler(0f, 0f, big ? Mathf.Sin(k * Mathf.PI * 4f) * 12f * (1f - k) : 0f);
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
}
