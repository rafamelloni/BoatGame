using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LastStand : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float healthThreshold = 0.30f;
    [SerializeField] private float cooldown = 45f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("VFX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject shockwaveVFX;
    [SerializeField] private float shockwaveVFXDuration = 2f;

    [Header("Shockwave Full Screen")]
    [SerializeField] private ScriptableRendererFeature shockwaveFeature;
    [SerializeField] private Material shockwaveMaterial;
    [SerializeField] private float shockwaveDuration = 1f;
    [SerializeField] private float shockwaveMagnitude = 0.03f;
    [SerializeField] private float shockwaveSize = 0.04f;

    private bool _isUnlocked = false;
    private bool _onCooldown = false;
    private float _cooldownTimer = 0f;

    private RT_PlayerHealth _playerHealth;
    private RT_PlayerStats _stats;

    private Coroutine _shockwaveCoroutine;
    private Coroutine _vfxCoroutine;

    public float CooldownProgress => _onCooldown ? Mathf.Clamp01(_cooldownTimer / cooldown) : 0f;

    private void Awake()
    {
        _playerHealth = GetComponent<RT_PlayerHealth>();
        _stats = GetComponent<RT_PlayerStats>();

        TurnOffShockwave();

        if (shockwaveVFX != null)
            shockwaveVFX.SetActive(false);
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDamage += CheckThreshold;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDamage -= CheckThreshold;
    }

    public void Unlock() => _isUnlocked = true;

    public void DebugActivate() => Activate();

    private void CheckThreshold()
    {
        if (!_isUnlocked || _onCooldown) return;
        if (_stats == null) return;

        float ratio = _stats.currentHealth / _stats.maxHealth;

        if (ratio <= healthThreshold)
            Activate();
    }

    private void Activate()
    {
        if (_onCooldown) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, 999f, enemyLayer);

        foreach (var e in enemies)
        {
            if (e.gameObject.CompareTag("DashBoss")) continue; 
            EnemyHealth health = e.GetComponent<EnemyHealth>();

            if (health != null)
                health.TakeDamage(999f);
        }

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        PlayShockwaveVFX();
        PlayShockwave();

        StartCoroutine(CooldownRoutine());
    }

    private void PlayShockwaveVFX()
    {
        if (shockwaveVFX == null) return;

        if (_vfxCoroutine != null)
            StopCoroutine(_vfxCoroutine);

        shockwaveVFX.SetActive(false);
        shockwaveVFX.SetActive(true);

        _vfxCoroutine = StartCoroutine(DisableShockwaveVFX());
    }

    private IEnumerator DisableShockwaveVFX()
    {
        yield return new WaitForSeconds(shockwaveVFXDuration);

        if (shockwaveVFX != null)
            shockwaveVFX.SetActive(false);

        _vfxCoroutine = null;
    }

    private void PlayShockwave()
    {
        if (_shockwaveCoroutine != null)
            StopCoroutine(_shockwaveCoroutine);

        _shockwaveCoroutine = StartCoroutine(ShockwaveRoutine());
    }

    private IEnumerator ShockwaveRoutine()
    {
        SetShockwaveFeature(true);

        if (shockwaveMaterial != null)
        {
            shockwaveMaterial.SetFloat("_Progress", 0f);
            shockwaveMaterial.SetFloat("_Magnitude", shockwaveMagnitude);
            shockwaveMaterial.SetFloat("_Size", shockwaveSize);
        }

        float timer = 0f;

        while (timer < shockwaveDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / shockwaveDuration);

            if (shockwaveMaterial != null)
                shockwaveMaterial.SetFloat("_Progress", progress);

            yield return null;
        }

        TurnOffShockwave();

        _shockwaveCoroutine = null;
    }

    private void TurnOffShockwave()
    {
        if (shockwaveMaterial != null)
        {
            shockwaveMaterial.SetFloat("_Progress", 1f);
            shockwaveMaterial.SetFloat("_Magnitude", 0f);
            shockwaveMaterial.SetFloat("_Size", 0f);
        }

        SetShockwaveFeature(false);
    }

    private void SetShockwaveFeature(bool enabled)
    {
        if (shockwaveFeature == null) return;

        shockwaveFeature.SetActive(enabled);
    }

    private IEnumerator CooldownRoutine()
    {
        _onCooldown = true;
        _cooldownTimer = 0f;

        while (_cooldownTimer < cooldown)
        {
            _cooldownTimer += Time.deltaTime;
            yield return null;
        }

        _cooldownTimer = 0f;
        _onCooldown = false;
    }
}