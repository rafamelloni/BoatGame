using UnityEngine;

[CreateAssetMenu(fileName = "SO_MolotovData", menuName = "Scriptable Objects/Abilities/Molotov Data")]
public class SO_MolotovData : ScriptableObject
{
    [Header("Projectile")]
    public GameObject molotovPrefab;
    public float arcHeight = 5f;
    public float projectileSpeed = 8f;
    public float damage = 20f;

    [Header("Launch")]
    public float launchInterval = 5f;
    public float detectionRadius = 20f;

    [Header("Explosion")]
    public float explosionRadius = 3f;
    public GameObject explosionVFX;

    [Header("Fire Zone")]
    public GameObject fireZonePrefab;
    public LayerMask damageLayers;
    public float fireDuration = 4f;
    public float fireDamagePerSecond = 5f;
    public float fireRadius = 2f;

    [Header("Abuility Molotov")]
    public float burstInterval = 10f;
    public int burstCount = 6;
}