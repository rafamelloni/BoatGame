using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float _bobAmplitude = 0.2f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _rotationSpeed = 90f;

    [Header("Pickup")]
    [SerializeField] private float _magnetRadius = 6f;
    [SerializeField] private float _orbitRadius = 1.2f;
    [SerializeField] private float _magnetSpeed = 8f;

    private float _baseY;
    private Transform _player;

    private enum State { Float, Pull, Orbit }
    private State _state = State.Float;



    public event System.Action<Coin> OnCollected;
    [SerializeField] private TrailRenderer _trail;

    private void OnDisable()
    {
        if (_trail != null)
            _trail.Clear();
    }
    public void Init(Transform player, Vector3 spawnPos)
    {
        _player = player;
        transform.position = spawnPos;
        _baseY = spawnPos.y;
        _state = State.Float;
    }

    private void Update()
    {
        if (_player == null) return;
        switch (_state)
        {
            case State.Float:
                DoFloat();
                if (Vector3.Distance(transform.position, _player.position) <= _magnetRadius)
                    _state = State.Pull;
                break;
            case State.Pull:
                DoPull();
                if (Vector3.Distance(transform.position, _player.position) <= _orbitRadius)
                {
                    OnCollected?.Invoke(this);
                    gameObject.SetActive(false);
                    CoinManager.Instance.AddCoin();
                }
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
        transform.position = Vector3.MoveTowards(
            transform.position,
            _player.position,
            _magnetSpeed * Time.deltaTime
        );

        float dist = Vector3.Distance(transform.position, _player.position);
        float speedMultiplier = Mathf.Lerp(6f, 1f, dist / _magnetRadius); // más rápido cuanto más cerca
        transform.Rotate(Vector3.up, _rotationSpeed * speedMultiplier * Time.deltaTime);
    }

    //private void DoOrbit()
    //{
    //    _orbitTimer += Time.deltaTime;
    //    _orbitAngle += _orbitSpeed * Time.deltaTime;

    //    float t = _orbitTimer / _orbitDuration;
    //    float currentRadius = Mathf.Lerp(_orbitRadius, 0f, t);

    //    float rad = _orbitAngle * Mathf.Deg2Rad;
    //    Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * currentRadius;

    //    transform.position = new Vector3(
    //        _player.position.x + offset.x,
    //        transform.position.y,
    //        _player.position.z + offset.z
    //    );

    //    transform.Rotate(Vector3.up, _rotationSpeed * (1f + t * 8f) * Time.deltaTime); // acelera al espiralizar

    //    if (_orbitTimer >= _orbitDuration)
    //    {
    //        OnCollected?.Invoke(this);
    //        gameObject.SetActive(false);
    //        CoinManager.Instance.AddCoin();

    //    }
    //}
}