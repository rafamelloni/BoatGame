using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UpgradeStatsUI : MonoBehaviour
{
    [Header("Stat Texts")]
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _cooldownText;
    [SerializeField] private TMP_Text _fireRateText;

    [Header("Progress Bar")]
    [SerializeField] private Slider _progressBar;
    [SerializeField] private float _barMax = 200f;

    [Header("Flash")]
    [SerializeField] private float _flashDuration = 0.5f;

    private readonly Dictionary<StatType, float> _totals = new();
    private readonly Dictionary<StatType, TMP_Text> _textMap = new();

    private void Awake()
    {
        _textMap[StatType.Damage] = _damageText;
        _textMap[StatType.Cooldown] = _cooldownText;
        _textMap[StatType.FireRate] = _fireRateText;

        // Leer el valor base que ya tiene cada texto en escena
        foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
        {
            if (_textMap.TryGetValue(stat, out TMP_Text text) && text != null
                && float.TryParse(text.text, out float baseValue))
                _totals[stat] = baseValue;
            else
                _totals[stat] = 0f;
        }

        if (_progressBar != null)
        {
            _progressBar.minValue = 0f;
            _progressBar.maxValue = _barMax;
            _progressBar.value = 0f;
        }
    }

    public void OnUpgradeApplied(StatType stat, float value)
    {
        // Acumular y mostrar
        if (stat == StatType.Cooldown || stat == StatType.FireRate)
            _totals[stat] -= value;
        else
            _totals[stat] += value;

        if (_textMap.TryGetValue(stat, out TMP_Text text) && text != null)
        {
            text.text = _totals[stat].ToString("F1");
            StopCoroutine(nameof(FlashGreen)); // por si estaba corriendo
            StartCoroutine(FlashGreen(text));
        }

        // Barra: siempre +1 por mejora aplicada
        if (_progressBar != null)
            _progressBar.value = Mathf.Min(_progressBar.value + 1f, _barMax);
    }

    private IEnumerator FlashGreen(TMP_Text text)
    {
        Color original = text.color;
        text.color = Color.green;
        yield return new WaitForSeconds(_flashDuration);
        text.color = original;
    }
}