using System.Collections.Generic;
using UnityEngine;

public class BladesStrategy : IAbilityStrategy
{
    public bool IsUnlocked { get; private set; } = false;
    public event System.Action<float> OnCooldownStarted;

    private SO_BladesData _data;
    public RT_BladesData _rtData;

    private Transform _playerTransform;
    private GameObject _bladePrefab;
    private List<GameObject> _blades = new();

    // Offset de ruido independiente por blade
    private List<float> _noiseOffsets = new();
    private float _currentAngleOffset = 0f;

    public BladesStrategy(SO_BladesData data, Transform playerTransform)
    {
        _data = data;
        _bladePrefab = data.bladePrefab;
        _playerTransform = playerTransform;
        _rtData = new RT_BladesData(data);
    }

    public void TryExecute() { }
    public void SetUnlocked(bool value) { IsUnlocked = value; }

    public void EnableBlades()
    {
        if (_blades.Count == 0)
            SpawnBlades(_rtData.bladeCount);
    }

    public void Tick()
    {
        if (_blades.Count == 0) return;

        _currentAngleOffset += _rtData.orbitSpeed * Time.deltaTime;

        int count = _blades.Count;
        float angleStep = count > 0 ? 360f / count : 0f;

        for (int i = 0; i < count; i++)
        {
            float angleDeg = _currentAngleOffset + angleStep * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Radio base + variacion errática con Perlin noise
            float noiseTime = Time.time * _rtData.noiseSpeed + _noiseOffsets[i];
            float noiseX = (Mathf.PerlinNoise(noiseTime, _noiseOffsets[i]) - 0.5f) * 2f;
            float noiseZ = (Mathf.PerlinNoise(_noiseOffsets[i], noiseTime) - 0.5f) * 2f;

            float dynamicRadius = _rtData.orbitRadius + Mathf.PerlinNoise(noiseTime * 0.5f, i) * _rtData.radiusVariation;

            Vector3 baseOffset = new Vector3(
                Mathf.Cos(angleRad) * dynamicRadius,
                0f,
                Mathf.Sin(angleRad) * dynamicRadius
            );

            // Desplazamiento lateral errático
            Vector3 erraticOffset = new Vector3(
                noiseX * _rtData.erraticAmount,
                0f,
                noiseZ * _rtData.erraticAmount
            );

            _blades[i].transform.position = _playerTransform.position + baseOffset + erraticOffset;
        }
    }

    public void ApplyUpgrade(StatType stat, float value)
    {
        switch (stat)
        {
            case StatType.BladeDamage:
                _rtData.damage = value;
                RefreshBladeData();
                break;
            case StatType.BladeCount:
                int newCount = Mathf.RoundToInt(value);
                if (newCount != _blades.Count)
                    SetBladeCount(newCount);
                break;
            case StatType.OrbitRadius:
                _rtData.orbitRadius = value;
                break;
            case StatType.BladeSpeed:
                _rtData.orbitSpeed = value;
                break;
        }
    }

    private void SetBladeCount(int newCount)
    {
        foreach (var b in _blades)
            Object.Destroy(b);
        _blades.Clear();
        _noiseOffsets.Clear();
        SpawnBlades(newCount);
        _rtData.bladeCount = newCount;
    }

    private void SpawnBlades(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var blade = Object.Instantiate(_bladePrefab, null, false);
            blade.GetComponent<BladeOrbitBehaviour>()?.Init(_rtData.damage, _rtData.damageCooldownPerEnemy);
            _blades.Add(blade);
            // Offset random para que cada blade tenga su propio patron de ruido
            _noiseOffsets.Add(Random.Range(0f, 100f));
        }
    }

    private void RefreshBladeData()
    {
        foreach (var b in _blades)
            b.GetComponent<BladeOrbitBehaviour>()?.Init(_rtData.damage, _rtData.damageCooldownPerEnemy);
    }

    public void Dispose()
    {
        foreach (var b in _blades)
            if (b != null) Object.Destroy(b);
        _blades.Clear();
        _noiseOffsets.Clear();
    }
}