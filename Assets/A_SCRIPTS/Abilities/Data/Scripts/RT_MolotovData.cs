using UnityEngine;

public class RT_MolotovData
{
    public GameObject molotovPrefab;
    public LayerMask damageLayers;
    public float arcHeight;
    public float projectileSpeed;
    public float damage;
    public float launchInterval;
    public float detectionRadius;
    public float explosionRadius;
    public GameObject explosionVFX;
    public GameObject fireZonePrefab;
    public float fireDuration;
    public float fireDamagePerSecond;
    public float fireRadius;
    public float burstInterval;
    public int burstCount;

    public RT_MolotovData(SO_MolotovData so)
    {
        molotovPrefab = so.molotovPrefab;
        arcHeight = so.arcHeight;
        projectileSpeed = so.projectileSpeed;
        damage = so.damage;
        launchInterval = so.launchInterval;
        detectionRadius = so.detectionRadius;
        explosionRadius = so.explosionRadius;
        explosionVFX = so.explosionVFX;
        fireZonePrefab = so.fireZonePrefab;
        fireDuration = so.fireDuration;
        fireDamagePerSecond = so.fireDamagePerSecond;
        fireRadius = so.fireRadius;
        damageLayers = so.damageLayers;
        burstInterval = so.burstInterval;
        burstCount = so.burstCount;
    }
}