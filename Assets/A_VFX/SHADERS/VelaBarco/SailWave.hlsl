#ifndef SAIL_WAVE_INCLUDED
#define SAIL_WAVE_INCLUDED

// Movimiento de vela atada ARRIBA y ABAJO (esos bordes no se mueven nunca).
//
// Qué es "arriba/abajo/costados" lo calcula SailController (C#) mirando la malla real:
//   AxisV = eje vertical de la malla, AxisU = eje horizontal, AxisN = eje del grosor (hacia donde infla)
//   BoundsMin/Max = caja de la malla (object space)
// Si no hay SailController (axes en 0), cae al UV + normal del vértice.
//
// Offset: cuánto mover el vértice (object space). ShadeMul: multiplicador de color para pliegues toon.

#define SAIL_PI 3.14159265

void SailWave_float(float2 UV, float3 ObjPos, float3 ObjNormal,
                    float4 AxisU, float4 AxisV, float4 AxisN, float4 BoundsMin, float4 BoundsMax,
                    float Speed, float Turn, float Phase, float Size,
                    float BillowAmount, float BillowDir, float IdleBillow, float IdleSway, float IdleSwaySpeed,
                    float FlutterIdle, float FlutterMove, float FlutterFreq, float FlutterSpeed,
                    float TwistAmount, float ShadeAmount, float Exaggeration, float SwapUV,
                    out float3 Offset, out float ShadeMul)
{
    float t = _Time.y + Phase;
    float s = saturate(Speed);
    float sEase = s * s * (3.0 - 2.0 * s);
    float ex = max(0.0, Exaggeration);

    // --- coordenadas de la vela (0..1) ---
    float u, v;
    float3 dir;
    bool hasAxes = dot(AxisV.xyz, AxisV.xyz) > 0.5;
    if (hasAxes)
    {
        float vMin = dot(BoundsMin.xyz, AxisV.xyz), vMax = dot(BoundsMax.xyz, AxisV.xyz);
        float uMin = dot(BoundsMin.xyz, AxisU.xyz), uMax = dot(BoundsMax.xyz, AxisU.xyz);
        v = saturate((dot(ObjPos, AxisV.xyz) - vMin) / max(vMax - vMin, 0.0001));
        u = saturate((dot(ObjPos, AxisU.xyz) - uMin) / max(uMax - uMin, 0.0001));
        dir = AxisN.xyz;
    }
    else
    {
        float2 uvs = SwapUV > 0.5 ? UV.yx : UV.xy;
        u = saturate(uvs.x);
        v = saturate(uvs.y);
        dir = normalize(ObjNormal);
    }

    // atada arriba y abajo: 0 en esos bordes, 1 en el medio
    float pinV = sin(SAIL_PI * v);
    float bellyU = lerp(0.55, 1.0, sin(SAIL_PI * u));
    float belly = pinV * bellyU;

    // INFLADO: de flojo (idle) a lleno de viento, con "respiración" marcada (toon)
    float inflate = lerp(IdleBillow, BillowAmount, sEase);
    inflate *= 1.0 + 0.18 * sin(t * 2.7) + 0.10 * sin(t * 5.3 + u * 3.0);

    // IDLE: se mece lento
    float sway = sin(t * IdleSwaySpeed + u * 2.2) * IdleSway * (1.0 - sEase);

    // FLAMEO: ondas que recorren la vela, más rápidas y fuertes con la velocidad
    float fs = FlutterSpeed * (1.0 + s * 2.0);
    float f1 = sin(u * FlutterFreq * 6.2831853 - t * fs);
    float f2 = sin((u * 1.7 + v * 0.9) * FlutterFreq * 6.2831853 - t * fs * 1.37);
    float flutter = f1 * 0.6 + f2 * 0.4;
    float flutterAmp = lerp(FlutterIdle, FlutterMove, s);

    // GIRO: un costado se infla más que el otro
    float twist = Turn * (u - 0.5) * 2.0 * TwistAmount;

    float d = belly * (inflate + twist) + pinV * (sway + flutter * flutterAmp);
    Offset = dir * (d * ex * Size * BillowDir);

    // pliegues toon
    float trough = smoothstep(0.42, 0.58, 0.5 - 0.5 * flutter);
    float strength = pinV * saturate(flutterAmp / max(FlutterMove, 0.0001) + 0.25);
    ShadeMul = 1.0 - ShadeAmount * trough * strength;
}

void SailWave_half(half2 UV, half3 ObjPos, half3 ObjNormal,
                   half4 AxisU, half4 AxisV, half4 AxisN, half4 BoundsMin, half4 BoundsMax,
                   half Speed, half Turn, half Phase, half Size,
                   half BillowAmount, half BillowDir, half IdleBillow, half IdleSway, half IdleSwaySpeed,
                   half FlutterIdle, half FlutterMove, half FlutterFreq, half FlutterSpeed,
                   half TwistAmount, half ShadeAmount, half Exaggeration, half SwapUV,
                   out half3 Offset, out half ShadeMul)
{
    float3 o; float sm;
    SailWave_float(UV, ObjPos, ObjNormal, AxisU, AxisV, AxisN, BoundsMin, BoundsMax, Speed, Turn, Phase, Size,
                   BillowAmount, BillowDir, IdleBillow, IdleSway, IdleSwaySpeed,
                   FlutterIdle, FlutterMove, FlutterFreq, FlutterSpeed, TwistAmount, ShadeAmount, Exaggeration, SwapUV, o, sm);
    Offset = o; ShadeMul = sm;
}

#endif
