using System.Collections;
using UnityEngine;

public class ChestLid : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _lid;
    [SerializeField] private CoinManager _coinManager;

    [Header("Rotacion")]
    [SerializeField] private float _openAngle = -75f;
    [SerializeField] private float _closedAngle = 0f;
    [SerializeField] private float _openSpeed = 8f;
    [SerializeField] private float _closeSpeed = 3f;

    [Header("Idle")]
    [SerializeField] private float _idleAmplitude = 3f;   // grados de oscilacion
    [SerializeField] private float _idleSpeed = 1.5f;     // velocidad de oscilacion

    private bool _isOpen = false;
    private int _coinsInFlight = 0;

    private void OnEnable()
    {
        // Suscribirse a cuando una moneda entra en Arrive
        Coin.OnCoinArriving += OnCoinArriving;
        Coin.OnCoinCollected += OnCoinCollected;
    }

    private void OnDisable()
    {
        Coin.OnCoinArriving -= OnCoinArriving;
        Coin.OnCoinCollected -= OnCoinCollected;
    }

    private void OnCoinArriving()
    {
        _coinsInFlight++;
        _isOpen = true;
    }

    private void OnCoinCollected()
    {
        _coinsInFlight--;
        if (_coinsInFlight <= 0)
        {
            _coinsInFlight = 0;
            _isOpen = false;
        }
    }

    private void Update()
    {
        if (_lid == null) return;

        float currentX = _lid.localEulerAngles.x;
        // Convertir a -180/180
        if (currentX > 180f) currentX -= 360f;

        float targetX;
        if (_isOpen)
        {
            targetX = _openAngle;
            float newX = Mathf.Lerp(currentX, targetX, _openSpeed * Time.deltaTime);
            _lid.localEulerAngles = new Vector3(newX, 0f, 0f);
        }
        else
        {
            // Idle: oscila suavemente alrededor del angulo cerrado
            float idleOffset = Mathf.Sin(Time.time * _idleSpeed) * _idleAmplitude;
            targetX = _closedAngle + idleOffset;
            float newX = Mathf.Lerp(currentX, targetX, _closeSpeed * Time.deltaTime);
            _lid.localEulerAngles = new Vector3(newX, 0f, 0f);
        }
    }
}