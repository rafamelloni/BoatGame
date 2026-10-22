using System.Collections.Generic;
using UnityEngine;

public class TurbulenceSource
{
    public Vector3 Position;
    public float Radius;
    public float Intensity;
    internal float FadeSpeed; // > 0 cuando se está apagando
}

[DefaultExecutionOrder(100)]
public class WaterTurbulenceManager : MonoBehaviour
{
    public const int MaxPoints = 16; // tiene que coincidir con MAX_TURBULENCE del .hlsl

    private static readonly int PointsID = Shader.PropertyToID("_TurbulencePoints");
    private static readonly int CountID = Shader.PropertyToID("_TurbulenceCount");

    public static WaterTurbulenceManager Instance { get; private set; }

    private readonly List<TurbulenceSource> _sources = new List<TurbulenceSource>();
    private readonly Vector4[] _buffer = new Vector4[MaxPoints];

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Upload(0);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        Upload(0);
    }

    public TurbulenceSource Create(Vector3 position, float radius)
    {
        var s = new TurbulenceSource { Position = position, Radius = radius, Intensity = 0f };
        _sources.Add(s);
        return s;
    }

    // La apaga de a poco y la saca sola, aunque el enemigo ya no exista
    public void Release(TurbulenceSource s, float fadeOutTime)
    {
        if (s == null) return;
        if (fadeOutTime <= 0f) { _sources.Remove(s); return; }
        s.FadeSpeed = Mathf.Max(s.Intensity, 0.01f) / fadeOutTime;
    }

    public void ClearAll()
    {
        _sources.Clear();
        Upload(0);
    }

    private void LateUpdate()
    {
        for (int i = _sources.Count - 1; i >= 0; i--)
        {
            var s = _sources[i];
            if (s.FadeSpeed <= 0f) continue;
            s.Intensity -= s.FadeSpeed * Time.deltaTime;
            if (s.Intensity <= 0f) _sources.RemoveAt(i);
        }

        int count = 0;
        for (int i = 0; i < _sources.Count && count < MaxPoints; i++)
        {
            var s = _sources[i];
            if (s.Intensity <= 0f) continue;
            _buffer[count++] = new Vector4(s.Position.x, s.Position.z, s.Radius, Mathf.Clamp01(s.Intensity));
        }
        Upload(count);
    }

    private void Upload(int count)
    {
        for (int i = count; i < MaxPoints; i++) _buffer[i] = Vector4.zero;
        Shader.SetGlobalVectorArray(PointsID, _buffer); // siempre el array entero, Unity fija el tamaño en la primera llamada
        Shader.SetGlobalFloat(CountID, count);
    }
}
