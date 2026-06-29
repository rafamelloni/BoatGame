using System.Collections;
using UnityEngine;

public class FinalBossDash : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _player;
    [SerializeField] private DashCircleIndicator _dashCirclePrefab;

    [Header("Dash")]
    [SerializeField] private float _dashDuration = 0.3f;
    [SerializeField] private float _stopDistanceFromPlayer = 4f;
    [SerializeField] private float _telegraphDuration = 1f;
    [SerializeField] private AnimationCurve _dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rotacion")]
    [SerializeField] private float _rotationSpeed = 4f;
    [SerializeField] private float _tiltAngle = -25f;
    [SerializeField] private float _tiltSpeed = 8f;

    [Header("Particulas")]
    [SerializeField] private ParticleSystem _dashParticles;
    [SerializeField] private ParticleSystem _dashParticlesSmoke;
    [SerializeField] private ParticleSystem _dashParticlesFire;

    private bool _isDashing = false;
    public bool IsDashing => _isDashing;

    public void SetPlayer(Transform player) => _player = player;

    public void DoDash()
    {
        if (_isDashing) return;
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;

        // Rotar hacia el player
        yield return StartCoroutine(RotateToFace(_player.position));

        // Calcular destino
        Vector3 dir = (_player.position - transform.position);
        dir.y = 6f;
        Vector3 destination = _player.position - dir.normalized * _stopDistanceFromPlayer;
        destination.y = transform.position.y;

        // Telegraph
        bool shouldDash = false;
        DashCircleIndicator indicator = SpawnIndicator(destination, _telegraphDuration, () => shouldDash = true);
        yield return new WaitUntil(() => shouldDash);
        Destroy(indicator.gameObject);

        // Dash
        yield return StartCoroutine(ExecuteDash(destination));

        _isDashing = false;
    }

    private IEnumerator ExecuteDash(Vector3 destination)
    {
        if (_dashParticles != null) _dashParticles.Play();
        if (_dashParticlesSmoke != null) _dashParticlesSmoke.Stop();
        if (_dashParticlesFire != null) _dashParticlesFire.Play();

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
        StartCoroutine(TiltX(0f));
    }

    private IEnumerator RotateToFace(Vector3 target, float tolerance = 5f, float timeout = 2f)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        float elapsed = 0f;

        while (Quaternion.Angle(transform.rotation, targetRot) > tolerance)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) break;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            yield return null;
        }

        if (_dashParticlesSmoke != null) _dashParticlesSmoke.Play();
        StartCoroutine(TiltX(_tiltAngle));
    }

    private IEnumerator TiltX(float targetAngle)
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

    private DashCircleIndicator SpawnIndicator(Vector3 destination, float duration, System.Action onComplete = null)
    {
        DashCircleIndicator indicator = Instantiate(_dashCirclePrefab, destination, Quaternion.identity);
        indicator.Play(duration, onComplete);
        return indicator;
    }
    public void ResetBoss()
    {
        StopAllCoroutines();
        _isDashing = false;
        if (_dashParticles != null) _dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_dashParticlesSmoke != null) _dashParticlesSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_dashParticlesFire != null) _dashParticlesFire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }


}