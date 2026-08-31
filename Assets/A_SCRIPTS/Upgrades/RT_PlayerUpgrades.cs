using System.Collections.Generic;
using UnityEngine;

public class RT_PlayerUpgrades : MonoBehaviour
{
    private Dictionary<SO_UpgradePath, int> _progress = new();
    private HashSet<SpecialAbilityType> _unlockedAbilities = new();

    public int GetLevel(SO_UpgradePath path) =>
        _progress.TryGetValue(path, out int lvl) ? lvl : 0;

    public bool CanUpgrade(SO_UpgradePath path) =>
        GetLevel(path) < 4;

    public SO_UpgradeStep GetNextStep(SO_UpgradePath path)
    {
        int current = GetLevel(path);
        return current < 4 ? path.GetStep(current) : null;
    }

    public void AdvancePath(SO_UpgradePath path)
    {
        int current = GetLevel(path);
        if (current >= 4) return;
        _progress[path] = current + 1;
    }

    public void UnlockAbility(SpecialAbilityType ability)
    {
        if (ability != SpecialAbilityType.None)
            _unlockedAbilities.Add(ability);
    }

    public bool HasAbility(SpecialAbilityType ability) =>
        _unlockedAbilities.Contains(ability);

    public void ResetAll()
    {
        _progress.Clear();
        _unlockedAbilities.Clear();
    }
}
