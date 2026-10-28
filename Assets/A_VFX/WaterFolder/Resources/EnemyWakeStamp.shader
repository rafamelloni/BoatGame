// Lo usa EnemyWaterWakeManager para "pintar" las estelas de los enemigos en una textura
// vista desde arriba que sigue al jugador. Cada frame:
//  1) desplaza lo que había (si el área se movió) y le resta vida a cada píxel,
//  2) estampa los tramos nuevos (cápsulas entre la posición anterior y la actual de cada enemigo).
// Canales de la textura: r = vida restante (1 -> 0), g = fuerza, b = velocidad de apagado (1 / segundos), a = sin uso.
// Está en una carpeta Resources para que entre en el build.
Shader "Hidden/Botes/EnemyWakeStamp"
{
    Properties
    {
        _MainTex ("", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define MAX_STAMPS 64   // tiene que coincidir con EnemyWaterWakeManager.MaxStampsPerPass

            sampler2D _MainTex;
            float4 _WakeArea;      // xy = centro XZ actual, z = tamaño en metros
            float4 _WakePrevArea;  // xy = centro XZ del frame anterior, z = tamaño
            float  _WakeDeltaTime;
            float  _StampCount;
            float4 _StampA[MAX_STAMPS]; // xy = punto anterior XZ, zw = punto actual XZ
            float4 _StampB[MAX_STAMPS]; // x = radio, y = 1 / vida, z = fuerza, w = forma del afinado
            float4 _StampC[MAX_STAMPS]; // x = hold

            float4 frag (v2f_img i) : SV_Target
            {
                float2 world = _WakeArea.xy + (i.uv - 0.5) * _WakeArea.z;

                // lo que había, desplazado si el área se movió
                float2 prevUV = (world - _WakePrevArea.xy) / _WakePrevArea.z + 0.5;
                float4 o = 0;
                if (prevUV.x >= 0.0 && prevUV.y >= 0.0 && prevUV.x <= 1.0 && prevUV.y <= 1.0)
                    o = tex2Dlod(_MainTex, float4(prevUV, 0, 0));

                // se va apagando
                o.r -= _WakeDeltaTime * o.b;
                if (o.r <= 0.0) o = 0;

                // tramos nuevos
                int count = (int)_StampCount;
                for (int k = 0; k < MAX_STAMPS; k++)
                {
                    if (k >= count) break;
                    float4 a = _StampA[k];
                    float4 b = _StampB[k];
                    float2 seg = a.zw - a.xy;
                    float len2 = dot(seg, seg);
                    float t = len2 > 1e-6 ? saturate(dot(world - a.xy, seg) / len2) : 1.0;
                    float d = distance(world, a.xy + seg * t);
                    if (d >= b.x) continue;

                    // los bordes viven menos que el centro: con el tiempo la estela se afina sola hasta desaparecer
                    float q = d / b.x;
                    float hold = _StampC[k].x;
                    float life = 1.0 - (1.0 - hold) * pow(q, b.w);
                    if (life > o.r) o = float4(life, b.z, b.y, 0.0);
                }
                return o;
            }
            ENDCG
        }
    }
}
