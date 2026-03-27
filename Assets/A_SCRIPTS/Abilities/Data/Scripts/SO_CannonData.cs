using UnityEngine;

[CreateAssetMenu(fileName = "SO_CannonsData", menuName = "Scriptable Objects/Abilities/Cannons Data")]
public class SO_CannonData : ScriptableObject
{
    [Header("Bullet")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float verticalArc = 0.25f;
    public float damage = 15f;

    [Header("Burst")]
    public int shotsPerBurst = 2;
    public float timeBetweenShots = 0.25f;

    [Header("Cooldown")]
    public float cooldown = 1.5f;

    [Header("VFX BULLET")]
    public GameObject explosionVfx;
    public  GameObject waterSplash;
    public TrailRenderer trasilRederer;



}
