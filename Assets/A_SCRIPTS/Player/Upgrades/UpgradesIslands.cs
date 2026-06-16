using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
    [SerializeField] PlayerUpgrades _playerUpgrades;

    public GameObject bnner;

    public GameObject UIDesactivar;



    public GameObject canvas;


    private void Start()
    {
        IslandManager.OnAnyIslandDefeated += OnRouglikeSelection;
    }
    private void OnDisable()
    {
        IslandManager.OnAnyIslandDefeated -= OnRouglikeSelection;
    }
    //TEST AP[AGAR
    public void FakeUpgrade()
    {
        // _cannonUpgrades.AddCharge();
        canvas.SetActive(false);
        OnRouglikeExit();

    }

    //BLADES SPEED
    public void BladesSpeed()
    {
        _playerUpgrades.BladesSpeed();
        canvas.SetActive(false);
        OnRouglikeExit();
    }
    public void BlockChanceUpgrade()
    {
        _playerUpgrades.BlockChanceUpgrade();
        canvas.SetActive(false);
        OnRouglikeExit();
    }
    //COOLDOWN 
    public void CooldownAll()
    {
        _playerUpgrades.CooldownAll();
        canvas.SetActive(false);
        OnRouglikeExit();

    }

    public void SummonCooldown()
    {
        _playerUpgrades.SummonCooldown();
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    //RAMMING
    public void RammingUpgrade()
    {
        _playerUpgrades.RammingDamage();
        canvas.SetActive(false);
        OnRouglikeExit();

    }

    //CANNON
    public void CannonChargeOnClick()
    {
       //_cannonUpgrades.AddCharge();
        canvas.SetActive(false);
        OnRouglikeExit();

    }
    public void CannonBulletSizeOnClick()
    {
       // _cannonUpgrades.IncreaseBulletSize();
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    //Damage
    public void DamageAllOnClick()
    {
        _playerUpgrades.DamageUpgrade();
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    public void SummonDamage()
    {
        _playerUpgrades.SummonDamage();
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    //Crit
    public void CritChanceOnClick()
    {
        _playerUpgrades.CritChanceUpgrade();
        canvas.SetActive(false);
        OnRouglikeExit();
    }
    public void CritMultiplierOnClick()
    {
        _playerUpgrades.CritMultiplierUpgrade();
        canvas.SetActive(false);
        OnRouglikeExit();
    }


    //PickUpRange
    public void PickUpRangeOnClick()
    {
        _playerUpgrades.PickUpRange();
        canvas.SetActive(false);
        OnRouglikeExit();
    }


    public void OnRouglikeSelection()
    {
        UIDesactivar.SetActive(false);
        Time.timeScale = 0f;
    }

    public void OnRouglikeExit()
    {
        UIDesactivar.SetActive(true);
        Time.timeScale = 1f;
    }
}
