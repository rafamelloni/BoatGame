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
    private HitFlash _hitFlash;
    public static event Action<Vector3> OnAnyEnemyDied;
    public static event Action<Vector3, float> OnAnyEnemyDamaged; // posicion + daño real, para damage numbers
    public static event Action<DamageInfo> OnAnyEnemyDamagedInfo;  // igual pero con enemigo, crit, tipo y si lo mató

    public bool IsDying { get; private set; }
    public bool IsInitialized => _rtData != null;

    private void Awake()
    {
        _hitFlash = GetComponent<HitFlash>();
    }
    public void InitializeComponent(SO_EnemyData baseData)
    {
        _rtData = new RT_EnemyStats(baseData);
    }

    public void ResetHealth()
    {
        if (_rtData == null) return;
        _rtData.Reset();
    }

    public void TakeDamage(float damage)
    {
        
        _rtData.currentHealth -= damage;
        OnDamage?.Invoke(_rtData.currentHealth);
        OnAnyEnemyDamaged?.Invoke(transform.position, damage);
        OnAnyEnemyDamagedInfo?.Invoke(new DamageInfo
        {
            target = this,
            position = transform.position,
            damage = damage,
            isCrit = DamageContext.IsCrit,
            kind = DamageContext.Kind,
            killed = _rtData.currentHealth <= 0
        });
        _hitFlash?.Flash();
        if (_rtData.currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        OnAnyEnemyDied?.Invoke(transform.position);
        IsDying = true;
        if (_rtData.woodExplosion != null)
        {
            ParticlePool.Instance.GetParticle(_rtData.woodExplosion, transform.position);
        }
        gameObject.SetActive(false);
        IsDying = false;
    }
    public void Revive()
    {
        gameObject.SetActive(true);
        _rtData.Reset();
        IsDying = false;
    }

    public void SetMaxHealth(float newHealth)
    {
        if (_rtData == null) return;
        _rtData.maxHealth = newHealth;
        _rtData.currentHealth = newHealth;
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
