using System.Collections;
using UnityEngine;

public class BossCurtainAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _mesh;
    [SerializeField] private Transform _player;

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

    private float _currentAngle = 0f;
    private float _gapCenter = 0f;
    private bool _isSpinning = false;

    public bool IsSpinning => _isSpinning;

    public void DoSpinGapAttack()
    {
        if (_isSpinning) return;
        StartCoroutine(SpinGapRoutine());
    }

    private IEnumerator SpinGapRoutine()
    {
        _isSpinning = true;

        // Gap arranca apuntando al player
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

        _isSpinning = false;
    }

    private void FireSpiralWithGap(float angleDeg)
    {
        float normalized = angleDeg % 360f;
        if (normalized < 0f) normalized += 360f;

        float gapNormalized = _gapCenter % 360f;
        if (gapNormalized < 0f) gapNormalized += 360f;

        float diff = Mathf.Abs(Mathf.DeltaAngle(normalized, gapNormalized));
        if (diff < _gapAngle / 2f) return;

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 spawnPos = transform.position + dir * _bulletSpawnOffset;
        spawnPos.y = transform.position.y + _bulletYOffset;

        var go = Instantiate(_bulletPrefab, spawnPos, Quaternion.identity);
        go.GetComponent<BossBullet>()?.Launch(dir);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            DoSpinGapAttack();
    }
}