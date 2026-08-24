using UnityEngine;
using UnityEngine.UI;

// Version para el nodo rojo. Mismo uso que SkillTreeNormalNodeButton:
// lo ponés en un Button armado a mano, le asignás el SO_SpecialNode y
// el RT_SkillTree.
[RequireComponent(typeof(Button))]
public class SkillTreeSpecialNodeButton : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RT_SkillTree _tree;
    [SerializeField] private SO_SpecialNode _special;

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
        _tree.OnNormalNodeLeveled += HandleTreeChanged;
        _tree.OnSpecialNodeUnlocked += HandleSpecialUnlocked;
        Refresh();
    }

    private void OnDisable()
    {
        if (_tree == null) return;
        _tree.OnNormalNodeLeveled -= HandleTreeChanged;
        _tree.OnSpecialNodeUnlocked -= HandleSpecialUnlocked;
    }

    private void HandleTreeChanged(SO_NormalNode node, int level) => Refresh();
    private void HandleSpecialUnlocked(SO_SpecialNode special) => Refresh();

    private void OnClick()
    {
        if (_tree == null || _special == null) return;
        if (!_tree.UnlockSpecial(_special))
            Debug.Log($"[SkillTreeSpecialNodeButton] {_special.displayName} todavia no cumple los requisitos");
    }

    public void Refresh()
    {
        if (_tree == null || _special == null) return;

        bool unlocked = _tree.IsSpecialUnlocked(_special);
        bool unlockable = _tree.IsSpecialUnlockable(_special);

        if (_label != null)
        {
            _label.text = unlocked
                ? $"{_special.displayName}\nDESBLOQUEADO"
                : $"{_special.displayName}\n{(unlockable ? "listo!" : "bloqueado")}";
        }

        _button.interactable = !unlocked && unlockable;
    }
}
