# Security

> Ajanların her plan ve review'da uyduğu temel kurallar.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Gerçek sırlar `dotnet user-secrets` (geliştirme) veya ortam değişkenleriyle verilir.
- Seed kullanıcı parolaları yalnızca Development ortamında ve `appsettings.Development.json`'da
  durur, açıkça "demo parolası" olarak işaretlenir; Production'da seed kullanıcı oluşturulmaz.

## Input & output
- Her POST antiforgery token ile korunur: global `AutoValidateAntiforgeryTokenAttribute` filtresi.
  Testlerde kapatılmaz (`docs/conventions.md`, Known traps).
- Girdi doğrulaması sınırda (ViewModel üzerinde DataAnnotations) yapılır; iş kuralları Domain'de
  yeniden doğrulanır — istemci tarafı doğrulamaya asla güvenilmez.
- Razor'ın varsayılan HTML encoding'i açıktır; `Html.Raw` ve `HtmlString` ile ham çıktı **yasaktır**.
- Hata mesajlarında stack trace, SQL, iç ID veya istisna metni gösterilmez (`docs/conventions.md`, Error handling).
- Güvenlik başlıkları (tüm yanıtlar): `Content-Security-Policy: default-src 'self'` (inline script
  ve inline event handler yok; JS dosyaları `wwwroot`'tan), `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: no-referrer`, `X-Frame-Options: DENY`; Production'da HSTS ve HTTPS yönlendirmesi.

## AuthN / AuthZ
- **Kimlik doğrulama:** ASP.NET Core Identity + cookie. Kayıt ekranı yok (v1). Parola politikası
  Identity varsayılanı. Cookie: `HttpOnly`, `SecurePolicy = Always`, `SameSite = Lax`.
- **Varsayılan reddet:** `FallbackPolicy = RequireAuthenticatedUser`. Anonim erişim yalnızca
  açıkça `[AllowAnonymous]` ile verilir (giriş sayfası, hata sayfası, statik dosyalar).
- **Roller:** `Employee`, `Manager`. Yönetici sayfaları `[Authorize(Roles = "Manager")]` (veya eşdeğer policy).
- **Kaynak bazlı yetki Application katmanında zorunludur** (hassas alan, `docs/domain.md`):
  "bu talep mevcut kullanıcının mı?" ve "bu çalışan mevcut yöneticiye doğrudan bağlı mı?"
  kontrolleri kullanım senaryosunda yapılır; controller'daki attribute'lar ek savunmadır, tek
  savunma değildir.
- Kullanıcının erişemediği bir kaynağa istek → **404** (varlığı sızdırmamak için 403 değil).
- Mevcut kullanıcı kimliği yalnızca kimlik doğrulanmış oturumdan okunur; formdan veya sorgu
  dizesinden gelen bir "çalışan ID'si" yetki kararına girdi olamaz.

## Dependencies
- Yeni bir NuGet paketi plan içinde gerekçesiyle önerilir; insan plan onayıyla onaylar.
- Kontrol listesi: lisans (MIT / Apache-2.0 / BSD tercih; copyleft ADR gerektirir), son 12 ayda
  bakım (sürüm veya commit), bilinen CVE yok.
- `dotnet list package --vulnerable --include-transitive` check'te full adım olarak çalışır; bulgu = kırmızı.
- Başlangıçta onaylı paketler: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`,
  `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `xunit.v3`,
  `Microsoft.AspNetCore.Mvc.Testing`, `AngleSharp`, `TngTech.ArchUnitNET`, `TngTech.ArchUnitNET.xUnitV3`,
  `Microsoft.Extensions.TimeProvider.Testing`.
- Frontend için CDN veya üçüncü taraf JS/CSS kütüphanesi yok (CSP `'self'`).

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
