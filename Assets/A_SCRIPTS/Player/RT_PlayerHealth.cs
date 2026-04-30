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

    //vignettetakedaage
    private static readonly int DamageFlashID = Shader.PropertyToID("_VignetteIntensity");
    [SerializeField] private Material _damageMaterial;

    [SerializeField] GameObject _vfxDestroyed;
    [SerializeField] float _vidaParaActivarFuego = 30f;


    void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
        _vignetteMaterial.SetFloat(VignetteIntensityID, 0f);
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        _stats.currentHealth -= amount;
        _stats.currentHealth = Mathf.Max(_stats.currentHealth, 0f);
        if (_damageMaterial != null)
            StartCoroutine(DamageFlash());
        UpdateVignette();
        if (_stats.currentHealth <= 0f)
        {
            _isDead = true;
            StartCoroutine(DeathRoutine());
        }
    }

    private IEnumerator DamageFlash()
    {
        float duration = 0.15f;
        float t = 0f;

        // sube a 0.9 rápido
        while (t < 1f)
        {
            t += Time.deltaTime / 0.05f;
            _damageMaterial.SetFloat(DamageFlashID, Mathf.Lerp(0f, 0.9f, t));
            yield return null;
        }

        t = 0f;

        // baja gradual
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            _damageMaterial.SetFloat(DamageFlashID, Mathf.Lerp(0.9f, 0f, t));
            yield return null;
        }

        _damageMaterial.SetFloat(DamageFlashID, 0f);
    }

    public void Heal(float amount)
    {
        if (_isDead) return;
        _stats.currentHealth = Mathf.Min(_stats.currentHealth + amount, _stats.maxHealth);
        UpdateVignette();
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
        if (_stats.currentHealth <= _vidaParaActivarFuego)
        {
            _vfxDestroyed.SetActive(true);
        }
        else
        {
            _vfxDestroyed.SetActive(false);
        }
    }

    void ResetVignette()
    {
        if (_vignetteMaterial == null) return;
        _vignetteMaterial.SetFloat(VignetteIntensityID, 0f);
        _vfxDestroyed.SetActive(false);
    }
}
