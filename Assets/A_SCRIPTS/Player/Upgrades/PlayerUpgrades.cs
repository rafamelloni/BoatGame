using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    private RT_PlayerStats _rtData;
    private RT_CannonData _cannonData;
    private RT_BladesData _bladesData;
    private RT_MolotovData _molotovData;

    [SerializeField] private AbilityController _abilityController;

    public void Setup(RT_PlayerStats statsPlayer)
    {
        _rtData = statsPlayer;
        _cannonData = _abilityController.CannonAbility.RuntimeData;
        _bladesData = _abilityController.BladesAbility._rtData;
        _molotovData = _abilityController.MolotovAbility._rtData;
    }

    public void DamageUpgrade()
    {

    }

    public void ResetAll()
    {
        _abilityController.CannonAbility.ResetUpgrades();
        _abilityController.BladesAbility.ResetUpgrades();
        _abilityController.MolotovAbility.ResetUpgrades();
    }

}