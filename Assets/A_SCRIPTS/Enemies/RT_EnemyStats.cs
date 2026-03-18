using UnityEngine;

public class RT_EnemyStats : MonoBehaviour
{

    //RunetimeStats Enemigo
    public float currentHealth;
    public float maxHealth;

    public RT_EnemyStats(SO_EnemyData baseData)
    {
        maxHealth = baseData.maxHealth;
        currentHealth = maxHealth;
    }

    public void Reset()
    {
        currentHealth = maxHealth;
    }
}
