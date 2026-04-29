using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
    public GameObject bnner;

    public GameObject UIDesactivar;


    [Header("Mortar")]
    public GameObject mortarGo;
    public GameObject mortargoAbilityUI;
    public GameObject mortargoAbilityNumber;
    public GameObject mortargoAbilityStats;
    public GameObject mortargoAbilityKey;

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
        ability._wasU = true;
        mortarGo.SetActive(true);


        mortargoAbilityUI.SetActive(true);
        mortargoAbilityNumber.SetActive(true);
        mortargoAbilityStats.SetActive(true);
        mortargoAbilityKey.SetActive(true);
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
        StartCoroutine(stopTime());
    }

    public void OnRouglikeExit()
    {
        UIDesactivar.SetActive(true);
        Time.timeScale = 1f;
    }

    IEnumerator stopTime()
    {
        yield return new WaitForSeconds(1f);
        Time.timeScale = 0f;
    }
}
