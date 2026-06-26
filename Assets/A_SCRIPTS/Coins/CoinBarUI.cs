// CoinBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoinBarUI : MonoBehaviour
{
    [SerializeField] private Image _barImage;
    [SerializeField] private TextMeshProUGUI _barLevel;

    private void OnEnable()
    {
        if (CoinManager.Instance == null)
        {
            StartCoroutine(WaitForCoinManager());
            return;
        }
        Subscribe();
    }

    private IEnumerator WaitForCoinManager()
    {
        yield return new WaitUntil(() => CoinManager.Instance != null);
        Subscribe();
    }

    private void Subscribe()
    {
        CoinManager.Instance.OnCoinChanged += UpdateBar;
        CoinManager.Instance.OnLevelChanged += UpdateLevel;
    }

    private void OnDisable()
    {

        if (CoinManager.Instance == null) return;
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