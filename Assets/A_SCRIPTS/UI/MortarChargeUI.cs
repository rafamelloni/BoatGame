using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MortarChargeUI : MonoBehaviour
{
    [SerializeField] private Image _radialImage;
    [SerializeField] private TextMeshProUGUI _chargesText;
    private System.Action _onRestoreCharge;

    private int _charges = 3;
    private int _rechargesQueued = 0;
    private bool _isRecharging = false;
    private float _cooldown;

    public void TurnOff()
    {
        StopAllCoroutines();
        _charges = 3;
        _rechargesQueued = 0;
        _isRecharging = false;
        _radialImage.fillAmount = 0f;
        _radialImage.enabled = false;
        _chargesText.text = "3";
    }
    public void Init(float cooldown, System.Action onRestoreCharge)
    {
        _cooldown = cooldown;
        _onRestoreCharge = onRestoreCharge;
        _radialImage.fillAmount = 0f;
        _radialImage.enabled = false;
        _chargesText.text = "3";
    }

    public void OnShot()
    {
        _charges--;
        _chargesText.text = _charges.ToString();
        _rechargesQueued++;

        if (!_isRecharging)
            StartCoroutine(RechargeLoop());
    }

    private IEnumerator RechargeLoop()
    {
        _isRecharging = true;
        _radialImage.enabled = true;

        while (_rechargesQueued > 0)
        {
            float elapsed = 0f;
            while (elapsed < _cooldown)
            {
                elapsed += Time.deltaTime;
                _radialImage.fillAmount = elapsed / _cooldown;
                yield return null;
            }
            _rechargesQueued--;
            _charges++;
            _chargesText.text = _charges.ToString();
            _onRestoreCharge?.Invoke();
        }

        _radialImage.fillAmount = 0f;
        _radialImage.enabled = false;
        _isRecharging = false;
    }
}