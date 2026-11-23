using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>Kova sırası öğesi: rengi (miktar yalnız bölüm verisinde renk hedefini hesaplamak için).</summary>
    public struct BucketOrder
    {
        public LayerColor Color;
        public int Amount;
        public BucketOrder(LayerColor color, int amount) { Color = color; Amount = amount; }
    }

    /// <summary>Hamlede gönderilen bir kova (sunum: kova kalkar, yenisi gelir).</summary>
    public struct BucketSend
    {
        public LayerColor Color;
        /// <summary>Kovadaki taş (teslim edilen).</summary>
        public int Have;
        /// <summary>Kovanın alabileceği en çok taş (rengin kalan görevi).</summary>
        public int Need;
        /// <summary>Rengin görevi bitmeden gönderildi (renk sıranın sonuna eklendi). Gönderim her zaman oyuncunun dokunuşuyla.</summary>
        public bool Partial;
        /// <summary>Gönderimden sonra gelen kova (yoksa Active false).</summary>
        public bool NextActive;
        public LayerColor NextColor;
        public int NextNeed;
    }

    /// <summary>
    /// Kova kipi (kullanıcı 2026-11-23: "bazı bölümlerde roket ve altın rulonun olduğu alana kovalar gelecek. bu kovaları rengine göre
    /// taşlarla dolduracağız. Süre olacak"; ikinci tur: "kovada sayı olmayacak. Kovayı ne kadar doldurduysam o kadar taş alacak ve ne
    /// zaman göndermek istersem üzerine tıklayıp göndereceğim, yeni kova gelecek").
    /// <para>Sırada bir etkin kova vardır. Soyulan taş kovanın rengindeyse kovaya girer (en çok o rengin kalan görevi kadar). Başka
    /// renkler kovaya girmez. Oyuncu kovaya dokunursa içindeki kadarı teslim edilir; rengin görevi bitmediyse renk sıranın sonuna
    /// eklenir; sıradaki (görevi bitmemiş) renk gelir. Torba kendiliğinden gitmez ve boşken de gönderilebilir (2026-11-23). Renk hedefleri
    /// yalnız teslimle ilerler; hepsi tamamlanınca bölüm kazanılır (süre bitmeden). Kural <see cref="RulesEngine"/>'de.</para>
    /// </summary>
    public sealed class BucketRun
    {
        public readonly List<BucketOrder> Queue = new List<BucketOrder>();
        public bool Active;
        public LayerColor Color;
        /// <summary>Kovanın alabileceği en çok taş: geldiği andaki kalan renk görevi.</summary>
        public int Need;
        public int Have;
        /// <summary>Bugüne dek gönderilen kova sayısı (sunum).</summary>
        public int Sent;

        public BucketRun Clone()
        {
            var b = new BucketRun { Active = Active, Color = Color, Need = Need, Have = Have, Sent = Sent };
            b.Queue.AddRange(Queue);
            return b;
        }

        public bool ContentEquals(BucketRun o)
        {
            if (o == null || Active != o.Active || Color != o.Color || Need != o.Need || Have != o.Have || Sent != o.Sent || Queue.Count != o.Queue.Count) return false;
            for (int i = 0; i < Queue.Count; i++) if (Queue[i].Color != o.Queue[i].Color || Queue[i].Amount != o.Queue[i].Amount) return false;
            return true;
        }
    }
}
