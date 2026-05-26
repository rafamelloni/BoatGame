using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PergaminoSlideByVisualBar : MonoBehaviour
{
    [Header("Barra visual")]
    [SerializeField] private Image fillBar;

    [Header("Objeto padre de la UI")]
    [SerializeField] private GameObject pergaminoPadre;

    [Header("Movimiento")]
    [SerializeField] private float targetX = 0f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float overshootAmount = 20f;


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
            OpenPergamino();
        }
    }

    private void OpenPergamino()
    {

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Vector2 startPos = rectTransform.anchoredPosition;

        // Se pasa un poquito
        Vector2 overshootPos =
            new Vector2(targetX + overshootAmount, startPos.y);

        // Posición final real
        Vector2 finalPos =
            new Vector2(targetX, startPos.y);

        // Movimiento principal
        yield return MoveTo(startPos, overshootPos, duration);

        // Rebote suave de vuelta
        yield return MoveTo(overshootPos, finalPos, 0.12f);

        routine = null;
    }

    private IEnumerator MoveTo(Vector2 from, Vector2 to, float moveDuration)
    {
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;

            float t = time / moveDuration;

            // Ease Out Cubic
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            rectTransform.anchoredPosition =
                Vector2.Lerp(from, to, smoothT);

            yield return null;
        }

        rectTransform.anchoredPosition = to;
    }
}