using UnityEngine;
using UnityEngine.UI;

// Poné este componente en un Button que armes vos a mano en el Canvas
// (posicionado donde quieras, como parte del dibujo del árbol).
// Le asignás qué SO_NormalNode representa y el RT_SkillTree, y el
// componente se encarga de mostrar su estado y subirle nivel al click.
[RequireComponent(typeof(Button))]
public class SkillTreeNormalNodeButton : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RT_SkillTree _tree;
    [SerializeField] private SO_NormalNode _node;

    [Header("UI (si lo dejas vacio, busca un Text hijo solo)")]
    [SerializeField] private Text _label;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_label == null) _label = GetComponentInChildren<Text>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        if (_tree == null) return;
        _tree.OnNormalNodeLeveled += HandleLeveled;
        _tree.OnCurrencyChanged += HandleCurrencyChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (_tree == null) return;
        _tree.OnNormalNodeLeveled -= HandleLeveled;
        _tree.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void HandleLeveled(SO_NormalNode node, int level) => Refresh();
    private void HandleCurrencyChanged(int amount) => Refresh();

    private void OnClick()
    {
        if (_tree == null || _node == null) return;
        if (!_tree.TryLevelUp(_node))
            Debug.Log($"[SkillTreeNormalNodeButton] No se pudo subir {_node.displayName} (maximo o sin plata)");
    }

    public void Refresh()
    {
        if (_tree == null || _node == null) return;

        int level = _tree.GetLevel(_node);
        if (_label != null)
            _label.text = $"{_node.displayName}\n{level}/{_node.maxLevel}  (costo {_node.cost})";

        _button.interactable = _tree.CanLevelUp(_node) && _tree.CanAfford(_node);
    }
}
