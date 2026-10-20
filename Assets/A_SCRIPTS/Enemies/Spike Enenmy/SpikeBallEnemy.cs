using UnityEngine;

public class SpikeBallEnemy : MonoBehaviour
{
    private enum State { Chase, WindUp, Dash, Brake, Spin, LongDash, SpinGlide }
    private enum SpinPhase { Loop, Approach, Settle }
    private enum Attack { Dash, LongDash }

    [Header("Refs")]
    [SerializeField] private Transform _player;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private Transform _tiltPivot;
    [SerializeField] private ParticleSystem _exhaustFire;

    [Header("Chase")]
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _chaseAccel = 3f;
    [SerializeField] private float _chaseTurnSpeed = 120f;
    [SerializeField] private float _minChaseTime = 0.3f;
    [SerializeField] private float _reverseSpeed = 5f;        // marcha atrás si está muy cerca

    [Header("WindUp")]
    [SerializeField] private float _windUpTime = 0.5f;
    [SerializeField] private float _windUpTurnSpeed = 300f;
    [SerializeField] private float _windUpDecel = 6f;

    [Header("Dash normal")]
    [SerializeField] private float _dashRange = 20f;          // arranca si está a esta distancia o menos
    [SerializeField] private float _dashMinDistance = 12f;    // ...y a esta o más
    [SerializeField] private float _dashSpeed = 26f;
    [SerializeField] private float _dashAccel = 50f;
    [SerializeField] private float _dashOvershoot = 14f;      // metros detrás del player donde queda frenado
    [SerializeField] private float _maxDashTime = 2f;

    [Header("Long Dash (dash + giro)")]
    [SerializeField, Range(0f, 1f)] private float _longDashChance = 0.4f;
    [SerializeField] private float _longDashRange = 35f;
    [SerializeField] private float _longDashMinDistance = 22f;
    [SerializeField] private float _longDashSpeed = 30f;
    [SerializeField] private float _longDashAccel = 40f;
    [SerializeField] private float _spinTriggerDistance = 8f; // empieza a girar a esta distancia ANTES de llegar
    [SerializeField] private float _longDashOvershoot = 12f;  // metros detrás del player donde queda frenado
    [SerializeField] private float _maxLongDashTime = 3f;

    [Header("Brake")]
    [SerializeField] private float _brakeDecel = 10f;
    [SerializeField] private float _brakeEndSpeed = 0.5f;
    [SerializeField] private float _maxBrakeTime = 4f;

    [Header("Spin - vuelta")]
    [SerializeField] private float _spinMaxSpeed = 900f;
    [SerializeField] private float _spinAccel = 3000f;
    [SerializeField] private float _spinDecel = 1500f;
    [SerializeField] private bool _spinShortestWay = true;

    [Header("Spin - pasarse y corregir")]
    [SerializeField] private float _overshootAngle = 25f;
    [SerializeField] private float _settleStiffness = 150f;
    [SerializeField, Range(0.1f, 1f)] private float _settleDamping = 0.45f;
    [SerializeField] private float _faceTolerance = 2f;
    [SerializeField] private float _settleEndSpeed = 15f;
    [SerializeField] private float _maxSettleTime = 1.5f;

    [Header("Inclinación (eje Z del pivot)")]
    [SerializeField] private float _chaseTilt = 8f;
    [SerializeField] private float _reverseTilt = -4f;
    [SerializeField] private float _windUpTilt = -5f;
    [SerializeField] private float _dashTilt = 22f;
    [SerializeField] private float _brakeTilt = -8f;
    [SerializeField] private float _spinTilt = 0f;
    [SerializeField] private float _tiltLerp = 5f;
    [SerializeField] private bool _invertTilt;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;

    private static readonly Quaternion ForwardFix = Quaternion.Euler(0f, 90f, 0f);

    private State _state;
    private float _stateTimer;
    private Vector3 _velocity;
    private float _currentTilt;
    private bool _reversing;

    private float _yaw;
    private Vector3 Forward => Quaternion.Euler(0f, _yaw, 0f) * Vector3.left;

    private Attack _nextAttack;
    private Vector3 _dashStartPos;
    private Vector3 _dashDir;
    private float _dashTargetDist;     // distancia total hasta donde tiene que quedar frenado
    private float _glideDecel;

    // Spin
    private SpinPhase _spinPhase;
    private float _spinSign = 1f;
    private float _spinSpeed;
    private float _spinAccumulated;
    private float _spinTargetYaw;
    private float _angVel;
    private float _settleTimer;

    private bool _warnedNoPlayer;

    private void OnEnable()
    {
        _yaw = transform.eulerAngles.y;
        _velocity = Vector3.zero;
        _currentTilt = 0f;
        if (_tiltPivot != null) _tiltPivot.localRotation = Quaternion.identity;
        StopExhaust(true);
        EnterState(State.Chase);
    }

    private bool EnsurePlayer()
    {
        if (_player != null && _player.gameObject.activeInHierarchy) return true;

        GameObject p = GameObject.FindGameObjectWithTag(_playerTag);
        if (p != null)
        {
            _player = p.transform;
            return true;
        }

        if (!_warnedNoPlayer)
        {
            Debug.LogWarning($"[SpikeBallEnemy] No encuentro ningún objeto con tag '{_playerTag}'", this);
            _warnedNoPlayer = true;
        }
        return false;
    }

    private void Update()
    {
        if (!EnsurePlayer()) return;

        _stateTimer += Time.deltaTime;

        switch (_state)
        {
            case State.Chase: UpdateChase(); break;
            case State.WindUp: UpdateWindUp(); break;
            case State.Dash: UpdateDash(); break;
            case State.LongDash: UpdateLongDash(); break;
            case State.SpinGlide: UpdateSpinGlide(); break;
            case State.Brake: UpdateBrake(); break;
            case State.Spin: UpdateSpin(); break;
        }

        transform.position += _velocity * Time.deltaTime;
        UpdateTilt();
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    // ---------- Chase / WindUp ----------

    private void UpdateChase()
    {
        RotateTowardsPlayer(_chaseTurnSpeed);

        float dist = FlatDirToPlayer().magnitude;
        float minDist = _nextAttack == Attack.LongDash ? _longDashMinDistance : _dashMinDistance;
        float maxDist = _nextAttack == Attack.LongDash ? _longDashRange : _dashRange;

        // Muy cerca: marcha atrás mirando al player
        _reversing = dist < minDist;
        Vector3 desired = _reversing ? -Forward * _reverseSpeed : Forward * _chaseSpeed;
        ApproachVelocity(desired, _chaseAccel, _brakeDecel);

        if (_stateTimer >= _minChaseTime && dist >= minDist && dist <= maxDist)
            EnterState(State.WindUp);
    }

    private void UpdateWindUp()
    {
        RotateTowardsPlayer(_windUpTurnSpeed);
        ApproachVelocity(Vector3.zero, _windUpDecel, _windUpDecel);

        if (_stateTimer >= _windUpTime)
            EnterState(_nextAttack == Attack.LongDash ? State.LongDash : State.Dash);
    }

    // ---------- Dash normal ----------

    private void UpdateDash()
    {
        ApproachVelocity(_dashDir * _dashSpeed, _dashAccel, _dashAccel);

        // Corta el empuje cuando la frenada lo va a dejar justo en el punto objetivo
        float traveled = Vector3.Dot(transform.position - _dashStartPos, _dashDir);
        float speed = _velocity.magnitude;
        float brakeDist = speed * speed / (2f * _brakeDecel);

        if (traveled + brakeDist >= _dashTargetDist || _stateTimer >= _maxDashTime)
            EnterState(State.Brake);
    }

    // ---------- Long Dash ----------

    private void UpdateLongDash()
    {
        ApproachVelocity(_dashDir * _longDashSpeed, _longDashAccel, _longDashAccel);

        float distAhead = Vector3.Dot(FlatDirToPlayer(), _dashDir);   // cuánto le falta para pasarlo

        if (distAhead <= _spinTriggerDistance || _stateTimer >= _maxLongDashTime)
            EnterState(State.SpinGlide);
    }

    private void UpdateSpinGlide()
    {
        // Recalcula la frenada para quedar parado _longDashOvershoot detrás del player
        float speed = _velocity.magnitude;
        float distToStop = Vector3.Dot(FlatDirToPlayer(), _dashDir) + _longDashOvershoot;
        if (distToStop > 0.5f)
            _glideDecel = Mathf.Max(speed * speed / (2f * distToStop), 1f);

        ApproachVelocity(Vector3.zero, _glideDecel, _glideDecel);

        _spinSpeed = Mathf.MoveTowards(_spinSpeed, _spinMaxSpeed, _spinAccel * Time.deltaTime);
        _yaw += _spinSpeed * Time.deltaTime * _spinSign;

        if (_velocity.magnitude <= _brakeEndSpeed || _stateTimer >= _maxBrakeTime)
        {
            _state = State.Spin;
            _stateTimer = 0f;
            if (_debugLogs) Debug.Log("[SpikeBallEnemy] -> Spin (desde SpinGlide)", this);
            BeginApproachToPlayer();
        }
    }

    // ---------- Brake / Spin ----------

    private void UpdateBrake()
    {
        ApproachVelocity(Vector3.zero, _brakeDecel, _brakeDecel);

        if (_velocity.magnitude <= _brakeEndSpeed || _stateTimer >= _maxBrakeTime)
            EnterState(State.Spin);
    }

    private void UpdateSpin()
    {
        ApproachVelocity(Vector3.zero, _brakeDecel, _brakeDecel);
        float dt = Time.deltaTime;

        if (_spinPhase != SpinPhase.Loop)
            _spinTargetYaw += Mathf.DeltaAngle(_spinTargetYaw, PlayerYaw());

        switch (_spinPhase)
        {
            case SpinPhase.Loop:
                {
                    _spinSpeed = Mathf.MoveTowards(_spinSpeed, _spinMaxSpeed, _spinAccel * dt);
                    float delta = _spinSpeed * dt;
                    _yaw += delta * _spinSign;
                    _spinAccumulated += delta;

                    if (_spinAccumulated >= 360f)
                        BeginApproachToPlayer();
                    break;
                }

            case SpinPhase.Approach:
                {
                    float overshootYaw = _spinTargetYaw + _overshootAngle * _spinSign;
                    float remaining = (overshootYaw - _yaw) * _spinSign;

                    if (remaining <= 0.5f)
                    {
                        _angVel = _spinSpeed * _spinSign;
                        _settleTimer = 0f;
                        _spinPhase = SpinPhase.Settle;
                        break;
                    }

                    float stopSpeed = Mathf.Sqrt(2f * _spinDecel * remaining);
                    float targetSpeed = Mathf.Min(_spinMaxSpeed, stopSpeed);
                    _spinSpeed = Mathf.MoveTowards(_spinSpeed, targetSpeed, Mathf.Max(_spinAccel, _spinDecel) * dt);
                    _spinSpeed = Mathf.Max(_spinSpeed, 60f);

                    float step = Mathf.Min(_spinSpeed * dt, remaining);
                    _yaw += step * _spinSign;
                    break;
                }

            case SpinPhase.Settle:
                {
                    _settleTimer += dt;
                    float error = _spinTargetYaw - _yaw;
                    float damping = 2f * _settleDamping * Mathf.Sqrt(_settleStiffness);

                    _angVel += (_settleStiffness * error - damping * _angVel) * dt;
                    _yaw += _angVel * dt;

                    bool settled = Mathf.Abs(error) <= _faceTolerance && Mathf.Abs(_angVel) <= _settleEndSpeed;
                    if (settled || _settleTimer >= _maxSettleTime)
                    {
                        _yaw = _spinTargetYaw;
                        EnterState(State.Chase);
                    }
                    break;
                }
        }
    }

    private void BeginApproachToPlayer()
    {
        float remaining = Mathf.Repeat(Mathf.DeltaAngle(_yaw, PlayerYaw()) * _spinSign, 360f);
        _spinTargetYaw = _yaw + remaining * _spinSign;
        _spinPhase = SpinPhase.Approach;
    }

    // ---------- Transiciones ----------

    private void EnterState(State newState)
    {
        bool wasDashing = _state == State.Dash || _state == State.LongDash;
        bool willDash = newState == State.Dash || newState == State.LongDash;
        if (wasDashing && !willDash) StopExhaust(false);

        _state = newState;
        _stateTimer = 0f;
        _reversing = false;

        if (_debugLogs) Debug.Log($"[SpikeBallEnemy] -> {newState}", this);

        switch (newState)
        {
            case State.Chase:
                _nextAttack = Random.value < _longDashChance ? Attack.LongDash : Attack.Dash;
                if (_debugLogs) Debug.Log($"[SpikeBallEnemy] próximo ataque: {_nextAttack}", this);
                break;

            case State.Dash:
            case State.LongDash:
                FaceTargetInstant();
                _dashDir = Forward;
                _dashStartPos = transform.position;
                _dashTargetDist = Vector3.Dot(FlatDirToPlayer(), _dashDir) + _dashOvershoot;
                if (_exhaustFire != null) _exhaustFire.Play(true);
                break;

            case State.SpinGlide:
                _spinSpeed = 0f;
                _glideDecel = _brakeDecel;
                PickSpinDirection();
                break;

            case State.Spin:
                _spinPhase = SpinPhase.Loop;
                _spinSpeed = 0f;
                _spinAccumulated = 0f;
                _angVel = 0f;
                PickSpinDirection();
                break;
        }
    }

    private void PickSpinDirection()
    {
        if (_spinShortestWay)
        {
            float signed = Mathf.DeltaAngle(_yaw, PlayerYaw());
            _spinSign = signed >= 0f ? 1f : -1f;
        }
        else
        {
            _spinSign = Random.value < 0.5f ? -1f : 1f;
        }
    }

    // ---------- Helpers ----------

    private float PlayerYaw()
    {
        Vector3 dir = FlatDirToPlayer();
        if (dir.sqrMagnitude < 0.001f) return _yaw;
        return (Quaternion.LookRotation(dir) * ForwardFix).eulerAngles.y;
    }

    private void ApproachVelocity(Vector3 desired, float accel, float decel)
    {
        float rate = desired.sqrMagnitude >= _velocity.sqrMagnitude ? accel : decel;
        _velocity = Vector3.MoveTowards(_velocity, desired, rate * Time.deltaTime);
    }

    private void RotateTowardsPlayer(float degPerSec)
    {
        _yaw = Mathf.MoveTowardsAngle(_yaw, PlayerYaw(), degPerSec * Time.deltaTime);
    }

    private void FaceTargetInstant()
    {
        _yaw = PlayerYaw();
    }

    private Vector3 FlatDirToPlayer()
    {
        Vector3 d = _player.position - transform.position;
        d.y = 0f;
        return d;
    }

    private void UpdateTilt()
    {
        if (_tiltPivot == null) return;

        float speed = _velocity.magnitude;
        float target = _state switch
        {
            State.Chase => _reversing ? _reverseTilt
                                          : _chaseTilt * Mathf.InverseLerp(0f, _chaseSpeed, speed),
            State.WindUp => _windUpTilt,
            State.Dash => _dashTilt,
            State.LongDash => _dashTilt,
            State.SpinGlide => _spinTilt,
            State.Brake => _brakeTilt * Mathf.InverseLerp(0f, _dashSpeed, speed),
            State.Spin => _spinTilt,
            _ => 0f
        };

        _currentTilt = Mathf.Lerp(_currentTilt, target, _tiltLerp * Time.deltaTime);
        float sign = _invertTilt ? -1f : 1f;
        _tiltPivot.localRotation = Quaternion.Euler(0f, 0f, _currentTilt * sign);
    }

    private void StopExhaust(bool clear)
    {
        if (_exhaustFire == null) return;
        _exhaustFire.Stop(true, clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p, _dashRange);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(p, _dashMinDistance);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(p, _longDashRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, _longDashMinDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(p, -transform.right * 3f);
    }
}