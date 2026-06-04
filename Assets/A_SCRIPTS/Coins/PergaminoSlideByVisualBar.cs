using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PergaminoSlideByVisualBar : MonoBehaviour
{
    [Header("Barra visual")]
    [SerializeField] private Image fillBar;
    [Header("Corsshjair Desactivarl")]
    [SerializeField] private GameObject _crosshair;

    [Header("Objeto padre de la UI")]
    [SerializeField] private GameObject pergaminoPadre;
    [Header("Movimiento")]
    [SerializeField] private float targetY = 0f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float overshootAmount = 20f;
    public float closeDuration = 0.25f;

    private RectTransform rectTransform;
    private Coroutine routine;
    private bool _isOpen = false;
    private float _startY;

    private void Awake()
    {
        if (pergaminoPadre == null)
            pergaminoPadre = gameObject;
        rectTransform = pergaminoPadre.GetComponent<RectTransform>();
        _startY = rectTransform.anchoredPosition.y;
    }

    private void Update()
    {
        if (_isOpen) return;
        if (fillBar == null) return;
        if (fillBar.fillAmount >= 0.99f)
        {
            _isOpen = true;
            _crosshair.SetActive(false);
            OpenPergamino();

        }
    }

    private void OpenPergamino()
    {
        Time.timeScale = 0f;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(OpenRoutine());
    }

    public void ClosePergamino(System.Action onComplete = null)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CloseRoutine(onComplete));
    }

    private IEnumerator OpenRoutine()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 overshootPos = new Vector2(startPos.x, targetY + overshootAmount);
        Vector2 finalPos = new Vector2(startPos.x, targetY);

        yield return MoveTo(startPos, overshootPos, duration);
        yield return MoveTo(overshootPos, finalPos, 0.12f);
        routine = null;
    }

    private IEnumerator CloseRoutine(System.Action onComplete)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 finalPos = new Vector2(startPos.x, _startY);

        yield return MoveTo(startPos, finalPos, closeDuration);
        _isOpen = false;
        fillBar.fillAmount = 0f;
        Time.timeScale = 1f;
        routine = null;
        onComplete?.Invoke();
        _crosshair.SetActive(true   );
    }

    private IEnumerator MoveTo(Vector2 from, Vector2 to, float moveDuration)
    {
        float time = 0f;
        while (time < moveDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / moveDuration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.anchoredPosition = Vector2.Lerp(from, to, smoothT);
            yield return null;
        }
        rectTransform.anchoredPosition = to;
    }
}