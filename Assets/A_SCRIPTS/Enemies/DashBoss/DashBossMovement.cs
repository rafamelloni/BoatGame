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
    }

    public IEnumerator ExecuteDash(Vector3 destination)
    {
        destination.y = transform.position.y;
        Vector3 origin = transform.position;
        float elapsed = 0f;
        while (elapsed < _dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = _dashCurve.Evaluate(elapsed / _dashDuration);
            transform.position = Vector3.LerpUnclamped(origin, destination, t);
            yield return null;
        }
        transform.position = destination;
    }

    public IEnumerator RotateToFace(Vector3 target, float tolerance = 5f, float timeout = 2f)
    {
        float elapsed = 0f;
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        while (Quaternion.Angle(transform.rotation, targetRot) > tolerance)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) break;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void RotateBroadside()
    {
        if (_player == null) return;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, _rotationAngle, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
    }
}