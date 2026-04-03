// EnemyUpgradeDisplay.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyUpgradeDisplay : MonoBehaviour
{
    [SerializeField] private AbilityUpgradeSystem _upgradeSystem;
    [SerializeField] private Image _iconImage;
    [SerializeField] private float _floatDistance = 80f;
    [SerializeField] private float _duration = 1f;

    private EnemyHealth _health;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _iconImage.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _upgradeSystem.OnUpgradeApplied += HandleUpgradeApplied;
    }

    private void OnDisable()
    {
        _upgradeSystem.OnUpgradeApplied -= HandleUpgradeApplied;
    }

    private void HandleUpgradeApplied(Sprite icon)
    {
        // Solo reacciona si es este enemigo el que murió
        if (!_health.IsDying) return;

        _iconImage.sprite = icon;
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        _iconImage.gameObject.SetActive(true);

        RectTransform rt = _iconImage.rectTransform;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * _floatDistance;

        Color startColor = _iconImage.color;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            _iconImage.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            yield return null;
        }

        _iconImage.gameObject.SetActive(false);
    }
}