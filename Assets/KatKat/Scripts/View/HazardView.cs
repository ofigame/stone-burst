using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Konsept pilotu: tehdidin tahtadaki görünümü (kural motoru durumundan her senkronda yeniden çizilir; mantık okumaz).
    /// Tehdit hep önceden görünür (mantık oyunu şartı): çökecek satır çatlaklı ve sayaçlı, fitil sayacı, kuzgunun yolu ve
    /// yuvası, yağmurun düşeceği yığınlar, yosun, balonlar. Sıralama yığınların ve örtülerin üstünde, ipucu çerçevesinin altında.
    /// </summary>
    public sealed class HazardView : MonoBehaviour
    {
        /// <summary>Yığın ve örtülerin üstünde (≈10 + satır×4 + 10), arayüz tuvalinin (500) ve kartların altında.</summary>
        private const int Order = 300;
        private BoardView _board;
        private bool[] _initialActive;
        private readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>();
        private readonly List<TextMesh> _texts = new List<TextMesh>();
        private int _usedSprites, _usedTexts;
        private float _pulseFrom;
        private readonly List<Coord> _pulse = new List<Coord>();

        public static readonly Color Danger = new Color(0.93f, 0.27f, 0.20f, 1f);
        public static readonly Color FuseOrange = new Color(0.98f, 0.55f, 0.13f, 1f);
        public static readonly Color RainBlue = new Color(0.25f, 0.62f, 0.98f, 1f);
        public static readonly Color MossGreen = new Color(0.30f, 0.62f, 0.22f, 0.86f);
        public static readonly Color RavenInk = new Color(0.13f, 0.11f, 0.20f, 1f);
        public static readonly Color BalloonRed = new Color(0.96f, 0.30f, 0.36f, 1f);

        public static HazardView Attach(BoardView board)
        {
            var go = new GameObject("Hazards");
            go.transform.SetParent(board.transform, false);
            var v = go.AddComponent<HazardView>();
            v._board = board;
            return v;
        }

        /// <summary>Bölüm başında: ilk etkin hücreler (çöken hücreleri çukur olarak çizmek için).</summary>
        public void Begin(LevelState s)
        {
            _initialActive = null;
            _pulse.Clear();
            if (s?.Hazard == null) { Clear(); return; }
            _initialActive = new bool[s.Board.Count];
            for (int i = 0; i < s.Board.Count; i++) _initialActive[i] = s.Board[i].Active;
            Show(s);
        }

        /// <summary>Bu hamlenin etkilediği hücreler kısa süre parlar.</summary>
        public void Pulse(IList<Coord> cells)
        {
            _pulse.Clear();
            if (cells != null) _pulse.AddRange(cells);
            _pulseFrom = Time.unscaledTime;
        }

        public void Clear()
        {
            foreach (var s in _sprites) s.gameObject.SetActive(false);
            foreach (var t in _texts) t.gameObject.SetActive(false);
            _usedSprites = _usedTexts = 0;
        }

        public void Show(LevelState s)
        {
            Clear();
            _last = s;
            var h = s?.Hazard;
            if (h == null || _board == null) return;
            var b = s.Board;
            float p = _board.Pitch;
            // Çöken hücreler (çürüme): koyu çukur.
            if (_initialActive != null && h.Kind == HazardKind.Rot)
                for (int i = 0; i < b.Count; i++)
                    if (_initialActive[i] && !b[i].Active) Put(HazardArt.Pit, Cell(b, i), p * 1.0f, new Color(0.10f, 0.07f, 0.06f, 0.82f), -6);
            switch (h.Kind)
            {
                case HazardKind.Rot:
                    if (h.Row < 0) break;
                    bool first = true;
                    for (int x = 0; x < b.Width; x++)
                    {
                        if (!b[x, h.Row].Active) continue;
                        int i = b.Index(x, h.Row);
                        var c = Cell(b, i);
                        Color col = h.Counter <= 1 ? Danger : new Color(0.96f, 0.62f, 0.25f, 1f);
                        Put(HazardArt.Frame, c, p * 0.98f, col, 0);
                        Put(HazardArt.Crack, c + new Vector3(0f, -p * 0.30f, 0f), p * 0.42f, new Color(0.22f, 0.12f, 0.08f, 0.9f), 1);
                        if (first) { Badge(c + new Vector3(-p * 0.36f, p * 0.36f, 0f), h.Counter.ToString(), col, p); first = false; }
                    }
                    break;
                case HazardKind.Fuse:
                    for (int i = 0; i < b.Count; i++)
                        if (h.Fuse[i] > 0) Badge(Top(b, i) + new Vector3(0f, p * 0.05f, 0f), h.Fuse[i].ToString(), h.Fuse[i] <= 1 ? Danger : FuseOrange, p * 1.25f);
                    break;
                case HazardKind.Raven:
                {
                    // Yol: kalan her hücrede bir sonrakine bakan altın ok (yön görünür); son hücre yuva.
                    for (int k = h.Index + 1; k < h.Path.Length; k++)
                    {
                        var pt = Top(b, h.Path[k]);
                        if (k == h.Path.Length - 1)
                        {
                            Put(HazardArt.Frame, Cell(b, h.Path[k]), p * 0.98f, Danger, 1);
                            Put(HazardPaint.Nest, pt + new Vector3(0f, p * 0.06f, 0f), p * 1.05f, Color.white, 3);
                            continue;
                        }
                        int nx = b.X(h.Path[k + 1]) - b.X(h.Path[k]), ny = b.Y(h.Path[k + 1]) - b.Y(h.Path[k]);
                        float ang = Mathf.Atan2(-ny, nx) * Mathf.Rad2Deg; // tahtada y aşağı
                        Put(HazardPaint.Chevron, pt, p * 0.62f, new Color(1f, 1f, 1f, 0.95f), 2, ang);
                    }
                    int at = h.RavenCell;
                    int dir = h.Index + 1 < h.Path.Length ? b.X(h.Path[h.Index + 1]) - b.X(at) : 1;
                    var rp = Top(b, at) + new Vector3(0f, p * 0.22f, 0f);
                    Put(HazardPaint.Raven, rp, p * 1.15f, Color.white, 5, 0f, dir < 0);
                    // Yuvaya kalan adım: kuzgunun yanında küçük rozet.
                    Badge(rp + new Vector3(-p * 0.36f, p * 0.30f, 0f), (h.Path.Length - 1 - h.Index).ToString(), RavenInk, p * 0.9f);
                    break;
                }
                case HazardKind.Rain:
                {
                    var vol = h.NextVolley;
                    if (vol == null) break;
                    foreach (var code in vol)
                    {
                        int i = code >> 3;
                        if (!b[i].Active) continue;
                        var top = Top(b, i);
                        // Damla mavi (her taş renginde görünür); içinde düşecek taşın rengi.
                        Put(HazardArt.Drop, top + new Vector3(0f, p * 0.34f, 0f), p * 0.78f, Color.white, 3);
                        Put(HazardArt.Drop, top + new Vector3(0f, p * 0.34f, 0f), p * 0.68f, RainBlue, 4);
                        Put(HazardArt.Disc, top + new Vector3(0f, p * 0.26f, 0f), p * 0.30f, PeelColor((LayerColor)(code & 7)), 5);
                        Badge(top + new Vector3(p * 0.34f, p * 0.56f, 0f), h.Counter.ToString(), RainBlue, p);
                    }
                    // Sınıra yakın yığınlar kırmızı çerçeve.
                    for (int i = 0; i < b.Count; i++)
                        if (b[i].Active && b[i].Layers.Count >= h.Max) Put(HazardArt.Frame, Cell(b, i), p * 0.98f, Danger, 0);
                    break;
                }
                case HazardKind.Moss:
                    for (int i = 0; i < b.Count; i++)
                        if (b[i].Moss) Put(HazardArt.Moss, Top(b, i), p * 0.86f, MossGreen, 3);
                    break;
                case HazardKind.Balloon:
                    for (int i = 0; i < b.Count; i++)
                    {
                        if (!h.Balloon[i]) continue;
                        var top = Top(b, i);
                        bool edge = b.Y(i) == 0 || !b[b.X(i), b.Y(i) - 1].Active;
                        // Kaçmak üzere (üstü tahta dışı): altın uyarı balonu ve kırmızı halka.
                        if (edge) Put(HazardArt.Ring, top + new Vector3(0f, p * 0.36f, 0f), p * 0.95f, Danger, 4);
                        Put(edge ? HazardPaint.BalloonDanger : HazardPaint.Balloon, top + new Vector3(0f, p * 0.38f, 0f), p * 1.2f, Color.white, 5);
                    }
                    break;
            }
            // Bu hamlenin etkilediği hücreler: kısa parıltı (0,9 sn).
            float age = Time.unscaledTime - _pulseFrom;
            if (age < 0.9f)
                foreach (var c in _pulse)
                    if (b.InBounds(c.X, c.Y)) Put(HazardArt.Ring, _board.CellCenter(c.X, c.Y), p * (0.9f + age * 0.3f), new Color(1f, 0.95f, 0.6f, 0.9f * (1f - age / 0.9f)), 7);
        }

        private LevelState _last;

        private void Update()
        {
            // Parıltı sürerken her karede yeniden çizim; bitince bir kez daha (parıltısız).
            if (_pulse.Count == 0 || _last == null) return;
            if (Time.unscaledTime - _pulseFrom > 0.95f) _pulse.Clear();
            Show(_last);
        }

        private static Color PeelColor(LayerColor c) => LayerPalette.FrontColor(c);

        private Vector3 Cell(BoardState b, int i) => _board.CellCenter(b.X(i), b.Y(i));

        /// <summary>Yığının üst yüzü (tahta yerel uzayında).</summary>
        private Vector3 Top(BoardState b, int i)
        {
            var st = _board.StackAt(b.X(i), b.Y(i));
            if (st == null || !st.gameObject.activeSelf) return Cell(b, i);
            var w = st.transform.TransformPoint(st.TopFaceLocal);
            return _board.transform.InverseTransformPoint(w);
        }

        private void Put(Sprite sprite, Vector3 pos, float size, Color color, int order, float angle = 0f, bool flip = false)
        {
            if (_usedSprites == _sprites.Count)
            {
                var go = new GameObject("Hz");
                go.transform.SetParent(transform, false);
                _sprites.Add(go.AddComponent<SpriteRenderer>());
            }
            var sr = _sprites[_usedSprites++];
            sr.gameObject.SetActive(true);
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = Order + order;
            sr.transform.localPosition = pos;
            sr.transform.localScale = new Vector3(flip ? -size : size, size, 1f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>Renkli disk üstünde beyaz sayı.</summary>
        private void Badge(Vector3 pos, string text, Color color, float pitch)
        {
            Put(HazardArt.Disc, pos, pitch * 0.40f, new Color(1f, 1f, 1f, 0.95f), 8);
            Put(HazardArt.Disc, pos, pitch * 0.33f, color, 9);
            if (_usedTexts == _texts.Count)
            {
                var go = new GameObject("HzText");
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<TextMesh>();
                var font = KatKat.UI.UiTypography.WorldBold;
                t.font = font;
                t.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                t.fontSize = 64;
                t.characterSize = 0.1f;
                t.anchor = TextAnchor.MiddleCenter;
                t.alignment = TextAlignment.Center;
                _texts.Add(t);
            }
            var tm = _texts[_usedTexts++];
            tm.gameObject.SetActive(true);
            tm.text = text;
            tm.color = Color.white;
            tm.GetComponent<MeshRenderer>().sortingOrder = Order + 10;
            tm.transform.localPosition = pos + new Vector3(0f, -pitch * 0.01f, 0f);
            float cs = pitch * 0.30f;
            tm.transform.localScale = new Vector3(cs, cs, 1f);
        }
    }
}
