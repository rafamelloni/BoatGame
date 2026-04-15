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

    [Header("World Bounds")]
    public Vector2 minBounds = new Vector2(-50f, -50f); // X y Z mínimos
    public Vector2 maxBounds = new Vector2(50f, 50f);   // X y Z máximos

    private Quaternion fixedRotation;

    void Start()
    {
        fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 basePos = target.position + offset;

        // --- BOB ---
        float t = Time.time;
        Vector3 bobOffset = bobDirection.normalized * (Mathf.Sin(t * bobFrequency) * bobAmplitude);

        Vector3 finalPos = basePos + bobOffset;

        // --- CLAMP XZ ---
        finalPos.x = Mathf.Clamp(finalPos.x, minBounds.x, maxBounds.x);
        finalPos.z = Mathf.Clamp(finalPos.z, minBounds.y, maxBounds.y);

        transform.position = finalPos;
        transform.rotation = fixedRotation;
    }
}