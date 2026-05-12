using UnityEngine;

public class RT_CannonData 
{
    public float bulletSpeed;
    public float verticalArc;
    public int shotsPerBurst;
    public float timeBetweenShots;
    public float cooldown;
    public float damage;
    public float dropDelay;

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
        dropDelay = so.dropDelay;

        waterSplashVFX = so.waterSplash;
        explosionVFX = so.explosionVfx;
    }
}
