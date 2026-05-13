using UnityEngine;
public class IslandIndicator : MonoBehaviour
{
    [SerializeField] float _edgePadding = 60f;
    [SerializeField] float _hideDistance = 50f;
    [SerializeField] float _minDistance = 50f;
    [SerializeField] float _maxDistance = 500f;
    [SerializeField] float _minScale = 0.3f;
    [SerializeField] float _maxScale = 1f;
    RectTransform _indicator;
    RectTransform _islaImage;
    Camera _cam;
    bool _active;

    public void Init(RectTransform indicator)
    {
        _indicator = indicator;
        _islaImage = _indicator.GetChild(0).GetComponent<RectTransform>();
        _cam = Camera.main;
        Show();
    }

    public void Show()
    {
        _active = true;
        _indicator?.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _active = false;
        if (_indicator != null)
        {
            Destroy(_indicator.gameObject);
            _indicator = null;
        }
    }

    void Update()
    {
        if (!_active || _indicator == null) return;

        Vector3 screenPos = _cam.WorldToScreenPoint(transform.position);
        bool isOnScreen = screenPos.z > 0
            && screenPos.x > 0 && screenPos.x < Screen.width
            && screenPos.y > 0 && screenPos.y < Screen.height;

        float dist = Vector3.Distance(_cam.transform.position, transform.position);

        if (isOnScreen || dist < _hideDistance)
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

        if (_islaImage != null)
            _islaImage.rotation = Quaternion.identity;

        float t = Mathf.InverseLerp(_minDistance, _maxDistance, dist);
        float scale = Mathf.Lerp(_maxScale, _minScale, t);
        _indicator.localScale = Vector3.one * scale;
    }
}