using UnityEngine;
using UnityEngine.UI;

public class UIHealthBarDashBoss : MonoBehaviour
{
    [Header("Vida Boss")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private EnemyHealth _bossHealth;

    [Header("Shader Grietas (BOSS)")]
    [SerializeField] private Renderer _bossRenderer;

    [Header("Propiedades Shader")]
    [SerializeField] private string _grietaStrengthProperty = "_grietaStrength";
    [SerializeField] private string _emissionStrengthProperty = "_EmissionStrength";
    [SerializeField] private string _normalStrengthProperty = "_NormalStrenght";

    [Header("Ajustes Visuales")]
    [SerializeField] private float _curvaGrietas = 0.6f;
    [SerializeField] private float _maxGrietaStrength = 1f;
    [SerializeField] private float _maxEmissionStrength = 2f;
    [SerializeField] private float _maxNormalStrength = 1f;

    [Header("Partículas por daño")]
    [SerializeField] private GameObject[] _damageParticleObjects;
    [SerializeField, Range(0f, 1f)] private float[] _healthThresholds;

    [Header("UI Ship Upgrade")]
    [SerializeField] GameObject _UIShipUpgrade;

    [Header("Spawner")]
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private IslandSpawnManager _islandSpawner;

    private Material _materialInstance;
    private bool[] _activatedParticles;

    private void Awake()
    {
        if (_bossRenderer != null)
            _materialInstance = _bossRenderer.material;

        _activatedParticles = new bool[_damageParticleObjects.Length];

        for (int i = 0; i < _damageParticleObjects.Length; i++)
        {
            if (_damageParticleObjects[i] != null)
                _damageParticleObjects[i].SetActive(false);
        }
    }

    private void OnEnable()
    {
        // resetear partículas
        _activatedParticles = new bool[_damageParticleObjects.Length];
        for (int i = 0; i < _damageParticleObjects.Length; i++)
            if (_damageParticleObjects[i] != null)
                _damageParticleObjects[i].SetActive(false);

        if (_bossHealth != null)
        {
            _bossHealth.OnDamage += UpdateBar;
            _bossHealth.OnDeath += DieBoss;

            if (_bossHealth.GetCurrenHealt() > 0f)
                UpdateBar(_bossHealth.GetCurrenHealt());
        }
        _islandSpawner.ResetIslands();


    }

    private void OnDisable()
    {
        if (_bossHealth != null)
        {
            _bossHealth.OnDamage -= UpdateBar;
            _bossHealth.OnDeath += DieBoss;
        }

        

    }

    private void UpdateBar(float currentHealth)
    {
        float healthNormalized = Mathf.Clamp01(_bossHealth.GetHealthNormalized());

        if (_fillImage != null)
            _fillImage.fillAmount = healthNormalized;

        UpdateCracks(healthNormalized);
        UpdateDamageParticles(healthNormalized);
    }

    private void UpdateCracks(float healthNormalized)
    {
        if (_materialInstance == null)
            return;

        float damagePercent = 1f - healthNormalized;
        float smoothDamage = Mathf.SmoothStep(0f, 1f, damagePercent);

        float crackValue = Mathf.Pow(smoothDamage, _curvaGrietas) * _maxGrietaStrength;
        float emissionValue = Mathf.Pow(smoothDamage, 2.5f) * _maxEmissionStrength;
        float normalValue = Mathf.Pow(smoothDamage, _curvaGrietas) * _maxNormalStrength;

        _materialInstance.SetFloat(_grietaStrengthProperty, crackValue);
        _materialInstance.SetFloat(_emissionStrengthProperty, emissionValue);
        _materialInstance.SetFloat(_normalStrengthProperty, normalValue);
    }

    private void UpdateDamageParticles(float healthNormalized)
    {
        if (_damageParticleObjects == null || _healthThresholds == null)
            return;

        int count = Mathf.Min(_damageParticleObjects.Length, _healthThresholds.Length);

        for (int i = 0; i < count; i++)
        {
            if (_activatedParticles[i])
                continue;

            if (healthNormalized <= _healthThresholds[i])
            {
                _activatedParticles[i] = true;

                if (_damageParticleObjects[i] != null)
                    _damageParticleObjects[i].SetActive(true);
            }
        }
    }

    private void DieBoss()
    {
        _UIShipUpgrade.SetActive(true);
       // _enemySpawner.ResumeSpawning();
        BasicEnemy.TriggerBossDefeated();
    }
}