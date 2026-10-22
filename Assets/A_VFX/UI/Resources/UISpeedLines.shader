// Líneas de velocidad estilo anime + viñeta, procedurales (sin textura).
// Lo usa SprintScreenFX sobre un RawImage a pantalla completa.
// Está en Resources para que entre en el build (WebGL).
Shader "Botes/UI SpeedLines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 2)) = 0
        _Aspect ("Aspect (w/h)", Float) = 1.777
        _LineColor ("Line Color", Color) = (1,1,1,0.85)
        _LineCount ("Line Count", Float) = 90
        _Density ("Density", Range(0,1)) = 0.35
        _Flicker ("Flicker (cambios por seg)", Float) = 12
        _InnerRadius ("Inner Radius", Float) = 0.38
        _LineWidth ("Line Width", Range(0.05, 1)) = 0.35
        _VignetteColor ("Vignette Color", Color) = (0.02, 0.08, 0.12, 1)
        _VignetteStrength ("Vignette Strength", Range(0,1)) = 0.35
        _VignetteInner ("Vignette Inner", Float) = 0.35
        _VignetteOuter ("Vignette Outer", Float) = 0.95
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            float _Intensity, _Aspect, _LineCount, _Density, _Flicker, _InnerRadius, _LineWidth;
            float _VignetteStrength, _VignetteInner, _VignetteOuter;
            fixed4 _LineColor, _VignetteColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash11(float x) { return frac(sin(x * 12.9898) * 43758.5453); }

            fixed4 frag (v2f i) : SV_Target
            {
                float k = saturate(_Intensity);
                if (_Intensity <= 0.001) return fixed4(0,0,0,0);

                float2 p = (i.uv - 0.5) * float2(_Aspect, 1.0) * 2.0;   // centro = 0, alto = [-1,1]
                float r = length(p);
                float a01 = atan2(p.y, p.x) / 6.2831853 + 0.5;

                float cell = floor(a01 * _LineCount);
                float stepT = floor(_Time.y * _Flicker);                 // "hierve" N veces por segundo (toon)
                float rnd  = hash11(cell * 1.731 + stepT * 13.17);
                float rnd2 = hash11(cell * 7.113 + stepT * 3.71 + 11.0);

                // cuántas líneas hay prendidas (más intensidad = más líneas)
                float on = step(1.0 - _Density * saturate(_Intensity), rnd);

                // cada línea arranca a una distancia distinta del centro
                float inner = _InnerRadius + rnd2 * 0.35 + (1.0 - k) * 0.25;
                float taper = saturate((r - inner) / 0.35);              // finita adentro, gruesa afuera (aguja)
                float f = frac(a01 * _LineCount);
                float w = _LineWidth * lerp(0.4, 1.0, rnd2) * taper;
                float lineMask = step(abs(f - 0.5), w * 0.5) * step(inner, r);

                float la = on * lineMask * _LineColor.a * saturate(_Intensity * 1.2);
                float va = smoothstep(_VignetteInner, _VignetteOuter, r) * _VignetteStrength * k;

                float alpha = la + va * (1.0 - la);
                if (alpha <= 0.0001) return fixed4(0,0,0,0);
                float3 rgb = (_LineColor.rgb * la + _VignetteColor.rgb * va * (1.0 - la)) / alpha;
                return fixed4(rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
