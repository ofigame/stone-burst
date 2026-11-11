using System;
using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>
    /// Deterministik kural motoru (belge Bölüm 5, 6, 8, 9; v5 KURALLAR). Zaman, rastgelelik veya sunum bilgisi kullanmaz.
    /// Geçersiz komut durumu değiştirmez. Geçerli komut bütün dalgaları çözer ve olay planını döndürür.
    /// Ataş hasarı (Bölüm 7) ve dip yıldızı aynı dalga sisteminde işlenir; rulo için ayrı motor yoktur.
    /// v5: özel taşlar (rulo roketi, gökkuşağı, birleşimler) aynı dalga sırasına girer; önce domino, sonra sıradaki özel taş.
    /// </summary>
    public static class RulesEngine
    {
        /// <summary>
        /// Domino (zincir) kuralı. v13 (kullanıcı, 2026-11-08: "Altından birlikte çıkan 3+ aynı renk patlar"): <see cref="ChainNewOnly"/>
        /// = soyulmayla altından YENİ bir renk çıkan hücrelerden yan yana en az 3 aynı renkli olanlar kendiliğinden soyulur;
        /// açık eski komşu ne engeller ne katılır. Ölçüm için (LevelLab chaincmp/chainfit): 0 = v1–v12 kuralı (bileşenin
        /// tamamı yeni olmalı), −1 = renk şartsız yeni-yalnız, N > 0 = 3+ bileşende en az N yeni (komşular katılır).
        /// </summary>
        public const int ChainNewOnly = -2;
        [ThreadStatic] private static int? _chainTouch;
        public static int ChainTouch { get => _chainTouch ?? ChainNewOnly; set => _chainTouch = value; }
        /// <summary>Ölçüm aracının bu iş parçacığındaki seçimini kaldırır (oyun kuralına döner).</summary>
        public static void ResetChainRule() => _chainTouch = null;

        public const int RollGroupSize = 5;
        /// <summary>v5 §2: dokunulan grup bu boyuttaysa rulo roketi, <see cref="RainbowGroupSize"/> ve üstüyse gökkuşağı doğar.</summary>
        public const int RocketGroupSize = 5;
        public const int RainbowGroupSize = 8;

        public static MoveResult TryApply(LevelState state, Command cmd)
        {
            var reject = Validate(state, cmd, out var wave0);
            if (reject != RejectReason.None)
            {
                var r = MoveResult.Reject(cmd, reject);
                if (reject == RejectReason.GroupTooSmall)
                    foreach (var i in wave0) r.TappedGroup.Add(new Coord(state.Board.X(i), state.Board.Y(i)));
                return r;
            }

            var result = new MoveResult { Accepted = true, Command = cmd };
            int scoreBefore = state.Score;

            if (cmd.Type == CommandType.UseSpecial)
            {
                // v6 tepsi: hamle harcamaz. Özel taş hedef hücrede ateşlenir (Altın Rulo hedefin üst rengini toplar).
                var kind = state.Tray[cmd.Slot];
                state.Tray.RemoveAt(cmd.Slot);
                result.UsedSpecial = kind;
                result.UsedAt = new Coord(cmd.X, cmd.Y);
                var target = state.Board[cmd.X, cmd.Y];
                var resolver = new Resolver(state, result);
                resolver.EnqueueFromTray(state.Board.Index(cmd.X, cmd.Y), kind, target.IsSelectable ? target.Top : default);
                resolver.Run(null, WaveKind.Special, -1);
                Finish(state, result, scoreBefore);
                return result;
            }

            state.MovesLeft--;

            if (cmd.Type == CommandType.Rotate)
            {
                // Döner taban (M08): alttan üste [a,b,c] → [c,a,b]; eski üst en alta iner. Soyulma, toplama,
                // otomatik zincir yok: oluşan grup bir sonraki hamlede seçilir.
                var layers = state.Board[cmd.X, cmd.Y].Layers;
                var top = layers[layers.Count - 1];
                layers.RemoveAt(layers.Count - 1);
                layers.Insert(0, top);
                result.Rotated = new Coord(cmd.X, cmd.Y);
                result.RotatedLayers.AddRange(layers);
                Finish(state, result, scoreBefore);
                return result;
            }

            if (cmd.Type == CommandType.Paint)
            {
                // M12: yalnızca üst renk değişir; toplama, hedef, kilit, zincir yok. Yeni grup ayrıca seçilir.
                var layers = state.Board[cmd.X, cmd.Y].Layers;
                result.Painted = new Coord(cmd.X, cmd.Y);
                result.PaintFrom = layers[layers.Count - 1];
                result.PaintTo = cmd.Color;
                layers[layers.Count - 1] = cmd.Color;
                state.PaintLeft--;
                Finish(state, result, scoreBefore);
                return result;
            }

            if (cmd.Type == CommandType.Tap && state.Board[cmd.X, cmd.Y].HasSpecial)
            {
                // Özel taşa dokunuş: ateşlenir (kenar komşusunda özel taş varsa birleşim).
                result.TappedGroup.Add(new Coord(cmd.X, cmd.Y));
                var resolver = new Resolver(state, result);
                resolver.EnqueueTapped(state.Board.Index(cmd.X, cmd.Y));
                resolver.Run(null, WaveKind.Special, -1);
                Finish(state, result, scoreBefore);
                return result;
            }

            int tapped = -1;
            if (cmd.Type == CommandType.Tap)
            {
                tapped = state.Board.Index(cmd.X, cmd.Y);
                foreach (var i in wave0) result.TappedGroup.Add(new Coord(state.Board.X(i), state.Board.Y(i)));
                // Rulo yalnızca doğrudan dokunulan ilk gruptan; dolu yuva korunur, ikinci rulo yok.
                if (state.RollEnabled && wave0.Count >= RollGroupSize && !state.RollSlotFull)
                {
                    state.RollSlotFull = true;
                    result.RollAwarded = true;
                }
            }
            else
            {
                state.RollSlotFull = false;
                result.RollConsumed = true;
            }

            new Resolver(state, result).Run(wave0, cmd.Type == CommandType.Tap ? WaveKind.Tap : WaveKind.Roll, tapped);
            Finish(state, result, scoreBefore);
            return result;
        }

        private static void Finish(LevelState state, MoveResult result, int scoreBefore)
        {
            state.Status = Evaluate(state);
            result.MovesLeftAfter = state.MovesLeft;
            result.StatusAfter = state.Status;
            result.ScoreGained = state.Score - scoreBefore;
        }

        /// <summary>Komutu durumu değiştirmeden denetler; geçerliyse ilk dalganın hücrelerini verir.</summary>
        public static RejectReason Validate(LevelState state, Command cmd, out List<int> wave0)
        {
            wave0 = new List<int>();
            if (state.Status != GameStatus.Idle) return RejectReason.NotIdle;
            var board = state.Board;
            if (cmd.Type == CommandType.UseSpecial)
            {
                if (cmd.Slot < 0 || cmd.Slot >= state.Tray.Count) return RejectReason.TrayEmpty;
                if (!board.InBounds(cmd.X, cmd.Y)) return RejectReason.OutOfBounds;
                return TrayHasEffect(board, state.Tray[cmd.Slot], cmd.X, cmd.Y) ? RejectReason.None : RejectReason.TrayNoEffect;
            }
            if (state.MovesLeft <= 0) return RejectReason.NoMovesLeft;
            if (cmd.Type == CommandType.Tap)
            {
                if (!board.InBounds(cmd.X, cmd.Y)) return RejectReason.OutOfBounds;
                var cell = board[cmd.X, cmd.Y];
                if (cell.Active && cell.HasSpecial) return RejectReason.None;
                if (!cell.IsSelectable) return RejectReason.NotSelectable;
                wave0 = GroupFinder.Component(board, cmd.X, cmd.Y);
                return wave0.Count >= GroupFinder.MinGroup ? RejectReason.None : RejectReason.GroupTooSmall;
            }
            if (cmd.Type == CommandType.Rotate)
            {
                if (!board.InBounds(cmd.X, cmd.Y)) return RejectReason.OutOfBounds;
                return board[cmd.X, cmd.Y].IsRotatable ? RejectReason.None : RejectReason.NotRotatable;
            }
            if (cmd.Type == CommandType.Paint)
            {
                if (state.PaintLeft <= 0) return RejectReason.PaintEmpty;
                if (!board.InBounds(cmd.X, cmd.Y)) return RejectReason.OutOfBounds;
                if (!board[cmd.X, cmd.Y].IsSelectable) return RejectReason.NotPaintable;
                return board[cmd.X, cmd.Y].Top == cmd.Color ? RejectReason.SameColor : RejectReason.None;
            }

            if (!state.RollSlotFull) return RejectReason.RollEmpty;
            if (cmd.Row < 0 || cmd.Row >= board.Height) return RejectReason.OutOfBounds;
            wave0 = RollTargets(board, cmd.Row);
            return wave0.Count > 0 ? RejectReason.None : RejectReason.RollNoEffect;
        }

        /// <summary>Satırdaki dolu ve örtüsüz aktif hücreler; boşluklar ve örtülü yığınlar atlanır.</summary>
        public static List<int> RollTargets(BoardState board, int row)
        {
            var list = new List<int>();
            for (int x = 0; x < board.Width; x++)
                if (board[x, row].IsSelectable) list.Add(board.Index(x, row));
            return list;
        }

        /// <summary>
        /// Tepsi taşının (x, y) hedefinde etkisi var mı: roket satırında/sütununda soyulacak ya da vurulacak bir şey;
        /// Altın Rulo için hedef örtüsüz dolu yığın (rengi toplanır).
        /// </summary>
        /// <summary>vr1 P3: tahta iki alana bölünmüş mü (herhangi bir hücrenin alanı 0 değil).</summary>
        public static bool HasAreas(BoardState board)
        {
            for (int i = 0; i < board.Count; i++) if (board[i].Area != 0) return true;
            return false;
        }

        public static bool TrayHasEffect(BoardState board, SpecialKind kind, int x, int y)
        {
            var c = board[x, y];
            if (!c.Active) return false;
            if (kind == SpecialKind.Rainbow) return c.IsSelectable;
            bool areas = HasAreas(board);
            for (int k = 0; k < (kind == SpecialKind.RocketRow ? board.Width : board.Height); k++)
            {
                var o = kind == SpecialKind.RocketRow ? board[k, y] : board[x, k];
                if (areas && o.Area != c.Area) continue;
                if (o.Active && (o.Layers.Count > 0 || o.HasCover || o.HasSpecial)) return true;
            }
            return false;
        }

        /// <summary>Tepsideki herhangi bir taşın etkili bir hedefi var mı.</summary>
        public static bool TrayUsable(LevelState state)
        {
            if (state.Tray.Count == 0) return false;
            var b = state.Board;
            foreach (var kind in state.Tray)
                for (int i = 0; i < b.Count; i++)
                    if (TrayHasEffect(b, kind, b.X(i), b.Y(i))) return true;
            return false;
        }

        /// <summary>Yasal komut: geçerli grup, özel taş, etkili rulo veya döndürülebilir taban (rehber §9); tepsi.</summary>
        public static bool HasLegalMove(LevelState state)
        {
            if (TrayUsable(state)) return true;
            if (state.MovesLeft <= 0) return false;
            if (GroupFinder.HasValidGroup(state.Board)) return true;
            for (int i = 0; i < state.Board.Count; i++)
            {
                var c = state.Board[i];
                if (c.IsRotatable || (c.Active && c.HasSpecial)) return true;
                if (state.PaintLeft > 0 && c.IsSelectable) return true;
            }
            if (!state.RollSlotFull) return false;
            for (int y = 0; y < state.Board.Height; y++)
                if (RollTargets(state.Board, y).Count > 0) return true;
            return false;
        }

        /// <summary>Sonuç sırası: Won → OutOfMoves → NoLegalMoves → Idle.</summary>
        public static GameStatus Evaluate(LevelState state)
        {
            if (state.ProtectedBroken) return GameStatus.LostProtected; // kristal kuralı kazanmadan önce
            if (state.AllGoalsComplete) return GameStatus.Won;
            // v6: hamle bitse de tepside kullanılabilir özel taş varsa oyun sürer (tepsi hamle harcamaz).
            if (state.MovesLeft <= 0 && !TrayUsable(state)) return GameStatus.LostOutOfMoves;
            if (!HasLegalMove(state)) return GameStatus.LostNoLegalMoves;
            return GameStatus.Idle;
        }

        // ---- Bitiş kutlaması (v5 §4) ------------------------------------------------------------------

        /// <summary>
        /// Kazanılmış bölümde: önce tahtadaki özel taşlar ateşlenir, sonra kalan her hamle için sabit tohumla seçilen dolu
        /// hücreye rulo roketi düşer (satır/sütun sırayla) ve ateşlenir. Yalnızca puan ve gösteri; durum Won kalır.
        /// Her adım ayrı bir sonuç (sunum sırayla oynatır).
        /// </summary>
        public static List<MoveResult> Celebrate(LevelState state, int seed)
        {
            var steps = new List<MoveResult>();
            if (state.Status != GameStatus.Won) return steps;
            var board = state.Board;
            // 1) Kalan özel taşlar.
            for (int i = 0; i < board.Count; i++)
            {
                if (!board[i].Active || !board[i].HasSpecial) continue;
                var r = new MoveResult { Accepted = true, Command = Command.Tap(board.X(i), board.Y(i)) };
                int before = state.Score;
                var res = new Resolver(state, r);
                res.EnqueueSingle(i);
                res.Run(null, WaveKind.Special, -1);
                r.ScoreGained = state.Score - before;
                r.MovesLeftAfter = state.MovesLeft;
                r.StatusAfter = state.Status;
                steps.Add(r);
            }
            uint rng = (uint)seed * 2654435761u + 0x9E3779B9u;
            // 1b) v6: tepside kalan özel taşlar sabit tohumla seçilen dolu hücrede ateşlenir.
            while (state.Tray.Count > 0)
            {
                var kind = state.Tray[0];
                state.Tray.RemoveAt(0);
                var candidates = new List<int>();
                for (int i = 0; i < board.Count; i++)
                    if (board[i].IsSelectable) candidates.Add(i);
                if (candidates.Count == 0) { state.Tray.Clear(); break; }
                rng = rng * 1664525u + 1013904223u;
                int pick = candidates[(int)(rng >> 8) % candidates.Count];
                var r = new MoveResult { Accepted = true, Command = Command.UseSpecial(0, board.X(pick), board.Y(pick)) };
                r.UsedSpecial = kind;
                r.UsedAt = new Coord(board.X(pick), board.Y(pick));
                int before = state.Score;
                var res = new Resolver(state, r);
                res.EnqueueFromTray(pick, kind, board[pick].Top);
                res.Run(null, WaveKind.Special, -1);
                r.ScoreGained = state.Score - before;
                r.MovesLeftAfter = state.MovesLeft;
                r.StatusAfter = state.Status;
                steps.Add(r);
            }
            // 2) Kalan hamleler rokete dönüşür.
            int n = 0;
            while (state.MovesLeft > 0)
            {
                var candidates = new List<int>();
                for (int i = 0; i < board.Count; i++)
                    if (board[i].IsSelectable) candidates.Add(i);
                if (candidates.Count == 0) break;
                rng = rng * 1664525u + 1013904223u;
                int pick = candidates[(int)(rng >> 8) % candidates.Count];
                state.MovesLeft--;
                board[pick].Special = n++ % 2 == 0 ? SpecialKind.RocketRow : SpecialKind.RocketColumn;
                var r = new MoveResult { Accepted = true, Command = Command.Tap(board.X(pick), board.Y(pick)) };
                r.SpecialCreated = board[pick].Special;
                r.SpecialCreatedAt = new Coord(board.X(pick), board.Y(pick));
                int before = state.Score;
                var res = new Resolver(state, r);
                res.EnqueueSingle(pick);
                res.Run(null, WaveKind.Special, -1);
                r.ScoreGained = state.Score - before;
                r.MovesLeftAfter = state.MovesLeft;
                r.StatusAfter = state.Status;
                steps.Add(r);
            }
            return steps;
        }

        // ---- Dalga çözümü --------------------------------------------------------------------------------

        private struct Pending
        {
            public int Cell;
            public SpecialKind Kind;
            public LayerColor Color;
            public int ComboCell;          // -1: birleşim yok
            public SpecialKind ComboKind;
            public LayerColor ComboColor;
            /// <summary>Tepsiden: hedef hücrede tahta özel taşı yoktur, hücre temizlenmez.</summary>
            public bool FromTray;
        }

        /// <summary>Bir hamlenin bütün dalgaları: ilk dalga, domino (zincir) ve sıraya giren özel taşlar.</summary>
        private sealed class Resolver
        {
            private readonly LevelState _state;
            private readonly BoardState _board;
            private readonly MoveResult _result;
            private readonly Queue<Pending> _queue = new Queue<Pending>();
            private readonly bool[] _queued;
            private readonly bool[] _hasBeforeTop, _peeled, _direct;
            private readonly LayerColor[] _beforeTop, _removedColor;

            public Resolver(LevelState state, MoveResult result)
            {
                _state = state;
                _board = state.Board;
                _result = result;
                int n = _board.Count;
                _queued = new bool[n];
                _hasBeforeTop = new bool[n];
                _peeled = new bool[n];
                _direct = new bool[n];
                _beforeTop = new LayerColor[n];
                _removedColor = new LayerColor[n];
            }

            /// <summary>Oyuncunun dokunduğu özel taş; kenar komşusunda özel taş varsa birleşim (sağ, sol, alt, üst).</summary>
            public void EnqueueTapped(int cell)
            {
                var c = _board[cell];
                var p = new Pending { Cell = cell, Kind = c.Special, Color = c.SpecialColor, ComboCell = -1 };
                int x = _board.X(cell), y = _board.Y(cell);
                int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (!_board.InBounds(nx, ny)) continue;
                    var o = _board[nx, ny];
                    if (!o.Active || !o.HasSpecial) continue;
                    p.ComboCell = _board.Index(nx, ny);
                    p.ComboKind = o.Special;
                    p.ComboColor = o.SpecialColor;
                    _queued[p.ComboCell] = true;
                    break;
                }
                _queued[cell] = true;
                _queue.Enqueue(p);
            }

            /// <summary>v6 tepsi: tahtada olmayan özel taş hedef hücrede ateşlenir (birleşim yok).</summary>
            public void EnqueueFromTray(int cell, SpecialKind kind, LayerColor color)
            {
                _queue.Enqueue(new Pending { Cell = cell, Kind = kind, Color = color, ComboCell = -1, FromTray = true });
            }

            /// <summary>Tek özel taş (birleşimsiz): bitiş kutlaması ve tetiklenen taşlar.</summary>
            public void EnqueueSingle(int cell)
            {
                if (_queued[cell]) return;
                var c = _board[cell];
                _queued[cell] = true;
                _queue.Enqueue(new Pending { Cell = cell, Kind = c.Special, Color = c.SpecialColor, ComboCell = -1 });
            }

            public void Run(List<int> first, WaveKind firstKind, int tappedCell)
            {
                int limit = _board.TotalLayers() + _board.Count * 4 + 16;
                List<int> wave = first;
                var kind = firstKind;
                Pending? fired = null;
                bool firstWave = true;

                while (true)
                {
                    if (wave == null || (wave.Count == 0 && fired == null))
                    {
                        if (_queue.Count == 0) break;
                        var p = _queue.Dequeue();
                        wave = Fire(p, out var hits);
                        fired = p;
                        kind = WaveKind.Special;
                        Array.Clear(_direct, 0, _direct.Length);
                        foreach (var h in hits) _direct[h] = true;
                    }
                    if (_result.Waves.Count > limit)
                        throw new InvalidOperationException($"Dalga güvenlik sınırı aşıldı ({limit}). Kural hatası.");

                    var pw = ApplyWave(wave, kind, fired);
                    // v5 §2: özel taş yalnızca doğrudan dokunulan ilk gruptan doğar.
                    if (firstWave && kind == WaveKind.Tap && tappedCell >= 0 && _state.SpecialsEnabled) Create(first, tappedCell, pw);
                    firstWave = false;

                    var next = ChainCandidates();
                    Array.Clear(_direct, 0, _direct.Length);
                    fired = null;
                    if (next.Count > 0) { wave = next; kind = WaveKind.Chain; }
                    else wave = null;
                }
            }

            /// <summary>Özel taşın etki alanı: soyulacak hücreler ve doğrudan vuruş alan örtülü hücreler.</summary>
            private List<int> Fire(Pending p, out List<int> hits)
            {
                hits = new List<int>();
                var peel = new List<int>();
                if (!p.FromTray) Clear(p.Cell);
                if (p.ComboCell >= 0) Clear(p.ComboCell);

                var area = new List<int>();
                bool aRocket = p.Kind.IsRocket(), bRocket = p.ComboKind.IsRocket();
                bool aRain = p.Kind == SpecialKind.Rainbow, bRain = p.ComboKind == SpecialKind.Rainbow;
                int x = _board.X(p.Cell), y = _board.Y(p.Cell);

                if (p.ComboCell < 0)
                {
                    if (p.Kind == SpecialKind.RocketRow) Row(y, area);
                    else if (p.Kind == SpecialKind.RocketColumn) Column(x, area);
                    else if (aRain) ColorCells(p.Color, area);
                }
                else if (aRocket && bRocket)
                {
                    Row(y, area);
                    Column(x, area);
                }
                else if (aRain && bRain)
                {
                    for (int i = 0; i < _board.Count; i++) area.Add(i);
                }
                else
                {
                    // Rulo + gökkuşağı: gökkuşağı rengindeki açık yığınlar sırayla rokete dönüşür.
                    var color = aRain ? p.Color : p.ComboColor;
                    var targets = new List<int>();
                    ColorCells(color, targets);
                    if (HasAreas(_board)) { int ta = _board[p.Cell].Area; targets.RemoveAll(t => _board[t].Area != ta); }
                    int n = 0;
                    foreach (var t in targets)
                    {
                        _board[t].Special = n++ % 2 == 0 ? SpecialKind.RocketRow : SpecialKind.RocketColumn;
                        EnqueueSingle(t);
                        _pendingTriggered.Add(t);
                    }
                    return peel; // bu dalga yalnızca dönüşümdür; roketler sırayla ateşlenir.
                }

                // vr1 P3 iki alan: roket ve Altın Rulo yalnız ateşlendiği hücrenin alanında işler.
                if (HasAreas(_board)) { int a = _board[p.Cell].Area; area.RemoveAll(i => _board[i].Area != a); }
                var seen = new HashSet<int>();
                foreach (var i in area)
                {
                    if (!seen.Add(i)) continue;
                    var c = _board[i];
                    if (!c.Active) continue;
                    if (c.HasSpecial)
                    {
                        if (!_queued[i]) { EnqueueSingle(i); _pendingTriggered.Add(i); }
                        continue;
                    }
                    if (c.HasCover) hits.Add(i);
                    else if (c.Layers.Count > 0) peel.Add(i);
                }
                return peel;
            }

            private readonly List<int> _pendingTriggered = new List<int>();

            private void Clear(int cell)
            {
                _board[cell].Special = SpecialKind.None;
                _board[cell].SpecialColor = default;
            }

            private void Row(int y, List<int> area)
            {
                for (int x = 0; x < _board.Width; x++) area.Add(_board.Index(x, y));
            }

            private void Column(int x, List<int> area)
            {
                for (int y = 0; y < _board.Height; y++) area.Add(_board.Index(x, y));
            }

            private void ColorCells(LayerColor color, List<int> area)
            {
                for (int i = 0; i < _board.Count; i++)
                    if (_board[i].IsSelectable && _board[i].Top == color) area.Add(i);
            }

            private PeelWave ApplyWave(List<int> wave, WaveKind kind, Pending? fired)
            {
                var board = _board;
                Array.Clear(_peeled, 0, _peeled.Length);
                for (int i = 0; i < board.Count; i++)
                {
                    _hasBeforeTop[i] = board[i].IsSelectable;
                    if (_hasBeforeTop[i]) _beforeTop[i] = board[i].Top;
                }

                var pw = new PeelWave { Kind = kind, Index = _result.Waves.Count };
                if (fired.HasValue)
                {
                    var f = fired.Value;
                    pw.Fired = f.Kind;
                    pw.FiredAt = new Coord(board.X(f.Cell), board.Y(f.Cell));
                    if (f.ComboCell >= 0)
                    {
                        pw.ComboWith = f.ComboKind;
                        pw.ComboAt = new Coord(board.X(f.ComboCell), board.Y(f.ComboCell));
                    }
                }
                foreach (var t in _pendingTriggered) pw.Triggered.Add(new Coord(board.X(t), board.Y(t)));
                _pendingTriggered.Clear();

                int score = 0;
                // Eşzamanlı soyulma: her hücreden tam bir katman.
                var removedByColor = new int[4];
                wave.Sort();
                foreach (var i in wave)
                {
                    var cell = board[i];
                    var removed = cell.Top;
                    removedByColor[(int)removed]++;
                    _removedColor[i] = removed;
                    cell.Layers.RemoveAt(cell.Layers.Count - 1);
                    _peeled[i] = true;
                    if (cell.Protected) _state.ProtectedBroken = true; // v12 deneme: kristal soyuldu
                    AddGoal(_state, removed, kind == WaveKind.Chain);
                    score += Scoring.Layer(pw.Index);
                    // Dip yıldızı: hücre bu soyulmayla boşaldıysa bir kez.
                    bool star = cell.Layers.Count == 0 && cell.HasBottomStar;
                    if (star)
                    {
                        cell.HasBottomStar = false;
                        AddStar(_state);
                        score += Scoring.BottomStar;
                    }
                    pw.Cells.Add(new CellPeel(board.X(i), board.Y(i), removed, cell.Layers.Count,
                        cell.Layers.Count > 0 ? cell.Top : default, star));
                }

                // Örtü etkileri (Bölüm 7; Premium-v2 §9 adım 5–6; v5 doğrudan vuruş). Dalga öncesi örtü durumundan
                // bütün etkiler önce toplanır, sonra birlikte uygulanır.
                // Ataş ve buz: kenar komşusu soyulduysa ya da doğrudan vurulduysa dalga başına tek hasar.
                // Renk kilidi: dalgada kaldırılan kendi rengindeki katman kadar (+ doğrudan vuruş 1); sıfırda durur.
                // Zarf: kenar komşusundan mühür renginde katman kalktıysa ya da doğrudan vurulduysa açılır.
                var clipHits = new List<int>();
                var iceHits = new List<int>();
                var lockHits = new List<int>();
                var envelopes = new List<int>();
                for (int i = 0; i < board.Count; i++)
                {
                    var c = board[i];
                    bool d = _direct[i];
                    if (d) pw.DirectHits.Add(new Coord(board.X(i), board.Y(i)));
                    if (c.ClipHitsRemaining > 0 && (d || NextToPeeled(board, i, _peeled))) clipHits.Add(i);
                    else if (c.IceHitsRemaining > 0 && (d || NextToPeeled(board, i, _peeled))) iceHits.Add(i);
                    else if (c.LockRemaining > 0 && (d || removedByColor[(int)c.LockColor] > 0)) lockHits.Add(i);
                    else if (c.HasEnvelope && (d || NextToPeeledColor(board, i, _peeled, _removedColor, c.EnvelopeColor))) envelopes.Add(i);
                }
                foreach (var i in envelopes)
                {
                    board[i].HasEnvelope = false;
                    AddEnvelope(_state);
                    score += Scoring.Envelope;
                    pw.EnvelopeOpens.Add(new Coord(board.X(i), board.Y(i)));
                }
                foreach (var i in clipHits)
                {
                    board[i].ClipHitsRemaining--;
                    score += Scoring.CoverHit;
                    pw.ClipHits.Add(new ClipHit(board.X(i), board.Y(i), board[i].ClipHitsRemaining));
                }
                foreach (var i in iceHits)
                {
                    board[i].IceHitsRemaining--;
                    score += Scoring.CoverHit;
                    pw.IceHits.Add(new ClipHit(board.X(i), board.Y(i), board[i].IceHitsRemaining));
                }
                foreach (var i in lockHits)
                {
                    var c = board[i];
                    int amount = Math.Min(c.LockRemaining, removedByColor[(int)c.LockColor] + (_direct[i] ? 1 : 0));
                    c.LockRemaining -= amount;
                    score += Scoring.CoverHit;
                    pw.LockHits.Add(new LockHit(board.X(i), board.Y(i), c.LockColor, amount, c.LockRemaining));
                }
                // M09 sünger: kenar komşularından kalkan renkler gereken yuvaları doldurur (renk başına bir yuva,
                // dalgalar arası kalıcı); doğrudan vuruş eksik ilk yuvayı doldurur. Hepsi dolunca açılır, bir sünger toplanır.
                for (int i = 0; i < board.Count; i++)
                {
                    var c = board[i];
                    if (c.SpongeNeed == 0) continue;
                    int missing = c.SpongeNeed & ~c.SpongeHave;
                    int gain = NeighbourColors(board, i, _peeled, _removedColor) & missing;
                    if (gain == 0 && _direct[i]) gain = missing & -missing;
                    if (gain == 0) continue;
                    c.SpongeHave |= gain;
                    score += Scoring.CoverHit;
                    pw.SpongeHits.Add(new SpongeHit(board.X(i), board.Y(i), c.SpongeHave, c.SpongeNeed));
                    if (c.SpongeHave == c.SpongeNeed)
                    {
                        c.SpongeNeed = 0;
                        c.SpongeHave = 0;
                        AddToken(_state, GoalType.CollectSponges);
                        score += Scoring.Envelope;
                        pw.SpongeOpens.Add(new Coord(board.X(i), board.Y(i)));
                    }
                }

                // M10 bağlı ataş: grubun herhangi bir üyesine komşu soyulma (ya da doğrudan vuruş) gruba dalgada TEK hasar.
                var linkIds = new SortedSet<int>();
                for (int i = 0; i < board.Count; i++)
                {
                    var c = board[i];
                    if (c.LinkHits > 0 && (_direct[i] || NextToPeeled(board, i, _peeled))) linkIds.Add(c.LinkId);
                }
                foreach (var id in linkIds)
                {
                    score += Scoring.CoverHit;
                    for (int i = 0; i < board.Count; i++)
                    {
                        var c = board[i];
                        if (c.LinkId != id || c.LinkHits <= 0) continue;
                        c.LinkHits--;
                        pw.LinkHits.Add(new ClipHit(board.X(i), board.Y(i), c.LinkHits));
                    }
                }

                // M11 düğme: dibinde düğme olan yığın bu dalgada boşaldıysa basılır, bağlı geçit bir kez etkinleşir.
                // Açılan hücreler bu dalgada soyulmadığı için zincir tohumu olamaz.
                foreach (var i in wave)
                {
                    var c = board[i];
                    if (c.ButtonGate <= 0 || c.Layers.Count > 0) continue;
                    int gate = c.ButtonGate;
                    c.ButtonGate = 0;
                    pw.ButtonPresses.Add(new Coord(board.X(i), board.Y(i)));
                    OpenGate(gate, pw);
                    score += Scoring.Envelope;
                }

                // vr1 P3 zincir mührü: bu hamlenin otomatik zincir derinliği mühür derinliğine ulaşınca bağlı geçit açılır
                // (doğrudan dokunuş derinlik 0; mührün komşuluk şartı yok). Açılan hücreler bu dalgada soyulmaz.
                if (_state.SealWave > 0 && !_state.SealOpen && kind == WaveKind.Chain)
                {
                    int depth = 1;
                    foreach (var w in _result.Waves) if (w.Kind == WaveKind.Chain) depth++;
                    if (depth >= _state.SealWave)
                    {
                        _state.SealOpen = true;
                        pw.SealOpened = true;
                        OpenGate(_state.SealGate, pw);
                        score += Scoring.Envelope;
                    }
                }

                // vr1 P3 aşamalı hedef: etkin aşama dalga sonunda tamamlandıysa sonrakine geçilir (fazla aktarılmaz).
                if (_state.ActiveStage < _state.StageCount - 1)
                {
                    bool done = true;
                    foreach (var g in _state.Goals) if (g.Stage == _state.ActiveStage && !g.IsComplete) { done = false; break; }
                    if (done) { _state.ActiveStage++; pw.StageAdvanced = true; }
                }
                pw.StageAfter = _state.ActiveStage;

                _state.Score += score;
                pw.Score = score;
                pw.GoalProgressAfter = new int[_state.Goals.Count];
                for (int g = 0; g < _state.Goals.Count; g++) pw.GoalProgressAfter[g] = _state.Goals[g].Progress;
                _result.Waves.Add(pw);
                return pw;
            }

            /// <summary>Geçidi açar (düğme ya da mühür): bekleyen hücreler katmanlarıyla etkinleşir.</summary>
            private void OpenGate(int gate, PeelWave pw)
            {
                if (gate <= 0) return;
                var board = _state.Board;
                for (int j = 0; j < board.Count; j++)
                {
                    var g = board[j];
                    if (g.GateId != gate || g.Active) continue;
                    g.Active = true;
                    g.Layers.AddRange(g.GateLayers);
                    g.GateLayers.Clear();
                    g.GateId = 0;
                    pw.GateOpens.Add(new Coord(board.X(j), board.Y(j)));
                }
            }

            /// <summary>
            /// v5 §2: dokunulan grupta 5–7 → roket (geniş grup satır, değilse sütun), 8+ → Altın Rulo.
            /// v6: özel taş tahtaya değil tepsiye girer (tepsi doluysa yalnızca puan).
            /// </summary>
            private void Create(List<int> group, int tapped, PeelWave pw)
            {
                if (group == null || group.Count < RocketGroupSize) return;
                SpecialKind kind;
                if (group.Count >= RainbowGroupSize)
                {
                    kind = SpecialKind.Rainbow;
                    _state.Score += Scoring.RainbowCreated;
                    pw.Score += Scoring.RainbowCreated;
                }
                else
                {
                    int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
                    foreach (var i in group)
                    {
                        int x = _board.X(i), y = _board.Y(i);
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
                    kind = maxX - minX > maxY - minY ? SpecialKind.RocketRow : SpecialKind.RocketColumn;
                    _state.Score += Scoring.RocketCreated;
                    pw.Score += Scoring.RocketCreated;
                }
                _result.SpecialCreated = kind;
                _result.SpecialCreatedAt = new Coord(_board.X(tapped), _board.Y(tapped));
                _result.SpecialCreatedColor = _removedColor[tapped];
                if (_state.Tray.Count < LevelState.TrayCapacity)
                {
                    _state.Tray.Add(kind);
                    _result.SpecialToTray = true;
                }
            }

            /// <summary>Domino adayları: tamamı bu dalgada yeni açılmış aynı renkten oluşan 3+ bileşenler.</summary>
            private List<int> ChainCandidates()
            {
                var next = new List<int>();
                foreach (var comp in GroupFinder.AllComponents(_board))
                {
                    if (comp.Count < GroupFinder.MinGroup) continue;
                    if (ChainTouch < 0)
                    {
                        // Yalnız yeni açılanlar: bileşenin bu dalgada soyulmuş hücrelerinden oluşan alt gruplar (en az 3, yan yana).
                        // Açık eski komşu ne engeller ne katılır. -2: hücrenin rengi de değişmiş olmalı; -1: renk şartı yok.
                        var col = _board[comp[0]].Top;
                        var inComp = new HashSet<int>();
                        foreach (var i in comp)
                            if (_peeled[i] && (ChainTouch == -1 || !(_hasBeforeTop[i] && _beforeTop[i] == col))) inComp.Add(i);
                        var seen = new HashSet<int>();
                        foreach (var s0 in inComp)
                        {
                            if (!seen.Add(s0)) continue;
                            var part = new List<int> { s0 };
                            for (int k = 0; k < part.Count; k++)
                            {
                                int c = part[k], cx = _board.X(c), cy = _board.Y(c);
                                foreach (var nb in new[] { cx > 0 ? c - 1 : -1, cx < _board.Width - 1 ? c + 1 : -1, cy > 0 ? c - _board.Width : -1, cy < _board.Height - 1 ? c + _board.Width : -1 })
                                    if (nb >= 0 && inComp.Contains(nb) && seen.Add(nb)) part.Add(nb);
                            }
                            if (part.Count >= GroupFinder.MinGroup) next.AddRange(part);
                        }
                        continue;
                    }
                    if (ChainTouch > 0)
                    {
                        int fresh = 0;
                        foreach (var i in comp) if (_peeled[i]) fresh++;
                        if (fresh >= ChainTouch) next.AddRange(comp);
                        continue;
                    }
                    var color = _board[comp[0]].Top;
                    bool allNew = true;
                    foreach (var i in comp)
                    {
                        if (!_peeled[i] || (_hasBeforeTop[i] && _beforeTop[i] == color)) { allNew = false; break; }
                    }
                    if (allNew) next.AddRange(comp);
                }
                return next;
            }
        }

        private static bool NextToPeeled(BoardState board, int i, bool[] peeled)
        {
            int x = board.X(i), y = board.Y(i);
            return (x > 0 && peeled[i - 1]) || (x < board.Width - 1 && peeled[i + 1])
                || (y > 0 && peeled[i - board.Width]) || (y < board.Height - 1 && peeled[i + board.Width]);
        }

        private static bool NextToPeeledColor(BoardState board, int i, bool[] peeled, LayerColor[] removed, LayerColor color)
        {
            int x = board.X(i), y = board.Y(i);
            return (x > 0 && peeled[i - 1] && removed[i - 1] == color)
                || (x < board.Width - 1 && peeled[i + 1] && removed[i + 1] == color)
                || (y > 0 && peeled[i - board.Width] && removed[i - board.Width] == color)
                || (y < board.Height - 1 && peeled[i + board.Width] && removed[i + board.Width] == color);
        }

        /// <summary>Kenar komşularından bu dalgada kalkan renklerin maskesi.</summary>
        private static int NeighbourColors(BoardState board, int i, bool[] peeled, LayerColor[] removed)
        {
            int x = board.X(i), y = board.Y(i), m = 0;
            if (x > 0 && peeled[i - 1]) m |= CellState.Bit(removed[i - 1]);
            if (x < board.Width - 1 && peeled[i + 1]) m |= CellState.Bit(removed[i + 1]);
            if (y > 0 && peeled[i - board.Width]) m |= CellState.Bit(removed[i - board.Width]);
            if (y < board.Height - 1 && peeled[i + board.Width]) m |= CellState.Bit(removed[i + board.Width]);
            return m;
        }

        private static void AddToken(LevelState state, GoalType type)
        {
            foreach (var g in state.Goals)
                if (g.Type == type && g.Stage == state.ActiveStage) g.Progress++;
        }

        private static void AddEnvelope(LevelState state)
        {
            foreach (var g in state.Goals)
                if (g.Type == GoalType.CollectEnvelopes && g.Stage == state.ActiveStage) g.Progress++;
        }

        private static void AddGoal(LevelState state, LayerColor color, bool chain = false)
        {
            // v12 deneme: zincir hedefi yalnız domino dalgasıyla soyulan katmanları sayar.
            if (chain)
                foreach (var g in state.Goals)
                    if (g.Type == GoalType.CollectChain && g.Color == color && g.Stage == state.ActiveStage) g.Progress++;
            foreach (var g in state.Goals)
                if (g.Type == GoalType.CollectColor && g.Color == color && g.Stage == state.ActiveStage) g.Progress++;
        }

        private static void AddStar(LevelState state)
        {
            foreach (var g in state.Goals)
                if (g.Type == GoalType.CollectStars && g.Stage == state.ActiveStage) g.Progress++;
        }
    }
}
