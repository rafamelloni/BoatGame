// Indicador de dash: franja finita sobre el agua que se llena hacia adelante,
// con chevrones que corren en la dirección del dash. Procedural, sin texturas.
// UV.x = ancho (0..1), UV.y = largo (0 = enemigo, 1 = punto final).
// Lo usa SpikeDashTelegraph. Está en Resources para que entre en el build.
Shader "Botes/DashTelegraph"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.35, 0.25, 1)
        _Progress ("Progress (carga)", Range(0,1)) = 0
        _Alpha ("Alpha", Range(0,1)) = 1
        _Flash ("Flash", Range(0,1)) = 0
        _Length ("Largo (m)", Float) = 20
        _Width ("Ancho (m)", Float) = 2
        _EdgeWidth ("Grosor borde (m)", Float) = 0.12
        _EdgeAlpha ("Alpha borde", Range(0,1)) = 0.55
        _FillAlpha ("Alpha relleno", Range(0,1)) = 0.12
        _ChevronAlpha ("Alpha chevrones", Range(0,1)) = 0.45
        _ChevronSpacing ("Separación chevrones (m)", Float) = 2.2
        _ChevronSpeed ("Velocidad chevrones (m/s)", Float) = 7
        _EndMarker ("Marca final doble (long dash)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float _Progress, _Alpha, _Flash, _Length, _Width, _EdgeWidth, _EdgeAlpha, _FillAlpha;
            float _ChevronAlpha, _ChevronSpacing, _ChevronSpeed, _EndMarker;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float along = i.uv.y * _Length;                 // metros desde el enemigo
                float across = (i.uv.x - 0.5) * _Width;         // metros desde el centro
                float ax = abs(across);
                float halfW = _Width * 0.5;

                // bordes laterales: siempre (dim), marcan el camino completo
                float edge = step(halfW - _EdgeWidth, ax);

                // barra final (y doble para long dash)
                float toEnd = _Length - along;
                float endBar = step(toEnd, _EdgeWidth * 1.5);
                float endBar2 = _EndMarker * step(abs(toEnd - _EdgeWidth * 5.0), _EdgeWidth * 0.75);

                // relleno que avanza con la carga
                float filled = step(i.uv.y, _Progress);

                // chevrones (›) que corren hacia adelante, solo en la parte llena
                float c = frac((along - ax * 0.8) / _ChevronSpacing - _Time.y * _ChevronSpeed / _ChevronSpacing);
                float chevron = step(c, 0.16) * step(ax, halfW - _EdgeWidth * 2.0);

                float a = edge * _EdgeAlpha
                        + (endBar + endBar2) * _EdgeAlpha
                        + filled * (_FillAlpha + chevron * _ChevronAlpha);

                // aparece desde el enemigo (no arranca cortado encima del modelo)
                float startFade = saturate(along / 1.5);

                a = saturate(a) * startFade * _Alpha;
                if (a <= 0.001) discard;
                float3 rgb = lerp(_Color.rgb, float3(1,1,1), _Flash);
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
