using System;

namespace KatKat.Core
{
    /// <summary>
    /// Bir bölüm oturumu: güncel durum, tek adımlı geri alma görüntüsü ve yeniden başlatma.
    /// Sunumun meşgul olup olmadığı burada değil, girdi kapısında tutulur.
    /// </summary>
    public sealed class GameSession
    {
        private readonly LevelState _initial;
        private LevelState _undo;
        private bool _victoryRecorded;

        public LevelState State { get; private set; }

        public GameSession(LevelState initial)
        {
            _initial = initial ?? throw new ArgumentNullException(nameof(initial));
            State = initial.Clone();
        }

        public bool CanUndo => _undo != null && !_victoryRecorded;

        public MoveResult Submit(Command cmd)
        {
            var snapshot = State.Clone();
            var result = RulesEngine.TryApply(State, cmd);
            if (result.Accepted) _undo = snapshot;
            return result;
        }

        /// <summary>Son tam oyuncu hamlesini bütün zincirleriyle geri alır. Kayıp ekranından kullanılabilir.</summary>
        public bool Undo()
        {
            if (!CanUndo) return false;
            State = _undo;
            _undo = null;
            return true;
        }

        /// <summary>Zafer kaydı yazıldıktan sonra geri alma kapanır.</summary>
        public void MarkVictoryRecorded() => _victoryRecorded = true;

        /// <summary>
        /// v12 ödüllü reklam: hamle eklenir; hamle bittiği için kaybedilmiş bölüm sürer. Geri alma noktası da aynı
        /// hamleyi alır (geri almak ödülü silmez). Zafer kaydından sonra ya da hamle yok kaybında etkisiz.
        /// </summary>
        public bool GrantMoves(int n)
        {
            if (n <= 0 || _victoryRecorded || State.Status == GameStatus.Won || State.Status == GameStatus.LostNoLegalMoves) return false;
            State.MovesLeft += n;
            if (State.Status == GameStatus.LostOutOfMoves) State.Status = GameStatus.Idle;
            if (_undo != null) _undo.MovesLeft += n;
            return true;
        }

        /// <summary>
        /// Reklam ödülü (2026-11-09, kullanıcı: "1 hamle + bir tane istediği taştan istediği yere yerleştirme hakkı"): seçilen
        /// renkte bir katman, etkin, örtüsüz, özel taşsız ve kristal olmayan yığının en üstüne konur. Soyma tetiklemez;
        /// geri alma noktası silinir (yerleştirme geri alınmaz). Yalnız oyun sürerken.
        /// </summary>
        public bool PlaceLayer(int x, int y, LayerColor color)
        {
            if (State.Status != GameStatus.Idle || _victoryRecorded) return false;
            var b = State.Board;
            if (!b.InBounds(x, y)) return false;
            var cell = b[x, y];
            if (!cell.Active || cell.HasCover || cell.HasSpecial || cell.Protected) return false;
            cell.Layers.Add(color);
            _undo = null;
            return true;
        }

        public void Restart()
        {
            State = _initial.Clone();
            _undo = null;
            _victoryRecorded = false;
        }
    }
}
