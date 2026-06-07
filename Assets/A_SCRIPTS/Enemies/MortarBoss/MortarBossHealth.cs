using System;
using UnityEngine;

public class MortarBossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 300f;

    private float _currentHealth;

    public event Action<float> OnHealthChanged; // 0-1 normalizado
    public event Action OnDeath;

    public float HealthPercent => _currentHealth / _maxHealth;
    public bool IsDead => _currentHealth <= 0f;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
       OnHealthChanged?.Invoke(HealthPercent);

        if (_currentHealth <= 0f)
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
            
    }
}