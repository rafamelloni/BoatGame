using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
    [SerializeField] CannonUpgrades _cannonUpgrades;
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
    
    //CANNON
    public void CannonChargeOnClick()
    {
        _cannonUpgrades.AddCharge();
        canvas.SetActive(false);
        OnRouglikeExit();

    }
    public void CannonBulletSizeOnClick()
    {
        _cannonUpgrades.IncreaseBulletSize();
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    //MORTAR
    public void MortarAbilityOnClick()
    {
        ability.MortarAveilable();
        ability.LetMortarBeUpgraded();
        canvas.SetActive(false);
        OnRouglikeExit();
    }

    //SHIP
    public void HPPlayerOnClick()
    {
        _playerUpgrades.MaxHP();
        canvas.SetActive(false);
        OnRouglikeExit();
    }
    public void SpeedPlayerOnClick()
    {
        _playerUpgrades.Speed();
        canvas.SetActive(false);
        OnRouglikeExit();
    }


    //??
    public void OnButtonClick5()
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
