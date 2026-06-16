using UnityEngine;
using UnityEngine.UI;

public class PlayerUpgrades : MonoBehaviour
{
    private RT_PlayerStats _rtData;
    private RT_CannonData _cannonData;
    private RT_BladesData _bladesData;
    private RT_MolotovData _molotovData;

    [SerializeField] private AbilityController _abilityController;
    public RT_PlayerStats Stats => _rtData;

    [Header("Crit")]
    [SerializeField] private float _critChancePerUpgrade = 0.1f;     // +10% por upgrade
    [SerializeField] private float _critMultiplierPerUpgrade = 0.25f; // +0.25 por upgrade

    [Header("Crit")]
    [SerializeField] private float _damageU = 10f;
    [SerializeField] private float _damageS = 10f;


    [Header("block")]
    [SerializeField] private float _blockChancePerUpgrade = 0.08f; // +8% por upgrade


    public void Setup(RT_PlayerStats statsPlayer)
    {
        _rtData = statsPlayer;
        _cannonData = _abilityController.CannonAbility.RuntimeData;
        _bladesData = _abilityController.BladesAbility._rtData;
        _molotovData = _abilityController.MolotovAbility._rtData;
    }
    //BLADES
    public void BladesSpeed() 
    {
        _bladesData.orbitSpeed += 50f;
    }
    //BLOCK
    public void BlockChanceUpgrade()
    {
        _rtData.blockChance = Mathf.Clamp01(_rtData.blockChance + _blockChancePerUpgrade);
    }
    //COOLDOWN
    public void CooldownAll()
    {
        _cannonData.cooldown -= 0.5f;
        _bladesData.burstInterval = 5f;
    }

    public void SummonCooldown()
    {
        _bladesData.burstInterval = 5f;
    }
    //PICKUPRANGE
    public void PickUpRange()
    {
        _rtData.pickUpRange += 5f;
    }
    //RAMMING
    public void RammingDamage()
    {
        _rtData.damageWhenBumpingEnemies -= 5f;
    }

    //DAMAGE
    public void DamageUpgrade()
    {
        _cannonData.damage += _damageU;
    }
    public void SummonDamage()
    {
        _bladesData.damage += _damageS;
    }

    //CRIT
    public void CritChanceUpgrade()
    {
        _rtData.critChance = Mathf.Clamp01(_rtData.critChance + _critChancePerUpgrade);
    }

    public void CritMultiplierUpgrade()
    {
        _rtData.critMultiplier += _critMultiplierPerUpgrade;
    }

    public void ResetAll()
    {
        _abilityController.CannonAbility.ResetUpgrades();
        _abilityController.BladesAbility.ResetUpgrades();
        _abilityController.MolotovAbility.ResetUpgrades();
    }

}