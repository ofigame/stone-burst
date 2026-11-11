using System;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Kısa efekt sesleri koddan sentezlenir (ses dosyası gerekmez). Dalga başına tek ana ses çalınır;
    /// zincir perdesi kademeli artar ama sınırlıdır.
    /// </summary>
    public sealed class SoundBank : MonoBehaviour
    {
        private const int Rate = 44100;
        private AudioClip[] _clips;
        private AudioSource[] _sources;
        private int _next;

        private bool _enabled = true;

        /// <summary>Ses ayarı. Kapatılınca çalan efektler de hemen susar; kapalıyken yeni ses başlamaz.</summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (!value && _sources != null) foreach (var s in _sources) s.Stop();
            }
        }

        // v10 WAV efektleri (docs/v10/TriKat-Mavi-Liman-Ust-Parcalar/ses → Resources/Audio/TriKat). Yüklenemezse sentez.
        private AudioClip _pop, _chain4, _reward;

        /// <summary>Dört vuruşlu zincir WAV'ı yüklü mü (yüklüyse hamle başına bir kez çalınır).</summary>
        public bool HasChainWav => _chain4 != null;

        /// <summary>Son çalınan klibin adı ve sayacı (testler: üst üste binme ve tek seferlik kontrolü).</summary>
        public string LastClip { get; private set; } = "";
        public int PlayCount { get; private set; }
        /// <summary>Çalınan her klibin adı (testler).</summary>
        public Action<string> OnPlayed;

        public static SoundBank Create(Transform parent)
        {
            var go = new GameObject("Sound");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<SoundBank>();
            b._sources = new AudioSource[4];
            for (int i = 0; i < b._sources.Length; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                b._sources[i] = s;
            }
            b._clips = new[]
            {
                Make("tap", 0.05f, (t, d) => Sine(t, 880f) * Decay(t, 60f) * 0.35f),
                Make("invalid", 0.16f, (t, d) => Sine(t, t < 0.07f ? 240f : 200f) * Gate(t, 0.0f, 0.06f, 0.09f, 0.15f) * 0.3f),
                Make("peel", 0.22f, PeelVoice),
                Make("chain", 0.32f, (t, d) => (Sine(t, 660f) * 0.6f + Sine(t, 1320f) * 0.25f + Sine(t, 1980f) * 0.1f) * Decay(t, 9f) * Attack(t, 0.004f) * 0.32f),
                Make("rollGain", 0.36f, (t, d) => Sine(t, t < 0.12f ? 523f : t < 0.24f ? 659f : 784f) * Decay(t % 0.12f, 14f) * 0.32f),
                Make("rollUse", 0.3f, (t, d) => Noise(t) * Mathf.Sin(Mathf.PI * t / d) * 0.18f),
                Make("win", 0.75f, (t, d) =>
                {
                    float f = t < 0.15f ? 523f : t < 0.3f ? 659f : t < 0.45f ? 784f : 1047f;
                    return (Sine(t, f) * 0.7f + Sine(t, f * 2f) * 0.2f) * Decay(t % 0.15f + (t > 0.45f ? (t - 0.45f) * 0.3f : 0f), 6f) * 0.3f;
                }),
                Make("lose", 0.45f, (t, d) => Sine(t, Mathf.Lerp(392f, 262f, t / d)) * Decay(t, 5f) * 0.25f),
                // Metalik "tık": iki kısa yüksek ton, ikincisi açılış.
                Make("clipOpen", 0.18f, (t, d) => (Sine(t, 2350f) * 0.5f + Sine(t, 3520f) * 0.3f) * Decay(t < 0.06f ? t : t - 0.06f, 45f) * 0.22f),
                Make("star", 0.42f, (t, d) =>
                {
                    float f = t < 0.09f ? 1175f : 1568f;
                    return (Sine(t, f) * 0.6f + Sine(t, f * 2.01f) * 0.18f) * Decay(t < 0.09f ? t : t - 0.09f, 8f) * Attack(t, 0.003f) * 0.26f;
                }),
            };
            b._pop = Resources.Load<AudioClip>("Audio/TriKat/peel-pop");
            b._chain4 = Resources.Load<AudioClip>("Audio/TriKat/chain-four");
            b._reward = Resources.Load<AudioClip>("Audio/TriKat/level-reward");
            // Kısa efektler önceden belleğe alınır (ilk çalmada gecikme olmasın).
            foreach (var c in new[] { b._pop, b._chain4, b._reward }) if (c != null && c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
            return b;
        }

        /// <summary>vr1 P1 prototip sesleri (Resources/Audio/Vr1; sentez prototipi, cihazda dinlenip miks onayı bekliyor).</summary>
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _vr1 = new System.Collections.Generic.Dictionary<string, AudioClip>();
        /// <summary>SFX kazancı taslağı (04_ANIMASYON_GORSEL_SES): 0,65; mevcut miksle dinlenerek ayarlanacak.</summary>
        public const float Vr1Gain = 0.65f;

        public void PlayVr1(string name)
        {
            if (!_enabled || string.IsNullOrEmpty(name)) return;
            if (!_vr1.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/Vr1/" + name);
                if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                _vr1[name] = clip;
            }
            if (clip != null) PlayClip(clip, 1f, Vr1Gain);
        }

        /// <summary>v10: normal bölüm zafer/ödül kartı ilk gösterilirken bir kez (WAV yoksa eski zafer sesi).</summary>
        public void PlayReward()
        {
            if (_reward != null) PlayClip(_reward, 1f, 1f);
            else Play(Sfx.Win);
        }

        private void PlayClip(AudioClip clip, float pitch, float volume)
        {
            if (!_enabled || clip == null) return;
            if (CaptureTap != null) { EnsureSamples(clip); CaptureTap(clip, pitch, Mathf.Clamp01(volume)); }
            var src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
            LastClip = clip.name;
            PlayCount++;
            OnPlayed?.Invoke(clip.name);
        }

        /// <summary>
        /// Kayıt aracı (tanıtım videosu) örnekleri mono 44,1 kHz bekler: stereo WAV kanalların ortalamasıyla teke indirilir,
        /// düzey değişmez (normalize edilmez).
        /// </summary>
        private static void EnsureSamples(AudioClip clip)
        {
            if (Samples.ContainsKey(clip)) return;
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            int ch = Mathf.Max(1, clip.channels);
            var raw = new float[clip.samples * ch];
            if (!clip.GetData(raw, 0)) return;
            var mono = new float[clip.samples];
            for (int i = 0; i < mono.Length; i++)
            {
                float s = 0f;
                for (int c = 0; c < ch; c++) s += raw[i * ch + c];
                mono[i] = s / ch;
            }
            Samples[clip] = mono;
        }

        /// <param name="pitchStep">Zincir adımı: perde 1 + 0.07·adım, en çok 1.35.</param>
        public void Play(Sfx sfx, int pitchStep = 0, float volume = 1f)
        {
            if (!Enabled) return;
            // v10 WAV'lar: soyulma pop'u ve dört vuruşlu zincir (perde yükseltmesi uygulanmaz).
            if (sfx == Sfx.Peel && _pop != null) { PlayClip(_pop, 1f, volume); return; }
            if (sfx == Sfx.Chain && _chain4 != null) { PlayClip(_chain4, 1f, volume); return; }
            float pitch = Mathf.Min(1.35f, 1f + 0.07f * pitchStep);
            CaptureTap?.Invoke(_clips[(int)sfx], pitch, Mathf.Clamp01(volume));
            var src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            src.pitch = pitch;
            src.PlayOneShot(_clips[(int)sfx], Mathf.Clamp01(volume));
            LastClip = _clips[(int)sfx].name;
            PlayCount++;
            OnPlayed?.Invoke(LastClip);
        }

        /// <summary>Tanıtım videosu kaydı: çalınan her efekt (ses, perde, düzey) zamanıyla birlikte miksajlanır. Oyunda null.</summary>
        public static Action<AudioClip, float, float> CaptureTap;

        /// <summary>Sentezlenen örnekler (44,1 kHz mono); kayıt aracı ses aygıtı olmadan miksajlar.</summary>
        public static readonly System.Collections.Generic.Dictionary<AudioClip, float[]> Samples = new System.Collections.Generic.Dictionary<AudioClip, float[]>();
        public const int SampleRate = Rate;

        private static AudioClip Make(string name, float seconds, Func<float, float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                // Kenar tıklamasını önlemek için kısa çıkış.
                float tail = Mathf.Clamp01((seconds - t) / 0.01f);
                data[i] = Mathf.Clamp(f(t, seconds) * tail, -1f, 1f);
            }
            var clip = AudioClip.Create("katkat_" + name, n, 1, Rate, false);
            if (CaptureTap != null) Samples[clip] = data; // yalnız kayıt sırasında tutulur
            clip.SetData(data, 0);
            return clip;
        }

        // Yumuşak "sticker" sesi: alçak geçiren gürültü + hafif aşağı kayan ton.
        private static float _lp;
        private static float PeelVoice(float t, float d)
        {
            if (t == 0f) _lp = 0f;
            _lp += (Noise(t) - _lp) * 0.18f;
            float env = Attack(t, 0.01f) * Mathf.Exp(-t * 11f);
            return (_lp * 0.9f + Sine(t, Mathf.Lerp(420f, 300f, t / d)) * 0.15f) * env * 0.5f;
        }

        private static float Sine(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);
        private static float Decay(float t, float k) => Mathf.Exp(-t * k);
        private static float Attack(float t, float a) => Mathf.Clamp01(t / a);
        private static float Gate(float t, float a0, float a1, float b0, float b1) => (t >= a0 && t < a1) || (t >= b0 && t < b1) ? 1f : 0f;

        // Belirlenimli gürültü (zaman tohumlu hash).
        private static float Noise(float t)
        {
            uint x = (uint)(t * Rate) * 747796405u + 2891336453u;
            x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return x / (float)uint.MaxValue * 2f - 1f;
        }
    }
}
