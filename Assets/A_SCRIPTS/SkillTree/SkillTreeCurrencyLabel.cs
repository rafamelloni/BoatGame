using UnityEngine;
using UnityEngine.UI;

// Text suelto que pongas en el Canvas a mano, para mostrar la plata
// (999999 mientras testeas, la real despues).
[RequireComponent(typeof(Text))]
public class SkillTreeCurrencyLabel : MonoBehaviour
{
    [SerializeField] private RT_SkillTree _tree;

    private Text _label;

    private void Awake() => _label = GetComponent<Text>();

    private void OnEnable()
    {
        if (_tree == null) return;
        _tree.OnCurrencyChanged += Handle;
        Refresh();
    }

    private void OnDisable()
    {
        if (_tree == null) return;
        _tree.OnCurrencyChanged -= Handle;
    }

    private void Handle(int amount) => Refresh();

    private void Refresh()
    {
        if (_tree == null || _label == null) return;
        _label.text = $"Plata: {_tree.Currency}";
    }
}
