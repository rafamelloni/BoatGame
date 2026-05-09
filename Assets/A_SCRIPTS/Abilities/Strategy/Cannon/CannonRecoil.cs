using UnityEngine;

public class CannonRecoil : MonoBehaviour
{
    [Header("Recoil")]
    public float recoilDistance = 0.3f;

    private Vector3 _localOrigin;
    private Vector3 _recoilVelocity = Vector3.zero;
    private float _recoilTimer = 0f;
    private float _returnDuration = 0f;
    private bool _returning = false;

    void Start()
    {
        _localOrigin = transform.localPosition;
    }

    public void UpdateLocalOrgin()
    {
        _localOrigin = transform.localPosition;
    }

    public void Fire(float fireRate)
    {
        transform.localPosition = _localOrigin - transform.localRotation * -Vector3.right * recoilDistance; _recoilTimer = fireRate;
        _returnDuration = fireRate;
        _returning = true;
        _recoilVelocity = Vector3.zero;
    }

    void Update()
    {
        if (!_returning) return;

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            _localOrigin,
            ref _recoilVelocity,
            _returnDuration
        );

        _recoilTimer -= Time.deltaTime;
        if (_recoilTimer <= 0f)
        {
            transform.localPosition = _localOrigin;
            _returning = false;
        }
    }
}