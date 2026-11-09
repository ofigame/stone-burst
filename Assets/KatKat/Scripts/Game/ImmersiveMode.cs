using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Android yapışkan sürükleyici mod (kullanıcı, Redmi: telefonun sanal geri/ana sayfa/uygulamalar tuşları gizli
    /// olsun; alttan sürükleyince geçici çıksın, ilk dokunuşta işlemesin). Sistem çubukları gizlenir ve
    /// IMMERSIVE_STICKY ile kenardan kaydırınca yarı saydam, kısa süreli görünür. Uygulamaya her dönüşte (odak)
    /// yeniden uygulanır; bazı arayüzler (MIUI) çubukları geri getirebiliyor.
    /// </summary>
    public sealed class ImmersiveMode : MonoBehaviour
    {
        private static ImmersiveMode _instance;

        public static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("ImmersiveMode");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ImmersiveMode>();
        }

        // Unity (GameActivity) pencere/insets ayarını açılışta ve odakta bizden sonra yapıp çubukları geri getirebiliyor:
        // gizleme hemen ve 0,3 / 1 / 2,5 sn sonra yeniden uygulanır.
        private float _reapplyAt = -1f;
        private int _reapplyStep;
        private static readonly float[] Delays = { 0.3f, 1f, 2.5f };

        private void Start() => Schedule();

        private void OnApplicationFocus(bool focus)
        {
            if (focus) Schedule();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) Schedule();
        }

        private void Schedule()
        {
            Apply();
            _reapplyStep = 0;
            _reapplyAt = Time.unscaledTime + Delays[0];
        }

        private void Update()
        {
            if (_reapplyAt < 0f || Time.unscaledTime < _reapplyAt) return;
            Apply();
            _reapplyStep++;
            _reapplyAt = _reapplyStep < Delays.Length ? Time.unscaledTime + Delays[_reapplyStep] - Delays[_reapplyStep - 1] : -1f;
        }

        public static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
            try
            {
                // Unity 6.6 (GameActivity) kendi insets denetleyicisiyle çubukları yönetiyor: önce onun API'si.
                var insets = UnityEngine.Android.AndroidApplication.currentWindowInsets;
                insets.SetSystemBarsBehavior(UnityEngine.Android.AndroidWindowInsets.SystemBarsBehavior.ShowTransientBarsBySwipe);
                insets.Hide(UnityEngine.Android.AndroidWindowInsets.Type.NavigationBars | UnityEngine.Android.AndroidWindowInsets.Type.StatusBars);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[KatKat] AndroidWindowInsets uygulanamadı: " + e.Message);
            }
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                        using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                        {
                            // LAYOUT_STABLE | LAYOUT_HIDE_NAVIGATION | LAYOUT_FULLSCREEN | HIDE_NAVIGATION | FULLSCREEN | IMMERSIVE_STICKY
                            const int flags = 0x100 | 0x200 | 0x400 | 0x2 | 0x4 | 0x1000;
                            decor.Call("setSystemUiVisibility", flags);
                            // Android 11+: aynı davranışın yeni API'si (kaydırınca geçici göster).
                            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                            {
                                if (version.GetStatic<int>("SDK_INT") >= 30)
                                {
                                    using (var ctrl = window.Call<AndroidJavaObject>("getInsetsController"))
                                    using (var types = new AndroidJavaClass("android.view.WindowInsets$Type"))
                                    {
                                        if (ctrl != null)
                                        {
                                            ctrl.Call("hide", types.CallStatic<int>("systemBars"));
                                            ctrl.Call("setSystemBarsBehavior", 2); // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                                        }
                                    }
                                }
                            }
                        }
                    }));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[KatKat] Sürükleyici mod uygulanamadı: " + e.Message);
            }
#endif
        }
    }
}
