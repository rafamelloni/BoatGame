using UnityEngine;

public class RT_CannonData : MonoBehaviour
{
    public float bulletSpeed;
    public float verticalArc;
    public int shotsPerBurst;
    public float timeBetweenShots;
    public float cooldown;

    public RT_CannonData(SO_CannonData so)
    {
        bulletSpeed = so.bulletSpeed;
        verticalArc = so.verticalArc;
        shotsPerBurst = so.shotsPerBurst;
        timeBetweenShots = so.timeBetweenShots;
        cooldown = so.cooldown;
    }
}
