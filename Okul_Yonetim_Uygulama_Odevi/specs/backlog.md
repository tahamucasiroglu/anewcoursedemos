# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- A criterion moved out at the ship gate (R-08, `→ moved to B-<n>`) lands here too, by the
  human's ship decision; the entry names the spec and criterion.
- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
  A decision deferred at bootstrap (its REPORT) uses Source: bootstrap <YYYY-MM-DD>.
-->

- **B-1** — Gerçek ödeme sağlayıcı entegrasyonu (iyzico/Paycell) `IPaymentProvider` arkasına; MVP'de manuel tahsilat kaydı — Source: bootstrap 2026-11-19 — Severity: feature — Recommendation: sağlayıcı hesabı açılınca ayrı spec — Deferred by: tahamucasiroglu, 2026-11-19, hesap henüz yok
- **B-2** — Gerçek SMS sağlayıcı (ör. Netgsm) `ISmsSender` arkasına; MVP'de log — Source: bootstrap 2026-11-19 — Severity: feature — Recommendation: hesap açılınca ayrı spec — Deferred by: tahamucasiroglu, 2026-11-19, hesap henüz yok
- **B-3** — WhatsApp Business kanalı `IMessagingChannel` arkasına (kapsamda Faz 2) — Source: bootstrap 2026-11-19 — Severity: feature — Recommendation: Faz 2 kapısından sonra — Deferred by: tahamucasiroglu, 2026-11-19, hesap yok
- **B-4** — AI metin sağlayıcısı seçimi (`IAiTextGenerator`; "eğitimde kullanılmaz" sözleşme şartıyla). Ses→metin (`ISpeechToText`) kararı verildi (2026-11-19): **yerelde çalışan Whisper benzeri model**; makinedeki mevcut kurulumlara dokunulmaz, bu repo için ayrı kurulur (model ve kurulum sesli not spec'inde) — Source: bootstrap 2026-11-19 — Severity: decision — Recommendation: AI sesli not spec'inin ANALYZE aşamasında — Deferred by: tahamucasiroglu, 2026-11-19, MVP iskeletinde sahte port yeterli
- **B-5** — PostgreSQL Row-Level Security ile ikinci kiracı izolasyon hattı (S1) — Source: bootstrap 2026-11-19 — Severity: hardening — Recommendation: MVP modülleri oturduktan sonra ayrı spec — Deferred by: tahamucasiroglu, 2026-11-19, global query filter + testler MVP için yeterli
- **B-6** — Push bildirim sağlayıcısı (Expo push / FCM / APNs) `IPushNotifier` arkasına — Source: bootstrap 2026-11-19 — Severity: feature — Recommendation: mobil bildirim spec'inde — Deferred by: tahamucasiroglu, 2026-11-19, MVP'de sahte port
- **B-7** — Web/mobil istemci hatalarının sunucu loguna toplanması (MVP'de yalnızca geliştirici konsolu) — Source: bootstrap 2026-11-19 (ADR-0003) — Severity: improvement — Recommendation: giriş/kimlik spec'inden sonra — Deferred by: tahamucasiroglu, 2026-11-19, iskelette gereksiz
- **B-8** — Mobilde durum değişimi iOS VoiceOver'da otomatik duyurulmuyor (`accessibilityLiveRegion` yalnızca Android); `AccessibilityInfo.announceForAccessibility` eklenebilir — Source: spec 0001, review round 1 (`mobile/src/features/system-status/SystemStatusScreen.tsx`) — Severity: Info — Recommendation: mobil erişilebilirlik ilk gerçek ekranla birlikte — Deferred by: tahamucasiroglu, 2026-11-19, iskelette tek durum metni var
- **B-9** — Log klasörü varsayılanı göreli (`logs`), servis olarak çalışınca çalışma dizinine göre çözülür — Source: spec 0001, review round 1 (`backend/src/WebAPI/Logging/LoggingSetup.cs`) — Severity: Info — Recommendation: Raspberry Pi kurulum spec'inde mutlak yol / içerik köküne göre çözüm — Deferred by: tahamucasiroglu, 2026-11-19, Pi kurulumuyla birlikte — **Kapandı:** spec 0003 (Pi\'de `FileLogging__Folder=/var/log/okul`, mutlak yol; servis ayarında)
- **B-10** — OpenAPI LF normalizasyonu yalnızca şema açıklamalarında; işlem (endpoint) açıklamaları çok satırlı olursa Windows/Linux arasında openapi.json farkı doğar — Source: spec 0001, review round 2 (`backend/src/WebAPI/OkulApi.cs`) — Severity: Info — Recommendation: ilk çok satırlı endpoint açıklamasıyla birlikte operation transformer eklenir — Deferred by: tahamucasiroglu, 2026-11-19, bugün endpoint açıklaması yok
- **B-11** — Tema seçimi yalnızca cihazda saklanıyor; kullanıcı girişi gelince hesaba kaydedilip cihazlar arası taşınabilir — Source: spec 0002, analiz (kapsam dışı) — Severity: Info — Recommendation: giriş/kimlik spec'inde kullanıcı tercihleri ile birlikte — Deferred by: tahamucasiroglu, 2026-11-19, henüz giriş yok
- **B-12** — Gerçek Android derlemesinde `userInterfaceStyle: "automatic"` için `expo-system-ui` gerekir; tema okunana kadar (≤ 500 ms) kök zemin varsayılan açık renkte kalabilir — Source: spec 0002, review round 1 (`mobile/app.json`, `mobile/src/theme/ThemeProvider.tsx`) — Severity: Low — Recommendation: ilk Android derleme işinde `npx expo install expo-system-ui` ve kök zemini temadan ayarlama (`SystemUI.setBackgroundColorAsync`) — Deferred by: tahamucasiroglu, 2026-11-19, bugün yalnızca Expo Go / web
- **B-13** — Raspberry Pi deneme ortamı yalnızca yerel ağ ve düz HTTP; dışarıdan erişim gerekirse HTTPS + erişim kısıtı (ör. VPN/Tailscale) — Source: spec 0003, analiz (kapsam dışı) — Severity: Info — Recommendation: pilot okul veya ağ dışı deneme ihtiyacında ayrı spec — Deferred by: tahamucasiroglu, 2026-11-19, yalnızca yerel ağ isteniyor
- **B-14** — Pi'deki veritabanının otomatik yedeği yok — Source: spec 0003, analiz (kapsam dışı) — Severity: Info — Recommendation: gerçek veri girilmeden önce günlük `pg_dump` + saklama süresi — Deferred by: tahamucasiroglu, 2026-11-19, deneme verisi
- **B-15** — Pi'de tek veritabanı rolü (`okul_app`, veritabanı sahibi, migration'ları kendisi uygular); daha sıkı model: şema sahibi rol + yalnızca veri okuyup yazan uygulama rolü — Source: spec 0003, plan Risks 1 — Severity: Info — Recommendation: gerçek okul verisi gelmeden önce iki role geçiş — Deferred by: tahamucasiroglu, 2026-11-19, sade çözüm yeterli
