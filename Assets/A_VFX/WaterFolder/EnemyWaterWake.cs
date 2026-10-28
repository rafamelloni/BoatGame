using UnityEngine;

// Deja una estela de espuma en el agua mientras el enemigo se mueve
// (mismo estilo que la del crosshair, pero más fina). Ponelo en el prefab de cada enemigo.
// Cuando el enemigo se desactiva, la estela que dejó se sigue desvaneciendo sola.
// Los valores globales (área, manchas, borde) están en EnemyWaterWakeManager.
public class EnemyWaterWake : MonoBehaviour
{
    [Header("Origen")]
    [Tooltip("Desde dónde sale la estela, relativo al enemigo (ej. la popa: Z negativo)")]
    [SerializeField] private Vector3 _localOffset = new Vector3(0f, 0f, -1f);

    [Header("Forma")]
    [Tooltip("Ancho de la estela pegada al barco: radio en metros")]
    [SerializeField, Min(0.02f)] private float _width = 0.45f;
    [Tooltip("Cómo se afina con el tiempo. Bajo = se afina enseguida y queda una línea finita; alto = se mantiene ancha y se afina recién al final.")]
    [SerializeField, Range(0.05f, 1f)] private float _endWidth = 0.35f;

    [Header("Duración")]
    [Tooltip("Segundos que tarda en desaparecer del todo (más alto = estela más larga)")]
    [SerializeField, Min(0.05f)] private float _lifetime = 1.5f;
    [Tooltip("Fracción de la vida en la que se mantiene con todo el ancho antes de empezar a afinarse")]
    [SerializeField, Range(0f, 0.9f)] private float _hold = 0.2f;

    [Header("Intensidad")]
    [Tooltip("Cuánta espuma tiene (0..1). Bajo = solo unas manchitas.")]
    [SerializeField, Range(0f, 1f)] private float _strength = 0.95f;
    [Tooltip("Velocidad mínima (m/s) para dejar estela. Quieto o casi quieto no deja nada.")]
    [SerializeField, Min(0f)] private float _minSpeed = 0.5f;
    [Tooltip("Suavizado de la velocidad medida (más alto = reacciona más rápido)")]
    [SerializeField, Min(0.1f)] private float _speedSmoothing = 8f;
    [Tooltip("Si se mueve más que esto en un frame (teletransporte, pool), no dibuja ese tramo")]
    [SerializeField, Min(0.1f)] private float _maxStepPerFrame = 4f;

    private Vector2 _lastPos;
    private bool _hasLast;
    private float _speed;

    private void OnDisable()
    {
        _hasLast = false;
        _speed = 0f;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // en pausa queda como está

        Vector3 wp = transform.TransformPoint(_localOffset);
        Vector2 p = new Vector2(wp.x, wp.z);

        if (!_hasLast)
        {
            _lastPos = p;
            _hasLast = true;
            return;
        }

        Vector2 from = _lastPos;
        float step = (p - from).magnitude;
        _lastPos = p;

        // teletransporte: no dibujar la línea
        if (step > _maxStepPerFrame)
        {
            _speed = 0f;
            return;
        }

        _speed = Mathf.Lerp(_speed, step / dt, 1f - Mathf.Exp(-_speedSmoothing * dt));
        if (_speed < _minSpeed) return;

        var mgr = EnemyWaterWakeManager.Get();
        if (mgr == null) return;
        mgr.AddStamp(from, p, _width, _lifetime, _strength, _hold, _endWidth);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(_localOffset), _width);
    }
}
