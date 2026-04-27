using UnityEngine;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    private RT_PlayerStats _stats;
    private Rigidbody _rb;
    public ParticleSystem trail;
    public ParticleSystem trail1;
    public ParticleSystem trail2;
    public ParticleSystem trailSprint;
    public ParticleSystem trailSprint1;
    public FakeWaveMovement fakeWaveMomenent;

    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.5f;
    public float sprintMaxDuration = 5f;
    public float sprintRechargeRate = 1f;
    public float sprintDrainRate = 1f;
    public Image sprintBarImage;

    private float _sprintStamina;
    private bool _isSprinting = false;
    private float _currentSpeed = 0f;
    private float _smoothTurn = 0f;

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
        _rb = GetComponent<Rigidbody>();
        _sprintStamina = sprintMaxDuration;
    }

 

    void Update()
    {
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
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, _smoothTurn * Time.fixedDeltaTime, 0f));

        Vector3 move = transform.forward * _currentSpeed * Time.fixedDeltaTime;
        Vector3 newPos = _rb.position + move;
        newPos.y = fakeWaveMomenent.GetWaveY(); // asignación directa, no suma
        _rb.MovePosition(newPos);
    }

    void HandleSprint()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        bool canSprint = _sprintStamina > 0f;

        if (shiftHeld && canSprint)
        {
            if (!_isSprinting)
            {
                _isSprinting = true;
                trailSprint.Play();
                trailSprint1.Play();
                fakeWaveMomenent.ApplyForwardTilt(0.2f);
            }
            _sprintStamina -= sprintDrainRate * Time.deltaTime;
            _sprintStamina = Mathf.Max(_sprintStamina, 0f);
            if (_sprintStamina <= 0f)
                StopSprint();
        }
        else
        {
            if (_isSprinting)
                StopSprint();
            _sprintStamina += sprintRechargeRate * Time.deltaTime;
            _sprintStamina = Mathf.Min(_sprintStamina, sprintMaxDuration);
        }
    }

    void StopSprint()
    {
        _isSprinting = false;
        trailSprint.Stop();
        trailSprint1.Stop();
    }
}