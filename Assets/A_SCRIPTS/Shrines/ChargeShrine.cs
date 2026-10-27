using System;
using UnityEngine;
using UnityEngine.Events;

// Templo de carga (estilo Megabonk): mientras el barco navega dentro de la zona se va cargando.
// Si salís, la carga baja despacio (no se resetea de golpe: con un barco es fácil salirse un segundo).
// El agua se tiñe con el color del templo desde el centro hacia afuera a medida que se llena.
//
// Recompensa: al completarse abre el panel de ShrineRewardUI (cofre 3D + fragmento de reliquia).
// También tenés el evento OnCompleted (C#) y el UnityEvent "_onCompleted" del Inspector.
public class ChargeShrine : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("Radio del área en metros. Tiene que entrar cómodo el radio de giro del barco.")]
    [SerializeField] private float _radius = 10f;
    [Tooltip("Opcional. Si queda vacío busca el objeto con Movement.")]
    [SerializeField] private Transform _player;

    [Header("Carga")]
    [Tooltip("Segundos dentro de la zona para llenarlo")]
    [SerializeField] private float _chargeTime = 8f;
    [Tooltip("Cuánto baja por segundo estando afuera (fracción de la barra: 0.1 = 10% por segundo)")]
    [SerializeField] private float _decayPerSecond = 0.08f;
    [Tooltip("Qué tan rápido sigue el agua al progreso real (más alto = más inmediato)")]
    [SerializeField] private float _visualFollowSpeed = 6f;

    [Header("Agua")]
    [SerializeField] private Color _waterColor = new Color(0.95f, 0.75f, 0.2f, 1f);
    [Tooltip("Intensidad del contorno/tinte cuando nadie lo está cargando (0..1)")]
    [SerializeField, Range(0f, 1f)] private float _idleIntensity = 0.6f;
    [Tooltip("Agita el agua dentro de lo llenado (usa WaterTurbulenceManager si existe). 0 = apagado.")]
    [SerializeField, Range(0f, 1f)] private float _turbulence = 0.3f;

    [Header("Al completarse")]
    [Tooltip("Segundos que tarda en desvanecerse el color después de completarlo. 0 = queda pintado.")]
    [SerializeField] private float _fadeOutAfterComplete = 2.5f;
    [SerializeField] private bool _disableAfterFade = true;
    [Tooltip("Abre el panel del cofre con un fragmento de reliquia al completarse")]
    [SerializeField] private bool _showRewardPanel = true;
    [SerializeField] private UnityEvent _onCompleted;

    public event Action<ChargeShrine> OnCompleted;
    public event Action<float> OnProgressChanged;

    public float Progress { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool PlayerInside { get; private set; }
    public float Radius => _radius;

    // lo lee ShrineWaterManager
    public Vector4 WaterData { get; private set; }
    public Vector4 WaterColor { get; private set; }

    private float _visualFill;
    private float _intensity;
    private float _fadeTimer;
    private TurbulenceSource _turb;

    private void OnEnable()
    {
        Progress = 0f;
        _visualFill = 0f;
        IsCompleted = false;
        PlayerInside = false;
        _intensity = _idleIntensity;
        _fadeTimer = 0f;
        ShrineWaterManager.Register(this);
        UpdateWaterData();
    }

    private void OnDisable()
    {
        ShrineWaterManager.Unregister(this);
        if (_turb != null && WaterTurbulenceManager.Instance != null)
            WaterTurbulenceManager.Instance.Release(_turb, 0f);
        _turb = null;
    }

    private void Update()
    {
        if (_player == null)
        {
            var mv = FindFirstObjectByType<Movement>();
            if (mv != null) _player = mv.transform;
        }

        float dt = Time.deltaTime;

        if (!IsCompleted)
        {
            PlayerInside = _player != null && DistanceXZ(_player.position) <= _radius;

            float before = Progress;
            if (PlayerInside) Progress += dt / Mathf.Max(_chargeTime, 0.01f);
            else Progress -= _decayPerSecond * dt;
            Progress = Mathf.Clamp01(Progress);

            if (!Mathf.Approximately(before, Progress)) OnProgressChanged?.Invoke(Progress);
            if (Progress >= 1f) Complete();

            // más fuerte mientras lo estás cargando
            float targetIntensity = PlayerInside ? 1f : _idleIntensity;
            _intensity = Mathf.MoveTowards(_intensity, targetIntensity, dt * 2f);
        }
        else if (_fadeOutAfterComplete > 0f)
        {
            _fadeTimer += dt;
            _intensity = 1f - Mathf.Clamp01(_fadeTimer / _fadeOutAfterComplete);
            if (_intensity <= 0f && _disableAfterFade)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        _visualFill = Mathf.Lerp(_visualFill, Progress, 1f - Mathf.Exp(-_visualFollowSpeed * dt));
        UpdateWaterData();
        UpdateTurbulence();
    }

    private void Complete()
    {
        IsCompleted = true;
        Progress = 1f;
        PlayerInside = false;
        _intensity = 1f;
        _fadeTimer = 0f;
        OnCompleted?.Invoke(this);
        _onCompleted?.Invoke();
        if (_showRewardPanel) ShrineRewardUI.ShowFor(this);
    }

    private void UpdateWaterData()
    {
        Vector3 p = transform.position;
        WaterData = new Vector4(p.x, p.z, _radius, _radius * _visualFill);
        WaterColor = new Vector4(_waterColor.r, _waterColor.g, _waterColor.b, _intensity);
    }

    private void UpdateTurbulence()
    {
        var mgr = WaterTurbulenceManager.Instance;
        if (mgr == null || _turbulence <= 0f) return;

        if (_turb == null) _turb = mgr.Create(transform.position, _radius);
        _turb.Position = transform.position;
        _turb.Radius = Mathf.Max(_radius * _visualFill, 0.01f);
        _turb.Intensity = IsCompleted ? _turbulence * _intensity : _turbulence * _visualFill * (PlayerInside ? 1f : 0.5f);
    }

    private float DistanceXZ(Vector3 pos)
    {
        Vector3 d = pos - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _waterColor;
        const int seg = 48;
        Vector3 c = transform.position;
        Vector3 prev = c + new Vector3(_radius, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * _radius, 0f, Mathf.Sin(a) * _radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
