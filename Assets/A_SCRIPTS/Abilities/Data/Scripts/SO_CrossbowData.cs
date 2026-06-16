using UnityEngine;

[CreateAssetMenu(fileName = "SO_CrossbowData", menuName = "Abilities/Crossbow Data")]
public class SO_CrossbowData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject arrowPrefab;

    [Header("Stats")]
    public float damage = 30f;
    public float cooldown = 1.2f;
    public float arrowSpeed = 18f;
    public float detectionRadius = 20f;

    [Header("Burst (T4)")]
    public int burstArrowCount = 3;
    public float burstSpreadAngle = 20f;

    [Header("Detection")]
    public LayerMask enemyLayers;

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    [Header("VFX BULLET")]
    public GameObject explosionVfx;
    public GameObject waterVfx;

}