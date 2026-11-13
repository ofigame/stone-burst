# App Review — 4.3(a) yanıtı ve yeniden gönderim notu (taslak)

Tarih: 2026-11-13. Ret: Stone Burst iOS 1.0 (1.0.0, yapı 12), Guideline 4.3(a) Design – Spam (DURUM.md). Claude yazdı; **gönderimi kullanıcı yapar**. İçerik koddan ve OYUN.md'den doğrulandı. "Extended Review" uyarısı nedeniyle tek, eksiksiz bir gönderim hedefleniyor.

Kullanım:
1. **Yanıt (A):** App Store Connect → Stone Burst → App Review → reddedilen gönderim → Messages → "Reply to App Review" alanına yapıştırılır. Yeni yapıyla birlikte gönderilecekse (B) ile aynı gün.
2. **Not (B):** yeni yapı (1.0.1) seçildikten sonra sürüm sayfası → App Review Information → Notes alanındaki eski metnin yerine (eski metin: `APP_REVIEW_NOTES.md`).
3. Mağaza metinleri ve görseller: `MAGAZA_1_0_1_FARK.md`, `docs/magaza/fark/`.

Köşeli parantezli yerler gönderimden önce doldurulur.

---

## (A) Reply to App Review

```
Hello App Review team,

Thank you for the review. We understand the concern under Guideline 4.3(a) and would like to explain how Stone Burst differs from other tile and match games, and what we changed in this submission.

Stone Burst is an original game designed and built by our studio. It is not based on a purchased template or on any of our other apps. Our only other submission, "foi - Robot Battle & Duel", is a different genre (robot battles) with its own codebase and artwork.

What is unique to Stone Burst:
- Layered stacks: every cell is a stack of up to 4 stone layers and only the top layer is visible. Tapping a group of 3+ removes one layer from each stone and reveals the hidden colors below. Players plan several layers ahead; a long press shows a stack's hidden layers.
- Chain rule: only freshly revealed stones can chain, so cascades are a planned outcome, not luck.
- Power stone tray: large groups create rockets or a Golden Roll that wait in a tray and are used whenever the player chooses, without spending a move.
- Mechanics that change the puzzle: ice, color locks, envelopes, sponges, gates opened by buttons, turning bases, paperclips, fragile crystals and paint drops.
- 1,000 handcrafted levels, each verified by our solver against a recorded solution. The hint system only shows a move that the solver has verified leads to a win.
- A daily puzzle with its own rule each day, a ZPE skill index (move efficiency, first-try wins, solving speed, no-help finishes) and a collection of 3D stones the player can rotate and inspect.

Changes in this build [1.0.1 (yapı no)]:
- New screenshots and description that show these mechanics instead of a generic "match" pitch, and keywords without generic trending terms.
- Removed a developer test scene from the app binary and rewrote a shared text utility, so the binary contains only Stone Burst code.

The fastest way to see the difference: play levels 2 (chain rule) and 6-9 (power stones), and long-press any stack to see its layers. Levels 21 and 22 introduce paperclips and color locks.

We are happy to provide a video or more details if helpful.

Best regards,
[Ad Soyad / Ofigame]
```

## (B) App Review Notes (yeni yapı için)

```
Stone Burst is a single-player puzzle game built around layered stacks. No account or login is needed; all levels are playable offline.

HOW TO PLAY
Each cell is a stack of up to 4 colored stone layers; only the top layer is visible. Tap a group of 3 or more touching stones of the same color to peel one layer from each. If the newly revealed stones form a group of 3+, they burst on their own (chain rule: only freshly revealed stones chain). Complete the level goals within the move limit. Long-press any stack to see its hidden layers.

WHAT TO LOOK AT
- Level 2: chain rule. Levels 6-9: rockets and the Golden Roll, kept in a tray and used without spending a move. Levels 21-22: paperclips and color locks. Later levels add ice, envelopes, sponges, gates with buttons, turning bases and fragile crystals.
- Map > Daily: a daily puzzle with its own rule.
- Map > ZPE badge (top left): the skill index. Stones tab: 3D collectible stones.
- Every level is verified by our solver; hints show a verified move.

HEARTS
From level 21, leaving a level after the first move costs 1 heart (max 5, +1 every 30 minutes). Levels 1-20 never use hearts. When hearts run out, an optional rewarded ad refills them.

ADS
Google AdMob. All rewarded ads are optional: +1 move and one stone placement after running out of moves, a hint, daily puzzle entry, and heart refill. Interstitial ads appear only after level 11, at most every 4 levels and at least 4 minutes apart. In the EU the Google consent form is shown; Settings contains a "Privacy options" row. The App Tracking Transparency prompt is shown before personalized ads.

GAME CENTER / ONLINE
Game Center sign-in is optional and used for the leaderboard and cloud save of progress (Firebase). Declining sign-in does not limit gameplay. Settings > Delete account removes the online data.

There are no in-app purchases.
```

Notlar (gönderen için):
- (A)'daki "1,000 handcrafted levels" ifadesi App Review'a yönelik bir gerçek bilgi; mağaza metinlerinde "1000 bölüm" kullanılmıyor (kullanıcı kararı 2026-11-12). İstenirse (A)'dan da çıkarılabilir.
- (B)'deki ATT ve hesap silme satırları yapı 12'deki kodla uyumlu (ATT bf77ffb; hesap silme 2026-11-13, `46_hesap_sil_onay`). Yeni yapı bu commit'lerden sonra alınmalı.
- Bölüm numaraları oyundaki görünen numaralardır (1'den başlar).
