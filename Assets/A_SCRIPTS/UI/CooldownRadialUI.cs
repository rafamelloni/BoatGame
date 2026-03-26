using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class CooldownRadialUI : MonoBehaviour
{
    [SerializeField] private Image radialImage;

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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            radialImage.fillAmount = 1f - (elapsed / duration);
            yield return null;
        }

        radialImage.fillAmount = 0f;
        radialImage.enabled = false;
        currentRoutine = null;
    }
}
