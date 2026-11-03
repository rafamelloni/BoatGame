using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Efecto de la barra de vida del jugador, igual al de los enemigos:
//  - La vida real baja de golpe
//  - Lo que se perdió queda marcado con menos alpha y después se drena
//  - En cada golpe: flash blanco + punch de escala + temblor
// RT_PlayerHealth lo agrega solo al padre de la barra.
public class PlayerHealthBarFx : MonoBehaviour
{
    [Header("Trail (lo que bajó)")]
    [SerializeField, Range(0f, 1f)] private float _trailAlpha = 0.4f;
    [SerializeField] private float _trailDelay = 0.35f;
    [Tooltip("Fracción de barra por segundo")]
    [SerializeField] private float _trailSpeed = 0.6f;

    [Header("Golpe")]
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashDuration = 0.07f;
    [SerializeField] private float _punchScale = 1.06f;
    [SerializeField] private float _punchDuration = 0.15f;
    [Tooltip("Temblor en unidades de UI")]
    [SerializeField] private float _shakeAmount = 4f;

    private struct BarPart
    {
        public RectTransform rect;
        public Vector3 scale;
        public Vector2 position;
    }

    private readonly List<BarPart> _parts = new List<BarPart>();
    private RT_PlayerHealth _health;
    private Image _fill;
    private Image _trail;
    private Color _fillColor;
    private float _shown = 1f;
    private float _trailValue = 1f;
    private float _trailWait;
    private float _flashTimer;
    private float _punchTimer;
    private bool _hasValue;

    public void Init(RT_PlayerHealth health, Image bar)
    {
        _health = health;
        Vector2 barPosition = bar.rectTransform.anchoredPosition;
        var filledImages = new List<Image>();

        // Las partes de la barra son los hermanos que están en el mismo lugar (fondo y rellenos)
        foreach (Transform child in transform)
        {
            var image = child.GetComponent<Image>();
            if (image == null) continue;

            var rect = image.rectTransform;
            if (Vector2.Distance(rect.anchoredPosition, barPosition) > 2f) continue;

            if (image.type == Image.Type.Filled) filledImages.Add(image);
            AddPart(rect);
        }

        // El de arriba es la vida real y el de abajo queda como trail
        if (filledImages.Count >= 2)
        {
            _fill = filledImages[filledImages.Count - 1];
            _trail = filledImages[filledImages.Count - 2];
        }
        else
        {
            _fill = bar;
            _trail = Instantiate(bar, bar.transform.parent);
            _trail.name = bar.name + " Trail";
            _trail.transform.SetSiblingIndex(bar.transform.GetSiblingIndex());
            AddPart(_trail.rectTransform);
        }

        _fillColor = _fill.color;
        Color trailColor = _fillColor;
        trailColor.a = _trailAlpha;
        _trail.color = trailColor;
    }

    private void AddPart(RectTransform rect)
    {
        _parts.Add(new BarPart { rect = rect, scale = rect.localScale, position = rect.anchoredPosition });
    }

    private void LateUpdate()
    {
        if (_health == null || _fill == null || _health.MaxHealth <= 0f) return;

        float target = Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth);

        if (!_hasValue)
        {
            _hasValue = true;
            _shown = _trailValue = target;
        }
        else if (target < _shown - 0.0001f)
        {
            _shown = target;
            _trailWait = _trailDelay;
            _flashTimer = _flashDuration;
            _punchTimer = _punchDuration;
        }
        else if (target > _shown + 0.0001f)
        {
            _shown = target;
            if (_trailValue < _shown) _trailValue = _shown;
        }

        float dt = Time.deltaTime;
        if (_trailValue > _shown)
        {
            if (_trailWait > 0f) _trailWait -= dt;
            else _trailValue = Mathf.MoveTowards(_trailValue, _shown, _trailSpeed * dt);
        }

        _fill.fillAmount = _shown;
        _trail.fillAmount = Mathf.Max(_trailValue, _shown);

        if (_flashTimer > 0f)
        {
            _flashTimer -= dt;
            _fill.color = _flashColor;
        }
        else _fill.color = _fillColor;

        ApplyPunch(dt);
    }

    private void ApplyPunch(float dt)
    {
        float scale = 1f;
        Vector2 shake = Vector2.zero;

        if (_punchTimer > 0f)
        {
            _punchTimer -= dt;
            float p = Mathf.Clamp01(_punchTimer / _punchDuration);
            scale = 1f + (_punchScale - 1f) * p;
            shake = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (_shakeAmount * p);
        }

        foreach (BarPart part in _parts)
        {
            if (part.rect == null) continue;
            part.rect.localScale = part.scale * scale;
            part.rect.anchoredPosition = part.position + shake;
        }
    }
}
