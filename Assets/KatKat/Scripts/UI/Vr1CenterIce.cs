using KatKat.Levels;
using KatKat.View;
using TMPro;
using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// vr1 P1 prototipi (03_KOLEKSIYON_ODUL "Merkez görseli"): ortası boş tahtalarda sıradaki koleksiyon taşı buz içinde,
    /// tahtanın iç boşluğunda. Yeni delik açılmaz (<see cref="BoardHoles"/>: kenara değmeyen, geçit olmayan boş bölge;
    /// en az 2×2). Görsel boş karenin %78'i, oran korunur; hücre örtmez; dokunuş yakalamaz (SpriteRenderer, tahta girdisi
    /// hücre koordinatından okunur). Altında küçük "Koleksiyon" etiketi: güçlendirici sanılmasın. Son hamle kazandırınca
    /// kısa çatlak ışığı; tam kırılma ve kazanım sunumu haritada (sonuç kesinleşince).
    /// </summary>
    public sealed class Vr1CenterIce : MonoBehaviour
    {
        public const float Fill = 0.78f;
        public const int MinSide = 2;

        private SpriteRenderer _back, _stone, _frost, _cracks, _rim, _flash;
        private TextMeshPro _label;
        private BoardHole _hole;
        private float _flashT = -1f;

        public BoardHole Hole => _hole;
        public bool Flashing => _flashT >= 0f;

        public static Vr1CenterIce Create(Transform board, BoardHole hole, Sprite stone, float p, string label)
        {
            var go = new GameObject("Vr1CenterIce");
            go.transform.SetParent(board, false);
            var c = go.AddComponent<Vr1CenterIce>();
            c._hole = hole;
            // Sıra: zemin/çerçeve (≤4) üstünde, yığınların (≥10) altında.
            c._back = Child(go.transform, "IceBack", Vr1IceArt.Frost, 5);
            c._back.color = new Color(0.55f, 0.78f, 0.95f, 0.5f);
            c._stone = Child(go.transform, "Stone", stone, 6);
            c._frost = Child(go.transform, "Frost", Vr1IceArt.Frost, 7);
            c._cracks = Child(go.transform, "Cracks", null, 8);
            c._rim = Child(go.transform, "IceRim", Vr1Proto.IceRim, 9);
            c._flash = Child(go.transform, "Flash", SpecialArt.Glow, 9);
            c._flash.color = new Color(1f, 1f, 1f, 0f);
            var lt = new GameObject("Label");
            lt.transform.SetParent(go.transform, false);
            c._label = lt.AddComponent<TextMeshPro>();
            c._label.font = UiTypography.Font;
            c._label.text = label;
            c._label.alignment = TextAlignmentOptions.Center;
            c._label.color = new Color(1f, 1f, 1f, 0.92f);
            c._label.outlineWidth = 0.25f;
            c._label.outlineColor = new Color32(0x10, 0x3A, 0x5A, 0xFF);
            c._label.textWrappingMode = TextWrappingModes.NoWrap;
            c._label.sortingOrder = 9;
            c.SetProgress(p);
            return c;
        }

        private static SpriteRenderer Child(Transform parent, string name, Sprite s, int order)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Evre görünümü haritadaki kartla aynı kuralla (taş tonu, kırağı, çatlak).</summary>
        public void SetProgress(float p)
        {
            int stage = KatKat.Meta.CollectionProgress.IceStage(p);
            Color[] tint = { new Color(0.30f, 0.42f, 0.58f, 0.9f), new Color(0.58f, 0.70f, 0.86f, 0.95f), new Color(0.84f, 0.90f, 1f, 1f), Color.white };
            float[] frost = { 0.90f, 0.62f, 0.34f, 0.14f };
            _stone.color = tint[stage];
            _frost.color = new Color(0.88f, 0.95f, 1f, frost[stage]);
            _cracks.sprite = stage >= 1 ? Vr1IceArt.Cracks(stage) : null;
        }

        /// <summary>Tahta yerleşimi değişince (ekran, bölüm başı) konum ve ölçek.</summary>
        public void Layout(BoardView board)
        {
            float pitch = board.Pitch;
            Vector3 a = board.CellCenter(_hole.SquareX, _hole.SquareY), b = board.CellCenter(_hole.SquareX + _hole.Side - 1, _hole.SquareY + _hole.Side - 1);
            transform.localPosition = (a + b) * 0.5f;
            // Boşluktaki mevcut süs (maskot, fener, çeşme) buzlu taş varken gizlenir; yerleşim yenilenince yeniden.
            if (board.Decor != null) board.Decor.HideNear(transform.localPosition, _hole.Side * pitch);
            float box = _hole.Side * pitch * Fill;
            Fit(_back, box * 0.82f);
            Fit(_stone, box * 0.66f, new Vector3(0f, box * 0.04f, 0f));
            Fit(_frost, box * 0.76f);
            Fit(_cracks, box * 0.76f);
            Fit(_rim, box);
            Fit(_flash, box * 1.15f);
            _label.fontSize = Mathf.Max(0.5f, pitch * 2.6f); // TMP dünya metni: yazı boyu ≈ 0,1 × fontSize birim
            _label.rectTransform.sizeDelta = new Vector2(box * 2f, pitch * 0.6f);
            _label.transform.localPosition = new Vector3(0f, -box * 0.5f + pitch * 0.02f, 0f);
        }

        private static void Fit(SpriteRenderer sr, float size, Vector3 offset = default)
        {
            sr.transform.localPosition = offset;
            if (sr.sprite == null) return;
            float w = Mathf.Max(sr.sprite.rect.width, sr.sprite.rect.height) / sr.sprite.pixelsPerUnit;
            float s = size / Mathf.Max(0.0001f, w);
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>Son hamle kazandırınca kısa çatlak ışığı (180 ms çıkış, 420 ms sönüş).</summary>
        public void Flash() { _flashT = 0f; Vr1Proto.Play("ice-crack"); }

        private void Update() => Advance(Time.unscaledDeltaTime * 1000f);

        public void Advance(float ms)
        {
            if (_flashT < 0f) return;
            _flashT += ms;
            float a = _flashT < 180f ? _flashT / 180f * 0.85f : Mathf.Max(0f, 0.85f - (_flashT - 180f) / 420f * 0.85f);
            _flash.color = new Color(1f, 0.97f, 0.85f, a);
            if (_flashT > 600f) { _flashT = -1f; _flash.color = new Color(1f, 1f, 1f, 0f); }
        }
    }
}
