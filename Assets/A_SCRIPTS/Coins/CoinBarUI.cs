// CoinBarUI.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinBarUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image _barImage;          // Relleno principal (sprite XPBar_Fill_White, el color lo pone el script)
    [SerializeField] private TextMeshProUGUI _barLevel;
    [SerializeField] private Image _ghostImage;        // Relleno claro que se adelanta (XPBar_Fill_White), va DEBAJO del Fill
    [SerializeField] private RectTransform _barRoot;   // Lo que rebota con cada moneda (el padre de la barra)
    [SerializeField] private RectTransform _badge;     // Círculo del nivel

    [Header("Colores")]
    [SerializeField] private Color _fillColor = new Color32(242, 184, 36, 255);
    [SerializeField] private Color _nearFullColor = new Color32(255, 226, 120, 255);
    [SerializeField] private Color _ghostColor = new Color32(255, 244, 200, 255);
    [SerializeField] private Color _flashColor = Color.white;

    [Header("Llenado")]
    [SerializeField] private float _fillDelay = 0.12f;          // cuánto espera el dorado antes de alcanzar a la franja clara
    [SerializeField] private float _fillSmoothTime = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _nearFullThreshold = 0.85f;
    [SerializeField] private float _nearFullPulseSpeed = 10f;

    [Header("Rebote por moneda")]
    [SerializeField] private Vector2 _barPunch = new Vector2(0.03f, 0.12f);
    [SerializeField] private float _badgePunch = 0.12f;
    [SerializeField] private float _springStiffness = 300f;
    [SerializeField] private float _springDamping = 14f;

    [Header("Level up")]
    [SerializeField] private float _fillToFullTime = 0.15f;
    [SerializeField] private float _flashDuration = 0.3f;
    [SerializeField] private float _badgeLevelUpPunch = 0.6f;
    [SerializeField] private float _minHoldTime = 0.3f;
    [SerializeField] private float _drainTime = 0.25f;

    private float _shown, _ghost, _target, _fillVelocity, _delayTimer;
    private float _barSpring, _barSpringVel, _badgeSpring, _badgeSpringVel;
    private float _flashTimer;
    private int _pendingLevelUps, _displayedLevel, _realLevel;
    private bool _inLevelUp, _subscribed;
    private Coroutine _levelUpRoutine;
    private Vector3 _barBaseScale = Vector3.one, _badgeBaseScale = Vector3.one;

    private void Awake()
    {
        if (_barRoot != null) _barBaseScale = _barRoot.localScale;
        if (_badge != null) _badgeBaseScale = _badge.localScale;
    }

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
        if (_subscribed) return;
        CoinManager.Instance.OnCoinChanged += HandleCoinChanged;
        CoinManager.Instance.OnLevelChanged += HandleLevelChanged;
        CoinManager.Instance.OnBarFilled += HandleBarFilled;
        _subscribed = true;
        SnapTo(CoinManager.Instance.Progress);
    }

    private void OnDisable()
    {
        if (_levelUpRoutine != null)
        {
            StopCoroutine(_levelUpRoutine);
            _levelUpRoutine = null;
        }
        _inLevelUp = false;

        if (!_subscribed || CoinManager.Instance == null) return;
        CoinManager.Instance.OnCoinChanged -= HandleCoinChanged;
        CoinManager.Instance.OnLevelChanged -= HandleLevelChanged;
        CoinManager.Instance.OnBarFilled -= HandleBarFilled;
        _subscribed = false;
    }

    // ---------- Eventos ----------

    private void HandleCoinChanged(float t)
    {
        // Durante el level up se ignora: al terminar se lee el progreso real
        if (_inLevelUp) return;

        if (t >= _target)
        {
            // Ganó XP: la franja clara salta ya, el dorado la alcanza después
            _ghost = t;
            _target = t;
            _delayTimer = _fillDelay;
            Punch(1f, _badgePunch);
        }
        else
        {
            _target = t;
            _ghost = t;
        }
    }

    private void HandleLevelChanged(int level)
    {
        _realLevel = level;
        if (level == 0) ResetBar(); // ResetCoins de la run
    }

    private void HandleBarFilled()
    {
        _pendingLevelUps++;
        if (_levelUpRoutine == null)
            _levelUpRoutine = StartCoroutine(LevelUpSequence());
    }

    // ---------- Level up ----------

    private IEnumerator LevelUpSequence()
    {
        _inLevelUp = true;

        while (_pendingLevelUps > 0)
        {
            _pendingLevelUps--;

            // 1. Completar la barra de golpe
            _ghost = 1f;
            yield return AnimateFill(_shown, 1f, _fillToFullTime);

            // 2. Flash + el círculo explota con el nivel nuevo
            _displayedLevel = Mathf.Min(_displayedLevel + 1, _realLevel);
            if (_barLevel != null) _barLevel.text = _displayedLevel.ToString();
            _flashTimer = _flashDuration;
            Punch(1f, _badgeLevelUpPunch);

            // 3. Queda llena mientras dura la cámara lenta y el pergamino
            float hold = 0f;
            while (hold < _minHoldTime)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitUntil(() => Time.timeScale >= 0.99f);

            // 4. Se vacía
            _ghost = 0f;
            yield return AnimateFill(_shown, 0f, _drainTime);
        }

        _inLevelUp = false;
        _levelUpRoutine = null;

        _displayedLevel = _realLevel;
        if (_barLevel != null) _barLevel.text = _displayedLevel.ToString();

        // Se rellena con lo que sobró (monedas juntadas durante el efecto)
        float p = CoinManager.Instance != null ? CoinManager.Instance.Progress : 0f;
        _target = p;
        _ghost = p;
        _fillVelocity = 0f;
        _delayTimer = _fillDelay;
        if (p > 0f) Punch(0.6f, _badgePunch);
    }

    private IEnumerator AnimateFill(float from, float to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(duration, 0.0001f);
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            _shown = Mathf.Lerp(from, to, e);
            yield return null;
        }
        _shown = to;
    }

    // ---------- Visual ----------

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        if (!_inLevelUp)
        {
            if (_delayTimer > 0f) _delayTimer -= dt;
            else _shown = Mathf.SmoothDamp(_shown, _target, ref _fillVelocity, _fillSmoothTime, Mathf.Infinity, dt);
        }

        if (_barImage != null)
        {
            _barImage.fillAmount = _shown;

            Color c = _fillColor;
            if (_shown >= _nearFullThreshold)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * _nearFullPulseSpeed) + 1f) * 0.5f;
                c = Color.Lerp(_fillColor, _nearFullColor, pulse);
            }
            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                c = Color.Lerp(c, _flashColor, Mathf.Clamp01(_flashTimer / _flashDuration));
            }
            _barImage.color = c;
        }

        if (_ghostImage != null)
        {
            _ghostImage.fillAmount = Mathf.Max(_ghost, _shown);
            _ghostImage.color = _ghostColor;
        }

        UpdateSpring(ref _barSpring, ref _barSpringVel, dt);
        UpdateSpring(ref _badgeSpring, ref _badgeSpringVel, dt);

        if (_barRoot != null)
            _barRoot.localScale = Vector3.Scale(_barBaseScale,
                new Vector3(1f + _barPunch.x * _barSpring, 1f + _barPunch.y * _barSpring, 1f));
        if (_badge != null)
            _badge.localScale = _badgeBaseScale * (1f + _badgeSpring);
    }

    private void Punch(float bar, float badge)
    {
        _barSpring = Mathf.Max(_barSpring, bar);
        _barSpringVel = 0f;
        _badgeSpring = Mathf.Max(_badgeSpring, badge);
        _badgeSpringVel = 0f;
    }

    private void UpdateSpring(ref float x, ref float v, float dt)
    {
        dt = Mathf.Min(dt, 1f / 30f);
        float a = -_springStiffness * x - _springDamping * v;
        v += a * dt;
        x += v * dt;
    }

    private void SnapTo(float p)
    {
        _shown = _ghost = _target = p;
        _fillVelocity = 0f;
        _delayTimer = 0f;
        if (_barImage != null) _barImage.fillAmount = p;
    }

    public void ResetBar()
    {
        if (_levelUpRoutine != null)
        {
            StopCoroutine(_levelUpRoutine);
            _levelUpRoutine = null;
        }
        _inLevelUp = false;
        _pendingLevelUps = 0;
        _displayedLevel = _realLevel = 0;
        if (_barLevel != null) _barLevel.text = "0";
        SnapTo(0f);
        _barSpring = _barSpringVel = _badgeSpring = _badgeSpringVel = 0f;
        _flashTimer = 0f;
    }
}
