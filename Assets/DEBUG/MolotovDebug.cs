using UnityEngine;
public class MolotovDebug : MonoBehaviour
{
    [SerializeField] private AbilityController abilityController;
    [SerializeField] private RT_PlayerUpgrades playerUpgrades;
    [SerializeField] private LastStand lastStand;
    [SerializeField] private DashMovement dashMovement;
    public Material liquidMat;

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.M))
            abilityController.MolotovAbility.SetUnlocked(true);

        if (Input.GetKeyDown(KeyCode.N))
        {
            abilityController.MolotovAbility.SetUnlocked(true);
            playerUpgrades.UnlockAbility(SpecialAbilityType.TripleMolotov);
        }

        if (Input.GetKeyDown(KeyCode.B))
            abilityController.BladesAvailable();

        if (Input.GetKeyDown(KeyCode.V))
            abilityController.BladesAbility.ApplyUpgrade(StatType.BladeCount, abilityController.BladesAbility._rtData.bladeCount + 1);

        if (Input.GetKeyDown(KeyCode.G))
            abilityController.BladesAbility.TriggerBurst();

        // L - desbloquea LastStand (limpiar pantalla al 30% HP)
         if (Input.GetKeyDown(KeyCode.L))
        {
            lastStand.DebugActivate();

        }
            

        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log($"[Debug] DashMovement ref: {dashMovement}");
            dashMovement.Unlock();
        }
    }
}