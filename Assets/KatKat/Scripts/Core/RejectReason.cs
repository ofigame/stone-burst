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
        /// <summary>Sürükle-değiştir: komşu yok, örtülü/özel/korunan yığın ya da komşuda o katta taş yok.</summary>
        SwapInvalid,
        /// <summary>Sürükle-değiştir: yer değişimi 3+ aynı renk grup oluşturmuyor (taş geri döner, hamle harcanmaz).</summary>
        SwapNoMatch,
        /// <summary>Kova kipi: kova yok ya da içinde taş yok (gönderilemez).</summary>
        BucketEmpty,
        /// <summary>2026-11-26: Altın Rulo dokunarak değil, komşu taşa sürüklenerek kullanılır.</summary>
        SpecialNeedsDrag,
        /// <summary>2026-11-26: özel taş sürüklemesi geçersiz (roket yalnız rokete, Altın Rulo renkli taşa ya da özel taşa).</summary>
        SpecialDragInvalid,
    }
}
