using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    [Header("Progress Bars")]
    [SerializeField] private Slider _barCannon;
    [SerializeField] private Slider _barMortar;
    [SerializeField] private float _barMax = 5f;



    private readonly Dictionary<(string, StatType), float> _baseValues = new();

    private void Awake()
    {
        InitBar(_barCannon);
        InitBar(_barMortar);
    }

    private void InitBar(Slider bar)
    {
        if (bar == null) return;
        bar.minValue = 0f;
        bar.maxValue = _barMax;
        bar.value = 0f;
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
            if (display.text != null)
            {
                float current = float.TryParse(display.text.text, out float parsed) ? parsed : 0f;
                display.text.text = (current + value).ToString("F1");
            }
        }

        Slider bar = abilityId switch
        {
            "cannon" => _barCannon,
            "mortar" => _barMortar,
            _ => null
        };

        if (bar != null)
            bar.value = Mathf.Min(bar.value + 1f, _barMax);
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

        InitBar(_barCannon);
        InitBar(_barMortar);
    }
}