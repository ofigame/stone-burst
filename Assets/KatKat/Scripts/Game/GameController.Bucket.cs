using KatKat.Core;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Kova kipi oyun akışı (kullanıcı 2026-11-23: "bazı bölümlerde roket ve altın rulonun olduğu alana kovalar gelecek … Süre olacak.
    /// Bu süre boyunca ne kadar kova geldiyse tamamını doldurmaya çalışacağız … mavi taş kovasını dolduğu kadarını kovanın üstüne
    /// tıklayarak göndeririz ve yeni kova gelir"). Kural <see cref="BucketRun"/> ve <see cref="RulesEngine"/>'de; burada tahta
    /// uçuşları kovaya yönlendirilir, varış sesleri ve kovaya dokunuş (yarım gönderim) işlenir.
    /// </summary>
    public sealed partial class GameController
    {
        // ---- Taş varış sesleri (kullanıcı 2026-11-23: "taşların hedefe gitme sesleri büyük grup taşlarda sinir bozuyor … yüksekten
        // alçalan tonda olmalı"; öneri onaylandı) ------------------------------------------------------------------------------

        /// <summary>Hamle başına en çok bu kadar varış sesi (büyük grupta birkaç taş tek seste toplanır).</summary>
        public const int FlowSoundMax = 5;
        /// <summary>Varış sesi dizisi: perde (yarım ses) tizden pese, düzey azalır, aralık (sn) sıklaşır.</summary>
        private static readonly float[] FlowPitch = { 5f, 3.5f, 2f, 0.5f, -1f }, FlowVolume = { 0.85f, 0.75f, 0.65f, 0.56f, 0.48f }, FlowGap = { 0.15f, 0.13f, 0.12f, 0.11f, 0.11f };

        /// <summary>
        /// Büyük grup (5+ taş; kullanıcı 2026-11-23: "ilk başta üst üste binsin sona doğru normal sesi tam duyulsun. yavaşlamada öyle
        /// hissedilir. 3 veya 4 taşta şuanki gibi kalsın"): en çok 8 ses; ilk beşi kısa aralıkla üst üste biner (kısık), sonra aralık
        /// açılır ve son sesler tam düzeyde, kesilmeden duyulur. Perde tizden pese.
        /// </summary>
        public const int FlowSoundMaxBig = 8;
        private static readonly float[] BigPitch = { 6f, 5f, 4f, 3f, 2f, 1f, 0f, -1f }, BigVolume = { 0.45f, 0.45f, 0.5f, 0.55f, 0.6f, 0.72f, 0.82f, 0.88f },
            BigGap = { 0.035f, 0.04f, 0.05f, 0.065f, 0.085f, 0.12f, 0.15f, 0.19f };

        private void FlowTickSound(int i)
        {
            int n = _lastResult != null ? _lastResult.TotalPeeled : 0;
            if (n >= 5)
            {
                if (i >= FlowSoundMaxBig) return;
                if (!_sound.QueueV17("jelly-pop", BigPitch[i], BigVolume[i], false, Haptics.Tick, BigGap[i], BigGap[i] < SoundBank.SerialGap)) { _sound.Play(Sfx.Coin, 0, BigVolume[i]); Haptics.Tick(); }
                return;
            }
            if (i >= FlowSoundMax) return;
            if (!_sound.QueueV17("jelly-pop", FlowPitch[i], FlowVolume[i], false, Haptics.Tick, FlowGap[i])) { _sound.Play(Sfx.Coin, 0, FlowVolume[i]); Haptics.Tick(); }
        }

        public bool BucketLevel => _session != null && _session.State.Bucket != null;

        private void BucketBegin()
        {
            var run = _session.State.Bucket;
            _hud.SetBucketMode(run);
            if (run != null)
            {
                _board.BucketTarget = _hud.BucketTarget;
                _board.BucketArrived = _ => _hud.BucketStoneIn();
            }
            else
            {
                _board.BucketTarget = null;
                _board.BucketArrived = null;
            }
        }

        /// <summary>Kovaya dokunuş: içindeki kadarı gönderilir, eksik kısım sıranın sonuna, yeni kova gelir.</summary>
        public void SendBucket()
        {
            if (!BucketLevel || CurrentPhase != Phase.Idle || _hud.PanelOpen || _hud.BucketBusy) return;
            var r = _session.Submit(Command.SendBucket());
            if (!r.Accepted)
            {
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                _hud.ShowToast(Loc.T("toast.bucket.empty"), 1.8f);
                return;
            }
            if (!_sound.PlayV17("goal-collect", 2f)) _sound.Play(Sfx.CoinFinal, 0, 0.85f);
            Haptics.Medium();
            HintAfterCommand();
            Play(r);
        }
    }
}
