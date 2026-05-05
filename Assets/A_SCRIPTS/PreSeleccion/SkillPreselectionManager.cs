using UnityEngine;

public class SkillPreselectionManager : MonoBehaviour
{
    public static SkillPreselectionManager Instance { get; private set; }

    [SerializeField] private SkillButton[] skillButtons;

    private void Awake()
    {
        Instance = this;
    }

    public void OnAbilitySelected(SelectedAbility selected)
    {
        foreach (var btn in skillButtons)
            btn.SetSelectedIndicator(btn.Ability == selected);
    }
}