using UnityEngine;

[CreateAssetMenu(fileName = "SO_CannonsData", menuName = "Scriptable Objects/Abilities/Cannons Data")]
public class SO_CannonData : ScriptableObject
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    [Tooltip("Escala el ángulo de la parábola. 1 = arco normal, menos de 1 = tiro más derecho y más rápido, más de 1 = lob más alto.")]
    [Range(0f, 2f)]
    public float verticalArc = 0.25f;
    public float damage = 15f;
    public float dropDelay = 0.5f;

    [Header("Burst")]
    public int shotsPerBurst = 2;
    [Tooltip("Pausa mínima entre bala y bala del cargador (y entre balas de una ráfaga)")]
    public float timeBetweenShots = 0.25f;

    [Header("Munición")]
    [Tooltip("Balas en el cargador. Se gasta 1 por disparo; al vaciarse recarga (cooldown).")]
    public int maxAmmo = 2;

    [Header("Cooldown")]
    [Tooltip("Tiempo de recarga del cargador completo")]
    public float cooldown = 1.5f;

    [Header("Explosion")]
    public float explosionRadius = 2f;

    [Header("Bala Cargada (ChargedShot)")]
    public int chargedShotInterval = 6;
    public float chargedBulletScale = 2.2f;
    public float chargedDamageMultiplier = 2f;
    public float chargedExplosionMultiplier = 1.5f;
    public float chargedVfxScale = 1.8f;

    [Header("Munici�n Incendiaria (BurnShot)")]
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

    [Header("Andanada (Broadside)")]
    [Tooltip("Balas por costado cuando se vacia el cargador")]
    public int broadsideShotsPerSide = 3;
    [Tooltip("A que distancia del barco caen las balas de la andanada")]
    public float broadsideDistance = 10f;
    [Tooltip("Separacion entre balas a lo largo del casco")]
    public float broadsideSpacing = 3f;
    [Tooltip("Espera despues de la ultima bala antes de soltar la andanada")]
    public float broadsideDelay = 0.2f;
    [Tooltip("Tiempo entre bala y bala de la andanada (0 = todas juntas)")]
    public float broadsideInterval = 0.06f;

    [Header("VFX BULLET")]
    public GameObject explosionVfx;
    public GameObject waterSplash;
    public TrailRenderer trasilRederer;

    [Header("Par�bola")]
    public float launchSpeed = 25f;

}