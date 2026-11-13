using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Ortografik kamerayı bir tasarım dikdörtgeni ekrana tam sığacak boyuta getirir: dar ekranda genişlik, geniş ekranda
    /// yükseklik belirleyicidir. Yalnız soyulma laboratuvarı (PeelLab, geliştirici sahnesi) kullanır.
    /// </summary>
    public static class OrthoFit
    {
        public static void Frame(Camera cam, Vector2 design)
        {
            float aspect = cam.aspect > 0f ? cam.aspect : 9f / 16f;
            float byHeight = design.y * 0.5f;
            float byWidth = design.x * 0.5f / aspect;
            cam.orthographicSize = byWidth > byHeight ? byWidth : byHeight;
        }
    }
}
