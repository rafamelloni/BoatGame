#ifndef WATER_TURBULENCE_INCLUDED
#define WATER_TURBULENCE_INCLUDED

#define MAX_TURBULENCE 16   // tiene que coincidir con WaterTurbulenceManager.MaxPoints

// xy = posición XZ en mundo, z = radio, w = intensidad (0..1)
// Globales: las setea WaterTurbulenceManager con Shader.SetGlobalVectorArray.
// NO agregarlas al Blackboard del Shader Graph (daría error de redefinición).
float4 _TurbulencePoints[MAX_TURBULENCE];
float  _TurbulenceCount;

#define MAX_IMPACTS 32      // tiene que coincidir con WaterImpacts.MaxImpacts

// xy = posición XZ en mundo, z = momento del impacto (reloj de C#), w = multiplicador de tamaño de la bala
float4 _ImpactPoints[MAX_IMPACTS];
// Reloj actual que manda C# (mismo que usa al registrar los impactos)
float _ImpactTime;

float Turb_Hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float Turb_Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = Turb_Hash(i);
    float b = Turb_Hash(i + float2(1, 0));
    float c = Turb_Hash(i + float2(0, 1));
    float d = Turb_Hash(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float Turb_Churn(float2 uv, float t, float Speed)
{
    float n1 = Turb_Noise(uv + float2(t, -t * 0.7) * Speed);
    float n2 = Turb_Noise(uv * 1.9 + float2(-t * 0.8, t) * Speed + 17.3);
    return (n1 + n2) * 0.5;
}

// Anillos de espuma que se abren desde donde pegó una bala
float Impact_Foam(float3 WorldPos, float Lifetime, float Radius, float RingWidth, out float2 normalPush)
{
    float foam = 0.0;
    normalPush = float2(0.0, 0.0);
    float life = max(Lifetime, 0.01);
    float t = _ImpactTime;

    for (int i = 0; i < MAX_IMPACTS; i++)
    {
        float4 p = _ImpactPoints[i];
        float age = t - p.z;
        if (age < 0.0 || age > life || p.w <= 0.0) continue;

        float a = age / life;                       // 0 -> 1
        float r = Radius * p.w * (1.0 - (1.0 - a) * (1.0 - a)); // se abre rápido y frena
        float d = distance(WorldPos.xz, p.xy);
        float w = RingWidth * (1.0 - a * 0.7) + 0.02;
        float ring = 1.0 - saturate(abs(d - r) / w);
        if (ring <= 0.0) continue;

        float fade = 1.0 - a;
        float n = Turb_Noise(WorldPos.xz * 3.0 + p.xy);  // lo rompe un poco para que no sea un círculo perfecto
        float v = ring * (0.6 + 0.4 * n);
        foam = max(foam, step(1.0 - fade * 0.8, v));    // grueso al principio, se deshilacha y desaparece

        float2 dir = (WorldPos.xz - p.xy) / max(d, 0.001);
        normalPush += dir * ring * fade;
    }
    return foam;
}

void WaterTurbulence_float(float3 WorldPos, float NoiseScale, float Speed, float RingFrequency, float FoamAmount, float NormalStrength,
                           float ImpactLifetime, float ImpactRadius, float ImpactRingWidth, float ImpactNormal,
                           out float Mask, out float Foam, out float Height, out float3 NormalOffset)
{
    Mask = 0.0;
    Foam = 0.0;
    Height = 0.0;
    NormalOffset = float3(0.0, 0.0, 0.0);
    float ring = 0.0;
    float t = _Time.y;

    float2 impactPush;
    Foam = Impact_Foam(WorldPos, ImpactLifetime, ImpactRadius, ImpactRingWidth, impactPush);
    NormalOffset = float3(impactPush * ImpactNormal, 0.0);
    int count = (int)_TurbulenceCount;

    for (int i = 0; i < MAX_TURBULENCE; i++)
    {
        if (i >= count) break;
        float4 p = _TurbulencePoints[i];
        if (p.w <= 0.0001 || p.z <= 0.0001) continue;

        float d = distance(WorldPos.xz, p.xy);
        float m = (1.0 - smoothstep(0.0, p.z, d)) * p.w;
        if (m > Mask)
        {
            Mask = m;
            // ondas concéntricas que se abren hacia afuera
            ring = sin(d * RingFrequency - t * Speed * 2.0) * 0.5 + 0.5;
        }
    }

    if (Mask <= 0.0001) return;

    float2 uv = WorldPos.xz * NoiseScale;
    float churn = Turb_Churn(uv, t, Speed);

    float field = lerp(churn, ring, 0.35);
    Foam = max(Foam, step(1.0 - Mask * FoamAmount, field));   // más espuma cuanto más cerca del centro
    Height = (churn * 2.0 - 1.0) * Mask;           // burbujeo, de -1 a 1 escalado por la máscara

    // Normales agitadas por píxel: gradiente del ruido + las ondas concéntricas.
    // Se SUMA a la normal (tangent space) que ya tenía el agua y después se normaliza.
    const float e = 0.12;
    float cx = Turb_Churn(uv + float2(e, 0.0), t, Speed);
    float cz = Turb_Churn(uv + float2(0.0, e), t, Speed);
    float2 grad = float2(cx - churn, cz - churn) / e;
    NormalOffset += float3(-grad * NormalStrength * Mask, 0.0);
}

void WaterTurbulence_half(half3 WorldPos, half NoiseScale, half Speed, half RingFrequency, half FoamAmount, half NormalStrength,
                          half ImpactLifetime, half ImpactRadius, half ImpactRingWidth, half ImpactNormal,
                          out half Mask, out half Foam, out half Height, out half3 NormalOffset)
{
    float m, f, h; float3 n;
    WaterTurbulence_float(WorldPos, NoiseScale, Speed, RingFrequency, FoamAmount, NormalStrength,
                          ImpactLifetime, ImpactRadius, ImpactRingWidth, ImpactNormal, m, f, h, n);
    Mask = m; Foam = f; Height = h; NormalOffset = n;
}

#endif
