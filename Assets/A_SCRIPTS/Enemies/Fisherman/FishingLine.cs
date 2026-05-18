using UnityEngine;

public class FishingLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _rodTip;
    [SerializeField] private Transform _hookAnchor; // punto en el agua donde cae el anzuelo

    [Header("Line Settings")]
    [SerializeField] private int _segmentCount = 12;
    [SerializeField] private float _lineWidth = 0.03f;

    [Header("Physics")]
    [SerializeField] private float _gravity = -9.8f;
    [SerializeField] private float _damping = 0.98f;
    [SerializeField] private float _stiffness = 0.5f; // qué tan tensa está la cuerda

    private Vector3[] _positions;
    private Vector3[] _velocities;
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _segmentCount;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth * 0.5f;

        _positions = new Vector3[_segmentCount];
        _velocities = new Vector3[_segmentCount];

        InitPositions();
    }

    private void InitPositions()
    {
        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);
            _positions[i] = Vector3.Lerp(_rodTip.position, _hookAnchor.position, t);
        }
    }

    private void Update()
    {
        Simulate();
        _lineRenderer.SetPositions(_positions);
        
    }


    private void Simulate()
    {
        // anclar extremos
        _positions[0] = _rodTip.position;
        _positions[_segmentCount - 1] = _hookAnchor.position;

        // física en segmentos intermedios
        for (int i = 1; i < _segmentCount - 1; i++)
        {
            _velocities[i].y += _gravity * Time.deltaTime;
            _velocities[i] *= _damping;
            _positions[i] += _velocities[i] * Time.deltaTime;
        }

        // restricción de distancia entre segmentos consecutivos
        float segmentLength = Vector3.Distance(_rodTip.position, _hookAnchor.position) / (_segmentCount - 1);

        for (int iter = 0; iter < 5; iter++)
        {
            for (int i = 1; i < _segmentCount - 1; i++)
            {
                // restricción con el anterior
                Vector3 dirPrev = _positions[i] - _positions[i - 1];
                float distPrev = dirPrev.magnitude;
                if (distPrev > segmentLength)
                    _positions[i] = _positions[i - 1] + dirPrev.normalized * segmentLength;

                // restricción con el siguiente
                Vector3 dirNext = _positions[i] - _positions[i + 1];
                float distNext = dirNext.magnitude;
                if (distNext > segmentLength)
                    _positions[i] = _positions[i + 1] + dirNext.normalized * segmentLength;
            }

            // suavizado hacia la línea recta para simular tensión
            for (int i = 1; i < _segmentCount - 1; i++)
            {
                float t = i / (float)(_segmentCount - 1);
                Vector3 straightPoint = Vector3.Lerp(_positions[0], _positions[_segmentCount - 1], t);
                _positions[i] = Vector3.Lerp(_positions[i], straightPoint, _stiffness * Time.deltaTime);
            }
        }
    }
}