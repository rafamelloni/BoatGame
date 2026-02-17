using UnityEngine;

public class followPlayer : MonoBehaviour
{
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 15f, -15f);

    [Header("Camera Bob Settings")]
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 1.2f;
    public Vector3 bobDirection = new Vector3(0f, 0f, 1f);

    private Quaternion fixedRotation;

    void Start()
    {
        fixedRotation = transform.rotation;  // tu rotación isométrica fija
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- SEGUIMIENTO NORMAL ---
        Vector3 basePos = target.position + offset;

        // --- MOVIMIENTO DE BOB ---
        float t = Time.time;
        Vector3 bobOffset = bobDirection.normalized * (Mathf.Sin(t * bobFrequency) * bobAmplitude);

        // --- POSICIÓN FINAL ---
        transform.position = basePos + bobOffset;

        // --- ROTACIÓN FIJA ---
        transform.rotation = fixedRotation;
    }
}
