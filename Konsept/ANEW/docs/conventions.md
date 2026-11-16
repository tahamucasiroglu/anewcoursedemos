# Conventions

> Bootstrap sırasında 2026-11-14'te dolduruldu (kaynak: `Promtlar/0007`, `0009`–`0011`).
> Buradaki her kural ya araçla zorlanır (tercih edilen) ya da review'da kontrol edilir; temenniler buraya girmez.

## Language
<!-- Set at bootstrap (step 0) and mirrored in AGENTS.md. Changing it later is allowed — edit both
     places; existing documents are not translated retroactively. -->
- Chat language (interviews, sessions): Türkçe
- Document language (docs/, specs, plans, ADRs, review and verify reports): Türkçe
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: kod isimleri (class, metot, değişken) ve branch
  adları **İngilizce** (branch şu an kullanılmıyor, bkz. `docs/git.md`); `/// <summary>` açıklamaları
  ve kod yorumları **Türkçe**; commit mesajları **Türkçe**, başında İngilizce tür öneki
  (`feat:`, `fix:`, `docs:`, `chore:`…) — örnek: `feat(concepts): konu önerisi üretimi [plan 0003/2]`.
- Arayüz dili: yalnızca Türkçe; tüm arayüz metinleri resx kaynak dosyalarında tutulur (ileride çoklu
  dil eklenebilsin diye). Video dili ayrı bir konudur, profil başına seçilir.
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
| Katman | Sürüm / seçim | Not |
|---|---|---|
| Dil / runtime | C# · **.NET 10 LTS** (10.0.x) | .NET 11 (STS) bilinçli olarak alınmadı; destek süresi aynı (Kasım 2028). |
| Web | **ASP.NET Core MVC** (Razor), HTML, CSS, JS | `Konsept.Web` (BFF); DB'ye dokunmaz. |
| Tarayıcı istekleri | **jQuery 3.7.1 `$.ajax`** — `fetch` yasak (K-6) | `src/Konsept.Web/wwwroot/lib/jquery/` (npm `jquery@3.7.1`'den, lisansıyla); tek sarmalayıcı `wwwroot/js/api.js` (plan 0001). |
| API | **ASP.NET Core Web API** | `Konsept.Api`, modüler monolit. |
| Masaüstü ajanı | **Avalonia 12.x**, ilk hedef **Windows** | Linux/macOS kapsam dışı (şimdilik). |
| Ajan ↔ API | **SignalR** (.NET istemcisi, yalnızca ajan tarafında) | Bağlantıyı ajan açar. |
| Tarayıcıda canlı akış | **Ajax polling** (2–3 sn) | SignalR tarayıcı istemcisi `fetch` kullandığı için yok. |
| Veritabanı | **PostgreSQL 17** + **EF Core 10** (Npgsql) | Pi'de kurulu PostgreSQL 17 kullanılır (spec 0001 kararı D1, 2026-11-14); ayrı `anewconcept` veritabanı ve kullanıcısı. Tek DB, modül başına şema + `DbContext`. |
| Doğrulama | **FluentValidation** | Generic validation filter ile (K-4). |
| Loglama | **Serilog** (dosya + DB sink) | Ortak kurulum `Konsept.Shared.Logging` (0008): kaynak (Web/Api/Agent), UTC zaman, korelasyon, maskeleme. Dosya `<klasör>/<önek>-yyyyMMdd.log`; yerel saklama 7 gün (tarihe göre `LogFileJanitor`; dosya adı yerel güne, temizlik UTC güne göre — dosya en fazla bir gün uzun tutulur, erken silinmez); Pi: `/opt/anewconcept/logs/{api,web}`, ajan: kendi klasörü altında `logs`. Veritabanında (`monitoring.technical_logs`) Api/Web Warning+ ve ajan Error/Critical özeti, 30 gün. Gizli bilgi tek mekanizma `SecretMasker` (Shared.Kernel) ile maskelenir; serbest metinde `anahtar=değer` / `anahtar: değer` çiftleri alan tablosuyla aynı kararla maskelenir; yeni gizli alan = `SecretMaskerTests` ve `SecretFieldSamples` tablolarına satır. Üretimde konsol kapalı (`Logging__Console=false`; Pi'de journald kalıcı). Host logger'ı statik `Log.Logger`'a bağlanmaz (`preserveStaticLogger: true`). |
| Sunucu | **Raspberry Pi 5**, `linux-arm64` | |
| Kullanıcı PC araçları | **Node.js LTS + FFmpeg + headless Chromium**; izole **Python** ortamı yalnızca isteğe bağlı "Ses klonlama" bileşeninde (Chatterbox, 0009) | Ajan kendi klasörüne, çakışmasız kurar. Ollama kurulmaz (E-4, 0009). |

- Tüm NuGet versiyonları sabittir ve merkezi olarak `Directory.Packages.props` içinde tutulur.
- Yeni paket = plan aşamasında gerekçesiyle öneri + insan onayı; lisans/bakım/açık kontrolü
  `docs/security.md` → Dependencies bölümüne göre.
- Video motoru, yerel model çalıştırma yöntemi ve render sandbox yöntemi Karar 5
  (`raporlar/b5-video-pipeline-arastirmasi`) ile belirlendi; uygulaması 0009, 0017 ve 0018'de.
  Temel stack bu karardan etkilenmez.

## Naming
- Interface `I` önekiyle, abstract base sınıf `Base` sonekiyle: `ITtsEngine` → `TtsEngineBase` →
  `SupertonicTtsEngine`.
- Validator `…Validator`; DTO `…Request` / `…Response`; async metot `…Async` ve **her async metot
  `CancellationToken` alır**.
- Klasörler: `Abstract/` · `Base/` · `Concrete/` · `Enums/` · `Dtos/` · `Validators/`.
- Dosya adı = içindeki tek tipin adı (K-1).
- Projeler: `Konsept.Modules.<Module>.<Layer>` (Layer: `Domain` / `Application` / `Infrastructure` /
  `Presentation` / `Contracts`); proje sayısında sabit kural yok, gerektiği kadar proje
  (bkz. `docs/architecture.md`).
- Test projeleri `tests/` altında (`Konsept.Modules.Concepts.Tests`, `Konsept.ArchitectureTests`);
  test adları `Method_State_Expected` (bkz. `docs/testing.md`).
- Domain terimlerinin kod adları `docs/domain.md` terim tablosundan alınır (ör. `Concept`,
  `TopicSuggestion`, `ProductionJob`).
- Versiyon etiketleri `anew-konsept/vX.Y.Z` (bkz. `docs/git.md`).

### Kodlama kuralları (K-1…K-7)
| # | Kural | Zorlama |
|---|---|---|
| K-1 | **Bir dosyada tek nesne** (class, interface, record, enum); iç içe tip yasak; dosya adı = tip adı | StyleCop **SA1402 + SA1649 hata** + iç içe tip için architecture test |
| K-2 | **Çıplak class yok:** davranış taşıyan her tip (servis, motor, sağlayıcı, repository) `I…` (`Abstract/`) → `…Base` (`Base/`) → somut sınıf (`Concrete/`) zinciriyle kurulur | Architecture test: `Concrete/` altındaki her sınıf bir `Base`'den türer ve bir `Abstract/` interface'ini uygular |
| K-2 istisnaları | Davranış taşımayan tipler: DTO/record, entity, enum, EF konfigürasyonları, migration'lar, `Program.cs`, testler. Controller'lar (`ControllerBase`) ve validator'lar (`AbstractValidator`) zaten bir base'den türer. Çerçeve adaptörleri: çerçevenin kendi sözleşmesini uygulayan ince sınıflar — middleware, MVC filter (`IAsyncActionFilter`), SignalR hub, `IRetryPolicy`, `ICommand`, `IDesignTimeDbContextFactory`; mantık taşımazlar, mantık bir servise gider. | Architecture testteki istisna listesi (davranış adı taşımayan sınıflar `Concrete/` dışında kalabilir) |
| K-3 | **Her tipin başında summary** (amacı, önemli üyeleri); her public üyede summary | Derleyici **CS1591 hata** |
| K-4 | **Controller tek satır:** doğrulama generic validation filter ile otomatik; gövde yalnızca `return await _service.X(request).ToActionResult();` | Architecture test (Y-6) + review |
| K-5 | **DRY:** aynı mantık birden fazla yerdeyse generic metoda ya da base sınıfa toplanır | Review |
| K-6 | **`fetch` yasak**; tüm istekler tek bir `api.js` sarmalayıcısı üzerinden `$.ajax` ile | `scripts/check`: Web JS dosyalarında `fetch(` araması → bulunursa kırmızı |
| K-7 | **Tema:** renkler yalnızca CSS değişkenleri (`--color-…`); açık/koyu tema `data-theme` ile; inline stil yasak | `scripts/check`: `style="` araması + review |

## Error handling
- Application servisleri beklenen iş hatalarında exception fırlatmaz; **`Result<T>`** döndürür
  (başarı ya da hata kodu + mesaj). Controller bunu `ToActionResult()` ile HTTP yanıtına çevirir.
- Beklenmeyen hatalar global middleware'e düşer; kullanıcıya **ProblemDetails** ile Türkçe, sade bir
  mesaj ve bir **korelasyon numarası** döner, ayrıntı Serilog'a yazılır.
- **Kullanıcıya asla gösterilmez:** stack trace, SQL, API key, iç sistem ayrıntısı.
- LLM çağrılarında yeniden deneme sınırlıdır (H-3): yalnızca **429 ve 5xx**'te, **en fazla 2 kez**,
  artan beklemeyle; diğer 4xx'te hiç tekrar yok.
- Kelime denetimi kaynaklı otomatik yeniden üretim en fazla 1 kezdir (BR-15); yine aşılırsa konsept
  uyarıyla Üretilenler'e düşer.

## Data rules
- **Para:** maliyet `decimal(18,6)` **USD** olarak saklanır (tek çağrı bir sentin kesri olabilir);
  ekranda 2 hane gösterilir (`$2,37`).
- **Zaman:** veritabanında **UTC** (`timestamptz`), ekranda kullanıcının yerel saati. Kod
  `DateTime.Now` yerine `TimeProvider` kullanır.
- **ID'ler:** **UUIDv7** (`Guid.CreateVersion7()`), sıralanabilir.
- **Silme:** Silinenler için soft delete (`DeletedAt`); Silinenler'den silmek kalıcı silmedir,
  dosyalarıyla birlikte gider (BR-7).
- **Sahiplik:** her kayıt bir kullanıcıya aittir (`UserId`); EF global query filter ile otomatik
  filtrelenir (BR-1).
- **Şemalar:** her modülün kendi şeması ve `DbContext`'i; şemalar arası FK ya da join yok (Y-4).
- **Gizli bilgi:** API key'ler ASP.NET Data Protection ile şifreli; loglarda maskeli (`sk-…a3f`);
  `appsettings*.json` dosyalarında gizli bilgi olmaz.
- **Token limitleri:** her modelin çıktı token limiti model kataloğunda tutulur; kodda sabit değer
  olmaz.
- **Harcama:** her online model çağrısının token sayısı ve maliyeti kaydedilir (BR-12); gösterim
  yalnızca bilgi amaçlıdır, limit ya da engelleme yok.

## Versioning
- **Tek ürün versiyonu** — Web, API ve Ajan için ortak — `Directory.Build.props` içinde; `0.1.0`'dan
  başlar.
- Küçük düzeltme → **patch** (`0.1.20 → 0.1.21`). Sayfa ekleme ya da kapsamlı değişiklik → **minor**
  (idareli kullanılır). `1.0.0` = tam testli, uçtan uca çalışan ürün.
- Pi'ye **her gönderimden önce** versiyon artırılır ve `anew-konsept/vX.Y.Z` etiketi atılır
  (`docs/git.md`). Önemsiz değişiklik Pi'ye gidecekse yine patch artırılır.

## Known traps
<!-- Starts empty. One line per trap of this stack that a builder can know up front:
     "<trap> — <what to do instead> — from <spec/finding>". A line is added when triage marks a
     finding real and the reviewer says a build-time rule would have caught it; the human
     approves it at triage as Amendment A-n and the Developer writes it in the fix round.
     Builders read this section before the first plan step.
     A trap that tooling can catch moves to "Enforced by tooling" instead. -->
- Türkçe I/İ: `ToUpper()`/`ToLower()` tr-TR kültüründe "i"yi "İ"ye çevirip anahtar karşılaştırmalarını bozar — anahtarlarda `ToUpperInvariant()`/`ToLowerInvariant()` ve `StringComparison.Ordinal` kullan (testler tr-TR kültürüyle koşar, yakalar) — from bootstrap 2026-11-14
- Npgsql + `DateTime`: `timestamptz` alanına `Kind=Utc` olmayan değer yazmak hata verir — her zaman `TimeProvider` üzerinden UTC (`DateTime.UtcNow` karşılığı) ya da `DateTimeOffset` kullan — from bootstrap 2026-11-14
- Avalonia tray: pencere kapanınca uygulama da kapanır — `ShutdownMode=OnExplicitShutdown` kullan, çıkış yalnızca tray menüsünden — from bootstrap 2026-11-14
- SignalR bağlantı kopması: otomatik yeniden bağlanan ajan sunucuda kayıtsız kalır — ajan yeniden bağlanınca kendini yeniden kaydeder ve yarım kalan işini bildirir — from bootstrap 2026-11-14
- SignalR hub mesajı: varsayılan `MaximumReceiveMessageSize` 32 KB; aşan mesaj bağlantıyı kapatır — büyük yük taşıyan hub metodunda istemci yükü sınırlansın ve sınır bilinçli ayarlansın — from 0022 review r1 H-1
- Pi'ye yayın: x64 çıktısı Pi'de çalışmaz — `dotnet publish -r linux-arm64` ile yayınla — from bootstrap 2026-11-14
- Pi'de `/opt/anewconcept/*` klasörleri `750 root:anewconcept`: deploy betiğindeki uzak dosya kontrolleri SSH kullanıcısıyla izin hatası verip sessizce "yok" der — uzak kontrolleri `sudo -n test …` ile yap — from 0009 review r1 H-1

## Enforced by tooling
<!-- List what the compiler/linter/analyzers already enforce, so review doesn't re-litigate it.
Wire new rules into `scripts/check` whenever possible — prose is advice, tooling is law. -->
Aşağıdaki kurallar walking skeleton'da (plan 0001) `scripts/check.conf` adımlarıyla etkinleşti; review'da
yeniden tartışılmaz.

| Kural | Araç |
|---|---|
| Nullable açık, uyarılar hata | `<Nullable>enable</Nullable>` + `TreatWarningsAsErrors` (`Directory.Build.props`) |
| K-1 tek tip / dosya adı | StyleCop SA1402 + SA1649 hata; iç içe tip → architecture test |
| K-3 summary zorunlu | CS1591 hata |
| Biçim | `dotnet format --verify-no-changes` |
| Y-1…Y-8, K-1 (iç içe tip), K-2, K-4 | ArchUnitNET testleri (`Konsept.ArchitectureTests`) |
| K-6 `fetch` yasak | check'te Web JS dosyalarında `fetch(` araması |
| K-7 inline stil yasak | check'te `style="` araması |
| Bilinen açıklı paket yok | `dotnet list package --vulnerable` + `npm audit` (check içinde) |
| Merkezi paket versiyonu | `Directory.Packages.props` (Central Package Management) |
