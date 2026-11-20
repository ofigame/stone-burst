// Bölge simgesi hareketi (2026-11-20; kullanıcı: "yeşil vadi objesine su akma efekti olsun, kum vadisinde de ağaç yapraklarına
// hareket katalım ve su hareketli olsun"). Maske (tools/region_icon_masks.ps1): R su (kırılma, parıltı, aşağı akış), G yaprak
// (rüzgârda salınım). Zaman _KatKatMapTime (MapMotion.Tick). UI/Default gibi köşe rengi, şablon ve RectMask2D kırpma.
Shader "KatKat/RegionIconMotion"
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
                // Yaprak salınımı (G): rüzgâr esintisi, tepeye doğru daha çok; iki frekans karışımı doğal görünür.
                float gust = sin(t * 1.3) * 0.6 + sin(t * 2.9 + uv.x * 3.0) * 0.4;
                float2 sway = float2(gust * 9.0, sin(t * 2.1 + uv.x * 5.0) * 2.5) * px * m.g;
                // Su (R): kırılma (dalgalı örnekleme).
                float2 wob = float2(sin(uv.y * 140.0 + t * 3.0), cos(uv.x * 120.0 + t * 2.4)) * 2.2 * px * m.r;
                fixed4 c = tex2D(_MainTex, uv + sway + wob);
                // Su parıltısı ve akış: aşağı kayan parlak çizgiler (çeşme, dere) ve kesişen dalga tepeleri.
                float s1 = sin(uv.x * 180.0 + t * 1.8 + sin(uv.y * 60.0 + t * 0.7) * 2.5);
                float s2 = sin(uv.y * 160.0 - t * 2.3 + sin(uv.x * 45.0 - t * 0.6) * 2.0);
                float sparkle = pow(saturate(s1 * s2), 8.0);
                float flow = smoothstep(0.7, 1.0, sin(uv.y * 90.0 + t * 6.0 + sin(uv.x * 30.0) * 2.0) * 0.5 + 0.5);
                c.rgb += (sparkle * 0.6 + flow * 0.22) * m.r;
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
