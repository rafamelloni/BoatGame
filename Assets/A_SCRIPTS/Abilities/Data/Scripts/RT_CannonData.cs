using UnityEngine;

public class RT_CannonData
{

    public GameObject bulletPrefab;
    public float bulletSpeed;
    public float verticalArc;
    public int shotsPerBurst;
    public float timeBetweenShots;
    public float cooldown;
    public float damage;
    public float dropDelay;
    public float launchSpeed;
    public float explosionRadius;
    public int chargedShotInterval;
    public float chargedBulletScale;
    public float chargedDamageMultiplier;
    public float chargedExplosionMultiplier;
    public float chargedVfxScale;

    public float burnDamagePerTick;
    public float burnTickInterval;
    public float burnDuration;
    public GameObject burnVfxPrefab;
    public Vector3 burnVfxOffset;

    public GameObject groundFireZonePrefab;
    public float groundFireRadius;
    public float groundFireDuration;
    public float groundFireDamagePerSecond;

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
        explosionRadius = so.explosionRadius;
        chargedShotInterval = so.chargedShotInterval;
        chargedBulletScale = so.chargedBulletScale;
        chargedDamageMultiplier = so.chargedDamageMultiplier;
        chargedExplosionMultiplier = so.chargedExplosionMultiplier;
        chargedVfxScale = so.chargedVfxScale;

        burnDamagePerTick = so.burnDamagePerTick;
        burnTickInterval = so.burnTickInterval;
        burnDuration = so.burnDuration;
        burnVfxPrefab = so.burnVfxPrefab;
        burnVfxOffset = so.burnVfxOffset;

        groundFireZonePrefab = so.groundFireZonePrefab;
        groundFireRadius = so.groundFireRadius;
        groundFireDuration = so.groundFireDuration;
        groundFireDamagePerSecond = so.groundFireDamagePerSecond;

        waterSplashVFX = so.waterSplash;
        explosionVFX = so.explosionVfx;
        bulletPrefab = so.bulletPrefab;
        launchSpeed = so.launchSpeed;
    }
}