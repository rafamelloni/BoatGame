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

    [Header("Ajustes Visuales")]
    [SerializeField] private float _curvaGrietas = 0.6f;        // más bajo = más progresivo
    [SerializeField] private float _maxGrietaStrength = 1f;
    [SerializeField] private float _maxEmissionStrength = 2f;

    private Material _materialInstance;

    private void Awake()
    {
        if (_bossRenderer != null)
            _materialInstance = _bossRenderer.material;
    }

    private void OnEnable()
    {
        if (_bossHealth != null)
        {
            _bossHealth.OnDamage += UpdateBar;
            UpdateBar(_bossHealth.GetCurrenHealt());
        }
    }

    private void OnDisable()
    {
        if (_bossHealth != null)
            _bossHealth.OnDamage -= UpdateBar;
    }

    private void UpdateBar(float currentHealth)
    {
        float healthNormalized = Mathf.Clamp01(_bossHealth.GetHealthNormalized());

        if (_fillImage != null)
            _fillImage.fillAmount = healthNormalized;

        UpdateCracks(healthNormalized);
    }

    private void UpdateCracks(float healthNormalized)
    {
        if (_materialInstance == null)
            return;

        // 0 = sin daño, 1 = daño total
        float damagePercent = 1f - healthNormalized;

        // suaviza la transición (clave para que sea progresivo)
        float smoothDamage = Mathf.SmoothStep(0f, 1f, damagePercent);

        // grietas progresivas
        float crackValue = Mathf.Pow(smoothDamage, _curvaGrietas) * _maxGrietaStrength;

        // brillo más tardío (solo cuando está bastante dañado)
        float emissionValue = Mathf.Pow(smoothDamage, 2.5f) * _maxEmissionStrength;

        _materialInstance.SetFloat(_grietaStrengthProperty, crackValue);
        _materialInstance.SetFloat(_emissionStrengthProperty, emissionValue);
    }
}