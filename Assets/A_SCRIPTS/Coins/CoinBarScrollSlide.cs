using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinBarScrollSlide : MonoBehaviour
{
    [Header("Barra visual")]
    [SerializeField] private Image fillBar;

    [Header("Objeto padre de la UI")]
    [SerializeField] private GameObject pergaminoPadre;

    [Header("Posición final en X")]
    [SerializeField] private float targetX = 0f;

    [Header("Animación")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rectTransform;
    private Coroutine routine;
    private bool alreadyOpened;

    private void Awake()
    {
        if (pergaminoPadre == null)
            pergaminoPadre = gameObject;

        rectTransform = pergaminoPadre.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (alreadyOpened) return;
        if (fillBar == null) return;

        if (fillBar.fillAmount >= 0.99f)
        {
            alreadyOpened = true;
            SlideToCenter();
        }
    }

    private void SlideToCenter()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = new Vector2(targetX, startPos.y);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            float smoothT = curve.Evaluate(t);

            rectTransform.anchoredPosition =
                Vector2.Lerp(startPos, endPos, smoothT);

            yield return null;
        }

        rectTransform.anchoredPosition = endPos;
        routine = null;
    }
}