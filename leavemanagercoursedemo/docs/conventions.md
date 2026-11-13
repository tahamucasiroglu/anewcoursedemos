# Conventions

> Yalnızca gerçek kurallar: her kural ya araçla zorlanır (tercih) ya da review'da kontrol edilir.

## Language
- Chat language (interviews, sessions): Turkish (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Turkish (`tr`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: English. Arayüz metinleri (Razor view'lar):
  Türkçe, doğrudan view'larda (v1 tek dilli; `.resx` yok).
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- .NET SDK 10.0.x (LTS), `net10.0` hedefi; C# dil sürümü SDK varsayılanı. Kök `global.json` SDK'yı
  `10.0.*` bandına sabitler (`rollForward: latestFeature`).
- ASP.NET Core MVC 10 (Razor view'lar), EF Core 10 + `Microsoft.EntityFrameworkCore.Sqlite`,
  ASP.NET Core Identity (EF Core store).
- Frontend: düz HTML / CSS / JavaScript; framework, bundler, Node/npm yok.
- Test: xUnit v3 (`xunit.v3`, Microsoft Testing Platform modu — `global.json` `test.runner`; .NET 10 SDK
  MTP tabanlı xUnit v3'ü VSTest modunda koşturmaz),
  `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), AngleSharp,
  ArchUnitNET (`TngTech.ArchUnitNET.xUnitV3`), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`).
  Çözüm dosyası `LeaveManager.slnx` (.NET 10 varsayılan biçimi).
- Paket sürümleri merkezi olarak `Directory.Packages.props` içinde (Central Package Management).

## Naming
- Standart .NET: tipler / metotlar / özellikler `PascalCase`; private alanlar `_camelCase`;
  arayüzler `I` önekli; async metotlar `…Async` sonekli; bir dosyada bir public tip, dosya adı = tip adı.
- Proje / namespace: `LeaveManager.<Katman>[.<Klasör>]`.
- Testler: `Method_State_ExpectedResult` (örn. `Approve_WhenNotPending_Throws`); bir iş kuralını
  kanıtlayan test adı BR numarasıyla başlar: `BR3_Create_WhenExceedsAvailableBalance_Throws`.
- ViewModel'ler `…ViewModel`, Application giriş modelleri `…Command`, sorgular `…Query`.

## Error handling
- İş kuralı ihlali Domain'de `DomainException` olarak fırlatılır (makine okunur `Code`, örn.
  `"BR-3"`, + Türkçe kullanıcı mesajı). Application bunu `Result` tipine çevirmez.
- Controller `DomainException`'ı yakalar, mesajı `ModelState`'e ekler ve formu yeniden gösterir.
- Kaynak bulunamadı veya kullanıcının erişimi yok → 404 (`docs/security.md`).
- Beklenmeyen hatalar global `UseExceptionHandler("/Home/Error")` ile genel hata sayfasına gider;
  kullanıcıya stack trace, SQL, iç ID veya istisna mesajı asla gösterilmez. Ayrıntı yalnızca loglanır.
- Boş `catch` yasak; yakalanan istisna ya işlenir ya loglanıp yeniden fırlatılır.

## Data rules
- İzin tarihleri `DateOnly` (saat ve zaman dilimi yok). Kayıt zaman damgaları `DateTimeOffset`, UTC.
- Gün sayıları `int` (yarım gün kapsam dışı).
- ID'ler `Guid`.
- Saat yalnızca enjekte edilen `TimeProvider` üzerinden okunur (`docs/architecture.md`, yasak 5).
- Enum'lar veritabanında string saklanır (`HasConversion<string>()`).
- Metin karşılaştırmaları kültürden bağımsızdır: `StringComparison.Ordinal[IgnoreCase]`,
  `ToUpperInvariant()`.

## Known traps
- SQLite `:memory:` veritabanı bağlantı kapanınca yok olur — test fabrikasında tek bir açık
  `SqliteConnection` tutup DbContext'e onu verin, her DbContext kendi bağlantısını açmasın —
  from bootstrap 2026-11-07.
- SQLite `DateTimeOffset` üzerinde sıralama/karşılaştırmayı SQL'e çeviremez — zaman damgası
  sütunlarına `DateTimeOffsetToBinaryConverter` uygulayın (`DateOnly` sorunsuz) — from bootstrap 2026-11-07.
- `EnsureCreated` ve migration'lar karışınca şema ayrışır — uygulama `Migrate()` kullanır;
  migration eklendiğinde testler de `Migrate()` kullanır — from bootstrap 2026-11-07.
- Türkçe kültürde `ToUpper()`/`ToLower()` "i"yi "İ"/"ı" yapar — `OrdinalIgnoreCase` veya
  `…Invariant()` kullanın — from bootstrap 2026-11-07.
- `WebApplicationFactory` POST testleri antiforgery yüzünden 400 alır — önce GET ile formdan
  `__RequestVerificationToken` ve cookie'yi alın; testte antiforgery'yi kapatmak yasak —
  from bootstrap 2026-11-07.

## Enforced by tooling
<!-- Walking skeleton bunları kurar ve check'e bağlar; o zamana kadar planlanmış durumdadır. -->
- `Directory.Build.props`: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`,
  `<AnalysisLevel>latest-recommended</AnalysisLevel>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- `.editorconfig` + `dotnet format --verify-no-changes` (check adımı `format [fast]`).
- Katman sınırları ve saat yasağı: `LeaveManager.ArchitectureTests` (check adımı `test-unit [fast]`).
- Bilinen açıklı paketler: `dotnet list package --vulnerable` (check adımı `vulnerable`, full).
