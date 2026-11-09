using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>Seviye dosyasındaki sabit renk kimlikleri.</summary>
    public static class ColorIds
    {
        public static bool TryParse(string id, out LayerColor color)
        {
            switch (id)
            {
                case "coral": color = LayerColor.Coral; return true;
                case "blue": color = LayerColor.Blue; return true;
                case "yellow": color = LayerColor.Yellow; return true;
                case "purple": color = LayerColor.Purple; return true;
                default: color = default; return false;
            }
        }

        public static string ToId(LayerColor c)
        {
            switch (c)
            {
                case LayerColor.Coral: return "coral";
                case LayerColor.Blue: return "blue";
                case LayerColor.Yellow: return "yellow";
                default: return "purple";
            }
        }
    }
}
