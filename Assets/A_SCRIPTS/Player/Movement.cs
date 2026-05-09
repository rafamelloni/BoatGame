using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Movement : MonoBehaviour
{
    private RT_PlayerStats _stats;
    private Rigidbody _rb;
    public ParticleSystem trail;
    public ParticleSystem trail1;
    public ParticleSystem trail2;
    public ParticleSystem trailSprint;
    public ParticleSystem trailSprint1;
    public PlayerWaveMovement playerWave;

    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.5f;
    public float sprintMaxDuration = 5f;
    public float sprintRechargeRate = 1f;
    public float sprintDrainRate = 1f;
    public Image sprintBarImage;

    [Header("Bank/Tilt")]
    public float bankAngle = 15f;
    public float bankSpeed = 5f;
    private float _currentBankZ = 0f;

    private bool _isKnockedBack = false;
    private Vector3 _knockbackVelocity;

    private float _sprintStamina;
    private bool _isSprinting = false;
    private float _currentSpeed = 0f;
    private float _smoothTurn = 0f;
    public float GetCurrentBankZ() => _currentBankZ;

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
        _rb = GetComponent<Rigidbody>();
        _sprintStamina = sprintMaxDuration;
    }

    private void Start()
    {
        SetMovementEnabled(false);
    }

    void Update()
    {
        if (!enabled) return;
        HandleSprint();

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Mathf.Clamp01(vertical);

        float targetTurn = horizontal * _stats.turnSpeed;
        _smoothTurn = Mathf.Lerp(_smoothTurn, targetTurn, Time.deltaTime * 5f);

        float targetSpeed = _stats.moveSpeed * (_isSprinting ? sprintSpeedMultiplier : 1f);

        if (vertical != 0)
        {
            trail.Play(); trail1.Play(); trail2.Play();
            _currentSpeed = Mathf.Lerp(_currentSpeed, vertical * targetSpeed, Time.deltaTime * _stats.acceleration);
        }
        else
        {
            trail.Stop(); trail1.Stop(); trail2.Stop();
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, Time.deltaTime * _stats.deceleration);
        }

        if (sprintBarImage != null)
            sprintBarImage.fillAmount = _sprintStamina / sprintMaxDuration;
    }

    void FixedUpdate()
    {
        if (!enabled) return;

        float newYaw = _rb.rotation.eulerAngles.y + _smoothTurn * Time.fixedDeltaTime;

        float targetBankZ = -(_smoothTurn / _stats.turnSpeed) * bankAngle;
        _currentBankZ = Mathf.Lerp(_currentBankZ, targetBankZ, Time.fixedDeltaTime * bankSpeed);

        Quaternion waveTilt = playerWave.GetWaveTilt(newYaw);
        Quaternion bankRot = Quaternion.Euler(0f, 0f, _currentBankZ);
        _rb.MoveRotation(waveTilt * bankRot);

        Vector3 move = _isKnockedBack
            ? _knockbackVelocity * Time.fixedDeltaTime
            : transform.forward * _currentSpeed * Time.fixedDeltaTime;

        Vector3 newPos = _rb.position + move;
        newPos.y = playerWave.GetWaveY();
        _rb.MovePosition(newPos);
    }

    void HandleSprint()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        float vertical = Input.GetAxisRaw("Vertical");
        bool canSprint = _sprintStamina > 0f && vertical > 0f;

        if (shiftHeld && canSprint)
        {
            if (!_isSprinting)
            {
                _isSprinting = true;
                trailSprint.Play();
                trailSprint1.Play();
            }

            playerWave.ApplyForwardTilt(0.2f);

            _sprintStamina -= sprintDrainRate * Time.deltaTime;
            _sprintStamina = Mathf.Max(_sprintStamina, 0f);

            if (_sprintStamina <= 0f)
                StopSprint();
        }
        else
        {
            if (_isSprinting)
                StopSprint();

            if (!shiftHeld)
            {
                _sprintStamina += sprintRechargeRate * Time.deltaTime;
                _sprintStamina = Mathf.Min(_sprintStamina, sprintMaxDuration);
            }
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        StartCoroutine(KnockbackRoutine(direction * force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 velocity, float duration)
    {
        _isKnockedBack = true;
        _knockbackVelocity = velocity;
        _currentSpeed = 0f;
        if (_isSprinting) StopSprint();

        yield return new WaitForSeconds(duration);
        _knockbackVelocity = Vector3.zero;
        yield return new WaitForSeconds(0.2f);
        _isKnockedBack = false;
    }

    void StopSprint()
    {
        _isSprinting = false;
        trailSprint.Stop();
        trailSprint1.Stop();
    }

    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
        float targetY = 7.49f;
        Vector3 pos = _rb.position;
        pos.y = targetY;
        _rb.position = pos;
        playerWave.SetBaseHeight(targetY);
    }
}