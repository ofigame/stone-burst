using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using KatKat.Levels;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace KatKat.Game.Online
{
    /// <summary>
    /// Firebase'e SDK'sız, REST ile bağlantı (kullanıcı 2026-11-18: "REST ile yapalım, Firebase'in API'si var onu
    /// kullanalım"). Huawei AppGallery derlemesinde Google Play Hizmetleri yok; Firebase Unity SDK çalışmaz. Bu sınıf
    /// Identity Toolkit (anonim hesap) ve Firestore REST uçlarıyla Android/iOS'takiyle AYNI <c>scores/{uid}</c>
    /// koleksiyonuna yazar ve okur; kurallar (<c>firebase/firestore.rules</c>) değişmeden geçerli.
    /// Yapılandırma: <c>Resources/Online/firebase_rest.json</c> (apiKey, projectId); AppGallery derlemesi
    /// <c>google-services.json</c>'dan üretir (git'e girmez). Gövde kurma/çözme saf işlevlerdir (testli).
    /// </summary>
    public sealed class FirebaseRestConfig
    {
        public string ApiKey, ProjectId;
        public const string ResourcePath = "Online/firebase_rest";

        public bool Valid => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ProjectId);

        public static FirebaseRestConfig Load()
        {
            var ta = Resources.Load<TextAsset>(ResourcePath);
            return ta == null ? null : Parse(ta.text);
        }

        public static FirebaseRestConfig Parse(string json)
        {
            if (!(SafeParse(json) is Dictionary<string, object> d)) return null;
            var c = new FirebaseRestConfig { ApiKey = Str(d, "apiKey"), ProjectId = Str(d, "projectId") };
            return c.Valid ? c : null;
        }

        internal static object SafeParse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return MiniJson.Parse(json); } catch { return null; }
        }

        internal static string Str(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v as string : null;
    }

    /// <summary>Firestore REST gövdeleri ve yanıt çözümü (ağdan bağımsız; EditMode testleri bunu sınar).</summary>
    public static class FirestoreRestCodec
    {
        public static string DocPath(string projectId, string collection, string id) =>
            "projects/" + projectId + "/databases/(default)/documents/" + collection + "/" + id;

        /// <summary>scores/{uid} yazımı: alanlar + sunucu zamanı ("updated", kurallar request.time ister).</summary>
        public static string ScoreCommit(string projectId, string uid, string name, string country, LeaderboardEntry self)
        {
            var sb = new StringBuilder(512);
            sb.Append("{\"writes\":[{\"update\":{\"name\":").Append(Q(DocPath(projectId, "scores", uid))).Append(",\"fields\":{");
            sb.Append("\"name\":{\"stringValue\":").Append(Q(name)).Append("},");
            sb.Append("\"country\":{\"stringValue\":").Append(Q(country ?? "")).Append("},");
            Int(sb, "score", self.Score).Append(',');
            Int(sb, "zpe", self.Zpe).Append(',');
            Int(sb, "level", self.Level).Append(',');
            Int(sb, "stars", self.Stars);
            sb.Append("}},\"updateTransforms\":[{\"fieldPath\":\"updated\",\"setToServerValue\":\"REQUEST_TIME\"}]}]}");
            return sb.ToString();
        }

        /// <summary>İlk 10 (puana göre azalan); ülke kapsamında country eşitliği (SDK ile aynı dizin).</summary>
        public static string TopQuery(string country, int limit = 10)
        {
            var sb = new StringBuilder(400);
            sb.Append("{\"structuredQuery\":{\"from\":[{\"collectionId\":\"scores\"}],");
            if (!string.IsNullOrEmpty(country)) sb.Append("\"where\":").Append(Eq("country", country)).Append(',');
            sb.Append("\"orderBy\":[{\"field\":{\"fieldPath\":\"score\"},\"direction\":\"DESCENDING\"}],\"limit\":")
              .Append(limit.ToString(CultureInfo.InvariantCulture)).Append("}}");
            return sb.ToString();
        }

        /// <summary>Kendinden yüksek puan sayısı (sıra = sayı + 1).</summary>
        public static string HigherCount(string country, int score)
        {
            string gt = "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"score\"},\"op\":\"GREATER_THAN\",\"value\":{\"integerValue\":\""
                        + score.ToString(CultureInfo.InvariantCulture) + "\"}}}";
            string where = string.IsNullOrEmpty(country) ? gt
                : "{\"compositeFilter\":{\"op\":\"AND\",\"filters\":[" + Eq("country", country) + "," + gt + "]}}";
            return "{\"structuredAggregationQuery\":{\"structuredQuery\":{\"from\":[{\"collectionId\":\"scores\"}],\"where\":" + where
                   + "},\"aggregations\":[{\"alias\":\"n\",\"count\":{}}]}}";
        }

        /// <summary>runQuery yanıtı → sıralı kayıtlar (belge kimliği uid ile karşılaştırılır).</summary>
        public static List<LeaderboardEntry> ParseTop(string json, string uid)
        {
            var list = new List<LeaderboardEntry>();
            if (!(FirebaseRestConfig.SafeParse(json) is List<object> arr)) return list;
            foreach (var o in arr)
            {
                if (!(o is Dictionary<string, object> row) || !(row.TryGetValue("document", out var dv) && dv is Dictionary<string, object> doc)) continue;
                string name = FirebaseRestConfig.Str(doc, "name") ?? "";
                string id = name.Substring(name.LastIndexOf('/') + 1);
                doc.TryGetValue("fields", out var fv);
                var f = fv as Dictionary<string, object>;
                list.Add(new LeaderboardEntry
                {
                    Rank = list.Count + 1, IsSelf = id == uid, Name = StrField(f, "name") ?? "",
                    Score = IntField(f, "score"), Zpe = IntField(f, "zpe"), Level = IntField(f, "level"), Stars = IntField(f, "stars"),
                });
            }
            return list;
        }

        /// <summary>runAggregationQuery yanıtı → sayı (çözülemezse -1).</summary>
        public static int ParseCount(string json)
        {
            if (!(FirebaseRestConfig.SafeParse(json) is List<object> arr)) return -1;
            foreach (var o in arr)
                if (o is Dictionary<string, object> row && row.TryGetValue("result", out var rv) && rv is Dictionary<string, object> res
                    && res.TryGetValue("aggregateFields", out var av) && av is Dictionary<string, object> agg)
                    return IntValue(agg.TryGetValue("n", out var n) ? n : null);
            return -1;
        }

        public static string Q(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        private static string Eq(string field, string value) =>
            "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"" + field + "\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":" + Q(value) + "}}}";

        private static StringBuilder Int(StringBuilder sb, string k, int v) =>
            sb.Append('"').Append(k).Append("\":{\"integerValue\":\"").Append(v.ToString(CultureInfo.InvariantCulture)).Append("\"}");

        private static string StrField(Dictionary<string, object> f, string k) =>
            f != null && f.TryGetValue(k, out var v) && v is Dictionary<string, object> d ? FirebaseRestConfig.Str(d, "stringValue") : null;

        private static int IntField(Dictionary<string, object> f, string k) => IntValue(f != null && f.TryGetValue(k, out var v) ? v : null);

        private static int IntValue(object v)
        {
            if (!(v is Dictionary<string, object> d)) return 0;
            if (d.TryGetValue("integerValue", out var iv))
            {
                if (iv is string s && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return (int)l;
                if (iv is double dd) return (int)dd;
            }
            if (d.TryGetValue("doubleValue", out var dv) && dv is double x) return (int)x;
            return 0;
        }
    }

    /// <summary>
    /// Ağ katmanı: anonim hesap (Identity Toolkit), kimlik yenileme (Secure Token) ve Firestore istekleri.
    /// Android'de anahtar uygulama kısıtlıysa geçsin diye paket adı ve imza SHA-1 üstbilgileri eklenir.
    /// CİHAZDA (Huawei) DENENMEDİ.
    /// </summary>
    public sealed class FirebaseRestClient
    {
        private const string PrefRefresh = "katkat.rest.refresh", PrefUid = "katkat.rest.uid";
        private readonly FirebaseRestConfig _cfg;
        private string _idToken;
        private float _expiresAt;

        public FirebaseRestClient(FirebaseRestConfig cfg) { _cfg = cfg; }

        public string Uid { get; private set; }
        public string ProjectId => _cfg.ProjectId;
        public bool HasStoredAccount => !string.IsNullOrEmpty(PlayerPrefs.GetString(PrefRefresh, ""));
        private string Base => "https://firestore.googleapis.com/v1/projects/" + _cfg.ProjectId + "/databases/(default)/documents";

        /// <summary>Kayıtlı hesabın kimliğini yeniler (yoksa false).</summary>
        public void Resume(Action<bool> done)
        {
            string refresh = PlayerPrefs.GetString(PrefRefresh, "");
            if (string.IsNullOrEmpty(refresh)) { done?.Invoke(false); return; }
            var form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(refresh);
            Send("POST", "https://securetoken.googleapis.com/v1/token?key=" + _cfg.ApiKey, form, "application/x-www-form-urlencoded", null, (ok, code, body) =>
            {
                var d = FirebaseRestConfig.SafeParse(body) as Dictionary<string, object>;
                if (!ok || d == null) { if (code == 400) Forget(); done?.Invoke(false); return; } // 400: hesap silinmiş/geçersiz
                Store(FirebaseRestConfig.Str(d, "id_token"), FirebaseRestConfig.Str(d, "refresh_token"), FirebaseRestConfig.Str(d, "user_id"), FirebaseRestConfig.Str(d, "expires_in"));
                done?.Invoke(_idToken != null);
            });
        }

        /// <summary>Yeni anonim hesap.</summary>
        public void SignUp(Action<bool> done)
        {
            Send("POST", "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + _cfg.ApiKey, "{\"returnSecureToken\":true}", "application/json", null, (ok, code, body) =>
            {
                var d = FirebaseRestConfig.SafeParse(body) as Dictionary<string, object>;
                if (!ok || d == null) { AccountDiagnostics.LastSignInError = "HTTP " + code; done?.Invoke(false); return; }
                Store(FirebaseRestConfig.Str(d, "idToken"), FirebaseRestConfig.Str(d, "refreshToken"), FirebaseRestConfig.Str(d, "localId"), FirebaseRestConfig.Str(d, "expiresIn"));
                done?.Invoke(_idToken != null);
            });
        }

        /// <summary>Geçerli kimlik belirteci (süresi dolmuşsa yenilenir).</summary>
        public void Token(Action<string> done)
        {
            if (_idToken != null && Time.realtimeSinceStartup < _expiresAt - 60f) { done(_idToken); return; }
            Resume(ok => done(ok ? _idToken : null));
        }

        public void Firestore(string method, string pathOrVerb, string json, Action<bool, string> done)
        {
            Token(tok =>
            {
                if (tok == null) { done?.Invoke(false, null); return; }
                Send(method, Base + pathOrVerb, json, "application/json", tok, (ok, code, body) => done?.Invoke(ok, body));
            });
        }

        /// <summary>Firebase kullanıcısını siler ve cihazdaki hesabı unutur.</summary>
        public void DeleteUser(Action<bool> done)
        {
            Token(tok =>
            {
                if (tok == null) { done?.Invoke(false); return; }
                Send("POST", "https://identitytoolkit.googleapis.com/v1/accounts:delete?key=" + _cfg.ApiKey, "{\"idToken\":" + FirestoreRestCodec.Q(tok) + "}",
                    "application/json", null, (ok, code, body) => { if (ok) Forget(); done?.Invoke(ok); });
            });
        }

        public void Forget()
        {
            PlayerPrefs.DeleteKey(PrefRefresh);
            PlayerPrefs.DeleteKey(PrefUid);
            PlayerPrefs.Save();
            _idToken = null;
            Uid = null;
        }

        private void Store(string idToken, string refresh, string uid, string expiresIn)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(uid)) return;
            _idToken = idToken;
            Uid = uid;
            int sec = int.TryParse(expiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 3600;
            _expiresAt = Time.realtimeSinceStartup + sec;
            if (!string.IsNullOrEmpty(refresh)) PlayerPrefs.SetString(PrefRefresh, refresh);
            PlayerPrefs.SetString(PrefUid, uid);
            PlayerPrefs.Save();
        }

        private static void Send(string method, string url, string body, string contentType, string bearer, Action<bool, long, string> done)
        {
            var req = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 15 };
            if (body != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.SetRequestHeader("Content-Type", contentType);
            }
            if (bearer != null) req.SetRequestHeader("Authorization", "Bearer " + bearer);
            AndroidIdentity.Apply(req);
            req.SendWebRequest().completed += _ =>
            {
                bool ok = req.result == UnityWebRequest.Result.Success;
                string text = req.downloadHandler?.text;
                long code = req.responseCode;
                if (!ok) Debug.LogWarning("[KatKat] Firebase REST " + method + " " + url.Split('?')[0] + " → " + code + " " + req.error);
                req.Dispose();
                done?.Invoke(ok, code, text);
            };
        }
    }

    /// <summary>Android API anahtarı kısıtlaması için paket adı + imza SHA-1 (X-Android-Package / X-Android-Cert).</summary>
    internal static class AndroidIdentity
    {
        private static bool _read;
        private static string _pkg, _sha1;

        public static void Apply(UnityWebRequest req)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_read) { _read = true; Read(); }
            if (!string.IsNullOrEmpty(_pkg)) req.SetRequestHeader("X-Android-Package", _pkg);
            if (!string.IsNullOrEmpty(_sha1)) req.SetRequestHeader("X-Android-Cert", _sha1);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void Read()
        {
            try
            {
                using var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var act = up.GetStatic<AndroidJavaObject>("currentActivity");
                _pkg = act.Call<string>("getPackageName");
                using var pm = act.Call<AndroidJavaObject>("getPackageManager");
                using var info = pm.Call<AndroidJavaObject>("getPackageInfo", _pkg, 0x40); // GET_SIGNATURES
                var sigs = info.Get<AndroidJavaObject[]>("signatures");
                if (sigs == null || sigs.Length == 0) return;
                sbyte[] raw = sigs[0].Call<sbyte[]>("toByteArray");
                var bytes = new byte[raw.Length];
                Buffer.BlockCopy(raw, 0, bytes, 0, raw.Length);
                using var sha = System.Security.Cryptography.SHA1.Create();
                var h = sha.ComputeHash(bytes);
                var sb = new StringBuilder(40);
                foreach (var b in h) sb.Append(b.ToString("X2"));
                _sha1 = sb.ToString();
            }
            catch (Exception e) { Debug.LogWarning("[KatKat] imza okunamadı: " + e.Message); }
        }
#endif
    }

    /// <summary>
    /// AppGallery hesabı: anonim Firebase hesabı (REST). Ad oyuncuya bir kez verilen "Oyuncu 4821" biçiminde
    /// (cihazda saklanır; dil değişse de aynı kalır). Bulut kaydı yok (anonim hesap cihaza bağlı; Ayarlar'da
    /// "İlerlemeyi kaydet" satırı gizli). Hesap silme: sıralama kaydı + Firebase kullanıcısı.
    /// </summary>
    public sealed class RestAccountService : IAccountService, IRenamableAccount
    {
        private const string PrefName = "katkat.rest.name", PrefChosen = "katkat.rest.name.chosen";
        private readonly FirebaseRestClient _client;

        public RestAccountService(FirebaseRestClient client) { _client = client; }

        public FirebaseRestClient Client => _client;
        public AccountState State { get; private set; } = AccountState.SignedOut;
        public string PlayerName { get; private set; }
        public string PlayerId => State == AccountState.SignedIn ? _client.Uid : null;
        public bool IsTest => false;

        public void SignIn(bool silent, Action<bool> done)
        {
            if (State == AccountState.SignedIn) { done?.Invoke(true); return; }
            if (State == AccountState.SigningIn) { done?.Invoke(false); return; }
            if (silent && !_client.HasStoredAccount) { done?.Invoke(false); return; }
            State = AccountState.SigningIn;
            Action<bool> fin = ok =>
            {
                State = ok ? AccountState.SignedIn : AccountState.SignedOut;
                if (ok) PlayerName = NameFor(_client.Uid);
                done?.Invoke(ok);
            };
            if (_client.HasStoredAccount)
                _client.Resume(ok => { if (ok || silent) fin(ok); else _client.SignUp(fin); });
            else _client.SignUp(fin);
        }

        /// <summary>Kalıcı görünen ad: ilk girişte "Oyuncu NNNN" (oyuncunun o anki dilinde), sonra hep aynı.</summary>
        private static string NameFor(string uid)
        {
            string n = PlayerPrefs.GetString(PrefName, "");
            if (!string.IsNullOrEmpty(n)) return n;
            n = GuestName(uid, Loc.T("acct.guest"));
            PlayerPrefs.SetString(PrefName, n);
            PlayerPrefs.Save();
            return n;
        }

        /// <summary>uid'den kararlı 4 haneli numara (1000–9999); <paramref name="pattern"/> "Oyuncu {0}".</summary>
        public static string GuestName(string uid, string pattern)
        {
            uint h = 2166136261;
            foreach (char c in uid ?? "") { h ^= c; h *= 16777619; }
            int num = (int)(h % 9000) + 1000;
            if (string.IsNullOrEmpty(pattern) || !pattern.Contains("{0}")) pattern = "Player {0}";
            return string.Format(CultureInfo.InvariantCulture, pattern, num);
        }

        /// <summary>2026-11-19 (kullanıcı: "ismini kendi versin"): oyuncunun yazdığı ad cihazda saklanır; puan bir sonraki
        /// yazımda bu adla gider.</summary>
        public bool HasChosenName => PlayerPrefs.GetInt(PrefChosen, 0) == 1;

        public void Rename(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            PlayerName = name;
            PlayerPrefs.SetString(PrefName, name);
            PlayerPrefs.SetInt(PrefChosen, 1);
            PlayerPrefs.Save();
        }

        public void SaveProgress(string json, Action<bool> done) => done?.Invoke(false);
        public void LoadProgress(Action<bool, string> done) => done?.Invoke(true, null);

        public void DeleteAccount(Action<bool> done)
        {
            if (!_client.HasStoredAccount) { State = AccountState.SignedOut; done?.Invoke(true); return; }
            _client.Firestore("DELETE", "/scores/" + (_client.Uid ?? PlayerPrefs.GetString("katkat.rest.uid", "")), null, (ok, _) =>
            {
                if (!ok) { done?.Invoke(false); return; }
                _client.DeleteUser(del =>
                {
                    if (!del) { done?.Invoke(false); return; }
                    PlayerPrefs.DeleteKey(PrefName);
                    PlayerPrefs.DeleteKey(PrefChosen);
                    PlayerPrefs.Save();
                    PlayerName = null;
                    State = AccountState.SignedOut;
                    done?.Invoke(true);
                });
            });
        }
    }

    /// <summary>AppGallery sıralaması: Firestore REST ile aynı <c>scores</c> koleksiyonu (Android/iOS ile ortak liste).</summary>
    public sealed class RestLeaderboard : ILeaderboardProvider, ILeaderboardWriter
    {
        private readonly RestAccountService _account;

        public RestLeaderboard(RestAccountService account) { _account = account; }

        private FirebaseRestClient C => _account.Client;

        public void Submit(LeaderboardEntry self, Action<bool> done)
        {
            if (_account.State != AccountState.SignedIn || self == null || self.Score <= 0) { done?.Invoke(false); return; }
            string body = FirestoreRestCodec.ScoreCommit(C.ProjectId, C.Uid, Trim(_account.PlayerName), GameController.DeviceCountry() ?? "", self);
            C.Firestore("POST", ":commit", body, (ok, _) => done?.Invoke(ok));
        }

        public void Fetch(LeaderboardScope scope, string country, LeaderboardEntry self, Action<LeaderboardResult> done)
        {
            var r = new LeaderboardResult { Scope = scope, Country = country, Self = self };
            if (_account.State != AccountState.SignedIn) { if (self != null) self.Rank = 0; r.Status = LeaderboardStatus.NotConnected; done?.Invoke(r); return; }
            if (scope == LeaderboardScope.Country && string.IsNullOrEmpty(country)) { r.Status = LeaderboardStatus.NoCountry; done?.Invoke(r); return; }
            string c = scope == LeaderboardScope.Country ? country : null;
            if (self != null && self.Score > 0) Submit(self, _ => Query(c, self, r, done));
            else Query(c, self, r, done);
        }

        private void Query(string country, LeaderboardEntry self, LeaderboardResult r, Action<LeaderboardResult> done)
        {
            C.Firestore("POST", ":runQuery", FirestoreRestCodec.TopQuery(country), (ok, body) =>
            {
                if (!ok) { r.Status = LeaderboardStatus.Error; r.Error = "REST"; done?.Invoke(r); return; }
                foreach (var e in FirestoreRestCodec.ParseTop(body, C.Uid))
                {
                    r.Top.Add(e);
                    if (e.IsSelf && self != null) self.Rank = e.Rank;
                }
                r.Status = r.Top.Count == 0 ? LeaderboardStatus.Empty : LeaderboardStatus.Ready;
                if (self == null || self.Score <= 0 || self.Rank > 0) { done?.Invoke(r); return; }
                C.Firestore("POST", ":runAggregationQuery", FirestoreRestCodec.HigherCount(country, self.Score), (ok2, body2) =>
                {
                    int n = ok2 ? FirestoreRestCodec.ParseCount(body2) : -1;
                    if (n >= 0) self.Rank = n + 1;
                    done?.Invoke(r);
                });
            });
        }

        private static string Trim(string s) => string.IsNullOrEmpty(s) ? "?" : s.Length > 40 ? s.Substring(0, 40) : s;
    }

#if KATKAT_APPGALLERY && UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>AppGallery derlemesinde REST hesabını ve sıralamayı bağlar (yapılandırma yoksa hesap kapalı kalır).</summary>
    internal static class RestSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var cfg = FirebaseRestConfig.Load();
            if (cfg == null) { Debug.LogWarning("[KatKat] Firebase REST yapılandırması yok: AppGallery'de hesap kapalı"); return; }
            var account = new RestAccountService(new FirebaseRestClient(cfg));
            GameController.AccountProvider = account;
            GameController.LeaderboardProvider = new RestLeaderboard(account);
        }
    }
#endif
}
