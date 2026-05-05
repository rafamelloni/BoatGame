using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
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

    public void OnButtonClick()
    {
        ability.Upgrade();
        canvas.SetActive(false);
        OnRouglikeExit();

    }
    public void OnButtonClick1()
    {
        ability.MortarAveilable();
        ability.LetMortarBeUpgraded();
        canvas.SetActive(false);
        OnRouglikeExit();


    }
    public void OnButtonClick2()
    {
        bnner.SetActive(true);
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
