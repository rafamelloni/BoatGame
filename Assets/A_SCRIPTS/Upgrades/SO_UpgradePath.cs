using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/UpgradePath")]
public class SO_UpgradePath : ScriptableObject
{
    [Header("Info")]
    public string pathName;
    public Sprite pathIcon;
    [TextArea] public string pathDescription;

    [Header("Steps (exactamente 4)")]
    public SO_UpgradeStep[] steps = new SO_UpgradeStep[4];

    public SO_UpgradeStep GetStep(int index)
    {
        if (index < 0 || index >= steps.Length) return null;
        return steps[index];
    }
}