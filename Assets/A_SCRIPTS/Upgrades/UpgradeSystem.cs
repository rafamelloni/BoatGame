using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    [SerializeField] private RT_PlayerStats playerStats;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;
    [SerializeField] private AbilityController abilityController;

    public void ApplyUpgrade(SO_UpgradePath path)
    {
        if (!playerUpgrades.CanUpgrade(path))
        {
            Debug.LogWarning($"Path {path.pathName} ya esta completo.");
            return;
        }

        SO_UpgradeStep step = playerUpgrades.GetNextStep(path);
        if (step == null) return;

        ApplyStat(step);
        ApplySpecialAbility(step);
        playerUpgrades.AdvancePath(path);

        Debug.Log($"[UpgradeSystem] {path.pathName} → {step.displayName}");
    }

    private void ApplyStat(SO_UpgradeStep step)
    {
        switch (step.statType)
        {
            case StatType.Damage:
                abilityController.CannonAbility._rtData.damage *= 1f + step.statValue / 100f;
                break;

            case StatType.Cooldown:
                abilityController.CannonAbility._rtData.cooldown *= 1f - step.statValue / 100f;
                break;

            case StatType.MoveSpeed:
                playerStats.moveSpeed *= 1f + step.statValue / 100f;
                break;

            case StatType.MaxHealth:
                float extra = playerStats.maxHealth * (step.statValue / 100f);
                playerStats.maxHealth += extra;
                playerStats.currentHealth += extra;
                break;

            // Estos los completás cuando implementes Molotov y Blades
            case StatType.MolotovDamage: 
            case StatType.MolotovArea:
            case StatType.BladeDamage:
            case StatType.BladeSpeed:
            case StatType.None:
                break;
        }
    }

    private void ApplySpecialAbility(SO_UpgradeStep step)
    {
        if (step.specialAbility == SpecialAbilityType.None) return;

        playerUpgrades.UnlockAbility(step.specialAbility);

        switch (step.specialAbility)
        {
            case SpecialAbilityType.Ricochet:
                // CannonBullet leerá HasAbility(Ricochet) desde RT_PlayerUpgrades
                break;

            case SpecialAbilityType.DoubleShot:
                abilityController.CannonAbility._rtData.shotsPerBurst *= 2;
                break;

            case SpecialAbilityType.InfiniteSprint:
                // SprintController leerá HasAbility(InfiniteSprint)
                break;

            case SpecialAbilityType.PassiveRegen:
                // RegenController leerá HasAbility(PassiveRegen)
                break;

            case SpecialAbilityType.TripleMolotov:
                abilityController.MolotovAbility.SetUnlocked(true);
                break;
            case SpecialAbilityType.ExplosiveBlades:
                break;
        }
    }

    public void ResetAll()
    {
        playerUpgrades.ResetAll();
        abilityController.ResetAbilities();
        // playerStats se resetea desde donde ya lo manejás
    }
}