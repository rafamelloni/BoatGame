using TMPro;
using UnityEngine;

// Texto de munición ("2/2") pegado al crosshair (que es el cursor del mouse).
// Ponelo en cualquier GameObject de la escena. Si no le asignás nada, crea solo
// su Canvas (Screen Space Overlay) y su texto, y busca el AbilityController.
public class CrosshairAmmoText : MonoBehaviour
{
    [SerializeField] private AbilityController _abilityController;

    [Header("Texto")]
    [Tooltip("Opcional: si lo dejás vacío se crea uno solo")]
    [SerializeField] private TextMeshProUGUI _text;
    [Tooltip("Separación del cursor en píxeles hasta la esquina sup. izquierda del texto (x a la derecha, y negativo = abajo)")]
    [SerializeField] private Vector2 _cursorOffset = new Vector2(26f, -26f);
    [SerializeField] private float _fontSize = 26f;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _emptyColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private string _reloadingText = "...";

    private CannonStrategy _cannon;
    private RectTransform _rect;

    private void Start()
    {
        if (_abilityController == null)
            _abilityController = FindFirstObjectByType<AbilityController>();
        if (_text == null)
            _text = CreateText();
        _rect = _text.rectTransform;

        _cannon = _abilityController != null ? _abilityController.CannonAbility : null;
        if (_cannon == null)
        {
            Debug.LogWarning("[CrosshairAmmoText] No encontré el AbilityController / cañón.");
            return;
        }
        _cannon.OnAmmoChanged += OnAmmoChanged;
        Refresh();
    }
    private void OnDestroy()
    {
        if (_cannon != null) _cannon.OnAmmoChanged -= OnAmmoChanged;
    }

    private void LateUpdate()
    {
        if (_cannon == null || _text == null) return;

        // solo se ve cuando el cañón está desbloqueado
        bool visible = _abilityController._wasUCannon;
        if (_text.gameObject.activeSelf != visible) _text.gameObject.SetActive(visible);
        if (!visible) return;

        _rect.position = (Vector2)Input.mousePosition + _cursorOffset;

        // durante la recarga el texto cambia, así que se refresca cada frame
        if (_cannon.IsReloading) Refresh();
    }

    private void OnAmmoChanged(int current, int max) => Refresh();

    private void Refresh()
    {
        int current = _cannon.CurrentAmmo;
        int max = _cannon.MaxAmmo;
        _text.text = _cannon.IsReloading ? $"{current}/{max} {_reloadingText}" : $"{current}/{max}";
        _text.color = current <= 0 ? _emptyColor : _normalColor;
    }

    private TextMeshProUGUI CreateText()
    {
        var canvasGo = new GameObject("CrosshairAmmoCanvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // arriba del resto de la UI

        var textGo = new GameObject("AmmoText", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        var t = textGo.AddComponent<TextMeshProUGUI>();
        t.fontSize = _fontSize;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.outlineWidth = 0.25f;
        t.outlineColor = Color.black;

        var r = t.rectTransform;
        r.pivot = new Vector2(0f, 1f); // esquina sup. izquierda: el texto queda abajo a la derecha del cursor
        r.sizeDelta = new Vector2(160f, 40f);
        return t;
    }
}
