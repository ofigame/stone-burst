using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// İçi boş tahtaların süsü (kullanıcı: "tahtanın ortası boş olsun, orada maskotumuz olsun; kimi bölümde fenerimiz,
    /// kimi bölümde kaya olsun; animasyonlu olsunlar"). Tahta şeklinin içinde kalan (kenara değmeyen) boş hücre
    /// bölgeleri bulunur; her birine bölge kutusuna sığan, hareketli bir süs konur:
    ///   maskot (zıplar, ezilip uzar) · fener (hafif salınır, ışık huzmesi döner) · kaya kemeri/vaha (sallanır, parıltı)
    ///   · çeşme (su damlaları sıçrar). Bölüm numarası ve tema seçer; iki boşlukta biri maskot, biri tema süsü.
    /// Süsler oyun kurallarını etkilemez; dokunuşu engellemez (yalnızca görsel).
    /// </summary>
    public sealed class BoardDecor : MonoBehaviour
    {
        public enum Kind { Mascot, Lighthouse, Rock, Fountain }

        private sealed class Item
        {
            public Kind Kind;
            public Transform Root;
            public SpriteRenderer Main, Shadow, Beam;
            public readonly List<SpriteRenderer> Bits = new List<SpriteRenderer>();
            public Vector3 Home;
            public float Size, Phase;
        }

        private readonly List<Item> _items = new List<Item>();
        private float _clock;

        public int Count => _items.Count;
        public Kind KindAt(int i) => _items[i].Kind;

        /// <summary>Şekle ait olmayan ve tahta kutusunun kenarına bağlanmayan boş hücre bölgeleri (4 komşuluk).</summary>
        public static List<List<Vector2Int>> InteriorHoles(bool[] inShape, int w, int h)
        {
            var result = new List<List<Vector2Int>>();
            if (inShape == null || inShape.Length < w * h) return result;
            var outside = new bool[w * h];
            var q = new Queue<int>();
            for (int x = 0; x < w; x++) { Seed(x, 0); Seed(x, h - 1); }
            for (int y = 0; y < h; y++) { Seed(0, y); Seed(w - 1, y); }
            void Seed(int x, int y)
            {
                int i = y * w + x;
                if (!inShape[i] && !outside[i]) { outside[i] = true; q.Enqueue(i); }
            }
            while (q.Count > 0)
            {
                int i = q.Dequeue(), x = i % w, y = i / w;
                if (x > 0) Seed(x - 1, y);
                if (x < w - 1) Seed(x + 1, y);
                if (y > 0) Seed(x, y - 1);
                if (y < h - 1) Seed(x, y + 1);
            }
            var seen = new bool[w * h];
            for (int i = 0; i < w * h; i++)
            {
                if (inShape[i] || outside[i] || seen[i]) continue;
                var comp = new List<Vector2Int>();
                var st = new Stack<int>();
                st.Push(i); seen[i] = true;
                while (st.Count > 0)
                {
                    int j = st.Pop(), x = j % w, y = j / w;
                    comp.Add(new Vector2Int(x, y));
                    foreach (var n in new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) })
                    {
                        if (n.Item1 < 0 || n.Item2 < 0 || n.Item1 >= w || n.Item2 >= h) continue;
                        int k = n.Item2 * w + n.Item1;
                        if (inShape[k] || outside[k] || seen[k]) continue;
                        seen[k] = true; st.Push(k);
                    }
                }
                result.Add(comp);
            }
            return result;
        }

        /// <summary>Boş bölgelere süs yerleştirir. <paramref name="cellRect"/>: hücre (x,y) → dünya (yerel) dikdörtgeni.</summary>
        public void Rebuild(bool[] inShape, int w, int h, System.Func<int, int, Rect> cellRect, int levelIndex, string themeId, int sortingBase)
        {
            Clear();
            var holes = InteriorHoles(inShape, w, h);
            for (int k = 0; k < holes.Count; k++)
            {
                var comp = holes[k];
                if (comp.Count < 2) continue; // tek hücrelik oyuk süssüz (çok küçük)
                Rect box = cellRect(comp[0].x, comp[0].y);
                int maxY = 0;
                foreach (var c in comp)
                {
                    var r = cellRect(c.x, c.y);
                    box = Rect.MinMaxRect(Mathf.Min(box.xMin, r.xMin), Mathf.Min(box.yMin, r.yMin), Mathf.Max(box.xMax, r.xMax), Mathf.Max(box.yMax, r.yMax));
                    maxY = Mathf.Max(maxY, c.y);
                }
                var kind = Choose(levelIndex, k, holes.Count, themeId);
                _items.Add(Make(kind, box, sortingBase + maxY * 4 + 2, levelIndex * 7 + k * 3));
            }
            Tick(0f);
        }

        private static Kind Choose(int level, int hole, int holes, string themeId)
        {
            Kind themed = themeId == "garden" ? Kind.Fountain : themeId == "desert" ? Kind.Rock : Kind.Lighthouse;
            // Tema dışında da çeşitlilik: her 3. bölümde diğer tema süsü.
            if (level % 6 == 3) themed = themed == Kind.Lighthouse ? Kind.Rock : Kind.Lighthouse;
            if (holes >= 2) return hole % 2 == 0 ? Kind.Mascot : themed;
            return (level / 2) % 2 == 0 ? Kind.Mascot : themed;
        }

        private Item Make(Kind kind, Rect box, int order, int seed)
        {
            var it = new Item { Kind = kind, Phase = (seed % 97) * 0.13f };
            var root = new GameObject("Decor_" + kind).transform;
            root.SetParent(transform, false);
            it.Root = root;
            Sprite sprite = kind switch
            {
                Kind.Mascot => PremiumArt.V4("mascot-crystal/mascot-full-v1"),
                Kind.Lighthouse => SpecialArt.V6("ui/region-coast"),
                Kind.Rock => SpecialArt.V6("ui/region-desert"),
                _ => SpecialArt.V6("ui/region-garden"),
            };
            // Kutuya sığdır (taşlara değmesin: %88); taban kutunun altına yakın, gölge altında.
            float fit = 0.88f;
            float sw = sprite != null ? sprite.bounds.size.x : 1f, sh = sprite != null ? sprite.bounds.size.y : 1f;
            float scale = Mathf.Min(box.width * fit / sw, box.height * fit / sh);
            it.Size = Mathf.Min(box.width, box.height);
            float bottom = box.yMin + box.height * (1f - fit) * 0.5f;
            it.Home = new Vector3(box.center.x, bottom, 0f);
            root.localPosition = it.Home;

            it.Shadow = Child(root, "Shadow", SpecialArt.Disc, new Color(0f, 0f, 0f, 0.22f), order - 1);
            it.Shadow.transform.localPosition = new Vector3(0f, box.height * 0.02f, 0f);
            float shadowW = sw * scale * 0.8f;
            it.Shadow.transform.localScale = new Vector3(shadowW / SpecialArt.Disc.bounds.size.x, shadowW * 0.22f / SpecialArt.Disc.bounds.size.y, 1f);

            // Ana görsel: pivot alt-orta gibi davranması için gövde nesnesi kaydırılır.
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            it.Main = Child(body, "Sprite", sprite, Color.white, order);
            it.Main.transform.localScale = new Vector3(scale, scale, 1f);
            float pivotY = sprite != null ? sprite.bounds.center.y * scale : 0f;
            it.Main.transform.localPosition = new Vector3(0f, sh * scale * 0.5f - pivotY, 0f);

            if (kind == Kind.Lighthouse)
            {
                // Lamba ~ görselin üst %74'ünde; yatay uzatılmış ışıma, genişliği salınır → dönen huzme.
                it.Beam = Child(body, "Beam", SpecialArt.Glow, new Color(1f, 0.93f, 0.55f, 0.7f), order + 1);
                it.Beam.transform.localPosition = new Vector3(0f, sh * scale * 0.74f, 0f);
            }
            if (kind == Kind.Fountain || kind == Kind.Rock || kind == Kind.Mascot)
            {
                int n = kind == Kind.Fountain ? 8 : 4;
                for (int i = 0; i < n; i++)
                {
                    var c = kind == Kind.Fountain ? new Color(0.62f, 0.9f, 1f, 0.95f) : new Color(1f, 0.97f, 0.8f, 0.9f);
                    var bit = Child(body, "Bit" + i, SpecialArt.Disc, c, order + 1);
                    it.Bits.Add(bit);
                }
            }
            it.Size = sh * scale;
            it.Main.transform.parent.localScale = Vector3.one;
            return it;
        }

        private static SpriteRenderer Child(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// vr1 P1: buzlu koleksiyon taşının yerleştiği boşluktaki süsü gizler (yerel konum, yarıçap). Hedef taş yoksa
        /// çağrılmaz; süs yerinde kalır (03 "Hedef kalmadıysa mevcut merkez dekoruna dön").
        /// </summary>
        public int HideNear(Vector3 local, float radius)
        {
            int n = 0;
            foreach (var it in _items)
            {
                if (it.Root == null) continue;
                var d = (Vector2)(it.Home - local);
                if (d.magnitude > radius) continue;
                it.Root.gameObject.SetActive(false);
                n++;
            }
            return n;
        }

        public void Clear()
        {
            foreach (var it in _items) if (it.Root != null) { if (Application.isPlaying) Destroy(it.Root.gameObject); else DestroyImmediate(it.Root.gameObject); }
            _items.Clear();
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            _clock += dt;
            foreach (var it in _items)
            {
                if (it.Root == null) continue;
                float t = _clock + it.Phase;
                var body = it.Main.transform.parent;
                float s = it.Size;
                switch (it.Kind)
                {
                    case Kind.Mascot:
                    {
                        // 1,2 s zıplama: yere değerken ezilir, havada uzar; her 3. zıplama yüksek ve bir dönüş.
                        const float hop = 1.2f;
                        float u = t / hop, ph = u - Mathf.Floor(u);
                        bool big = Mathf.FloorToInt(u) % 3 == 2;
                        float air = Mathf.Sin(ph * Mathf.PI);
                        float hgt = air * s * (big ? 0.22f : 0.12f);
                        float land = ph < 0.1f ? 1f - ph / 0.1f : ph > 0.92f ? (ph - 0.92f) / 0.08f : 0f;
                        body.localPosition = new Vector3(0f, hgt, 0f);
                        body.localScale = new Vector3(1f + 0.10f * land - 0.04f * air, 1f - 0.10f * land + 0.06f * air, 1f);
                        body.localRotation = Quaternion.Euler(0f, 0f, big ? 10f * Mathf.Sin(ph * 2f * Mathf.PI) : 4f * Mathf.Sin(t * 2.2f));
                        float k = 1f - 0.45f * air * (big ? 1f : 0.6f);
                        it.Shadow.transform.localScale = Vector3.Scale(ShadowBase(it), new Vector3(k, k, 1f));
                        Sparkles(it, t, s, 0.55f);
                        break;
                    }
                    case Kind.Lighthouse:
                    {
                        body.localPosition = new Vector3(0f, s * 0.012f * Mathf.Sin(t * 1.6f), 0f);
                        body.localRotation = Quaternion.Euler(0f, 0f, 1.2f * Mathf.Sin(t * 1.1f));
                        // Dönen huzme: genişlik cos ile salınır, arkadayken söner.
                        float c = Mathf.Cos(t * 2.1f);
                        float gw = s * (0.25f + 1.1f * Mathf.Abs(c)) / SpecialArt.Glow.bounds.size.x;
                        float gh = s * 0.16f / SpecialArt.Glow.bounds.size.y;
                        it.Beam.transform.localScale = new Vector3(gw, gh, 1f);
                        it.Beam.transform.localPosition = new Vector3(c * s * 0.28f, it.Beam.transform.localPosition.y, 0f);
                        it.Beam.color = new Color(1f, 0.93f, 0.55f, 0.35f + 0.45f * Mathf.Max(0f, Mathf.Sin(t * 2.1f + 1.57f) * 0.5f + 0.5f));
                        break;
                    }
                    case Kind.Rock:
                    {
                        body.localRotation = Quaternion.Euler(0f, 0f, 1.8f * Mathf.Sin(t * 1.3f));
                        float b = 1f + 0.015f * Mathf.Sin(t * 2.4f);
                        body.localScale = new Vector3(b, 2f - b, 1f);
                        Sparkles(it, t, s, 0.6f);
                        break;
                    }
                    case Kind.Fountain:
                    {
                        body.localPosition = new Vector3(0f, s * 0.008f * Mathf.Sin(t * 2f), 0f);
                        // Damlalar tepeden parabolle sıçrar.
                        for (int i = 0; i < it.Bits.Count; i++)
                        {
                            float cyc = ((t * 0.9f + i / (float)it.Bits.Count) % 1f);
                            float dir = (i % 2 == 0 ? -1f : 1f) * (0.12f + 0.06f * (i % 3));
                            float x = dir * s * cyc;
                            float y = s * (0.86f + 0.22f * cyc - 0.55f * cyc * cyc);
                            var tr = it.Bits[i].transform;
                            tr.localPosition = new Vector3(x, y, 0f);
                            float d = s * 0.045f / SpecialArt.Disc.bounds.size.x;
                            tr.localScale = new Vector3(d, d, 1f);
                            var col = it.Bits[i].color; col.a = 0.9f * Mathf.Sin(cyc * Mathf.PI); it.Bits[i].color = col;
                        }
                        break;
                    }
                }
            }
        }

        private static Vector3 ShadowBase(Item it)
        {
            // İlk kurulumdaki ölçek (Make) — gölgenin kendi ölçeği; maskot zıpladıkça küçülür.
            float shadowW = it.Main.sprite != null ? it.Main.sprite.bounds.size.x * it.Main.transform.localScale.x * 0.8f : 1f;
            return new Vector3(shadowW / SpecialArt.Disc.bounds.size.x, shadowW * 0.22f / SpecialArt.Disc.bounds.size.y, 1f);
        }

        /// <summary>Görselin çevresinde sırayla yanıp sönen küçük parıltılar.</summary>
        private static void Sparkles(Item it, float t, float s, float spread)
        {
            for (int i = 0; i < it.Bits.Count; i++)
            {
                float cyc = (t * 0.7f + i * 0.27f) % 1f;
                float ang = i * 2.4f + Mathf.Floor(t * 0.7f + i * 0.27f) * 1.7f;
                var tr = it.Bits[i].transform;
                tr.localPosition = new Vector3(Mathf.Cos(ang) * s * spread * 0.5f, s * (0.55f + 0.4f * Mathf.Sin(ang)), 0f);
                float d = s * 0.05f * Mathf.Sin(cyc * Mathf.PI) / SpecialArt.Disc.bounds.size.x;
                tr.localScale = new Vector3(d, d, 1f);
            }
        }
    }
}
