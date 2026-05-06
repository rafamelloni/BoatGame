using UnityEngine;

public class FakeWaveMovement : MonoBehaviour
{
    [Header("Altura")]
    public float amplitude = 0.3f;
    public float frequency = 1.5f;

    [Header("Rotación / Inclinación")]
    public float tiltAmplitude = 5f;
    public float tiltFrequency = 1f;

    [Header("Forward Tilt")]
    public float manualTiltAmount = 25f;
    public float tiltSmoothSpeed = 10f;
    public float tiltReturnSpeed = 3f;
    private Rigidbody _rb;

    public Animator anim;

    private float baseHeight;
    private float _phaseOffset;
    private float _currentExtraTiltX = 0f;
    private float _targetExtraTiltX = 0f;
    private float _tiltHoldTimer = 0f;

    void Start()
    {
        baseHeight = transform.position.y;
        Debug.Log($"baseHeight: {baseHeight}");
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        _rb = GetComponent<Rigidbody>();
    }

    public float GetWaveY()
    {
        return baseHeight + Mathf.Sin((Time.fixedTime + _phaseOffset) * frequency) * amplitude;
    }
    void LateUpdate()
    {
        float t = Time.time + _phaseOffset;
        float before = _rb.position.y;

        if (_tiltHoldTimer > 0f) { _tiltHoldTimer -= Time.deltaTime; _targetExtraTiltX = -manualTiltAmount; }
        else { _targetExtraTiltX = 0f; }

        float smooth = _targetExtraTiltX != 0 ? tiltSmoothSpeed : tiltReturnSpeed;
        _currentExtraTiltX = Mathf.Lerp(_currentExtraTiltX, _targetExtraTiltX, Time.deltaTime * smooth);

        float tiltX = Mathf.Sin(t * tiltFrequency) * tiltAmplitude + _currentExtraTiltX;
        float tiltZ = Mathf.Cos(t * tiltFrequency * 0.8f) * tiltAmplitude;

        // Tomar el yaw actual del rb, no del transform
        float currentYaw = _rb.rotation.eulerAngles.y;
        _rb.MoveRotation(Quaternion.Euler(tiltX, currentYaw, tiltZ));
        float after = _rb.position.y;
        if (Mathf.Abs(after - before) > 0.01f)
            Debug.Log($"LateUpdate movió Y: {before} -> {after}");
    }
    public void ApplyForwardTilt(float duration)
    {
        _tiltHoldTimer = duration;
    }

    public void SetBaseHeight(float y)
    {
        baseHeight = y;
    }
}