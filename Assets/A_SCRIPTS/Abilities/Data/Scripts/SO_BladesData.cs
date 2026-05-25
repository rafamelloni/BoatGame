using UnityEngine;

[CreateAssetMenu(fileName = "SO_BladesData", menuName = "Abilities/Blades Data")]
public class SO_BladesData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject bladePrefab;
    public GameObject burstProjectilePrefab;

    [Header("Orbit")]
    public float orbitRadius = 6f;
    public float orbitSpeed = 130f;

    [Header("Erratic Movement")]
    public float erraticAmount = 2.5f;
    public float radiusVariation = 2f;
    public float noiseSpeed = 1.2f;

    [Header("Damage")]
    public float damage = 15f;
    public float damageCooldownPerEnemy = 0.8f;

    [Header("Blades")]
    public int bladeCount = 1;

    [Header("Burst")]
    public int burstCount = 10;
    public float burstSpeed = 12f;
    public float burstMaxDistance = 15f;
    public float burstDamage = 30f;
    public float burstInterval = 5f;

    [Header("Detection")]
    public LayerMask enemyLayers;
}