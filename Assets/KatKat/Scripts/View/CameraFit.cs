using UnityEngine;

namespace KatKat.View
{
    /// <summary>Ortografik kamerayı, verilen tasarım genişliği ve yüksekliği ekrana sığacak şekilde ayarlar.</summary>
    public static class CameraFit
    {
        public static void Fit(Camera cam, float designWidth, float designHeight)
        {
            float aspect = cam.aspect > 0f ? cam.aspect : 9f / 16f;
            cam.orthographicSize = Mathf.Max(designHeight * 0.5f, designWidth * 0.5f / aspect);
        }

        public static float HalfWidth(Camera cam) => cam.orthographicSize * cam.aspect;
    }
}
