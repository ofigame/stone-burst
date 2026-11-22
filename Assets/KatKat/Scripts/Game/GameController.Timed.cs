using KatKat.Core;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Süreli bölümler (kullanıcı, 2026-11-21: "süreli bölümler olacak. Süreli bölümlerde son 10 saniyeye girince oyun tahtası
    /// kıvılcım atarak kalp atışı gibi atacak. süre biterse patlayacak ve tüm taşlar aşağıya düşecek"; "süreli bölümlerde hamle
    /// sınırsız olsun süre olsun sadece"; "süre bitince animasyonlardan sonra bölüm bitsin tekrarlamak zorunda kalsın, dilerse oyun
    /// esnasında güçlendiricilerden + hamle kullanırsa artı süre almış olur").
    /// <para>Süre bölüm verisinden (LevelData.TimeLimit); kural motoru zamana bakmaz. Süre yalnız oyun akarken azalır (duraklatma,
    /// kart, menü, kutlama sırasında durur). Son 10 saniyede tahta kalp atışı; bitince patlama, taşlar düşer, kayıp kartı
    /// yalnız "Tekrar".</para>
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>+Hamle güçlendiricisinin süreli bölümdeki karşılığı (sn).</summary>
        public const int TimedBonusSeconds = 15;
        public const float TimedUrgentSeconds = 10f;

        private bool _timed, _timeUp;
        private float _timeLeft;

        public bool TimedActive => _timed && !_timeUp;
        public float TimeLeft => _timeLeft;

        private void TimedBegin()
        {
            _timed = _level != null && _level.TimeLimit > 0;
            _timeUp = false;
            _timeLeft = _timed ? _level.TimeLimit : 0f;
            _hud.TimerMode = _timed;
            _hud.SetTimer(_timed ? _timeLeft : -1f);
            if (!_timed && _session != null) _hud.SetMoves(_session.State.MovesLeft);
            _board.SetHeartbeat(0f);
            _board.OnBeat = () => { _sound.Play(Sfx.Beat); Haptics.Medium(); };
        }

        private void TimedTick(float dt)
        {
            if (!_timed || _timeUp || _session == null) return;
            if (_session.State.Status != GameStatus.Idle) return;
            if (CurrentPhase != Phase.Idle && CurrentPhase != Phase.Animating) return;
            if (_hud.PanelOpen || _menu.IsOpen || _hud.RuleCardOpen || _celebrating) return;
            _timeLeft = Mathf.Max(0f, _timeLeft - dt);
            _hud.SetTimer(_timeLeft);
            _board.SetHeartbeat(_timeLeft <= TimedUrgentSeconds ? 1f - _timeLeft / TimedUrgentSeconds : 0f);
            if (_timeLeft <= 0f) TimeUp();
        }

        /// <summary>Süreli bölüme süre ekler (+Hamle güçlendiricisi).</summary>
        public void AddTime(float sec)
        {
            if (!TimedActive) return;
            _timeLeft += sec;
            _hud.SetTimer(_timeLeft);
            _board.SetHeartbeat(_timeLeft <= TimedUrgentSeconds ? 1f - _timeLeft / TimedUrgentSeconds : 0f);
            _hud.ShowToast(Loc.F("toast.time.bonus", Mathf.RoundToInt(sec)), 1.6f);
        }

        private void TimeUp()
        {
            _timeUp = true;
            _session.State.Status = GameStatus.LostOutOfTime;
            CancelBoosterMode();
            SetPhase(Phase.Ended);
            _board.SetHeartbeat(0f);
            // 2026-11-22 (kullanıcı: "bitişinde ise süre dolmasından dolayı taşların dökülmesi ses efektiyle ve titreşimle olsun").
            _sound.Play(Sfx.Boom);
            Haptics.Strong();
            _board.TimeUpBlast(() => ShowLost(GameStatus.LostOutOfTime));
            _board.Later(260f, () => { Haptics.Strong(); _sound.Play(Sfx.Peel); });
            _board.Later(620f, () => { Haptics.Medium(); _sound.Play(Sfx.Peel, 0, 0.7f); });
        }

        /// <summary>Süre bitti kartı: yalnız yeniden oyna (deneme setinde liste de).</summary>
        private void ShowTimeLost()
        {
            var buttons = new System.Collections.Generic.List<HudView.PanelButton>
            {
                new HudView.PanelButton(Loc.T("btn.retry"), UiIcon.Restart, true, Restart),
            };
            if (_sessionKind == KatKat.Meta.SessionKind.Trial)
                buttons.Add(new HudView.PanelButton(Loc.T("trial.list"), UiIcon.Menu, false, () => { ShowLevels(); ShowSet(); }));
            _hud.ShowPanel(Loc.T("lose.time.title"), Loc.T("lose.time.body"), buttons.ToArray());
        }

        /// <summary>Testler: süreyi ilerletir (oyun akışı koşullarıyla).</summary>
        public void AdvanceTimeForTest(float sec)
        {
            for (float t = 0f; t < sec; t += 0.1f) TimedTick(0.1f);
        }
    }
}
