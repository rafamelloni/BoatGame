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
    public float manualTiltAmount = 10f;
    public float tiltSmoothSpeed = 5f;
    public float tiltReturnSpeed = 3f;
    public Animator anim;

    private float baseHeight;

    private float _currentExtraTiltX = 0f;
    private float _targetExtraTiltX = 0f;

    void Start()
    {
        baseHeight = transform.position.y;
    }

    void LateUpdate()
    {
        float t = Time.time;

        // --- ALTURA ---
        Vector3 pos = transform.position;
        pos.y = baseHeight + Mathf.Sin(t * frequency) * amplitude;
        transform.position = pos;

        // --- INTERPOLAR TILT EXTRA ---
        float smooth = _targetExtraTiltX != 0 ? tiltSmoothSpeed : tiltReturnSpeed;
        _currentExtraTiltX = Mathf.Lerp(
            _currentExtraTiltX,
            _targetExtraTiltX,
            Time.deltaTime * smooth
        );

        // --- TILT DE OLAS ---
        float tiltX = Mathf.Sin(t * tiltFrequency) * tiltAmplitude;
        float tiltZ = Mathf.Cos(t * tiltFrequency * 0.8f) * tiltAmplitude;

        tiltX += _currentExtraTiltX;

        float currentYaw = transform.rotation.eulerAngles.y;

        transform.rotation = Quaternion.Euler(
            tiltX,
            currentYaw,
            tiltZ
        );

        // si nadie llamó al método este frame, vuelve a 0 suavemente
        _targetExtraTiltX = 0f;
        if (anim != null)
        {
            anim.SetBool("IsSprinting", false);
        }
        

    }

    public void ApplyForwardTilt()
    {
        _targetExtraTiltX = -manualTiltAmount;
        if (anim != null)
        {
            anim.SetBool("IsSprinting", true);
        }
    }
}