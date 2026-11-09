using System;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Oyun üst kartındaki hareketli maskot (kullanıcı referansı 2026-11-05): hamle hapının üstünden bakan turkuaz
    /// kafa, kırmızı yaka, hapı tutan krem eller. Parçalar ayrı çizilir ki canlandırılabilsin:
    ///   - Gözler etrafa bakar (göz bebekleri kayar), yüz bakış yönüne kayar ve kafa hafif eğilir: "başını çevirme".
    ///   - Ara sıra göz kırpar, sürekli hafifçe nefes alır, yanındaki parıltılar yanıp söner.
    ///   - Excite(seviye): zıplar, ağzı büyür, kaşları kalkar, parıltılar patlar (kombo, özel taş, yıldız).
    ///   - Worried: az hamlede kaşlar endişeli, ağız küçük.
    /// Bütün zaman unscaled; kural durumuna dokunmaz. Parçalar koddan çizilir (Resources gerektirmez).
    /// </summary>
    public sealed class MascotView : MonoBehaviour
    {
        private RectTransform _root, _body, _face, _mouth;
        private readonly RectTransform[] _eyes = new RectTransform[2], _pupils = new RectTransform[2], _brows = new RectTransform[2];
        private readonly RectTransform[] _sparkles = new RectTransform[2];
        private readonly Image[] _sparkleImg = new Image[2];
        private Vector2 _rootHome;

        private float _clock, _gaze, _gazeTarget, _gazeY, _gazeYTarget, _nextLook = 1.2f;
        private float _blinkT = 9f, _nextBlink = 2.6f;
        private float _exciteT = 9f, _exciteDur = 0.9f;
        private int _exciteLevel;
        private bool _worried;
        private float _worry;
        private readonly System.Random _rnd = new System.Random(7);

        /// <param name="parent">Üst kart.</param>
        /// <param name="anchor">Hamle hapının üst kenarının ortası (kart koordinatı).</param>
        /// <param name="handsParent">Ellerin çizileceği katman (hapın önünde).</param>
        public static MascotView Create(RectTransform parent, Vector2 anchor, RectTransform handsParent, float width = 155f)
        {
            var root = UiFactory.Rect("Mascot", parent);
            UiFactory.Place(root, new Vector2(0.5f, 0.5f), anchor, new Vector2(10f, 10f));
            var m = root.gameObject.AddComponent<MascotView>();
            m._root = root;
            m._rootHome = anchor;
            m._width = width;
            m.Build(handsParent, anchor);
            return m;
        }

        // ---- Kristal maskot (docs/Kristal-Maskot-Claude, 2026-11-05) -------------------------------------------
        // Gövde: PremiumV4/mascot-crystal/mascot-body-v1 (kıvrım, yanaklar, eller, ayaklar gövdede; yüz boş).
        // Göz akı ×2, göz bebeği ×2, tek kaş ve ağız ayrı parçalar; yerleri onaylı tam görselden ölçüldü
        // (iki PNG aynı dikdörtgenle kırpıldı: 893×984, y aşağı). Eski eller ve yanaklar bu dalda çizilmez.
        private const float CW = 893f, CH = 984f;
        private bool _crystal;
        private float _k = 1f; // görüntü birimi / kaynak pikseli
        private readonly Vector2[] _eyeHome = new Vector2[2], _pupilHome = new Vector2[2], _eyeSize = new Vector2[2], _pupilSize = new Vector2[2];
        private Vector2 _browHome, _mouthHome;

        /// <summary>Kırpılmış kaynak pikselinin (y aşağı) gövde merkezine göre yerel konumu.</summary>
        private Vector2 C(float px, float py) => new Vector2((px - CW * 0.5f) * _k, (CH * 0.5f - py) * _k);

        private float _width = 155f;

        /// <summary>Ayakların tabanı ile anchor arasındaki yükseklik (haritada düğüm üstüne oturtmak için).</summary>
        public static float FeetBelowAnchor(float width) => (984f - 770f) * width / 893f;

        private bool BuildCrystal(RectTransform handsParent, Vector2 anchor)
        {
            var bodySprite = View.PremiumArt.V4("mascot-crystal/mascot-body-v1");
            if (bodySprite == null) return false;
            _crystal = true;
            // Ölçek: en yüksek zıplamada kıvrımın tepesi güvenli alanda kalır (kıvrım tepesi y≈15, hap kenarı y≈770).
            float wd = _width;
            _k = wd / CW;
            float hd = CH * _k;
            var body = UiFactory.Image("Body", _root, bodySprite, Color.white);
            body.preserveAspect = true;
            // İnci yüz kartın kremiyle karışmasın: ince sıcak kenar + yumuşak alt gölge.
            var edge = body.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.55f, 0.36f, 0.2f, 0.45f);
            edge.effectDistance = new Vector2(1.5f, -1.5f);
            var drop = body.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(0.3f, 0.18f, 0.08f, 0.3f);
            drop.effectDistance = new Vector2(0f, -4f);
            var rt = body.rectTransform;
            float centerY = (770f - CH * 0.5f) * _k;
            UiFactory.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, centerY), new Vector2(wd, hd));
            rt.pivot = new Vector2(0.5f, (CH - 800f) / CH); // eğilme mor tabanın üstünden
            rt.anchoredPosition = new Vector2(0f, centerY + (rt.pivot.y - 0.5f) * hd);
            _body = rt;

            _face = UiFactory.Rect("Face", _body);
            UiFactory.Place(_face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(wd, hd));

            // Gözler (asimetrik: sol büyük yuvarlak, sağ dar ve uzun).
            // Kullanıcı: "gözü ve ağzı hep sol tarafına bakıyor; hareketliyken doğru baksın". Onaylı görseldeki bakış
            // sağ üste sabitti; burada göz bebekleri göz ortasından başlar, bakış yönünü Tick verir. Gözler aynı
            // boyda (uzaktaki göz bakışla daralır), konumlar onaylı görselden.
            _eyeHome[0] = C(298f, 495f); _eyeSize[0] = new Vector2(196f, 206f) * _k;
            _eyeHome[1] = C(588f, 495f); _eyeSize[1] = new Vector2(196f, 206f) * _k;
            _pupilHome[0] = Vector2.zero; _pupilSize[0] = new Vector2(104f, 104f) * _k;
            _pupilHome[1] = Vector2.zero; _pupilSize[1] = new Vector2(104f, 104f) * _k;
            for (int s = 0; s < 2; s++)
            {
                var eye = UiFactory.Image("Eye", _face, CrystalEyeSprite, Color.white);
                UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0.5f), _eyeHome[s], _eyeSize[s]);
                _eyes[s] = eye.rectTransform;
                var pupil = UiFactory.Image("Pupil", eye.rectTransform, CrystalPupilSprite, Color.white);
                UiFactory.Place(pupil.rectTransform, new Vector2(0.5f, 0.5f), _pupilHome[s], _pupilSize[s]);
                _pupils[s] = pupil.rectTransform;
            }
            _browHome = C(588f, 374f);
            var brow = UiFactory.Image("Brow", _face, CrystalBrowSprite, Color.white);
            UiFactory.Place(brow.rectTransform, new Vector2(0.5f, 0.5f), _browHome, new Vector2(118f, 68f) * _k);
            _brows[1] = brow.rectTransform;
            _mouthHome = C(443f, 655f); // iki gözün ortasının altı
            var mouth = UiFactory.Image("Mouth", _face, CrystalMouthSprite, Color.white);
            UiFactory.Place(mouth.rectTransform, new Vector2(0.5f, 0.5f), _mouthHome, new Vector2(176f, 105f) * _k);
            _mouth = mouth.rectTransform;

            // Parıltılar kıvrımın ucunda (kıvrım ucu ≈ (729, 29)).
            var tip = new Vector2(0f, centerY) + C(729f, 29f);
            for (int i = 0; i < 2; i++)
            {
                var sp = UiFactory.Image("Sparkle", handsParent, SparkleSprite, Color.white);
                UiFactory.Place(sp.rectTransform, new Vector2(0.5f, 0.5f), anchor + tip + (i == 0 ? new Vector2(30f, 4f) : new Vector2(52f, -22f)),
                    i == 0 ? new Vector2(30f, 30f) : new Vector2(22f, 22f));
                _sparkles[i] = sp.rectTransform;
                _sparkleImg[i] = sp;
            }
            foreach (var g in _root.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            foreach (var s in _sparkleImg) s.raycastTarget = false;
            Tick(0f);
            return true;
        }

        private void TickCrystal(float blink, float joy, bool excited)
        {
            _face.anchoredPosition = new Vector2(_gaze * 5f * _k / 0.17f, joy * 2f);
            for (int s = 0; s < 2; s++)
            {
                // Kapak yalnız göz grubunda; heyecanda gözler hafif kısılır; bakılan yönün tersindeki göz daralır.
                float side = s == 0 ? -1f : 1f;
                float far = Mathf.Clamp01(-_gaze * side) * 0.16f;
                _eyes[s].localScale = new Vector3(1f - far, blink * (1f - joy * 0.25f), 1f);
                // Bakış: göz bebeği göz akının iç elipsi içinde kalır.
                float rx = _eyeSize[s].x * 0.5f * 0.9f - _pupilSize[s].x * 0.5f;
                float ry = _eyeSize[s].y * 0.5f * 0.9f - _pupilSize[s].y * 0.5f;
                var o = _pupilHome[s] + new Vector2(_gaze * rx * 0.9f, _gazeY * ry * 0.8f);
                var n = new Vector2(o.x / Mathf.Max(1f, rx), o.y / Mathf.Max(1f, ry));
                if (n.magnitude > 1f) { n.Normalize(); o = new Vector2(n.x * rx, n.y * ry); }
                _pupils[s].anchoredPosition = o;
            }
            // Kaş: sevinçte kalkar; endişede iç ucu kalkıp eğilir.
            var brow = _brows[1];
            brow.anchoredPosition = _browHome + new Vector2(_gaze * 2f, (joy * 9f + _worry * 4f) * _k / 0.17f);
            brow.localRotation = Quaternion.Euler(0f, 0f, -_worry * 18f + joy * 4f);
            // Ağız: sevinçte büyür; endişede daralıp yassılaşır.
            // Ağız simetrik; bakış yönüne kayar ve o yana hafif eğilir (yüz dönüyormuş gibi).
            _mouth.localScale = new Vector3(1f + joy * 0.2f - _worry * 0.35f, 1f + joy * 0.3f - _worry * 0.55f, 1f);
            _mouth.anchoredPosition = _mouthHome + new Vector2(_gaze * 14f * _k / 0.17f, 0f);
            _mouth.localRotation = Quaternion.Euler(0f, 0f, -_gaze * 10f);
        }

        private void Build(RectTransform handsParent, Vector2 anchor)
        {
            if (BuildCrystal(handsParent, anchor)) return;
            var body = UiFactory.Image("Body", _root, BodySprite, Color.white);
            UiFactory.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 66f), new Vector2(188f, 188f));
            body.raycastTarget = false;
            _body = body.rectTransform;
            _body.pivot = new Vector2(0.5f, 0.12f); // eğilme boyundan (yakadan) olur
            _body.anchoredPosition = new Vector2(0f, 66f - 188f * 0.38f);

            _face = UiFactory.Rect("Face", _body);
            UiFactory.Place(_face, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(188f, 188f));

            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? -1f : 1f;
                var cheek = UiFactory.Image("Cheek", _face, View.SpecialArt.Glow, new Color(1f, 0.52f, 0.52f, 0.8f));
                UiFactory.Place(cheek.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(side * 58f, -6f), new Vector2(50f, 32f));
                var eye = UiFactory.Image("Eye", _face, EyeSprite, Color.white);
                UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(side * 27f, 20f), new Vector2(44f, 52f));
                _eyes[s] = eye.rectTransform;
                var pupil = UiFactory.Image("Pupil", eye.rectTransform, PupilSprite, Color.white);
                UiFactory.Place(pupil.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
                _pupils[s] = pupil.rectTransform;
                var brow = UiFactory.Image("Brow", _face, UiArt.Box, new Color32(0x3B, 0x2A, 0x26, 0xFF), true);
                brow.pixelsPerUnitMultiplier = UiArt.Box.border.x / 4.5f;
                UiFactory.Place(brow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(side * 27f, 56f), new Vector2(34f, 11f));
                _brows[s] = brow.rectTransform;
            }
            var mouth = UiFactory.Image("Mouth", _face, MouthSprite, Color.white);
            UiFactory.Place(mouth.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(58f, 42f));
            mouth.rectTransform.pivot = new Vector2(0.5f, 0.85f);
            mouth.rectTransform.anchoredPosition = new Vector2(0f, -14f + 42f * 0.35f);
            _mouth = mouth.rectTransform;

            // Eller hapın önünde, hapın üst kenarını tutar (kafa zıplasa da yerinde kalır).
            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? -1f : 1f;
                var hand = UiFactory.Image("Hand", handsParent, HandSprite, Color.white);
                UiFactory.Place(hand.rectTransform, new Vector2(0.5f, 0.5f), anchor + new Vector2(side * 60f, 2f), new Vector2(62f, 50f));
                hand.raycastTarget = false;
            }
            // Parıltılar: kafanın sağ üstünde iki dört uçlu yıldız.
            for (int i = 0; i < 2; i++)
            {
                var sp = UiFactory.Image("Sparkle", handsParent, SparkleSprite, Color.white);
                var pos = anchor + (i == 0 ? new Vector2(112f, 128f) : new Vector2(140f, 102f));
                UiFactory.Place(sp.rectTransform, new Vector2(0.5f, 0.5f), pos, i == 0 ? new Vector2(40f, 40f) : new Vector2(30f, 30f));
                sp.raycastTarget = false;
                _sparkles[i] = sp.rectTransform;
                _sparkleImg[i] = sp;
            }
            foreach (var g in _root.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            Tick(0f);
        }

        /// <summary>Heyecan: 1 küçük sevinç, 2 büyük (iki zıplama), 3 çok büyük (üç zıplama + parıltı patlaması).</summary>
        public void Excite(int level)
        {
            level = Mathf.Clamp(level, 1, 3);
            if (_exciteT < _exciteDur && level < _exciteLevel) return;
            _exciteLevel = level;
            _exciteDur = 0.55f + 0.3f * level;
            _exciteT = 0f;
            _gazeTarget = 0f;
            _gazeYTarget = 0.4f;
        }

        public void SetWorried(bool worried) => _worried = worried;

        private void Update() => Tick(Time.unscaledDeltaTime);

        /// <summary>Elle ilerletme (oynatma dışı ekran yakalama).</summary>
        public void Advance(float ms) => Tick(ms / 1000f);

        private float Rand(float a, float b) => a + (float)_rnd.NextDouble() * (b - a);

        private void Tick(float dt)
        {
            if (_root == null) return;
            _clock += dt;
            bool excited = _exciteT < _exciteDur;

            // Etrafa bakma: birkaç saniyede bir yeni yön (sol / orta / sağ, bazen tahtaya aşağı).
            _nextLook -= dt;
            if (_nextLook <= 0f && !excited)
            {
                int pick = _rnd.Next(5);
                _gazeTarget = pick == 0 ? -1f : pick == 1 ? 1f : pick == 2 ? 0f : Rand(-0.6f, 0.6f);
                _gazeYTarget = _rnd.Next(4) == 0 ? -1f : Rand(-0.2f, 0.3f);
                _nextLook = Rand(1.6f, 3.4f);
            }
            float k = 1f - Mathf.Exp(-dt * 9f);
            _gaze = Mathf.Lerp(_gaze, _gazeTarget, k);
            _gazeY = Mathf.Lerp(_gazeY, _gazeYTarget, k);
            _worry = Mathf.MoveTowards(_worry, _worried && !excited ? 1f : 0f, dt * 3f);

            // Göz kırpma (çift kırpma bazen).
            _nextBlink -= dt;
            if (_nextBlink <= 0f) { _blinkT = 0f; _nextBlink = _rnd.Next(5) == 0 ? 0.28f : Rand(2.2f, 4.8f); }
            _blinkT += dt;
            float blink = _blinkT < 0.14f ? 1f - Mathf.Sin(_blinkT / 0.14f * Mathf.PI) * 0.92f : 1f;

            // Heyecan eğrisi: seviye kadar zıplama, ezilip uzama.
            float jump = 0f, squash = 0f, joy = 0f;
            if (excited)
            {
                _exciteT += dt;
                float u = Mathf.Clamp01(_exciteT / _exciteDur);
                joy = Mathf.Sin(u * Mathf.PI);
                float hops = _exciteLevel;
                float ph = u * hops;
                float local = ph - Mathf.Floor(ph);
                jump = Mathf.Sin(local * Mathf.PI) * (5f + 3f * _exciteLevel) * (1f - u * 0.35f);
                squash = local < 0.12f ? -(1f - local / 0.12f) * 0.08f : Mathf.Sin(local * Mathf.PI) * 0.06f;
            }

            // Nefes + zıplama.
            float breathe = Mathf.Sin(_clock * 2f * Mathf.PI / 2.6f) * 0.018f;
            _root.anchoredPosition = _rootHome + new Vector2(0f, jump);
            _body.localScale = new Vector3(1f - squash * 0.6f - breathe * 0.4f, 1f + squash + breathe, 1f);

            // Baş çevirme: yüz bakış yönüne kayar, kafa o yöne hafif eğilir.
            float wobble = excited ? Mathf.Sin(_clock * 18f) * 4f * joy : 0f;
            // Boşta gövde birkaç derece salınır (kristal), bakışla da hafif eğilir.
            float sway = _crystal ? 2.2f * Mathf.Sin(_clock * 2f * Mathf.PI / 3.4f) : 0f;
            _body.localRotation = Quaternion.Euler(0f, 0f, -_gaze * (_crystal ? 3f : 5f) + wobble + sway);
            if (_crystal) { TickCrystal(blink, joy, excited); TickSparkles(joy, excited); return; }
            _face.anchoredPosition = new Vector2(_gaze * 11f, _gazeY * 3f + joy * 3f);

            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? -1f : 1f;
                // Uzak taraftaki göz hafif daralır (dönme hissi).
                float far = Mathf.Clamp01(-_gaze * side) * 0.12f;
                float happySquint = joy * 0.35f;
                _eyes[s].localScale = new Vector3(1f - far, blink * (1f - happySquint), 1f);
                _pupils[s].anchoredPosition = new Vector2(_gaze * 8f, _gazeY * 6f + joy * 3f);
                float browUp = joy * 7f - _worry * 2f;
                _brows[s].anchoredPosition = new Vector2(side * 27f + _gaze * 2f, 56f + browUp);
                float tilt = side * (6f + joy * 6f) - side * _worry * 22f;
                _brows[s].localRotation = Quaternion.Euler(0f, 0f, -tilt);
            }
            float mouthScale = 1f + joy * 0.35f - _worry * 0.35f;
            _mouth.localScale = new Vector3(mouthScale * (1f - _worry * 0.15f), mouthScale, 1f);
            _mouth.anchoredPosition = new Vector2(_gaze * 3f, -14f + 42f * 0.35f);
            TickSparkles(joy, excited);
        }

        private void TickSparkles(float joy, bool excited)
        {
            for (int i = 0; i < 2; i++)
            {
                float tw = 0.5f + 0.5f * Mathf.Sin(_clock * (2.1f + i * 0.9f) + i * 1.7f);
                float burst = joy * (1f + 0.4f * _exciteLevel);
                float s = (0.75f + 0.25f * tw) * (1f + burst * 0.6f);
                _sparkles[i].localScale = new Vector3(s, s, 1f);
                _sparkles[i].localRotation = Quaternion.Euler(0f, 0f, (_clock * 25f + i * 30f) % 360f * (excited ? 4f : 0.3f));
                _sparkleImg[i].color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.55f + 0.45f * tw + burst));
            }
        }

        // ---- Parça çizimleri (bir kez üretilir) ----------------------------------------------------------

        private static Sprite _bodySprite, _eyeSprite, _pupilSprite, _mouthSprite, _handSprite, _sparkleSprite, _creamStar;

        private static Sprite BodySprite => _bodySprite ? _bodySprite : (_bodySprite = Make("mascot_body", 256, BodyPixel));
        private static Sprite EyeSprite => _eyeSprite ? _eyeSprite : (_eyeSprite = Make("mascot_eye", 96, EyePixel));
        private static Sprite PupilSprite => _pupilSprite ? _pupilSprite : (_pupilSprite = Make("mascot_pupil", 64, PupilPixel));
        private static Sprite MouthSprite => _mouthSprite ? _mouthSprite : (_mouthSprite = Make("mascot_mouth", 128, MouthPixel));
        private static Sprite HandSprite => _handSprite ? _handSprite : (_handSprite = Make("mascot_hand", 96, HandPixel));
        public static Sprite SparkleSprite => _sparkleSprite ? _sparkleSprite : (_sparkleSprite = Make("sparkle4", 96, SparklePixel));
        /// <summary>Henüz kazanılmamış yıldız: krem, hafif gölgeli beş köşe.</summary>
        public static Sprite CreamStar => _creamStar ? _creamStar : (_creamStar = Make("star_cream", 128, CreamStarPixel));

        // ---- Kristal yüz parçaları (renkler onaylı görselden örneklendi) ----------------------------------------

        private static Sprite _cEye, _cPupil, _cBrow, _cMouth;
        private static Sprite CrystalEyeSprite => _cEye ? _cEye : (_cEye = MakeWH("crystal_eye", 128, 128, CrystalEyePixel));
        private static Sprite CrystalPupilSprite => _cPupil ? _cPupil : (_cPupil = MakeWH("crystal_pupil", 96, 96, CrystalPupilPixel));
        private static Sprite CrystalBrowSprite => _cBrow ? _cBrow : (_cBrow = MakeWH("crystal_brow", 128, 74, CrystalBrowPixel));
        private static Sprite CrystalMouthSprite => _cMouth ? _cMouth : (_cMouth = MakeWH("crystal_mouth", 176, 105, CrystalMouthPixel));

        /// <summary>Göz akı: sıcak inci dolgu (253,234,216), ince koyu turkuaz halka (13,68,70).</summary>
        private static Color CrystalEyePixel(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float d = r - 0.485f;
            float a = Mathf.Clamp01(0.5f - d / (1.5f / 128f));
            if (a <= 0f) return Color.clear;
            var fill = Color.Lerp(new Color32(0xF1, 0xDC, 0xCB, 0xFF), new Color32(0xFE, 0xF0, 0xE2, 0xFF), Mathf.Clamp01(0.55f + y * 1.1f));
            float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.462f) / 0.024f);
            var c = Color.Lerp(fill, new Color32(0x0D, 0x44, 0x46, 0xFF), Mathf.Clamp01(ring * 1.6f));
            if (r > 0.462f) c = new Color32(0x0D, 0x44, 0x46, 0xFF);
            c.a = a;
            return c;
        }

        /// <summary>Göz bebeği: koyu kahve-siyah, sağ üstte beyaz parlama, sol altta küçük ikinci parlama.</summary>
        private static Color CrystalPupilPixel(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y) - 0.48f;
            float a = Mathf.Clamp01(0.5f - d / (1.5f / 96f));
            if (a <= 0f) return Color.clear;
            var c = Color.Lerp(new Color32(0x1C, 0x14, 0x13, 0xFF), new Color32(0x3E, 0x2C, 0x28, 0xFF), Mathf.Clamp01(0.45f - y * 0.9f));
            float h1 = Mathf.Sqrt(Sq(x - 0.17f) + Sq(y - 0.2f));
            c = Color.Lerp(c, Color.white, Mathf.Clamp01((0.115f - h1) / 0.02f));
            float h2 = Mathf.Sqrt(Sq(x + 0.18f) + Sq(y + 0.2f));
            c = Color.Lerp(c, new Color(1f, 1f, 1f), Mathf.Clamp01((0.045f - h2) / 0.02f) * 0.7f);
            c.a = a;
            return c;
        }

        /// <summary>Kaş: kalın, uçları yuvarlak koyu arduvaz yay (sol uç alçak, tepe sağa yakın, sağ uç hafif iner).</summary>
        private static Color CrystalBrowPixel(float x, float y)
        {
            var p0 = new Vector2(-0.40f, -0.13f); var p1 = new Vector2(0.06f, 0.36f); var p2 = new Vector2(0.42f, -0.08f);
            float best = 9f;
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f, u = 1f - t;
                var q = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                best = Mathf.Min(best, Vector2.Distance(new Vector2(x, y), q));
            }
            float d = best - 0.075f;
            float a = Mathf.Clamp01(0.5f - d / (1.5f / 128f));
            if (a <= 0f) return Color.clear;
            var c = Color.Lerp(new Color32(0x22, 0x31, 0x37, 0xFF), new Color32(0x3E, 0x55, 0x5E, 0xFF), Mathf.Clamp01(0.5f + y * 2f));
            c.a = a;
            return c;
        }

        /// <summary>Ağız: üst kenarı sağa doğru yükselen açık gülüş; içi mercan-kırmızı, altta pembe dil.</summary>
        private static Color CrystalMouthPixel(float x, float y)
        {
            // Simetrik açık gülüş: üst kenar hafif yukarı kıvrık düz çizgi; alt kenar elips.
            float top = 0.17f + 0.25f * x * x;
            float ell = Mathf.Sqrt(Sq(x / 0.48f) + Sq((y - 0.1f) / 0.4f)) - 1f;
            float d = Mathf.Max(ell * 0.3f, y - top);
            float a = Mathf.Clamp01(0.5f - d / (1.5f / 176f));
            if (a <= 0f) return Color.clear;
            var c = Color.Lerp(new Color32(0xB8, 0x36, 0x30, 0xFF), new Color32(0xE0, 0x55, 0x48, 0xFF), Mathf.Clamp01(0.4f + (y - top) * -1.5f));
            float tongue = Sqr(x / 0.27f) + Sqr((y + 0.17f) / 0.13f);
            if (tongue < 1f) c = Color.Lerp(c, Color.Lerp(new Color32(0xF5, 0x7A, 0x6E, 0xFF), new Color32(0xFF, 0x9C, 0x8C, 0xFF), Mathf.Clamp01(0.5f + (y + 0.17f) * 3f)), Mathf.Clamp01((1f - tongue) * 6f));
            float rim = Mathf.Clamp01(1f + d / 0.02f);
            c = Color.Lerp(c, new Color32(0x9C, 0x2C, 0x28, 0xFF), rim * 0.5f);
            c.a = a;
            return c;
        }

        private static float Sqr(float v) => v * v;

        /// <summary>w×h doku; x ∈ [−0.5, 0.5], y aynı birimde (oran korunur, yuvarlak şekil bozulmaz).</summary>
        private static Sprite MakeWH(string name, int w, int h, Func<float, float, Color> f)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                px[j * w + i] = f((i + 0.5f) / w - 0.5f, ((j + 0.5f) / h - 0.5f) * h / w);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
            sp.name = name;
            sp.hideFlags = HideFlags.DontSave;
            return sp;
        }

        private static Color BodyPixel(float x, float y)
        {
            // v6 (kullanıcı: "bizim taşlarımızın şekline daha uygun olsun"): maskot küçük bir taş yığını —
            // üstte yüzlü turkuaz taş (taşlarla aynı yuvarlak köşeli kare, alt kenarında taş kalınlığı),
            // altında yaka yerine mercan katman şeridi.
            float dTeal = RoundRect(x, y - 0.08f, 0.44f, 0.34f, 0.12f);
            float dCoral = RoundRect(x, y + 0.28f, 0.44f, 0.10f, 0.08f);
            float covTeal = Cov(dTeal, 256), covCoral = Cov(dCoral, 256);
            if (covTeal <= 0f && covCoral <= 0f) return Color.clear;

            Color coral = new Color32(0xF4, 0x68, 0x60, 0xFF), coralDark = new Color32(0xC0, 0x40, 0x3A, 0xFF), coralLight = new Color32(0xFF, 0x96, 0x86, 0xFF);
            var under = Color.Lerp(coralDark, coral, Mathf.Clamp01((y + 0.38f) / 0.1f));
            under = Color.Lerp(under, coralLight, Mathf.Clamp01(1f - Mathf.Abs(y + 0.235f) / 0.02f) * 0.5f);
            under.a = covCoral;

            Color dark = new Color32(0x12, 0x86, 0x7E, 0xFF), mid = new Color32(0x22, 0xB0, 0xA6, 0xFF), light = new Color32(0x5C, 0xD4, 0xC8, 0xFF);
            float l = Mathf.Clamp01(0.55f + (y - 0.08f) * 0.8f - x * 0.35f);
            var c = l < 0.5f ? Color.Lerp(dark, mid, l * 2f) : Color.Lerp(mid, light, (l - 0.5f) * 2f);
            // Taş kalınlığı: alt kenarda koyu dudak (taş görsellerindeki yan yüz gibi).
            float lip = Mathf.Clamp01((-0.19f - y) / 0.05f);
            c = Color.Lerp(c, dark, lip * 0.75f);
            // Kenar eğimi: üst ve sol kenar açık, sağ ve alt kenar koyu.
            float edge = Mathf.Clamp01(1f + dTeal / 0.035f);
            c = Color.Lerp(c, y - 0.08f > -x * 0.3f ? light : dark, edge * 0.5f);
            float e = Sq((x + 0.2f) / 0.15f) + Sq((y - 0.33f) / 0.045f);
            if (e < 1f) c = Color.Lerp(c, Color.white, (1f - e) * 0.5f);
            c.a = covTeal;

            // Turkuaz taş mercan katmanın önünde.
            float a = c.a + under.a * (1f - c.a);
            var o = a > 0.0001f ? (c * c.a + under * (under.a * (1f - c.a))) / a : Color.clear;
            o.a = a;
            return o;
        }

        private static Color EyePixel(float x, float y)
        {
            float d = Mathf.Sqrt(Sq(x / 0.43f) + Sq(y / 0.47f)) - 1f;
            float cov = Cov(d * 0.45f, 96);
            if (cov <= 0f) return Color.clear;
            var c = Color.Lerp(new Color(0.84f, 0.88f, 0.9f), Color.white, Mathf.Clamp01(0.6f + y * 1.4f));
            float rim = Mathf.Clamp01(1f + d / 0.14f);
            c = Color.Lerp(c, new Color32(0x2A, 0x3A, 0x3A, 0xFF), rim * 0.75f);
            c.a = cov;
            return c;
        }

        private static Color PupilPixel(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y) - 0.46f;
            float cov = Cov(d, 64);
            if (cov <= 0f) return Color.clear;
            var c = Color.Lerp(new Color32(0x1E, 0x14, 0x12, 0xFF), new Color32(0x4A, 0x30, 0x28, 0xFF), Mathf.Clamp01(0.5f - y));
            if (Mathf.Sqrt(Sq(x - 0.14f) + Sq(y - 0.17f)) < 0.15f) c = Color.white;
            if (Mathf.Sqrt(Sq(x + 0.16f) + Sq(y + 0.15f)) < 0.06f) c = Color.Lerp(c, Color.white, 0.8f);
            c.a = cov;
            return c;
        }

        private static Color MouthPixel(float x, float y)
        {
            // Açık gülümseme: üstü düz, altı yuvarlak "D".
            float ell = Mathf.Sqrt(Sq(x / 0.44f) + Sq((y - 0.2f) / 0.62f)) - 1f;
            float top = y - 0.2f;
            float d = Mathf.Max(ell * 0.4f, top);
            float cov = Cov(d, 128);
            if (cov <= 0f) return Color.clear;
            Color inside = new Color32(0x7E, 0x22, 0x2C, 0xFF);
            var c = inside;
            float tongue = Mathf.Sqrt(Sq(x / 0.24f) + Sq((y + 0.24f) / 0.16f));
            if (tongue < 1f) c = Color.Lerp(new Color32(0xF2, 0x7E, 0x86, 0xFF), new Color32(0xFF, 0xA8, 0xAE, 0xFF), Mathf.Clamp01(0.5f + (y + 0.24f) * 3f));
            if (y > 0.13f) c = Color.Lerp(c, Color.white, 0.9f); // üst dişler
            float rim = Mathf.Clamp01(1f + d / 0.04f);
            c = Color.Lerp(c, new Color32(0x4A, 0x14, 0x1A, 0xFF), rim * 0.7f);
            c.a = cov;
            return c;
        }

        private static Color HandPixel(float x, float y)
        {
            float d = Mathf.Sqrt(Sq(x / 0.45f) + Sq(y / 0.38f)) - 1f;
            float cov = Cov(d * 0.42f, 96);
            if (cov <= 0f) return Color.clear;
            Color light = new Color32(0xFF, 0xF3, 0xE2, 0xFF), dark = new Color32(0xDF, 0xBB, 0x93, 0xFF);
            var c = Color.Lerp(dark, light, Mathf.Clamp01(0.55f + y * 1.1f - x * 0.4f));
            for (int i = -1; i <= 1; i += 2)
            {
                float crease = Mathf.Abs(x - i * 0.12f) - 0.012f;
                if (y > -0.05f && crease < 0f) c = Color.Lerp(c, dark, 0.7f);
            }
            float rim = Mathf.Clamp01(1f + d / 0.12f);
            c = Color.Lerp(c, new Color32(0xB8, 0x8E, 0x66, 0xFF), rim * 0.55f);
            float hl = Sq((x + 0.15f) / 0.16f) + Sq((y - 0.17f) / 0.08f);
            if (hl < 1f) c = Color.Lerp(c, Color.white, (1f - hl) * 0.6f);
            c.a = cov;
            return c;
        }

        private static Color SparklePixel(float x, float y)
        {
            float f = Mathf.Pow(Mathf.Abs(x), 0.5f) + Mathf.Pow(Mathf.Abs(y), 0.5f);
            float d = (f - 0.66f) * 0.3f;
            float cov = Cov(d, 96);
            if (cov <= 0f) return Color.clear;
            var c = Color.Lerp(new Color32(0xF6, 0xB3, 0x1E, 0xFF), new Color32(0xFF, 0xF2, 0x9A, 0xFF), Mathf.Clamp01(1f - f / 0.66f) * 1.4f);
            c.a = cov;
            return c;
        }

        private static Color CreamStarPixel(float x, float y)
        {
            float d = Star5(x, y + 0.03f, 0.47f, 0.48f) - 0.035f;
            float cov = Cov(d, 128);
            if (cov <= 0f) return Color.clear;
            Color light = new Color32(0xFF, 0xFB, 0xF0, 0xFF), mid = new Color32(0xF6, 0xE9, 0xD2, 0xFF), dark = new Color32(0xD9, 0xC2, 0x9E, 0xFF);
            float l = Mathf.Clamp01(0.55f + y * 0.9f - x * 0.4f);
            var c = Color.Lerp(mid, light, l);
            float rim = Mathf.Clamp01(1f + d / 0.05f);
            c = Color.Lerp(c, dark, rim * 0.8f);
            c.a = cov;
            return c;
        }

        /// <summary>Beş köşeli yıldız SDF (iq), r dış yarıçap, rf iç oran.</summary>
        private static float Star5(float px, float py, float r, float rf)
        {
            var k1 = new Vector2(0.809016994375f, -0.587785252292f);
            var k2 = new Vector2(-k1.x, k1.y);
            var p = new Vector2(Mathf.Abs(px), -py);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            var ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        private static float Sq(float v) => v * v;

        private static float Cov(float d, int n) => Mathf.Clamp01(0.5f - d / (1.2f / n));

        private static float RoundRect(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - hx + r, qy = Mathf.Abs(y) - hy + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static Sprite Make(string name, int n, Func<float, float, Color> f)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
                px[j * n + i] = f((i + 0.5f) / n - 0.5f, (j + 0.5f) / n - 0.5f);
            tex.SetPixels32(px);
            tex.Apply(true, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            sp.name = name;
            sp.hideFlags = HideFlags.DontSave;
            return sp;
        }
    }
}
