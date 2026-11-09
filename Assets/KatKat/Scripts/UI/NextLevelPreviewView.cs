using System;
using KatKat.Core;
using KatKat.Levels;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Bölüm sonu kartındaki sonraki bölüm önizlemesi (Görsel Şartname v3 §6). Gerçek sonraki LevelData'dan canlı
    /// uGUI ile kurulur; salt okunurdur: GameController, kural motoru, girdi, zincir veya ödül çalıştırmaz.
    /// Her etkin hücre: üst renk taşı (şekil işaretiyle), altında en çok iki alt katman şeridi; boş/etkin olmayan
    /// hücre tepsi zemini. Örtüler (buz, kilit, zarf, ataş) ve taban etkileri (yıldız, döner taban) sade rozet.
    /// Tahta zemini sonraki bölümün temasıdır (bölge geçişinde yeni tema). Bir çözüm ipucu değildir.
    /// Sonraki bölüm yoksa tahta çizilmez; Loc.T("ui.soon") yazar.
    /// </summary>
    public sealed class NextLevelPreviewView : MonoBehaviour
    {
        public readonly struct Data
        {
            public readonly LevelData Next;
            public readonly ThemeDefinition Theme;
            public readonly int Number;
            /// <summary>Sonraki bölüm yeni bir bölgede (100→101 gibi): kartta bölge adı gösterilir.</summary>
            public readonly bool NewRegion;
            public Data(LevelData next, ThemeDefinition theme, int number, bool newRegion = false)
            {
                Next = next; Theme = theme; Number = number; NewRegion = newRegion;
            }
            public bool IsEnd => Next == null;
        }

        // Zafer kartının (800) iç alanında kalır: iki çerçeve üst üste binmez.
        public const float Width = 660f, Height = 300f;
        private const float BoardW = 360f, BoardH = 256f;

        public int CellCount { get; private set; }
        public string LevelId { get; private set; }

        public static NextLevelPreviewView Create(RectTransform parent, Data data)
        {
            var root = UiFactory.Rect("NextPreview", parent);
            root.sizeDelta = new Vector2(Width, Height);
            var v = root.gameObject.AddComponent<NextLevelPreviewView>();
            v.Build(root, data);
            return v;
        }

        private void Build(RectTransform root, Data data)
        {
            var slot = Ui1.Art("next-preview-panel") ?? UiFactory.Art("ui/next-preview-panel", "btn_cream"); // ui1/16
            var bg = UiFactory.Image("Slot", root, slot ?? UiArt.Box, slot != null ? Color.white : UiArt.Cream, slot != null && slot.border.sqrMagnitude > 0f);
            UiFactory.Stretch(bg.rectTransform);
            UiFactory.FitSliced(bg, Height);

            if (data.IsEnd)
            {
                var t = UiFactory.Text("End", root, Loc.T("ui.soon"), 40, UiArt.Brown);
                UiFactory.Stretch(t.rectTransform, 40, 40, 0, 6);
                return;
            }

            var label = UiFactory.Text("Label", root, Loc.T("ui.next"), 34, UiArt.Brown, TextAnchor.MiddleLeft);
            label.lineSpacing = 0.9f;
            UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(128f, 24f), new Vector2(150f, 100f));
            var num = UiFactory.Text("Number", root, Loc.F("ui.level", data.Number), 26, new Color(UiArt.Brown.r, UiArt.Brown.g, UiArt.Brown.b, 0.8f), TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Place(num.rectTransform, new Vector2(0f, 0.5f), new Vector2(128f, -40f), new Vector2(150f, 34f));
            if (data.NewRegion && data.Theme != null)
            {
                // Bölge geçişi: yeni temanın adı, temanın çerçeve renginde küçük etiket.
                var tag = UiFactory.Image("Region", root, UiArt.Box, data.Theme.RimFill, true);
                UiFactory.Place(tag.rectTransform, new Vector2(0f, 0.5f), new Vector2(124f, -84f), new Vector2(150f, 40f));
                tag.pixelsPerUnitMultiplier = UiArt.Box.border.x / 18f;
                var tt = UiFactory.Text("Name", tag.transform, Loc.F("ui.newtheme", data.Theme.LocalName), 24, Color.white);
                UiFactory.Stretch(tt.rectTransform);
                var sh = tt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
            }

            var arrowSprite = PremiumArt.V4("icons/next");
            var arrow = UiFactory.Image("Arrow", root, arrowSprite ?? UiArt.Icon(UiIcon.Play), arrowSprite != null ? UiArt.HudTeal : UiArt.Teal);
            arrow.preserveAspect = true;
            UiFactory.Place(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(56f, 76f));

            var area = UiFactory.Rect("Board", root);
            UiFactory.Place(area, new Vector2(0.5f, 0.5f), new Vector2(40f, 2f), new Vector2(BoardW, BoardH));
            try
            {
                LevelId = data.Next.Id;
                BuildBoard(area, data.Next, data.Theme ?? ThemeDefinition.Coast);
            }
            catch (Exception e)
            {
                // Bozuk/eksik veri: uydurma tahta yok, kontrollü yer tutucu + geliştirici günlüğü.
                Debug.LogError($"[KatKat] Sonraki bölüm önizlemesi kurulamadı ({data.Next?.Id}): {e.Message}");
                foreach (Transform c in area) UiFactory.Kill(c.gameObject);
                var t = UiFactory.Text("Error", area, Loc.T("ui.previewfail"), 28, UiArt.Brown);
                UiFactory.Stretch(t.rectTransform);
            }
        }

        /// <summary>Kesit penceresi: sonraki bölümün tahtasından en çok 3 satır × 4 sütunluk bir parça (kullanıcı referansı).</summary>
        private const int WinW = 4, WinH = 3;

        /// <summary>
        /// Kesit: sonraki bölümün tamamı değil, mekaniklerin en çok göründüğü 3×4'lük parça. Yalnızca o parçadaki
        /// etkin hücreler çizilir; altlarındaki tepsi hücre hücre (silüet tahtanın şeklini izler). Taşlarda gerçek
        /// mekanik kaplamaları (buz, kilit, zarf, ataş) ve tabanları (yıldız, döner taban) bulunur.
        /// </summary>
        public static RectInt PickWindow(BoardState b)
        {
            int ww = Mathf.Min(WinW, b.Width), wh = Mathf.Min(WinH, b.Height);
            var best = new RectInt(0, 0, ww, wh);
            int bestScore = int.MinValue;
            for (int oy = 0; oy + wh <= b.Height; oy++)
            for (int ox = 0; ox + ww <= b.Width; ox++)
            {
                int score = 0;
                for (int y = oy; y < oy + wh; y++)
                for (int x = ox; x < ox + ww; x++)
                {
                    var c = b[x, y];
                    if (!c.Active) continue;
                    score += c.Layers.Count > 0 ? 2 : 1;
                    if (c.HasCover) score += 6;
                    if (c.HasBottomStar || c.HasRotor) score += 4;
                }
                // Eşitlikte tahtanın ortasına yakın pencere.
                score = score * 100 - Mathf.Abs(2 * ox + ww - b.Width) - Mathf.Abs(2 * oy + wh - b.Height);
                if (score > bestScore) { bestScore = score; best = new RectInt(ox, oy, ww, wh); }
            }
            return best;
        }

        private void BuildBoard(RectTransform area, LevelData d, ThemeDefinition theme)
        {
            var state = LevelLoader.ToState(d); // yalnızca okunur dönüşüm; komut uygulanmaz
            var b = state.Board;
            var win = PickWindow(b);
            const float rowK = 1.08f;
            float cell = Mathf.Min(BoardW / (win.width + 0.3f), BoardH / ((win.height - 1) * rowK + 1.2f));
            float w = cell * win.width, h = cell * ((win.height - 1) * rowK + 1f);
            float x0 = -w * 0.5f + cell * 0.5f, y0 = h * 0.5f - cell * 0.5f;
            float face = cell * 0.88f, stripe = Mathf.Max(2f, cell * 0.1f);

            // Hücre başına tepsi parçası (birleşince tahtanın silüeti) — sonraki bölümün tema rengi, ince altın kenar.
            for (int y = win.yMin; y < win.yMax; y++)
            for (int x = win.xMin; x < win.xMax; x++)
            {
                if (!b[x, y].Active) continue;
                var center = new Vector2(x0 + (x - win.xMin) * cell, y0 - (y - win.yMin) * cell * rowK);
                var plate = UiFactory.Image("Plate", area, UiArt.Box, theme.BackplateColor, true);
                Place(plate.rectTransform, center + new Vector2(0f, -stripe), new Vector2(cell * 1.06f, cell * rowK * 1.06f + stripe));
                plate.pixelsPerUnitMultiplier = BoxPpu(cell * 1.2f);
                var po = plate.gameObject.AddComponent<Outline>();
                po.effectColor = new Color(theme.RimAccent.r / 255f, theme.RimAccent.g / 255f, theme.RimAccent.b / 255f, 0.9f);
                po.effectDistance = new Vector2(2f, -2f);
            }

            int count = 0;
            for (int y = win.yMin; y < win.yMax; y++)
            for (int x = win.xMin; x < win.xMax; x++)
            {
                var c = b[x, y];
                var center = new Vector2(x0 + (x - win.xMin) * cell, y0 - (y - win.yMin) * cell * rowK);
                if (!c.Active) continue;
                if (c.Layers.Count == 0)
                {
                    var slotImg = UiFactory.Image("Slot", area, UiArt.Box, theme.Slot, true);
                    Place(slotImg.rectTransform, center + new Vector2(0f, -stripe), new Vector2(face, face));
                    slotImg.pixelsPerUnitMultiplier = BoxPpu(face);
                    Badges(area, c, center, face);
                    continue;
                }
                count++;
                // Alt katman şeritleri (en çok iki): üst taşın altında aşağı kaymış ince bantlar.
                int under = Mathf.Min(2, c.Layers.Count - 1);
                for (int k = under; k >= 1; k--)
                {
                    var col = c.Layers[c.Layers.Count - 1 - k];
                    var s = UiFactory.Image("Under", area, UiStoneArt.Layer(col, false), Color.white);
                    Place(s.rectTransform, center + new Vector2(0f, -stripe * k), new Vector2(face, face));
                }
                var top = UiFactory.Image("Top", area, PremiumArt.Chip(c.Top) ?? UiArt.Chip(c.Top), Color.white);
                top.preserveAspect = true;
                Place(top.rectTransform, center, new Vector2(face, face));
                Badges(area, c, center, face);
            }
            CellCount = count;
        }

        /// <summary>Taşın üstündeki mekanik: oyundaki gibi kaplama (buz kabuğu, kilit kayışı + asma kilit, zarf + mühür, ataş) ve taban rozeti.</summary>
        private static void Badges(RectTransform area, CellState c, Vector2 center, float face)
        {
            if (c.IceHitsRemaining > 0)
            {
                Badge(area, SpecialArt.Ice(c.IceHitsRemaining < 2), center, face * TileSkin.IceWidth);
            }
            else if (c.LockRemaining > 0)
            {
                Badge(area, "mechanics/lock-ribbon", center + face * TileSkin.RibbonOffset, face * TileSkin.RibbonWidth);
                Badge(area, "icons/lock", center + face * TileSkin.LockOffset, face * TileSkin.LockWidth);
                var chip = PremiumArt.Chip(c.LockColor);
                if (chip != null)
                {
                    var ci = UiFactory.Image("LockColor", area, chip, Color.white);
                    ci.preserveAspect = true;
                    Place(ci.rectTransform, center + new Vector2(face * 0.3f, -face * 0.3f), new Vector2(face * 0.4f, face * 0.4f));
                    var n = UiFactory.Title("Count", ci.transform, c.LockRemaining.ToString(), Mathf.RoundToInt(face * 0.32f), Color.white, UiArt.Brown);
                    UiFactory.Stretch(n.rectTransform);
                }
            }
            else if (c.HasEnvelope)
            {
                Badge(area, "mechanics/envelope-closed", center, face * TileSkin.EnvelopeWidth);
                var seal = PremiumArt.Chip(c.EnvelopeColor);
                if (seal != null)
                {
                    var disc = UiFactory.Image("SealDisc", area, UiArt.Circle, new Color32(0xF6, 0xE2, 0xC0, 0xFF));
                    Place(disc.rectTransform, center + TileSkin.SealOffset * face, Vector2.one * (face * 0.28f));
                    var si = UiFactory.Image("Seal", area, seal, Color.white);
                    si.preserveAspect = true;
                    Place(si.rectTransform, center + TileSkin.SealOffset * face, Vector2.one * (face * 0.21f));
                }
            }
            else if (c.ClipHitsRemaining > 0 || c.LinkHits > 0)
                Badge(area, SpecialArt.Clip(false), center + face * TileSkin.ClipOffset, face * TileSkin.ClipWidth);
            else if (c.SpongeNeed != 0)
                Badge(area, SpecialArt.Sponge, center, face);

            if (c.HasSpecial)
            {
                var sprite = SpecialArt.For(c.Special);
                var box = TileSkin.SpecialBox(c.Special);
                float width = Mathf.Min(box.x, box.y * sprite.rect.width / sprite.rect.height) * face;
                Badge(area, sprite, center + face * TileSkin.SpecialOffset(c.Special), width)
                    .localRotation = Quaternion.Euler(0f, 0f, TileSkin.SpecialAngle(c.Special));
            }
            if (c.HasBottomStar) Badge(area, "icons/star", center + new Vector2(face * 0.32f, -face * 0.32f), face * 0.42f);
            if (c.HasRotor) Badge(area, "mechanics/rotor-base", center + new Vector2(face * 0.36f, -face * 0.38f), face * 0.4f);
        }

        private static void Badge(RectTransform area, string key, Vector2 pos, float size)
        {
            Badge(area, TileSkin.Sprite(key), pos, size);
        }

        private static RectTransform Badge(RectTransform area, Sprite sp, Vector2 pos, float width)
        {
            if (sp == null) return null;
            var img = UiFactory.Image("Badge", area, sp, Color.white);
            img.preserveAspect = true;
            Place(img.rectTransform, pos, new Vector2(width, width * sp.rect.height / sp.rect.width));
            return img.rectTransform;
        }

        /// <summary>Kod üretimi kutunun 9-dilim köşesi küçük hücrede yüzün ~%22'si kalsın.</summary>
        private static float BoxPpu(float face) => Mathf.Max(1f, UiArt.Box.border.x / (face * 0.22f));

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
