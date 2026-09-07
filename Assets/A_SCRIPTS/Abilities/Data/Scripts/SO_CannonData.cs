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

    [Header("Munición Incendiaria (BurnShot)")]
    public float burnDamagePerTick = 5f;
    public float burnTickInterval = 0.5f;
    public float burnDuration = 3f;
    public GameObject burnVfxPrefab;
    public Vector3 burnVfxOffset = new Vector3(0f, 1f, 0f);

    [Header("Fuego en el Piso (GroundFire)")]
    public GameObject groundFireZonePrefab;
    public float groundFireRadius = 3f;
    public float groundFireDuration = 4f;
    public float groundFireDamagePerSecond = 10f;

    [Header("VFX BULLET")]
    public GameObject explosionVfx;
    public GameObject waterSplash;
    public TrailRenderer trasilRederer;

    [Header("Parábola")]
    public float launchSpeed = 25f;

}