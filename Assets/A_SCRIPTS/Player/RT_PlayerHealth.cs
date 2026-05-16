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

    [Header("Modelos por vida")]
    [SerializeField] private GameObject _ship100;
    [SerializeField] private GameObject _ship75;
    [SerializeField] private GameObject _ship50;
    [SerializeField] private GameObject _ship25;

    [Header("Shader Daño Barco")]
    [SerializeField] private Renderer _shipRenderer;
    [SerializeField] private string _grietaProperty = "_grietaStrenght";
    [SerializeField] private string _normalProperty = "_NormalStrenght";

    [Header("Valores de grietas por modelo")]
    [SerializeField] private float _grieta100 = 0f;
    [SerializeField] private float _normal100 = 0f;

    [SerializeField] private float _grieta75 = 0.25f;
    [SerializeField] private float _normal75 = 0.4f;

    [SerializeField] private float _grieta50 = 0.55f;
    [SerializeField] private float _normal50 = 0.9f;

    [SerializeField] private float _grieta25 = 1f;
    [SerializeField] private float _normal25 = 1.5f;

    [Header("Shader Daño Velas")]
    [SerializeField] private Renderer _sailRenderer;
    [SerializeField] private string _sailBreakProperty = "_BreakAmount";
    [SerializeField] private float _minSailBreakAmount = -0.1f;
    [SerializeField] private float _maxSailBreakAmount = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _healthPercentToStartSailDamage = 0.9f;
    [SerializeField] private AnimationCurve _sailDamageCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fuego en velas por vida")]
    [SerializeField] private GameObject _fireVela50;
    [SerializeField] private GameObject _fireVela25;

    [Header("Vignette por vida baja")]
    [SerializeField, Range(0f, 1f)] private float _healthPercentToShowVignette = 0.5f;
    [SerializeField] private float _lowHealthCurve = 2f;

    [Header("Vignette por Heal")]
    [SerializeField] private Material _lifeVignette;

    [Header("Latido al recibir daño")]
    [SerializeField] private float _damagePulseExtraIntensity = 0.45f;
    [SerializeField] private float _damagePulseInDuration = 0.06f;
    [SerializeField] private float _damagePulseOutDuration = 0.25f;

    [Header("VFX Destrucción")]
    [SerializeField] private GameObject _vfxDestroyed;
    [SerializeField] private float _vidaParaActivarFuego = 30f;

    private Material _shipMaterial;
    private Material _sailMaterial;
    private Coroutine _vignettePulseRoutine;

    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();

        if (_shipRenderer != null)
            _shipMaterial = _shipRenderer.material;

        if (_sailRenderer != null)
            _sailMaterial = _sailRenderer.material;

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

        if (_lifeVignette != null)
        {
            _lifeVignette.SetFloat("_VignetteIntensity", 1.35f);
            StartCoroutine(ResetHealVignette());
        }

        UpdateVisuals();
    }

    private IEnumerator ResetHealVignette()
    {
        yield return new WaitForSeconds(1.5f);

        if (_lifeVignette != null)
            _lifeVignette.SetFloat("_VignetteIntensity", 0f);
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
        UpdateShipModelByHealth();
        UpdateDamageShader();
        UpdateSailDamageShader();
        UpdateSailFireByHealth();
        UpdateDestroyedVFX();
    }

    private void UpdateShipModelByHealth()
    {
        if (_stats == null || _stats.maxHealth <= 0f)
            return;

        float healthPercent = (_stats.currentHealth / _stats.maxHealth) * 100f;

        if (_ship100 != null) _ship100.SetActive(false);
        if (_ship75 != null) _ship75.SetActive(false);
        if (_ship50 != null) _ship50.SetActive(false);
        if (_ship25 != null) _ship25.SetActive(false);

        if (healthPercent > 75f)
        {
            if (_ship100 != null) _ship100.SetActive(true);
        }
        else if (healthPercent > 50f)
        {
            if (_ship75 != null) _ship75.SetActive(true);
        }
        else if (healthPercent > 25f)
        {
            if (_ship50 != null) _ship50.SetActive(true);
        }
        else
        {
            if (_ship25 != null) _ship25.SetActive(true);
        }

        UpdateCurrentShipRenderer();
    }

    private void UpdateCurrentShipRenderer()
    {
        if (_ship100 != null && _ship100.activeSelf)
            _shipRenderer = _ship100.GetComponentInChildren<Renderer>();

        else if (_ship75 != null && _ship75.activeSelf)
            _shipRenderer = _ship75.GetComponentInChildren<Renderer>();

        else if (_ship50 != null && _ship50.activeSelf)
            _shipRenderer = _ship50.GetComponentInChildren<Renderer>();

        else if (_ship25 != null && _ship25.activeSelf)
            _shipRenderer = _ship25.GetComponentInChildren<Renderer>();

        if (_shipRenderer != null)
            _shipMaterial = _shipRenderer.material;
    }

    private void UpdateDamageShader()
    {
        if (_shipMaterial == null || _stats == null || _stats.maxHealth <= 0f)
            return;

        float healthPercent = (_stats.currentHealth / _stats.maxHealth) * 100f;

        if (healthPercent > 75f)
        {
            SetShipCrackValues(_grieta100, _normal100);
        }
        else if (healthPercent > 50f)
        {
            SetShipCrackValues(_grieta75, _normal75);
        }
        else if (healthPercent > 25f)
        {
            SetShipCrackValues(_grieta50, _normal50);
        }
        else
        {
            SetShipCrackValues(_grieta25, _normal25);
        }
    }

    private void SetShipCrackValues(float grietaValue, float normalValue)
    {
        _shipMaterial.SetFloat(_grietaProperty, grietaValue);
        _shipMaterial.SetFloat(_normalProperty, normalValue);
    }

    private void UpdateSailDamageShader()
    {
        if (_sailMaterial == null)
            return;

        float damagePercent = GetDamagePercent(_healthPercentToStartSailDamage, _sailDamageCurve);

        float sailBreakValue = Mathf.Lerp(
            _minSailBreakAmount,
            _maxSailBreakAmount,
            damagePercent
        );

        _sailMaterial.SetFloat(_sailBreakProperty, sailBreakValue);
    }

    private void UpdateSailFireByHealth()
    {
        if (_stats == null || _stats.maxHealth <= 0f)
            return;

        float healthPercent = (_stats.currentHealth / _stats.maxHealth) * 100f;

        if (_fireVela50 != null)
            _fireVela50.SetActive(healthPercent <= 50f);

        if (_fireVela25 != null)
            _fireVela25.SetActive(healthPercent <= 25f);
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

    private float GetDamagePercent(float startHealthPercent, AnimationCurve curve)
    {
        if (_stats == null || _stats.maxHealth <= 0f)
            return 0f;

        float healthPercent = Mathf.Clamp01(_stats.currentHealth / _stats.maxHealth);

        float damagePercent = Mathf.InverseLerp(
            startHealthPercent,
            0f,
            healthPercent
        );

        damagePercent = Mathf.Clamp01(damagePercent);

        if (curve != null)
            damagePercent = curve.Evaluate(damagePercent);

        return damagePercent;
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

        UpdateShipModelByHealth();

        if (_vignetteMaterial != null)
            _vignetteMaterial.SetFloat(VignetteIntensityID, _vignetteMinIntensity);

        UpdateDamageShader();

        if (_sailMaterial != null)
            _sailMaterial.SetFloat(_sailBreakProperty, _minSailBreakAmount);

        if (_fireVela50 != null)
            _fireVela50.SetActive(false);

        if (_fireVela25 != null)
            _fireVela25.SetActive(false);

        if (_vfxDestroyed != null)
            _vfxDestroyed.SetActive(false);
    }
}
