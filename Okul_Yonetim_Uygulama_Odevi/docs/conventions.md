# Conventions

> Her kural ya araçla zorlanır (tercih) ya da review'da kontrol edilir. Bootstrap 2026-11-19.

## Language
- Chat language (interviews, sessions): Turkish (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Turkish (`tr`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers: **English** (`Student`, `Guardian`, `InstallmentPlan`); tablo/kolon adları snake_case İngilizce.
- `/// <summary>` açıklamaları: **Türkçe**.
- Commit messages: **Türkçe açıklama**; Conventional Commits tür kelimesi ve plan referansı İngilizce (protokol):
  `feat(okul/yoklama): toplu yoklama ucu eklendi [plan 0003/2]` (`docs/git.md`).
- Kullanıcıya görünen metinler: Türkçe; **koda gömülmez** — .NET'te nArchitecture YAML yerelleştirmesi
  (`Application/Features/<Modül>/Resources/Locales/<ad>.tr.yaml`, anahtar → metin; bölüm adı = özellik klasörü adı,
  ör. `SystemHealth`), web/mobilde `tr.json`. MVP tek dil; yeni dil = yeni kaynak dosyası (ADR-0005).
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- .NET SDK 10.0.4xx (`backend/global.json`), C# latest, `net10.0`; `NArchitecture.Core.*` 2.0.0; EF Core 10.0.15 +
  Npgsql EF 10.0.3; Serilog.AspNetCore 10.0.0; PostgreSQL 18.7. Test çalıştırıcı: Microsoft.Testing.Platform (xUnit v3).
- Node.js 26 (`.nvmrc`), npm workspaces (`web`, `mobile`, `packages/api-client`); TypeScript ~6.0 strict;
  React 19.3.0 (web ve mobil **aynı** sürüm — Expo SDK 58'in istediği); Vite 8; Expo SDK 58 / React Native 0.88;
  ESLint 10 (web ve mobil aynı kural seti).
- Paket sürümleri merkezi: `backend/Directory.Packages.props` (Central Package Management); npm tarafında
  `package-lock.json` commit edilir. Kesin sürümler spec 0001 planında (plan 0001, Risks 3).

## Naming
- nArchitecture düzeni: `Application/Features/<Modül>/Commands/<Eylem>/<Eylem><Varlık>Command.cs`,
  `...CommandHandler.cs`, `...CommandValidator.cs`; Query'ler aynı düzende; kurallar `Rules/<Varlık>BusinessRules.cs`.
- **K1 — dosya başına tek tip:** bir dosyada yalnızca 1 class *veya* 1 interface *veya* 1 enum *veya* 1 record/struct;
  dosya adı = tip adı. (StyleCop SA1402, SA1649 — error.)
- **K3 — tip başına summary:** her tipin (class/interface/enum/record/struct) başında kısa Türkçe
  `/// <summary>` — "bu ne işe yarar". Metot/property/alan için ayrı summary **yazılmaz**; önemli üyeler tipin
  summary'sinde anlatılır.
- Arayüzler `I` önekli; async metotlar `Async` sonekli; sınıflar varsayılan `sealed`.
- Test adı: `<Metot>_<Durum>_<Beklenen>` (ör. `Handle_StudentFromOtherTenant_ThrowsNotFound`).
- Kodda Türkçe terim karşılıkları `docs/domain.md` tablosundan (Kurum=`Tenant`, Şube=`Branch`, Sınıf=`Classroom`).

## Error handling
- nArchitecture global exception middleware; tüm hatalar **RFC 9457 ProblemDetails**.
- İş kuralı ihlali → `BusinessException` → 400 (Türkçe mesaj, YAML yerelleştirmesinden).
- Doğrulama → `ValidationException` (FluentValidation) → 400, alan bazlı liste.
- Yetki yok → `AuthorizationException` → 403. İstisna (spec 0004): giriş, kilit ve geçersiz/süresi dolmuş oturum da
  `AuthorizationException` ile atılır ama **401** döner (kimlik doğrulanamadı); seçili okul olmadan okul gerektiren
  uç 403 alır.
- Kayıt yok **veya başka kiracıya/yetkisiz şubeye ait** → **404** ("var ama sizin değil" bilgisi sızdırılmaz).
- Beklenmeyen → 500; yanıtta yalnızca genel mesaj + `traceId`. Stack trace, SQL, kişisel veri **asla** yanıtta olmaz.
- Loglarda kişisel veri maskelenir (`docs/security.md`).
- Beklenmeyen hata, global middleware'de **bir kez** Error seviyesinde `traceId` ile loglanır; aynı hata katman katman yeniden loglanmaz.

## Logging
Altyapı ve dosya düzeni: `docs/architecture.md` "Logging" (ADR-0003). Kodda:
- Logger `ILogger<T>` ile enjekte edilir; statik logger yok.
- **Mesaj şablonu** kullanılır, string birleştirme/interpolasyon değil: `logger.LogInformation("Öğrenci {StudentId} kaydedildi", id)`
  (CA2254 — uyarı = hata kuralıyla derlemeyi kırar).
- Seviyeler: **Debug** ayrıntı (varsayılan kapalı) · **Information** iş olayı (kayıt oluştu, yoklama alındı) ·
  **Warning** beklenen ama olağandışı (iş kuralı ihlali, dış servis yeniden deneme) · **Error** beklenmeyen hata ·
  **Critical** uygulama ayağa kalkamıyor (ör. veritabanı başlangıçta yok).
- Her Command handler en az bir Information iş olayı loglar; her `catch` ya yeniden fırlatır ya da Warning/Error loglar (sessiz yutma yok).
- Log alanlarında yalnızca **kimlikler** (StudentId, TenantId, UserId) — isim, TC, telefon, e-posta yazılmaz.
- Port (dış servis) hataları: zaman aşımı + yeniden deneme + "beklemede" durumu; istek çökmez.

## Data rules
1. **Para:** C# `decimal`, PostgreSQL `numeric(18,2)`, TRY. Para için `float`/`double` yasak. Taksit bölmede kuruş farkı son taksite (BR-7).
2. **Zaman:** DB'de UTC (`timestamptz`); gösterim `Europe/Istanbul`. Sadece-tarih alanları `DateOnly`/`date`. Saat `TimeProvider`'dan; testlerde `FakeTimeProvider`.
3. **ID:** UUID v7 (`Guid.CreateVersion7()`); ardışık int ID dışarı açılmaz.
4. **Kiracı alanları:** kiracıya ait her tabloda `tenant_id` (zorunlu), şubeye bağlıda `branch_id`; ikisi de **oturumdan** gelir, istemciden alınmaz.
5. **Silme:** iş kayıtları soft delete (`deleted_at`); saklama süresi dolan kişisel veri anonimleştirilir.
6. **Denetim alanları:** `created_at/by`, `updated_at/by`; hassas okuma/değişiklik ayrıca audit log'a.
7. **TC kimlik no:** 11 hane + algoritma doğrulaması; şifreli saklanır, arama için HMAC parmak izi; gösterimde maskeli (`*******1234`).
8. **Telefon:** E.164 (`+905xxxxxxxxx`).
9. **Metin karşılaştırma:** kullanıcı aramalarında `tr-TR` (ICU collation); teknik değerler (e-posta, kod) `ToLowerInvariant` / `OrdinalIgnoreCase`.
10. İçe aktarılan veriye (Excel/e-Okul) aynı doğrulamalar uygulanır.

## Known traps
- T1 — Türkçe I/ı: kültüre bağlı `ToUpper/ToLower/Compare` `tr-TR`'de "mail"→"MAİL" yapar — teknik değerlerde Invariant/Ordinal, kullanıcı metninde açık `tr-TR`; CA1304/CA1309/CA1311 error — from bootstrap 2026-11-19.
- T2 — Windows Türkçe locale (`Turkish_Türkiye.1254`) ASCII dışı olduğu için PostgreSQL `initdb` çöker — kümeyi `--locale C` + UTF8 kur, Türkçe sıralamayı kolon bazında ICU `tr-TR` ile yap — from bootstrap 2026-11-19 (yaşandı).
- T3 — Npgsql: `timestamptz`'e `Kind=Local/Unspecified` `DateTime` yazmak exception — `DateTimeOffset`/UTC ve `TimeProvider` kullan; istemciden gelen `DateTimeOffset` sorguya girmeden önce `ToUniversalTime()` (ör. "+03:00" ile gelen zaman Npgsql'de 500 verir) — from bootstrap 2026-11-19; ek: spec 0010, review round 2.
- T4 — EF Core `decimal` hassasiyeti yapılandırılmazsa beklenmeyen kolon tipi — tüm para alanlarına convention ile `HasPrecision(18, 2)` — from bootstrap 2026-11-19.
- T5 — `IgnoreQueryFilters()`, `FromSql` veya doğrudan `DbContext` kiracı filtresini deler (S1) — yalnızca izinli listede, gerekçe + review onayıyla — from bootstrap 2026-11-19.
- T6 — `dotnet build/test` MSBuild node'ları ve VBCSCompiler'ı arkada bırakır (residue) — `--disable-build-servers`, `-nodeReuse:false`, `DOTNET_CLI_USE_MSBUILD_SERVER=0` — from bootstrap 2026-11-19.
- T7 — Liste sorgularında N+1 / tracking maliyeti ("10 saniyede yoklama") — okumada `AsNoTracking()` + `Select` ile DTO projeksiyonu — from bootstrap 2026-11-19.
- T8 — ArchUnitNET `Types()` yüklenmemiş paketlerin (Npgsql, Serilog…) tiplerini içermez; onlara bakan "bağımlı olmamalı" kuralı sessizce boş geçer — dış paket kümelerini `Types(true)` ile kur ve her kümeye bir pozitif kontrol testi yaz — from spec 0001, review round 1 (H-1).
- T9 — nArchitecture `LoggingBehavior`, `ILoggableRequest` işaretli isteği bütünüyle (tüm alanlarıyla) JSON olarak loglar — kişisel veri (S2/S3: TC, telefon, şifre, sağlık…) taşıyan Command/Query'ye `ILoggableRequest` konmaz; gerekirse yalnızca kimlik numaralarını loglayan ayrı bir log satırı yazılır — from spec 0001, review round 1 (I-1).
- T10 — EF Core, yeni (boş) veritabanında ilk `Migrate()` sırasında henüz olmayan `__EFMigrationsHistory` tablosunu sorgulayıp `Microsoft.EntityFrameworkCore.Database.Command` kaynağından bir `[ERR]` satırı yazar; zararsızdır, yalnızca ilk kurulumda görülür — log testlerinde Error satırlarını kaynağa göre süz; üretimde ilk kurulumdaki bu satırı hata sanma — from spec 0001, fix-1 (L-1).
- T11 — Web'deki tema ön betiği (`web/index.html`, `theme-pre-paint`) sayfa çizilmeden çalışmak zorunda olduğu için `@okul/theme` mantığının düz JS kopyasıdır; biri değişip öteki unutulursa açılışta yanlış mod görünür — tercih mantığını değiştirirken ikisini birlikte değiştir; `prePaint.test.ts` ikisini aynı vaka tablosuyla sınar — from spec 0002, plan.
- T12 — Raspberry Pi'de PostgreSQL **17** çalışıyor (geliştirme 18): 18'e özgü bir özellik (SQL, uzantı, EF/Npgsql ayarı) kullanılırsa Pi'de hata çıkar — yeni veritabanı özelliği kullanmadan önce 17 desteğine bak; dağıtım sonrası sağlık kontrolü ve migration uygulaması bunu ilk yakalar — from spec 0003, kullanıcı kararı.
- T14 — EF Core: `bool` alanda `HasDefaultValue(true)` → `false` "girilmemiş" sayılır ve veritabanı varsayılanı (`true`) yazılır; pasif oluşturulan kayıt sessizce etkin kaydedilir — varsayılanı yalnızca migration'da ver ya da `HasSentinel` ayarla (`BranchConfiguration.IsActive` bugün hep `true` oluşturulduğu için zararsız) — from spec 0005, review I-2.
- T13 — Git Bash (MSYS) Windows programına (node, npx, dotnet) giden ortam değişkenlerinde `/` ile başlayan değeri yol sanıp çevirir: `VITE_API_BASE_URL=/` pakete `C:/Program Files/Git/` olarak gömülür, sayfa API'ye ulaşamaz — yol olmayan böyle bir değeri `MSYS2_ENV_CONV_EXCL=<AD>` ile ver; derlenmiş pakette **bilinen** yerel yolları (`X:/Program Files`, `X:/Users`, `X:/msys`, `X:/Windows`, `file:///`) arayan koruma koy (`tools/pi/deploy.sh`; Git başka bir klasöre kuruluysa koruma yakalamaz — asıl önlem çevirinin kapatılması, pi-config onu zorunlu tutar; genel `X:/` deseni `http://` yüzünden yanlış alarm verir); curl ile sağlık kontrolü bunu yakalamaz, tarayıcıda sayfanın kendi isteği gözlenmeli — from spec 0003, VERIFY V-1.
- T15 — React: aynı yerde koşullu çizilen ve `useState(initial)` kullanan form, düzenlenen kayda bağlı `key` almalı (ör. `key={editing.id ?? "new"}`); yoksa React aynı bileşeni yeniden kullanır, başka bir satırda "Düzenle"ye ya da düzenlerken "Ekle"ye basınca form ilk kaydın değerlerini tutar ve yanlış kayda yanlış değer gider — from spec 0008, review round 1 (M-1)
- T16 — React Native'de View kaydırmaz; uzunluğu sınırsız liste FlatList/ScrollView içinde olmalı (kart `scrollCard`, liste `scrollList` — `useAuthStyles`); başlık ve alttaki düğmeler listenin dışında sabit kalır. Jest yerleşimi sınamaz: bileşen testi yalnızca listenin kayan bir öğe (`RCTScrollView`) içinde olduğunu sınar — gerçek kaydırma cihaz/emülatör görüntüsüyle doğrulanır — from spec 0010, review round 1 (F-2).
- T17 — Web/mobil testlerde ertelenmiş yanıt (`fail`/`release`) sahte istek işleyicisinin içinde kurulursa, test istek gelmeden `fail()` çağırabilir: o an boş fonksiyon çalışır, yanıt hiç gelmez, test ara ara zaman aşımına düşer — testte ertelenmiş yanıtı istek gelmeden önce kur (`const failed = new Promise<Response>(…)`, işleyici onu döndürür); yükleniyor yazısı isteğin gittiğini kanıtlamaz — from spec 0010, review round 2.
- T18 — Kayıt süresi sınırı sunucuda katıysa istemci birkaç yüz ms önce durmalı; tik sayan sayaç gerçek kayıt süresini geçer (zamanlayıcı geç çalışır, kodlayıcı kaydı uzatır) — otomatik durmayı kaydedicinin kendi süresine bağla (`expo-audio` `getStatus().durationMillis` ≥ 59 500) — from spec 0012, review round 1 (M-1).
- T19 — nArchitecture `ExceptionMiddleware` tek (singleton) `HttpExceptionHandler` tutar ve her hatada onun `Response` özelliğini değiştirir: aynı anda iki iş hatasında bir yanıta iki ProblemDetails gövdesi, ötekine boş gövde yazılır (yanıtlar istekler arasında karışır) — `TraceIdExceptionMiddleware` beklenen hataları her çağrıda yeni `HttpExceptionHandler { Response = response }` ile yazar; temel `HandleExceptionAsync`'e geri dönülmez. Eşzamanlı istek testleri hata gövdesini de ayrıştırarak denetler — from spec 0012, fix-1 (`ConcurrentSends_SameDraft_OnlyOneMessage`).

## Enforced by tooling
Walking skeleton ile devreye girer (`backend/Directory.Build.props`, `backend/.editorconfig`, `tests/Architecture`):
- **K2:** `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `Nullable=enable`, `AnalysisLevel=latest-recommended` — tüm projeler.
- **K1:** StyleCop.Analyzers SA1402 (dosyada tek tip), SA1649 (dosya adı = tip adı) — error.
- **K3:** `GenerateDocumentationFile=true`; CS1591 ve SA1600 kapalı (üye summary'si istenmez); architecture testi her tipin XML belgesinde boş olmayan `<summary>` arar.
- T1: CA1304, CA1309, CA1311 — error. Log: CA2254 (şablon) error; CA1848 ve CA1873 kapalı (performans ipuçları, kapsamlı loglamayı zorlaştırır).
- Otomatik üretilen kod (migration'lar, OpenAPI istemcisi) `.editorconfig`'de `generated_code = true` ile K1/K3'ten muaf.
- Yasak bağımlılıklar 1–5: ArchUnitNET architecture testleri (`backend/tests/Architecture/LayerDependencyTests.cs`);
  7: ESLint `no-restricted-globals` / `no-restricted-imports` (fetch/axios yalnızca `@okul/api-client`'ta);
  ortak istemcinin güncelliği: `tools/check/api-client-fresh.mjs`.
- Web/mobil: `tsc --strict`, `eslint --max-warnings 0` (K2 karşılığı), dosya başına tek bileşen.
- Renkler: yalnızca `@okul/theme`; kodda renk değeri ESLint ile yasak (`#…`, `rgb(…)`, `hsl(…)`; adla yazılan renkler — `"white"`, `"red"` — lint'e takılmaz ama aynı kural gereği yazılmaz, review'da kontrol edilir); iki modda tüm renk çiftleri WCAG AA (`packages/theme/src/contrast.test.ts`, `theme-test` adımı). Yeni bir renk rolü kullanılınca `contrastPairs`'e eklenir.
- Paket güvenliği: `dotnet list package --vulnerable --include-transitive`, `npm audit --audit-level=high`.
