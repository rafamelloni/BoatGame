using System.Collections;
using UnityEngine;

public class BossCurtainAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _mesh;
    [SerializeField] private Transform _player;
    [SerializeField] private GameObject canons;
    [SerializeField] RT_PlayerHealth _healthPlayer;

    [Header("Material / Shader")]
    [SerializeField] private Renderer[] _materialRenderers;
    [SerializeField] private string _materialProperty = "_Progress";
    [SerializeField] private float _materialFadeInDuration = 1f;
    [SerializeField] private float _materialFadeOutDuration = 0.5f;

    [Header("Spin")]
    [SerializeField] private float _spinSpeed = 180f;
    [SerializeField] private float _spinDuration = 8f;

    [Header("Disparo")]
    [SerializeField] private float _fireRate = 0.06f;
    [SerializeField] private float _bulletSpawnOffset = 2f;
    [SerializeField] private float _bulletYOffset = 1f;

    [Header("Gap")]
    [SerializeField, Range(10f, 90f)] private float _gapAngle = 40f;
    [SerializeField] private float _gapRotateSpeed = 20f;

    [Header("Particulas")]
    [SerializeField] private GameObject[] _fireParticles;
    [SerializeField] private float _particleDuration = 0.1f;

    private float _currentAngle = 0f;
    private float _gapCenter = 0f;
    private bool _isSpinning = false;
    private MaterialPropertyBlock _propertyBlock;

    public bool IsSpinning => _isSpinning;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        SetMaterialValue(0f);
    }

    public void DoSpinGapAttack()
    {
        if (_isSpinning) return;
        StartCoroutine(SpinGapRoutine());
    }

    private IEnumerator SpinGapRoutine()
    {
        _isSpinning = true;

        if (canons != null)
            canons.SetActive(true);

        yield return StartCoroutine(AnimateMaterialValue(0f, 1f, _materialFadeInDuration));

        Vector3 dirToPlayer = _player.position - transform.position;
        dirToPlayer.y = 0f;
        _gapCenter = Mathf.Atan2(dirToPlayer.x, dirToPlayer.z) * Mathf.Rad2Deg;

        float elapsed = 0f;
        float nextFireTime = 0f;

        while (elapsed < _spinDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            _currentAngle += _spinSpeed * delta;
            _gapCenter += _gapRotateSpeed * delta;

            if (_mesh != null)
                _mesh.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);

            if (elapsed >= nextFireTime)
            {
                FireSpiralWithGap(_currentAngle);
                FireSpiralWithGap(_currentAngle + 180f);
                nextFireTime = elapsed + _fireRate;
            }

            yield return null;
        }

        if (_mesh != null)
            _mesh.localRotation = Quaternion.identity;

        yield return StartCoroutine(AnimateMaterialValue(1f, 0f, _materialFadeOutDuration));

        if (canons != null)
            canons.SetActive(false);

        _isSpinning = false;
    }

    private IEnumerator AnimateMaterialValue(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetMaterialValue(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            SetMaterialValue(Mathf.Lerp(from, to, normalized));
            yield return null;
        }

        SetMaterialValue(to);
    }

    private void SetMaterialValue(float value)
    {
        if (_materialRenderers == null) return;

        for (int i = 0; i < _materialRenderers.Length; i++)
        {
            if (_materialRenderers[i] == null) continue;
            _materialRenderers[i].GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(_materialProperty, value);
            _materialRenderers[i].SetPropertyBlock(_propertyBlock);
        }
    }

    private void FireSpiralWithGap(float angleDeg)
    {
        float normalized = angleDeg % 360f;
        if (normalized < 0f) normalized += 360f;

        float gapNormalized = _gapCenter % 360f;
        if (gapNormalized < 0f) gapNormalized += 360f;

        float diff = Mathf.Abs(Mathf.DeltaAngle(normalized, gapNormalized));
        if (diff < _gapAngle / 2f) return;

        TriggerFireParticles();

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 spawnPos = transform.position + dir * _bulletSpawnOffset;
        spawnPos.y = transform.position.y + _bulletYOffset;

        GameObject go = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);
        go.GetComponent<BossBullet>()?.setup(_healthPlayer);
        go.GetComponent<BossBullet>()?.Launch(dir);
    }

    private void TriggerFireParticles()
    {
        StopCoroutine(nameof(FireParticlesRoutine));
        StartCoroutine(nameof(FireParticlesRoutine));
    }

    private IEnumerator FireParticlesRoutine()
    {
        foreach (var p in _fireParticles)
            if (p != null) p.SetActive(true);

        yield return new WaitForSeconds(_particleDuration);

        foreach (var p in _fireParticles)
            if (p != null) p.SetActive(false);
    }
}