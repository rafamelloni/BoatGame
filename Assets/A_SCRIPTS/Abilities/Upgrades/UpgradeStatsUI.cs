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

    [Header("Base Data")]
    [SerializeField] private SO_CannonData _cannonData;

    private readonly Dictionary<StatType, float> _totals = new();
    private readonly Dictionary<StatType, TMP_Text> _textMap = new();

    private void Awake()
    {
        _textMap[StatType.Damage] = _damageText;
        _textMap[StatType.Cooldown] = _cooldownText;
        _textMap[StatType.FireRate] = _fireRateText;

        _totals[StatType.Damage] = _cannonData.damage;
        _totals[StatType.Cooldown] = _cannonData.cooldown;
        _totals[StatType.FireRate] = _cannonData.timeBetweenShots;

        foreach (var kvp in _textMap)
            if (kvp.Value != null)
                kvp.Value.text = _totals[kvp.Key].ToString("F1");

        if (_progressBar != null)
        {
            _progressBar.minValue = 0f;
            _progressBar.maxValue = _barMax;
            _progressBar.value = 0f;
        }
    }

    public void OnUpgradeApplied(StatType stat, float value)
    {
        if (stat == StatType.Cooldown || stat == StatType.FireRate)
            _totals[stat] -= value;
        else
            _totals[stat] += value;

        if (_textMap.TryGetValue(stat, out TMP_Text text) && text != null)
            text.text = _totals[stat].ToString("F1");

        if (_progressBar != null)
            _progressBar.value = Mathf.Min(_progressBar.value + 1f, _barMax);
    }

    public void ResetStats()
    {
        _totals[StatType.Damage] = _cannonData.damage;
        _totals[StatType.Cooldown] = _cannonData.cooldown;
        _totals[StatType.FireRate] = _cannonData.timeBetweenShots;

        foreach (var kvp in _textMap)
            if (kvp.Value != null)
                kvp.Value.text = _totals[kvp.Key].ToString("F1");

        if (_progressBar != null) _progressBar.value = 0f;
    }

    private IEnumerator FlashGreen(TMP_Text text)
    {
        Color original = text.color;
        text.color = Color.green;
        yield return new WaitForSeconds(_flashDuration);
        text.color = original;
    }
}