using UnityEngine;

// Barquito del Overworld. Mismos controles que en el juego: W avanza, A/D giran.
public class OverworldBoat : MonoBehaviour
{
    [SerializeField] private OverworldMap _map;
    [Tooltip("Hijo con el modelo: se usa para el bamboleo y la inclinación, así no afecta la posición")]
    [SerializeField] private Transform _visual;

    [Header("Movimiento")]
    [SerializeField] private float _maxSpeed = 7f;
    [SerializeField] private float _acceleration = 3f;
    [SerializeField] private float _deceleration = 2.5f;
    [SerializeField] private float _turnSpeed = 140f;
    [SerializeField] private float _heightOffset = 0.15f;

    [Header("Game feel")]
    [SerializeField] private float _bobAmplitude = 0.06f;
    [SerializeField] private float _bobFrequency = 2.2f;
    [SerializeField] private float _bankAngle = 14f;
    [SerializeField] private float _pitchAngle = 6f;

    [Header("Estela")]
    [Tooltip("Prefab de partículas (ej. TrailPartices, el mismo del barco del juego). Se instancia en la popa.")]
    [SerializeField] private ParticleSystem _wakePrefab;
    [Tooltip("O una estela que ya esté puesta como hijo del barco (si está, se usa esta)")]
    [SerializeField] private ParticleSystem _wake;
    [SerializeField] private Vector3 _wakeLocalPosition = new Vector3(0f, 0.05f, -0.75f);
    [SerializeField] private Vector3 _wakeLocalRotation = Vector3.zero;
    [Tooltip("El barco del juego es más grande: achicamos las partículas para el barquito")]
    [SerializeField] private float _wakeScale = 0.3f;
    [Tooltip("Por debajo de esta fracción de la velocidad máxima no sale estela")]
    [SerializeField, Range(0f, 1f)] private float _wakeMinSpeed = 0.12f;

    public bool InputEnabled { get; set; } = true;
    public float SpeedRatio => _maxSpeed > 0f ? Mathf.Abs(_speed) / _maxSpeed : 0f;

    private float _speed;
    private float _turn;
    private float _bank;
    private float _y;
    private ParticleSystem[] _wakeSystems;
    private float[] _wakeBaseRates;

    private void Start()
    {
        if (_map != null) _y = _map.GetSurfaceY(transform.position) + _heightOffset;
        SetupWake();
    }

    private void SetupWake()
    {
        if (_wake == null && _wakePrefab != null)
        {
            _wake = Instantiate(_wakePrefab, transform);
            _wake.transform.localPosition = _wakeLocalPosition;
            _wake.transform.localRotation = Quaternion.Euler(_wakeLocalRotation);
            _wake.transform.localScale = Vector3.one * _wakeScale;
        }
        if (_wake == null) return;

        _wakeSystems = _wake.GetComponentsInChildren<ParticleSystem>(true);
        _wakeBaseRates = new float[_wakeSystems.Length];
        for (int i = 0; i < _wakeSystems.Length; i++)
        {
            var main = _wakeSystems[i].main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy; // que respete el _wakeScale
            _wakeBaseRates[i] = _wakeSystems[i].emission.rateOverTimeMultiplier;
        }
        _wake.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float v = InputEnabled ? Mathf.Clamp01(Input.GetAxisRaw("Vertical")) : 0f;
        float h = InputEnabled ? Input.GetAxisRaw("Horizontal") : 0f;

        _turn = Mathf.Lerp(_turn, h * _turnSpeed, dt * 6f);
        transform.Rotate(0f, _turn * dt, 0f, Space.World);

        float target = v * _maxSpeed;
        float rate = target > _speed ? _acceleration : _deceleration;
        _speed = Mathf.MoveTowards(_speed, target, rate * _maxSpeed * dt);

        Vector3 pos = transform.position + transform.forward * _speed * dt;
        if (_map != null)
        {
            pos = _map.ClampToMap(pos);
            _y = Mathf.Lerp(_y, _map.GetSurfaceY(pos) + _heightOffset, dt * 10f);
            pos.y = _y;
        }
        transform.position = pos;

        if (_visual != null)
        {
            float t = Time.time * _bobFrequency;
            _bank = Mathf.Lerp(_bank, -(_turn / _turnSpeed) * _bankAngle * Mathf.Clamp01(SpeedRatio + 0.3f), dt * 6f);
            _visual.localPosition = new Vector3(0f, Mathf.Sin(t) * _bobAmplitude, 0f);
            _visual.localRotation = Quaternion.Euler(-SpeedRatio * _pitchAngle + Mathf.Sin(t * 0.8f) * 2f, 0f, _bank + Mathf.Cos(t * 0.6f) * 2f);
        }

        UpdateWake();
    }

    private void UpdateWake()
    {
        if (_wake == null || _wakeSystems == null) return;

        bool moving = SpeedRatio > _wakeMinSpeed;
        if (moving && !_wake.isEmitting) _wake.Play(true);
        else if (!moving && _wake.isEmitting) _wake.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Más rápido = más partículas
        for (int i = 0; i < _wakeSystems.Length; i++)
        {
            var emission = _wakeSystems[i].emission;
            emission.rateOverTimeMultiplier = _wakeBaseRates[i] * Mathf.Lerp(0.3f, 1f, SpeedRatio);
        }
    }
}
