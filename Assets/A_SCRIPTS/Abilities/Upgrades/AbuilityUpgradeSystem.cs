using UnityEngine;
using System;
using System.Collections.Generic;

public class AbilityUpgradeSystem : MonoBehaviour
{
    [System.Serializable]
    public struct UpgradeRule
    {
        public StatType stat;
        public float valuePerKill;
    }

    [SerializeField] private List<UpgradeRule> _upgradeRules;

    //data para icons upgrades
    [SerializeField] private UpgradeIconLibrary _iconLibrary;
    public event Action<Sprite> OnUpgradeApplied;

    private readonly List<IUpgradeable> _abilities = new();

    private void Start()
    {
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.OnDeath += HandleEnemyDied;
        }
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

    public void HandleEnemyDied()
    {
        if (_abilities.Count == 0 || _upgradeRules.Count == 0) return;

        int abilityIndex = UnityEngine.Random.Range(0, _abilities.Count);
        IUpgradeable chosenAbility = _abilities[abilityIndex];

        int ruleIndex = UnityEngine.Random.Range(0, _upgradeRules.Count);
        UpgradeRule chosenRule = _upgradeRules[ruleIndex];

        chosenAbility.ApplyUpgrade(chosenRule.stat, chosenRule.valuePerKill);

        Sprite icon = _iconLibrary.GetIcon(chosenAbility.AbilityId, chosenRule.stat);
        OnUpgradeApplied?.Invoke(icon);
    }
}