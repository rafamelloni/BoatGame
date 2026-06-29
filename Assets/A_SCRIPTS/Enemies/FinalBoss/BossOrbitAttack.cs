using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class BossOrbitAttack : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _player;

    [Header("Orbita")]
    [SerializeField] private float _orbitRadius = 12f;
    [SerializeField] private float _arcDegrees = 130f;
    [SerializeField] private float _duration = 3f;
    [SerializeField] private float _rotationSpeed = 6f;
    [SerializeField] private float _meshRotationOffset = -90f;
    [SerializeField] private ParticleSystem Poison;

    [Header("Poison Zone")]
    [SerializeField] private GameObject _poisonZonePrefab;
    [SerializeField] private float _poisonSpawnInterval = 0.15f;
    [SerializeField] private Transform _poisonSpawnPoint; // ← agregás

    private float _poisonTimer = 0f;

    public void Reset()
    {
        StopAllCoroutines();
        _isOrbiting = false;
    }

    private bool _isOrbiting = false;
    public bool IsOrbiting => _isOrbiting;

    public void DoOrbitAttack()
    {
        if (_isOrbiting) return;
        StartCoroutine(OrbitRoutine());
    }

    private IEnumerator OrbitRoutine()
    {
        _isOrbiting = true;
        Poison.Play();
        Vector3 startPos = transform.position;
        startPos.y = transform.position.y;

        // Angulo final: arcDegrees desde el angulo inicial relativo al player
        Vector3 dirFromPlayer = (startPos - _player.position);
        dirFromPlayer.y = 0f;
        float startAngle = Mathf.Atan2(dirFromPlayer.x, dirFromPlayer.z) * Mathf.Rad2Deg;
        float endAngle = startAngle + _arcDegrees;

        // Punto final en el circulo
        float endRad = endAngle * Mathf.Deg2Rad;
        Vector3 endPos = _player.position + new Vector3(Mathf.Sin(endRad), 0f, Mathf.Cos(endRad)) * _orbitRadius;
        endPos.y = transform.position.y;

        // Punto de control Bezier: perpendicular al punto medio del arco
        float midAngle = (startAngle + endAngle) / 2f;
        float midRad = midAngle * Mathf.Deg2Rad;
        Vector3 midOnCircle = _player.position + new Vector3(Mathf.Sin(midRad), 0f, Mathf.Cos(midRad)) * _orbitRadius;
        midOnCircle.y = transform.position.y;

        // Control point empuja la curva hacia afuera del circulo para que sea mas suave
        Vector3 outDir = (midOnCircle - _player.position).normalized;
        Vector3 controlPoint = midOnCircle + outDir * Vector3.Distance(startPos, midOnCircle) * 0.5f;

        float elapsed = 0f;
        Vector3 prevPos = startPos;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            _poisonTimer += Time.deltaTime; // ← agregás

            float t = Mathf.Clamp01(elapsed / _duration);

            // Bezier cuadratica
            Vector3 newPos = QuadraticBezier(startPos, controlPoint, endPos, t);
            newPos.y = transform.position.y;

            if (_poisonTimer >= _poisonSpawnInterval)
            {
                _poisonTimer = 0f;
                
                Vector3 spawnPos = _poisonSpawnPoint != null ? _poisonSpawnPoint.position : transform.position;
                Instantiate(_poisonZonePrefab, spawnPos, Quaternion.identity);
            }

            // Rotar hacia la direccion de movimiento
            Vector3 moveDir = (newPos - prevPos);
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
                targetRot *= Quaternion.Euler(0f, _meshRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }

            prevPos = newPos;
            transform.position = newPos;

            yield return null;
        }

        transform.position = endPos;
        _isOrbiting = false;
        Poison.Stop();
    }

    private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }



    public void ResetBoss()
    {
        StopAllCoroutines();
        _isOrbiting = false;
        _poisonTimer = 0f;
        if (Poison != null) Poison.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }


}