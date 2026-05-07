using UnityEngine;
using UnityEngine.UI;

public class SkillPreselectionManager : MonoBehaviour
{
    public static SkillPreselectionManager Instance { get; private set; }
    public static bool IsUnlocked { get; private set; } = false;

    [SerializeField] private SkillButton[] skillButtons;
    [SerializeField] private Button[] buttons;

    private void Awake()
    {
        Instance = this;
        IsUnlocked = false;
        foreach (var btn in buttons)
            btn.interactable = false;
    }

    public void OnAbilitySelected(SelectedAbility selected)
    {
        foreach (var btn in skillButtons)
            btn.SetSelectedIndicator(btn.Ability == selected);
    }

    public void UnlockSkillButtons()
    {
        IsUnlocked = true;
        foreach (var btn in buttons)
            btn.interactable = true;
    }

    public void Reset()
    {
        IsUnlocked = false;
        foreach (var btn in buttons)
            btn.interactable = false;
        foreach (var btn in skillButtons)
            btn.SetSelectedIndicator(false);
    }
}