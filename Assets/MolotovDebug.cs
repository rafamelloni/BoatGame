using UnityEngine;

public class MolotovDebug : MonoBehaviour
{
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;
    public Material liquidMat;

    private void Update()
    {
        Debug.Log(gameObject.name, gameObject);

        // M - desbloquea molotov normal
        if (Input.GetKeyDown(KeyCode.M))
            abilityController.MolotovAbility.SetUnlocked(true);

        // N - desbloquea rafaga (tier 4)
        if (Input.GetKeyDown(KeyCode.N))
        {
            abilityController.MolotovAbility.SetUnlocked(true);
            playerUpgrades.UnlockAbility(SpecialAbilityType.TripleMolotov);
        }

        // B - desbloquea blades (tier 1)
        if (Input.GetKeyDown(KeyCode.B))
            abilityController.BladesAvailable();

        // V - agrega una blade extra (simula upgrade de BladeCount)
        if (Input.GetKeyDown(KeyCode.V))
            abilityController.BladesAbility.ApplyUpgrade(StatType.BladeCount, abilityController.BladesAbility._rtData.bladeCount + 1);

        if (Input.GetKeyDown(KeyCode.G))
            abilityController.BladesAbility.TriggerBurst();
    }
}