using UnityEngine;
using UnityEngine.UI;


public class UIHealthBarDashBoss : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private EnemyHealth _bossHealth;

    private void OnEnable()
    {
        _bossHealth.OnDamage += UpdateBar;
    }

    private void OnDisable()
    {
        _bossHealth.OnDamage -= UpdateBar;
    }

    private void UpdateBar(float currentHealth)
    {
        _fillImage.fillAmount = _bossHealth.GetHealthNormalized();
    }
}
