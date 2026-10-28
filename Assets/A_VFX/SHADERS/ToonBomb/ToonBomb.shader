// Toon con brillo "pintado" para objetos de metal (la bomba).
// Misma iluminación que ToonLit (corte luz/sombra, color de sombra, línea de dibujo) y además:
//  - Brillo especular toon: una mancha de borde duro donde pega el sol (en vez del metallic PBR).
//  - Rim: borde de luz finito alrededor del objeto, para separarlo del fondo.
//  - Pulso: emisión que late (para la mecha, o un rojo suave en todo el cuerpo). Apagado por defecto.
// Prueba aparte: si queda bien, se pasa al ToonLit.
Shader "Botes/Toon Bomb"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Tinte", Color) = (0.17, 0.18, 0.23, 1)
        _TextureDetail ("Detalle textura", Range(0, 1)) = 1
        _BlurMip ("Nivel de blur (mip)", Range(0, 8)) = 4

        [Header(Sombra)]
        _ShadowColor ("Color de sombra", Color) = (0.45, 0.45, 0.6, 1)
        _ShadowThreshold ("Umbral sombra", Range(-1, 1)) = 0
        _ShadowSoftness ("Suavidad sombra", Range(0.001, 0.5)) = 0.02
        _AmbientStrength ("Ambiente en sombra", Range(0, 1)) = 0.3

        [Header(Linea de dibujo)]
        _LineColor ("Línea - Color", Color) = (0.3, 0.3, 0.4, 1)
        _LineWidth ("Línea - Grosor", Range(0, 0.5)) = 0
        _LineStrength ("Línea - Fuerza", Range(0, 1)) = 0

        [Header(Brillo toon)]
        _SpecColor ("Brillo - Color", Color) = (1, 1, 1, 1)
        _SpecSize ("Brillo - Tamaño", Range(0, 0.5)) = 0.06
        _SpecSoftness ("Brillo - Suavidad del borde", Range(0.001, 0.2)) = 0.01
        _SpecStrength ("Brillo - Fuerza", Range(0, 1)) = 1
        [Toggle] _SpecInShadow ("Brillo - También en sombra proyectada", Float) = 0

        [Header(Rim)]
        _RimColor ("Rim - Color", Color) = (0.75, 0.82, 1, 1)
        _RimWidth ("Rim - Ancho", Range(0, 1)) = 0.25
        _RimSoftness ("Rim - Suavidad", Range(0.001, 0.5)) = 0.02
        _RimStrength ("Rim - Fuerza", Range(0, 1)) = 0.6
        _RimLitSide ("Rim - Solo del lado iluminado", Range(0, 1)) = 0.7

        [Header(Pulso emision)]
        [HDR] _PulseColor ("Pulso - Color", Color) = (1, 0.15, 0.05, 1)
        _PulseMin ("Pulso - Mínimo", Range(0, 4)) = 0
        _PulseMax ("Pulso - Máximo", Range(0, 4)) = 0
        _PulseSpeed ("Pulso - Veces por segundo", Range(0, 10)) = 1.5
        _PulseSharpness ("Pulso - Parpadeo seco (0 = suave)", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _TextureDetail;
            float  _BlurMip;
            float4 _ShadowColor;
            float  _ShadowThreshold;
            float  _ShadowSoftness;
            float  _AmbientStrength;
            float4 _LineColor;
            float  _LineWidth;
            float  _LineStrength;
            float4 _SpecColor;
            float  _SpecSize;
            float  _SpecSoftness;
            float  _SpecStrength;
            float  _SpecInShadow;
            float4 _RimColor;
            float  _RimWidth;
            float  _RimSoftness;
            float  _RimStrength;
            float  _RimLitSide;
            float4 _PulseColor;
            float  _PulseMin;
            float  _PulseMax;
            float  _PulseSpeed;
            float  _PulseSharpness;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- color
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fog        : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // --- albedo (igual que ToonLit) ---
                float3 texSharp = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float3 texBlur  = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, i.uv, _BlurMip).rgb;
                float3 albedo   = lerp(texBlur, texSharp, _TextureDetail) * _BaseColor.rgb;

                float3 n    = normalize(i.normalWS);
                float3 v    = normalize(GetWorldSpaceViewDir(i.positionWS));
                float  soft = max(_ShadowSoftness, 0.001);

                // --- luz y sombra (igual que ToonLit) ---
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS + n * 0.05);
                Light mainLight    = GetMainLight(shadowCoord, i.positionWS, half4(1, 1, 1, 1));
                float3 lightDir    = mainLight.direction;
                float  facing      = saturate(dot(n, lightDir) * 4.0);
                float  rawShadow   = lerp(1.0, mainLight.shadowAttenuation, facing);
                float  shadowAtt   = smoothstep(0.5 - soft, 0.5 + soft, rawShadow);
                float3 ambient     = SampleSH(n);

                float NdotL = dot(n, lightDir);
                float lightAmount = smoothstep(_ShadowThreshold - soft, _ShadowThreshold + soft, NdotL) * shadowAtt;

                float3 litCol    = albedo * mainLight.color;
                float3 shadowCol = albedo * (_ShadowColor.rgb + ambient * _AmbientStrength);
                float3 col = lerp(shadowCol, litCol, lightAmount);

                float w = max(_LineWidth, 0.0001);
                float lineMask = (1.0 - smoothstep(w * 0.5, w, abs(NdotL - _ShadowThreshold))) * _LineStrength * step(0.0001, _LineWidth);
                col = lerp(col, albedo * _LineColor.rgb, saturate(lineMask));

                // --- rim: borde de luz finito ---
                float rim = 1.0 - saturate(dot(n, v));
                float rimEdge = 1.0 - _RimWidth;
                float rimMask = smoothstep(rimEdge - _RimSoftness, rimEdge + _RimSoftness, rim);
                rimMask *= lerp(1.0, saturate(NdotL * 2.0 + 0.5), _RimLitSide);
                col = lerp(col, _RimColor.rgb * mainLight.color, saturate(rimMask * _RimStrength));

                // --- brillo toon: mancha de borde duro ---
                float3 h = normalize(lightDir + v);
                float NdotH = saturate(dot(n, h));
                float specEdge = 1.0 - _SpecSize * _SpecSize; // tamaño más fácil de ajustar
                float spec = smoothstep(specEdge - _SpecSoftness, specEdge + _SpecSoftness, NdotH);
                spec *= step(0.0, NdotL) * lerp(shadowAtt, 1.0, _SpecInShadow);
                col = lerp(col, _SpecColor.rgb * mainLight.color, saturate(spec * _SpecStrength));

                // --- pulso de emisión ---
                float wave = sin(_Time.y * _PulseSpeed * 6.28318) * 0.5 + 0.5;
                float sharp = saturate(_PulseSharpness);
                wave = lerp(wave, smoothstep(0.5 - (1.0 - sharp) * 0.5, 0.5 + (1.0 - sharp) * 0.5 + 0.001, wave), sharp);
                col += _PulseColor.rgb * lerp(_PulseMin, _PulseMax, wave);

                col = MixFog(col, i.fog);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- proyecta sombra
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert (Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag () : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- profundidad
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert (Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half frag () : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- normales (outline / SSAO)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n);
                float2 remapped = saturate(oct * 0.5 + 0.5);
                return half4(PackFloat2To888(remapped), 0.0);
            #else
                return half4(n, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
