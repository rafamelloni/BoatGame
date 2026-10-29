using System;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyHealth : MonoBehaviour, IDamageable, IHealthBarSource
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

    // Para debug del bug "spawnean muertos": si muere muy rápido después de revivir, avisa quién lo mató
    [SerializeField] private bool _logEarlyDeaths = true;
    private float _revivedAt = -999f;

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
        // Un enemigo apagado (muerto o guardado en el pool) no puede recibir daño.
        // Antes una explosión con varios colliders del mismo enemigo, o un burn,
        // le podía seguir pegando después de muerto.
        if (_rtData == null || IsDying || !gameObject.activeInHierarchy) return;

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
        if (_logEarlyDeaths && Time.time - _revivedAt < 1.5f)
            Debug.LogWarning($"[EnemyHealth] {name} murió {Time.time - _revivedAt:0.00}s después de spawnear. Quién lo mató:\n{Environment.StackTrace}", this);

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
        _revivedAt = Time.time;

        // Si murió prendido fuego, el BurnStatus quedaba pegado con su VFX
        var burn = GetComponent<BurnStatus>();
        if (burn != null) burn.Clear();
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
