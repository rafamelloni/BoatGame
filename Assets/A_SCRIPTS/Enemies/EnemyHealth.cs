using System;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyHealth : MonoBehaviour, IDamageable
{

    //Data
    private RT_EnemyStats _rtData;

    //Temporal Vfx
    public Coins coins;

    //Notifyers
    public event Action OnDeath;
    public event Action<float> OnDamage; // para UI o feedback


    public void InitializeComponent(SO_EnemyData baseData)
    {
        _rtData = new RT_EnemyStats(baseData);
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
        
        ParticlePool.Instance.GetParticle(_rtData.woodExplosion, transform.position);
        //coins.Init(transform);
        gameObject.SetActive(false);
    }

    public float GetHealthNormalized()
    {
        return _rtData.currentHealth / _rtData.maxHealth;
    }

}
