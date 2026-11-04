using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Feedback de pantalla de los golpes de un boss: temblor, congelado, flash y anillo de espuma.
// Se crea solo la primera vez que se usa y vive en su propio objeto, así que sigue funcionando
// aunque el boss se apague o corte sus corrutinas en medio del golpe.
public class BossHitFeedback : MonoBehaviour
{
    private const float HitStopScale = 0.02f;
    private const float HitCooldown = 0.4f;

    private static BossHitFeedback _instance;

    private followPlayer _camera;
    private Image _flashImage;
    private Coroutine _flashRoutine;
    private Coroutine _hitStopRoutine;
    private bool _hitStopActive;
    private float _nextHitTime;

    private static BossHitFeedback Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GameObject("BossHitFeedback").AddComponent<BossHitFeedback>();
            return _instance;
        }
    }

    public static void PlayHit(Vector3 point, float ringSize, float shakeStrength, float shakeDuration,
        float hitStopDuration, Color flashColor, float flashDuration)
    {
        BossHitFeedback feedback = Instance;
        if (Time.unscaledTime < feedback._nextHitTime) return;
        feedback._nextHitTime = Time.unscaledTime + HitCooldown;

        WaterImpacts.Spawn(point, ringSize);
        Shake(shakeStrength, shakeDuration);
        feedback.PlayFlash(flashColor, flashDuration);
        feedback.PlayHitStop(hitStopDuration);
    }

    public static void Shake(float strength, float duration)
    {
        BossHitFeedback feedback = Instance;
        if (feedback._camera == null)
            feedback._camera = FindFirstObjectByType<followPlayer>();
        if (feedback._camera != null)
            feedback._camera.ApplyShake(strength, duration);
    }

    private void PlayHitStop(float duration)
    {
        // Si el juego ya está en pausa o en cámara lenta (ej. la muerte), no se toca el tiempo
        if (duration <= 0f || !Mathf.Approximately(Time.timeScale, 1f)) return;

        if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
        _hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = HitStopScale;
        _hitStopActive = true;
        yield return new WaitForSecondsRealtime(duration);
        ReleaseHitStop();
        _hitStopRoutine = null;
    }

    // Solo devuelve el tiempo a 1 si nadie más lo cambió mientras tanto
    private void ReleaseHitStop()
    {
        if (_hitStopActive && Mathf.Approximately(Time.timeScale, HitStopScale))
            Time.timeScale = 1f;
        _hitStopActive = false;
    }

    private void PlayFlash(Color color, float duration)
    {
        if (duration <= 0f || color.a <= 0f) return;
        if (_flashImage == null) BuildFlash();

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        _flashImage.gameObject.SetActive(true);
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            _flashImage.color = color;
            yield return null;
        }

        _flashImage.gameObject.SetActive(false);
        _flashRoutine = null;
    }

    private void BuildFlash()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var imageObject = new GameObject("Flash", typeof(RectTransform));
        imageObject.transform.SetParent(transform, false);

        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _flashImage = imageObject.AddComponent<Image>();
        _flashImage.raycastTarget = false;
        imageObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ReleaseHitStop();
        if (_instance == this) _instance = null;
    }
}
