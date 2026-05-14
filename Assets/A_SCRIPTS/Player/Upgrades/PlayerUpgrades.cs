using UnityEngine;

public class PlayerUpgrades : MonoBehaviour
{
    RT_PlayerStats _rtData;
    public void Setup(RT_PlayerStats statsPlayer)
    {
        _rtData = statsPlayer;
    }

    public void MaxHP()
    {
        _rtData.maxHealth = 300;
    }

    public void Speed()
    {
        _rtData.moveSpeed += 3;
    }

}
