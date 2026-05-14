using UnityEngine;

public class Zone : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private float _startRadius = 200f;
    [SerializeField] private float _endRadius = 30f;
    [SerializeField] private float _shrinkDuration = 120f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _borderParticles;
    [SerializeField] private int _particleCount = 20;
    [SerializeField] private float _particleHeightOffset = 0f;

    [Header("Shader")]
    [SerializeField] private Material _material;
    [SerializeField] private float _planeHalfSize = 14.37f;

    private float _currentRadius;
    private float _elapsed = 0f;
    private bool _shrinking = false;
    private ParticleSystem[] _borderInstances;

    private void Start()
    {
        _currentRadius = _startRadius;
        SpawnBorderParticles();
        StartShrinking();
    }

    public void StartShrinking()
    {
        _shrinking = true;
    }

    private void Update()
    {
        if (!_shrinking) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _shrinkDuration);
        _currentRadius = Mathf.Lerp(_startRadius, _endRadius, t);

        float normalizedRadius = _currentRadius / _startRadius;
        _material.SetFloat("_Radius", normalizedRadius);

        UpdateBorderParticles();

        if (t >= 1f) _shrinking = false;
    }

    private void SpawnBorderParticles()
    {
        _borderInstances = new ParticleSystem[_particleCount];
        for (int i = 0; i < _particleCount; i++)
        {
            float angle = (360f / _particleCount) * i * Mathf.Deg2Rad;
            Vector3 pos = GetBorderPosition(angle);
            _borderInstances[i] = Instantiate(_borderParticles, pos, Quaternion.identity);
            _borderInstances[i].Play();
        }
    }

    private void UpdateBorderParticles()
    {
        for (int i = 0; i < _borderInstances.Length; i++)
        {
            float angle = (360f / _particleCount) * i * Mathf.Deg2Rad;
            _borderInstances[i].transform.position = GetBorderPosition(angle);
        }
    }

    private Vector3 GetBorderPosition(float angle)
    {
        float worldRadius = (_currentRadius / _startRadius) * _planeHalfSize;
        float x = transform.position.x + Mathf.Cos(angle) * worldRadius;
        float z = transform.position.z + Mathf.Sin(angle) * worldRadius;
        return new Vector3(x, transform.position.y + _particleHeightOffset, z);
    }

    public float GetCurrentRadius() => _currentRadius;
    public bool IsShrinking() => _shrinking;
}