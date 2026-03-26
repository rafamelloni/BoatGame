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
    public GameObject pos;
    public float backDistance = 8f;
    public float height = 12f;
    public float randomRadius = 2f;
    public float damage = 35f;
    public float fallingSpeed = 10f;

    [Header("VFX")]
    public GameObject explosionVFX;
    public GameObject waterSplashVFX;
    public GameObject circleVfx;
}
