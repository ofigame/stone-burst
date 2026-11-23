using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// v10 menü müziği (docs/v10/TriKat-Mavi-Liman-Ust-Parcalar/ses/01-anasayfa-opal-esinti-loop.wav ; 2026-11-23 yalnız Resources/Audio/TriKat/game-music).
    /// Tek 2D AudioSource; 32 sn kesintisiz döngü, düşük seviye. Menü (harita ve diğer menü sayfaları) açıkken çalar;
    /// zaten çalıyorsa yeniden başlamaz (kaydırma, bölge değişimi, sayfa yeniden çizimi döngüyü sıfırlamaz).
    /// Bölüm başlarken çok kısık düzeye iner (2026-11-23 ikinci tur; canlı maçta susar). Ses ayarı kapalıysa hemen susar ve yeniden başlamaz; açılınca menüdeyse
    /// yumuşakça sürer. Uygulama arka plana geçince duraklar, öne dönünce yalnız menüdeyse ve ses açıksa sürer.
    /// </summary>
    public sealed class MenuMusic : MonoBehaviour
    {
        // 2026-11-22: 0,7 → 0,5 → 0,3; 2026-11-23 (kullanıcı: "müziği ana sayfada kıs ve oyun ekranından tamamen kaldır") 0,3 → 0,18.
        public const float Volume = 0.18f,
            FadeInMs = 400f, FadeOutMs = 200f;
        /// <summary>Oyun ekranı düzeyi (2026-11-23, kullanıcı: "oyun alanına müziği tekrar koyalım ama çok kısık olsun").</summary>
        public const float InGameVolume = 0.05f;

        private AudioSource _src;
        private bool _enabled = true, _want, _paused, _inGame;
        /// <summary>Oyun ekranında kısık çalıyor mu (testler).</summary>
        public bool InGame => _inGame;
        public float CurrentVolume => _src != null ? _src.volume : 0f;
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
            m._src.clip = KatKat.View.Res.Load<AudioClip>("Audio/TriKat/game-music");
            return m;
        }

        /// <summary>Menüye girildi: çalmıyorsa yumuşak başlar; çalıyorsa (ya da sönüyorsa) aynı yerden sürer.</summary>
        public void Enter()
        {
            _want = true;
            _inGame = false;
            if (_enabled) Begin();
        }

        /// <summary>Bölüm başlıyor: müzik kesilmeden çok kısık düzeye iner (2026-11-23 ikinci tur).</summary>
        public void Leave()
        {
            _want = true;
            _inGame = true;
            if (_enabled) Begin();
        }

        /// <summary>Müzik tamamen susar (oyun ekranı, canlı maç).</summary>
        public void Silence()
        {
            _want = false;
            _inGame = false;
            _target = 0f;
            _fadeMs = FadeOutMs;
        }

        private void Begin()
        {
            if (_src == null || _src.clip == null || _paused) return;
            _target = _inGame ? InGameVolume : Volume;
            _fadeMs = _inGame ? FadeOutMs : FadeInMs;
            if (!_src.isPlaying) { _src.volume = 0f; _src.Play(); }
        }

        private void Update() => Step(Time.unscaledDeltaTime * 1000f);

        /// <summary>Sönme/yükselmeyi elle ilerletir (testler).</summary>
        public void Step(float ms)
        {
            if (_src == null || !_src.isPlaying) return;
            float rate = Volume / Mathf.Max(1f, _fadeMs);
            _src.volume = Mathf.MoveTowards(_src.volume, _target, rate * ms);
            if (!_want && _src.volume <= 0.0001f) _src.Stop();
        }

        private void OnApplicationPause(bool paused)
        {
            _paused = paused;
            if (_src == null) return;
            if (paused) { if (_src.isPlaying) _src.Pause(); }
            else if (_want && _enabled) { _src.UnPause(); Begin(); }
        }
    }
}
