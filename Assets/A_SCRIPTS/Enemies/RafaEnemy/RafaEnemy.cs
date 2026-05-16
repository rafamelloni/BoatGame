using System;
using UnityEngine;

public class RafaEnemy : Enemy
{
    [Header("RafaEnemy")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float steerSpeed = 3f;
    [SerializeField] private float chargeRange = 25f;
    [SerializeField] private float chargeSpeed = 18f;
    [SerializeField] private float chargeOvershoot = 5f;
    [SerializeField] private float chargeCooldown = 1.5f;
    [SerializeField] private float aimDuration = 0.8f;

    private Transform _player;
    private Vector3 _currentDir;

    private enum State { Chase, Aim, Charge, Overshoot }
    private State _state;

    private Vector3 _chargeDir;
    private Vector3 _overshootTarget;
    private float _nextChargeTime;
    private float _aimEndTime;

    public event Action<RafaEnemy> OnDead;

    protected override void Awake()
    {
        base.Awake();
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _currentDir = transform.forward;
        _state = State.Chase;
        _nextChargeTime = 0f;
        _enemyHealth.Revive();
    }

    private void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        switch (_state)
        {
            case State.Chase:
                if (dist <= chargeRange && Time.time >= _nextChargeTime)
                {
                    _state = State.Aim;
                    _aimEndTime = Time.time + aimDuration;
                    break;
                }
                Chase();
                break;

            case State.Aim:
                // Frena y rota para mirar al player
                RotateToward(_player.position);
                if (Time.time >= _aimEndTime)
                {
                    _state = State.Charge;
                    _chargeDir = (_player.position - transform.position);
                    _chargeDir.y = 0f;
                    _chargeDir.Normalize();
                }
                break;

            case State.Charge:
                Vector3 toPlayer = _player.position - transform.position;
                toPlayer.y = 0f;
                if (Vector3.Dot(_chargeDir, toPlayer) < 0f)
                {
                    _state = State.Overshoot;
                    _overshootTarget = transform.position + _chargeDir * chargeOvershoot;
                }
                else
                {
                    MoveStraight(_chargeDir, chargeSpeed);
                }
                break;

            case State.Overshoot:
                Vector3 toTarget = _overshootTarget - transform.position;
                toTarget.y = 0f;
                if (toTarget.magnitude < 0.5f)
                {
                    _state = State.Chase;
                    _currentDir = transform.forward;
                    _nextChargeTime = Time.time + chargeCooldown;
                }
                else
                {
                    MoveStraight(_chargeDir, chargeSpeed);
                }
                break;
        }
    }

    private void Chase()
    {
        Vector3 desiredDir = _player.position - transform.position;
        desiredDir.y = 0f;
        desiredDir.Normalize();

        _currentDir = Vector3.Slerp(_currentDir, desiredDir, steerSpeed * Time.deltaTime);
        _currentDir.y = 0f;

        MoveStraight(_currentDir, speed);
    }

    private void RotateToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        targetRot *= Quaternion.Euler(0f, -90f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private void MoveStraight(Vector3 dir, float spd)
    {
        transform.position += dir * spd * Time.deltaTime;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            targetRot *= Quaternion.Euler(0f, -90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Island"))
        {
            collision.gameObject.GetComponent<RT_PlayerHealth>()?.TakeDamage(10f);
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, collision.contacts[0].point);
            _enemyHealth.TakeDamage(999f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 explosionPoint = other.ClosestPoint(transform.position);
        if (other.gameObject.CompareTag("Enemy"))
        {
            other.gameObject.GetComponent<EnemyHealth>().TakeDamage(999f);
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, explosionPoint);
        }
    }
}