using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>KatKat/Layer shader'ının köşe biçiminde düz dörtgenler biriktirir.</summary>
    public sealed class LayerMeshBuilder
    {
        private readonly List<Vector3> _pos = new List<Vector3>(64);
        private readonly List<Color32> _col = new List<Color32>(64);
        private readonly List<Vector2> _uv = new List<Vector2>(64);
        private readonly List<Vector2> _face = new List<Vector2>(64);
        private readonly List<Vector4> _icon = new List<Vector4>(64);
        private readonly List<int> _tri = new List<int>(96);

        public void Clear()
        {
            _pos.Clear(); _col.Clear(); _uv.Clear(); _face.Clear(); _icon.Clear(); _tri.Clear();
        }

        /// <summary>Shader'daki yüzey türü (TEXCOORD2.w): parlak üst yüz, katman yanı, düz (gölge/yuva).</summary>
        public const float Face = 0f, Side = 1f, Flat = 2f;

        /// <param name="iconCell">Atlas hücresi; iconStrength 0 ise işaret çizilmez.</param>
        public void AddQuad(Vector2 center, Vector2 size, Color32 color, Vector2 iconCell, float iconStrength, float shade = 1f, float kind = Face)
        {
            int b = _pos.Count;
            float hx = size.x * 0.5f, hy = size.y * 0.5f;
            _pos.Add(new Vector3(center.x - hx, center.y - hy, 0f));
            _pos.Add(new Vector3(center.x - hx, center.y + hy, 0f));
            _pos.Add(new Vector3(center.x + hx, center.y + hy, 0f));
            _pos.Add(new Vector3(center.x + hx, center.y - hy, 0f));
            _uv.Add(new Vector2(0f, 0f)); _uv.Add(new Vector2(0f, 1f)); _uv.Add(new Vector2(1f, 1f)); _uv.Add(new Vector2(1f, 0f));
            var face = new Vector2(1f, shade);
            var icon = new Vector4(iconCell.x, iconCell.y, iconStrength, kind);
            for (int i = 0; i < 4; i++) { _col.Add(color); _face.Add(face); _icon.Add(icon); }
            _tri.Add(b); _tri.Add(b + 1); _tri.Add(b + 2);
            _tri.Add(b); _tri.Add(b + 2); _tri.Add(b + 3);
        }

        public void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(_pos);
            mesh.SetColors(_col);
            mesh.SetUVs(0, _uv);
            mesh.SetUVs(1, _face);
            mesh.SetUVs(2, _icon);
            mesh.SetTriangles(_tri, 0, true);
        }
    }
}
