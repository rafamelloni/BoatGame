using UnityEngine;

// Maneja solo el barco del menú principal. Usa el MISMO Movement que el juego
// (mismo feel, sway de olas, bank al girar, estelas y velas), pero en vez del
// teclado elige puntos al azar dentro de lo que ve la cámara y navega hacia ellos.
// Si se acerca demasiado al borde de la pantalla, vuelve hacia el centro.
[DefaultExecutionOrder(100)] // después del Start de Movement (que se apaga solo)
public class MenuBoatAutopilot : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [Header("Zona de pantalla")]
    [Tooltip("Margen de pantalla (0-0.45) donde NO se eligen destinos. Más alto = el barco anda más por el centro.")]
    [SerializeField, Range(0f, 0.45f)] private float _targetMargin = 0.25f;
    [Tooltip("Si el barco pasa este margen de pantalla, pega la vuelta hacia el centro.")]
    [SerializeField, Range(0f, 0.45f)] private float _edgeMargin = 0.1f;

    [Header("Navegación")]
    [Tooltip("Distancia a la que considera que llegó al destino")]
    [SerializeField] private float _arriveDistance = 6f;
    [Tooltip("Distancia mínima entre el barco y un destino nuevo")]
    [SerializeField] private float _minTargetDistance = 15f;
    [Tooltip("Cada cuánto cambia de destino aunque no haya llegado (min, max)")]
    [SerializeField] private Vector2 _retargetTime = new Vector2(4f, 9f);
    [Tooltip("Ángulo (grados) a partir del cual el timón queda a fondo")]
    [SerializeField] private float _fullTurnAngle = 45f;
    [Tooltip("Qué tan rápido mueve el timón (más bajo = giros más suaves)")]
    [SerializeField] private float _steerResponse = 2f;
    [SerializeField, Range(0.1f, 1f)] private float _cruiseThrottle = 0.8f;
    [Tooltip("Velocidad mientras gira cerrado")]
    [SerializeField, Range(0.1f, 1f)] private float _turnThrottle = 0.55f;

    [SerializeField] private bool _drawGizmos = true;

    [Header("Escape (al darle Play)")]
    [Tooltip("Multiplica la velocidad del barco mientras escapa")]
    [SerializeField] private float _escapeSpeedMultiplier = 1.5f;
    [Tooltip("Qué tan rápido endereza hacia la derecha al escapar")]
    [SerializeField] private float _escapeSteerResponse = 4f;

    private Movement _movement;
    private bool _escaping;
    private Vector3 _escapeDir;
    private Vector3 _target;
    private float _retargetAt;
    private float _steer;
    private bool _returning;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
        if (_camera == null) _camera = Camera.main;
    }

    private void Start()
    {
        if (_movement == null)
        {
            Debug.LogError("[MenuBoatAutopilot] Falta Movement en el barco.", this);
            enabled = false;
            return;
        }

        _movement.SetSprintEnabled(false);
        _movement.InputOverride = GetInput;
        _movement.SetMovementEnabled(true);
        PickTarget();
    }

    private void OnDestroy()
    {
        if (_movement != null) _movement.InputOverride = null;
    }

    // Al darle Play: deja de pasear y se va derecho hacia la derecha de la pantalla
    public void Escape()
    {
        if (_escaping || _camera == null) return;
        _escaping = true;

        _escapeDir = _camera.transform.right;
        _escapeDir.y = 0f;
        _escapeDir.Normalize();

        var stats = GetComponent<RT_PlayerStats>();
        if (stats != null) stats.moveSpeed *= _escapeSpeedMultiplier;
    }

    // Qué tan afuera de la pantalla está el barco (viewport x; > 1 = salió por la derecha)
    public float ViewportX => _camera != null ? _camera.WorldToViewportPoint(transform.position).x : 0f;

    private Vector2 GetInput()
    {
        if (!enabled || _camera == null) return Vector2.zero;

        Vector3 pos = transform.position;

        if (_escaping)
        {
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            float a = Vector3.SignedAngle(fwd, _escapeDir, Vector3.up);
            float s = Mathf.Clamp(a / 25f, -1f, 1f);
            _steer = Mathf.MoveTowards(_steer, s, Time.deltaTime * _escapeSteerResponse);
            return new Vector2(_steer, 1f);
        }

        if (IsNearEdge(pos))
        {
            if (!_returning)
            {
                _returning = true;
                _target = ViewportToWater(new Vector2(0.5f, 0.5f));
                _retargetAt = Time.time + 3f;
            }
        }
        else if (_returning || FlatDistance(pos, _target) < _arriveDistance || Time.time >= _retargetAt)
        {
            _returning = false;
            PickTarget();
        }

        Vector3 toTarget = _target - pos;
        toTarget.y = 0f;
        Vector3 forward = transform.forward;
        forward.y = 0f;

        float angle = Vector3.SignedAngle(forward, toTarget, Vector3.up);
        float desiredSteer = Mathf.Clamp(angle / Mathf.Max(1f, _fullTurnAngle), -1f, 1f);
        _steer = Mathf.MoveTowards(_steer, desiredSteer, Time.deltaTime * _steerResponse);

        float throttle = Mathf.Lerp(_cruiseThrottle, _turnThrottle, Mathf.Abs(_steer));
        return new Vector2(_steer, throttle);
    }

    private void PickTarget()
    {
        Vector3 pos = transform.position;
        Vector3 best = ViewportToWater(new Vector2(0.5f, 0.5f));

        for (int i = 0; i < 12; i++)
        {
            Vector2 vp = new Vector2(
                Random.Range(_targetMargin, 1f - _targetMargin),
                Random.Range(_targetMargin, 1f - _targetMargin));
            Vector3 candidate = ViewportToWater(vp);
            if (FlatDistance(pos, candidate) >= _minTargetDistance)
            {
                best = candidate;
                break;
            }
        }

        _target = best;
        _retargetAt = Time.time + Random.Range(_retargetTime.x, _retargetTime.y);
    }

    private bool IsNearEdge(Vector3 worldPos)
    {
        Vector3 vp = _camera.WorldToViewportPoint(worldPos);
        return vp.x < _edgeMargin || vp.x > 1f - _edgeMargin ||
               vp.y < _edgeMargin || vp.y > 1f - _edgeMargin;
    }

    // Punto del agua (a la altura del barco) que se ve en esa posición de pantalla
    private Vector3 ViewportToWater(Vector2 viewport)
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return transform.position;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos || !Application.isPlaying) return;
        if (_escaping) return;
        Gizmos.color = _returning ? Color.red : Color.green;
        Gizmos.DrawWireSphere(_target, 1.5f);
        Gizmos.DrawLine(transform.position, _target);
    }
}
