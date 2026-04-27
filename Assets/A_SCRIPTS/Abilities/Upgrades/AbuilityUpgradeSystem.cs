using UnityEngine;
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

    public void ResetAllUpgrades()
    {
        foreach (var ability in _abilities)
            ability.ResetUpgrades();
    }

    private void HandleEnemyDied(Vector3 position)
    {
        if (_abilities.Count == 0 || _upgradeRules.Count == 0) return;

        int abilityIndex = Random.Range(0, _abilities.Count);
        IUpgradeable chosenAbility = _abilities[abilityIndex];

        int ruleIndex = Random.Range(0, _upgradeRules.Count);
        UpgradeRule chosenRule = _upgradeRules[ruleIndex];

        chosenAbility.ApplyUpgrade(chosenRule.stat, chosenRule.valuePerKill);
        _statsUI.OnUpgradeApplied(chosenRule.stat, chosenRule.valuePerKill);

        Sprite icon = _iconLibrary.GetIcon(chosenAbility.AbilityId, chosenRule.stat);
        if (icon == null) return;

        EnemyUpgradeDisplay popup = Instantiate(_popupPrefab, position, Quaternion.identity);
        popup.Play(icon, position);
    }
}