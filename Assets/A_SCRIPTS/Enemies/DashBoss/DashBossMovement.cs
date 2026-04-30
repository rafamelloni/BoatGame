using System.Collections;
using UnityEngine;

public class DashBossMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _rotationSpeed = 4f;
    [SerializeField] private float _rotationAngle;
    [Header("Dash")]
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private AnimationCurve _dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _stopDistanceFromPlayer = 3f;
    [SerializeField] private ParticleSystem _dashParticles;
    [SerializeField] private float _tiltAngle = 25f;
    [SerializeField] private float _tiltSpeed = 8f;

    [SerializeField] private ParticleSystem _dashParticlesSmoke;      
    [SerializeField] private ParticleSystem _dashParticlesFire;
    public float StopDistance => _stopDistanceFromPlayer;
    private Transform _player;
    private bool _isLocked;

    public void SetPlayer(Transform player) => _player = player;
    public void LockRotation(bool value) => _isLocked = value;

    private void Update()
    {
        if (_player == null) return;
        if (_isLocked) return;
        RotateTowardsPlayer();
    }

    private void RotateTowardsPlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        _dashParticlesSmoke.Play();
    }

    public IEnumerator ExecuteDash(Vector3 destination, float durationOverride = -1f, bool revertTilt = true)
    {
        _dashParticles.Play();
         
        //yield return new WaitForSeconds(0.2f); // ajustá según el feel
        _dashParticlesSmoke.Stop();
        _dashParticlesFire.Play();

        float duration = durationOverride > 0f ? durationOverride : _dashDuration;
        destination.y = transform.position.y;
        Vector3 origin = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = _dashCurve.Evaluate(elapsed / duration);
            transform.position = Vector3.LerpUnclamped(origin, destination, t);
            yield return null;
        }
        transform.position = destination;
        if (revertTilt)
            StartCoroutine(TiltX(0f));
    }
    public IEnumerator RotateToFace(Vector3 target, float tolerance = 5f, float timeout = 2f, float speedOverride = -1f)
    {
        float speed = speedOverride > 0f ? speedOverride : _rotationSpeed;
        float elapsed = 0f;
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        while (Quaternion.Angle(transform.rotation, targetRot) > tolerance)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) break;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, speed * Time.deltaTime);
            yield return null;
        }

        _dashParticlesSmoke.Play();
        StartCoroutine(TiltX(_tiltAngle));
    }

    public void RotateBroadside()
    {
        if (_player == null) return;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, _rotationAngle, 0f);
        if (Quaternion.Angle(transform.rotation, targetRot) < 2f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
    }
    public IEnumerator TiltX(float targetAngle)
    {
        float elapsed = 0f;
        Quaternion baseRot = transform.rotation;
        Quaternion tiltRot = baseRot * Quaternion.Euler(targetAngle, 0f, 0f);
        float duration = 1f / _tiltSpeed;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(baseRot, tiltRot, elapsed / duration);
            yield return null;
        }
    }
}