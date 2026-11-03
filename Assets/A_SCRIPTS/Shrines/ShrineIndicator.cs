using UnityEngine;

// Indicador en el borde de pantalla. Solo aparece si estás cerca del templo y no lo ves.
public class ShrineIndicator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform _indicatorPrefab;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _player;

    [Header("Distancia")]
    [Tooltip("Más lejos que esto, no se muestra")]
    [SerializeField] private float _showDistance = 70f;
    [SerializeField] private float _minDistance = 15f;

    [Header("Visual")]
    [SerializeField] private float _edgePadding = 60f;
    [SerializeField] private float _minScale = 0.5f;
    [SerializeField] private float _maxScale = 1f;

    private ChargeShrine _target;
    private RectTransform _indicator;
    private RectTransform _icon;
    private Camera _camera;

    public void SetTarget(ChargeShrine target)
    {
        _target = target;

        if (_indicator == null && target != null && _indicatorPrefab != null && _canvas != null)
        {
            _indicator = Instantiate(_indicatorPrefab, _canvas.transform);
            if (_indicator.childCount > 0) _icon = _indicator.GetChild(0) as RectTransform;
        }

        if (_indicator != null) _indicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_indicator == null) return;

        if (_camera == null || !_camera.isActiveAndEnabled) _camera = Camera.main;

        if (_camera == null || _player == null || _target == null ||
            !_target.gameObject.activeInHierarchy || _target.IsCompleted)
        {
            Hide();
            return;
        }

        Vector3 toTarget = _target.transform.position - _player.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        Vector3 screenPosition = _camera.WorldToScreenPoint(_target.transform.position);
        bool isOnScreen = screenPosition.z > 0f
            && screenPosition.x > 0f && screenPosition.x < Screen.width
            && screenPosition.y > 0f && screenPosition.y < Screen.height;

        if (isOnScreen || distance > _showDistance)
        {
            Hide();
            return;
        }

        _indicator.gameObject.SetActive(true);

        if (screenPosition.z < 0f) screenPosition *= -1f;
        screenPosition -= new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        float angle = Mathf.Atan2(screenPosition.y, screenPosition.x);
        float slope = Mathf.Tan(angle);
        float halfWidth = Screen.width * 0.5f - _edgePadding;
        float halfHeight = Screen.height * 0.5f - _edgePadding;

        Vector2 clamped;
        if (Mathf.Abs(slope) * halfWidth <= halfHeight)
        {
            float sign = Mathf.Sign(screenPosition.x);
            clamped = new Vector2(sign * halfWidth, sign * slope * halfWidth);
        }
        else
        {
            float sign = Mathf.Sign(screenPosition.y);
            clamped = new Vector2(sign * halfHeight / slope, sign * halfHeight);
        }

        _indicator.anchoredPosition = clamped;
        _indicator.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f);
        if (_icon != null) _icon.rotation = Quaternion.identity;

        float t = Mathf.InverseLerp(_minDistance, _showDistance, distance);
        _indicator.localScale = Vector3.one * Mathf.Lerp(_maxScale, _minScale, t);
    }

    private void Hide()
    {
        if (_indicator.gameObject.activeSelf) _indicator.gameObject.SetActive(false);
    }
}
