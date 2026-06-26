// CoinManager.cs
using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [SerializeField] private int _coinsToFill = 20;

    private int _coins;
    private int _level;

    public event Action<float> OnCoinChanged;
    public event Action<int> OnLevelChanged;
    public event Action OnBarFilled;

    private void Awake() => Instance = this;

    public void AddCoin()
    {
        _coins++;
        float t = Mathf.Clamp01((float)_coins / _coinsToFill);
        OnCoinChanged?.Invoke(t);

        if (_coins >= _coinsToFill)
        {
            _level++;
            _coins = 0;
            OnLevelChanged?.Invoke(_level);
            OnBarFilled?.Invoke();
        }
    }

    public void SetCoinsRequired(int amount)
    {
        _coinsToFill = amount;
        OnCoinChanged?.Invoke(Mathf.Clamp01((float)_coins / _coinsToFill));
    }

    public void ResetCoins()
    {
        _coins = 0;
        _level = 0;
        OnCoinChanged?.Invoke(0f);
        OnLevelChanged?.Invoke(0);
    }
}