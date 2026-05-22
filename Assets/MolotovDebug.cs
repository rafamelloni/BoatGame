using UnityEngine;

public class MolotovDebug : MonoBehaviour
{
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;

    private void Update()
    {
        // M - desbloquea molotov normal
        if (Input.GetKeyDown(KeyCode.M))
            abilityController.MolotovAbility.SetUnlocked(true);

        // B - desbloquea rafaga (tier 4)
        if (Input.GetKeyDown(KeyCode.N))
        {
            abilityController.MolotovAbility.SetUnlocked(true);
            playerUpgrades.UnlockAbility(SpecialAbilityType.TripleMolotov);
        }
    }
}