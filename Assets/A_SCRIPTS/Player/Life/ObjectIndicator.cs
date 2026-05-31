using UnityEngine;

public class ObjectIndicator : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _edgePadding = 60f;
    [SerializeField] private float _hideDistance = 50f;
    [SerializeField] private float _minDistance = 50f;
    [SerializeField] private float _maxDistance = 500f;
    [SerializeField] private float _minScale = 0.3f;
    [SerializeField] private float _maxScale = 1f;

    [SerializeField] private RectTransform _indicator;
    [SerializeField] private RectTransform _targetImage;

    [SerializeField] private float _maxShowDistance = 300f;

    public Camera _cam;



    private void Update()
    {
        if (_target == null || _indicator == null) return;

        Vector3 screenPos = _cam.WorldToScreenPoint(_target.position);
        bool isOnScreen = screenPos.z > 0
            && screenPos.x > 0 && screenPos.x < Screen.width
            && screenPos.y > 0 && screenPos.y < Screen.height;

        float dist = Vector3.Distance(_cam.transform.position, _target.position);

        if (isOnScreen || dist < _hideDistance || dist > _maxShowDistance)
        {
            _indicator.gameObject.SetActive(false);
            return;
        }

        _indicator.gameObject.SetActive(true);

        if (screenPos.z < 0) screenPos *= -1;

        Vector3 center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        screenPos -= center;

        float angle = Mathf.Atan2(screenPos.y, screenPos.x);
        float slope = Mathf.Tan(angle);
        float halfW = Screen.width / 2f - _edgePadding;
        float halfH = Screen.height / 2f - _edgePadding;

        Vector2 clamped;
        if (Mathf.Abs(slope) * halfW <= halfH)
        {
            float sign = Mathf.Sign(screenPos.x);
            clamped = new Vector2(sign * halfW, sign * slope * halfW);
        }
        else
        {
            float sign = Mathf.Sign(screenPos.y);
            clamped = new Vector2(sign * halfH / slope, sign * halfH);
        }

        _indicator.anchoredPosition = clamped;

        float rot = Mathf.Atan2(screenPos.y, screenPos.x) * Mathf.Rad2Deg;
        _indicator.rotation = Quaternion.Euler(0, 0, rot + 90f);

        if (_targetImage != null)
            _targetImage.rotation = Quaternion.identity;

        float t = Mathf.InverseLerp(_minDistance, _maxDistance, dist);
        float scale = Mathf.Lerp(_maxScale, _minScale, t);
        _indicator.localScale = Vector3.one * scale;
    }
}