using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
    public GameObject bnner;

    public GameObject mortargo;
    public GameObject mortargoAbility;

    public GameObject canvas;



    public GameObject bordesa;
    public GameObject bordes1;
    public GameObject bordes2;
    
    public void OnButtonClick()
    {
        ability.Upgrade();
        canvas.SetActive(false);

        bordesa.SetActive(false);
        bordes1.SetActive(false);
        bordes2.SetActive(false);
    }
    public void OnButtonClick1()
    {
        //ability.wasUpgraded = true;
        mortargo.SetActive(true);
        mortargoAbility.SetActive(true);
        canvas.SetActive(false);

        bordesa.SetActive(false);
        bordes1.SetActive(false);
        bordes2.SetActive(false);
    }
    public void OnButtonClick2()
    {
        bnner.SetActive(true);
        canvas.SetActive(false);

        bordesa.SetActive(false);
        bordes1.SetActive(false);
        bordes2.SetActive(false);
    }
}
