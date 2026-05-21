using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/UpgradeStep")]
public class SO_UpgradeStep : ScriptableObject
{
    [Header("Visual")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Stat Modifier")]
    public StatType statType = StatType.None;
    public float statValue;

    [Header("Special Ability (solo tier 4)")]
    public SpecialAbilityType specialAbility = SpecialAbilityType.None;
}