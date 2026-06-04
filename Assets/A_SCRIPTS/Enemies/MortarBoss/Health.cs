using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private EnemyHealth _bossHealth;

    [Header("Renderers")]
    [SerializeField] private Renderer _bossRenderer;
    [SerializeField] private Renderer _mortarRenderer;

    [Header("Propiedades Shader")]
    [SerializeField] private string _grietaStrengthProperty = "_grietaStrength";
    [SerializeField] private string _emissionStrengthProperty = "_EmissionStrength";
    [SerializeField] private string _normalStrengthProperty = "_NormalStrenght";

    [Header("Ajustes Visuales")]
    [SerializeField] private float _curvaGrietas = 0.6f;
    [SerializeField] private float _maxGrietaStrength = 1f;
    [SerializeField] private float _maxEmissionStrength = 2f;
    [SerializeField] private float _maxNormalStrength = 1f;

    private Material _bossMaterial;
    private Material _mortarMaterial;

    private void Awake()
    {
        if (_bossRenderer != null)
            _bossMaterial = _bossRenderer.material;

        if (_mortarRenderer != null)
            _mortarMaterial = _mortarRenderer.material;
    }

    private void OnEnable()
    {
        if (_bossHealth != null)
        {
            _bossHealth.OnDamage += UpdateCracks;
            UpdateCracks(_bossHealth.GetCurrenHealt());
        }
    }

    private void OnDisable()
    {
        if (_bossHealth != null)
            _bossHealth.OnDamage -= UpdateCracks;
    }

    private void UpdateCracks(float currentHealth)
    {
        if (_bossHealth == null)
            return;

        float healthNormalized = Mathf.Clamp01(_bossHealth.GetHealthNormalized());

        float damagePercent = 1f - healthNormalized;
        float smoothDamage = Mathf.SmoothStep(0f, 1f, damagePercent);

        float crackValue = Mathf.Pow(smoothDamage, _curvaGrietas) * _maxGrietaStrength;
        float emissionValue = Mathf.Pow(smoothDamage, 2.5f) * _maxEmissionStrength;
        float normalValue = Mathf.Pow(smoothDamage, _curvaGrietas) * _maxNormalStrength;

        ApplyToMaterial(_bossMaterial, crackValue, emissionValue, normalValue);
        ApplyToMaterial(_mortarMaterial, crackValue, emissionValue, normalValue);
    }

    private void ApplyToMaterial(Material mat, float crack, float emission, float normal)
    {
        if (mat == null) return;

        mat.SetFloat(_grietaStrengthProperty, crack);
        mat.SetFloat(_emissionStrengthProperty, emission);
        mat.SetFloat(_normalStrengthProperty, normal);
    }
}