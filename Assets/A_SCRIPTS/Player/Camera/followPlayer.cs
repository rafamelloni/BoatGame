using UnityEngine;

public class followPlayer : MonoBehaviour
{
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 15f, -15f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private float forwardCompensation = 3f;

    [Header("World Bounds")]
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);

    [Header("Cannon Impact")]
    public float zoomPunchAmount = 2f;
    public float zoomPunchDuration = 0.4f;
    public float shakeStrength = 0.3f;
    public float shakeDuration = 0.3f;

    private Quaternion fixedRotation;
    private Rigidbody _targetRb;

    // zoom punch
    private float _zoomOffset = 0f;
    private float _zoomVelocity = 0f;
    private float _zoomTimer = 0f;

    // shake
    private float _shakeTimer = 0f;
    private Vector3 _shakeOffset = Vector3.zero;

    // smooth follow
    private Vector3 _currentVelocity = Vector3.zero;

    void Start()
    {
        fixedRotation = transform.rotation;
        if (target != null)
        {
            _targetRb = target.GetComponent<Rigidbody>();
            transform.position = target.position + offset;
        }
    }

    public void ApplyCannonImpact()
    {
        _zoomOffset = zoomPunchAmount;
        _zoomTimer = zoomPunchDuration;
        _shakeTimer = shakeDuration;
    }

    void FixedUpdate()
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
            float strength = (_shakeTimer / shakeDuration) * shakeStrength;
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

        // --- DESIRED ---
        Vector3 basePos = _targetRb != null ? _targetRb.position : target.position;
        Vector3 desiredPos = basePos + offset;
        float compensation = forwardCompensation * Mathf.Min(target.forward.z, 0f);
        desiredPos += Vector3.forward * compensation;
        desiredPos.y += _zoomOffset;
        desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
        desiredPos.z = Mathf.Clamp(desiredPos.z, minBounds.y, maxBounds.y);

        // --- SMOOTH FOLLOW ---
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, desiredPos, ref _currentVelocity, smoothTime);
        transform.position = smoothedPos + _shakeOffset;
        transform.rotation = fixedRotation;
    }
}