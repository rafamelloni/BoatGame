using UnityEngine;

public class RT_EnemyStats 
{

    //RunetimeStats Enemigo
    public float currentHealth;
    public float maxHealth;
    public float fireRate;
    [HideInInspector]public GameObject woodExplosion;

    //[[Header("Bullet")]
    public float bulletSpeed;
    public float bulletVerticalArc;
    public float lifeTime ;

    public GameObject waterSplashVFX;
    public GameObject explosionVFX;

    public RT_EnemyStats(SO_EnemyData baseData)
    {
        maxHealth = baseData.maxHealth;
        woodExplosion = baseData.woodExplosionVFX;
        fireRate = baseData.fireRate;
        currentHealth = maxHealth;

        bulletSpeed = baseData.bulletSpeed;
        bulletVerticalArc = baseData.bulletVerticalArc;
        lifeTime = baseData.lifeTime;

        waterSplashVFX = baseData.waterSplash;
        explosionVFX = baseData.explosionVfx;
    }

    public void Reset()
    {
        currentHealth = maxHealth;
    }
}
