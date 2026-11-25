// Kat Kat tepsi suyu (2026-11-25, kullanıcı: "tepsideki suyu daha doğal yapalım. tepsinin köşelerinde su çizgi gibi duruyor. Tepsiyi
// köşelerden tam kapasın. ekranı çevirdiğimiz yön doğrultusunda su eğim alsın").
//   _MainTex : tepsinin iç alan maskesi (a; TrayArt.LastWaterMask) — su kenarın iç çizgisine, yuvarlak köşelere tam oturur.
//   Yüzey (dünya y) = _Level + _Tilt·(x − _CenterX) + iki sinüs dalga (_Amp) + en çok 4 sıçrama halkası (_Rip0..3: x, yaş, genlik).
//   Altı saydam mavi (derinleştikçe koyu), ışık oyunu (kostik), yüzeyde köpük bandı ve beyaz çizgi. Taşların arkasında çizilir.
Shader "KatKat/TrayWater"
{
    Properties
    {
        _MainTex ("Water Mask", 2D) = "white" {}
        _Level ("Level (world y)", Float) = 0
        _CenterX ("Center X (world)", Float) = 0
        _Tilt ("Tilt (dy/dx)", Float) = 0
        _Amp ("Wave Amp (world)", Float) = 0.02
        _Pitch ("Cell Pitch (world)", Float) = 1
        _WTime ("Time", Float) = 0
        _Span ("Bottom..Top span (world)", Float) = 5
        _Bottom ("Bottom (world y)", Float) = -3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Level, _CenterX, _Tilt, _Amp, _Pitch, _WTime, _Span, _Bottom;
            float4 _Rip0, _Rip1, _Rip2, _Rip3;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = v.color;
                return o;
            }

            float Ripple(float4 r, float x)
            {
                if (r.z <= 0.0) return 0.0;
                float d = abs(x - r.x);
                float front = r.y * _Pitch * 3.2;
                float env = exp(-r.y * 1.8) * saturate(1.0 - (d - front) / _Pitch);
                return r.z * _Pitch * env * cos((d - front) / _Pitch * 5.0);
            }

            float Surface(float x)
            {
                float k = 1.0 / max(_Pitch, 1e-4);
                float y = _Level + _Tilt * (x - _CenterX);
                y += _Amp * (0.6 * sin(x * k * 2.1 + _WTime * 2.4) + 0.4 * sin(x * k * 4.7 - _WTime * 3.3));
                y += Ripple(_Rip0, x) + Ripple(_Rip1, x) + Ripple(_Rip2, x) + Ripple(_Rip3, x);
                return y;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float mask = tex2D(_MainTex, i.uv).a;
                if (mask <= 0.002) discard;
                float surf = Surface(i.world.x);
                float below = surf - i.world.y; // + suyun içinde
                float aa = max(fwidth(i.world.y), 1e-5) * 1.2;
                float inWater = saturate(below / aa + 0.5);
                if (inWater <= 0.0) discard;
                float k = 1.0 / max(_Pitch, 1e-4);
                float depth = saturate(below / max(_Span * 0.6, 1e-4));
                float4 top = float4(0.30, 0.76, 1.0, 0.80);
                float4 deep = float4(0.04, 0.30, 0.78, 0.93);
                float4 c = lerp(top, deep, depth);
                // Işık oyunu: kesişen iki dalganın parlak düğümleri (yavaş kayar).
                float2 p = i.world * k;
                float ca = max(0.0, sin(p.x * 3.1 + p.y * 1.7 + _WTime * 1.9) * sin(p.x * 1.3 - p.y * 2.9 - _WTime * 1.4));
                c.rgb = lerp(c.rgb, float3(0.85, 0.98, 1.0), ca * ca * 0.45);
                // Yüzey: köpük bandı ve beyaz çizgi.
                float band = _Pitch * 0.08;
                float foam = saturate(1.0 - below / band);
                c.rgb = lerp(c.rgb, float3(0.78, 0.96, 1.0), foam * 0.6);
                float edgeLine = saturate(1.0 - abs(below - _Pitch * 0.012) / (_Pitch * 0.018));
                c.rgb = lerp(c.rgb, float3(1.0, 1.0, 1.0), edgeLine * 0.9);
                c.a = max(c.a, edgeLine * 0.95) * inWater * mask * i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
