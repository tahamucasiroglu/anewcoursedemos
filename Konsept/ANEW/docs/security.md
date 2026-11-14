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
- Üretim gizli bilgileri (DB şifresi vb.) Pi'de ortam değişkeni ya da systemd `EnvironmentFile` ile verilir;
  repoda yalnızca `.env.example` bulunur. `appsettings*.json` içinde gizli bilgi olmaz.
- **Loglar (Serilog):** şifre hiçbir zaman loglanmaz; API key yalnızca maskeli (`sk-…a3f`).

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

### H-4 — Kullanıcı PC'sinde LLM kodu
- LLM'in ürettiği video kodu **yalnızca kendi iş klasörüne erişebilen, ağ erişimi kapalı** bir süreçte render
  edilir (Y-8). Somut yöntem (Windows Job Object / AppContainer, Node permission model…) **henüz seçilmedi**:
  video motoru araştırma raporunda (`raporlar/`) kesinleşecek → `specs/backlog.md`.
- Ajan güncellemeleri **imza ya da hash doğrulamasından** geçmeden kurulmaz.
- Ajan yalnızca **kimliği doğrulanmış API bağlantısından** (SignalR) gelen komutları çalıştırır.
- Videolar kullanıcının PC'sinde kalır; web player ajanın **yalnızca localhost'a açık, kısa ömürlü token'lı**
  stream'inden oynatır.
- Kurulum, silme ve diğer ajan işleri H-4 kapsamında **değildir** (kullanıcı emriyle çalışan, bizim yazdığımız kod).

## AuthN / AuthZ
**Kimlik doğrulama (H-1)**
- **Login:** e-posta + şifre, ASP.NET Core Identity (Identity modülü). **3 hatalı denemede geçici kilit**; kilit
  süresi login spec'inde netleşecek. Her yanlış girişte kalan hak gösterilir: "Yanlış girdiniz, 2 hakkınız kaldı".
  Bu mesaj e-postanın kayıtlı olup olmadığını belli eder (user enumeration); LAN + tek kullanıcı için kabul edildi,
  internete açılırken yeniden değerlendirilecek → `specs/backlog.md`.
- **Kayıt:** ad + e-posta + şifre ile doğrudan. E-posta doğrulaması ertelendi → `specs/backlog.md`.
- **Web ↔ API:** API kısa ömürlü **JWT (15 dk)** + **dönen (rotating) refresh token** verir; refresh token DB'de
  hash'li tutulur. Web bunları **sunucu tarafında, şifreli, HttpOnly cookie** içinde saklar; tarayıcı token'ı görmez.
- **Ajan girişi:** cihaz eşleştirme — ajan bir kod gösterir, kullanıcı web'de onaylar, ajan kendi refresh token'ını
  alır. Ajana şifre yazdırılmaz.
- **Çıkış (BR-14):** web'den çıkış kullanıcının **tüm oturumlarını, ajan dahil** iptal eder; sunucu ajanın SignalR
  bağlantısını kapatır.
- **Ban (BR-13):** banlanan kullanıcının tüm token'ları anında iptal edilir.

**Yetkilendirme**
- **Varsayılan: kapalı (default-deny).** Her API endpoint'i giriş ister; yalnızca login ve kayıt gibi açıkça
  işaretlenmiş uçlar açıktır.
- **Yetki tabanlı politikalar:** örn. `admin.panel`, `publish.use`, `model.use:<model>`; roller bu yetkilerin paketidir.
- **Asıl yetki kontrolü API'dedir.** Web yetkisiz butonları gizler ama karar her zaman API'nindir.
- **Sahiplik (BR-1):** EF Core global query filter ile her sorgu otomatik olarak `UserId`'ye göre filtrelenir.
- **Rate limiting:** login ve üretim uçlarında.

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
