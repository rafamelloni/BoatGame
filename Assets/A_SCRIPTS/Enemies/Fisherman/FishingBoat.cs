using UnityEngine;

public class FishingBoat : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float _detectionRadius = 15f;
    [SerializeField] private float _safeDistance = 40f;
    [SerializeField] private Transform _player;

    [Header("Movement")]
    [SerializeField] private float _fleeSpeed = 20f;
    [SerializeField] private float _rotationSpeed = 5f;

    [Header("Idle")]
    [SerializeField] private float _idleDriftSpeed = 0.3f;
    [SerializeField] private float _idleRotationSpeed = 0.4f;
    [SerializeField] private float _idleNoiseScale = 0.3f;

    private bool _isFleeing = false;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (!_isFleeing && distance <= _detectionRadius)
            _isFleeing = true;
        else if (_isFleeing && distance >= _safeDistance)
            _isFleeing = false;

        if (_isFleeing)
            Flee();
        else
            IdleDrift();
    }

    private void IdleDrift()
    {
        float noiseX = (Mathf.PerlinNoise(Time.time * _idleNoiseScale, 0f) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(0f, Time.time * _idleNoiseScale) - 0.5f) * 2f;

        Vector3 driftDirection = new Vector3(noiseX, 0f, noiseZ).normalized;
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, driftDirection * _idleDriftSpeed, 0.05f);

        float rotationNoise = (Mathf.PerlinNoise(Time.time * _idleNoiseScale, 99f) - 0.5f) * 2f;
        _rb.angularVelocity = new Vector3(0f, rotationNoise * _idleRotationSpeed, 0f);
    }

    private void Flee()
    {
        Vector3 fleeDirection = (transform.position - _player.position).normalized;
        fleeDirection.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(fleeDirection) * Quaternion.Euler(0f, -90f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);

        _rb.linearVelocity = transform.right * _fleeSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _safeDistance);
    }
}