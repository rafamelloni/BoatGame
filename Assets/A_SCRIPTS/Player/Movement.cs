using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float turnSpeed = 90f;

    [Header("Inertia")]
    public float acceleration = 5f;
    public float deceleration = 4f;

    private float currentSpeed = 0f;
    private float smoothTurn = 0f;

    void Update()
    {
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        // --- ACELERACIÓN ---
        if (vertical != 0)
        {
            currentSpeed = Mathf.Lerp(
                currentSpeed,
                vertical * moveSpeed,
                Time.deltaTime * acceleration
            );
        }
        else
        {
            currentSpeed = Mathf.Lerp(
                currentSpeed,
                0f,
                Time.deltaTime * deceleration
            );
        }

        // Calcular movimiento antes de aplicarlo (para debug)
        Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
        float movementMagnitude = movement.magnitude / Time.deltaTime; // m/s reales

        // --- MOVIMIENTO ---
        transform.position += movement;

        // --- GIRO SUAVIZADO ---
        float targetTurn = horizontal * turnSpeed;
        smoothTurn = Mathf.Lerp(smoothTurn, targetTurn, Time.deltaTime * 5f);
        transform.Rotate(0f, smoothTurn * Time.deltaTime, 0f);

     
    }
}
