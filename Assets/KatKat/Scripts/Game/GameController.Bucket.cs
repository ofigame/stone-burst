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
                _hud.BucketDelivered = () => _hud.CountGoalsTo(_session.State.Goals);
                // Torba tahtanın üst ortasındaki basamaklı oyukta (bölüm verisinde boş hücreler): tabanı oyuğun en alt satırının
                // altında, boyu ~3 satır.
                // 2026-11-26 (kullanıcı: "Torba ise tahtanın üst kısmındaki altın ve roket rulolarının yerine gelsin"): torba üst
                // güç bandında (HudView.PlaceBucketInBand); tahtadaki oyuk kullanılmaz.
                _hud.BucketBottom = null;
                _hud.BucketWorldHeight = null;
            }
            else
            {
                _board.BucketTarget = null;
                _board.BucketArrived = null;
            }
        }

        /// <summary>
        /// Akış sesi (kullanıcı 2026-11-24: "taşların hedefe dolma sesi torbaya dolarken de olmalı"; "en yakın taştan gitmeye başlar
        /// ve ses doğru orantıda çıkar"): her taşın varışında bir "jelly-pop"; perde ilk taştan son taşa tizden pese iner, düzey
        /// yükselir. Taşlar zaten sırayla vardığı için ses doğrudan çalar (kuyruk yok); son taşta dolma ("ka-ching").
        /// </summary>
        private void StreamTick(int i, int total)
        {
            if (total <= 0) return;
            if (i == total - 1)
            {
                if (total > 1 && _sound.PlayV17("jelly-pop", -1f, 0.85f)) Haptics.Tick();
                if (!_sound.QueueV17("goal-collect", 0f, 1f, true, Haptics.Medium)) { _sound.Play(Sfx.CoinFinal, 0, 0.85f); Haptics.Medium(); }
                return;
            }
            float u = total <= 1 ? 0f : i / (float)(total - 1);
            float pitch = Mathf.Lerp(6f, -1f, u);
            float vol = Mathf.Lerp(0.5f, 0.85f, u) * (total > 10 && i % 2 == 1 ? 0.7f : 1f);
            if (_sound.PlayV17("jelly-pop", pitch, vol)) Haptics.Tick();
            else { _sound.Play(Sfx.Coin, 0, vol); Haptics.Tick(); }
        }

        /// <summary>Dolu torbanın genişliğinin doku boyuna oranı: dolu torba oyuğun dip genişliğine tam oturur (kullanıcı çizimi).</summary>
        private const float BagFullWidth = 0.94f;
        /// <summary>Tepsi çerçevesinin oyuk dibindeki kalınlığı (satır payı): torba çerçevenin üstünde durur (kullanıcı 2026-11-24:
        /// "Torba bulunduğu yerdeki tepsinin border ının üstünde olmalı").</summary>
        private const float BagRimRows = 0.2f;

        /// <summary>Oyuğun en derin satırındaki boş hücrelerin dünya genişliği (yoksa iki sütun).</summary>
        private float BagNotchWidth()
        {
            var b = _session.State.Board;
            int deepest = NotchDeepest(b), mid = (b.Width - 1) / 2, n = 0;
            if (deepest >= 0)
                for (int x = 0; x < b.Width; x++) if (!b[x, deepest].Active && Mathf.Abs(x - mid - 0.5f) < b.Width * 0.5f - 1f) n++;
            float col = Mathf.Abs(_board.transform.TransformPoint(_board.CellCenter(1, 0)).x - _board.transform.TransformPoint(_board.CellCenter(0, 0)).x);
            return Mathf.Max(2, n) * col;
        }

        private static int NotchDeepest(BoardState b)
        {
            int mid = (b.Width - 1) / 2, deepest = -1;
            for (int y = 0; y < b.Height; y++)
            {
                if (b[mid, y].Active) break;
                deepest = y;
            }
            return deepest;
        }

        /// <summary>Bir satırın dünya yüksekliği (iki satır merkezi arası, tahta ölçeği içinde).</summary>
        private float RowWorld() => Mathf.Abs(_board.transform.TransformPoint(_board.CellCenter(0, 0)).y - _board.transform.TransformPoint(_board.CellCenter(0, 1)).y);

        /// <summary>Oyuğun dibi: üst ortadaki boş hücrelerin en alt satırının alt kenarı (yoksa tahtanın üst kenarı), tahta ortasında.</summary>
        private Vector3 BagNotchBottom()
        {
            var b = _session.State.Board;
            int w = b.Width, deepest = NotchDeepest(b);
            var left = _board.transform.TransformPoint(_board.CellCenter(0, Mathf.Max(0, deepest)));
            var right = _board.transform.TransformPoint(_board.CellCenter(w - 1, Mathf.Max(0, deepest)));
            float half = RowWorld() * 0.5f;
            // Taban oyuğun dibindeki çerçevenin üst yüzünde.
            return new Vector3((left.x + right.x) * 0.5f, left.y + (deepest >= 0 ? -half : half) + RowWorld() * BagRimRows, left.z);
        }

        // ---- Yerleşim denetimi (2026-11-26, kullanıcı: "Tüm bölümlerde kovanın ve torbanın bu durumlarını kontrol et. görsel hata olmasın") ----

        /// <summary>Denetim: dolu torbanın dünya dikdörtgeni (torba kipinde), yoksa boş.</summary>
        public Rect AuditBagWorldRect()
        {
            if (!BucketLevel || _hud.BucketWorldHeight == null) return Rect.zero;
            float h = _hud.BucketWorldHeight(), w = h * BagFullWidth;
            var b = BagNotchBottom();
            return new Rect(b.x - w * 0.5f, b.y, w, h);
        }

        /// <summary>Denetim: su kovasının dünya dikdörtgeni (taban–ağız, ağız genişliği), yoksa boş.</summary>
        public Rect AuditWaterBucketWorldRect()
        {
            if (!WaterLevel || WaterCap <= 0) return Rect.zero;
            Vector3 lo = _board.WaterBucketMinWorld, hi = _board.WaterBucketMaxWorld;
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }

        /// <summary>Denetim: etkin hücrelerin dünya dikdörtgenleri (adım kadar, verilen oranla küçültülmüş).</summary>
        public System.Collections.Generic.List<Rect> AuditCellWorldRects(float shrink)
        {
            var list = new System.Collections.Generic.List<Rect>();
            var b = _session.State.Board;
            float col = Mathf.Abs(_board.transform.TransformPoint(_board.CellCenter(1, 0)).x - _board.transform.TransformPoint(_board.CellCenter(0, 0)).x);
            float row = RowWorld();
            for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                if (!b[x, y].Active) continue;
                var c = _board.transform.TransformPoint(_board.CellCenter(x, y));
                list.Add(new Rect(c.x - col * 0.5f * shrink, c.y - row * 0.5f * shrink, col * shrink, row * shrink));
            }
            return list;
        }

        /// <summary>Denetim: tahtanın sığması gereken dünya alanı (üst ve alt arayüz arası).</summary>
        public Rect AuditBoardArea() => _hud.BoardWorldRect(_cam);

        /// <summary>Torbaya dokunuş: içindeki kadarı (boşsa hiç) gönderilir, eksik kısım sıranın sonuna, yeni torba gelir.</summary>
        public void SendBucket()
        {
            if (!BucketLevel || CurrentPhase != Phase.Idle || _hud.PanelOpen || _hud.BucketBusy) return;
            var r = _session.Submit(Command.SendBucket());
            if (!r.Accepted)
            {
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                return;
            }
            if (!_sound.PlayV17("goal-collect", 2f)) _sound.Play(Sfx.CoinFinal, 0, 0.85f);
            Haptics.Medium();
            HintAfterCommand();
            Play(r);
        }
    }
}
