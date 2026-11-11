using System;
using System.Collections.Generic;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// ui1 tasarım paketi (docs/ui1) menü sayfaları: krem panel, ince altın çerçeve, turkuaz ana düğme, lacivert metin.
    /// Mantık ve veri mevcut koddan; yalnız görünüm. Her sayfanın eski hâli ui1 kaynakları yoksa çalışır.
    /// </summary>
    public sealed partial class MenuView
    {
        internal static Sprite CalendarSprite() => CalendarIcon();

        // ---- Ortak üst çubuk (03 koleksiyon, 09 deneme): geri · logo · yıldız kapsülü · ayarlar ----------------------

        private int _shellStars;
        private Action _shellSettings;
        /// <summary>Bölüm sayısı (yıldız toplamı için; harita açılmadan da doğru).</summary>
        public Func<int> LevelCount;
        /// <summary>Bölüm dışı yıldızlar (günlük bulmaca; kullanıcı 2026-11-08). Toplam yıldıza eklenir.</summary>
        public Func<int> ExtraStars;

        private int ShellStarSum()
        {
            if (StarsOf == null || LevelCount == null) return _shellStars;
            int n = LevelCount(), sum = 0;
            for (int i = 0; i < n; i++) sum += StarsOf(i);
            return sum + (ExtraStars != null ? ExtraStars() : 0);
        }

        /// <summary>
        /// ui1/03, 09: haritanın üst satırıyla aynı öğeler tek sırada (tasarım: sol geri, logo, krem yıldız kapsülü,
        /// sağ ayarlar). Yıldız sayısı ve ayarlar eylemi haritanın son kurulumundan (aynı veri).
        /// </summary>
        private void Ui1TopBar(RectTransform page, Action back, Action settings)
        {
            settings = settings ?? _shellSettings;
            if (back != null) Ui1.Round("Back", page, "icon-back", new Vector2(0f, 1f), new Vector2(84f, -82f), 112f, back);
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            if (logoSprite != null)
            {
                var logo = Decor("Logo", page, logoSprite);
                logo.preserveAspect = true;
                UiFactory.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(back != null ? -190f : -280f, -80f), new Vector2(330f, 136f)); // iki satırlı logo kutuya sığar
            }
            if (Ui3.Ready) { U3StarCapsule(page, 700f, 56f, ShellStarSum()); if (settings != null) U3Gear(page, settings); return; }
            var capArt = V11("star-capsule");
            if (capArt != null)
            {
                var cap = Decor("StarCapsule", page, capArt);
                UiFactory.Place(cap.rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, -82f), new Vector2(250f, 250f * capArt.rect.height / capArt.rect.width));
                var star = Decor("Star", cap.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
                star.preserveAspect = true;
                UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(62f, 3f), new Vector2(76f, 76f));
                var n = UiFactory.Title("Stars", cap.transform, ShellStarSum().ToString(), 52, V11Navy, PillCream);
                n.alignment = UiTypography.Alignment(TextAnchor.MiddleCenter);
                n.raycastTarget = false;
                UiFactory.Place(n.rectTransform, new Vector2(0f, 0.5f), new Vector2(164f, 2f), new Vector2(124f, 70f));
                FitText(n, 24);
            }
            if (settings != null) TopGear(page, settings, new Vector2(-90f, -82f));
        }

        // ---- 02 Günlük bulmaca (harita üstünde modal) ----------------------------------------------------------

        private void ShowDailyUi1(DailyModel m, Action enter, Func<bool> play)
        {
            var root = UiFactory.Rect("DailyModal", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _dailyModal = root;
            Ui1.Dimmer(root, CloseDaily);

            // yerlesim 02: panel 929×1267, üst y 394 (merkez −67); tarih 432×123; kapat sağ üst; sandık 324×326; düğme 734×173.
            float W = Ui3.Ready ? 860f : 929f; const float H = 1267f; // ui3/02: genişlik 860
            var panel = Ui1.Panel("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -67f), new Vector2(W, H));
            var c = panel.rectTransform;

            // Tarih kapsülü (takvim + tarih).
            var date = Ui1.Chip("Date", c, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(432f, 123f));
            var cal = Decor("Calendar", date.transform, CalendarIcon());
            cal.preserveAspect = true;
            UiFactory.Place(cal.rectTransform, new Vector2(0f, 0.5f), new Vector2(82f, 2f), new Vector2(78f, 78f));
            var dt = Ui1.Label("Day", date.transform, m.DayId ?? "", 50, Ui1.Navy);
            UiFactory.Place(dt.rectTransform, new Vector2(0f, 0.5f), new Vector2(265f, 2f), new Vector2(270f, 80f));
            FitText(dt, 24);
            if (Ui3.Ready) Ui1.Round("Back", c, "icon-close", new Vector2(1f, 1f), new Vector2(-24f, -16f), 96f, CloseDaily);
            else Ui1.Round("Back", c, "icon-close", new Vector2(1f, 1f), new Vector2(-71f, -48f), 130f, CloseDaily);

            var title = Ui1.Label("Title", c, Loc.T("daily.title"), 86, Ui1.Navy);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -256f), new Vector2(820f, 110f));
            FitText(title, 36);

            float y = -316f;
            if (m.Available && m.TierMin > 0)
            {
                var tier = Ui1.Chip("Tier", c, new Vector2(0.5f, 1f), new Vector2(0f, y - 46f), new Vector2(760f, 92f));
                string s = Loc.F("daily.tier", m.TierMin, m.TierMax);
                int dot = s.LastIndexOf('·');
                string rich = dot > 0 ? s.Substring(0, dot + 1) + "<color=#E2463A>" + s.Substring(dot + 1) + "</color>" : s;
                var tt = Ui1.Label("TierText", tier.transform, rich, 40, Ui1.Purple);
                tt.richText = true;
                UiFactory.Stretch(tt.rectTransform, 36, 36, 6, 8);
                tt.enableAutoSizing = true; tt.fontSizeMax = 40; tt.fontSizeMin = 22;
                y -= 120f;
            }

            // Sandık (dekor) solda; sağda durum ve taş satırı (gerçek günlük durumundan).
            var chest = Ui1.Decor("Chest", c, "chest");
            UiFactory.Place(chest.rectTransform, new Vector2(0.5f, 1f), new Vector2(-227f, y - 200f), new Vector2(324f, 326f));
            string status;
            if (!m.Available) status = Loc.T("daily.none");
            else if (m.Won) status = Loc.T("daily.status.won");
            else if (!m.Entered) status = Loc.T("daily.status.locked");
            else if (m.AttemptOpen) status = Loc.T("daily.status.open");
            else if (m.AttemptsLeft > 0) status = Loc.F("daily.status.left", m.AttemptsLeft, m.AttemptsAllowed);
            else status = Loc.T("daily.status.none");
            var lines = new List<string> { status, m.StoneName != null ? Loc.F("daily.stone", m.StoneName) : Loc.T("daily.nostone") };
            if (m.BonusToday) lines.Add(Loc.T("daily.bonus.active"));
            float ly = y - 90f;
            foreach (var line in lines)
            {
                var tick = UiFactory.Image("Tick", c, UiArt.Icon(UiIcon.Check), Ui1.TealDark);
                tick.raycastTarget = false;
                tick.preserveAspect = true;
                UiFactory.Place(tick.rectTransform, new Vector2(0.5f, 1f), new Vector2(-30f, ly), new Vector2(46f, 46f));
                var lt = Ui1.Label("Line", c, line, 42, Ui1.Navy, true, TextAnchor.MiddleLeft);
                UiFactory.Place(lt.rectTransform, new Vector2(0.5f, 1f), new Vector2(182f, ly), new Vector2(364f, 110f)); // panel iç kenarında 40 px pay (de: satırlar kenara dayanıyordu)
                lt.enableAutoSizing = true; lt.fontSizeMax = 42; lt.fontSizeMin = 24;
                ly -= 116f;
            }
            if (lines.Count > 0 && lines[0] == status) c.Find("Line").name = "Status"; // test/okunurluk adı
            y = Mathf.Min(y - 420f, ly + 20f);

            // Yıldızlı ayraç.
            for (int side = -1; side <= 1; side += 2)
            {
                var ln = UiFactory.Image("Line", c, UiArt.Box, Ui1.Gold);
                ln.raycastTarget = false;
                UiFactory.Place(ln.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 230f, y), new Vector2(340f, 4f));
            }
            var star = Decor("Star", c, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(64f, 64f));

            // Ana eylem: giriş (reklamla ya da reklamsız) / oyna / devam — mevcut hak durumuna göre; hak uydurulmaz.
            string label = null; Action act = null; bool ad = false;
            if (m.Available && !m.Won)
            {
                if (!m.Entered) { label = m.AdFree ? Loc.T("daily.enter.free") : Loc.T("daily.enter.ad"); act = enter; ad = !m.AdFree; }
                else if (m.AttemptOpen || m.AttemptsLeft > 0) { label = m.AttemptOpen ? Loc.T("daily.resume") : Loc.T("daily.play"); act = () => play(); }
            }
            if (label != null)
                Ui1.Button(!m.Entered ? "Enter" : "Play", c, label, true, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(734f, 173f), act,
                    ad ? UiArt.Icon(UiIcon.Play) : null, 58);
            // "Prototip" notu kaldırıldı (2026-11-11): mağaza sürümünde oyuncuya görünüyordu.
        }

        // ---- 05 Ayarlar -----------------------------------------------------------------------------------------

        private void ShowSettingsUi1(bool sound, bool vibration, Action back, Action onLevels)
        {
            if (Ui3.Ready) { ShowSettingsUi3(sound, vibration, back, onLevels); return; } // ui3/05
            var page = NewPage(RankArt("bg"));
            Ui1.Round("Back", page, "icon-back", new Vector2(0f, 1f), new Vector2(108f, -130f), 140f, onLevels ?? back);
            Ui1.Header(page, Loc.T("ui.settings"), new Vector2(0.5f, 1f), new Vector2(0f, -278f), new Vector2(648f, 190f), 84);
            // yerlesim 05: panel 940×826 (üst y 518); üç satır 864×202 çerçevesiz, aralarında ince ayraç; simge ~130.
            const float W = 940f, H = 826f, rowH = 202f;
            var panel = Ui1.Panel("Card", page, new Vector2(0.5f, 1f), new Vector2(0f, -518f - H * 0.5f), new Vector2(W, H));
            var c = panel.rectTransform;
            float y = 254f;
            SwitchRowUi1(c, y, "icon-sound", Loc.T("ui.sound"), sound, v => SoundChanged?.Invoke(v));
            SettingsDivider(c, 129f);
            y = 4f;
            SwitchRowUi1(c, y, "icon-vibration", Loc.T("ui.vibration"), vibration, v => VibrationChanged?.Invoke(v));
            SettingsDivider(c, -125f);
            y = -255f;
            var lrow = UiFactory.Rect("Language", c);
            UiFactory.Place(lrow, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(864f, rowH));
            var lic = Ui1.Decor("Icon", lrow.transform, "icon-language");
            UiFactory.Place(lic.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 2f), new Vector2(132f, 132f));
            var lt = Ui1.Label("Label", lrow.transform, Loc.T("ui.language"), 58, Ui1.Navy, true, TextAnchor.MiddleLeft);
            UiFactory.Place(lt.rectTransform, new Vector2(0f, 0.5f), new Vector2(190f + 130f, 2f), new Vector2(260f, 100f));
            FitText(lt, 30);
            var pick = Ui1.Button("Pick", lrow.transform, ArabicShaper.ShapeLines(Loc.Current.NativeName) + "  ›", true, new Vector2(1f, 0.5f), new Vector2(-185f, 2f), new Vector2(370f, 116f), ShowLanguagePicker, null, 52);
            var note = Ui1.Label("Note", page, Loc.T("ui.settingsnote"), 30, Color.white, false);
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -518f - H - 70f), new Vector2(W - 60f, 100f));
            note.enableAutoSizing = true; note.fontSizeMax = 30; note.fontSizeMin = 22;
            var nsh = note.gameObject.AddComponent<Shadow>(); nsh.effectColor = new Color(0.03f, 0.2f, 0.35f, 0.7f); nsh.effectDistance = new Vector2(0f, -2f);
            var ver = Ui1.Label("Version", page, "Stone Burst " + Application.version + "  ·  OFIGAME", 26, new Color(1f, 1f, 1f, 0.85f), false);
            UiFactory.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, BarBottom + BarH + 50f), new Vector2(800f, 40f));
            Tabs(page, 2, back, onLevels ?? back, () => { });
        }

        /// <summary>Krem satır: solda simge, yazı; sağda turkuaz/gri anahtar. Satırın tamamı dokunulur (≥96 birim).</summary>
        /// <summary>ui1/05: ayar satırları arasında ince altın ayraç.</summary>
        private static void SettingsDivider(RectTransform card, float y)
        {
            var d = UiFactory.Image("Divider", card, UiArt.Box, new Color(0.8f, 0.62f, 0.34f, 0.45f));
            d.raycastTarget = false;
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(780f, 4f));
        }

        private void SwitchRowUi1(RectTransform card, float y, string iconKey, string label, bool initial, Action<bool> changed)
        {
            bool state = initial;
            // ui1/05: çerçevesiz satır 864×202 (ayraçlar SettingsDivider), simge 132, anahtar 190×96 (durum yazısız; düğme konumu).
            var row = UiFactory.SpriteButton(label, card, null, new Vector2(864f, 202f), null);
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(864f, 202f));
            var icon = Ui1.Decor("Icon", row.transform, iconKey);
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(90f, 2f), new Vector2(132f, 132f));
            var t = Ui1.Label("Label", row.transform, label, 58, Ui1.Navy, true, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(190f + 200f, 2f), new Vector2(400f, 100f));
            FitText(t, 28);
            var track = UiFactory.Image("Track", row.transform, UiArt.Box, Ui1.Teal, true);
            track.raycastTarget = false;
            track.pixelsPerUnitMultiplier = UiArt.Box.border.x / 48f;
            UiFactory.Place(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(-125f, 2f), new Vector2(190f, 96f));
            var ol = track.gameObject.AddComponent<Outline>();
            ol.effectColor = Ui1.Gold; ol.effectDistance = new Vector2(3f, -3f);
            var knob = UiFactory.Image("Knob", track.transform, UiArt.Circle, Color.white);
            knob.raycastTarget = false;
            var ks = knob.gameObject.AddComponent<Shadow>();
            ks.effectColor = new Color(0f, 0f, 0f, 0.25f); ks.effectDistance = new Vector2(0f, -3f);
            var stateText = Ui1.Label("State", track.transform, "", 22, Color.white);
            UiFactory.Stretch(stateText.rectTransform);
            stateText.textWrappingMode = TextWrappingModes.NoWrap;
            stateText.enableAutoSizing = true; stateText.fontSizeMax = 22; stateText.fontSizeMin = 13;
            stateText.enabled = false; // ui1/05: tasarımda anahtar yazısız (metin erişilebilirlik için nesnede kalır)
            void Refresh()
            {
                track.color = state ? Ui1.Teal : new Color32(0xC9, 0xBF, 0xB2, 0xFF);
                UiFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(state ? 47f : -47f, 0f), new Vector2(80f, 80f));
                Loc.Set(stateText, state ? Loc.T("ui.on") : Loc.T("ui.off"));
                stateText.rectTransform.offsetMin = new Vector2(state ? 8f : 76f, 0f);
                stateText.rectTransform.offsetMax = new Vector2(state ? -76f : -8f, 0f);
                stateText.color = state ? Color.white : new Color(0.29f, 0.21f, 0.18f, 0.75f);
            }
            row.onClick.AddListener(() => { state = !state; Refresh(); changed(state); });
            PressFeedback.Attach(row, null, 80f, 120f);
            Refresh();
        }

        // ---- 06 ZPE istatistik (harita üstünde modal) -------------------------------------------------------------

        private void ShowZpeStatsUi1(ZpeStatsModel m)
        {
            var root = UiFactory.Rect("ZpeModal", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _zpeModal = root;
            Ui1.Dimmer(root, CloseZpe);
            // yerlesim 06: panel 950×1286 (üst y 374); kapat sağ üst; büyük "78/100" (kapsülsüz); beş bileşen satırı
            // (solda turkuaz simge dairesi, kalın çubuk, değer; aralarında ayraç); yıldızlı ayraç; not alanı.
            float W = Ui3.Ready ? 860f : 950f; const float H = 1286f; // ui3/06: genişlik 860
            var panel = Ui1.Panel("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 960f - 374f - H * 0.5f), new Vector2(W, H));
            var c = panel.rectTransform;
            if (Ui3.Ready) Ui1.Round("Back", c, "icon-close", new Vector2(1f, 1f), new Vector2(-24f, -16f), 96f, CloseZpe);
            else Ui1.Round("Back", c, "icon-close", new Vector2(1f, 1f), new Vector2(-81f, -39f), 130f, CloseZpe);
            var title = Ui1.Label("Title", c, "ZPE", 84, Ui1.Navy);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(700f, 100f));
            var full = Ui1.Label("FullName", c, Loc.T("zpe.title"), 30, Ui1.Ink2, false);
            UiFactory.Place(full.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 36f));
            FitText(full, 20);
            var big = Ui1.Label("Score", c, m.Calibrating ? Loc.F("zpe.calibrating", m.Count, m.Min) : m.Score + "<size=55%><color=#4A5D72>/100</color></size>", m.Calibrating ? 52 : 150, Ui1.TealDark);
            big.richText = true;
            UiFactory.Place(big.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -275f), new Vector2(700f, 160f));
            big.enableAutoSizing = true; big.fontSizeMax = m.Calibrating ? 52 : 150; big.fontSizeMin = 30;
            if (Ui3.Ready)
            {
                // ui3/06: değer (180,148,500,112) zpeValue 88 teal, fildişi plakada.
                var plate = U3Plate("ScorePlate", c, 56f, 112f);
                UiFactory.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(500f, 112f));
                plate.transform.SetSiblingIndex(big.transform.GetSiblingIndex());
                big.fontSizeMax = m.Calibrating ? 44 : 88; big.color = Ui3.Teal;
                UiFactory.Place(big.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(480f, 104f));
            }
            var parts = new (string key, float v, UiIcon icon)[]
            {
                ("zpe.part.efficiency", m.Efficiency, UiIcon.Ring), ("zpe.part.difficulty", m.Difficulty, UiIcon.Lock),
                ("zpe.part.firsttry", m.FirstTry, UiIcon.Check), ("zpe.part.time", m.Time, UiIcon.Play),
                ("zpe.part.noassist", m.NoAssist, UiIcon.Undo),
            };
            float y = -407f;
            for (int i = 0; i < parts.Length; i++)
            {
                var (key, v, ic) = parts[i];
                var row = UiFactory.Rect("Part", c);
                UiFactory.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Ui3.Ready ? 760f : 821f, 116f));
                if (Ui3.Ready)
                {
                    // ui3: bileşen satırı fildişi, 2 px kum kontur, yarıçap 28 (748×88 ölçüsüne yakın; mevcut satır aralığı).
                    var rbg = Ui1.Row("RowBg", row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 100f));
                    rbg.transform.SetAsFirstSibling();
                }
                var disc = UiFactory.Image("IconDisc", row, UiArt.Circle, Ui1.TealDark); // ui3: tealBottom
                disc.raycastTarget = false;
                UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(92f, 92f));
                var dic = UiFactory.Image("Icon", disc.transform, UiArt.Icon(ic), Color.white);
                dic.raycastTarget = false;
                UiFactory.Stretch(dic.rectTransform, 20, 20, 20, 20);
                var lab = Ui1.Label("Label", row, Loc.T(key), 40, Ui1.Navy, true, TextAnchor.MiddleLeft);
                UiFactory.Place(lab.rectTransform, new Vector2(0f, 0.5f), new Vector2(118f + 170f, 2f), new Vector2(340f, 80f));
                FitText(lab, 24);
                var tr = UiFactory.Image("Track", row, UiArt.Box, Ui3.Ready ? Ui3.Track : (Color)new Color32(0xE8, 0xDA, 0xC4, 0xFF), true);
                tr.raycastTarget = false;
                tr.pixelsPerUnitMultiplier = UiArt.Box.border.x / 22f;
                if (Ui3.Ready) UiFactory.Place(tr.rectTransform, new Vector2(1f, 0.5f), new Vector2(-196f, 0f), new Vector2(196f, 28f)); // ui3: çubuk adın alanına girmez
                else UiFactory.Place(tr.rectTransform, new Vector2(1f, 0.5f), new Vector2(-235f, 0f), new Vector2(250f, 44f));
                if (v > 0.01f)
                {
                    var fl = UiFactory.Image("Fill", tr.transform, UiArt.Box, Ui1.Teal, true);
                    fl.raycastTarget = false;
                    fl.pixelsPerUnitMultiplier = UiArt.Box.border.x / 19f;
                    fl.rectTransform.anchorMin = Vector2.zero; fl.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
                    fl.rectTransform.offsetMin = new Vector2(3f, 3f); fl.rectTransform.offsetMax = new Vector2(-3f, -3f);
                }
                var pct = Ui1.Label("Pct", row, Mathf.RoundToInt(Mathf.Clamp01(v) * 100f).ToString(), 44, Ui1.Navy, true, TextAnchor.MiddleRight);
                UiFactory.Place(pct.rectTransform, new Vector2(1f, 0.5f), new Vector2(-50f, 2f), new Vector2(100f, 70f));
                if (i < parts.Length - 1 && !Ui3.Ready)
                {
                    var d = UiFactory.Image("Divider", c, UiArt.Box, new Color(0.8f, 0.62f, 0.34f, 0.35f));
                    d.raycastTarget = false;
                    UiFactory.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 61f), new Vector2(760f, 3f));
                }
                y -= 122f;
            }
            // Yıldızlı ayraç.
            float sy = y + 20f;
            for (int side = -1; side <= 1; side += 2)
            {
                var ln = UiFactory.Image("Line", c, UiArt.Box, Ui3.Ready ? Ui3.Brass : Ui1.Gold);
                ln.raycastTarget = false;
                UiFactory.Place(ln.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 220f, sy), new Vector2(340f, 4f));
            }
            var star = Decor("Star", c, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, sy), new Vector2(56f, 56f));
            // Not alanı: eğilim + sayımlar tek satır, IQ değildir notu.
            string sub = m.Calibrating ? Loc.F("zpe.stat.calib", Mathf.Max(0, m.Min - m.Count)) : Loc.F("zpe.stat.trend", (m.Trend > 0 ? "+" : "") + m.Trend);
            var st = Ui1.Label("Sub", c, sub, 34, Ui1.Navy, true);
            UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, sy - 62f), new Vector2(800f, 50f));
            FitText(st, 22);
            string stats = Loc.F("zpe.stat.levels", m.Count) + "  ·  " + Loc.F("zpe.stat.win", m.WinPct) + "  ·  " + Loc.F("zpe.stat.first", m.FirstPct);
            var sl = Ui1.Label("Stat", c, stats, 28, Ui1.Ink2, false);
            UiFactory.Place(sl.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, sy - 114f), new Vector2(740f, 46f)); // 840 kart kenarına taşıyordu (2026-11-11)
            FitText(sl, 18);
            var note = Ui1.Label("Note", c, Loc.T("zpe.note"), 34, Ui1.Navy, true);
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(800f, 100f));
            note.enableAutoSizing = true; note.fontSizeMax = 34; note.fontSizeMin = 22;
        }

        // ---- 08 Sıralama puanı bilgisi (sıralama üstünde modal) ---------------------------------------------------

        private void ShowRankInfoUi1()
        {
            var root = UiFactory.Rect("RankInfo", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _rankInfo = root;
            Ui1.Dimmer(root, CloseRankInfo);
            // yerlesim 08: panel 972 genişlik, üst y 432; kupa ~340; satırlar 821 genişlik; Tamam 486×115. Kural metni korunduğu
            // için panel 1380 boyunda.
            float W = Ui3.Ready ? 860f : 972f; const float H = 1380f; // ui3/08: genişlik 860
            var c = Ui1.Panel("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 960f - 432f - H * 0.5f), new Vector2(W, H)).rectTransform;
            if (Ui3.Ready) Ui1.Round("Close", c, "icon-close", new Vector2(1f, 1f), new Vector2(-24f, -16f), 96f, CloseRankInfo);
            else Ui1.Round("Close", c, "icon-close", new Vector2(1f, 1f), new Vector2(-80f, -57f), 130f, CloseRankInfo);
            var t = Ui1.Label("Title", c, Loc.T("rank.score"), 72, Ui1.Navy);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(760f, 96f));
            FitText(t, 32);
            var cup = Decor("Trophy", c, RankArt("trophy"));
            cup.preserveAspect = true;
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -285f), new Vector2(340f, 250f));
            var intro = Ui1.Label("Intro", c, Loc.T("rank.info.intro"), 38, Ui1.Navy, false);
            UiFactory.Place(intro.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -472f), new Vector2(840f, 110f));
            intro.enableAutoSizing = true; intro.fontSizeMax = 38; intro.fontSizeMin = 22;
            var rows = new (string title, string desc, string pct, Sprite icon)[]
            {
                (Loc.T("rank.part.zpe"), Loc.T("rank.part.zpe.desc"), "%50", Ui1.Art("icon-zpe")),
                (Loc.T("rank.part.level"), Loc.T("rank.part.level.desc"), "%30", RuleIcons.For("exact")),
                (Loc.T("rank.part.stars"), Loc.T("rank.part.stars.desc"), "%20", PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite),
            };
            float y = -630f;
            foreach (var r in rows)
            {
                var row = Ui1.Row("Part", c, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(840f, 160f));
                var ic = UiFactory.Image("Icon", row.transform, r.icon, Color.white);
                ic.preserveAspect = true; ic.raycastTarget = false;
                UiFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(92f, 2f), new Vector2(100f, 100f));
                var rt = Ui1.Label("Name", row.transform, r.title, 46, Ui1.Navy, true, TextAnchor.MiddleLeft);
                UiFactory.Place(rt.rectTransform, new Vector2(0f, 0.5f), new Vector2(170f + 220f, 14f), new Vector2(440f, 54f));
                FitText(rt, 24);
                var rd = Ui1.Label("Desc", row.transform, r.desc, 32, Ui1.Ink2, false, TextAnchor.MiddleLeft);
                UiFactory.Place(rd.rectTransform, new Vector2(0f, 0.5f), new Vector2(170f + 220f, -30f), new Vector2(440f, 44f));
                FitText(rd, 18);
                var pill = UiFactory.Image("Pct", row.transform, UiArt.Box, Ui1.Teal, true);
                pill.raycastTarget = false;
                UiFactory.Place(pill.rectTransform, new Vector2(1f, 0.5f), new Vector2(-118f, 2f), new Vector2(176f, 84f));
                var pt = Ui1.Label("Text", pill.transform, r.pct, 46, Color.white);
                UiFactory.Stretch(pt.rectTransform);
                y -= 170f;
            }
            var rules = Ui1.Label("Rules", c, Loc.T("rank.info.rules"), 32, Ui1.Ink2, false);
            UiFactory.Place(rules.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y + 38f), new Vector2(820f, 90f));
            rules.enableAutoSizing = true; rules.fontSizeMax = 32; rules.fontSizeMin = 20;
            Ui1.Button("Ok", c, Loc.T("btn.ok"), true, new Vector2(0.5f, 0f), new Vector2(0f, 165f), new Vector2(560f, 130f), CloseRankInfo, null, 54);
        }

        // ---- 09 Deneme bölümleri ---------------------------------------------------------------------------------

        private void ShowTrialsUi1(IList<TrialItem> items, Action<int> pick)
        {
            var root = UiFactory.Rect("TrialModal", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _trialModal = root;
            var trialDim = Ui1.Dimmer(root, CloseTrials);
            // ui3/09: deneme listesi tam sayfa; alt menü (harita sayfasının) karartılmadan görünür.
            if (Ui3.Ready) ((RectTransform)trialDim.transform).offsetMin = new Vector2(0f, 400f + ShellBarH);
            var pageBg = Ui3.Ready ? U3SageBg() : RankArt("bg");
            if (pageBg != null)
            {
                // ui1/09: tam sayfa sahil zemini (harita başlığı arkadan görünmez); alt menü üstte kalır.
                var bgi = UiFactory.Image("PageBg", root, pageBg, Color.white);
                bgi.preserveAspect = false;
                bgi.raycastTarget = true;
                UiFactory.Stretch(bgi.rectTransform, 200f, 200f, 400f, 400f + ShellBarH);
            }
            var body = UiFactory.Rect("Body", root);
            UiFactory.Stretch(body, 200f, 200f, 400f, 400f); // güvenli alan içi
            Ui1TopBar(body, CloseTrials, null); // yerlesim 09: üst satır haritayla aynı; başlık 821×154 (y 250), açıklama 821×115
            Ui1.Ribbon(body, Loc.T("trial.list.title"), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(821f, 154f), 60);
            var sub = Ui1.Decor("SubPlate", body, "header-ivory");
            sub.preserveAspect = false;
            UiFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(860f, 125f));
            var st = Ui1.Label("Sub", sub.transform, Loc.T("trial.list.sub"), 34, Ui1.Navy, false);
            UiFactory.Stretch(st.rectTransform, 80, 80, 14, 20);
            st.enableAutoSizing = true; st.fontSizeMax = 34; st.fontSizeMin = 20;

            var viewport = UiFactory.Rect("Viewport", body);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(30f, ShellBarH + 10f);
            viewport.offsetMax = new Vector2(-20f, -470f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            const float rowH = 190f, gap = 14f;
            float y = -6f;
            foreach (var it in items)
            {
                int idx = it.Index;
                var row = UiFactory.SpriteButton("Trial" + (idx + 1), content, null, new Vector2(1000f, rowH), () => { CloseTrials(); pick?.Invoke(idx); });
                row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 1f), new Vector2(0f, y - rowH * 0.5f), new Vector2(1000f, rowH));
                PressFeedback.Attach(row, null, 80f, 120f);
                Color ink = Ui1.Navy, ink2 = Ui1.Ink2;
                if (it.Boss)
                {
                    var rb = Ui1.Decor("Bg", row.transform, "ribbon-purple");
                    rb.preserveAspect = false;
                    UiFactory.Stretch(rb.rectTransform, -10f, -10f, -6f, -6f);
                    ink = Color.white; ink2 = new Color(1f, 0.92f, 1f, 0.9f);
                }
                else
                {
                    var bg = Ui1.Row("Bg", row.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, rowH));
                    if (it.Done) bg.color = new Color(0.9f, 1f, 0.94f);
                }
                var disc = UiFactory.Image("IconDisc", row.transform, UiArt.Circle, it.Boss ? Ui1.Gold : Ui1.Purple);
                disc.raycastTarget = false;
                UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f, 2f), new Vector2(140f, 140f));
                var ring = disc.gameObject.AddComponent<Outline>(); ring.effectColor = Ui1.Gold; ring.effectDistance = new Vector2(5f, -5f);
                var ic = UiFactory.Image("Rule", disc.transform, RuleIcons.For(it.Rules.Count > 0 ? it.Rules[0] : "exact"), Color.white);
                ic.raycastTarget = false;
                UiFactory.Stretch(ic.rectTransform, 18f, 18f, 18f, 18f);
                var nt = Ui1.Label("Name", row.transform, (idx + 1) + ". " + it.Title, 48, ink, true, TextAnchor.MiddleLeft);
                UiFactory.Place(nt.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f + 300f, 30f), new Vector2(600f, 78f));
                // Arapçada (sağdan sola) üç nokta kesmesi satırı tamamen siliyordu (2026-11-09 dil denetimi): sığmazsa küçülür.
                nt.textWrappingMode = TextWrappingModes.NoWrap;
                nt.overflowMode = Loc.IsRtl ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
                nt.enableAutoSizing = true; nt.fontSizeMax = 48; nt.fontSizeMin = 28;
                Loc.Set(nt, (idx + 1) + ". " + it.Title);
                var it2 = Ui1.Label("Intro", row.transform, it.Intro ?? "", 32, ink2, false, TextAnchor.MiddleLeft);
                UiFactory.Place(it2.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f + 300f, -32f), new Vector2(600f, 56f));
                it2.enableAutoSizing = true; it2.fontSizeMax = 32; it2.fontSizeMin = 20;
                if (it.Done)
                {
                    var ok = UiFactory.Image("Done", row.transform, UiArt.Circle, UiArt.CheckGreen);
                    ok.raycastTarget = false;
                    UiFactory.Place(ok.rectTransform, new Vector2(1f, 0.5f), new Vector2(-190f, 2f), new Vector2(60f, 60f));
                    var mk = UiFactory.Image("Mark", ok.transform, UiArt.Icon(UiIcon.Check), Color.white);
                    mk.raycastTarget = false;
                    UiFactory.Stretch(mk.rectTransform, 8, 8, 8, 8);
                }
                var arrow = UiFactory.Image("Arrow", row.transform, UiArt.Circle, Ui1.Teal);
                arrow.raycastTarget = false;
                UiFactory.Place(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(-96f, 2f), new Vector2(96f, 96f));
                var at = Ui1.Label("Chevron", arrow.transform, "›", 72, Color.white);
                UiFactory.Stretch(at.rectTransform, 0, 0, 0, 8);
                y -= rowH + gap;
            }
            content.sizeDelta = new Vector2(1000f, -y);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
        }

        // ---- 03 Koleksiyon ---------------------------------------------------------------------------------------

        private const float U1CardW = 455f, U1CardH = 420f, U1CardGap = 24f, U1CollTop = 470f;

        private void ShowCollectionUi1(IList<StoneItem> items, Action back, Action settings)
        {
            var page = NewPage(Ui3.Ready ? U3SageBg() : RankArt("bg") ?? CollArt("bg"));
            Ui1TopBar(page, back, settings);
            Ui1.Ribbon(page, Loc.T("collection.title"), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(820f, 150f), 58, Ui3.Ready ? "ribbon-teal" : "ribbon-purple");

            // Yapışkan kategori satırı: yıldız + kategori adı + sayaç (kaydırınca güncellenir).
            var cat = Ui1.Row("CategoryCapsule", page, new Vector2(0.5f, 1f), new Vector2(0f, -U1CollTop + 60f), new Vector2(980f, 110f));
            var cstar = Decor("Star", cat.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            cstar.preserveAspect = true;
            UiFactory.Place(cstar.rectTransform, new Vector2(0f, 0.5f), new Vector2(84f, 2f), new Vector2(70f, 70f));
            var ctitle = Ui1.Label("CategoryTitle", cat.transform, "", 44, Ui1.Navy, true, TextAnchor.MiddleLeft);
            UiFactory.Place(ctitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(140f + 270f, 2f), new Vector2(540f, 80f));
            FitText(ctitle, 26);
            var cntBg = UiFactory.Image("CounterCapsule", cat.transform, UiArt.Box, Ui3.Ready ? Ui3.TabSelected : Ui1.Purple, true);
            cntBg.raycastTarget = false;
            UiFactory.Place(cntBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(-120f, 2f), new Vector2(170f, 70f));
            var ccount = Ui1.Label("Count", cntBg.transform, "", 40, Color.white);
            UiFactory.Stretch(ccount.rectTransform, 10, 10, 4, 4);
            FitText(ccount, 24);

            // Liste krem panelin içinde (tasarım): kategori satırı panelin üst kenarına biner.
            var listPanel = Ui3.Ready ? Ui1.Sliced("ListPanel", page, "card-ivory", 50f) : Ui1.Sliced("ListPanel", page, "panel-modal", 60f);
            var lp = listPanel.rectTransform;
            lp.anchorMin = Vector2.zero; lp.anchorMax = Vector2.one;
            lp.offsetMin = new Vector2(16f, ShellBarH + 4f);
            lp.offsetMax = new Vector2(-16f, -U1CollTop + 110f);
            listPanel.transform.SetSiblingIndex(cat.transform.GetSiblingIndex());
            // Kaydırma alanı: kategori satırının altından alt menünün üstüne.
            var viewport = UiFactory.Rect("Viewport", page);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(34f, ShellBarH + 30f);
            viewport.offsetMax = new Vector2(-34f, -U1CollTop - 10f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            float y = -10f;
            string section = null;
            int col = 0;
            var tops = new List<(float y, string title, int owned, int total)>();
            foreach (var it in items)
            {
                if (it.Section != section)
                {
                    if (col != 0) { y -= U1CardH + U1CardGap; col = 0; }
                    int owned = 0, total = 0;
                    foreach (var o in items) if (o.Section == it.Section) { total++; if (o.Owned) owned++; }
                    if (section != null)
                    {
                        var sep = Ui1.Row("Section", content, new Vector2(0.5f, 1f), new Vector2(0f, y - 50f), new Vector2(900f, 96f));
                        var st = Ui1.Label("SectionTitle", sep.transform, it.SectionTitle, 40, Ui1.Purple);
                        UiFactory.Stretch(st.rectTransform, 60, 60, 4, 8);
                        FitText(st, 24);
                        y -= 120f;
                    }
                    tops.Add((y, it.SectionTitle, owned, total));
                    section = it.Section;
                }
                CollCardUi1(content, it, new Vector2((col == 0 ? -1 : 1) * (U1CardW + U1CardGap) * 0.5f, y - U1CardH * 0.5f), StonePreviewRequested);
                if (++col == 2) { col = 0; y -= U1CardH + U1CardGap; }
            }
            if (col != 0) y -= U1CardH + U1CardGap;
            content.sizeDelta = new Vector2(2f * U1CardW + U1CardGap, -y + 20f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            scroll.decelerationRate = 0.12f;
            _collectionScroll = scroll;

            int shown = -1;
            void SyncHeader()
            {
                if (tops.Count == 0) return;
                float top = content.anchoredPosition.y + 40f;
                int idx = 0;
                for (int k = 0; k < tops.Count; k++) if (-tops[k].y <= top) idx = k;
                if (idx == shown) return;
                shown = idx;
                Loc.Set(ctitle, tops[idx].title);
                Loc.Set(ccount, Loc.F("collection.count", tops[idx].owned, tops[idx].total));
            }
            scroll.onValueChanged.AddListener(_ => SyncHeader());
            SyncHeader();

            Tabs(page, TabCollection, back, back, null);
        }

        /// <summary>
        /// ui1 koleksiyon kartı: krem kart; kazanılmış taş net + "Koleksiyonda"; kilitli taş yoğun bulanık + ortada kilit +
        /// açılma koşulu (veriden). Dokunma: yalnız kazanılmış kart vitrini açar.
        /// </summary>
        private static void CollCardUi1(RectTransform content, StoneItem it, Vector2 pos, Action<string> preview)
        {
            var card = UiFactory.Rect("Item_" + it.Id, content);
            UiFactory.Place(card, new Vector2(0.5f, 1f), pos, new Vector2(U1CardW, U1CardH));
            if (it.Owned && preview != null)
            {
                var h = card.gameObject.AddComponent<Image>();
                h.color = new Color(0f, 0f, 0f, 0f);
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                string id = it.Id;
                btn.onClick.AddListener(() => preview(id));
                PressFeedback.Attach(btn, null, 80f, 120f);
            }
            // yerlesim 03: kart 464×394; görsel kartın üst ~%70'ini doldurur, ad kapsülü görselin altına biner,
            // altta simgeli tek satır durum (onay + Koleksiyonda / yıldız + eşik).
            var bg = Ui1.Card("Card", card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(U1CardW, U1CardH));
            if (it.Owned)
            {
                var well = UiFactory.Image("Well", card, UiArt.Box, it.Warm ? new Color32(0xFF, 0xD9, 0x8A, 0xFF) : new Color32(0x7F, 0xD8, 0xE6, 0xFF), true);
                well.raycastTarget = false;
                well.pixelsPerUnitMultiplier = UiArt.Box.border.x / 34f;
                UiFactory.Place(well.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(U1CardW - 44f, 260f));
                var halo = Decor("Halo", card, SpecialArt.Glow);
                halo.color = it.Warm ? new Color(1f, 0.85f, 0.4f, 0.75f) : new Color(0.6f, 0.95f, 1f, 0.75f);
                UiFactory.Place(halo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(380f, 300f));
            }
            if (!it.Owned && !it.Hidden) KatKat.View.ResourceUsage.Note("Collection/stones_blur/" + it.Id);
            var blur = !it.Owned && !it.Hidden ? Resources.Load<Sprite>("Collection/stones_blur/" + it.Id) : null;
            var art = Decor("Icon", card, blur != null ? blur : it.Sprite);
            art.preserveAspect = true;
            art.color = it.Owned ? Color.white : it.Hidden ? new Color(0.12f, 0.10f, 0.20f, 0.55f) : Color.white;
            UiFactory.Place(art.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), it.Owned ? new Vector2(300f, 270f) : new Vector2(U1CardW - 44f, 270f));
            if (!it.Owned && blur != null) art.preserveAspect = false; // kilitli: bulanık görsel üst alanı doldurur
            if (!it.Owned)
            {
                var lk = Decor("Lock", card, PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Back));
                lk.preserveAspect = true;
                UiFactory.Place(lk.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(118f, 118f));
            }
            if (it.Selected)
            {
                var sel = Decor("Active", card, UiArt.Circle);
                sel.color = Ui1.Teal;
                UiFactory.Place(sel.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -34f), new Vector2(28f, 28f));
            }
            var pill = Ui1.Chip("NamePill", card, new Vector2(0.5f, 1f), new Vector2(0f, -272f), new Vector2(360f, 84f));
            var n = Ui1.Label("Name", pill.transform, it.Name, 42, Ui1.Navy);
            UiFactory.Stretch(n.rectTransform, 30, 30, 4, 8);
            n.enableAutoSizing = true; n.fontSizeMax = 42; n.fontSizeMin = 22;
            bool starLine = !it.Owned && it.Stars > 0;
            string line = it.Owned ? Loc.T("collection.owned") : starLine ? Loc.F("collection.unlock.stars", it.Stars) : it.Desc;
            bool icon = it.Owned || starLine;
            var d = Ui1.Label("Desc", card, line, 34, it.Owned ? Ui1.TealDark : Ui1.Navy, icon, TextAnchor.MiddleCenter);
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(icon ? 24f : 0f, -352f), new Vector2(U1CardW - (icon ? 100f : 40f), 70f));
            d.enableAutoSizing = true; d.fontSizeMax = 34; d.fontSizeMin = 18;
            if (icon)
            {
                // Simge metnin hemen solunda (metin genişliğine göre).
                d.ForceMeshUpdate();
                float tw = Mathf.Min(d.preferredWidth, U1CardW - 100f);
                var ic = Decor("StatusIcon", card, it.Owned ? UiArt.Icon(UiIcon.Check) : PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
                ic.preserveAspect = true;
                if (it.Owned) ic.color = new Color32(0x2F, 0xB3, 0x5A, 0xFF);
                UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(24f - tw * 0.5f - 30f, -352f), new Vector2(46f, 46f));
            }
        }
    }
}
