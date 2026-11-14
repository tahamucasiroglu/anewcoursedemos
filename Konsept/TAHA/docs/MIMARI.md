# Taha Konsept — Mimari Sözleşmesi (tüm kod buna uyar)

> Bu dosya kodun **bağlayıcı** teknik sözleşmesidir. Kurallar: `memory/kurallar/*.md`. Ürün: `memory/urun-tanimi.md`. Plan: `PLAN.md`.
> Kısaca zorunlular: **dosya başına tek tür** · **interface → abstract base → concrete** · her türde `/// <summary>` · controller'da tek satır iş · tarayıcıda **jQuery Ajax (fetch YASAK)** · tarayıcı → MVC → API.

## 1. Araçlar / sürümler
- .NET SDK 10.0.4xx, `net10.0`, C# latest. Çözüm dosyası: `TahaKonsept.slnx` (TAHA kökünde).
- `Directory.Build.props` (kökte): `Nullable=enable`, `ImplicitUsings=enable`, `GenerateDocumentationFile=true`, **`CS1591` hata** (public türlerde summary zorunlu), **`TreatWarningsAsErrors=true`** (tüm projeler; kullanıcı kuralı 14 Kas 2026 — uyarı bastırmak yerine düzelt), `LangVersion=latest`, `Version` = `VERSION` dosyasından okunur.
- `Directory.Packages.props`: merkezi paket sürümleri (CPM). Paket eklerken sadece buraya sürüm, projede sürümsüz `PackageReference`.
- Paketler: EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL 10, ASP.NET Core Identity (EF), `Microsoft.AspNetCore.Authentication.JwtBearer`, FluentValidation 12, Serilog.AspNetCore (+ File, Console), `Microsoft.AspNetCore.SignalR.Client` (ajan), Avalonia 11.3.x (son kararlı) + `Avalonia.Desktop` + `Avalonia.Themes.Fluent`, Velopack, `Microsoft.ML.OnnxRuntime` (Supertonic), xUnit v3 / xunit + FluentAssertions yerine **Shouldly** (lisanssız), NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules`. **MediatR, AutoMapper, FluentAssertions 8 KULLANILMAZ** (ticari lisans).
- İstemci kütüphaneleri (wwwroot/lib, yerel kopya, CDN yok): jQuery 3.7.1, Chart.js 4.x. Başka JS framework'ü yok.

## 2. Dizin yapısı
```
TAHA/
  TahaKonsept.slnx  Directory.Build.props  Directory.Packages.props  VERSION  global.json
  src/
    BuildingBlocks/
      TahaKonsept.BuildingBlocks.Domain/          # Entity/AggregateRoot base, IAuditable, Result<T>, Error
      TahaKonsept.BuildingBlocks.Application/     # IService işaretleri, ICurrentUser, IClock, IUnitOfWork, sayfalama, validasyon
      TahaKonsept.BuildingBlocks.Infrastructure/  # ModuleDbContextBase, Repository<T>, AES-GCM ISecretProtector, Serilog kurulum, generic HttpClient base
      TahaKonsept.BuildingBlocks.Api/             # ApiControllerBase (Result→IActionResult), ValidationFilter<T>, IModule (kayıt), hata middleware
      TahaKonsept.Shared.Contracts/               # API ↔ Web ↔ Ajan ortak DTO'lar, SignalR mesajları, enum'lar (her biri ayrı dosya)
    Modules/<Modül>/
      TahaKonsept.Modules.<Modül>.Domain/
      TahaKonsept.Modules.<Modül>.Application/    # Abstract/ (I...Service), Base/, Concrete/ (servisler), Validators/
      TahaKonsept.Modules.<Modül>.Infrastructure/ # Persistence/ (<Modül>DbContext, Configurations/, Migrations/), Providers/...
      TahaKonsept.Modules.<Modül>.Api/            # Controllers/, <Modül>Module.cs (IModule)
    Hosts/
      TahaKonsept.Api/      # tüm modüller + SignalR hub + BackgroundService'ler. Port 7111
      TahaKonsept.Web/      # MVC, Areas/Admin, wwwroot. Port 7110
    Desktop/
      TahaKonsept.Agent.Core/    # pipeline yürütücüleri (motorlar), kurulum/ön gereksinim, yerel HTTP video sunucusu — UI'sız, test edilebilir
      TahaKonsept.Agent/         # Avalonia: tray, kurulum penceresi, eşleme penceresi
  tests/
    TahaKonsept.ArchitectureTests/   # kurallar: dosya başına tek tür, summary, katman bağımlılıkları, controller inceliği, fetch yasağı (wwwroot JS taraması)
    TahaKonsept.Modules.<Modül>.Tests/
    TahaKonsept.Agent.Core.Tests/
    TahaKonsept.IntegrationTests/    # WebApplicationFactory (InMemory/SQLite yerine gerçek PG varsa onu kullanır; yoksa skip)
  deploy/pi/   # install.sh, systemd unit'leri, publish betiği (PowerShell: deploy/publish-pi.ps1)
  scripts/     # geliştirici betikleri
```
Modüller: `Identity`, `Profiles`, `Providers`, `Concepts`, `Production`, `Agents`, `Diagnostics`.
Bağımlılık yönü: Domain ← Application ← Infrastructure ← Api ← Host. Modüller birbirinin **Domain/Infrastructure**'ına referans vermez; modüller arası iletişim yalnızca diğer modülün `Application/Abstract` içindeki **public servis interface'i** (ör. `IProviderGateway`) veya `Shared.Contracts` üzerinden. (Application → başka modülün Application projesine referans serbest, sadece interface kullanılır.)

## 3. Kod kalıpları
- **Klasör kalıbı** her katmanda: `Abstract/` (interface), `Base/` (abstract class), `Concrete/` (somut). Entity'ler `Domain/Entities/`, enum'lar `Domain/Enums/`.
- Dosya adı = tür adı. İç içe tür yok. `record` da tür sayılır (DTO başına dosya).
- Her türde summary: ne yapar + önemli üyeler. Public üyelerde de `<summary>`.
- `Result` / `Result<T>` (BuildingBlocks.Domain): `IsSuccess`, `Value`, `Error(Code, Message, Type)`; `ErrorType`: Validation, NotFound, Conflict, Unauthorized, Forbidden, Failure. Servisler istisna fırlatmak yerine `Result` döner.
- Controller (API): `ApiControllerBase` türevi; aksiyon tek satır: `=> ToActionResult(await _service.XAsync(dto, ct));`. Validasyon `ValidationFilter` ile otomatik (FluentValidation, `IValidator<T>` DI'dan). Controller'da başka mantık yok.
- Controller (Web/MVC): `WebControllerBase`; Ajax uçları `[HttpPost] public Task<IActionResult> X([FromBody] XRequest r, CancellationToken ct) => ProxyAsync(c => c.PostAsync<XRequest, XResponse>("api/...", r, ct));` (tek satır). Sayfa aksiyonları sadece `View()`.
- Persistence: her modülün kendi `DbContext`'i (`ModuleDbContextBase`'den), **kendi şeması** (snake_case: `identity`, `profiles`, `providers`, `concepts`, `production`, `agents`, `diagnostics`), migration geçmiş tablosu kendi şemasında. EF migration'ları her modülün `Infrastructure/Persistence/Migrations` altında; host başlarken sırayla `MigrateAsync`. Design-time factory her Infrastructure projesinde (`<Modül>DesignTimeDbContextFactory`, bağlantı `TAHAKONSEPT_DB` env ya da `Host=localhost;Port=5432;Database=tahakonsept;Username=tahakonsept;Password=dev`).
- Tüm entity'ler `UserId` taşır (tek kullanıcı da olsa, gelecekte çok kullanıcı için) — Identity hariç.
- Zaman: `IClock` (UTC). Kimlik: `Guid` (v7: `Guid.CreateVersion7()`).
- Gizli değerler (API key'ler): `ISecretProtector` (AES-256-GCM, anahtar `TAHAKONSEPT_MASTER_KEY` env/base64 ya da `App_Data/keys/master.key` dosyası, ilk açılışta üretilir). Veritabanında sadece şifreli + son 4 hane maskesi.
- Loglama: Serilog; dosya `logs/<app>-.log`, günlük rolling, **7 gün** saklama; seviyeler Information/Warning/Error/Critical (Fatal). Kullanıcıya görünen işlem günlüğü ayrıca DB'de (`diagnostics.activity_logs`).
- Türkçe: kültür `tr-TR`; HTML `lang="tr"`; `ToUpper` yerine `ToUpper(CultureInfo("tr-TR"))`.

## 4. Kimlik doğrulama
- Tek kullanıcı modu: kayıt kapalı. Veritabanında hiç kullanıcı yoksa Web `/Kurulum` sayfası tek admin hesabını oluşturur (kullanıcı adı + parola). Sonra kapanır.
- API: ASP.NET Core Identity (PBKDF2), JWT erişim token'ı 15 dk + dönen (rotating) refresh token (DB'de hash, `TokenFamilyId`). Roller/izinler: `Permission` claim'leri (`models.use`, `publish.use`, `admin.access`…) policy ile; admin hepsine sahip.
- Web: cookie auth (BFF). Cookie içinde (Data Protection ile şifreli) API access + refresh token tutulur; `ApiClient` 401'de refresh dener. Web logout → API `POST api/identity/logout-all` → **tüm refresh token'lar (ajan dahil) iptal** + ajan hub'dan düşürülür.
- Ajan eşleme (RFC 8628 benzeri): ajan `POST api/agents/device/start` → `{deviceCode, userCode(8 hane XXXX-XXXX), verificationUrl, interval}`; kullanıcı web'de `Ayarlar → Masaüstü Programı` sayfasında kodu girer/onaylar; ajan `POST api/agents/device/token` poll eder → access + refresh (ajan için ayrı aile). Token'lar ajanda DPAPI (CurrentUser) ile şifreli saklanır.

## 5. Web ↔ API ↔ Ajan iletişimi
- Tarayıcı → MVC: jQuery `$.ajax` (ortak sarmalayıcı `wwwroot/js/core/api.js` → `TK.api.post(url, data)`; antiforgery header otomatik). **fetch, axios, EventSource, SignalR JS YOK.** Canlı durum: `TK.poll(url, ms)` ile polling (varsayılan 2 sn).
- MVC → API: `IApiClient` (generic `GetAsync<T>`, `PostAsync<TReq,TRes>`, `PutAsync`, `DeleteAsync`), base URL `Api:BaseUrl` (Pi'de `http://127.0.0.1:7111`).
- Ajan → API: HTTP (`api/agents/...`) + SignalR hub `/hubs/agent` (JWT `access_token` query). API dışarı açık port 7111 (yerel ağ) — ajan bağlanabilsin.
- Hub sözleşmesi (`Shared.Contracts/Agent/`):
  - Sunucu → ajan (`IAgentClient`): `ExecuteStage(StageCommand)`, `CancelJob(Guid jobId)`, `PauseJob(Guid jobId)`, `OpenFolder(Guid videoId)`, `OpenInSystemPlayer(Guid videoId)`, `DeleteVideo(Guid videoId)`, `PreviewSpeech(SpeechPreviewCommand)`, `RefreshCapabilities()`.
  - Ajan → sunucu (`IAgentHub`): `Hello(AgentHello)` (sürüm, makine adı, LAN IP, video sunucu portu, stream anahtarı, GPU/VRAM, kurulu motorlar/modeller), `StageProgress(StageProgressReport)` (jobId, stage, yüzde, mesaj), `StageCompleted(StageResultReport)` (çıktı artefaktları, süre, maliyet 0), `StageFailed(StageFailureReport)`, `Log(AgentLogEntry)`, `Heartbeat()`.
- Artefaktlar: metin/JSON artefaktlar DB'de (`production.artifacts`); online üretilen ikili dosyalar (online TTS ses, online görsel) Pi'de `App_Data/artifacts/<jobId>/` altında geçici; ajan `GET api/agents/artifacts/{id}` ile indirir, iş bitince silinir. Yerel üretilen ikililer (render, yerel TTS/görsel) **sadece ajanda** (`%LocalAppData%\TahaKonsept\Workspace\<jobId>\`), Pi'ye sadece metadata gider.
- Video oynatma: ajan yerel HTTP sunucusu (Kestrel, varsayılan port **7120**, LAN'a açık) `GET /videos/{videoId}?k=<streamKey>` (Range destekli), `GET /thumbs/{videoId}?k=`. Web, ajan `Hello`'dan gelen IP/port/anahtar ile `<video src>` kurar.

## 6. Pipeline
Aşamalar (`Shared.Contracts/Production/PipelineStage` enum): `Script, FactSheet, ScenePlan, SceneCode, Images, Normalize, Speech, Align, SceneRender, Compose, Mux`.
- Yer: `Script, FactSheet, ScenePlan, SceneCode, Normalize` → **API** (online LLM). `Images`, `Speech` → sağlayıcı yerelse **ajan**, online ise **API** (sonucu ajan indirir). `Align, SceneRender, Compose, Mux` → **ajan**.
- Her aşama çıktısı `InputHash` (girdiler + ayarlar SHA256) ile saklanır; "Yeni sürüm"de hash'i değişmeyen aşama atlanır (artefakt yeniden kullanılır).
- Orkestratör: API'de `ProductionOrchestrator` (BackgroundService) — kuyruğu sırayla işler, durdur/devam/iptal, aşama/sahne kontrol noktası.
- Konsept modeli (JSON, `Shared.Contracts/Concepts/ConceptDocument`): `title, summary, language, format(Vertical|Horizontal), targetDurationSec, tone, audience, narrativeStyle, scenes[] { index, title, onScreen (ekranda gösterilecek açıklama), visualPrompt, narration (yazılış), durationHintSec }, sources[] {title,url}, pronunciations[] {written, spoken}, tags[]`.
- Okunuş (Normalize): `ITextNormalizer` zinciri (Sözlük → Sayı → Tarih → Romen/Sıra → Kısaltma); `SpokenText` + yazılış↔okunuş kelime eşlemesi.
- Altyazı: ASS karaoke (`\k`), ilerleme çubuğu: ffmpeg `drawbox`. Seçenekler profil/iş ayarından gerçekten uygulanır.

## 7. Sağlayıcı katmanı (Providers modülü + Agent.Core)
- Sunucu (online): `IAiModel` (PlatformName, ModelId, DisplayName, Capabilities) → `ITextModel : IAiModel` (`RunAsync<T>(TextRequest, ct)` structured JSON + `RunTextAsync`) · `IImageModel` · `ISpeechModel`. Base: `TextModelBase` (ortak: retry, usage → `CallLog`, maliyet), `OpenAiCompatibleTextModelBase` (OpenAI, OpenRouter, GLM, Kimi), `AnthropicTextModel` (Messages API, prompt caching, effort, streaming, web_search tool opsiyonel).
- Model kataloğu (`ModelCatalog`, seed JSON `providers/catalog.json`): her model için platform, id, görünen ad, yetenek, fiyatlar (in/out/cache/birim), max output, efor seviyeleri, dil etiketi ("Sadece Türkçe"), VRAM ihtiyacı (yerel), çalışma yeri (Server/Agent).
- Ajan (yerel): `ISpeechEngine` → `SpeechEngineBase` → `SupertonicSpeechEngine` (ONNX, C#), `PythonSpeechEngineBase` → (Chatterbox, XTTS, F5, EMA, Ahenk, MMS, Piper, OmniVoice, VoxCPM2, Fish, Kokoro, Trendyol) — gömülü Python + `uv` venv + `python-workers/tts_worker.py` (JSON stdin/stdout protokolü). `IImageEngine` → `StableDiffusionCppImageEngine`, `ComfyUiImageEngine`. `IVideoEngine` → `NodeVideoEngineBase` → `HyperFramesVideoEngine`, `RemotionVideoEngine`, `RevideoVideoEngine`. `IComposer` → `FfmpegComposer`. `IAligner` → `ProportionalAligner` (varsayılan), `WhisperAligner` (opsiyonel).
- Ön gereksinimler: `IPrerequisite` → `DownloadablePrerequisiteBase` → Node (portable zip), FFmpeg, ChromeHeadlessShell, Uv/Python, HyperFrames paketleri, model dosyaları. Kurulum dizini `%LocalAppData%\TahaKonsept\` (ad çakışması yok).

## 8. Portlar / ortam
- Web 7110, API 7111 (Pi, `0.0.0.0`), ajan video sunucusu 7120 (PC).
- Pi: self-contained `linux-arm64` publish → `/opt/tahakonsept/{web,api}`, veri `/var/lib/tahakonsept`, systemd `tahakonsept-web.service`, `tahakonsept-api.service`, kullanıcı `tahakonsept`. PostgreSQL DB `tahakonsept`, rol `tahakonsept`.
- Geliştirme: PG'ye `ssh -L 5433:localhost:5432 tmpi` tüneli; bağlantı `ConnectionStrings:Main` (user-secrets / env `TAHAKONSEPT_DB`).

## 9. UI (Web)
- Tema: CSS değişkenleri `wwwroot/css/theme.css` (`:root` açık: krem/turuncu/kahve; `[data-theme="dark"]` koyu). Sağ üst ☀/🌙 düğmesi, tercih `localStorage` (try/catch) + sunucu tarafı kullanıcı ayarı.
- Üst bar (`_Layout.cshtml`): logo + "Taha Konsept" · aylık harcama ($x.yy) · Konseptler · Konseptten Video · Üretilenler · Silinenler · Yayınlama · [profil dropdown] ✏️ ➕ · Hata bildir · Ayarlar · ☀/🌙. Alt ince **Günlük** şeridi (polling, renkli seviyeler).
- Modallar: ortak `TK.modal.open({title, body, onClose})`; dışına tıkla / kapat → kaydet.
- Sayfa JS'leri `wwwroot/js/pages/<sayfa>.js`, ortak `wwwroot/js/core/*.js`. Her JS dosyası tek modül (IIFE, `TK.<ad>`).
- İkon: `source/` içindeki SVG'ler build sırasında `wwwroot/img/` ve ajana kopyalanır (yoksa geçici SVG).
