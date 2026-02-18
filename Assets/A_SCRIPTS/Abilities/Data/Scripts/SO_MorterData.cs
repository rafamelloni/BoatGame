using UnityEngine;

[CreateAssetMenu(fileName = "SO_MortarData", menuName = "Scriptable Objects/Abilities/Mortar Data")]
public class SO_MorterData : ScriptableObject
{
    [Header("Cooldown")]
    public float cooldown = 1.5f;

    [Header("Visual Projectile")]
    public GameObject visualProjectilePrefab;
    public float visualShootForce = 15f;
    public float visualLifetime = 1.2f;

    [Header("Real Projectile")]
    public GameObject realProjectilePrefab;
    public float backDistance = 8f;
    public float height = 12f;
    public float randomRadius = 2f;

    [Header("VFX")]
    public GameObject explosionVfx;
    public GameObject circleVfx;
}
