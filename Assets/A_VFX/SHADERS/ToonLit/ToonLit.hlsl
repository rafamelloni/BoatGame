#ifndef BOTES_TOON_LIT_INCLUDED
#define BOTES_TOON_LIT_INCLUDED

// Iluminacion toon para objetos con textura (Botes).
// - ShadowThreshold / ShadowSoftness: donde corta luz/sombra y que tan duro es el corte (0.01 = cel shading duro).
// - Las sombras proyectadas usan el mismo corte, asi tienen borde limpio aunque la luz use Soft Shadows.
// - La sombra = textura * ShadowColor + un poco de luz del cielo (AmbientStrength). En 0 = solo ShadowColor.
// - TextureDetail mezcla con una version borroneada (mip alto). En texturas atlas dejarlo en 1.
// - Línea (LineColor/Width/Strength): franja oscura en el corte luz/sombra, look de dibujo.
// - Anti shadow acne: muestreo desplazado sobre la normal + sin sombra proyectada en caras rasantes.

void ToonLit_float(UnityTexture2D BaseMap, float2 UV, float4 BaseColor, float4 ShadowColor,
                   float ShadowThreshold, float ShadowSoftness, float TextureDetail, float BlurMip,
                   float3 PositionWS, float3 NormalWS, float AmbientStrength,
                   float4 LineColor, float LineWidth, float LineStrength,
                   out float3 Color)
{
    float2 uv = BaseMap.GetTransformedUV(UV);
    float3 texSharp = SAMPLE_TEXTURE2D(BaseMap.tex, BaseMap.samplerstate, uv).rgb;
    float3 texBlur  = SAMPLE_TEXTURE2D_LOD(BaseMap.tex, BaseMap.samplerstate, uv, BlurMip).rgb;
    float3 albedo   = lerp(texBlur, texSharp, TextureDetail) * BaseColor.rgb;

    float3 n    = normalize(NormalWS);
    float  soft = max(ShadowSoftness, 0.001);

#ifdef SHADERGRAPH_PREVIEW
    float3 lightDir   = normalize(float3(0.5, 0.6, -0.4));
    float3 lightColor = float3(1, 1, 1);
    float  shadowAtt  = 1.0;
    float3 ambient    = float3(0.3, 0.3, 0.35);
#else
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS + n * 0.05);
    Light mainLight    = GetMainLight(shadowCoord, PositionWS, half4(1, 1, 1, 1));
    float3 lightDir    = mainLight.direction;
    float3 lightColor  = mainLight.color;
    float  facing      = saturate(dot(n, lightDir) * 4.0);
    float  rawShadow   = lerp(1.0, mainLight.shadowAttenuation, facing);
    // Mismo corte que la rampa: sombra proyectada con borde marcado
    float  shadowAtt   = smoothstep(0.5 - soft, 0.5 + soft, rawShadow);
    float3 ambient     = SampleSH(n);
#endif

    float NdotL = dot(n, lightDir);
    float lightAmount = smoothstep(ShadowThreshold - soft, ShadowThreshold + soft, NdotL);
    lightAmount *= shadowAtt;

    float3 litCol    = albedo * lightColor;
    float3 shadowCol = albedo * (ShadowColor.rgb + ambient * AmbientStrength);
    Color = lerp(shadowCol, litCol, lightAmount);

    // línea de "dibujo" justo donde la luz pasa a sombra (grosor en unidades de N·L)
    float w = max(LineWidth, 0.0001);
    float lineMask = (1.0 - smoothstep(w * 0.5, w, abs(NdotL - ShadowThreshold))) * LineStrength * step(0.0001, LineWidth);
    Color = lerp(Color, albedo * LineColor.rgb, saturate(lineMask));
}

#endif
