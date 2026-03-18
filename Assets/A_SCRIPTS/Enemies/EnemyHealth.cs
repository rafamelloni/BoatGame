using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{

    //Data
    [SerializeField]SO_EnemyData _baseData;
    private RT_EnemyStats _rtData;


    //Temporal Vfx
    public GameObject woodExplosion;
    public ParticlePool pool;


    //Notifyers
    public event Action OnDeath;
    public event Action<float> OnDamage; // para UI o feedback

    private void Start()
    {
        Init(_baseData, woodExplosion, pool);
    }

    public void Init(SO_EnemyData baseData, GameObject woodExplosionVFX, ParticlePool pool)
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
        pool.GetParticle(woodExplosion, transform.position);
        gameObject.SetActive(false);
    }

    public float GetHealthNormalized()
    {
        return _rtData.currentHealth / _rtData.maxHealth;
    }

}
