using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/UpgradeIconLibrary")]

public class UpgradeIconLibrary : ScriptableObject
{
    [System.Serializable]
    public struct UpgradeIcon
    {
        public string abilityId;
        public Sprite icon;
    }
    [SerializeField] private List<UpgradeIcon> _icons;

    public Sprite GetIcon(string abilityId)
    {
        foreach (var entry in _icons)
            if (entry.abilityId == abilityId) return entry.icon;
        return null;
    }
}
