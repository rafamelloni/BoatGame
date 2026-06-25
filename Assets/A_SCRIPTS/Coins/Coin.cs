using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float _bobAmplitude = 0.2f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _rotationSpeed = 90f;

    [Header("Pickup")]
    [SerializeField] private float _magnetRadius;
    [SerializeField] private float _orbitRadius = 0.5f;
    [SerializeField] private float _magnetSpeed = 8f;

    [Header("Player Avoidance")]
    [SerializeField] private float _avoidRadius = 1.5f;      // distancia a la que empieza a esquivar
    [SerializeField] private float _avoidStrength = 5f;      // fuerza del desvio

    [Header("Arrival")]
    [SerializeField] private float _arrivalRadius = 3f;      // distancia al cofre para empezar el arco
    [SerializeField] private float _arcHeight = 2f;          // altura maxima del arco
    [SerializeField] private float _descendSpeed = 4f;       // velocidad de bajada final
    [SerializeField] private float _scaleDownDistance = 3f;  // distancia al cofre para empezar a achicarse

    private float _baseY;
    private Transform _target;
    private Transform _player;
    private Vector3 _velocity;

    private enum State { Float, Pull, Arrive }
    private State _state = State.Float;

    public event System.Action<Coin> OnCollected;

    [SerializeField] private TrailRenderer _trail;
    public static event System.Action OnCoinArriving;
    public static event System.Action OnCoinCollected;


    private void OnDisable()
    {
        if (_trail != null)
            _trail.Clear();
        transform.localScale = Vector3.one;
        StopAllCoroutines();
    }

    public void Init(Transform target, Transform player, Vector3 spawnPos, float pickupRange)
    {
        _target = target;
        _player = player;
        transform.position = spawnPos;
        _baseY = spawnPos.y;
        _state = State.Float;
        _magnetRadius = pickupRange;
        _velocity = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (_target == null) return;

        float distToTarget = Vector3.Distance(transform.position, _target.position);

        // Achicarse cuando esta cerca del cofre
        if (_state == State.Pull || _state == State.Arrive)
        {
            float t = 1f - Mathf.Clamp01(distToTarget / _scaleDownDistance);
            float scale = Mathf.Lerp(1f, 0.1f, t);
            transform.localScale = Vector3.one * scale;
        }

        switch (_state)
        {
            case State.Float:
                DoFloat();
                if (distToTarget <= _magnetRadius)
                    _state = State.Pull;
                break;

            case State.Pull:
                DoPull();
                if (distToTarget <= _arrivalRadius)
                {
                    OnCoinArriving?.Invoke(); // agregá esto
                    _state = State.Arrive;
                }
                   
                break;

            case State.Arrive:
                DoArrive();
                break;
        }
    }

    private void DoFloat()
    {
        float newY = _baseY + Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
    }

    private void DoPull()
    {
        Vector3 toTarget = (_target.position - transform.position);
        toTarget.y = 0f;
        Vector3 moveDir = toTarget.normalized;

        // Esquive suave del player
        if (_player != null)
        {
            Vector3 toPlayer = transform.position - _player.position;
            toPlayer.y = 0f;
            float distToPlayer = toPlayer.magnitude;
            if (distToPlayer < _avoidRadius && distToPlayer > 0.01f)
            {
                float avoidForce = (1f - distToPlayer / _avoidRadius) * _avoidStrength;
                moveDir += toPlayer.normalized * avoidForce;
                moveDir.Normalize();
            }
        }

        float dist = Vector3.Distance(transform.position, _target.position);
        float speedMultiplier = Mathf.Lerp(2f, 1f, dist / _magnetRadius);

        Vector3 newPos = transform.position + moveDir * _magnetSpeed * speedMultiplier * Time.deltaTime;
        newPos.y = _baseY + Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude * 0.5f;
        transform.position = newPos;

        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }

    private void DoArrive()
    {
        Vector3 above = new Vector3(_target.position.x, _target.position.y + _arcHeight, _target.position.z);

        if (transform.position.y < above.y - 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, above, _magnetSpeed * 1.5f * Time.deltaTime);
        }
        else
        {
            Vector3 stopPos = new Vector3(_target.position.x, _target.position.y + 0.3f, _target.position.z);
            transform.position = Vector3.MoveTowards(transform.position, stopPos, _descendSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, stopPos) <= _orbitRadius)
            {
                OnCoinCollected?.Invoke(); // agregá esto
                OnCollected?.Invoke(this);
                gameObject.SetActive(false);
                CoinManager.Instance.AddCoin();
            }
        }

        transform.Rotate(Vector3.up, _rotationSpeed * 2f * Time.deltaTime);
    }
}