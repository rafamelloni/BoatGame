using Unity.VisualScripting;
using UnityEngine;

public class UpgradesIslands : MonoBehaviour
{
    public AbilityController ability;
    public GameObject bnner;

    public GameObject mortargo;
    public GameObject mortargoAbility;

    public GameObject canvas;
    
    public void OnButtonClick()
    {
        ability.upgrade();
        canvas.SetActive(false);
    }
    public void OnButtonClick1()
    {
        ability.wasUpgraded = true;
        mortargo.SetActive(true);
        mortargoAbility.SetActive(true);
        canvas.SetActive(false);
    }
    public void OnButtonClick2()
    {
        bnner.SetActive(true);
        canvas.SetActive(false);
    }
}
