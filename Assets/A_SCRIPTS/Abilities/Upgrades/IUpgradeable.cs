using UnityEngine;

public interface IUpgradeable 
{
    string AbilityId { get; }
    void ApplyUpgrade(StatType stat, float value);
}
