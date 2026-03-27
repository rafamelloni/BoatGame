using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Movement : MonoBehaviour
{
    private RT_PlayerStats _stats;
    public ParticleSystem trail;
    public ParticleSystem trail1;

    public ParticleSystem trailSprint;
    public ParticleSystem trailSprint1;
    public FakeWaveMovement fakeWaveMomenent;

    private float _currentSpeed = 0f;
    private float _smoothTurn = 0f;

    private void Awake()
    {
        _stats = GetComponent<RT_PlayerStats>();
    }

    void Update()
    {
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        vertical = Mathf.Clamp01(vertical);

        //sprint prototype
        bool sprint = Input.GetKey(KeyCode.LeftShift);

        float moveSpeed = _stats.moveSpeed;
        if (sprint && vertical > 0)
        {
            moveSpeed *= _stats.sprintMultiplier;
            trailSprint.Play();
            trailSprint1.Play();
            fakeWaveMomenent.ApplyForwardTilt();
        }else if (!sprint)
        {
            trailSprint.Stop();
            trailSprint1.Stop();
        }


        // --- ACELERACIÓN ---
        if (vertical != 0)
        {
            trail.Play();

            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                vertical * moveSpeed,
                Time.deltaTime * _stats.acceleration
            );
        }
        else
        {
            trail.Stop();

            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                0f,
                Time.deltaTime * _stats.deceleration
            );
        }

        // --- MOVIMIENTO ---
        transform.position += transform.forward * _currentSpeed * Time.deltaTime;

        // --- GIRO SUAVIZADO ---
        float targetTurn = horizontal * _stats.turnSpeed;
        _smoothTurn = Mathf.Lerp(_smoothTurn, targetTurn, Time.deltaTime * 5f);
        transform.Rotate(0f, _smoothTurn * Time.deltaTime, 0f);
    }
}