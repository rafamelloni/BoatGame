using UnityEngine;

public class RT_EnemyStats : MonoBehaviour
{

    //RunetimeStats Enemigo
    public float currentHealth;
    public float maxHealth;
    [HideInInspector]public GameObject woodExplosion;

    public RT_EnemyStats(SO_EnemyData baseData)
    {
        maxHealth = baseData.maxHealth;
        woodExplosion = baseData.woodExplosionVFX;

        currentHealth = maxHealth;
        
    }

    public void Reset()
    {
        currentHealth = maxHealth;
    }
}
