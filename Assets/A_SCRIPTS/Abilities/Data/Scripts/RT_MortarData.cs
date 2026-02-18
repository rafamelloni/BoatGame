using UnityEngine;

public class RT_MortarData : MonoBehaviour
{
    public float cooldown;
    public float visualShootForce;
    public float visualLifetime;

    public float backDistance;
    public float height;
    public float randomRadius;

    public RT_MortarData(SO_MorterData so)
    {
        cooldown = so.cooldown;
        visualShootForce = so.visualShootForce;
        visualLifetime = so.visualLifetime;

        backDistance = so.backDistance;
        height = so.height;
        randomRadius = so.randomRadius;
    }
}
