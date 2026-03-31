using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealth _cannonTower;

    [Header("Images")]
    [SerializeField] private Image _greenFill;
    [SerializeField] private Image _redFill;

    private float _maxHealth;

    private void Start()
    {
        if (_cannonTower != null)
        {
            _cannonTower.OnDamage += UpdateUI;
            _maxHealth = _cannonTower.GetMaxHealt();
        }
    }

    private void OnDisable()
    {
        if (_cannonTower != null)
        {
            _cannonTower.OnDamage -= UpdateUI;
        }
    }

    private void UpdateUI(float currentHealth)
    {
        if (_maxHealth <= 0f) return;

        float normalizedHealth = currentHealth / _maxHealth;

        _greenFill.fillAmount = normalizedHealth;
        _redFill.fillAmount = 1f;
    }
}