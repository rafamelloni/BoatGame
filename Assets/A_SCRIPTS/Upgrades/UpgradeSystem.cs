using UnityEngine;

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
    [SerializeField] private Sprite spriteUnlockCrossbow;
    [SerializeField] private Sprite spriteCrossbowBurst;

    [SerializeField] private DashMovement dashMovement;
    [SerializeField] private LastStand lastStand;

    private int _molotovSlotIndex = -1;
    private int _bladesSlotIndex = -1;

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
            case StatType.CrossbowDamage:
                abilityController.CrossbowAbility._rtData.damage *= 1f + step.statValue / 100f;
                break;
            case StatType.CrossbowCooldown:
                abilityController.CrossbowAbility._rtData.cooldown *= 1f - step.statValue / 100f;
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
                abilityHUD.UnlockNext(spriteClearScreen, () => lastStand.CooldownProgress);
                break;
            case SpecialAbilityType.UnlockMolotov:
                abilityController.MolotovAbility.SetUnlocked(true);
                abilityHUD.UnlockNext(spriteUnlockMolotov);
                _molotovSlotIndex = abilityHUD.GetLastUnlockedIndex();
                break;
            case SpecialAbilityType.TripleMolotov:
                abilityHUD.AddProgressToIndex(_molotovSlotIndex, () => abilityController.MolotovAbility.LaunchProgress);
                break;
            case SpecialAbilityType.UnlockCrossbow:
                abilityController.CrossbowAvailable();
                abilityHUD.UnlockNext(spriteUnlockCrossbow);
                break;
            case SpecialAbilityType.CrossbowBurst:
                abilityHUD.UnlockNext(spriteCrossbowBurst);
                break;
            case SpecialAbilityType.UnlockBlades:
                abilityController.BladesAvailable();
                abilityHUD.UnlockNext(spriteUnlockBlades);
                _bladesSlotIndex = abilityHUD.GetLastUnlockedIndex();
                Debug.Log($"[UpgradeSystem] UnlockBlades slot index: {_bladesSlotIndex}");
                break;
            case SpecialAbilityType.BladesBurst:
                abilityHUD.AddProgressToIndex(_bladesSlotIndex, () => abilityController.BladesAbility.BurstProgress);
                Debug.Log($"[UpgradeSystem] BladesBurst usando index: {_bladesSlotIndex}");

                Debug.Log("[UpgradeSystem] BladesBurst desbloqueado");
                break;
        }
    }

    public void ResetAll()
    {
        _molotovSlotIndex = -1;
        _bladesSlotIndex = -1;
        playerUpgrades.ResetAll();
        abilityController.ResetAbilities();
        abilityHUD.ResetAll();
    }
}