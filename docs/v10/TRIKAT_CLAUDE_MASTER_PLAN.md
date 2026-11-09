# TRIKAT — CLAUDE CODE ANA GELİŞTİRME PLANI
**Sürüm:** 2.0 — Birleştirilmiş çalışma belgesi  
**Oyun markası:** TriKat  
**Android paket adı / iOS Bundle ID hedefi:** `com.ofigame.trikat`  
**Platformlar:** Google Play, Apple App Store, Huawei AppGallery  
**İlk sürüm:** Mevcut, geliştirilmiş ve kullanıcı tarafından test edilmiş 1.000 bölüm  
**Uzun vadeli hedef:** 10.000 bölüme uygun mimari  
**Hedef uygulayıcı:** Claude Code

> **Claude Code için kritik talimat:** Bu belge mevcut Unity projesi üzerine uygulanacak ürün gereksinimlerini içerir. İlk görev kod yazmak değil, projeyi salt okunur biçimde incelemektir. Bu belgede belirtilen öneriler mevcut projede uygulanmış kabul edilmemelidir. Kullanıcı onayı olmadan çalışan mekanikleri, mevcut 1.000 bölüm verisini veya kayıt formatını değiştirme.

## A. Öncelikli ürün kararları — kesinleşmiş
1. Koleksiyon taşları kalıcı kazanılır ve özel güçleri her gün yeniden kullanılabilir.
2. Gizli hazine taşları hem koleksiyona katılır hem günlük bulmacada güç sağlar.
3. Günlük bulmacaya ödüllü reklam izleyerek girilir; ilk girişten sonra **bir ücretsiz yeniden deneme** vardır.
4. Reklamsız paket sahipleri günlük bulmacaya reklam izlemeden girer.
5. Her bölüm için yalnızca **en yüksek yıldız sonucu** toplam yıldız hesabına katılır; tekrar oynayarak yıldız biriktirilemez.
6. **ZPE** oyuncu profilinde ve sıralamada görünür; gerçek IQ iddiası değildir.
7. Oyuncu misafir başlar; sıralamaya katılmak için hesap bağlar.
8. Temel ürün sistemleri ilk 1.000 bölümlük sürümün kapsamındadır. Facebook entegrasyonu sonraya bırakılır.

## B. Mevcut projenin korunması
- **1.000 bölüm zaten mevcut ve test edildi.** Yeniden üretme, rastgele dengeleme, numaralandırma değiştirme veya toplu düzenleme yapma.
- İlk önce mevcut sahne, prefab, ScriptableObject, bölüm veri yapısı, hamle/taş mekaniği, kayıt ve test kodlarını incele.
- Oyuncunun yaptığı hamlelerin tam biçimini veya oyun kurallarını bu dokümandan varsayma; gerçek koddan çıkar.
- Bölümlerin kullanıcı tarafından test edilmiş olması, otomatik matematiksel çözülebilirlik kanıtı olduğu anlamına gelmez. Var olanı koruyup güvenli regresyon testleri ekle.
- 10.000 bölüm hedefi, ilk sürümde 10.000 yeni bölüm oluşturma talimatı değildir.
- Yeni paket adı `com.ofigame.trikat` hedefidir; mevcut paket adı ve mağaza yayını durumunu denetlemeden değiştirme. Yayımlanmış uygulamanın kimliğini sessizce değiştirme.
- Ürün adı **TriKat**; önceki dosya, sınıf, asset ve save anahtarlarında `KatKat` geçebilir. Sırf marka değişikliği için teknik isimleri otomatik yeniden adlandırma; serileştirme ve kayıt uyumluluğunu koru.

## C. Ürün kapsamı ve öneri / kesin karar ayrımı
**Kesin kapsam:** Bölüm ilerlemesi, yıldızlar, koleksiyon/gizli hazine, günlük bulmaca ve kalıcı yetenekler, günlük başarı ödülü, ZPE, hesap ve birleşik sıralama, reklam ve reklamsız satın alma, analitik, bildirimler, gizlilik ve veri güvenliği.

**Öneri niteliğinde, doğrulanması gereken içerik hedefleri:**
- 20 dünya × 500 bölüm = uzun vadede 10.000 bölüm.
- İlk sürümde iki tema: Akdeniz Sahili (1–500) ve Çiçek Bahçesi (501–1.000).
- Uzun vadede 100 yıldız taşı + 100 gizli hazine + 20 efsanevi taş = 220 koleksiyon öğesi.
- İlk sürümde 10 yıldız taşı + 10 gizli hazine + 2 efsanevi taş = 22 öğe.
- İlk 1.000 bölümde azami 3.000 yıldız; 10.000 bölümde teorik azami 30.000 yıldız.
- 250 yıldızlık eşiklerle açılan yıldız taşları, **taslak öneridir**; gerçek mevcut sistemle karşılaştırılmalıdır.

**Günlük mücadele:** Başarılı günlük mücadele sonrasında, o gün oynanacak kalan normal bölümlere başlangıçta **+1 hamle** verilmesi hedeflenir; sonraki gün sıfırlanır. Bu bonusun ZPE'yi haksız yükseltmemesi gerekir. Günlük güçlerin mücadele başına bir kez kullanılması ve tek taş seçimi **denge önerisidir**, kesin karar olarak uygulanmamalıdır.

## D. Claude Code çalışma yöntemi
1. **İlk tur salt okunur denetim:** Kod, sahneler, asset'ler, 1.000 bölüm, kayıt sistemi, SDK'lar, platform ayarları, testler.
2. `docs/trikat/AUDIT.md` üret: Kanıt olarak gerçek dosya/sınıf adları ve bulunan durum.
3. `docs/trikat/GAP_ANALYSIS.md` üret: Her özellik için mevcut / kısmi / eksik / doğrulanamadı; bağımlılık ve risk.
4. `docs/trikat/IMPLEMENTATION_BACKLOG.md` üret: P0–P7 bağımlılık sıralı, küçük, test edilebilir görevler ve kabul kriterleri.
5. `docs/trikat/OPEN_DECISIONS.md` üret: Yalnızca gerçekten açık kararları listele; kesinleşmiş 8 kararı yeniden tartışma.
6. **İlk turda kod değiştirme.** Analiz ve önerilen ilk küçük görev için onay bekle.
7. Sonraki her görevde: Değişen dosyalar, yapılan testler, test edilemeyenler, mevcut bölüm/kayıt uyumluluğu ve bir sonraki adımı raporla.
8. Haftalık tahminlerle planı uzatma; bağımlılık, test ve tamamlanma kriterleriyle ilerle.

---

## E. Ayrıntılı teknik gereksinimler ve uygulama backlog'u

Aşağıdaki bölüm, önceki kapsamlı teknik master planın bütün ayrıntılarını korur. Üstteki **TriKat markası, mevcut 1.000 bölümü koruma, kesin karar / öneri ayrımı ve ilk turda salt okunur denetim** kuralları, aşağıdaki eski ifadelerle çelişirse önceliklidir.

# TRIKAT — CLAUDE CODE MASTER DEVELOPMENT PLAN
**Version:** 1.1  
**Target:** Unity mobile puzzle game — iOS App Store, Google Play, Huawei AppGallery  
**Launch content:** 1,000 existing, tested levels  
**Long-term architecture:** 10,000 levels  
**Implementation agent:** Claude Code

> This is an implementation brief. Claude Code must inspect the actual repository before selecting concrete Unity packages, APIs, persistence mechanisms or refactors. Do not assume a blank project.

## 0. NON-NEGOTIABLE BASELINE

- **1,000 levels already exist and have been tested. Do not regenerate, replace, rebalance, or bulk-edit them without explicit approval.** Preserve existing level IDs, layouts, difficulty, completion logic, and save compatibility.
- Existing Unity gameplay is the source of truth. Inspect scenes, prefabs, scripts, ScriptableObjects, serialized assets, packages, build settings and tests before changing code.
- No unnecessary rewrite. Add modular services/adapters around current gameplay and migrate only where technically necessary.
- Protect existing player progress; introduce schema versions, migration tests and backup strategy.
- Do not claim that existing levels are mathematically solvable based solely on prior user testing; preserve existing test results and add automated regression coverage where feasible.
- No fictional integrations or fake production success. All platform SDKs, purchases, identity, ad events and backend calls require real verification.
- Work in small, reviewable commits/patches. After each stage report changed files, decisions, tests run, failures and next action.
- Do not silently make business decisions listed as OPEN DECISIONS. Implement them as configurable policies or ask the developer.
- **Time planning:** no week-based estimates. Work by dependency and acceptance criteria. Claude Code can accelerate coding, but device, store and real-user testing still take actual time.

## 1. PRODUCT SCOPE

**Game:** TriKat — casual puzzle with attention, logic, strategic moves, satisfying outcomes, premium soft 3D toy aesthetic.

**Launch:** 1,000 tested levels, 2 themed worlds of 500 levels (Akdeniz Sahili / Çiçek Bahçesi) *as a planned presentation grouping; verify existing level/world organization before migration*. No Facebook integration at launch.

**Long-term:** 10,000 levels, 20 worlds x 500 levels; data-driven loading, paginated map UI, content bundles/remote configuration if justified; never instantiate all 10,000 level views simultaneously.

**Core systems at launch:** stars/progression; collectible stones and hidden treasures; daily challenge and permanent stone powers; dynamic game-specific performance index (ZPE); optional account linking and shared global leaderboards; rewarded/interstitial ads and no-ads IAP; Firebase Analytics, Firebase Cloud Messaging (push notifications), engagement/active-user measurement; privacy/account deletion.

**Long-term collection plan:** 100 star stones + 100 hidden treasures + 20 legendary stones = 220 items. **First 1,000 levels:** planned 10 star stones, 10 hidden treasures, 2 legendary stones (22). These are target content counts, not evidence that assets already exist.

## 2. FIRST ACTION: REPOSITORY AUDIT (NO CODE CHANGES)

Inspect and report:
1. Unity editor version, render pipeline, input system, target platforms, dependencies, existing SDKs and project compile state.
2. Main scenes, UI navigation, map, game board, win/lose flow, move logic, boosters, star rules, daily reset or other existing features.
3. How the 1,000 levels are stored and indexed; level format; any tests, generators or solvers; whether 500-level world grouping exists.
4. Save model, local/cloud persistence, migrations and whether users already have live progress.
5. Existing ads, IAP, auth, analytics, Firebase, push notifications, backend and leaderboard implementations, if any.
6. Performance risks on low-end Android/Huawei/iOS; build automation; CI/test feasibility.
7. **Gap matrix:** already implemented / partially implemented / missing / needs verification for every feature in this document.

Deliver `docs/trikat/AUDIT.md` and `docs/trikat/IMPLEMENTATION_BACKLOG.md` with P0/P1/P2 priorities, exact code entry points and risks. Only after this proceed to implementation.

## 3. IMPLEMENTATION ARCHITECTURE

Create logical boundaries compatible with existing project, without forcing separate assemblies if harmful:
- `Game.Core`: existing board state, rules, move resolution, win/lose outcomes; preserve contracts.
- `Levels`: existing level catalog, world mapping, IDs, loading and future 10k extensibility.
- `Progression`: completed level, per-level best stars, aggregate stars, unlocks.
- `Collections`: catalog, owned stones, acquisition sources, rarities, equip/selection, skill definitions.
- `DailyChallenge`: server-date challenge, admission, attempts, power selection/use, reward entitlement.
- `Performance`: ZPE inputs, scoring version, history, public visibility, leaderboard eligibility.
- `Identity`: guest identity, platform account linking, cross-device restore, conflict handling.
- `Leaderboard`: global stars, optional ZPE, rank neighborhood, pagination, anti-cheat.
- `Monetization`: ads, purchase verification, no-ads entitlement, retry/idempotency.
- `Analytics`: consent-aware event tracking, crash/performance telemetry.

Use interfaces/adapters for providers and test doubles. Distinguish local offline game state from authoritative online competitive state. Use secure server-side verification where feasible; do not imply client-side checks alone stop cheating.

## 4. LEVELS, STARS AND PROGRESSION

- Keep existing 1,000 level data and completion behavior intact.
- Prepare stable identifiers for up to 10,000 levels; separate content data from world/map presentation.
- World plan: 20 worlds x 500 levels; first two world themes Akdeniz Sahili and Çiçek Bahçesi, **only if compatible with existing assets**.
- Best star score per level is 1–3; replaying a level can improve its record but must **not** add stars repeatedly. Aggregate stars = sum of each completed level's highest recorded star value.
- Maximum stars in first 1,000 levels: 3,000; in 10,000 levels: 30,000.
- Planned star-stone unlock thresholds: 250, 500, ... 25,000 stars (100 stones). First 1,000 levels can unlock 10 star stones up to 2,500 stars.
- Make reward grants idempotent. Test replay 1-star -> 3-star -> 2-star, cross-device sync, offline reconnect and migration from existing save format.
- Avoid changing existing level scoring to match new assumptions without approval.

## 5. COLLECTION SYSTEM

**Categories:** `StarStone` (100), `HiddenTreasure` (100), `LegendaryStone` (20).

**Launch content:** 10 + 10 + 2 = 22 items; verify visual assets, animations and acquisition triggers actually available.

**Star stones:** awarded at cumulative best-star thresholds. Permanent ownership.

**Hidden treasures:** approximately 1 per 100 levels over 10k (10 in first 1k), distributed irregularly. Special rock/tree/other object breaks on a defined performance condition; reward persists; level may be replayed to obtain missed treasure. Do not inject objects into existing tested levels until level compatibility and solvability are confirmed. Define a safe overlay/level modifier approach and per-level QA.

**Legendary stones:** one per 500-level world, earned from world completion and/or explicit mastery conditions; final rule is configurable and requires approval.

**Abilities:** stone powers are permanent once unlocked and usable **only in Daily Challenge**. Initial rule: choose one owned stone per challenge attempt and use its ability once during that attempt. Implement data-driven ability definitions, not 220 bespoke scripts. Initial proposed families: fire (area), ocean (row), storm (column), moon (moves), life (rescue), galaxy (color), crystal (obstacle damage), wind (reshuffle), transformation (recolor), star (combo boost), plus extensible variants. Abilities must respect actual board rules; each must have deterministic tests.

**UI:** catalog grid, category filters, locked/unlocked state, acquisition method, preview, rarity, ability description, equip/selection; use performant 2D icons in grid, 3D models for high-value reveal where available. Never imply assets exist without inspecting.

## 6. DAILY CHALLENGE

**Goal:** notably harder than normal gameplay, informally '10x difficulty' feeling; NOT literally 10x moves/tiles. Use calibrated difficulty metrics and playtests. The daily puzzle must be solvable under permitted rules.

**Entry:** rewarded ad for users without no-ads entitlement; no-ads purchasers enter without watching. Ad-unavailable fallback and policy compliance must be defined before launch. Successful reward callback opens one daily entry granting up to **two total attempts** (first + one free retry). No new ad for the free retry. After two failures challenge closes until next day; reward can be earned once per day.

**Stone:** choose one owned stone per attempt, activate at most once in that attempt; permanent stone ownership unaffected. Confirm whether stone may be changed between first attempt and retry; default configurable.

**Win reward:** +1 starting move on **every remaining normal level played that day**, not on daily challenge. This is an entitlement, not a one-time consumable. Expire at the agreed server-based daily reset; avoid device-clock exploits. Normal levels with this bonus must not unfairly inflate competitive ZPE.

**Daily streak:** optional seven-day achievement badge; do not add punitive streak losses without explicit product decision.

**Backend:** idempotent admission, attempt submission and daily reward grant; prevent duplicated ads/reward claims, replayed requests and concurrent-device exploits. Define offline behavior explicitly; daily challenge may require network access.

**Telemetry:** daily viewed, ad requested/completed/failed, entry, attempt, stone chosen/used, win/loss, reward granted, retries and retention.

## 7. ZPE — ZEKÂ PERFORMANS ENDEKSİ

**Mandatory ethical/product rule:** ZPE is a TriKat-specific game performance index, **not a real IQ measurement**, diagnosis or scientifically validated intelligence test. Never label as measured IQ. Explain it in onboarding/help/profile.

**Visibility:** fixed home-screen display, end-of-level change, profile and opt-in leaderboard/public profile settings as required by privacy law and store policy. User requested public ZPE; provide clear disclosure and privacy controls, especially for minors.

**Initial component weights (CONFIGURABLE; require calibration):** move efficiency 35%, difficulty-adjusted success 25%, first-attempt success 20%, solve time 10%, no-assist completion 10%.

**Calculation design:**
- Use existing level difficulty and verified reference move count if available; do not invent an optimal move count.
- Calibrate on real play data; consider distributions, skill drift, ceiling/floor effects and time-based accessibility.
- First 10–20 levels display `Kalibrasyon yapılıyor`; then compute stable score. Historical performance included with greater weight on recent 30–50 levels, but single-level volatility constrained.
- Store raw inputs, algorithm version, resulting score, timestamp, assistance flags; support recalculation/migrations.
- Extra moves from ads, no-ads daily bonus, boosters and special powers must not create a ranking advantage. Decide whether to exclude assisted rounds from competitive ZPE or use a separately normalized metric.
- Avoid misleading IQ-like norm labels and arbitrary claims about intelligence.
- Build unit tests for easy/hard level comparisons, replay, speed, assisted play, missing reference values, migrations and stability.

## 8. PLAYER IDENTITY AND CROSS-PLATFORM LEADERBOARDS

**Launch distribution:** Apple App Store, Google Play, Huawei AppGallery. **Facebook deferred**.

**Onboarding:** guest can play immediately. Verified platform account required to enter global leaderboards and restore progress across devices. No mandatory signup wall at launch.

**Identity options:** Apple Game Center, Google Play Games Services, Huawei Account/Game Service. Verify actual supported auth integration in chosen backend/Unity SDK; Huawei may need a custom verified integration. Game Center alone is not the unified global identity database.

**Shared backend identity:** stable TriKat player ID, linked provider IDs, account merge rules, data deletion, account recovery, guest-to-linked migration and device conflicts. Do not rely on mutable display names as identifiers.

**Leaderboards:** initial global total best stars; show player's own rank and nearby players even if outside top 100. Optional collection and ZPE rankings can be added when verified and calibrated. Weekly leagues are post-launch unless already implemented.

**Integrity:** authoritative backend validation, event logs, rate limits, replay protection, suspicious-score review. Absolute cheat prevention cannot be guaranteed for fully offline levels; document trust model and realistic mitigation.

**Privacy:** nickname rather than legal name; visibility controls, privacy notice, consent and account deletion/export processes where applicable. Region and age handling should be reviewed before release.

## 9. MONETIZATION

**No banner ads** on home, map or gameplay.

**Interstitial starting experiment:** none in first five levels; thereafter after roughly 3–4 completed levels and at least 180 seconds between interstitials; never interrupt active gameplay or immediately follow a rewarded ad. Use remote-configurable frequency caps, session caps and analytics. These are test settings, not immutable rules.

**Rewarded ads:** optional extra moves/boosters/daily entry; show clear value and grant only on verified completion callback; handle ad failure and duplicate callbacks.

**No-ads one-time IAP:** initial pricing hypothesis **99.99 TRY**, subject to store pricing tiers, taxes, localization and A/B testing. Removes forced interstitials and grants daily challenge admission without ad. Explicitly define behavior of optional rewarded extras; do not silently promise all ads removed if rewarded extras remain.

**Cross-store entitlements:** verify receipts/server-side where supported and specify whether one purchase is portable between stores; **do not assume Apple/Google/Huawei purchases transfer automatically**. Restore purchases and refunds/revocations must be handled.

**Commercial fairness:** purchasing or watching ads must not directly buy stars, collection achievements or competitive leaderboard points.

## 10. ANALYTICS AND LIVE OPERATIONS

Minimum events with anonymized player ID and schema version: `session_start`, `level_start`, `level_complete`, `level_fail`, `stars_improved`, `treasure_revealed`, `collection_unlocked`, `daily_entry`, `daily_attempt`, `daily_win`, `zpe_updated`, `ad_impression`, `rewarded_ad_complete`, `iap_purchase`, `account_link`, `leaderboard_view`.

Core dashboards: D1/D7/D30 retention, progression funnel, per-level fail/retry, ZPE distributions, daily challenge engagement, collection unlock rates, ad impressions per DAU, rewarded completion, IAP conversion, crashes, frame-time and memory.

Ensure privacy/consent and SDK compliance for each store and region.

## 10A. FIREBASE — NOTIFICATIONS, ACTIVE USER TRACKING AND ENGAGEMENT

**Launch requirement:** Add Firebase to the existing Unity project for push notifications and engagement analytics. Do not replace the selected player identity/backend/leaderboard architecture with Firebase Auth or Firestore without a separate decision. Firebase Analytics and FCM are complementary services.

### Integration and store compatibility
- Audit Unity version, existing Firebase packages, Android/iOS build pipeline, consent SDKs and dependency conflicts before installation. Use compatible official Firebase Unity SDK packages and document exact versions.
- Create/configure Firebase projects/apps for each actual bundle/package ID; keep separate development/staging/production environments where practical. Never commit service-account private keys, APNs private keys or other secrets. Platform configuration files may contain non-secret identifiers but must be managed per environment.
- **Google Play Android:** Verify Firebase Analytics and Firebase Cloud Messaging end-to-end on actual Google Play Services-capable devices.
- **Apple iOS:** Configure APNs entitlement, push capability, APNs authentication key/certificate in Firebase, permissions and token lifecycle; test foreground/background/terminated behavior on physical iPhones. Do not assume Windows-only tooling can complete iOS signing.
- **Huawei AppGallery:** Do **not** assume Google-dependent FCM delivery works on Huawei devices without Google Mobile Services. Perform a compatibility spike on an HMS-only Huawei device. Prefer an abstract `INotificationProvider` with FCM on supported devices and **Huawei Push Kit** for HMS-only devices if needed. Verify whether Firebase Analytics SDK functions in the target Huawei distribution; if not, use a compatible alternative or server-side events with unified reporting. Never silently omit Huawei users from activity metrics.
- Add separate notification-provider configuration per store/flavor, feature flags and graceful fallback if SDK initialization fails.

### Analytics: user activity, retention and behavior
- Integrate **Firebase Analytics** where compatible. Define an event catalog with consistent snake_case names, documented parameters, schema version and no direct personal identifiers. Reuse the event list in section 10; add `app_open`, `session_start` (custom only if not duplicating automatic measurement), `session_end` only when reliably observable, `daily_challenge_view`, `notification_permission_result`, `notification_open`, `account_link_success`, `level_retry`, `stone_ability_used`.
- Use Firebase's defined **DAU/WAU/MAU, retention and engagement** reports for aggregate user activity. Clarify that analytics active-user metrics are not an exact real-time online-player presence indicator. If the product later needs **currently online / last-seen**, define a separate opt-in heartbeat/presence service with expiration and backend-side timestamps; do not claim Firebase Analytics provides exact online status.
- Track level funnel (start/complete/fail), first 1/10/50/100/500/1000 level milestones, session duration where measurable, daily challenge participation, collection engagement, ad impressions/rewards and IAP conversions. Avoid excessive event cardinality and personal data in parameters.
- Define an internal analytics interface (e.g. `IGameAnalytics`) so gameplay never calls Firebase directly. Handle offline event buffering and avoid duplicate events on retries; distinguish analytics events from authoritative backend progression.
- Instrument cohort dashboards for D1/D7/D30 retention, DAU/WAU/MAU, average engagement time, sessions per user, level drop-off, challenge conversion, notification opt-in and notification-driven return rates. Firebase dashboard definitions and delays must be documented; don't present estimates as real-time exact counts.

### Push notification rules
- Use **Firebase Cloud Messaging** for supported platforms. Ask OS notification permission at a contextual moment, not automatically on first frame; provide an in-game notification preference toggle and respect OS settings. Do not require push permission to play or enter leaderboards.
- Notification use cases: optional daily challenge reminder, optional new collection/unlock reminder, and important content/event updates. Prefer player-configurable reminder time and frequency caps; no repeated spam, no manipulative urgency, no unsolicited sensitive data in notification text.
- Device push token is not player identity. Register/refresh/revoke tokens against the linked guest/player ID securely, supporting multiple devices, logout, account deletion and token rotation. Never log raw tokens in analytics.
- Implement deep links that safely open the daily challenge/collection/map after launch; handle foreground, background and cold-start notification taps, duplicate delivery, time zones, quiet hours and local-day reset boundaries.
- Sending notifications requires an authorized backend/scheduler (or securely administered campaign tools). **Never embed FCM server credentials in the Unity client.** If the notification targets an individual player, resolve their device tokens server-side and enforce consent/preferences.
- Distinguish **local notifications** (device-scheduled) from **remote push** (FCM/Huawei Push Kit). Choose per use case; do not promise exact delivery times or delivery when device/OS suppresses notifications.

### Privacy, consent and release validation
- Document KVKK/GDPR and store disclosures as applicable, consent/opt-out requirements, child-directed audience handling, data retention, deletion, and Apple's ATT applicability based on actual tracking behavior. Do not assume all Firebase use requires ATT, or that ATT alone satisfies privacy obligations.
- Provide a data inventory for Firebase Analytics identifiers, push tokens, events, linked player IDs and retention settings; avoid recording actual IQ, sensitive health data, names, emails or precise locations in analytics.
- Add Firebase initialization failure, offline mode, denied permission, token rotation, app reinstall, Android notification permission, APNs/HMS differences, foreground/background/deep-link and privacy opt-out tests. Test on actual iOS, GMS Android and HMS-only Huawei hardware.

**Acceptance:** compatible platforms send documented analytics events; DAU/retention dashboards are verifiable with test users (allowing reporting delay); opted-in devices receive and open appropriate reminders; opted-out users receive none; Huawei behavior is explicitly tested and documented; existing 1,000 levels and saves remain unchanged.

## 11. ORDERED IMPLEMENTATION BACKLOG — NO WEEK ESTIMATES

### P0 — Audit and baseline
- [ ] Inspect repo and capture existing tests/working builds.
- [ ] Produce feature gap matrix and migration risk register.
- [ ] Lock existing 1,000-level data; add regression snapshots/checksums where appropriate.
- [ ] Decide backend/provider approach after actual SDK compatibility spike.

**Acceptance:** existing gameplay works unchanged; gaps and dependencies are documented.

### P1 — Foundation and persistence
- [ ] Introduce versioned save model and safe migration.
- [ ] Define stable level IDs, worlds and data-driven catalogs up to 10k.
- [ ] Implement best-star aggregate and idempotent unlock logic.
- [ ] Add tests for replay and restore.

**Acceptance:** existing saves retained, level results unchanged, star totals cannot be farmed by replay.

### P2 — Collection and treasure
- [ ] Catalog for 220 future items, first 22 launch items.
- [ ] Star thresholds, legendary criteria and hidden treasure triggers.
- [ ] Integrate UI and reward reveals using actual assets.
- [ ] Confirm hidden object overlays don't break existing tested levels.

**Acceptance:** 22 launch rewards can be earned and persist exactly once.

### P3 — Daily challenge and abilities
- [ ] Implement challenge rules, admission, attempts and daily clock.
- [ ] Implement ability interface and initial skill families.
- [ ] Reward +1 starting move until daily reset.
- [ ] Rewarded ad/no-ads entitlement integration behind interfaces.
- [ ] Unit/integration tests for retry, day rollover, duplicate callbacks.

**Acceptance:** 1 rewarded entry (or entitlement), 2 attempts, one daily reward; no exploitable duplicates.

### P4 — Identity, backend and leaderboard
- [ ] Guest ID and cloud linking/merge.
- [ ] Verify Game Center, Play Games and Huawei login on devices.
- [ ] Shared leaderboard and rank-neighborhood API.
- [ ] Server-side integrity, retries, offline queue/conflict rules.

**Acceptance:** cross-platform rank is consistent and account restore works on real devices.

### P5 — ZPE
- [ ] Instrument raw performance events and assistance flags.
- [ ] Calibrate initial algorithm on existing 1,000 levels and test sessions.
- [ ] Home, end-of-level and profile views; versioned calculation.
- [ ] Public ranking only after fairness and privacy checks.

**Acceptance:** ZPE is stable, understandable and not presented as IQ.

### P6 — Ads, IAP and analytics
- [ ] Interstitial rules and remote configuration.
- [ ] Rewarded flow, ad failure handling and deduplication.
- [ ] No-ads IAP, receipt checks, restore/refund behavior on each store.
- [ ] Analytics, privacy disclosure and consent.

**Acceptance:** ad/IAP state and rewards survive restart, failures and duplicate callbacks.

### P6A — Firebase analytics and notifications
- [ ] Audit SDK and store compatibility; choose FCM/Huawei Push Kit provider strategy.
- [ ] Integrate Firebase initialization and analytics behind interfaces; document event schema.
- [ ] Instrument DAU/retention/engagement funnels and validate dashboard data.
- [ ] Implement push permission UX, token registration/rotation, notification preferences and deep links.
- [ ] Implement server-side scheduled daily challenge reminders and frequency caps (after approval of schedule/content).
- [ ] Test iOS APNs + FCM, Google Android FCM and HMS-only Huawei delivery/analytics; document any unsupported paths.
- [ ] Validate consent, privacy notices, deletion and opt-out behavior.

**Acceptance:** notification and analytics behavior works on supported real devices, respects preferences, and does not affect gameplay or existing save files.

### P7 — QA and release
- [ ] Automated edit-mode/play-mode tests; actual-device smoke tests.
- [ ] Validate 1,000 existing levels unchanged, plus all treasure modifiers.
- [ ] Test low-end Android/Huawei and iOS performance, memory, UI safe areas.
- [ ] Test network loss, clock manipulation, account merge, refunds and app updates.
- [ ] Prepare store-specific SDKs, signing, privacy declarations, metadata and release tracks.
- [ ] Closed beta and telemetry review; fix blockers before public launch.

**Acceptance:** release builds are verified on target platforms, no P0/P1 regressions and launch checklist approved.

## 12. DEFINITION OF DONE FOR EVERY CLAUDE CODE TASK

Each task response must contain:
1. What was discovered in the existing project.
2. What was changed (exact files/classes and public contract implications).
3. Tests executed and their actual results; if Unity/device unavailable, state what could not be run.
4. Risks, required credentials/SDK setup and pending human actions.
5. Whether existing 1,000 levels and save files are unaffected.
6. A small next-task recommendation.

Never mark a feature production-ready solely because code compiles.

## 13. OPEN DECISIONS / CONFIGURABLE POLICIES

1. Daily challenge reset timezone (recommend fixed UTC, clearly shown in UI).
2. If ads are unavailable, what free fallback grants daily admission?
3. Can player switch selected stone between first and second daily attempt?
4. Exact legendary-stone mastery unlock rules and treasure performance thresholds.
5. Whether public ZPE display can be disabled and minimum sample count for competitive ZPE leaderboard.
6. Whether no-ads entitlement is portable across stores (requires explicit backend and commercial policy).
7. What existing 1,000-level/world arrangement can be preserved; do not force thematic remapping without approval.
8. Backend provider choice after SDK/platform compatibility audit.
9. First-release actual availability of art assets for 22 stones and two world maps.
10. Daily challenge content creation: same puzzle for all players, skill-tier variants, or per-player deterministic challenge; specify fairness implications.
11. Daily challenge reminder default timing, timezone/quiet hours, notification frequency and whether reminders are opt-in only (recommended).
12. Huawei push and analytics compatibility results; decide Huawei Push Kit and alternate measurement provider after real-device verification.
13. Whether "active status" means aggregate DAU/MAU and retention (in scope) or live online presence/last-seen (separate backend feature requiring explicit approval).

## 14. COPY-PASTE START PROMPT FOR CLAUDE CODE

You are the lead Unity engineer implementing the attached `TRIKAT_CLAUDE_MASTER_PLAN.md` in my existing TriKat repository.

CRITICAL: This is **not** a new project. There are already **1,000 implemented and tested levels**. Do not recreate, rebalance, rename, bulk-edit or delete them. Do not rewrite existing gameplay unless absolutely necessary and approved. Target architecture supports **10,000 levels**, but launch content remains 1,000.

First, perform a **read-only repository audit**. Inspect Unity version, packages, scenes, gameplay code, level data, test coverage, save system, existing integrations (including Firebase/FCM/Huawei Push Kit) and build settings. Produce `docs/trikat/AUDIT.md` and `docs/trikat/IMPLEMENTATION_BACKLOG.md`, mapping each requested feature to existing code, missing work, risks and exact proposed changes. Identify how to preserve save and level compatibility.

Do **not** implement features in this first step. Do not guess missing APIs, invent successful tests, add SDKs blindly, or make unapproved product decisions. At the end, report audit findings, blockers and the smallest safe first implementation task. We will then implement the backlog incrementally, testing after each step.

---

## F. Önceki ürün yol haritasından korunan kalite ve yayın hedefleri

Aşağıdaki değerler **kabul edilmiş kesin iş sonuçları değil, ilk kullanıcı testleri için önerilen eşiklerdir**:

| Ölçüm | İlk test hedefi |
|---|---:|
| İlk bölüm tamamlama | %90+ |
| İlk 10 bölümü tamamlama | %60+ |
| D1 retention | %35+ |
| D7 retention | %12–15+ |
| Crash-free sessions | %99,5+ |
| Bölüm çözülebilirliği | Otomatik doğrulama hedefi %100; mevcut 1.000 bölümün test statüsünden ayrı raporlanmalı |

**Çapraz sistem kontrolleri:** Offline normal bölüm oynama, sunucu zamanına bağlı günlük sıfırlama, günlük +1 hamlenin ZPE adaletine etkisi, hesap bağlama ve cihaz değiştirme, reklam ödüllerinin çift verilmemesi, satın alma geri yükleme, kayıt şema migrasyonu, Firebase/FCM ile Huawei servis uyumluluğu, veri silme ve izin yönetimi.

**Zaman tahminleri:** Eski yol haritasındaki 21–32 hafta toplamı AI destekli geliştirme için bağlayıcı bir plan değildir. Gerçek cihaz, mağaza ve kullanıcı testleri ayrıca zaman gerektirir. Claude Code her aşamayı bağımlılığa ve gerçek doğrulamaya göre planlamalıdır.

## G. Claude Code'a ilk gönderilecek talimat (kopyala/yapıştır)

> Mevcut Unity TriKat projemde `TRIKAT_CLAUDE_MASTER_PLAN.md` belgesini eksiksiz oku. Bu proje sıfırdan yapılmıyor: **1.000 mevcut ve test edilmiş bölüm** var; bunları yeniden oluşturma, yeniden dengeleme veya topluca değiştirme. Uzun vadeli hedef 10.000 bölüme uygun altyapıdır, ilk sürüm 1.000 bölümdür. Oyunun marka adı TriKat, hedef paket kimliği `com.ofigame.trikat`; ancak mevcut paket kimliği, serileştirme ve kayıt uyumluluğunu kontrol etmeden teknik yeniden adlandırma yapma. Önce yalnızca **salt okunur proje denetimi** gerçekleştir. Gerçek oyun mekaniklerini koddan tespit et, gereksinimlerle karşılaştır ve `docs/trikat/AUDIT.md`, `docs/trikat/GAP_ANALYSIS.md`, `docs/trikat/IMPLEMENTATION_BACKLOG.md`, `docs/trikat/OPEN_DECISIONS.md` dosyalarını oluştur. Her bulguya gerçek dosya/sınıf kanıtı ekle. İlk aşamada oyun kodunu, sahneleri, prefabları ve bölüm verilerini değiştirme. Kesinleşmiş sekiz ürün kararını koru; öneri niteliğindeki içerik sayılarını ve açık politikaları otomatik kesinleştirme. Raporları Türkçe sun, riskleri ve en küçük güvenli ilk uygulama görevini belirt, uygulamaya geçmek için onayımı bekle.
