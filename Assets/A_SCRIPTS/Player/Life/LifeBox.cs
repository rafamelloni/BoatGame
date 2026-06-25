using UnityEngine;

public class LifeBox : MonoBehaviour
{
    
    [SerializeField] private ParticleSystem _lifeParticlesPrefab;
    [SerializeField] private float _amount = 25f;

    [Header("Indicator")]
    [SerializeField] float _edgePadding = 60f;
    [SerializeField] float _hideDistance = 20f;
    [SerializeField] float _minDistance = 20f;
    [SerializeField] float _maxDistance = 500f;
    [SerializeField] float _minScale = 0.3f;
    [SerializeField] float _maxScale = 1f;

    RectTransform _indicator;
    RectTransform _indicatorImage;
    Camera _cam;
    bool _indicatorActive;

    public void InitIndicator(RectTransform indicator)
    {
        _indicator = indicator;
        _indicatorImage = _indicator.GetChild(0).GetComponent<RectTransform>();
        _cam = Camera.main;
        _indicatorActive = true;
        _indicator.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!_indicatorActive || _indicator == null) return;

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

        if (_indicatorImage != null)
            _indicatorImage.rotation = Quaternion.identity;

        float t = Mathf.InverseLerp(_minDistance, _maxDistance, dist);
        float scale = Mathf.Lerp(_maxScale, _minScale, t);
        _indicator.localScale = Vector3.one * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        other.GetComponent<RT_PlayerHealth>().Heal(_amount);

        

        if (_lifeParticlesPrefab != null)
        {
            ParticleSystem particles = Instantiate(
                _lifeParticlesPrefab,
                other.transform.position,
                Quaternion.identity
            );
            particles.Play();
            Destroy(particles.gameObject,
                particles.main.duration + particles.main.startLifetime.constantMax);
        }

        // Ocultar indicador antes de desactivar
        if (_indicator != null)
        {
            Destroy(_indicator.gameObject);
            _indicator = null;
        }
        _indicatorActive = false;

        Destroy(gameObject);
    }

    public void ForceDestroy()
    {
        if (_indicator != null)
        {
            Destroy(_indicator.gameObject);
            _indicator = null;
        }
        _indicatorActive = false;
        Destroy(gameObject);
    }
}