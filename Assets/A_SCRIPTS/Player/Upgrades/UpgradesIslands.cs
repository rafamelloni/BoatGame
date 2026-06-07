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
    //CANNON
    public void CannonChargeOnClick()
    {
       // _cannonUpgrades.AddCharge();
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
        
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    //Crit
    public void CritChanceOnClick()
    {
        
        canvas.SetActive(false);
        OnRouglikeExit();
    }
    public void CritMultiplierOnClick()
    {
        
        canvas.SetActive(false);
        OnRouglikeExit();
    }


    //PickUpRange
    public void PickUpRangeOnClick()
    {
        //bnner.SetActive(true);
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
