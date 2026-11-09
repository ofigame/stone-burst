// Kat Kat katman yüzeyi. Statik yığınlar ve soyulma ağı aynı shader'ı kullanır;
// renk, işaret ve yüz bilgisi köşe verisinden gelir, böylece tek malzeme bütün renklere yeter.
//   COLOR        : ana renk (a = opaklık)
//   TEXCOORD0    : maske/işaret UV
//   TEXCOORD1.x  : ön yüz oranı (1 ön yüz, 0 alt yüz), .y ışık çarpanı
//   TEXCOORD2.xy : işaret atlası hücresi (0/1, 0/1), .z işaret gücü, .w yüzey türü (0 üst yüz, 1 yan, 2 düz)
// _MainTex: yuvarlatılmış kare maskesi (a = şekil, r = yastık tonu, g = parlama lekesi, b = sol üst ışıltı)
// _IconTex: 2x2 kabartma işaret atlası (r = koyu oyuk, g = açık vurgu)
// Işık sol üstten; siyah dış çizgi yok. Premium-v2 rehberi §7: ana renk + kenar açılması + yumuşak parlama + alt kenar koyulaşması.
Shader "KatKat/Layer"
{
    Properties
    {
        _MainTex ("Shape Mask", 2D) = "white" {}
        _IconTex ("Icon Atlas", 2D) = "black" {}
        _FaceTex ("Colour Stone Atlas (v6)", 2D) = "black" {}
        _UseFace ("Use Colour Stones", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
        _Cutoff ("Cutoff", Range(0,1)) = 0.01
        _Gloss ("Gloss", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        ZWrite [_ZWrite]
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _IconTex;
            sampler2D _FaceTex;
            float _UseFace;
            float _Cutoff;
            float _Gloss;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 face : TEXCOORD1;
                float4 icon : TEXCOORD2;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 face : TEXCOORD1;
                float4 icon : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.face = v.face;
                o.icon = v.icon;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.uv);
                fixed3 baseCol = i.color.rgb;
                float kind = i.icon.w;

                // v6 renkli taşlar: üst yüz doğrudan atlastaki çizimdir (işaret çizime işli, 8 px pay: 496/512).
                // Soyulurken görünen alt yüz eski maskeyle çizilir; ikisi arasında face.x ile geçilir.
                if (_UseFace > 0.5 && kind < 0.5)
                {
                    fixed4 art = tex2D(_FaceTex, (i.icon.xy + 0.015625 + i.uv * 0.96875) * 0.5);
                    fixed3 backV6 = lerp(baseCol, fixed3(1, 0.97, 0.94), 0.34) * (0.86 + 0.16 * mask.r);
                    float f = saturate(i.face.x);
                    float a = lerp(mask.a, art.a, f);
                    clip(a - _Cutoff);
                    return fixed4(lerp(backV6, art.rgb, f) * i.face.y, a * i.color.a);
                }
                clip(mask.a - _Cutoff);

                if (kind > 1.5)
                    return fixed4(baseCol * i.face.y, mask.a * i.color.a);

                if (kind > 0.5)
                {
                    // Katman yanı: görünen alt hilal. Alta doğru koyulaşır, üst yüzün hemen altı temas gölgesi.
                    float lower = smoothstep(0.0, 0.11, i.uv.y);
                    // v10: koyulaşma azaltıldı (0.70–0.93 → 0.84–1.0): alt katman rengi okunur kalsın.
                    fixed3 side = baseCol * lerp(0.84, 1.0, lower) * lerp(0.90, 1.0, mask.r);
                    side += (1 - side) * 0.10 * mask.b;
                    return fixed4(side * i.face.y, mask.a * i.color.a);
                }

                // Üst yüz: yastık tonu, kabartma işaret, sol üstte yumuşak ışıltı ve küçük parlama lekesi.
                fixed3 front = baseCol * (0.58 + 0.47 * mask.r);
                fixed4 icon = tex2D(_IconTex, (i.icon.xy + i.uv) * 0.5);
                fixed3 groove = i.icon.x < 0.5 && i.icon.y > 0.5 ? fixed3(0.43, 0.38, 0.34) : baseCol * 0.64;
                front = lerp(front, groove, icon.r * i.icon.z);
                front = lerp(front, front + 0.18, icon.g * i.icon.z);
                front += (1 - front) * 0.3 * mask.b;
                front += mask.g * (_Gloss * 0.19);
                float2 grainCell = floor(i.uv * 148.0);
                float grain = frac(52.9829189 * frac(dot(grainCell, float2(0.06711056, 0.00583715))));
                float grainVisibility = 1.0 - smoothstep(0.004, 0.013, max(fwidth(i.uv.x), fwidth(i.uv.y)));
                front += (grain - 0.5) * 0.055 * grainVisibility;

                // Alt yüz (soyulurken görünen): aynı rengin açık, mat tonu; işaret yok.
                fixed3 back = lerp(baseCol, fixed3(1, 0.97, 0.94), 0.34) * (0.86 + 0.16 * mask.r);

                fixed3 col = lerp(back, front, saturate(i.face.x)) * i.face.y;
                return fixed4(col, mask.a * i.color.a);
            }
            ENDCG
        }
    }
}
