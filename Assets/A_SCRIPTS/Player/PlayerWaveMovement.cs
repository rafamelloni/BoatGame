using UnityEngine;

public class PlayerWaveMovement : MonoBehaviour
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
    private float _baseHeight;
    private float _phaseOffset;
    private float _currentExtraTiltX = 0f;
    private float _targetExtraTiltX = 0f;
    private float _tiltHoldTimer = 0f;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _baseHeight = _rb.position.y;
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

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

        float smooth = _targetExtraTiltX != 0f ? tiltSmoothSpeed : tiltReturnSpeed;
        _currentExtraTiltX = Mathf.Lerp(_currentExtraTiltX, _targetExtraTiltX, Time.deltaTime * smooth);
    }

    // Lo llama Movement.FixedUpdate — devuelve Y de la ola
    public float GetWaveY()
    {
        return _baseHeight + Mathf.Sin((Time.fixedTime + _phaseOffset) * frequency) * amplitude;
    }

    // Lo llama Movement.FixedUpdate — devuelve rotación final combinada
    public Quaternion GetWaveTilt(float currentYaw)
    {
        float t = Time.fixedTime + _phaseOffset;
        float tiltX = Mathf.Sin(t * tiltFrequency) * tiltAmplitude + _currentExtraTiltX;
        float tiltZ = Mathf.Cos(t * tiltFrequency * 0.8f) * tiltAmplitude;
        return Quaternion.Euler(tiltX, currentYaw, tiltZ);
    }

    public void ApplyForwardTilt(float duration)
    {
        _tiltHoldTimer = duration;
    }

    public void SetBaseHeight(float y)
    {
        _baseHeight = y;
    }


}