using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
    public GameObject bnner;


    [Header("Mortar")]
    public GameObject mortarGo;
    public GameObject mortargoAbilityUI;
    public GameObject mortargoAbilityNumber;
    public GameObject mortargoAbilityStats;
    public GameObject mortargoAbilityKey;

    public GameObject canvas;


    
    
    public void OnButtonClick()
    {
        ability.Upgrade();
        canvas.SetActive(false);

       
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

        
    }
    public void OnButtonClick2()
    {
        bnner.SetActive(true);
        canvas.SetActive(false);

        
    }
}
