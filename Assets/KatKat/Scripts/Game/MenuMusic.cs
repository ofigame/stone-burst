using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// v10 menü müziği (docs/v10/TriKat-Mavi-Liman-Ust-Parcalar/ses/01-anasayfa-opal-esinti-loop.wav → Resources/Audio/TriKat/menu-loop).
    /// Tek 2D AudioSource; 32 sn kesintisiz döngü, düşük seviye. Menü (harita ve diğer menü sayfaları) açıkken çalar;
    /// zaten çalıyorsa yeniden başlamaz (kaydırma, bölge değişimi, sayfa yeniden çizimi döngüyü sıfırlamaz).
    /// Bölüm başlarken durmaz, InGameVolume seviyesine kısılır (2026-11-20). Ses ayarı kapalıysa hemen susar ve yeniden başlamaz; açılınca menüdeyse
    /// yumuşakça sürer. Uygulama arka plana geçince duraklar, öne dönünce yalnız menüdeyse ve ses açıksa sürer.
    /// </summary>
    public sealed class MenuMusic : MonoBehaviour
    {
        public const float Volume = 0.3f, // 2026-11-22: 0,7 → 0,5 → 0,3 (kullanıcı: "anasayfadaki müzik sesi çok yüksek, biraz kısalım")
            FadeInMs = 400f, FadeOutMs = 200f;
        /// <summary>Oyun sırasında çok kısık (kullanıcı 2026-11-20: "müzik oyun sırasında çok kısık olsun").</summary>
        public const float InGameVolume = 0.03f; // kullanıcı (2026-11-21): "oyun esnasındaki müzik sesi çok yüksek arkada çok kısık olsun" (0,08 → 0,03)

        private AudioSource _src;
        private bool _enabled = true, _want, _paused, _inGame;
        private float _target, _fadeMs = FadeInMs;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (_src == null) return;
                if (!value) { _src.Stop(); _src.volume = 0f; }
                else if (_want) Begin();
                else if (_inGame) Quiet();
            }
        }

        /// <summary>Müzik şu an çalıyor mu (testler için).</summary>
        public bool IsPlaying => _src != null && _src.isPlaying;
        public bool Wanted => _want;
        public AudioClip Clip => _src != null ? _src.clip : null;

        public static MenuMusic Create(Transform parent)
        {
            var go = new GameObject("MenuMusic");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<MenuMusic>();
            m._src = go.AddComponent<AudioSource>();
            m._src.playOnAwake = false;
            m._src.loop = true;
            m._src.spatialBlend = 0f;
            m._src.volume = 0f;
            // 2026-11-20 (kullanıcı: "docs/muzik'teki müziği oyunumuzun müziği yapalım"): A Pocketful of Marbles (91 sn, akış
            // yüklemeli). Yoksa eski 32 sn döngü.
            m._src.clip = Resources.Load<AudioClip>("Audio/TriKat/game-music") ?? Resources.Load<AudioClip>("Audio/TriKat/menu-loop");
            return m;
        }

        /// <summary>Menüye girildi: çalmıyorsa yumuşak başlar; çalıyorsa (ya da sönüyorsa) aynı yerden sürer.</summary>
        public void Enter()
        {
            _want = true;
            _inGame = false;
            if (_enabled) Begin();
        }

        /// <summary>Bölüm başlıyor: müzik durmaz, <see cref="InGameVolume"/> seviyesine kısılır (2026-11-20).</summary>
        public void Leave()
        {
            _want = false;
            _inGame = true;
            if (_enabled) Quiet();
        }

        /// <summary>Oyun içi kısık seviye; çalmıyorsa sessizden başlar.</summary>
        private void Quiet()
        {
            if (_src == null || _src.clip == null || _paused) return;
            _target = InGameVolume;
            _fadeMs = FadeInMs;
            if (!_src.isPlaying) { _src.volume = 0f; _src.Play(); }
        }

        /// <summary>Müzik tamamen susar (canlı maç; kullanıcı 2026-11-22: "canlı maçta anasayfa müziğini kapatalım").</summary>
        public void Silence()
        {
            _want = false;
            _inGame = false;
            _target = 0f;
            _fadeMs = FadeOutMs;
        }

        /// <summary>Oyun içinde kısık çalıyor mu (testler).</summary>
        public bool InGame => _inGame;

        private void Begin()
        {
            if (_src == null || _src.clip == null || _paused) return;
            _target = Volume;
            _fadeMs = FadeInMs;
            if (!_src.isPlaying) { _src.volume = 0f; _src.Play(); }
        }

        private void Update() => Step(Time.unscaledDeltaTime * 1000f);

        /// <summary>Sönme/yükselmeyi elle ilerletir (testler).</summary>
        public void Step(float ms)
        {
            if (_src == null || !_src.isPlaying) return;
            float rate = Volume / Mathf.Max(1f, _fadeMs);
            _src.volume = Mathf.MoveTowards(_src.volume, _target, rate * ms);
            if (!_want && !_inGame && _src.volume <= 0.0001f) _src.Stop();
        }

        private void OnApplicationPause(bool paused)
        {
            _paused = paused;
            if (_src == null) return;
            if (paused) { if (_src.isPlaying) _src.Pause(); }
            else if ((_want || _inGame) && _enabled) { _src.UnPause(); if (_want) Begin(); else Quiet(); }
        }
    }
}
