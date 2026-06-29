using System;
using System.Collections;
using UnityEngine;

public class BossChargeAttack : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float _moveSpeed = 6f;

    [Header("Disparo")]
    [SerializeField] private float _bulletShootRange = 5f;
    [SerializeField] private BulletFactory _factory;
    [SerializeField] private SO_CannonData _cannonData;
    [SerializeField] private Transform[] _shootPoints;

    [Header("Referencias")]
    [SerializeField] private Transform _player;
    [SerializeField] private BossCrossPattern _crossPattern;
    [SerializeField] private BossRandomPattern _randomPattern;

    public bool IsCharging { get; private set; } = false;

    public event Action OnChargeComplete;

    private RT_CannonData _rtCannonData;
    private BossMovement _movement;
    [SerializeField] private float _rotationOffset = 0f;

    private void Awake()
    {
        _rtCannonData = new RT_CannonData(_cannonData);
        _movement = GetComponent<BossMovement>();
    }

    public void DoCharge()
    {
        if (IsCharging) return;
        StartCoroutine(ChargeRoutine());
    }

    private IEnumerator ChargeRoutine()
    {
        IsCharging = true;
        if (_movement != null) _movement.Paused = true;

        // ── 1. Avanzar hasta bulletShootRange y disparar ────────────────────
        while (true)
        {
            Vector3 dir = (_player.position - transform.position);
            dir.y = 0f;

            if (dir.magnitude <= _bulletShootRange)
            {
                ShootAllCannons();
                FireRandomPattern();
                break;
            }

            transform.position += dir.normalized * _moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, _rotationOffset, 0f);

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        // ── 3. Retomar movimiento normal ────────────────────────────────────
        if (_movement != null) _movement.Paused = false;
        IsCharging = false;
        OnChargeComplete?.Invoke();
    }

    private void FireRandomPattern()
    {
        if (UnityEngine.Random.value < 0.5f)
            _crossPattern.FireCross();
        else
            _randomPattern.FireRandom();
    }

    private void ShootAllCannons()
    {
        if (_shootPoints == null || _shootPoints.Length == 0)
        {
            return;
        }

        foreach (var point in _shootPoints)
        {
            if (point == null) continue;

            Vector3 shootDir = point.right; // dirección local del cañón en este frame

            var bullet = _factory.Create() as CannonBulletBoss;
            if (bullet == null)
            {
                continue;
            }

            bullet.Setup(point, _rtCannonData, shootDir);
        }
    }

    public void ResetBoss()
    {
        StopAllCoroutines();
        IsCharging = false;
        if (_movement != null) _movement.Paused = false;
    }
}