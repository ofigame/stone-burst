// Yolculuk haritası hareketi (2026-11-19; kullanıcı: "yolculuk görselimizdeki su ağaç çicek gibi objelere hareket katabilir
// miyiz" → "evet bahçeden başla"). Görsel aynı kalır; maske dokusu (R su, G yaprak, B çiçek; tools/map_motion_masks.ps1)
// bölgeleri seçer: yapraklarda ara ara yanıp sönen ışık benekleri (salınım 2026-11-19 kaldırıldı: "göz yoruyor"), su dalgalanır, parıltı ve aşağı akan
// çizgiler taşır. Zaman _KatKatMapTime (MapMotion.cs her karede yazar; video kaydı kendi saatini verir).
// UI/Default gibi: köşe rengi, şablon (Mask) ve RectMask2D kırpma desteklenir.
Shader "KatKat/MapMotion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Motion ("Motion mask", 2D) = "black" {}
        _Strength ("Strength", Float) = 1
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            sampler2D _MainTex; float4 _MainTex_TexelSize;
            sampler2D _Motion;
            float _Strength;
            fixed4 _Color;
            float4 _ClipRect;
            float _KatKatMapTime;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _KatKatMapTime;
                float2 uv = i.uv;
                float2 px = _MainTex_TexelSize.xy;
                fixed3 m = tex2D(_Motion, uv).rgb * _Strength;

                // 2026-11-19 (kullanıcı: "yeşil alanlardaki zemin dalgalaması göz yoruyor. Ağaç yapraklarına parıltı koymamız
                // yeterli olur"): yaprak ve çiçek bölgeleri artık kaymıyor; yalnız su kırılır.
                float2 off = float2(sin(uv.y * 160.0 + t * 2.0), cos(uv.x * 130.0 + t * 1.6)) * 1.4 * px * m.r;

                fixed4 c = tex2D(_MainTex, uv + off);

                // Yaprak parıltısı: taç üstünde 28 px'lik hücrelerde, her biri kendi evresinde ara ara yanıp sönen küçük
                // sıcak ışık beneği (yapraklardan süzülen güneş). Çoğu zaman sönük; aynı anda hücrelerin ~%10'u parlar.
                float2 cellUv = uv / (px * 28.0);
                float2 cell = floor(cellUv);
                float2 f = frac(cellUv) - 0.5;
                float h1 = frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.55);
                float h2 = frac(sin(dot(cell, float2(269.5, 183.3))) * 43758.55);
                float2 c0 = (float2(h1, h2) - 0.5) * 0.6;
                float d = length(f - c0);
                float blink = pow(saturate(sin(t * (0.9 + h1 * 0.8) + h2 * 6.2832)), 7.0);
                float2 q = f - c0;
                float core = saturate(1.0 - d * 7.0);
                float halo = saturate(1.0 - d * 2.6); halo *= halo;
                // Dört kollu yıldız: ince yatay ve dikey ışın.
                float rays = saturate(1.0 - abs(q.y) * 28.0) * saturate(1.0 - abs(q.x) * 2.4) + saturate(1.0 - abs(q.x) * 28.0) * saturate(1.0 - abs(q.y) * 2.4);
                float speck = blink * (core * 1.2 + halo * 0.35 + rays * 0.8);
                // Açık taçta da seçilsin: ekleme yerine renge doğru beyazlatma (screen benzeri).
                float g = saturate(speck * smoothstep(0.35, 0.8, m.g));
                c.rgb = lerp(c.rgb, fixed3(1.0, 0.98, 0.88), g);

                // Su parıltısı: kesişen iki dalga deseninin tepeleri (kısa, beyaz pırıltılar).
                float s1 = sin(uv.x * 260.0 + t * 1.4 + sin(uv.y * 80.0 + t * 0.6) * 2.5);
                float s2 = sin(uv.y * 220.0 - t * 1.9 + sin(uv.x * 60.0 - t * 0.5) * 2.0);
                float sparkle = pow(saturate(s1 * s2), 10.0);
                // Akış: aşağı kayan yumuşak çizgiler (dere ve şelale).
                float flow = smoothstep(0.75, 1.0, sin(uv.y * 210.0 + t * 3.2 + sin(uv.x * 45.0) * 3.0) * 0.5 + 0.5);
                c.rgb += (sparkle * 0.45 + flow * 0.10) * m.r;

                c *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
