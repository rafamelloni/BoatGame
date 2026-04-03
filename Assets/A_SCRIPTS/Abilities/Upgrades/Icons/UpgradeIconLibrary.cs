using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/UpgradeIconLibrary")]

public class UpgradeIconLibrary : ScriptableObject
{
    [System.Serializable]
    public struct UpgradeIcon
    {
        public string abilityId;   // "Cannon", "Morter"
        public StatType stat;
        public Sprite icon;
    }

    [SerializeField] private List<UpgradeIcon> _icons;

    public Sprite GetIcon(string abilityId, StatType stat)
    {
        foreach (var entry in _icons)
        {
            if (entry.abilityId == abilityId && entry.stat == stat)
                return entry.icon;
        }
        return null;
    }
}
