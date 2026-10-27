using System.Collections.Generic;
using UnityEngine;

// Le pasa al shader del agua (WaterToon.hlsl) dónde están los templos y cuánto se llenaron.
// Se crea solo la primera vez que un ChargeShrine se registra: no hace falta ponerlo en la escena.
[DefaultExecutionOrder(100)]
public class ShrineWaterManager : MonoBehaviour
{
    public const int MaxShrines = 8; // tiene que coincidir con MAX_SHRINES de WaterToon.hlsl

    private static readonly int DataID = Shader.PropertyToID("_ShrineData");
    private static readonly int ColorID = Shader.PropertyToID("_ShrineColor");
    private static readonly int ParamsID = Shader.PropertyToID("_ShrineParams");

    private static ShrineWaterManager _instance;
    private static readonly List<ChargeShrine> _shrines = new List<ChargeShrine>();

    private readonly Vector4[] _data = new Vector4[MaxShrines];
    private readonly Vector4[] _colors = new Vector4[MaxShrines];

    [Tooltip("Ancho del contorno de la zona, en metros")]
    [SerializeField] private float _zoneLineWidth = 0.18f;
    [Tooltip("Ancho de la línea que avanza al llenarse, en metros")]
    [SerializeField] private float _fillLineWidth = 0.25f;

    public static void Register(ChargeShrine shrine)
    {
        if (!_shrines.Contains(shrine)) _shrines.Add(shrine);
        if (_instance == null)
        {
            var go = new GameObject("ShrineWaterManager");
            _instance = go.AddComponent<ShrineWaterManager>();
        }
    }

    public static void Unregister(ChargeShrine shrine)
    {
        _shrines.Remove(shrine);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        Upload(0);
    }

    private void OnDisable() => Upload(0);

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void LateUpdate()
    {
        int count = 0;
        for (int i = 0; i < _shrines.Count && count < MaxShrines; i++)
        {
            var s = _shrines[i];
            if (s == null || s.WaterColor.w <= 0f) continue;
            _data[count] = s.WaterData;
            _colors[count] = s.WaterColor;
            count++;
        }
        Upload(count);
    }

    private void Upload(int count)
    {
        for (int i = count; i < MaxShrines; i++)
        {
            _data[i] = Vector4.zero;
            _colors[i] = Vector4.zero;
        }
        // siempre el array entero: Unity fija el tamaño en la primera llamada
        Shader.SetGlobalVectorArray(DataID, _data);
        Shader.SetGlobalVectorArray(ColorID, _colors);
        Shader.SetGlobalVector(ParamsID, new Vector4(count, _zoneLineWidth, _fillLineWidth, 0f));
    }
}
