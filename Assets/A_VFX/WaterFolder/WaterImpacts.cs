using UnityEngine;

// Registra dónde pegan las balas en el agua. El anillo de espuma lo dibuja el shader
// (WaterTurbulence.hlsl) y su look se ajusta desde el MATERIAL del agua:
// _ImpactLifetime, _ImpactRadius, _ImpactRingWidth, _ImpactNormal.
// Uso: WaterImpacts.Spawn(punto, multiplicadorDeTamaño);
public class WaterImpacts : MonoBehaviour
{
    public const int MaxImpacts = 32; // tiene que coincidir con MAX_IMPACTS del .hlsl

    private static readonly int PointsID = Shader.PropertyToID("_ImpactPoints");
    private static readonly int TimeID = Shader.PropertyToID("_ImpactTime");

    public static WaterImpacts Instance { get; private set; }

    private static bool _warned;

    private readonly Vector4[] _buffer = new Vector4[MaxImpacts];
    private int _next;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        ClearAll();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        ClearAll();
    }

    private void LateUpdate()
    {
        // el shader compara contra este reloj, no contra _Time, así nunca se desfasan
        Shader.SetGlobalFloat(TimeID, Time.time);
    }

    public static void Spawn(Vector3 position, float sizeMultiplier = 1f)
    {
        if (Instance != null) { Instance.Add(position, sizeMultiplier); return; }
        if (!_warned)
        {
            _warned = true;
            Debug.LogWarning("[WaterImpacts] No hay ningún WaterImpacts en la escena: agregá el componente a un GameObject.");
        }
    }

    public void Add(Vector3 position, float sizeMultiplier = 1f)
    {
        _buffer[_next] = new Vector4(position.x, position.z, Time.time, sizeMultiplier);
        _next = (_next + 1) % MaxImpacts;
        Shader.SetGlobalVectorArray(PointsID, _buffer);
        Shader.SetGlobalFloat(TimeID, Time.time);
    }

    public void ClearAll()
    {
        for (int i = 0; i < MaxImpacts; i++)
            _buffer[i] = new Vector4(0f, 0f, -9999f, 0f);
        _next = 0;
        Shader.SetGlobalVectorArray(PointsID, _buffer);
        Shader.SetGlobalFloat(TimeID, Time.time);
    }
}
