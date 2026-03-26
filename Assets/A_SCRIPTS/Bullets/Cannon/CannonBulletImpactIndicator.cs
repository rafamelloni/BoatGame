using UnityEngine;

public class CannonBulletImpactIndicator : MonoBehaviour
{

    [Header("Indicator")]
    [SerializeField] private Transform indicatorVisual;
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private string shaderFloatName = "_Size";


    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxPredictTime = 10f;
    [SerializeField] private float simulationStep = 0.05f;
    [SerializeField] private float groundOffset = 0.05f;

    [Header("Shader Values")]
    [SerializeField] private float startValue = 1f;
    [SerializeField] private float endValue = 0f;

    private MaterialPropertyBlock _mpb;
    private int _shaderFloatID;

    private float _flightTime;
    private float _currentTime;
    private bool _initialized;
    private Vector3 _originalLocalScale;

    private Transform _originalParent;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _shaderFloatID = Shader.PropertyToID(shaderFloatName);

        if (indicatorVisual != null)
        {
            _originalParent = indicatorVisual.parent;
            _originalLocalScale = indicatorVisual.localScale;
            indicatorVisual.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_initialized) return;
        if (indicatorRenderer == null) return;

        _currentTime += Time.deltaTime;

        float t = Mathf.Clamp01(_currentTime / _flightTime);
        float currentValue = Mathf.Lerp(startValue, endValue, t);

        indicatorRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_shaderFloatID, currentValue);
        indicatorRenderer.SetPropertyBlock(_mpb);
    }

    public void Init(Vector3 startPos, Vector3 startVelocity)
    {
        _initialized = false;
        _currentTime = 0f;
        _flightTime = 0f;

        if (indicatorVisual != null)
            indicatorVisual.gameObject.SetActive(false);

        Vector3 previousPos = startPos;

        for (float t = simulationStep; t <= maxPredictTime; t += simulationStep)
        {
            Vector3 currentPos = startPos
                               + startVelocity * t
                               + 0.5f * Physics.gravity * t * t;

            Vector3 dir = currentPos - previousPos;
            float distance = dir.magnitude;

            if (distance > 0f && Physics.Raycast(previousPos, dir.normalized, out RaycastHit hit, distance, groundLayer))
            {
                _flightTime = t;
                _currentTime = 0f;
                _initialized = true;

                if (indicatorVisual != null)
                {
                    indicatorVisual.SetParent(null, true);
                    indicatorVisual.position = hit.point + Vector3.up * groundOffset;
                    indicatorVisual.gameObject.SetActive(true);
                }

                if (indicatorRenderer != null)
                {
                    indicatorRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetFloat(_shaderFloatID, startValue);
                    indicatorRenderer.SetPropertyBlock(_mpb);
                }

                return;
            }

            previousPos = currentPos;
        }
    }

    public void ResetIndicator()
    {
        _initialized = false;
        _currentTime = 0f;
        _flightTime = 0f;

        if (indicatorVisual != null)
        {
            indicatorVisual.gameObject.SetActive(false);
            indicatorVisual.SetParent(_originalParent, false);
            indicatorVisual.localPosition = Vector3.zero;
            indicatorVisual.localRotation = Quaternion.identity;
            indicatorVisual.localScale = _originalLocalScale;
        }
    }
}

