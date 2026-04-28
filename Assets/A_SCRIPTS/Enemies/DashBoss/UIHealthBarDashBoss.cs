using UnityEngine;
using UnityEngine.UI;

public class UIHealthBarDashBoss : MonoBehaviour
{
    [Header("Vida Boss")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private EnemyHealth _bossHealth;

    [Header("Shader Grietas (BOSS)")]
    [SerializeField] private Renderer _bossRenderer;
    [SerializeField] private string _grietaStrengthProperty = "_grietaStrength";

    [Header("Ajustes Visuales")]
    [SerializeField] private float _curvaGrietas = 0.55f;

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

        float value = Mathf.Pow(healthNormalized, _curvaGrietas);

        _materialInstance.SetFloat(_grietaStrengthProperty, value);
    }
}