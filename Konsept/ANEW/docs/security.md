# Security

> Bootstrap sırasında 2026-11-14 tarihinde dolduruldu (kaynak: `Promtlar/0011`, `Promtlar/0012`). Agent'ların her plan ve review'da uyması gereken temel kurallar.

**Erişim kapsamı (şimdilik):** yalnızca yerel ağ (LAN), tek kullanıcı (geliştirici). Site LAN'da **HTTP** ile
çalışır; yapı bir ayarla HTTPS'e geçebilecek şekilde kurulur (HTTPS-ready, config ile). İnternete açılırken
**HTTPS + Pi'de TLS sonlandıran reverse proxy zorunlu** → `specs/backlog.md`. Buna rağmen kod **çok kullanıcılı**
kurulur (kayıt, roller, ban, sahiplik BR-1); internete açılma günü mimari değil yalnızca ayarlar değişir.

Hassas alanlar (`docs/domain.md`): **H-1** Kimlik ve yetki · **H-2** Gizli bilgiler · **H-3** Maliyet doğuran
çağrılar · **H-4** LLM'in ürettiği video kodunun kullanıcı PC'sinde çalıştırılması. Bu alanlarda lite modda da
netleştirme, her düzeltme turunda yeniden review ve "kasıtlı bozma" kanıtı zorunludur (`docs/testing.md`).

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- **H-2 — API key'ler:** kullanıcının online API key'leri **ASP.NET Core Data Protection** ile şifreli saklanır.
  Çözülmüş key'i görebilen ve dış LLM API'lerini çağırabilen **tek modül AiGateway**'dir (Y-7). Ajan, kullanıcının
  online API key'lerini hiç tutmaz (Y-8).
- **Data Protection anahtarları** Pi'de kalıcı bir klasörde durur ve **yedeklenir**. Kaybolurlarsa kayıtlı tüm
  API key'ler çözülemez hale gelir.
- **API key'ler (0004):** `ai_gateway.api_keys` (kullanıcı + sağlayıcı tekil) yalnızca şifreli değer (Data Protection, amaç
  `Konsept.AiGateway.ApiKeys.v1`; Api uygulama adı `anewconcept-api`, anahtarlar `DataProtection:KeysDirectory` → Pi'de
  `/opt/anewconcept/keys/api`) ve maskeli ipucu (`ilk ≤3 … son 3`) tutar; düz key sütunu yoktur. Yanıtlar yalnızca maskeli hali
  taşır; çözülemeyen satır "Key okunamıyor" olur. Kaydedilen key `SecretMasker.RegisterKnownSecret` ile bilinen sırdır (önekten
  bağımsız her logda gizlenir). Doğrulama sağlayıcının seed'deki maliyetsiz GET ucuyla, tek deneme, 10 sn; testlerde sahte
  doğrulayıcı/HTTP işleyicisi — gerçek sağlayıcı çağrısı yok. Her işlem oturumdaki kullanıcıyla süzülür; yolda key kimliği yoktur.
- Üretim gizli bilgileri (DB şifresi vb.) Pi'de ortam değişkeni ya da systemd `EnvironmentFile` ile verilir;
  repoda yalnızca `.env.example` bulunur. `appsettings*.json` içinde gizli bilgi olmaz.
- **Loglar (Serilog):** şifre hiçbir zaman loglanmaz (maskeli bile değil: `***`); API key yalnızca maskeli (`sk-…a3f`);
  token, eşleştirme kodu/sırrı, video akış token'ı ve bağlantı dizesindeki şifre düz metin görünmez. Tek mekanizma
  `SecretMasker` (0008): log olayı özellikleri, dosya/konsol çıktısı, veritabanı teknik logu, ajan özeti ve olay
  kaydı hep buradan geçer; desen tablosu `SecretMaskerTests`'te, kasıtlı bozma ile kanıtlıdır.
- **Web ↔ Api iç uçları** (ör. `POST /api/internal/logs`): Pi'de üretilen paylaşılan servis anahtarı
  (`X-Konsept-Service-Key`; `/etc/anewconcept/{api,web}.env` → `Internal__ServiceKey`); sabit zamanlı
  karşılaştırma; anahtar yapılandırılmamışsa her istek 401 (0008, plan Risk 2).

## Input & output
- **Sınırda doğrulama:** her API isteği FluentValidation'dan geçer; doğrulama generic validation filter ile
  otomatik yapılır, controller gövdesi tek satırdır (K-4, Y-6 — architecture test + review).
- **Encode:** Razor otomatik encode eder. Kullanıcı ya da LLM içeriği için **`Html.Raw` yasak** (review).
- **LLM çıktısı güvenilmeyen veridir:** konsept metni ekranda düz metin gösterilir. İnternetten araştırılan içerik
  promptlarda talimat olarak değil **"veri" olarak işaretlenir** (prompt injection önlemi).
- **CSRF:** MVC Ajax POST'larında (`api.js` içindeki `$.ajax`, K-6) anti-forgery token gönderilir.
- **Güvenlik başlıkları:** CSP (inline script yasak; K-7 ile uyumlu), `X-Content-Type-Options`, `frame-ancestors`.
- **Hata mesajları:** beklenmeyen hatalar global middleware'de **ProblemDetails** ile Türkçe, sade bir mesaj ve
  korelasyon numarasıyla döner. **Stack trace, SQL ve API key kullanıcıya asla gösterilmez** (`docs/conventions.md`).
- **H-3:** online çağrılarda yeniden deneme yalnızca 429 ve 5xx'te, en fazla 2 kez, artan beklemeyle; 4xx'te tekrar
  yok. Testlerde gerçek LLM çağrısı yapılmaz (stub). Her online çağrının token ve maliyeti kaydedilir (BR-12).
- **Çağrı kapısı (0013):** online model yalnızca `IAiCallGateway` ile çağrılır. Sağlayıcıya gitmeden önce: model yetkisi
  (`model.use:*` / `model.use:<model>`), çağıranın **kendi** key satırı (yalnızca çağıranın `UserId`'siyle; depo süzgecine ek
  olarak kapı da sahipliği denetler) ve çözülmesi, efor (katalog dışı → red), çıktı sınırı (katalog limitine kırpılır). Tekrar
  yalnızca 429/5xx/kurulamayan bağlantıda (≤3 deneme, 1 sn → 2 sn); zaman aşımı (katalog, varsayılan 600 sn) ve gönderildikten
  sonra kopan yanıt tekrar edilmez. Sağlayıcıya ulaşan her deneme `ai_gateway.spend_records`'a yazılır (`numeric(20,10)` USD;
  hata yanıtı 0, token yoksa giriş tahmini "tahmini"; OpenRouter'ın `usage.cost`'u esas). Hata gövdesi okunmaz, kullanıcıya
  yalnızca Türkçe sabit mesaj döner; HTTP istemcisi (`ai-calls`) logsuz, yönlendirmesiz, `Timeout=Infinite` (süre kapıda).
  Ham HTTP kullanılır (SDK'ların kendi tekrarları H-3'ü delerdi).

### H-4 — Kullanıcı PC'sinde LLM kodu
- LLM'in ürettiği video kodu **yalnızca kendi iş klasörüne erişebilen, ağ erişimi kapalı** bir süreçte render
  edilir (Y-8). Yöntem (0018, [ADR-0004](decisions/0004-render-sandbox-fallback.md)), yönetici yetkisi istemez: L0 sabit sözleşme
  (yalnız `image.png`/`gsap.min.js`) · L1 ortak yasaklı yapı kapısı `RenderCodeGate` (Pi + ajan; ret → en fazla 1 düzeltme) · L2 iş
  klasörü, sıfırdan ortam, bağlantı (symlink/junction) reddi · L3 Chromium sandbox'ı zorunlu + `render-guard.mjs` (ölü proxy, DNS
  kapatma, WebRTC UDP kapatma; sandbox kapatan bayraklar ayıklanır) · L4 Job Object (süre/bellek/süreç, kill-on-close; sınırlar
  `agentsettings.json` `render*`) · L5 ajanın CSP etiketi (`'unsafe-eval'` yok, `connect-src 'self'`) · L6 Node izin modeli (okuma: iş +
  bileşenler, yazma: iş) · L7 ffprobe çıktı kapısı (çıktı hiç çalıştırılmaz). AppContainer spike'ı B-125, isteğe bağlı güçlü mod
  (güvenlik duvarı) B-124.
- Ajan güncellemeleri **imza ya da hash doğrulamasından** geçmeden kurulmaz.
- Ajan yalnızca **kimliği doğrulanmış API bağlantısından** (SignalR) gelen komutları çalıştırır.
- Videolar kullanıcının PC'sinde kalır; web player ajanın **yalnızca localhost'a açık, kısa ömürlü token'lı**
  stream'inden oynatır.
- Kurulum, silme ve diğer ajan işleri H-4 kapsamında **değildir** (kullanıcı emriyle çalışan, bizim yazdığımız kod).

## AuthN / AuthZ
**Kimlik doğrulama (H-1)**
- **Login:** e-posta + şifre (Identity modülü). Şifre ASP.NET Core Identity'nin **`PasswordHasher`**'ıyla (V3: PBKDF2-HMAC-SHA512,
  rastgele tuz) hash'lenir; `UserManager`/`IdentityDbContext` kullanılmaz (plan 0002 Risk 1). **3 hatalı denemede 15 dakika kilit**
  (0002); sayaç tek atomik SQL ifadesiyle sayılır, eşzamanlı denemeler atlatamaz; kilitliyken denemeler süreyi uzatmaz.
  Her yanlış girişte kalan hak gösterilir: "Yanlış girdiniz, 2 hakkınız kaldı".
  Bu mesaj e-postanın kayıtlı olup olmadığını belli eder (user enumeration); LAN + tek kullanıcı için kabul edildi,
  internete açılırken yeniden değerlendirilecek → `specs/backlog.md`.
- **Kayıt:** ad + e-posta + şifre ile doğrudan. E-posta doğrulaması ertelendi → `specs/backlog.md`.
- **Web ↔ API:** API kısa ömürlü **JWT (15 dk; `sub`, `sid`, `name`)** + **dönen (rotating) refresh token** (`knr_` + 32 bayt
  rastgele) verir; refresh token DB'de SHA-256 hash'li tutulur ve tek ifadelik `UPDATE` ile döner (eski token ikinci kez
  çalışmaz). Web bunları **Data Protection ile şifreli, HttpOnly cookie** (`konsept.auth`, SameSite=Lax, 14 gün kayan) içinde
  saklar; tarayıcı token'ı görmez. HTTPS'e geçince `Auth__SecureCookie=true`; Web'in Data Protection anahtarları
  `DataProtection__KeysDirectory` ayarlıysa orada kalıcıdır.
- **Oturum doğrulama (0002):** Api'de her JWT'nin oturumu (`sid`) JwtBearer `OnTokenValidated`'da veritabanından doğrulanır
  (iptal edilmemiş, son kullanım + 14 gün geçmemiş; son kullanım en fazla dakikada bir kaydırılır) — çıkış diğer tarayıcıyı
  15 dakikayı beklemeden **anında** düşürür. Web her istekte oturumu Api'ye sorar; düşen oturumda sayfa
  `/giris?oturum=bitti`'ye, Ajax isteği 401 JSON'a (`auth.session_expired`; `api.js` girişe yönlendirir) gider.
- **Refresh tek uçuş (0002):** Web'de aynı eski refresh token için eşzamanlı istekler (sayfa + 3 sn yoklama) eski token'ın
  hash'i başına tek kilitle Api'ye **tek** çağrı yapar, sonuç 60 sn bellekte paylaşılır (Web tek süreç). Api'de eski token
  yeniden gelirse yalnızca 401 + Warning (tüm oturumları iptal edilmez; LAN'da yanlış alarmla kullanıcıyı atmamak için).
- **İmza anahtarı:** `Auth__SigningKey` (≥ 32 bayt) Pi'de ortam değişkeni / `EnvironmentFile` ile verilir; Development dışında
  yoksa Api açılmaz; Development'ta süreç başına rastgele üretilir ve Warning yazılır (anahtarın kendisi asla). Ad
  `SigningKey` ile bittiği için yapılandırma taramasıyla bilinen sır olarak her logda gizlenir.
- **Ajan girişi:** cihaz eşleştirme — ajan bir kod gösterir, kullanıcı web'de onaylar, ajan kendi refresh token'ını
  alır. Ajana şifre yazdırılmaz. **Uygulama (0012):** kod `ABCD-1234` (O/0/I/1 yok), 10 dk, tek kullanımlık, bellekte; kod
  alma/sorgu/onay/ret IP başına 0002 sınırıyla (`agent-pairing` politikası). Ajan durumu yalnızca kendisine verilen 256 bit onay
  sırrıyla sorar; onayda ajan oturum belirteci (256 bit, `knr_` önekli → her logda gizlenir) **bir kez** teslim edilir, sunucuda
  yalnızca SHA-256 hash'i (`identity.agent_devices`) durur, döndürülmez, son kullanımdan 90 gün kayar (dakikada en fazla bir
  yazma). Hub yalnızca `Authorization: Bearer <ajan belirteci>` ile açılır (adreste taşınmaz, istek loguna düşmez); sahip ve cihaz
  hub kimliğinden okunur, istemciden alınmaz. Kullanıcı başına tek ajan: yeni eşleştirme öncekini iptal eder. Ajan belirteci ajan
  klasöründe `agent-session.dat`'ta düz durur (şifreleme backlog'da).
- **Çıkış (BR-14):** web'den çıkış kullanıcının **tüm oturumlarını, ajan dahil** iptal eder (0012: `agent_devices` iptal +
  `IAgentDeviceRevocationListener` → "SessionClosed" bildirimi ve sunucu tarafı kesme; eski belirteç 401); sunucu ajanın SignalR
  bağlantısını kapatır.
- **Ban (BR-13):** banlanan kullanıcının tüm token'ları anında iptal edilir.

**Yetkilendirme**
- **Varsayılan: kapalı (default-deny).** Her API endpoint'i giriş ister; yalnızca login ve kayıt gibi açıkça
  işaretlenmiş uçlar açıktır.
- **Yetki tabanlı politikalar:** örn. `admin.panel`, `publish.use`, `model.use:<model>`; roller bu yetkilerin paketidir.
- **Model yetkileri (0013):** Api her istekte rol yetkilerine ek olarak model claim'lerini kurar (`SessionValidatingJwtEvents`):
  ayar yoksa her rol `model.use:*`; kısıtlama yalnızca ayarla — `Identity:ModelPermissions:User` / `:Admin` (virgüllü model
  kimlikleri; Pi'de `Identity__ModelPermissions__User`). Ekran yoktur. Oturum yanıtının `permissions` listesi değişmez.
- **Roller (0011):** iki rol — `user` (varsayılan, yetkisiz) ve `admin` (`admin.panel`); `identity.users.role` sütununda (check
  constraint). **Yetki her istekte veritabanından okunur, JWT'de rol/yetki taşınmaz:** Api'nin her istekteki oturum doğrulaması
  (`sid`) aynı sorguda rolü de alır ve principal'a `konsept.perm` claim'lerini ekler; token'dan gelen aynı adlı claim'ler önce
  silinir (sahte claim işe yaramaz). Rol değişikliği bir sonraki istekte geçerlidir, yeniden giriş gerekmez. Web de yetkileri her
  istekte `GET api/auth/session` yanıtından yeniden kurar; Api erişilemezse hiç yetki yoktur (kapalı başarısızlık). Yetkisiz sayfa
  `/yetkisiz` (403, "Bu sayfaya erişim yetkiniz yok"), yetkisiz Ajax 403 JSON (`auth.forbidden`, "Bu işlem için yetkiniz yok"),
  Api'de 403 ve gövdede admin verisi yok.
- **İlk admin (0011):** yalnızca **`Identity:AdminEmail`** ayarı (Pi'de `/etc/anewconcept/api.env` → `Identity__AdminEmail`;
  gizli bilgi değil, repoda değeri yok — `appsettings.json`'da boş). Karşılaştırma 0002 kuralıyla (kırpma + kültürden bağımsız
  büyük harf, ordinal). Kayıtta eşleşen hesap admin olur; **açılış senkronu** (arka planda, DB hazır değilse 30 sn arayla yeniden
  dener, migrate-on-startup kapalı olsa da çalışır) ayarı tek doğruluk kaynağı yapar: eşleşen hesap admin, diğer her hesap `user`;
  ayar boşsa kimse admin değildir. İstekteki hiçbir alan (kayıt formuna eklenen `role` vb.) rolü etkilemez; rol yönetim arayüzü
  yoktur. `deploy/pi/provision.sh` bu satırı yazmaz, silmez, ezmez (`DeployAdminEmailTests`).
- **Asıl yetki kontrolü API'dedir.** Web yetkisiz butonları gizler ama karar her zaman API'nindir.
- **Sahiplik (BR-1):** EF Core global query filter ile her sorgu otomatik olarak `UserId`'ye göre filtrelenir.
- **Rate limiting:** login ve üretim uçlarında. Giriş/kayıt sınırı (0002) **yalnızca Api'de**: istemci IP'si başına 60 sn'de
  10 deneme (kayan pencere: son 60 sn içindeki izinler sayılır, sabit pencere kenarında patlama olmaz), 11.'de 60 sn blok
  (`FixedBlockRateLimiter`, `TimeProvider`'lı); reddedilen istek servise ulaşmaz, hak sayacı
  değişmez, 429 `auth.too_many_attempts`. Web tarayıcının IP'sini `X-Konsept-Client-Ip` ile iletir; Api bu başlığa yalnızca
  geçerli servis anahtarıyla güvenir.

## Dependencies
- Yeni NuGet ya da npm paketi **PLAN aşamasında gerekçesiyle önerilir, insan onaylar.**
- Kontrol listesi:
  - **Lisans:** MIT, Apache-2.0, BSD uygun; GPL ve ticari lisanslı paketler (ör. FluentAssertions v8) kabul edilmez.
  - **Bakım:** son 12 ayda en az bir sürüm çıkmış olmalı.
  - **Bilinen açık yok:** `dotnet list package --vulnerable` ve `npm audit` `scripts/check` içinde çalışır
    (greenfield: adımlar walking skeleton ile etkinleşir).
- Versiyonlar sabittir; tüm NuGet versiyonları merkezi olarak `Directory.Packages.props` içinde tutulur.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
