using UnityEngine;
using UnityEngine.UI;

public class UpgradeSystem : MonoBehaviour
{
    [SerializeField] private RT_PlayerStats playerStats;
    [SerializeField] private RT_PlayerHealth playerH;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private SpecialAbilityHUD abilityHUD;
    [SerializeField] private Movement movement;

    [Header("Sprites HUD")]
    [SerializeField] private Sprite spriteRicochet;
    [SerializeField] private Sprite spriteDoubleShot;
    [SerializeField] private Sprite spriteDashes;
    [SerializeField] private Sprite spriteClearScreen;
    [SerializeField] private Sprite spriteUnlockMolotov;
    [SerializeField] private Sprite spriteTripleMolotov;
    [SerializeField] private Sprite spriteUnlockBlades;
    [SerializeField] private Sprite spriteBladesBurst;

    [SerializeField] private DashMovement dashMovement;
    [SerializeField] private LastStand lastStand;


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
                playerH.RefreshUI();
                break;
            case StatType.HealthRegen:
                playerH.StartRegen(0.5f);
                break;
            case StatType.MolotovDamage:
                abilityController.MolotovAbility._rtData.damage *= 1f + step.statValue / 100f;
                break;
            case StatType.MolotovArea:
                abilityController.MolotovAbility._rtData.explosionRadius *= 1f + step.statValue / 100f;
                break;
            case StatType.BladeDamage:
                abilityController.BladesAbility._rtData.damage += step.statValue;
                break;
            case StatType.BladeSpeed:
                abilityController.BladesAbility._rtData.orbitSpeed += step.statValue;
                break;
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
                abilityHUD.UnlockNext(spriteRicochet);
                break;
            case SpecialAbilityType.DoubleShot:
                abilityController.CannonAbility._rtData.shotsPerBurst *= 2;
                abilityHUD.UnlockNext(spriteDoubleShot);
                break;
            case SpecialAbilityType.Dashes:
                movement.UpgradeSprintDuration();
                abilityHUD.UnlockNext(spriteDashes);
                break;
            case SpecialAbilityType.ClearScreen:
                lastStand.Unlock();
                abilityHUD.UnlockNext(spriteClearScreen);
                break;
            case SpecialAbilityType.UnlockMolotov:
                abilityController.MolotovAbility.SetUnlocked(true);
                abilityHUD.UnlockNext(spriteUnlockMolotov);
                break;
            case SpecialAbilityType.TripleMolotov:
               // abilityHUD.UnlockNext(spriteTripleMolotov);
                break;
            case SpecialAbilityType.UnlockBlades:
                abilityController.BladesAvailable();
                abilityHUD.UnlockNext(spriteUnlockBlades);
                break;
            case SpecialAbilityType.BladesBurst:
                Debug.Log("[UpgradeSystem] BladesBurst desbloqueado");
                //abilityHUD.UnlockNext(spriteBladesBurst);
                break;
        }
    }

    public void ResetAll()
    {
        playerUpgrades.ResetAll();
        abilityController.ResetAbilities();
        abilityHUD.ResetAll();
    }
}