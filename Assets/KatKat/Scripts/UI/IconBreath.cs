using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// Seçili alt menü ikonu yavaşça büyüyüp küçülür (kullanıcı, 2026-11-11: "seçili olan menü iconu büyüyüp küçülsün").
    /// Yalnız seçili ikonda; diğerleri sabit. Ölçek 1 → 1,08 → 1, periyot 1,6 sn, ölçeklenmemiş zaman (duraklatmadan bağımsız).
    /// Dokunma alanı değişmez (yalnız görselin ölçeği).
    /// </summary>
    public sealed class IconBreath : MonoBehaviour
    {
        public const float Amp = 0.08f, Period = 1.6f;
        private float _t;

        private void OnEnable() => _t = 0f;

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float dt)
        {
            if (Motion.Reduced) { transform.localScale = UnityEngine.Vector3.one; return; } // azaltılmış hareket
            _t += dt;
            float s = 1f + Amp * 0.5f * (1f - Mathf.Cos(_t * Mathf.PI * 2f / Period));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
