using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityUpgradeSystem : MonoBehaviour
{
    [System.Serializable]
    public struct UpgradeRule
    {
        public StatType stat;
        public float valuePerKill;
    }

    [SerializeField] private List<UpgradeRule> _upgradeRules;
    [SerializeField] private UpgradeIconLibrary _iconLibrary;
    [SerializeField] private EnemyUpgradeDisplay _popupPrefab;
    [SerializeField] private UpgradeStatsUI _statsUI;

    private readonly List<IUpgradeable> _abilities = new();

    private void OnEnable()
    {
        EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
    }

    public void RegisterAbility(IUpgradeable ability)
    {
        if (!_abilities.Contains(ability))
            _abilities.Add(ability);
    }

    public void UnregisterAbility(IUpgradeable ability)
    {
        _abilities.Remove(ability);
    }

    public void ResetAllUpgradesStats()
    {
        foreach (var ability in _abilities)
            ability.ResetUpgrades();
    }

    private void HandleEnemyDied(Vector3 position)
    {
        if (_abilities.Count == 0) return;
        var unlocked = _abilities.Where(a => a.IsUnlocked).ToList();
        if (unlocked.Count == 0) return;

        IUpgradeable chosenAbility = unlocked[Random.Range(0, unlocked.Count)];

        StatType[] validStats = chosenAbility.ValidStats;
        if (validStats == null || validStats.Length == 0) return;
        StatType chosenStat = validStats[Random.Range(0, validStats.Length)];

        // buscar valuePerKill en las reglas
        float value = 0f;
        foreach (var rule in _upgradeRules)
        {
            if (rule.stat == chosenStat) { value = rule.valuePerKill; break; }
        }
        if (value == 0f) return;

        chosenAbility.ApplyUpgrade(chosenStat, value);
        _statsUI.OnUpgradeApplied(chosenAbility.AbilityId, chosenStat, value);

        Sprite icon = _iconLibrary.GetIcon(chosenAbility.AbilityId);
        if (icon == null) return;

        EnemyUpgradeDisplay popup = Instantiate(_popupPrefab, position, Quaternion.identity);
        popup.Play(icon, position);
    }
}