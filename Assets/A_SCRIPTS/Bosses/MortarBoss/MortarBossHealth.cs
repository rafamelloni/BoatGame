using System;
using UnityEngine;

public class MortarBossHealth : MonoBehaviour, IDamageable, IBoss
{
    [SerializeField] private float _maxHealth = 300f;

    [Header("Shader Grietas")]
    [SerializeField] private Renderer _bossRenderer;
    [SerializeField] private Renderer _bossRenderer2;
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

    [Header("Spawner")]
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private IslandSpawnManager _islandSpawner;

    private float _currentHealth;
    private Material _materialInstance;
    private Material _materialInstance2;
    private bool[] _activatedParticles;

    public event Action<float> OnDamage;
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    public float HealthPercent => _currentHealth / _maxHealth;
    public bool IsDead => _currentHealth <= 0f;

    public float GetCurrenHealt() => _currentHealth;
    public float GetHealthNormalized() => HealthPercent;

    private void Awake()
    {
        _currentHealth = _maxHealth;

        if (_bossRenderer != null)
            _materialInstance = _bossRenderer.material;
        if (_bossRenderer2 != null)
            _materialInstance2 = _bossRenderer2.material;

        _activatedParticles = new bool[_damageParticleObjects?.Length ?? 0];

        if (_damageParticleObjects != null)
            foreach (var p in _damageParticleObjects)
                if (p != null) p.SetActive(false);
    }

    private void OnEnable()
    {
        _enemySpawner.StopSpawning();
        _islandSpawner.ResetIslands();
    }

    private void OnDisable()
    {
        _enemySpawner.StartSpawning();
        _islandSpawner.StartSpawning();

    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnDamage?.Invoke(_currentHealth);
        OnHealthChanged?.Invoke(HealthPercent);
        UpdateCracks(HealthPercent);
        UpdateDamageParticles(HealthPercent);

        if (_currentHealth <= 0f)
        {
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }
    }

    private void UpdateCracks(float healthNormalized)
    {
        float dmg = 1f - healthNormalized;
        float smooth = Mathf.SmoothStep(0f, 1f, dmg);
        float crackValue = Mathf.Pow(smooth, _curvaGrietas) * _maxGrietaStrength;
        float emitValue = Mathf.Pow(smooth, 2.5f) * _maxEmissionStrength;
        float normalValue = Mathf.Pow(smooth, _curvaGrietas) * _maxNormalStrength;

        if (_materialInstance != null)
        {
            _materialInstance.SetFloat(_grietaStrengthProperty, crackValue);
            _materialInstance.SetFloat(_emissionStrengthProperty, emitValue);
            _materialInstance.SetFloat(_normalStrengthProperty, normalValue);
        }

        if (_materialInstance2 != null)
        {
            _materialInstance2.SetFloat(_grietaStrengthProperty, crackValue);
            _materialInstance2.SetFloat(_emissionStrengthProperty, emitValue);
            _materialInstance2.SetFloat(_normalStrengthProperty, normalValue);
        }
    }

    private void UpdateDamageParticles(float healthNormalized)
    {
        if (_damageParticleObjects == null || _healthThresholds == null) return;

        int count = Mathf.Min(_damageParticleObjects.Length, _healthThresholds.Length);
        for (int i = 0; i < count; i++)
        {
            if (_activatedParticles[i]) continue;
            if (healthNormalized <= _healthThresholds[i])
            {
                _activatedParticles[i] = true;
                if (_damageParticleObjects[i] != null)
                    _damageParticleObjects[i].SetActive(true);
            }
        }
    }

    public void ResetHealth()
    {
        _currentHealth = _maxHealth;

        // Resetear shader de grietas
        UpdateCracks(1f);

        // Apagar partículas de daño
        if (_damageParticleObjects != null)
            foreach (var p in _damageParticleObjects)
                if (p != null) p.SetActive(false);

        // Resetear flags
        for (int i = 0; i < _activatedParticles.Length; i++)
            _activatedParticles[i] = false;
    }
}