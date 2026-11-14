# Conventions

> Yalnız gerçek kurallar: her kural ya araçla zorlanır (tercih) ya da review'da denetlenir.

## Language
- Chat language (interviews, sessions): Türkçe (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Türkçe (`tr`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers: İngilizce (domain tablosundaki karşılıklar, `docs/domain.md`). Kullanıcıya görünen metinler: Türkçe.
  Commit mesajları: Türkçe açıklama, Conventional Commits tipi/kapsamı İngilizce (`docs/git.md`). Branch yok (`docs/git.md`).
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- .NET SDK en az 10.0.400 (`global.json`: `"version": "10.0.400", "rollForward": "latestFeature"`), C# 14, `net10.0`.
- ASP.NET Core 10 · EF Core 10 (`Microsoft.EntityFrameworkCore.Sqlite`) · xUnit v3 · NetArchTest.
- Node 22+ (makinede 26), npm; React 19 · Vite · TypeScript (strict) · ESLint + Prettier · Vitest (yalnız gerektiğinde).
- NuGet sürümleri Central Package Management ile `backend/Directory.Packages.props`'ta; npm sürümleri `package-lock.json`'da.

## Naming
- C#: standart .NET adlandırması (`.editorconfig` ile): tipler/metotlar `PascalCase`, yerel/parametre `camelCase`, private alan `_camelCase`, arayüz `I` önekli.
- Proje adları `LeaveManager.<Katman>`; test projeleri `LeaveManager.<Katman>.Tests`.
- Test adları: `<Metot>_<Durum>_<Beklenen>` ve kural testi adında BR numarası geçer (ör. `Create_WhenBalanceInsufficient_FailsWithBR2`).
- TS: bileşen dosyaları/adları `PascalCase.tsx`, diğer her şey `camelCase`; feature klasörleri `kebab-case`.

## Error handling
- Domain kural ihlali istisna fırlatmaz: `Result` / `Result<T>` döner; hata kodu BR numarasını taşır (`BR-2`) ve Türkçe mesaj içerir.
- Beklenmeyen durumlar istisnadır; global işleyici yakalar, loglar.
- API her hatayı **ProblemDetails** (RFC 9457) olarak döner: kural ihlali → 422, doğrulama → 400, kimlik yok → 401, yetki → 403, bulunamadı → 404. Yanıta stack trace, SQL, iç tip adı girmez (`docs/security.md`).
- Frontend API hatasını ProblemDetails `title`/`detail` üzerinden kullanıcıya Türkçe gösterir.

## Data rules
- İzin tarih aralığı `DateOnly`; zaman damgaları UTC `DateTimeOffset`.
- "Şimdi/bugün" yalnız `TimeProvider` üzerinden okunur; `DateTime.Now/UtcNow/Today`, `DateTimeOffset.Now/UtcNow` yasak (BR-7 test edilebilirliği).
- Gün sayıları `int`; para birimi yok. Kimlikler `Guid`.
- Nullable reference types açık; `null` yerine `Result`/opsiyonel dönüş tercih edilir.

## Known traps
- SQLite `DateTimeOffset` alanlarında sıralama/karşılaştırmayı sunucu tarafında yapamaz — EF Core değer dönüştürücüsü (ör. UTC ticks/`long`) kullan — from bootstrap 2026-11-14.
- EF Core InMemory sağlayıcısı ilişkisel davranışı taklit etmez — kullanma; kalıcılık gerekiyorsa SQLite kullan — from bootstrap 2026-11-14.

## Enforced by tooling
> Greenfield: aşağıdakiler walking skeleton spec'iyle (ilk spec) kurulur ve `scripts/check.conf` adımları o zaman açılır.
- Derleyici: `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest` (`backend/Directory.Build.props`).
  NuGet audit: `NuGetAuditLevel=high` ve `NU1900` `WarningsNotAsErrors`'ta — düşük/orta bulgu ya da çevrimdışılık derlemeyi kırmaz; açık denetimi `backend-vulnerable` adımındadır (`docs/security.md`).
- `dotnet format --verify-no-changes` (biçim + `.editorconfig` stil kuralları).
- Mimari testleri: `docs/architecture.md` yasak bağımlılık 1–4. ESLint: yasak bağımlılık 5 ve `react/no-danger` (`dangerouslySetInnerHTML` yasak).
- TypeScript `strict`; `typecheck` script'i `tsc -b` (Vite şablonunun kök tsconfig'i yalnız referans içerir, `tsc --noEmit` hiçbir dosyayı denetlemez). `lint` script'i `eslint . && prettier --check .`.
- Banned API: `DateTime.Now/UtcNow/Today`, `DateTimeOffset.Now/UtcNow` (`Microsoft.CodeAnalysis.BannedApiAnalyzers`, `BannedSymbols.txt` `AdditionalFiles` olarak `Directory.Build.props`'ta).
