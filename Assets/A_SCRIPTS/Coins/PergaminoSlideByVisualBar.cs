using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PergaminoSlideByVisualBar : MonoBehaviour
{
    [Header("Barra visual")]
    [SerializeField] private Image fillBar;
    [Header("Corsshjair Desactivarl")]
    [SerializeField] private GameObject _crosshair;

    [Header("Efecto level up")]
    [SerializeField] private LevelUpBurst _levelUpBurst;

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

    // Subidas de nivel que todavía no se mostraron. Antes el pergamino miraba si la
    // barra llegaba a 0.99 en Update; si entraban 2 monedas en el mismo frame la barra
    // pasaba de 1 a 0.05 antes de que Update la viera y el level up se perdía.
    // Ahora escuchamos el evento de CoinManager y las encolamos.
    private int _pendingLevelUps = 0;
    private bool _subscribed = false;

    private void Awake()
    {
        if (pergaminoPadre == null)
            pergaminoPadre = gameObject;
        rectTransform = pergaminoPadre.GetComponent<RectTransform>();
        _startY = rectTransform.anchoredPosition.y;
    }

    private void OnEnable()
    {
        if (CoinManager.Instance != null) Subscribe();
        else StartCoroutine(WaitForCoinManager());
    }

    private void OnDisable()
    {
        if (!_subscribed || CoinManager.Instance == null) return;
        CoinManager.Instance.OnBarFilled -= HandleBarFilled;
        CoinManager.Instance.OnLevelChanged -= HandleLevelChanged;
        _subscribed = false;
    }

    private IEnumerator WaitForCoinManager()
    {
        yield return new WaitUntil(() => CoinManager.Instance != null);
        Subscribe();
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        CoinManager.Instance.OnBarFilled += HandleBarFilled;
        CoinManager.Instance.OnLevelChanged += HandleLevelChanged;
        _subscribed = true;
    }

    private void HandleBarFilled()
    {
        _pendingLevelUps++;
        TryStartLevelUp();
    }

    // ResetCoins() manda nivel 0 (reset de la run): descartamos lo que quedó en cola
    private void HandleLevelChanged(int level)
    {
        if (level == 0) _pendingLevelUps = 0;
    }

    private void TryStartLevelUp()
    {
        if (_isOpen || _pendingLevelUps <= 0) return;

        _pendingLevelUps--;
        _isOpen = true;
        if (fillBar != null) fillBar.fillAmount = 1f;
        _crosshair.SetActive(false);

        if (_levelUpBurst != null)
            _levelUpBurst.Play(OpenPergamino);
        else
            OpenPergamino();
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
        // Antes se ponía en 0: si habías juntado monedas durante el efecto se perdían visualmente
        fillBar.fillAmount = CoinManager.Instance != null ? CoinManager.Instance.Progress : 0f;
        Time.timeScale = 1f;
        routine = null;
        onComplete?.Invoke();
        _crosshair.SetActive(true);

        // Si mientras elegías se llenó la barra otra vez, sigue el próximo level up
        TryStartLevelUp();
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
