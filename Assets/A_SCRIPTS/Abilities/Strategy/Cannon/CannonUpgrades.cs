using UnityEngine;

public class CannonUpgrades : MonoBehaviour
{

    RT_CannonData _rtData;


    //setup para tener la data del cannon
    public void Setup(RT_CannonData cannonData)
    {
        _rtData = cannonData;   
    }

    public void AddCharge()
    {
        _rtData.shotsPerBurst = 2;
    }

    public void IncreaseBulletSize()
    {
        _rtData.bulletPrefab.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
    }


}
