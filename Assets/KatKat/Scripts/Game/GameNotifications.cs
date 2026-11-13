using System;
using System.Collections;
using System.Collections.Generic;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Unity.Notifications;
#endif

namespace KatKat.Game
{
    /// <summary>
    /// Yerel bildirimler (kullanıcı, 2026-11-13; plan <see cref="NotifyPlan"/>). Unity Mobile Notifications 2.5 (birleşik API).
    /// - İzin ilk kez kalp bitince oyun içi kartla sorulur ("Kalpler dolunca haber verelim mi?"); "Evet" → işletim sistemi izni.
    /// - Oyuncu kabul etmediyse ya da sistem izni yoksa hiçbir şey planlanmaz.
    /// - Arka plana geçişte plan kurulur, açılışta / öne gelişte hepsi iptal edilir (oyundayken bildirim gelmez).
    /// Editörde ve toplu çalıştırmada gerçek API yok: <see cref="ScheduledForTest"/> kaydedilir.
    /// </summary>
    public static class GameNotifications
    {
        private const string AskedKey = "katkat.notify.asked", EnabledKey = "katkat.notify.enabled";
        public const string AndroidChannel = "stoneburst_reminders";

        /// <summary>Oyuncuya oyun içi soru soruldu mu (bir kez).</summary>
        public static bool Asked
        {
            get => PlayerPrefs.GetInt(AskedKey, 0) == 1;
            set { PlayerPrefs.SetInt(AskedKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Oyuncu bildirimleri istedi (oyun içi tercih; sistem izni ayrıca gerekir).</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 0) == 1;
            set { PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Testler: son planlanan bildirimler (gerçek cihazda da doldurulur).</summary>
        public static readonly List<PlannedNotify> ScheduledForTest = new List<PlannedNotify>();
        public static int CancelCountForTest;

        private static bool _init;

        private static void Init()
        {
            if (_init) return;
            _init = true;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            try
            {
                var args = NotificationCenterArgs.Default;
                args.AndroidChannelId = AndroidChannel;
                args.AndroidChannelName = Loc.T("notify.channel");
                args.AndroidChannelDescription = Loc.T("notify.channel.desc");
                args.AndroidSmallIcon = "icon_0"; // NotificationSetup: beyaz siluet (Android durum çubuğu)
                args.PresentationOptions = NotificationPresentation.Alert | NotificationPresentation.Sound | NotificationPresentation.Badge;
                NotificationCenter.Initialize(args);
            }
            catch (Exception e) { Debug.LogWarning("[KatKat] Bildirim başlatılamadı: " + e.Message); }
#endif
        }

        /// <summary>Sistem iznini ister (Android 13+ / iOS). Sonuç: izin verildi mi.</summary>
        public static IEnumerator RequestPermission(Action<bool> done)
        {
            Init();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NotificationsPermissionRequest req = null;
            try { req = NotificationCenter.RequestPermission(); }
            catch (Exception e) { Debug.LogWarning("[KatKat] Bildirim izni istenemedi: " + e.Message); }
            if (req == null) { done?.Invoke(false); yield break; }
            yield return req;
            done?.Invoke(req.Status == NotificationsPermissionStatus.Granted);
#else
            yield return null;
            done?.Invoke(true);
#endif
        }

        /// <summary>Planlanmış ve gösterilmiş bildirimleri temizler (oyun öne geldi).</summary>
        public static void CancelAll()
        {
            CancelCountForTest++;
            ScheduledForTest.Clear();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!Enabled) return;
            Init();
            try
            {
                NotificationCenter.CancelAllScheduledNotifications();
                NotificationCenter.CancelAllDeliveredNotifications();
                NotificationCenter.ClearBadge();
            }
            catch (Exception e) { Debug.LogWarning("[KatKat] Bildirimler iptal edilemedi: " + e.Message); }
#endif
        }

        /// <summary>Planı kurar (arka plana geçiş). Oyuncu istemediyse hiçbir şey yapmaz.</summary>
        public static void Schedule(IList<PlannedNotify> plan)
        {
            ScheduledForTest.Clear();
            if (!Enabled || plan == null) return;
            Init();
            foreach (var p in plan)
            {
                ScheduledForTest.Add(p);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                try
                {
                    var n = new Notification
                    {
                        Identifier = (int)p.Kind,
                        Title = Title(p.Kind),
                        Text = Body(p.Kind),
                        Data = p.Kind.ToString(),
                    };
                    NotificationCenter.ScheduleNotification(n, new NotificationDateTimeSchedule(p.At));
                }
                catch (Exception e) { Debug.LogWarning("[KatKat] Bildirim planlanamadı: " + p + " " + e.Message); }
#endif
            }
        }

        public static string Title(NotifyKind k) => k switch
        {
            NotifyKind.HeartsFull => Loc.T("notify.hearts.title"),
            NotifyKind.Daily => Loc.T("notify.daily.title"),
            _ => Loc.T("notify.miss.title"),
        };

        public static string Body(NotifyKind k) => k switch
        {
            NotifyKind.HeartsFull => Loc.T("notify.hearts.body"),
            NotifyKind.Daily => Loc.T("notify.daily.body"),
            NotifyKind.Miss7 => Loc.T("notify.miss7.body"),
            _ => Loc.T("notify.miss.body"),
        };
    }
}
