using UnityEngine;

public class RT_EnemyStats : MonoBehaviour
{

    //RunetimeStats Enemigo
    public float currentHealth;
    public float maxHealth;
    public float fireRate;
    [HideInInspector]public GameObject woodExplosion;

    public RT_EnemyStats(SO_EnemyData baseData)
    {
        maxHealth = baseData.maxHealth;
        woodExplosion = baseData.woodExplosionVFX;
        fireRate = baseData.fireRate;
        currentHealth = maxHealth;
        
    }

    public void Reset()
    {
        currentHealth = maxHealth;
    }
}
