using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CooldownRadialUI : MonoBehaviour
{
    [SerializeField] private Image radialImage;
    [SerializeField] private RectTransform _rotatingObject;
    [SerializeField] private float _maxRotationAngle = 45f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        radialImage.fillAmount = 0f;
        radialImage.enabled = false;
    }

    public void PlayCooldown(float duration)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(CooldownRoutine(duration));
    }

    private IEnumerator CooldownRoutine(float duration)
    {
        radialImage.enabled = true;
        radialImage.fillAmount = 1f;

        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            radialImage.fillAmount = 1f - (elapsed / duration);

            // Primera mitad: rota hacia la izquierda
            // Segunda mitad: vuelve a la posicion original
            float t;
            float zRotation;

            if (elapsed < halfDuration)
            {
                t = elapsed / halfDuration;
                zRotation = Mathf.Lerp(0f, _maxRotationAngle, t);
            }
            else
            {
                t = (elapsed - halfDuration) / halfDuration;
                zRotation = Mathf.Lerp(_maxRotationAngle, 0f, t);
            }

            _rotatingObject.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            yield return null;
        }

        radialImage.fillAmount = 0f;
        radialImage.enabled = false;
        _rotatingObject.localRotation = Quaternion.identity;
        currentRoutine = null;
    }
}