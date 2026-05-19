// CoinManager.cs
using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [SerializeField] private int _coinsToFill = 20; // cuántas monedas llenan la barra

    private int _coins;

    public event Action<float> OnCoinChanged; // 0-1 normalizado
    public event Action OnBarFilled;

    private void Awake()
    {
        Instance = this;
    }

    public void AddCoin()
    {
        _coins++;
        float t = Mathf.Clamp01((float)_coins / _coinsToFill);
        OnCoinChanged?.Invoke(t);

        if (_coins >= _coinsToFill)
        {
            _coins = 0;
            OnBarFilled?.Invoke();
        }
    }
}