namespace KatKat.Core
{
    public enum RejectReason
    {
        None = 0,
        NotIdle,
        OutOfBounds,
        NotSelectable,
        GroupTooSmall,
        RollEmpty,
        RollNoEffect,
        NoMovesLeft,
        /// <summary>Döndürme: hücrede taban yok, örtülü ya da ikiden az katman.</summary>
        NotRotatable,
        /// <summary>Boya: kaynak yok.</summary>
        PaintEmpty,
        /// <summary>Boya: örtülü/boş/özel taşlı hücre.</summary>
        NotPaintable,
        /// <summary>Boya: aynı renge boyama (kaynak harcamaz).</summary>
        SameColor,
        /// <summary>Tepsi: yuva boş ya da hedefte etkisi yok.</summary>
        TrayEmpty,
        TrayNoEffect,
    }
}
