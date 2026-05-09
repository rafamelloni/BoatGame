using UnityEngine;

public class CannonTiltCompensation : MonoBehaviour
{
    public bool instantCompensation = false;
    public bool invertCompensation = false;
    public float lerpSpeed = 8f;

    private Movement _movement;
    private Quaternion _localRestRotation;

    void Start()
    {
        _movement = GetComponentInParent<Movement>();
        _localRestRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        float bankZ = _movement.GetCurrentBankZ();
        float compensationZ = invertCompensation ? bankZ : -bankZ;
        Quaternion compensation = Quaternion.Euler(0f, 0f, compensationZ);
        Quaternion target = _localRestRotation * compensation;

        transform.localRotation = instantCompensation
            ? target
            : Quaternion.Lerp(transform.localRotation, target, Time.deltaTime * lerpSpeed);
    }
}