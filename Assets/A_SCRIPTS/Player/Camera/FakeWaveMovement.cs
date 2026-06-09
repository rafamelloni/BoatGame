using UnityEngine;

public class FakeWaveMovement : MonoBehaviour
{
    [Header("Rotación / Inclinación")]
    public float tiltAmplitude = 5f;
    public float tiltFrequency = 1f;

    [Header("Forward Tilt")]
    public float manualTiltAmount = 25f;
    public float tiltSmoothSpeed = 10f;
    public float tiltReturnSpeed = 3f;

    public Animator anim;

    private float _phaseOffset;
    private float _currentExtraTiltX = 0f;
    private float _targetExtraTiltX = 0f;
    private float _tiltHoldTimer = 0f;
    private float _yaw = 0f;

    void Start()
    {
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        _yaw = transform.eulerAngles.y;
    }

    public void SetYaw(float yaw) => _yaw = yaw;

    void Update()
    {
        if (_tiltHoldTimer > 0f)
        {
            _tiltHoldTimer -= Time.deltaTime;
            _targetExtraTiltX = -manualTiltAmount;
        }
        else
        {
            _targetExtraTiltX = 0f;
        }

        float smooth = _targetExtraTiltX != 0 ? tiltSmoothSpeed : tiltReturnSpeed;
        _currentExtraTiltX = Mathf.Lerp(_currentExtraTiltX, _targetExtraTiltX, Time.deltaTime * smooth);
    }

    void LateUpdate()
    {
        float t = Time.time + _phaseOffset;
        float tiltX = Mathf.Sin(t * tiltFrequency) * tiltAmplitude + _currentExtraTiltX;
        float tiltZ = Mathf.Cos(t * tiltFrequency * 0.8f) * tiltAmplitude;
        transform.rotation = Quaternion.Euler(tiltX, _yaw, tiltZ);
    }

    public void ApplyForwardTilt(float duration)
    {
        _tiltHoldTimer = duration;
    }
}