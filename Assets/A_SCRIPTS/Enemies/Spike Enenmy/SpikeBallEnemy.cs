using UnityEngine;

// Integrado al sistema de enemigos: hereda de Enemy (EnemyHealth + SO_EnemyData),
// avisa OnDead para que la EnemyFactory lo devuelva al pool, y funciona con
// EnemyEmerge, las fases, el tumulto y la oleada del EnemySpawner.
public class SpikeBallEnemy : Enemy
{
    public event System.Action<SpikeBallEnemy> OnDead;

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
    [Tooltip("Aviso extra ANTES de la carga: frena y apunta, el indicador ya se ve, pero todavía no se inclina. No hace más lenta la animación de carga.")]
    [SerializeField] private float _telegraphLeadTime = 0.6f;
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
    [Tooltip("Arranca el giro esta cantidad de segundos antes de pasar al player (a la velocidad actual). Se usa lo que llegue primero entre esto y Spin Trigger Distance.")]
    [SerializeField] private float _spinLeadTime = 0.45f;
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
    [Tooltip("En el dash normal, empieza a girar mientras todavía frena, cuando la velocidad baja de esto (no espera a quedar quieto)")]
    [SerializeField] private float _spinStartSpeed = 10f;
    [Tooltip("Velocidad de giro con la que arranca (fracción del máximo) para que el giro se note de entrada")]
    [SerializeField, Range(0f, 1f)] private float _spinKickstart = 0.35f;

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

    [Header("Inclinación - game feel")]
    [Tooltip("Inclinación hacia atrás al cargar el dash (anticipación). Se suma a Wind Up Tilt.")]
    [SerializeField] private float _windUpLeanBack = -20f;
    [Tooltip("Cuánto se aplasta el modelo al cargar (0.15 = 15%). Al salir se estira y rebota.")]
    [SerializeField] private float _windUpSquash = 0.15f;
    [Tooltip("Estirón al arrancar el dash")]
    [SerializeField] private float _dashStretch = 0.25f;
    [SerializeField] private float _squashStiffness = 220f;
    [SerializeField, Range(0.1f, 1f)] private float _squashDamping = 0.35f;
    [Tooltip("Golpe hacia adelante extra al arrancar el dash (grados)")]
    [SerializeField] private float _dashKickTilt = 18f;
    [Tooltip("Rigidez del resorte de inclinación (más = más rápido)")]
    [SerializeField] private float _tiltStiffness = 160f;
    [Tooltip("Amortiguación del resorte (menos = rebota más)")]
    [SerializeField, Range(0.1f, 1f)] private float _tiltDamping = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private static readonly Quaternion ForwardFix = Quaternion.Euler(0f, 90f, 0f);

    private State _state;
    private float _stateTimer;
    private Vector3 _velocity;
    private float _currentTilt;
    private float _tiltVel;
    private float _squash;      // >0 aplastado, <0 estirado
    private float _squashVel;
    private Vector3 _pivotBaseScale = Vector3.one;
    private bool _pivotScaleCached;
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

    [Header("Vida (si falta el SO)")]
    [Tooltip("Vida que usa si el prefab no tiene Base Data asignado")]
    [SerializeField] private float _fallbackMaxHealth = 300f;

    protected override void Awake()
    {
        if (_enemyHealth == null) _enemyHealth = GetComponent<EnemyHealth>();
        if (baseData == null)
        {
            Debug.LogWarning("[SpikeBallEnemy] El prefab no tiene Base Data (SO_EnemyData): uso vida por defecto. Corré Tools > Botes > Preparar Spike Enemy.", this);
            baseData = ScriptableObject.CreateInstance<SO_EnemyData>();
            baseData.maxHealth = _fallbackMaxHealth;
        }
        base.Awake();
        if (_enemyHealth != null) _enemyHealth.OnDeath += HandleDeath;
        else Debug.LogWarning("[SpikeBallEnemy] Falta EnemyHealth en el prefab.", this);
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null) _enemyHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath() => OnDead?.Invoke(this);

    // Lo llama la EnemyFactory al sacarlo del pool
    public void SetPlayer(Transform player)
    {
        _player = player;
        _warnedNoPlayer = false;
        if (_enemyHealth != null) _enemyHealth.Revive();
        ResetState();
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void ResetState()
    {
        _yaw = transform.eulerAngles.y;
        _velocity = Vector3.zero;
        _currentTilt = 0f;
        _tiltVel = 0f;
        _squash = 0f;
        _squashVel = 0f;
        if (_tiltPivot != null)
        {
            if (!_pivotScaleCached) { _pivotBaseScale = _tiltPivot.localScale; _pivotScaleCached = true; }
            _tiltPivot.localScale = _pivotBaseScale;
        }
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

        if (_stateTimer >= _telegraphLeadTime + _windUpTime)
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

        float trigger = Mathf.Max(_spinTriggerDistance, _velocity.magnitude * _spinLeadTime);
        if (distAhead <= trigger || _stateTimer >= _maxLongDashTime)
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

        if (_velocity.magnitude <= Mathf.Max(_brakeEndSpeed, _spinStartSpeed) || _stateTimer >= _maxBrakeTime)
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
                _tiltVel += _dashKickTilt * 12f; // latigazo hacia adelante
                _squash = -_dashStretch;          // estirón
                _squashVel = 0f;
                if (_exhaustFire != null) _exhaustFire.Play(true);
                break;

            case State.SpinGlide:
                _spinSpeed = _spinMaxSpeed * _spinKickstart;
                _glideDecel = _brakeDecel;
                PickSpinDirection();
                break;

            case State.Spin:
                _spinPhase = SpinPhase.Loop;
                _spinSpeed = _spinMaxSpeed * _spinKickstart;
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

    // ---------- Telegraph (lo lee SpikeDashTelegraph) ----------

    public bool IsWindingUp => _state == State.WindUp;
    public bool IsDashing => _state == State.Dash || _state == State.LongDash;
    // progreso del aviso completo (lead + carga) y cuánto falta para salir
    public float WindUpProgress01
    {
        get
        {
            float total = _telegraphLeadTime + _windUpTime;
            return total > 0f ? Mathf.Clamp01(_stateTimer / total) : 1f;
        }
    }
    public float TimeToDash => _state == State.WindUp ? Mathf.Max(0f, _telegraphLeadTime + _windUpTime - _stateTimer) : 0f;
    public bool NextAttackIsLong => _nextAttack == Attack.LongDash;

    public Vector3 PredictedDashDirection
    {
        get
        {
            Vector3 d = _player != null ? FlatDirToPlayer() : Forward;
            return d.sqrMagnitude > 0.001f ? d.normalized : Forward;
        }
    }

    // hasta donde va a quedar frenado: el player + el overshoot del ataque que viene
    public float PredictedDashLength =>
        (_player != null ? FlatDirToPlayer().magnitude : 0f) + (NextAttackIsLong ? _longDashOvershoot : _dashOvershoot);

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
            State.WindUp => _windUpTilt + _windUpLeanBack * WindUpProgress(),
            State.Dash => _dashTilt,
            State.LongDash => _dashTilt,
            State.SpinGlide => _spinTilt,
            State.Brake => _brakeTilt * Mathf.InverseLerp(0f, _dashSpeed, speed),
            State.Spin => _spinTilt,
            _ => 0f
        };

        // resorte: llega rápido, se pasa un poco y rebota (más vivo que un Lerp)
        float dt = Time.deltaTime;
        float damping = 2f * _tiltDamping * Mathf.Sqrt(_tiltStiffness);
        _tiltVel += (_tiltStiffness * (target - _currentTilt) - damping * _tiltVel) * dt;
        _currentTilt += _tiltVel * dt;

        // aplastado mientras carga
        float squashTarget = _state == State.WindUp ? _windUpSquash * WindUpProgress() : 0f;

        // squash & stretch con resorte (al salir del estirón rebota)
        float sDamp = 2f * _squashDamping * Mathf.Sqrt(_squashStiffness);
        _squashVel += (_squashStiffness * (squashTarget - _squash) - sDamp * _squashVel) * dt;
        _squash += _squashVel * dt;
        float sy = 1f - _squash;
        float sxz = 1f + _squash * 0.5f;
        _tiltPivot.localScale = Vector3.Scale(_pivotBaseScale, new Vector3(sxz, sy, sxz));

        float sign = _invertTilt ? -1f : 1f;
        _tiltPivot.localRotation = Quaternion.Euler(0f, 0f, _currentTilt * sign);
    }

    private float WindUpProgress()
    {
        // la animación de carga (inclinarse/aplastarse) usa solo el último tramo, después del aviso
        float k = _windUpTime > 0f ? Mathf.Clamp01((_stateTimer - _telegraphLeadTime) / _windUpTime) : 1f;
        return k * k * (3f - 2f * k); // suave al principio, firme al final
    }

    private void StopExhaust(bool clear)
    {
        if (_exhaustFire == null) return;
        _exhaustFire.Stop(true, clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting);
    }

    // Si te embiste durante el dash, frena (el daño y el knockback al player los hace PlayerCollisions por el tag ShipEnemy)
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(_playerTag)) return;
        if (_state == State.Dash || _state == State.LongDash || _state == State.SpinGlide)
        {
            _velocity *= 0.3f;
            EnterState(State.Brake);
        }
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