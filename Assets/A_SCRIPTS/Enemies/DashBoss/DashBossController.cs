using System.Collections;
using UnityEngine;

public class DashBossController : Enemy
{
    [Header("References")]
    [SerializeField] private DashBossMovement _movement;
    [SerializeField] private DashTelegraph _telegraph;
    [SerializeField] private DashBossShoot _shoot;
    [Header("Dash Settings")]
    [SerializeField] private float _telegraphDuration = 0.8f;
    [SerializeField] private float _timeBetweenDashes = 2f;
    [Header("Special Attack")]
    [SerializeField] private int _specialDashCount = 3;
    [SerializeField] private float _specialCooldown = 8f;

    private Transform _player;
    private float _nextDashTime;
    private float _nextSpecialTime;
    private bool _isBusy;

    [SerializeField] Transform player;

    private void Awake()
    {
        base.Awake();
        SetPlayer(player);
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _movement.SetPlayer(player);
        _nextSpecialTime = Time.time + _specialCooldown;
        _shoot.SetPlayer(player);
    }

    private void Update()
    {
        if (_player == null) return;
        if (!_isBusy)
        {
            _movement.RotateBroadside();
            _shoot.TryShoot();
        }

        if (_isBusy) return;
        if (Time.time >= _nextSpecialTime)
            StartCoroutine(DoSpecialAttack());
        else if (Time.time >= _nextDashTime)
            StartCoroutine(DoDash());
    }

    private IEnumerator DoDash()
    {
        _isBusy = true;
        _shoot.SetCanShoot(false);
        _movement.LockRotation(true);
        Vector3 playerPos = _player.position;
        Vector3 dir = (playerPos - transform.position);
        dir.y = 0f;
        Vector3 destination = playerPos + dir.normalized * _movement.StopDistance;
        destination.y = transform.position.y;
        yield return _movement.RotateToFace(destination);
        _telegraph.Show(transform.position, destination, _telegraphDuration);
        yield return new WaitForSeconds(_telegraphDuration);
        _telegraph.Hide();
        yield return _movement.ExecuteDash(destination);
        _movement.LockRotation(false);
        _nextDashTime = Time.time + _timeBetweenDashes;
        _isBusy = false;
        _shoot.SetCanShoot(true);
    }

    private IEnumerator DoSpecialAttack()
    {
        _isBusy = true;
        for (int i = 0; i < _specialDashCount; i++)
        {
            Vector3 playerPos = _player.position;
            _movement.LockRotation(true);
            Vector3 dir = (playerPos - transform.position);
            dir.y = 0f;
            Vector3 destination = playerPos + dir.normalized * _movement.StopDistance;
            destination.y = transform.position.y;
            yield return _movement.RotateToFace(destination);
            _telegraph.Show(transform.position, destination, _telegraphDuration);
            yield return new WaitForSeconds(_telegraphDuration);
            _telegraph.Hide();
            yield return _movement.ExecuteDash(destination);
            _movement.LockRotation(false);
            yield return new WaitForSeconds(0.15f);
        }
        _nextSpecialTime = Time.time + _specialCooldown;
        _nextDashTime = Time.time + _timeBetweenDashes;
        _isBusy = false;
        _shoot.SetCanShoot(true);
        print("Special finished, isBusy: " + _isBusy);
    }
}