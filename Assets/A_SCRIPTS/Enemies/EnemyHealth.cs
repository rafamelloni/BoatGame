using System;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyHealth : MonoBehaviour, IDamageable
{

    //Data
    private RT_EnemyStats _rtData;

    //Notifyers
    public event Action OnDeath;
    public event Action<float> OnDamage; // para UI o feedback


    public void InitializeComponent(SO_EnemyData baseData)
    {
        _rtData = new RT_EnemyStats(baseData);
        print("initialized: " +  _rtData.maxHealth);
    }

    public void ResetHealth()
    {
        _rtData.Reset();
    }

    public void TakeDamage(float damage)
    {
        _rtData.currentHealth -= damage;

        OnDamage?.Invoke(_rtData.currentHealth);

        if (_rtData.currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();


        if (_rtData.woodExplosion != null)
        {
            ParticlePool.Instance.GetParticle(_rtData.woodExplosion, transform.position);

        }
        gameObject.SetActive(false);
    }

    public float GetHealthNormalized()
    {
        return _rtData.currentHealth / _rtData.maxHealth;
    }
    public float GetMaxHealt()
    {
        return _rtData.maxHealth;
    }
    public float GetCurrenHealt()
    {
        print("returned");
        return _rtData.currentHealth;

    }

}
