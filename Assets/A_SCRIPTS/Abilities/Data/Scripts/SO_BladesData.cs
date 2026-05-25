using UnityEngine;

[CreateAssetMenu(fileName = "SO_BladesData", menuName = "Abilities/Blades Data")]
public class SO_BladesData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject bladePrefab;

    [Header("Orbit")]
    public float orbitRadius = 6f;
    public float orbitSpeed = 130f;

    [Header("Erratic Movement")]
    public float erraticAmount = 2.5f;    // cuanto se desvía del círculo
    public float radiusVariation = 2f;    // variación del radio
    public float noiseSpeed = 1.2f;       // velocidad del ruido

    [Header("Damage")]
    public float damage = 15f;
    public float damageCooldownPerEnemy = 0.8f;

    [Header("Blades")]
    public int bladeCount = 1;
}