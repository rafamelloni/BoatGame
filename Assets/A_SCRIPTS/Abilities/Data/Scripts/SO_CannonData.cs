using UnityEngine;

[CreateAssetMenu(fileName = "SO_CannonsData", menuName = "Scriptable Objects/Abilities/Cannons Data")]
public class SO_CannonData : ScriptableObject
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float verticalArc = 0.25f;
    public float damage = 15f;
    public float dropDelay = 0.5f;

    [Header("Burst")]
    public int shotsPerBurst = 2;
    public float timeBetweenShots = 0.25f;

    [Header("Cooldown")]
    public float cooldown = 1.5f;

    [Header("Explosion")]
    public float explosionRadius = 2f;

    [Header("Bala Cargada (ChargedShot)")]
    public int chargedShotInterval = 6;
    public float chargedBulletScale = 2.2f;
    public float chargedDamageMultiplier = 2f;
    public float chargedExplosionMultiplier = 1.5f;
    public float chargedVfxScale = 1.8f;

    [Header("VFX BULLET")]
    public GameObject explosionVfx;
    public GameObject waterSplash;
    public TrailRenderer trasilRederer;

    [Header("Parábola")]
    public float launchSpeed = 25f;

}