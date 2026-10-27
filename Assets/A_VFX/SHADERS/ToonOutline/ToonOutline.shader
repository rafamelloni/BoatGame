// Outline por objeto (inverted hull): dibuja el modelo inflado, solo las caras de atrás, de un color plano.
// El grosor es en PÍXELES de pantalla (se ve igual de grueso cerca o lejos).
// Se usa como material extra del Renderer (lo agrega el componente ToonOutline).
Shader "Botes/ToonOutline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (0.16, 0.08, 0.06, 1)
        _OutlineWidth ("Grosor (píxeles)", Range(0, 12)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  fogFactor  : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 nCS = TransformWorldToHClipDir(nWS);

                // empuja el vértice en pantalla en la dirección de la normal, en píxeles
                float2 dir = nCS.xy;
                float len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(0, 0);
                float2 pixel = 2.0 / _ScreenParams.xy;
                posCS.xy += dir * pixel * _OutlineWidth * posCS.w;

                OUT.positionCS = posCS;
                OUT.fogFactor = ComputeFogFactor(posCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half3 col = MixFog(_OutlineColor.rgb, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
