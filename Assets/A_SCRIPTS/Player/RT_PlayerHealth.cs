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
    [SerializeField] private float _vignetteMinIntensity = 0f;

    [Header("Shader Daño Barco")]
    [SerializeField] private Renderer _shipRenderer;
    [SerializeField] private string _grietaProperty = "_grietaStrenght";
    [SerializeField] private string _normalProperty = "_NormalStrenght";
    [SerializeField] private float _maxGrietaStrenght = 1f;
    [SerializeField] private float _maxNormalStrenght = 1.5f;
    [SerializeField, Range(0f, 1f)] private float _healthPercentToStartCracks = 0.9f;
    [SerializeField] private AnimationCurve _crackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Vignette por vida baja")]
    [SerializeField, Range(0f, 1f)] private float _healthPercentToShowVignette = 0.5f;
    [SerializeField] private float _lowHealthCurve = 2f;

    [Header("Latido al recibir daño")]
    [SerializeField] private float _damagePulseExtraIntensity = 0.45f;
    [SerializeField] private float _damagePulseInDuration = 0.06f;
    [SerializeField] private float _damagePulseOutDuration = 0.25f;

    [Header("VFX Destrucción")]
    [SerializeField] private GameObject _vfxDestroyed;
    [SerializeField] private float _vidaParaActivarFuego = 30f;

    private Material _shipMaterial;
    private Coroutine _vignettePulseRoutine;

    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();

        if (_shipRenderer != null)
            _shipMaterial = _shipRenderer.material;

        ResetVisuals();
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _stats.currentHealth -= amount;
        _stats.currentHealth = Mathf.Max(_stats.currentHealth, 0f);

        UpdateVisuals();
        PlayDamageVignettePulse();

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

        UpdateVisuals();
    }

    private IEnumerator DeathRoutine()
    {
        OnDeath?.Invoke();

        yield return new WaitForSeconds(1f);

        Debug.Log("Player muerto");

        UpdateVisuals();
    }

    public void ResetHealth()
    {
        _isDead = false;
        _stats.currentHealth = _stats.maxHealth;

        ResetVisuals();
    }

    private void UpdateVisuals()
    {
        UpdateVignette();
        UpdateDamageShader();
        UpdateDestroyedVFX();
    }

    private void UpdateVignette()
    {
        if (_vignetteMaterial == null)
            return;

        float baseIntensity = GetBaseVignetteIntensity();
        _vignetteMaterial.SetFloat(VignetteIntensityID, baseIntensity);
    }

    private float GetBaseVignetteIntensity()
    {
        if (_stats == null || _stats.maxHealth <= 0f)
            return _vignetteMinIntensity;

        float healthNormalized = Mathf.Clamp01(_stats.currentHealth / _stats.maxHealth);

        if (healthNormalized > _healthPercentToShowVignette)
            return _vignetteMinIntensity;

        float lowHealthPercent = 1f - (healthNormalized / _healthPercentToShowVignette);
        lowHealthPercent = Mathf.Pow(lowHealthPercent, _lowHealthCurve);

        return Mathf.Lerp(_vignetteMinIntensity, _vignetteMaxIntensity, lowHealthPercent);
    }

    private void UpdateDamageShader()
    {
        if (_shipMaterial == null || _stats == null || _stats.maxHealth <= 0f)
            return;

        float healthPercent = Mathf.Clamp01(_stats.currentHealth / _stats.maxHealth);

        float damagePercent = Mathf.InverseLerp(
            _healthPercentToStartCracks,
            0f,
            healthPercent
        );

        damagePercent = Mathf.Clamp01(damagePercent);
        damagePercent = _crackCurve.Evaluate(damagePercent);

        float grietaValue = damagePercent * _maxGrietaStrenght;
        float normalValue = damagePercent * _maxNormalStrenght;

        _shipMaterial.SetFloat(_grietaProperty, grietaValue);
        _shipMaterial.SetFloat(_normalProperty, normalValue);
    }

    private void PlayDamageVignettePulse()
    {
        if (_vignetteMaterial == null)
            return;

        if (_vignettePulseRoutine != null)
            StopCoroutine(_vignettePulseRoutine);

        _vignettePulseRoutine = StartCoroutine(DamageVignettePulseRoutine());
    }

    private IEnumerator DamageVignettePulseRoutine()
    {
        float baseIntensity = GetBaseVignetteIntensity();
        float pulseIntensity = baseIntensity + _damagePulseExtraIntensity;

        pulseIntensity = Mathf.Clamp(
            pulseIntensity,
            _vignetteMinIntensity,
            _vignetteMaxIntensity
        );

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / _damagePulseInDuration;

            float value = Mathf.Lerp(baseIntensity, pulseIntensity, t);
            _vignetteMaterial.SetFloat(VignetteIntensityID, value);

            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / _damagePulseOutDuration;

            baseIntensity = GetBaseVignetteIntensity();

            float value = Mathf.Lerp(pulseIntensity, baseIntensity, t);
            _vignetteMaterial.SetFloat(VignetteIntensityID, value);

            yield return null;
        }

        _vignetteMaterial.SetFloat(VignetteIntensityID, GetBaseVignetteIntensity());
        _vignettePulseRoutine = null;
    }

    private void UpdateDestroyedVFX()
    {
        if (_vfxDestroyed == null || _stats == null)
            return;

        _vfxDestroyed.SetActive(_stats.currentHealth <= _vidaParaActivarFuego);
    }

    private void ResetVisuals()
    {
        if (_vignettePulseRoutine != null)
        {
            StopCoroutine(_vignettePulseRoutine);
            _vignettePulseRoutine = null;
        }

        if (_vignetteMaterial != null)
            _vignetteMaterial.SetFloat(VignetteIntensityID, _vignetteMinIntensity);

        if (_shipMaterial != null)
        {
            _shipMaterial.SetFloat(_grietaProperty, 0f);
            _shipMaterial.SetFloat(_normalProperty, 0f);
        }

        if (_vfxDestroyed != null)
            _vfxDestroyed.SetActive(false);
    }
}