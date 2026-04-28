using UnityEngine;

public interface IUpgradeable
{
    string AbilityId { get; }
    bool IsUnlocked { get; }
    void ApplyUpgrade(StatType stat, float value);
    void ResetUpgrades();
}