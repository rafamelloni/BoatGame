using UnityEngine;

public class Movement : MonoBehaviour
{
    private PlayerStats stats;

    private float _currentSpeed = 0f;
    private float _smoothTurn = 0f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        // --- ACELERACIÓN ---
        if (vertical != 0)
        {
            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                vertical * stats.moveSpeed,
                Time.deltaTime * stats.acceleration
            );
        }
        else
        {
            _currentSpeed = Mathf.Lerp(
                _currentSpeed,
                0f,
                Time.deltaTime * stats.deceleration
            );
        }

        // --- MOVIMIENTO ---
        transform.position += transform.forward * _currentSpeed * Time.deltaTime;

        // --- GIRO SUAVIZADO ---
        float targetTurn = horizontal * stats.turnSpeed;
        _smoothTurn = Mathf.Lerp(_smoothTurn, targetTurn, Time.deltaTime * 5f);
        transform.Rotate(0f, _smoothTurn * Time.deltaTime, 0f);
    }
}