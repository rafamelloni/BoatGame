#ifndef WATER_TOON_INCLUDED
#define WATER_TOON_INCLUDED

// ---------------- Templos (ChargeShrine) ----------------
// Globales: las setea ShrineWaterManager con Shader.SetGlobalVectorArray.
// NO agregarlas al Blackboard del Shader Graph.
#define MAX_SHRINES 8   // tiene que coincidir con ShrineWaterManager.MaxShrines
float4 _ShrineData[MAX_SHRINES];   // xy = posición XZ, z = radio de la zona, w = radio ya llenado (metros)
float4 _ShrineColor[MAX_SHRINES];  // rgb = color, a = intensidad (0 = apagado)
float4 _ShrineParams;              // x = cantidad, y = ancho borde zona, z = ancho borde de llenado, w = 1 si completo (no se usa por templo)

// ---------------- Espuma bajo el crosshair (CrosshairWaterWake) ----------------
// Globales seteadas por CrosshairWaterWake.cs. NO agregarlas al Blackboard.
#define MAX_CURSOR_TRAIL 12  // tiene que coincidir con CrosshairWaterWake.MaxTrail
float4 _CursorFoam;        // xy = posición XZ (ya suavizada), z = radio, w = intensidad (0 = apagado)
float4 _CursorFoamParams;  // x = escala del ruido, y = velocidad del ruido, z = cantidad de puntos de estela
float4 _CursorFoamTrail[MAX_CURSOR_TRAIL]; // estela: mismo formato que _CursorFoam, se van apagando

float CursorFoam_Hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float CursorFoam_Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = CursorFoam_Hash(i);
    float b = CursorFoam_Hash(i + float2(1, 0));
    float c = CursorFoam_Hash(i + float2(0, 1));
    float d = CursorFoam_Hash(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Densidad de espuma de un punto (0..1): más densa en el centro
float CursorFoam_Density(float2 xz, float4 p, float core)
{
    if (p.w <= 0.001 || p.z <= 0.001) return 0.0;
    float d = distance(xz, p.xy);
    if (d > p.z) return 0.0;
    // core = parte del radio con densidad pareja (0 = cae desde el centro)
    return (1.0 - smoothstep(p.z * core, p.z, d)) * p.w;
}

// Manchas de espuma (mismo estilo cortado que la espuma del agua) bajo el cursor y en su estela
float CursorFoam_Mask(float3 posWS)
{
    float m = CursorFoam_Density(posWS.xz, _CursorFoam, 0.0);
    int count = (int)_CursorFoamParams.z;
    for (int i = 0; i < MAX_CURSOR_TRAIL; i++)
    {
        if (i >= count) break;
        m = max(m, CursorFoam_Density(posWS.xz, _CursorFoamTrail[i], 0.35));
    }
    if (m <= 0.0) return 0.0;

    float t = _Time.y * _CursorFoamParams.y;
    float2 uv = posWS.xz * max(_CursorFoamParams.x, 0.01);
    float n = (CursorFoam_Noise(uv + float2(t, -t * 0.7)) + CursorFoam_Noise(uv * 1.9 + float2(-t * 0.8, t) + 17.3)) * 0.5;
    return step(1.0 - m, n);
}

// ---------------- Estelas de enemigos (EnemyWaterWake) ----------------
// Globales seteadas por EnemyWaterWakeManager.cs. NO agregarlas al Blackboard.
// Las estelas están pintadas en una textura vista desde arriba que sigue al jugador:
// r = vida restante (1 -> 0), g = fuerza. Los bordes viven menos, así que se afina sola.
TEXTURE2D(_EnemyWakeTex);
SAMPLER(sampler_EnemyWakeTex);
float4 _EnemyWakeArea;    // xy = centro XZ, z = tamaño en metros, w = 1 si está activo
float4 _EnemyWakeParams;  // x = escala del ruido, y = velocidad del ruido, z = dissolve, w = nitidez del borde

// Manchas de espuma a lo largo de las estelas de los enemigos
float EnemyWake_Mask(float3 posWS)
{
    if (_EnemyWakeArea.w < 0.5) return 0.0;

    float2 uv = (posWS.xz - _EnemyWakeArea.xy) / _EnemyWakeArea.z + 0.5;
    if (uv.x <= 0.0 || uv.y <= 0.0 || uv.x >= 1.0 || uv.y >= 1.0) return 0.0;

    float4 w = SAMPLE_TEXTURE2D_LOD(_EnemyWakeTex, sampler_EnemyWakeTex, uv, 0);
    if (w.r <= 0.0005) return 0.0;

    float edge = saturate(w.r * max(_EnemyWakeParams.w, 1.0));   // borde: deshilachado o limpio
    float k = w.r * w.r * (3.0 - 2.0 * w.r);                       // desvanecido suave de la vida
    float m = w.g * edge * lerp(1.0, k, saturate(_EnemyWakeParams.z));
    if (m <= 0.0) return 0.0;

    float2 xz = posWS.xz;
    float tt = _Time.y * _EnemyWakeParams.y;
    float2 nuv = xz * max(_EnemyWakeParams.x, 0.01);
    float n = (CursorFoam_Noise(nuv + float2(tt, -tt * 0.7)) + CursorFoam_Noise(nuv * 1.9 + float2(-tt * 0.8, tt) + 31.7)) * 0.5;
    return step(1.0 - m, n);
}

float Shrine_Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

// Pinta el agua dentro del radio llenado con el color del templo (conserva las bandas del agua),
// dibuja una línea brillante en el borde que avanza y el contorno punteado de la zona.
float3 Shrine_Apply(float3 col, float3 posWS, float3 lightTint)
{
    int count = (int)_ShrineParams.x;
    float zoneW = max(_ShrineParams.y, 0.01);
    float fillW = max(_ShrineParams.z, 0.01);
    float t = _Time.y;

    for (int i = 0; i < MAX_SHRINES; i++)
    {
        if (i >= count) break;
        float4 d4 = _ShrineData[i];
        float4 c4 = _ShrineColor[i];
        if (c4.a <= 0.001) continue;

        float2 rel = posWS.xz - d4.xy;
        float d = length(rel);
        if (d > d4.z + zoneW * 2.0) continue;

        float ang = atan2(rel.y, rel.x);

        // borde de llenado ondulado para que no sea un círculo perfecto
        float wob = sin(ang * 7.0 + t * 2.0) * 0.12 + sin(ang * 3.0 - t * 1.3) * 0.08;
        float fillR = d4.w > 0.001 ? d4.w + wob : 0.0;

        // relleno: color del templo, más claro/oscuro según la banda del agua que había
        float inside = step(d, fillR);
        float3 tinted = c4.rgb * lightTint * (0.7 + 0.6 * Shrine_Luma(col));
        col = lerp(col, tinted, inside * c4.a * 0.85);

        // línea brillante en el borde que avanza
        float edge = step(abs(d - fillR), fillW) * step(0.001, d4.w);
        col = lerp(col, lerp(c4.rgb, float3(1, 1, 1), 0.55), edge * c4.a);

        // contorno de la zona: rayitas que giran despacio (sólido cuando está lleno)
        float dashes = step(0.5, frac(ang / 6.28318 * 28.0 + t * 0.15));
        float full = step(d4.z - 0.01, d4.w);
        float ring = step(abs(d - d4.z), zoneW) * max(dashes, full);
        col = lerp(col, lerp(c4.rgb, float3(1, 1, 1), 0.35), ring * c4.a);
    }
    return col;
}

// Sombreado toon del agua (Botes). Reemplaza la parte Lit/PBR de WaterShaderTurbulence:
// - Profundidad en bandas planas (DepthBands colores entre Shallow y Deep) en vez del degradé suave.
// - Transparencia/refracción igual que antes (SceneAmount + SceneColor), sin especular realista.
// - Recibe las sombras de los barcos con borde marcado y tinte ShadowColor (misma idea que ToonLit).
// - Relieve de olas en escalones (RippleShade) para que no quede plana.
// - Brillos estilizados: manchas planas en las pendientes de las olitas que miran al sol.
// - Espuma y turbulencia como estaban, por encima.
// - Templos (ChargeShrine): tiñen el agua en círculo a medida que se cargan.
// - Estelas de enemigos (EnemyWaterWake): espuma fina detrás de cada barco enemigo.

void WaterToonShade_float(float DepthT, float Foam, float SceneAmount, float3 SceneColor,
                          float TurbMask, float4 TurbColor, float4 ShallowColor, float4 DeepColor,
                          float3 NormalTS, float3 PositionWS,
                          float DepthBands, float BandSoftness, float4 FoamColor, float4 ShadowColor,
                          float SparkleThreshold, float4 SparkleColor, float RippleShade,
                          out float3 Color)
{
    // --- profundidad en bandas ---
    float b     = max(2.0, round(DepthBands));
    float x     = saturate(DepthT) * b;
    float level = min(floor(x), b - 1.0);
    float edge  = smoothstep(1.0 - max(BandSoftness, 0.0001), 1.0, frac(x));
    level      += (level < b - 1.0) ? edge : 0.0;
    float t     = level / (b - 1.0);
    float3 water = lerp(ShallowColor.rgb, DeepColor.rgb, t);

    // --- luz / sombra ---
#ifdef SHADERGRAPH_PREVIEW
    float3 lightDir = normalize(float3(0.5, 0.7, 0.3));
    float  shadow   = 1.0;
#else
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight    = GetMainLight(shadowCoord, PositionWS, half4(1, 1, 1, 1));
    float3 lightDir    = mainLight.direction;
    float  shadow      = smoothstep(0.45, 0.55, mainLight.shadowAttenuation);
#endif
    float3 lightTint = lerp(ShadowColor.rgb, float3(1, 1, 1), shadow);

    // pendiente de cada olita (normal en tangent space) contra la dirección del sol en XZ
    float2 sunXZ  = lightDir.xz;
    float  sunLen = length(sunXZ);
    sunXZ = sunLen > 0.001 ? sunXZ / sunLen : float2(0.0, 1.0);
    float  slope  = dot(NormalTS.xy, sunXZ);

    // --- relieve toon de las olas ---
    // las caras que miran al sol se aclaran y las de atrás se oscurecen, en escalones (no degradé)
    float r = clamp(slope / max(SparkleThreshold, 0.05), -1.0, 1.0);
    r = round(r * 2.0) * 0.5;               // -1, -0.5, 0, 0.5, 1
    water *= 1.0 + r * RippleShade;

    // cuerpo del agua: color plano sombreado + lo que se ve abajo (refracción)
    float3 col = lerp(water * lightTint, SceneColor, saturate(SceneAmount));

    // --- templos: el agua se va tiñendo desde el centro mientras se cargan ---
    col = Shrine_Apply(col, PositionWS, lightTint);

    // --- espuma del crosshair: se suma a la espuma del agua (sin brillo ni tinte) ---
    Foam = max(saturate(Foam), CursorFoam_Mask(PositionWS));

    // --- estelas de los enemigos (EnemyWaterWake) ---
    Foam = max(Foam, EnemyWake_Mask(PositionWS));

    // --- brillos estilizados ---
    float  sparkle = step(SparkleThreshold, slope) * shadow * (1.0 - saturate(Foam)) * SparkleColor.a;
    col = lerp(col, SparkleColor.rgb, sparkle);

    // --- espuma (un poco tintada si está en sombra) ---
    float3 foamTint = lerp(lerp(ShadowColor.rgb, float3(1, 1, 1), 0.5), float3(1, 1, 1), shadow);
    col = lerp(col, FoamColor.rgb * foamTint, saturate(Foam));

    // --- turbulencia ---
    col = lerp(col, TurbColor.rgb, saturate(TurbMask));

    Color = col;
}

void WaterToonShade_half(half DepthT, half Foam, half SceneAmount, half3 SceneColor,
                         half TurbMask, half4 TurbColor, half4 ShallowColor, half4 DeepColor,
                         half3 NormalTS, float3 PositionWS,
                         half DepthBands, half BandSoftness, half4 FoamColor, half4 ShadowColor,
                         half SparkleThreshold, half4 SparkleColor, half RippleShade,
                         out half3 Color)
{
    float3 c;
    WaterToonShade_float(DepthT, Foam, SceneAmount, SceneColor, TurbMask, TurbColor, ShallowColor, DeepColor,
                         NormalTS, PositionWS, DepthBands, BandSoftness, FoamColor, ShadowColor,
                         SparkleThreshold, SparkleColor, RippleShade, c);
    Color = c;
}

#endif
