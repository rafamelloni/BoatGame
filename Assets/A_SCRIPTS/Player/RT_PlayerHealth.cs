using UnityEngine;
using System.Collections;

public class RT_PlayerHealth : MonoBehaviour
{
    private RT_PlayerStats _stats;
    private bool _isDead = false;

    public event System.Action OnDeath;

    [Header("Vignette")]
    [SerializeField] private Material _vignetteMaterial;
    [SerializeField] private float _vignetteMaxIntensity = 1.5f;
    // Intensidad en full salud — ajustá si tu material tiene un valor base distinto de 0
    [SerializeField] private float _vignetteMinIntensity = 0f;
    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");


    void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        _stats.currentHealth -= amount;
        _stats.currentHealth = Mathf.Max(_stats.currentHealth, 0f);
        UpdateVignette();
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
        UpdateVignette();
    }

    public void ResetHealth()
    {
        _isDead = false;
        _stats.currentHealth = _stats.maxHealth; 
        ResetVignette();
    }

    private void UpdateVignette()
    {
        if (_vignetteMaterial == null) return;
        float t = 1f - (_stats.currentHealth / _stats.maxHealth); // 0 = full HP, 1 = muerto
        float intensity = Mathf.Lerp(_vignetteMinIntensity, _vignetteMaxIntensity, t);
        _vignetteMaterial.SetFloat(VignetteIntensityID, intensity);
    }

    void ResetVignette()
    {
        if (_vignetteMaterial == null) return;
        _vignetteMaterial.SetFloat(VignetteIntensityID, 0f);
    }
}
