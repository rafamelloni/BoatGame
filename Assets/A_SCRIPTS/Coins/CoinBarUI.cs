// CoinBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinBarUI : MonoBehaviour
{
    [SerializeField] private Image _barImage;
    [SerializeField] private TextMeshProUGUI _barLevel;

    private void Start()
    {
        CoinManager.Instance.OnCoinChanged += UpdateBar;
        CoinManager.Instance.OnLevelChanged += UpdateLevel;
    }

    private void OnDisable()
    {
        CoinManager.Instance.OnCoinChanged -= UpdateBar;
        CoinManager.Instance.OnLevelChanged -= UpdateLevel;
    }

    private void UpdateBar(float t) => _barImage.fillAmount = t;
    private void UpdateLevel(int level) => _barLevel.text = level.ToString();

    public void ResetBar()
    {
        _barLevel.text = "0";
        _barImage.fillAmount = 0;
    }
}