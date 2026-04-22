using UnityEngine;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    private RT_PlayerStats _stats;
    public ParticleSystem trail;
    public ParticleSystem trail1;
    public ParticleSystem trail2;
    public ParticleSystem trailSprint;
    public ParticleSystem trailSprint1;
    public FakeWaveMovement fakeWaveMomenent;

    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.5f;
    public float sprintMaxDuration = 5f;
    public float sprintRechargeRate = 1f;   // segundos que recarga por segundo
    public float sprintDrainRate = 1f;      // segundos que consume por segundo
    public Image sprintBarImage;            // Image con Fill Method: Horizontal

    private float _sprintStamina;           // valor actual (0 a sprintMaxDuration)
    private bool _isSprinting = false;

    private float _currentSpeed = 0f;
    private float _smoothTurn = 0f;

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
        _sprintStamina = sprintMaxDuration;
    }

    void Update()
    {
        HandleSprint();

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Mathf.Clamp01(vertical);

        // --- GIRO ---
        float targetTurn = horizontal * _stats.turnSpeed;
        _smoothTurn = Mathf.Lerp(_smoothTurn, targetTurn, Time.deltaTime * 5f);
        transform.Rotate(0f, _smoothTurn * Time.deltaTime, 0f);

        float targetSpeed = _stats.moveSpeed * (_isSprinting ? sprintSpeedMultiplier : 1f);

        // --- ACELERACIÓN ---
        if (vertical != 0)
        {
            trail.Play();
            trail1.Play();
            trail2.Play();
            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                vertical * targetSpeed,
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
                Time.deltaTime * _stats.deceleration
            );
        }

        // --- MOVIMIENTO ---
        transform.position += transform.forward * _currentSpeed * Time.deltaTime;

        // --- UI ---
        if (sprintBarImage != null)
            sprintBarImage.fillAmount = _sprintStamina / sprintMaxDuration;
    }

    void HandleSprint()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        bool canSprint = _sprintStamina > 0f;

        if (shiftHeld && canSprint)
        {
            // Activar sprint
            if (!_isSprinting)
            {
                _isSprinting = true;
                trailSprint.Play();
                trailSprint1.Play();
                fakeWaveMomenent.ApplyForwardTilt(0.2f);
            }

            // Consumir stamina
            _sprintStamina -= sprintDrainRate * Time.deltaTime;
            _sprintStamina = Mathf.Max(_sprintStamina, 0f);

            // Se acabó la stamina → cortar sprint
            if (_sprintStamina <= 0f)
                StopSprint();
        }
        else
        {
            // Soltar Shift o sin stamina → cortar sprint
            if (_isSprinting)
                StopSprint();

            // Recargar stamina cuando no está sprintando
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