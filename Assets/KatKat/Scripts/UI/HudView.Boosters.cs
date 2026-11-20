using System;
using System.Collections.Generic;
using KatKat.Meta;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Güçlendiriciler (2026-11-20; kullanıcı: Candy Crush'taki alt alan gibi, "+3 Hamle", "Taş Değiştir", "Bomba"; külçeyle
    /// alınır). Alt şeritte tepsinin (oyunda kazanılan roket/Altın Rulo) sağında; tepsiden ayrılsın diye turkuaz pirinç
    /// diskli. Sağ altta adet rozeti (0'da "+", dokununca satın alma penceresi), açılmamışta kilit. İlk güçlendirici
    /// açılana kadar (bölüm 11) hiç görünmez. Şerit kalabalıkta bütün öğeler birlikte küçülür (<see cref="LayoutBottom"/>).
    /// </summary>
    public sealed partial class HudView
    {
        public event Action<Booster> BoosterPressed;

        private readonly Button[] _boosters = new Button[3];
        private readonly Image[] _boosterIcons = new Image[3], _boosterLocks = new Image[3], _boosterSel = new Image[3], _boosterAura = new Image[3];
        private readonly Text[] _boosterCounts = new Text[3];
        private readonly Image[] _boosterBadges = new Image[3];
        private readonly Twinkle[] _boosterTwinkle = new Twinkle[3];
        private readonly bool[] _boosterReady = new bool[3];
        private readonly float[] _boosterPop = { 9f, 9f, 9f };
        private RectTransform _boosterRoot;
        private Image _boosterPlate;
        private int _boosterSelected = -1;
        private float _bottomScale = 1f, _boosterScale = 1f, _extraScale = 1f;
        // 2026-11-20: 118 → 140 (kullanıcı: "iconlar küçük"), sonra 172 denendi, "çok büyük olmuşlar" → 150, "makul ölçülerde büyük" → 132 (eski görünen boy ~118).
        private const float BoosterSize = 132f, BoosterGap = 28f, BoosterLift = 8f, ExtraStep = 200f;
        private static readonly Color BoosterGlow = new Color(1f, 0.86f, 0.38f, 1f);

        private void BuildBoosters(RectTransform bottom)
        {
            _boosterRoot = UiFactory.Rect("Boosters", bottom);
            UiFactory.Stretch(_boosterRoot);
            // 2026-11-20 (kullanıcı: güçlendiricileri "candy crush görselindeki gibi bir zemin üzerine" → "yalnız güçlendirici altına"):
            // fildişi seramik plaka, pirinç kenar; konum ve boy LayoutBottom'da düğmelere göre.
            _boosterPlate = MenuView.U3Plate("BoosterPlate", _boosterRoot, 44f, 150f);
            _boosterPlate.raycastTarget = false;
            _boosterPlate.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                var b = ShopCatalog.All[i];
                var btn = UiFactory.SpriteButton("Booster_" + ShopCatalog.Key(b), _boosterRoot, Ui3.Art("ikon-dugmesi") ?? UiArt.Circle,
                    new Vector2(BoosterSize, BoosterSize), () => BoosterPressed?.Invoke(b));
                btn.GetComponent<Image>().preserveAspect = true;
                UiFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, BoosterLift), new Vector2(BoosterSize, BoosterSize));
                // Eldeki güçlendiricinin arkasında nabız atan altın ışıltı (kullanıcı: "elimizde olanlar büyüyüp küçülsün ışıltılı olsun").
                var aura = UiFactory.Image("Aura", btn.transform, SpecialArt.Glow, Color.clear);
                aura.raycastTarget = false;
                UiFactory.Place(aura.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * BoosterSize * 1.75f);
                aura.transform.SetAsFirstSibling();
                var sel = UiFactory.Image("Sel", btn.transform, SpecialArt.Glow, new Color(1f, 0.85f, 0.35f, 0f));
                sel.raycastTarget = false;
                UiFactory.Place(sel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * BoosterSize * 1.9f);
                sel.transform.SetAsFirstSibling();
                var icon = UiFactory.Image("Icon", btn.transform, MenuView.ShopArt(ShopCatalog.Key(b)), Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.one * BoosterSize * 1.02f);
                var tw = Twinkle.Attach(icon.rectTransform, 3, 44f, 0.48f);
                var lk = UiFactory.Image("Lock", btn.transform, PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Back), Color.white);
                lk.preserveAspect = true;
                lk.raycastTarget = false;
                UiFactory.Place(lk.rectTransform, new Vector2(1f, 0f), new Vector2(-20f, 22f), new Vector2(60f, 60f));
                var badge = UiFactory.Image("CountBadge", btn.transform, Ui3.Rounded(26f, Ui3.TealTop, Ui3.TealBottom, 3f, Ui3.Brass), Color.white, true);
                badge.pixelsPerUnitMultiplier = 1f;
                badge.raycastTarget = false;
                UiFactory.Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-14f, 16f), new Vector2(62f, 54f));
                var cnt = UiFactory.Title("Count", badge.transform, "0", 34, Color.white, Ui3.TealPressed);
                cnt.raycastTarget = false;
                UiFactory.Stretch(cnt.rectTransform, 2f, 2f, 2f, 4f);
                _boosters[i] = btn;
                _boosterIcons[i] = icon;
                _boosterLocks[i] = lk;
                _boosterSel[i] = sel;
                _boosterAura[i] = aura;
                _boosterTwinkle[i] = tw;
                _boosterBadges[i] = badge;
                _boosterCounts[i] = cnt;
                btn.gameObject.SetActive(false);
            }
        }

        /// <param name="visible">En az biri açılmış (bölüm ≥ 11) ve normal bölüm.</param>
        /// <param name="counts">Adetler (Booster sırasıyla).</param>
        /// <param name="unlocked">Açık mı (kilitli olanda kilit, dokununca açılış bölümü söylenir).</param>
        /// <param name="selected">Hedef seçimi bekleyen güçlendirici (Bomba, Taş Değiştir); -1 yok.</param>
        public void SetBoosters(bool visible, int[] counts, bool[] unlocked, int selected)
        {
            if (_boosterRoot == null) return;
            bool changed = false;
            _boosterSelected = selected;
            for (int i = 0; i < 3; i++)
            {
                var btn = _boosters[i];
                if (btn.gameObject.activeSelf != visible) { btn.gameObject.SetActive(visible); changed = true; }
                if (_boosterPlate != null) _boosterPlate.gameObject.SetActive(visible);
                if (!visible) continue;
                bool open = unlocked != null && unlocked[i];
                int n = counts != null ? counts[i] : 0;
                _boosterLocks[i].gameObject.SetActive(!open);
                _boosterBadges[i].gameObject.SetActive(open);
                _boosterCounts[i].text = n > 0 ? n.ToString() : "+";
                _boosterIcons[i].color = open ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                ((Image)btn.targetGraphic).color = open ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                bool sel = i == selected;
                _boosterSel[i].color = new Color(1f, 0.85f, 0.35f, sel ? 0.95f : 0f);
                _boosterReady[i] = open && n > 0;
                _boosterTwinkle[i].SetOn(_boosterReady[i] || sel);
                if (!_boosterReady[i] && !sel) _boosterAura[i].color = Color.clear;
                btn.transform.localScale = Vector3.one * _boosterScale * (sel ? TraySelScale : 1f);
            }
            if (changed) LayoutBottom();
        }

        /// <summary>Testler: güçlendirici düğmesi görünür mü, rozet metni.</summary>
        public bool BoostersShown => _boosters[0] != null && _boosters[0].gameObject.activeSelf;
        public string BoosterCountText(int i) => _boosterCounts[i] != null ? _boosterCounts[i].text : null;
        public void PressBoosterForTest(Booster b) => _boosters[(int)b].onClick.Invoke();

        // ---- "Bomba bitti" penceresi (tasarım docs/yeni_v2 "Bomba Bitti_ Güçlendirici Mağazası") ---------------------

        private RectTransform _offerCard;
        public bool BoosterOfferShown => _offerCard != null;

        /// <summary>Güçlendirici bitti: simge, açıklama, külçe bakiyesi, "[külçe] 60 ile al" ve "Mağazaya git".</summary>
        public void ShowBoosterOffer(Booster b, int ingots, Action buy, Action goShop)
        {
            CloseBoosterOffer();
            const float W = 860f, H = 860f;
            var card = MenuView.U3Modal(_safe, "BoosterOfferCard", W, H, CloseBoosterOffer, out var root);
            _offerCard = root;
            var icon = UiFactory.Image("Icon", card, MenuView.ShopArt(ShopCatalog.Key(b)), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            MenuView.U3In(icon.rectTransform, W, 300f, 40f, 260f, 240f);
            var title = UiFactory.Text("Title", card, Loc.T("booster." + ShopCatalog.Key(b) + ".empty"), 56, Ui3.Ink);
            title.raycastTarget = false;
            MenuView.U3In(title.rectTransform, W, 56f, 292f, 748f, 84f);
            title.enableAutoSizing = true; title.fontSizeMax = 56; title.fontSizeMin = 30;
            var info = UiFactory.Text("Info", card, Loc.T("booster." + ShopCatalog.Key(b) + ".desc"), 32, Ui3.InkSecondary, TextAnchor.MiddleCenter, FontStyle.Normal);
            info.raycastTarget = false;
            info.textWrappingMode = TMPro.TextWrappingModes.Normal;
            MenuView.U3In(info.rectTransform, W, 70f, 384f, 720f, 100f);
            info.enableAutoSizing = true; info.fontSizeMax = 32; info.fontSizeMin = 20;
            var bal = MenuView.U3Plate("Balance", card, 36f, 76f);
            MenuView.U3In(bal.rectTransform, W, 300f, 498f, 260f, 76f);
            var bi = UiFactory.Image("Ingot", bal.transform, MenuView.ShopArt("ingot"), Color.white);
            bi.preserveAspect = true;
            bi.raycastTarget = false;
            UiFactory.Place(bi.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(60f, 60f));
            var bt = UiFactory.Text("Value", bal.transform, ingots.ToString(), 42, Ui3.Ink);
            bt.raycastTarget = false;
            UiFactory.Place(bt.rectTransform, new Vector2(0f, 0.5f), new Vector2(170f, 0f), new Vector2(150f, 60f));
            int price = ShopCatalog.Price(b);
            // 2026-11-20: fiyat düğmesi yeşil gradyan, büyüyüp küçülür (mağazadakiyle aynı).
            var buyBtn = Ui1.PriceButton("Buy", card, Loc.F("booster.buy", price), new Vector2(0.5f, 1f), new Vector2(0f, -(600f + 56f)), new Vector2(748f, 112f),
                () => { CloseBoosterOffer(); buy?.Invoke(); }, MenuView.ShopArt("ingot"), 44);
            if (ingots < price) buyBtn.gameObject.AddComponent<CanvasGroup>().alpha = 0.6f;
            Ui1.Button("Shop", card, Loc.T("booster.goshop"), false, new Vector2(0.5f, 1f), new Vector2(0f, -(730f + 46f)), new Vector2(748f, 92f),
                () => { CloseBoosterOffer(); goShop?.Invoke(); }, null, 38);
        }

        public void CloseBoosterOffer()
        {
            if (_offerCard != null) UiFactory.Kill(_offerCard.gameObject);
            _offerCard = null;
        }

        public void PressBoosterOfferForTest(bool buy)
        {
            if (_offerCard == null) return;
            _offerCard.Find("Card/" + (buy ? "Buy" : "Shop")).GetComponent<Button>().onClick.Invoke();
        }

        // ---- Canlılık ve kullanım efekti (kullanıcı 2026-11-20: "güçlendiricilerin kullanımı ve kullandıktan sonraki
        // animasyonları çok pasif kalıyor. Daha efektif daha görsel şölenli olsun. Altta durdukları yerde de elimizde olanlar
        // büyüyüp küçülsün ışıltılı olsun") --------------------------------------------------------------------------------

        private RectTransform _uiFxLayer;

        private sealed class UiP
        {
            public Image Img;
            public Vector2 Pos, Vel;
            public float Age, Life, S0, S1, Rot, RotVel, Grav, Drag, Delay;
            public Color C0, C1;
        }
        private readonly List<UiP> _uiP = new List<UiP>();

        private sealed class Flyer
        {
            public RectTransform Rt;
            public Vector2 From, Ctrl, To;
            public float Age, Dur;
            public Action Done;
        }
        private readonly List<Flyer> _flyers = new List<Flyer>();
        private float _movesPop = 9f;

        /// <summary>Etkin efekt sayısı (testler).</summary>
        public int UiFxCount => _uiP.Count + _flyers.Count;

        private RectTransform FxLayer()
        {
            if (_uiFxLayer == null)
            {
                _uiFxLayer = UiFactory.Rect("UiFx", _safe);
                UiFactory.Stretch(_uiFxLayer);
            }
            _uiFxLayer.SetAsLastSibling();
            if (_offerCard != null) _offerCard.SetAsLastSibling();
            return _uiFxLayer;
        }

        private Vector2 FxLocal(Transform t) => FxLayer().InverseTransformPoint(t.position);

        private void EmitUi(Sprite sp, Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c0, Color c1,
            float grav = 0f, float drag = 0f, float rotVel = 0f, float delay = 0f)
        {
            var img = UiFactory.Image("P", FxLayer(), sp, Color.clear);
            img.raycastTarget = false;
            img.preserveAspect = true;
            var p = new UiP { Img = img, Pos = pos, Vel = vel, Life = life, S0 = s0, S1 = s1, C0 = c0, C1 = c1, Grav = grav, Drag = drag,
                Rot = UnityEngine.Random.Range(0f, 90f), RotVel = rotVel, Delay = delay };
            ApplyUi(p);
            _uiP.Add(p);
        }

        private static void ApplyUi(UiP p)
        {
            var rt = p.Img.rectTransform;
            if (p.Age < p.Delay) { p.Img.color = Color.clear; return; }
            float t = Mathf.Clamp01((p.Age - p.Delay) / p.Life);
            float s = Mathf.Lerp(p.S0, p.S1, t);
            rt.localPosition = p.Pos;
            rt.sizeDelta = new Vector2(s, s);
            rt.localRotation = Quaternion.Euler(0f, 0f, p.Rot);
            p.Img.color = Color.Lerp(p.C0, p.C1, t * t);
        }

        private void TickUiParticles(float dt)
        {
            for (int i = _uiP.Count - 1; i >= 0; i--)
            {
                var p = _uiP[i];
                p.Age += dt;
                if (p.Img == null || p.Age >= p.Delay + p.Life) { if (p.Img != null) UiFactory.Kill(p.Img.gameObject); _uiP.RemoveAt(i); continue; }
                if (p.Age >= p.Delay)
                {
                    p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                    p.Vel += Vector2.down * p.Grav * dt;
                    p.Pos += p.Vel * dt;
                    p.Rot += p.RotVel * dt;
                }
                ApplyUi(p);
            }
            for (int i = _flyers.Count - 1; i >= 0; i--)
            {
                var f = _flyers[i];
                f.Age += dt;
                if (f.Rt == null) { _flyers.RemoveAt(i); continue; }
                float t = Mathf.Clamp01(f.Age / f.Dur);
                float e = Ease.InOutQuad(t);
                var a = Vector2.Lerp(f.From, f.Ctrl, e);
                var b = Vector2.Lerp(f.Ctrl, f.To, e);
                f.Rt.localPosition = Vector2.Lerp(a, b, e);
                float s = t < 0.25f ? Mathf.Lerp(0.5f, 1.35f, Ease.OutBack(t / 0.25f)) : Mathf.Lerp(1.35f, 0.75f, (t - 0.25f) / 0.75f);
                f.Rt.localScale = new Vector3(s, s, 1f);
                // Uçarken arkasında altın yıldız izi.
                if (!Motion.Reduced && UnityEngine.Random.value < dt * 40f)
                    EmitUi(FxKit.Star, f.Rt.localPosition, UnityEngine.Random.insideUnitCircle * 60f, 0.45f, 34f, 4f,
                        new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.7f, 0.2f, 0f), rotVel: 180f);
                if (t >= 1f)
                {
                    UiFactory.Kill(f.Rt.gameObject);
                    _flyers.RemoveAt(i);
                    f.Done?.Invoke();
                }
            }
            if (_movesPop < 1f && _moves != null)
            {
                _movesPop = Mathf.Min(1f, _movesPop + dt / 0.45f);
                float s = 1f + 0.55f * Mathf.Sin(_movesPop * Mathf.PI) * (1f - _movesPop * 0.4f);
                _moves.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        private void TickBoosterFx(float dt)
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < 3; i++)
            {
                var btn = _boosters[i];
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;
                bool sel = i == _boosterSelected;
                bool live = (_boosterReady[i] || sel) && !Motion.Reduced;
                float w = Mathf.Sin((t / 1.4f + i * 0.33f) * 2f * Mathf.PI);
                float pop = 1f, rot = 0f;
                if (_boosterPop[i] < 1f)
                {
                    _boosterPop[i] = Mathf.Min(1f, _boosterPop[i] + dt / 0.6f);
                    float k = _boosterPop[i];
                    pop = 1f + 0.55f * Mathf.Sin(k * Mathf.PI) * (1f - 0.5f * k);
                    rot = 16f * Mathf.Sin(k * Mathf.PI * 3f) * (1f - k);
                }
                float breath = live ? 1f + (sel ? 0.1f : 0.075f) * w : 1f;
                _boosterIcons[i].rectTransform.localScale = Vector3.one * breath * pop;
                _boosterIcons[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
                if (live)
                {
                    var c = BoosterGlow;
                    c.a = (sel ? 0.8f : 0.55f) + 0.25f * w;
                    _boosterAura[i].color = c;
                    _boosterAura[i].rectTransform.localScale = Vector3.one * (1f + 0.12f * w);
                }
                if (sel) _boosterSel[i].color = new Color(1f, 0.85f, 0.35f, 0.7f + 0.25f * w);
            }
            TickUiParticles(dt);
        }

        /// <summary>
        /// Güçlendirici seçildi ya da kullanıldı: simge zıplayıp sallanır, düğmeden altın şok halkası ve dışa saçılan yıldızlar;
        /// <paramref name="big"/> (kullanım anı) daha çok ve daha uzağa.
        /// </summary>
        public void PlayBoosterBurst(Booster b, bool big)
        {
            int i = (int)b;
            if (i < 0 || i >= 3 || _boosters[i] == null || !_boosters[i].gameObject.activeInHierarchy) return;
            _boosterPop[i] = 0f;
            if (Motion.Reduced) return;
            var at = FxLocal(_boosters[i].transform);
            float k = big ? 1f : 0.6f;
            EmitUi(SpecialArt.Glow, at, Vector2.zero, 0.45f, 120f, 420f * k, new Color(1f, 0.95f, 0.7f, 0.95f), new Color(1f, 0.7f, 0.2f, 0f));
            EmitUi(FxKit.Ring, at, Vector2.zero, 0.5f, 60f, 520f * k, new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.6f, 0.2f, 0f));
            if (big) EmitUi(FxKit.Ring, at, Vector2.zero, 0.65f, 40f, 380f, new Color(1f, 1f, 1f, 0.8f), new Color(1f, 0.85f, 0.4f, 0f), delay: 0.1f);
            int n = big ? 26 : 12;
            for (int j = 0; j < n; j++)
            {
                float a = (j + UnityEngine.Random.Range(-0.3f, 0.3f)) / n * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.35f);
                var col = Color.HSVToRGB(Mathf.Repeat(0.11f + UnityEngine.Random.Range(-0.05f, 0.08f), 1f), 0.45f, 1f);
                EmitUi(FxKit.Star, at, dir * UnityEngine.Random.Range(380f, 820f) * k, UnityEngine.Random.Range(0.55f, 0.85f), UnityEngine.Random.Range(34f, 62f), 6f,
                    col, new Color(col.r, col.g, col.b, 0f), grav: 900f, drag: 2.2f, rotVel: UnityEngine.Random.Range(-260f, 260f), delay: UnityEngine.Random.Range(0f, 0.06f));
            }
        }

        /// <summary>
        /// +3 Hamle kullanıldı: düğmeden "+3" yazısı yıldız iziyle hamle sayacına uçar; varınca sayaç zıplar ve orada ikinci bir
        /// ışık patlaması olur. <paramref name="arrived"/> varışta çağrılır (sayı o an güncellenir).
        /// </summary>
        public void PlayMovesGain(int amount, Action arrived)
        {
            PlayBoosterBurst(Booster.Moves, true);
            if (_moves == null || _boosters[0] == null || Motion.Reduced) { _movesPop = 0f; arrived?.Invoke(); return; }
            var from = FxLocal(_boosters[0].transform);
            var to = FxLocal(_moves.transform);
            var label = UiFactory.Title("Gain", FxLayer(), "+" + amount, 96, new Color(1f, 0.93f, 0.55f), new Color32(0x6A, 0x3A, 0x08, 0xFF));
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(260f, 140f);
            label.rectTransform.localPosition = from;
            var ctrl = new Vector2((from.x + to.x) * 0.5f + 260f, (from.y + to.y) * 0.5f);
            _flyers.Add(new Flyer
            {
                Rt = label.rectTransform, From = from, Ctrl = ctrl, To = to, Dur = 0.85f,
                Done = () =>
                {
                    _movesPop = 0f;
                    arrived?.Invoke();
                    EmitUi(SpecialArt.Glow, to, Vector2.zero, 0.5f, 100f, 360f, new Color(1f, 0.95f, 0.7f, 1f), new Color(1f, 0.7f, 0.2f, 0f));
                    EmitUi(FxKit.Ring, to, Vector2.zero, 0.5f, 60f, 420f, new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.6f, 0.2f, 0f));
                    for (int j = 0; j < 18; j++)
                    {
                        float a = j / 18f * Mathf.PI * 2f;
                        EmitUi(FxKit.Star, to, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(300f, 560f), 0.6f, 44f, 6f,
                            new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.6f, 0.2f, 0f), drag: 2.5f, rotVel: 200f);
                    }
                },
            });
        }

        /// <summary>Testler: uçan yazıları ve parçacıkları hemen bitirir.</summary>
        public void FinishUiFxForTest()
        {
            for (int k = 0; k < 200 && UiFxCount > 0; k++) TickUiParticles(0.1f);
        }
    }
}
