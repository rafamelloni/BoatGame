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

    [Header("Velocidad")]
    [SerializeField] private float _chaseAccel = 8f;
    [Tooltip("Qué tan rápido frena al empezar a apuntar")]
    [SerializeField] private float _aimBrake = 14f;
    [Tooltip("Qué tan rápido llega a la velocidad de embestida")]
    [SerializeField] private float _chargeAccel = 70f;
    [Tooltip("Corta la embestida si después de este tiempo todavía no te cruzó")]
    [SerializeField] private float _maxChargeTime = 3f;

    [Header("Indicador de embestida")]
    [SerializeField] private float _lineWidth = 2f;
    [SerializeField] private Color _lineColor = new Color(1f, 0.35f, 0.25f, 1f);

    [Header("Transiciones: inclinación (+ = proa arriba)")]
    [Tooltip("Vacío = se crea solo con los modelos hijos del Rafa")]
    [SerializeField] private Transform _visualPivot;
    [Tooltip("Tildar si la proa se inclina para el lado contrario")]
    [SerializeField] private bool _invertTilt;
    [SerializeField] private float _chaseTilt = -3f;
    [SerializeField] private float _aimTilt = 18f;
    [SerializeField] private float _chargeTilt = -6f;
    [Tooltip("Golpe extra de proa hacia abajo al salir")]
    [SerializeField] private float _chargeKickTilt = 16f;
    [SerializeField] private float _brakeTilt = -14f;
    [SerializeField] private float _tiltStiffness = 160f;
    [SerializeField, Range(0.1f, 1f)] private float _tiltDamping = 0.4f;

    [Header("Transiciones: giro")]
    [Tooltip("Cuánto se recuesta al girar (grados por cada grado/segundo de giro)")]
    [SerializeField] private float _bankAmount = 0.08f;
    [SerializeField] private float _maxBank = 15f;
    [SerializeField] private float _bankSmooth = 8f;

    [Header("Transiciones: aplastar y estirar")]
    [SerializeField] private float _aimSquash = 0.15f;
    [SerializeField] private float _chargeStretch = 0.25f;
    [SerializeField] private float _squashStiffness = 220f;
    [SerializeField, Range(0.1f, 1f)] private float _squashDamping = 0.35f;

    [Header("Espuma al salir")]
    [SerializeField] private float _launchRingSize = 1f;

    private Transform _player;
    private Vector3 _currentDir;
    private float _speed;

    private enum State { Chase, Aim, Charge, Overshoot }
    private State _state;

    private Vector3 _chargeDir;
    private float _nextChargeTime;
    private float _aimStartTime;
    private float _aimEndTime;
    private float _chargeStartTime;
    private float _overshootBrake;

    private DashLineTelegraph _line;

    private float _tilt;
    private float _tiltVel;
    private float _bank;
    private float _lastYaw;
    private float _squash;
    private float _squashVel;
    private Vector3 _pivotBaseScale = Vector3.one;

    public event Action<RafaEnemy> OnDead;

    protected override void Awake()
    {
        base.Awake();
        _enemyHealth.OnDeath += () => OnDead?.Invoke(this);
        BuildVisualPivot();
    }

    private void OnDisable()
    {
        if (_line != null) _line.Cancel();
        _line = null;
    }

    public void SetPlayer(Transform player)
    {
        _player = player;
        _currentDir = transform.forward;
        _speed = 0f;
        _state = State.Chase;
        _nextChargeTime = 0f;
        ResetVisuals();
        _enemyHealth.Revive();
    }

    private void Update()
    {
        if (_player == null) return;

        switch (_state)
        {
            case State.Chase: UpdateChase(); break;
            case State.Aim: UpdateAim(); break;
            case State.Charge: UpdateCharge(); break;
            case State.Overshoot: UpdateOvershoot(); break;
        }

        UpdateVisuals();
    }

    // ---------- Estados ----------

    private void UpdateChase()
    {
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= chargeRange && Time.time >= _nextChargeTime)
        {
            EnterAim();
            return;
        }

        Vector3 desiredDir = FlatDirToPlayer();
        _currentDir = Vector3.Slerp(_currentDir, desiredDir, steerSpeed * Time.deltaTime);
        _currentDir.y = 0f;

        _speed = Mathf.MoveTowards(_speed, speed, _chaseAccel * Time.deltaTime);
        MoveStraight(_currentDir, _speed);
    }

    private void EnterAim()
    {
        _state = State.Aim;
        _aimStartTime = Time.time;
        _aimEndTime = Time.time + aimDuration;
        _line = DashLineTelegraph.Spawn(transform.position, PredictedChargeEnd(), aimDuration, _lineWidth, _lineColor);
    }

    private void UpdateAim()
    {
        // Frena de a poco y gira para mirar al jugador
        _speed = Mathf.MoveTowards(_speed, 0f, _aimBrake * Time.deltaTime);
        transform.position += _currentDir * _speed * Time.deltaTime;
        RotateToward(FlatDirToPlayer());

        if (_line != null) _line.UpdatePath(transform.position, PredictedChargeEnd());

        if (Time.time >= _aimEndTime) EnterCharge();
    }

    private void EnterCharge()
    {
        _state = State.Charge;
        _chargeStartTime = Time.time;
        _chargeDir = FlatDirToPlayer();

        if (_line != null)
        {
            _line.UpdatePath(transform.position, PredictedChargeEnd());
            _line.Fire();
            _line = null;
        }

        _tiltVel -= _chargeKickTilt * 12f;
        _squash = -_chargeStretch;
        _squashVel = 0f;
        WaterImpacts.Spawn(transform.position, _launchRingSize);
    }

    private void UpdateCharge()
    {
        _speed = Mathf.MoveTowards(_speed, chargeSpeed, _chargeAccel * Time.deltaTime);
        MoveStraight(_chargeDir, _speed);

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        if (Vector3.Dot(_chargeDir, toPlayer) < 0f || Time.time >= _chargeStartTime + _maxChargeTime)
            EnterOvershoot();
    }

    private void EnterOvershoot()
    {
        _state = State.Overshoot;
        // Frena justo a chargeOvershoot metros de donde te cruzó
        _overshootBrake = Mathf.Max(_speed * _speed / (2f * Mathf.Max(chargeOvershoot, 0.1f)), 1f);
    }

    private void UpdateOvershoot()
    {
        _speed = Mathf.MoveTowards(_speed, 0f, _overshootBrake * Time.deltaTime);
        MoveStraight(_chargeDir, _speed);

        if (_speed <= 0.5f)
        {
            _state = State.Chase;
            _currentDir = _chargeDir;
            _nextChargeTime = Time.time + chargeCooldown;
        }
    }

    // ---------- Movimiento ----------

    private Vector3 FlatDirToPlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : _currentDir;
    }

    private Vector3 PredictedChargeEnd()
    {
        return _player.position + FlatDirToPlayer() * chargeOvershoot;
    }

    private void RotateToward(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        targetRot *= Quaternion.Euler(0f, -90f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private void MoveStraight(Vector3 dir, float spd)
    {
        transform.position += dir * spd * Time.deltaTime;
        RotateToward(dir);
    }

    // ---------- Transiciones visuales ----------

    // Los modelos van dentro de un pivot para inclinarlos sin tocar la rotación que usa la IA
    private void BuildVisualPivot()
    {
        if (_visualPivot == null)
        {
            var pivotObject = new GameObject("VisualPivot");
            _visualPivot = pivotObject.transform;
            _visualPivot.SetParent(transform, false);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != _visualPivot && child.GetComponent<MeshRenderer>() != null)
                    child.SetParent(_visualPivot, false);
            }
        }

        _pivotBaseScale = _visualPivot.localScale;
    }

    private void ResetVisuals()
    {
        _tilt = 0f;
        _tiltVel = 0f;
        _bank = 0f;
        _squash = 0f;
        _squashVel = 0f;
        _lastYaw = transform.eulerAngles.y;

        if (_visualPivot == null) return;
        _visualPivot.localRotation = Quaternion.identity;
        _visualPivot.localScale = _pivotBaseScale;
    }

    private void UpdateVisuals()
    {
        if (_visualPivot == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float aimProgress = _state == State.Aim && aimDuration > 0f
            ? Mathf.SmoothStep(0f, 1f, (Time.time - _aimStartTime) / aimDuration)
            : 0f;

        float targetTilt = _state switch
        {
            State.Chase => _chaseTilt * Mathf.InverseLerp(0f, speed, _speed),
            State.Aim => _aimTilt * aimProgress,
            State.Charge => _chargeTilt,
            State.Overshoot => _brakeTilt,
            _ => 0f
        };

        // Resortes: llegan rápido, se pasan un poco y rebotan
        float tiltDamp = 2f * _tiltDamping * Mathf.Sqrt(_tiltStiffness);
        _tiltVel += (_tiltStiffness * (targetTilt - _tilt) - tiltDamp * _tiltVel) * dt;
        _tilt += _tiltVel * dt;

        float targetSquash = _aimSquash * aimProgress;
        float squashDamp = 2f * _squashDamping * Mathf.Sqrt(_squashStiffness);
        _squashVel += (_squashStiffness * (targetSquash - _squash) - squashDamp * _squashVel) * dt;
        _squash += _squashVel * dt;

        float yaw = transform.eulerAngles.y;
        float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
        _lastYaw = yaw;
        float targetBank = Mathf.Clamp(-yawRate * _bankAmount, -_maxBank, _maxBank);
        _bank = Mathf.Lerp(_bank, targetBank, 1f - Mathf.Exp(-_bankSmooth * dt));

        // La proa del modelo apunta a +X: Z la levanta o la baja, X lo recuesta
        float tiltSign = _invertTilt ? -1f : 1f;
        _visualPivot.localRotation = Quaternion.Euler(_bank, 0f, _tilt * tiltSign);

        // Aplastado: más bajo y ancho. Estirado: más largo hacia la proa.
        Vector3 scale = _squash >= 0f
            ? new Vector3(1f + _squash * 0.5f, 1f - _squash, 1f + _squash * 0.5f)
            : new Vector3(1f - _squash, 1f + _squash * 0.5f, 1f + _squash * 0.5f);
        _visualPivot.localScale = Vector3.Scale(_pivotBaseScale, scale);
    }

    // ---------- Choques ----------

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
            // No matar enemigos que todavia estan saliendo del agua (recien spawneados)
            var emerge = other.GetComponentInParent<EnemyEmerge>();
            if (emerge != null && emerge.IsEmerging) return;

            other.gameObject.GetComponent<EnemyHealth>().TakeDamage(999f);
            ParticlePool.Instance.GetParticle(baseData.woodExplosionVFX, explosionPoint);
        }
    }
}
