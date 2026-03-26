using UnityEngine;

public class RT_CannonData : MonoBehaviour
{
    public float bulletSpeed;
    public float verticalArc;
    public int shotsPerBurst;
    public float timeBetweenShots;
    public float cooldown;
    public float damage;

    public GameObject waterSplashVFX;
    public GameObject explosionVFX;

    public RT_CannonData(SO_CannonData so)
    {
        bulletSpeed = so.bulletSpeed;
        verticalArc = so.verticalArc;
        shotsPerBurst = so.shotsPerBurst;
        timeBetweenShots = so.timeBetweenShots;
        cooldown = so.cooldown;
        damage = so.damage;

        waterSplashVFX = so.waterSplash;
        explosionVFX = so.explosionVfx;
    }
}
