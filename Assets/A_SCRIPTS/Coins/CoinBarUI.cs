// CoinBarUI.cs
using UnityEngine;
using UnityEngine.UI;

public class CoinBarUI : MonoBehaviour
{
    [SerializeField] private Image _barImage; // tu imagen filled vertical

    private void Start() => CoinManager.Instance.OnCoinChanged += UpdateBar;
    private void OnDisable() => CoinManager.Instance.OnCoinChanged -= UpdateBar;

    private void UpdateBar(float t) => _barImage.fillAmount = t;
}