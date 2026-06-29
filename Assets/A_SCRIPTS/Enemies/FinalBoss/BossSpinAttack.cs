using System.Collections;
using UnityEngine;

public class BossSpinAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _mesh;
    [SerializeField] private RT_PlayerHealth _healthPlayer;
    [SerializeField] private GameObject canons;

    [Header("Material / Shader")]
    [SerializeField] private Renderer[] _materialRenderers;
    [SerializeField] private string _materialProperty = "_Progress";
    [SerializeField] private float _materialFadeInDuration = 1f;
    [SerializeField] private float _materialFadeOutDuration = 0.5f;

    [Header("Spin")]
    [SerializeField] private float _spinSpeed = 360f;
    [SerializeField] private float _spinDuration = 4f;

    [Header("Disparo")]
    [SerializeField] private float _fireRate = 0.08f;
    [SerializeField] private float _bulletSpawnOffset = 2f;
    [SerializeField] private float _bulletYOffset = 1f;

    private float _currentAngle = 0f;
    private bool _isSpinning = false;
    private MaterialPropertyBlock _propertyBlock;

    public bool IsSpinning => _isSpinning;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        SetMaterialValue(0f);
    }

    public void DoSpinAttack()
    {
        if (_isSpinning) return;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        _isSpinning = true;

        if (canons != null)
            canons.SetActive(true);

        yield return StartCoroutine(AnimateMaterialValue(0f, 1f, _materialFadeInDuration));

        float elapsed = 0f;
        float nextFireTime = 0f;

        while (elapsed < _spinDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            _currentAngle += _spinSpeed * delta;
            if (_mesh != null)
                _mesh.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);

            if (elapsed >= nextFireTime)
            {
                FireSpiral(_currentAngle);
                FireSpiral(_currentAngle + 180f);
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

    private void FireSpiral(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 spawnPos = transform.position + dir * _bulletSpawnOffset;
        spawnPos.y = transform.position.y + _bulletYOffset;

        var go = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);
        go.GetComponent<BossBullet>()?.setup(_healthPlayer);
        go.GetComponent<BossBullet>()?.Launch(dir);
    }
}