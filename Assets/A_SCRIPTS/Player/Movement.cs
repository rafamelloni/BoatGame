using UnityEngine;

public class Movement : MonoBehaviour
{
    private RT_PlayerStats _stats;
    public ParticleSystem trail;
    public ParticleSystem trail1;
    public ParticleSystem trail2;
    public ParticleSystem trailSprint;
    public ParticleSystem trailSprint1;
    public FakeWaveMovement fakeWaveMomenent;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.5f;
    public float shiftTapThreshold = 0.2f;
    public float dashDecelerationMultiplier = 0.3f;

    private float _currentSpeed = 0f;
    private float _smoothTurn = 0f;

    private bool _isDashing = false;
    private float _dashTimer = 0f;
    private float _cooldownTimer = 0f;

    private float _shiftHeldTime = 0f;
    private bool _shiftWasDown = false;

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
    }

    void Update()
    {
        HandleDashInput();

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Mathf.Clamp01(vertical);

        // --- GIRO (siempre activo, incluso durante dash) ---
        float targetTurn = horizontal * _stats.turnSpeed;
        _smoothTurn = Mathf.Lerp(_smoothTurn, targetTurn, Time.deltaTime * 5f);
        transform.Rotate(0f, _smoothTurn * Time.deltaTime, 0f);

        if (_isDashing)
        {
            UpdateDash();
            return;
        }

        // --- ACELERACIÓN ---
        if (vertical != 0)
        {
            trail.Play();
            trail1.Play();
            trail2.Play();
            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                vertical * _stats.moveSpeed,
                Time.deltaTime * _stats.acceleration
            );
        }
        else
        {
            trail.Stop();
            trail1.Stop();
            trail2.Stop();
            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                0f,
                Time.deltaTime * _stats.deceleration * (_currentSpeed > _stats.moveSpeed ? dashDecelerationMultiplier : 1f)
            );
        }

        // --- MOVIMIENTO ---
        transform.position += transform.forward * _currentSpeed * Time.deltaTime;
    }

    void HandleDashInput()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _shiftHeldTime = 0f;
            _shiftWasDown = true;
        }

        if (Input.GetKey(KeyCode.LeftShift) && _shiftWasDown)
            _shiftHeldTime += Time.deltaTime;

        if (Input.GetKeyUp(KeyCode.LeftShift) && _shiftWasDown)
        {
            if (_shiftHeldTime <= shiftTapThreshold && _cooldownTimer <= 0f && !_isDashing)
                StartDash();

            _shiftWasDown = false;
            _shiftHeldTime = 0f;
        }
    }

    void StartDash()
    {
        _isDashing = true;
        _dashTimer = dashDuration;
        _cooldownTimer = dashCooldown;

        trail.Stop();
        trail1.Stop();
        trail2.Stop();
        trailSprint.Play();
        trailSprint1.Play();
        fakeWaveMomenent.ApplyForwardTilt(dashDuration);
    }

    void UpdateDash()
    {
        _dashTimer -= Time.deltaTime;
        transform.position += transform.forward * dashSpeed * Time.deltaTime;

        if (_dashTimer <= 0f)
        {
            _isDashing = false;
            _currentSpeed = dashSpeed;
            trailSprint.Stop();
            trailSprint1.Stop();
        }
    }
}