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
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);

    [Header("Cannon Impact")]
    public float zoomPunchAmount = 2f;    // cuánto se aleja la cámara (Y)
    public float zoomPunchDuration = 0.4f;
    public float shakeStrength = 0.3f;
    public float shakeDuration = 0.3f;

    private Quaternion fixedRotation;

    // zoom punch
    private float _zoomOffset = 0f;
    private float _zoomVelocity = 0f;
    private float _zoomTimer = 0f;

    // shake
    private float _shakeTimer = 0f;
    private Vector3 _shakeOffset = Vector3.zero;

    void Start()
    {
        fixedRotation = transform.rotation;
    }

    public void ApplyCannonImpact()
    {
        _zoomOffset = zoomPunchAmount;
        _zoomTimer = zoomPunchDuration;
        _shakeTimer = shakeDuration;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- ZOOM PUNCH ---
        if (_zoomTimer > 0f)
        {
            _zoomTimer -= Time.deltaTime;
            _zoomOffset = Mathf.SmoothDamp(_zoomOffset, 0f, ref _zoomVelocity, zoomPunchDuration);
        }
        else
        {
            _zoomOffset = 0f;
            _zoomVelocity = 0f;
        }

        // --- SHAKE ---
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float strength = (_shakeTimer / shakeDuration) * shakeStrength; // se va apagando
            _shakeOffset = new Vector3(
                Random.Range(-strength, strength),
                0f,
                Random.Range(-strength, strength)
            );
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }

        // --- BOB ---
        float t = Time.time;
        Vector3 bobOffset = bobDirection.normalized * (Mathf.Sin(t * bobFrequency) * bobAmplitude);

        Vector3 basePos = target.position + offset;
        basePos.y += _zoomOffset; // zoom = aleja en Y

        Vector3 finalPos = basePos + bobOffset + _shakeOffset;

        // --- CLAMP XZ ---
        finalPos.x = Mathf.Clamp(finalPos.x, minBounds.x, maxBounds.x);
        finalPos.z = Mathf.Clamp(finalPos.z, minBounds.y, maxBounds.y);

        transform.position = finalPos;
        transform.rotation = fixedRotation;
    }
}