using KatKat.Core;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Su kovası akışı (2026-11-25/26, kullanıcı: "Su bölümlerini kovaya çeviriyoruz … Kovayı dolduracağız"; "kova doldukça gönder
    /// diyelim ve görev çubuğu yatayda su dolan progress bar gibi olsun. kovayı en az 3 kez gönderebileceğimiz şekilde olmalı").
    /// Kural <see cref="RulesEngine"/>'de: hücre dibine kadar boşalınca su kovaya akar (kapasiteye kadar, fazlası taşar); "Gönder"
    /// (<see cref="CommandType.SendBucket"/>) kovadaki suyu göreve aktarır. Burada kurulum, gönderim, eşitleme, çatlak ve sesler.
    /// </summary>
    public sealed partial class GameController
    {
        private bool _waterSendHooked;

        private GoalState WaterGoal()
        {
            if (_session == null) return null;
            foreach (var g in _session.State.Goals) if (g.Type == GoalType.FillWater) return g;
            return null;
        }

        public bool WaterLevel => WaterGoal() != null;
        private int WaterCap => _session != null ? _session.State.WaterCap : 0;

        private void WaterBegin()
        {
            var g = WaterGoal();
            var s = _session.State;
            // Kovalı bölümde kova kapasitesi kadar dolar; eski (kapasitesiz) bölümde görev kadar.
            int cap = g == null ? 0 : s.WaterCap > 0 ? s.WaterCap : g.Amount;
            int have = g == null ? 0 : s.WaterCap > 0 ? s.WaterHave : g.Progress;
            _board.SetWater(cap, have, s.Board);
            _board.WaterArrived = g != null ? WaterArrived : (System.Action<int>)null;
            _board.WaterLeaked = g != null ? WaterLeaked : (System.Action<int>)null;
            _board.CrackHealed = g != null ? CrackHealed : (System.Action)null;
            if (g != null) _hud.SetWaterBar(g.Amount > 0 ? g.Progress / (float)g.Amount : 0f, true);
            _hud.WaterSendAnchor = g != null && s.WaterCap > 0 ? () => _board.WaterSendAnchorWorld : (System.Func<Vector3>)null;
            _hud.WaterBucketMin = g != null && s.WaterCap > 0 ? () => _board.WaterBucketMinWorld : (System.Func<Vector3>)null;
            _hud.WaterBucketMax = g != null && s.WaterCap > 0 ? () => _board.WaterBucketMaxWorld : (System.Func<Vector3>)null;
            _hud.WaterSendState = g != null && s.WaterCap > 0 ? () => SendState() : (System.Func<int>)null;
            if (!_waterSendHooked) { _hud.WaterSendPressed += SendWaterBucket; _waterSendHooked = true; }
        }

        private int SendState()
        {
            var s = _session.State;
            if (s.WaterHave <= 0 || CurrentPhase != Phase.Idle) return s.WaterHave <= 0 ? 0 : 1;
            return s.WaterHave >= s.WaterCap ? 2 : 1;
        }

        /// <summary>"Gönder": kovadaki su görev çubuğuna akar; çubuk varışta dolar. Hamle harcamaz.</summary>
        public void SendWaterBucket()
        {
            if (!WaterLevel || WaterCap <= 0 || CurrentPhase != Phase.Idle || _hud.PanelOpen) return;
            var r = _session.Submit(Command.SendBucket());
            if (!r.Accepted) { _sound.Play(Sfx.Invalid); Haptics.Light(); return; }
            if (!_sound.PlayV17("water-splash", 2f, 1f)) _sound.Play(Sfx.CoinFinal, 0, 0.85f);
            Haptics.Medium();
            var g = WaterGoal();
            float fill = g != null && g.Amount > 0 ? g.Progress / (float)g.Amount : 0f;
            // Çubuğun ekran noktası tahta dünyasına (tahtayı çizen kamerayla).
            var sp = _hud.WaterBarScreen();
            var cam = Camera.main;
            Vector3 target = cam != null ? cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, Mathf.Abs(cam.transform.position.z - _board.transform.position.z))) : _board.transform.position;
            _board.SendWater(target, () =>
            {
                _hud.SetWaterBar(fill, false);
                if (!_sound.PlayV17("goal-collect", 0f, 1f)) _sound.Play(Sfx.CoinFinal, 0, 0.85f);
                Haptics.Medium();
            });
            HintAfterCommand();
            Play(r);
        }

        // ---- Çatlak ve sızıntı (2026-11-25/26, kullanıcı: "Bomba tuzaklarında kova yarılacak … Belli bir sürede yarık kendini onarsın";
        // "Geri alma sadece normal hamlelerde olsun. Bu tarz durumlarda geri alma olmasın") ----

        /// <summary>Hamlede bombalı yığın patladıysa kova çatlar: ses, titreşim, uyarı; o hamle geri alınamaz.</summary>
        private void WaterCracks(MoveResult r)
        {
            if (r == null || r.Cracks.Count == 0 || !WaterLevel) return;
            foreach (var x in r.Cracks) _board.OpenCrack(x);
            _session.DropUndo();
            KatKat.UI.Vr1Proto.Play("ice-crack");
            _sound.PlayV17("water-splash", -6f, 1f);
            Haptics.Strong();
            _board.RimSweep(new Color(1f, 0.45f, 0.3f), new Color(0.4f, 0.8f, 1f), 0.9f);
            _hud.ShowToast(Loc.T("water.crack"), 2.6f);
        }

        /// <summary>Çatlaktan bir birim su aktı: kovadaki su (kapasitesiz bölümde görev) azalır; sızıntı geri alınamaz.</summary>
        private void WaterLeaked(int units)
        {
            var g = WaterGoal();
            if (g == null) return;
            var s = _session.State;
            if (s.WaterCap > 0) s.WaterHave = Mathf.Max(0, s.WaterHave - units);
            else g.Progress = Mathf.Max(0, g.Progress - units);
            _session.DropUndo();
            _sound.PlayV17("water-drop", -5f + Random.Range(-0.5f, 0.5f), 0.45f);
        }

        private void CrackHealed()
        {
            _hud.ShowToast(Loc.T("water.healed"), 1.6f);
            _sound.PlayV17("star-gain", 3f, 0.6f);
            Haptics.Light();
        }

        private void WaterArrived(int units)
        {
            var g = WaterGoal();
            if (g == null) return;
            int cap = WaterCap > 0 ? WaterCap : g.Amount;
            float fill = cap > 0 ? Mathf.Clamp01(_board.WaterShown / (float)cap) : 0f;
            if (_board.WaterShown >= cap)
            {
                if (!_sound.PlayV17("water-splash", -3f, 1f)) _sound.Play(Sfx.CoinFinal, 0, 0.85f);
                _sound.PlayV17("water-drop", 0f, 0.8f);
                Haptics.Medium();
                return;
            }
            if (_sound.PlayV17("water-splash", Mathf.Lerp(-2f, 5f, fill) + Random.Range(-0.5f, 0.5f), 0.75f + 0.08f * units)) Haptics.Tick();
            else { _sound.Play(Sfx.Coin, 0, 0.7f); Haptics.Tick(); }
            _sound.PlayV17("water-drop", Mathf.Lerp(-1f, 6f, fill), 0.5f);
        }

        private void WaterAfterPlay()
        {
            var g = WaterGoal();
            if (g == null) return;
            _board.WaterSettle(WaterCap > 0 ? _session.State.WaterHave : g.Progress);
        }

        private void WaterResync() => WaterBegin();
    }
}
