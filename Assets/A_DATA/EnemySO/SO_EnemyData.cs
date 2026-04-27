using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/Data")]
public class SO_EnemyData : ScriptableObject
{
    public float maxHealth;
    public float fireRate;
    public GameObject woodExplosionVFX;

    [Header("Bullet")]
    public float bulletSpeed = 15f;
    public float bulletVerticalArc = 1.2f;
    public float lifeTime = 3f;
    public float damage =7f;
    [Header("VFX BULLET")]
    public GameObject explosionVfx;
    public GameObject waterSplash;
    public TrailRenderer trasilRederer;

}
