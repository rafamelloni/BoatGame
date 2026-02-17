using UnityEngine;

public class FakeWaveMovement : MonoBehaviour
{
    [Header("Altura")]
    public float amplitude = 0.3f;
    public float frequency = 1.5f;

    [Header("Rotación / Inclinación")]
    public float tiltAmplitude = 5f;
    public float tiltFrequency = 1f;

    private float baseHeight;

    void Start()
    {
        baseHeight = transform.position.y;
    }

    void LateUpdate()
    {
        float t = Time.time;

        // --- SOLO ALTURA ---
        Vector3 pos = transform.position;
        pos.y = baseHeight + Mathf.Sin(t * frequency) * amplitude;
        transform.position = pos;

        // --- SOLO TILT (NO TOCAR EL YAW) ---
        float tiltX = Mathf.Sin(t * tiltFrequency) * tiltAmplitude;
        float tiltZ = Mathf.Cos(t * tiltFrequency * 0.8f) * tiltAmplitude;

        // Guardamos Y actual (rotation.y que viene del Movement)
        float currentYaw = transform.rotation.eulerAngles.y;

        // Aplicamos solo inclinación X/Z
        transform.rotation = Quaternion.Euler(
            tiltX,
            currentYaw,   //  AHORA EL BARCO PUEDE GIRAR CON A/D
            tiltZ
        );
    }
}
