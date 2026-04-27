using UnityEngine;
using System.Collections;

public class RT_PlayerHealth : MonoBehaviour
{
    private RT_PlayerStats _stats;
    private bool _isDead = false;

    public event System.Action OnDeath;

    void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        _stats.currentHealth -= amount;
        _stats.currentHealth = Mathf.Max(_stats.currentHealth, 0f);

        if (_stats.currentHealth <= 0f)
        {
            _isDead = true;
            StartCoroutine(DeathRoutine());
        }
    }

    public void Heal(float amount)
    {
        if (_isDead) return;
        _stats.currentHealth = Mathf.Min(_stats.currentHealth + amount, _stats.maxHealth);
    }

    private IEnumerator DeathRoutine()
    {
        OnDeath?.Invoke();
        yield return new WaitForSeconds(1f);
        Debug.Log("Player muerto");
    }

    public void ResetHealth()
    {
        _isDead = false;
        _stats.currentHealth = _stats.maxHealth;
    }
}
