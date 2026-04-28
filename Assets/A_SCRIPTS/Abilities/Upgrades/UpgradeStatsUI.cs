using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UpgradeStatsUI : MonoBehaviour
{
    [System.Serializable]
    public struct AbilityStatDisplay
    {
        public string abilityId;
        public StatType stat;
        public TMP_Text text;
    }

    [Header("Stat Displays")]
    [SerializeField] private List<AbilityStatDisplay> _displays;

    [Header("Progress Bar")]
    [SerializeField] private Slider _progressBar;
    [SerializeField] private float _barMax = 200f;

    [Header("Flash")]
    [SerializeField] private float _flashDuration = 0.5f;

    private readonly Dictionary<(string, StatType), float> _baseValues = new();
    private void Awake()
    {
        if (_progressBar != null)
        {
            _progressBar.minValue = 0f;
            _progressBar.maxValue = _barMax;
            _progressBar.value = 0f;
        }
    }
    public void RegisterBase(string abilityId, StatType stat, float baseValue)
    {
        _baseValues[(abilityId, stat)] = baseValue;
    }



    public void OnUpgradeApplied(string abilityId, StatType stat, float value)
    {
        var displays = _displays.FindAll(d => d.abilityId == abilityId && d.stat == stat);

        foreach (var display in displays)
        {
            if (display.text == null) continue;

            float current = float.TryParse(display.text.text, out float parsed) ? parsed : 0f;
            display.text.text = (current + value).ToString("F1");

            StartCoroutine(FlashGreen(display.text));
        }

        if (_progressBar != null)
            _progressBar.value = Mathf.Min(_progressBar.value + 1f, _barMax);
    }

    public void ResetStats()
    {
        foreach (var display in _displays)
        {
            if (display.text == null) continue;

            if (_baseValues.TryGetValue((display.abilityId, display.stat), out float baseVal))
                display.text.text = baseVal.ToString("F1");
            else
                display.text.text = "0.0";
        }

        if (_progressBar != null)
            _progressBar.value = 0f;
    }

    private IEnumerator FlashGreen(TMP_Text text)
    {
        Color original = text.color;
        text.color = Color.green;
        yield return new WaitForSeconds(_flashDuration);
        text.color = original;
    }
}