// Agua para el Overworld: misma idea que WaterShaderTurbulence (color por profundidad,
// espuma contra los bordes, normal map doble, refracción, brillo) pero con todos los
// valores pensados para océanos chicos (~10 unidades). Las distancias están en unidades de mundo.
// Necesita Depth Texture y Opaque Texture prendidos en el URP Asset (el agua del juego ya los usa).
Shader "Botes/OverworldWater"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.75, 0.8, 1)
        _DeepColor ("Deep Color", Color) = (0.05, 0.25, 0.45, 1)
        _DepthDistance ("Depth Distance (unidades hasta el deep)", Float) = 0.9
        _DepthPower ("Depth Power", Range(0.2, 4)) = 1.2
        _ShallowOpacity ("Shallow Opacity", Range(0, 1)) = 0.55

        [Header(Foam)]
        _FoamColor ("Foam Color (alpha = intensidad)", Color) = (1, 1, 1, 0.9)
        _FoamDistance ("Foam Distance (unidades)", Float) = 0.12
        _FoamNoiseScale ("Foam Noise Scale", Float) = 6
        _FoamSpeed ("Foam Speed", Float) = 0.25
        _FoamSoftness ("Foam Softness", Range(0.001, 0.5)) = 0.06

        [Header(Normals)]
        [NoScaleOffset][Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalTiling ("Normal Tiling (por unidad)", Float) = 0.35
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.45
        _ScrollSpeed ("Scroll Speed", Float) = 0.03

        [Header(Waves)]
        _WaveHeight ("Wave Height", Float) = 0.015
        _WaveFrequency ("Wave Frequency", Float) = 2.2
        _WaveSpeed ("Wave Speed", Float) = 0.5

        [Header(Light)]
        _Refraction ("Refraction", Range(0, 0.1)) = 0.012
        _Glossiness ("Glossiness", Range(1, 512)) = 120
        _SpecularStrength ("Specular Strength", Range(0, 3)) = 0.7
        _FresnelColor ("Fresnel Color", Color) = (0.8, 0.95, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 4
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "OverworldWater"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Off
            // Mezclamos a mano con la Opaque Texture (para la refracción), por eso no hace falta Blend
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DepthDistance;
                float _DepthPower;
                float _ShallowOpacity;
                half4 _FoamColor;
                float _FoamDistance;
                float _FoamNoiseScale;
                float _FoamSpeed;
                float _FoamSoftness;
                float _NormalTiling;
                float _NormalStrength;
                float _ScrollSpeed;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _Refraction;
                float _Glossiness;
                float _SpecularStrength;
                half4 _FresnelColor;
                float _FresnelPower;
                float _FresnelStrength;
            CBUFFER_END

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float t = _Time.y * _WaveSpeed;
                ws.y += (sin(ws.x * _WaveFrequency + t) + sin(ws.z * _WaveFrequency * 0.83 + t * 1.27)) * 0.5 * _WaveHeight;
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 posWS = i.positionWS;
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float surfaceEye = i.screenPos.w;

                // Normales: dos capas del normal map desplazándose en direcciones distintas
                float2 uvA = posWS.xz * _NormalTiling + _Time.y * _ScrollSpeed * float2(1.0, 0.6);
                float2 uvB = posWS.xz * _NormalTiling * 1.37 - _Time.y * _ScrollSpeed * float2(0.7, 1.0);
                float3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvA));
                float3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvB));
                float2 nxy = (nA.xy + nB.xy) * _NormalStrength;
                float3 normalWS = normalize(float3(nxy.x, 1.0, nxy.y));

                // Profundidad del agua en este pixel (distancia hasta lo que hay abajo)
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float depthDiff = max(0.0, sceneEye - surfaceEye);

                // Refracción: si el pixel desplazado cae sobre algo que está delante del agua, no se desplaza
                float2 refrUV = screenUV + nxy * _Refraction;
                float refrEye = LinearEyeDepth(SampleSceneDepth(refrUV), _ZBufferParams);
                if (refrEye < surfaceEye) { refrUV = screenUV; refrEye = sceneEye; }
                float refrDepth = max(0.0, refrEye - surfaceEye);
                float3 sceneCol = SampleSceneColor(refrUV);

                // Color por profundidad
                float depth01 = pow(saturate(refrDepth / max(_DepthDistance, 1e-4)), _DepthPower);
                float3 waterCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                float opacity = lerp(_ShallowOpacity, 1.0, depth01);
                float3 col = lerp(sceneCol, waterCol, opacity);

                // Espuma contra los bordes, cortada con ruido para que no sea una línea pareja
                float n = ValueNoise(posWS.xz * _FoamNoiseScale + _Time.y * _FoamSpeed * float2(1.0, 0.5));
                n = n * 0.5 + ValueNoise(posWS.xz * _FoamNoiseScale * 2.1 - _Time.y * _FoamSpeed) * 0.5;
                float foamLine = 1.0 - saturate(depthDiff / max(_FoamDistance, 1e-4));
                float foam = smoothstep(n - _FoamSoftness, n + _FoamSoftness, foamLine) * _FoamColor.a;
                col = lerp(col, _FoamColor.rgb, foam);

                // Luz: brillo especular de la luz principal + fresnel
                Light mainLight = GetMainLight();
                float3 viewDir = GetWorldSpaceNormalizeViewDir(posWS);
                float3 h = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, h)), _Glossiness) * _SpecularStrength;
                col += spec * mainLight.color * (1.0 - foam);
                float fres = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower) * _FresnelStrength;
                col = lerp(col, _FresnelColor.rgb, fres * (1.0 - foam));

                col = MixFog(col, i.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
