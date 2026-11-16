# Architecture

> Bootstrap sırasında 2026-11-14'te dolduruldu (kaynak: `Promtlar/0007`–`0009`); aynı gün spec bölme sonrası onaylanan kararlarla güncellendi. Burada karar verilmiş mimari anlatılır, hedef değil.
> Agents read this file before planning; vague answers here become vague code.

## System overview
ANEW Konsept **3 ayrı uygulamadan** oluşur: **Konsept.Web** (ASP.NET Core MVC/Razor, yalnızca ön yüz/BFF, veritabanına dokunmaz), **Konsept.Api** (ASP.NET Core Web API, **modüler monolit**; asıl uygulama, online model çağrıları ve arka plan işleri burada) ve **Konsept.Agent** (Avalonia 12 tray uygulaması, Windows öncelikli; yalnızca yerel işler: render, yerel ses/görsel üretimi, dosya işlemleri, kurulum). Web, Api ve PostgreSQL 17 (Pi'de kurulu Debian paketi) Raspberry Pi 5 (`linux-arm64`) üzerinde çalışır; ajan kullanıcının bilgisayarındadır. Arka plan kuyruğu PostgreSQL üzerindedir; Redis/RabbitMQ yok (Pi'de daha az RAM ve bakım). Gerekçe: tek geliştirici ve tek Pi için mikroservisin bedeli yok, modül sınırları ise architecture testleriyle korunuyor → [ADR-0001](decisions/0001-modular-monolith-three-apps.md). Online LLM çağrılarının Pi'de, videoların kullanıcı PC'sinde olması → [ADR-0002](decisions/0002-online-calls-on-pi-videos-on-user-pc.md).

```
Tarayıcı ──Ajax──▶ Konsept.Web (MVC, Pi) ──HTTP+JWT──▶ Konsept.Api (modüler monolit, Pi) ──▶ PostgreSQL (Pi)
                                                              ▲
                                     SignalR (bağlantıyı ajan açar) + HTTP (dosya)
                                                              │
                                              Konsept.Agent (Avalonia, kullanıcı PC'si)
```

**Üretim akışı:** AiGateway (Pi) online LLM'i çağırır → üretilen video kodu Pi'ye gelir → Production modülü kodu AgentGateway üzerinden ajana iletir → ajan kısıtlı ortamda render eder (H-4) ve sonucu bildirir. API key'ler Pi'den hiç çıkmaz.

**Video dosyaları** kullanıcının bilgisayarında, ajanın klasöründe kalır. Web'deki oynatıcı videoyu ajanın yalnızca `localhost`'tan erişilebilen, kısa ömürlü token'lı yerel akışından oynatır. Bedeli: video yalnızca ajanın kurulu olduğu bilgisayardan izlenebilir.

**Arayüz dili:** yalnızca Türkçe; tüm metinler resx kaynak dosyalarında (ileride çoklu dil eklenebilsin diye). Video dili ayrıca profil başına seçilir.

## Modules / components and ownership
Konsept.Api içindeki 7 modül:

| Module | Single responsibility | Owns |
|---|---|---|
| **Identity** | Login, kayıt, roller ve yetkiler, ban, oturumlar, ajan cihaz eşleştirme | Kullanıcı, rol, yetki, oturum, cihaz |
| **Profiles** | Kanal profilleri, hazır şablonlar ve profil başına Konseptler ayarları (0005) | Profil, şablon, konsept ayarları (rol başına model + efor, ince ayar, kelime denetimi) |
| **Concepts** | Konu önerisi, konsept üretimi ve düzenleme döngüsü, onay (BR-16) | Konu önerisi, konsept, sahne, revizyon |
| **AiGateway** | Sağlayıcı ve model kataloğu, kullanıcının şifreli API key'leri, tüm online model çağrıları (key doğrulama çağrısı dahil), maliyet kaydı (BR-12) | Sağlayıcı, model, API key, harcama (profil başına model seçimleri Profiles'tadır; AiGateway yalnızca "seçilebilir mi" kararını verir — 0005) |
| **Production** | Üretim sırası, işler, aşamalar, pipeline yönetimi, videolar ve sürümleri | Kuyruk, iş, aşama, aşama çıktısı, video, video sürümü |
| **AgentGateway** | Ajanla bağlantı: SignalR hub, ajanın çevrimiçi durumu, komut gönderme, dosya alışverişi | Ajan bağlantısı, komut kaydı |
| **Monitoring** | Günlük akışı, hata bildirimleri, teknik loglar, admin istatistikleri | Günlük, hata bildirimi, log |

- **Silinenler (Trash) modül değildir.** Konsept ve video kendi modüllerinde "silindi" durumuna geçer; Silinenler sayfası Concepts ve Production'ın `Contracts` arayüzlerinden derlenir.
- **Model kataloğu** sistemle gelen sürümlenmiş seed verisidir (0004). Online girişler: metin modelleri, online görsel olarak yalnızca OpenAI gpt-image (0004) ve online ses olarak ElevenLabs (0017). Yerel girişler (0006 Req 18): yerel görsel modelleri (Z-Image-Turbo, FLUX.2 klein 4B), yerel ses modelleri (Supertonic 3, Chatterbox Multilingual V3) ve video motorları (HyperFrames, Remotion) katalogda "yerel" olarak tanımlıdır (türleri sırasıyla "görsel", "ses" ve "video motoru"; "ses" ve "video motoru" türleri 0006 Req 18'de eklenir); API key gerektirmez, birim fiyat taşımaz ve AiGateway üzerinden çağrılmaz, ajanda çalışır. Katalogda Ollama/yerel LLM girişi yoktur (E-4). Ajanın bildirdiği kurulu isteğe bağlı bileşenler kullanıcı başına saklanır ve yalnızca Remotion'un listelenmesini ve yeni profilin görsel varsayılanını belirler (0006 Req 19).
- **İlk admin** Identity tarafından Pi'deki bir ortam ayarında verilen tek bir e-posta adresiyle belirlenir (gizli bilgi değil; kayıtta ve API açılışında eşleşen hesap admin rolü alır; ayar boşsa hiçbir hesap admin olmaz). Başka yol yoktur (0011 Req 5). Ayarın adı **`Identity:AdminEmail`** (Pi'de `Identity__AdminEmail`, `/etc/anewconcept/api.env`); açılış senkronu ayarı tek doğruluk kaynağı yapar (eşleşen hesap admin, diğerleri kullanıcı). Rol `identity.users.role`'de durur; yetki her istekte oturum doğrulama sorgusuyla DB'den okunur, JWT'de taşınmaz (plan 0011). İskelet admin ucu (`GET api/admin/overview`, `admin.panel` politikası) şimdilik `Identity.Presentation`'dadır; 0010 yerini yeniden değerlendirir.
- **Admin paneli modül değildir.** Veriyi Monitoring ve Identity'nin `Contracts` arayüzlerinden okur; üretim istatistikleri (ör. toplam klip sayısı) için Production `Contracts`'ı da okuyabilir. Tam kaynak listesi admin paneli spec'inde netleşir (düşük öncelik).

Diğer uygulamalar:

| Component | Single responsibility | Owns |
|---|---|---|
| **Konsept.Web** | Razor sayfaları, `$.ajax` uçları, oturum cookie'si (BFF); her şeyi API'ye sorar | Yalnızca şifreli HttpOnly cookie içindeki token'lar; veri yok |
| **Konsept.Agent** | Yerel kurulum (Node.js LTS, FFmpeg, headless Chromium; izole Python yalnızca isteğe bağlı "Ses klonlama" bileşeninde — 0009; Ollama kurulmaz, E-4), render, yerel ses/görsel modelleri, dosya işlemleri, localhost video akışı | Yerel video dosyaları ve aşama çıktıları, kendi refresh token'ı |

### Layer / project structure
Proje sayısında **sabit bir kural yok; gerektiği kadar proje** açılır. Değişmeyen şey katman ayrımı ve Y-1…Y-8'dir; bunları proje sayısı değil **architecture testleri (ArchUnitNET, `Konsept.ArchitectureTests`)** korur. Bir modülün katmanları:

| Katman | İçerik | Örnek ad |
|---|---|---|
| **Contracts** | Başka modüllerin görebildiği tek yüzey: interface, DTO, olay | `Konsept.Modules.Concepts.Contracts` |
| **Domain** | Entity, iş kuralı; altyapı bağımlılığı yok | `Konsept.Modules.Concepts.Domain` |
| **Application** | Servisler, validator'lar (FluentValidation), use-case'ler; `Result<T>` döndürür | `Konsept.Modules.Concepts.Application` |
| **Infrastructure** | EF Core 10 (Npgsql), modülün `DbContext`'i ve şeması, dış çağrılar | `Konsept.Modules.Concepts.Infrastructure` |
| **Presentation** | API controller'ları (yalnızca doğrula, servise ver, döndür; K-4) | `Konsept.Modules.Concepts.Presentation` |

**AiGateway (0004):** `Konsept.Modules.AiGateway.{Domain,Application,Infrastructure,Presentation}` — `ai_gateway` şeması (`api_keys`), kendi `AiGatewayDbContext`'i; katalog DB'de değil, Infrastructure'a gömülü sürümlü `Catalog/catalog.json` seed'idir (`ICatalogSource`). Uçlar: `GET api/ai/catalog`, `GET|PUT|DELETE api/ai/keys[/{provider}]`. **0013:** çağrı kapısı `IAiCallGateway`
(Application), harcama kayıtları `ai_gateway.spend_records`, sağlayıcı istemcisi `Providers/Concrete/HttpProviderClient`
(seed'deki `dialect`, `chatUrl`, `imageUrl`, `apiModel`), uçlar `POST api/ai/calls/trial` (Ayarlar'daki "Dene") ve
`GET api/ai/spend/month`. **0005:** `Konsept.Modules.AiGateway.Contracts` açıldı — `ITextModelDirectory` (isteği yapan kullanıcı için
katalogdaki metin modelleri + seçilebilir bayrağı: yetkili ve key `available`); ilk tüketici Profiles. **0014:** Contracts'a `IModelCaller`
(başka modülün metin çağrısı: `Current()` ile çağıran kimliği + yetkileri isteğin içinde yakalanır, `CallTextAsync` rolü harcama amacına
çevirip kapıdan geçirir; çıktı sınırı katalog limiti).

**Profiles (0003):** `Konsept.Modules.Profiles.{Domain,Application,Infrastructure,Presentation}` — `profiles` şeması (`profiles`: `(user_id, name_key)` tekil, `name_key` = tr-TR küçük harf; `selections`: kullanıcı başına hatırlanan seçili profil), kendi `ProfilesDbContext`'i. Hazır şablonlar Infrastructure'a gömülü `Templates/templates.json`'dır. Uçlar: `GET|POST api/profiles`, `PUT|DELETE api/profiles/{id}`, `PUT api/profiles/selected`; başka kullanıcının profili her işlemde 404'tür. Seçili profil okunurken çözülür (hatırlanan yoksa ilk oluşturulan). Sistem talimatı Domain'de saf `ProfileInstruction.Build`'dir, hiçbir uçta dönmez; konsept üretiminde kullanımı (ve Contracts katmanı) 0014'te eklenir. Web'de üst bar profil çubuğu ve modal `profiles.js` ile `/profiller` uçlarını kullanır. **0005:** `profiles.concept_settings` (profil başına tek satır, `profile_id` FK cascade; rol başına `*_model`/`*_effort` × konsept/tekrar denetimi/konu önerisi, `tone`/`audience`/`narration`, `word_check`); uçlar `GET|PUT api/profiles/{id}/concept-settings` (başka kullanıcının profili 404; model seçimi AiGateway.Contracts `ITextModelDirectory` ile sunucuda denetlenir, BR-13). Web: Konseptler şeridi `concepts.js` + `/konsept-ayarlari/{profil}`; `profiles.js` seçili profili `profile:selected` olayıyla duyurur.
**0014:** `Konsept.Modules.Profiles.Contracts` — `IProfileGenerationReader` (kullanıcı kimliğiyle profil + konsept ayarları + sistem
talimatı; istek bağlamı gerektirmez). Profil silme Concepts.Contracts `IProfileConceptPresence`'a sorar: konsepti olan profil silinemez (409).

**Concepts (0014):** `Konsept.Modules.Concepts.{Contracts,Domain,Application,Infrastructure,Presentation}` — `concepts` şeması
(`concepts`: sahneler `jsonb`, durum, profil değerleri satırda — dil/biçim/hedef süre, uyarılar, `deleted_at`; `topic_suggestions`:
bekleyen / önerilip seçilmemiş / seçildi). Üretim **arka plan işidir**: başlatma ucu doğrular, çağıran kimliğini ve profil bağlamını
yakalar, `IConceptJobLauncher` işi kendi DI kapsamında `Task.Run` ile başlatır ve hemen döner; iş durumu Api sürecinin belleğindeki
`IConceptJobRegistry`'dedir (kullanıcı × şerit başına bir iş: `topic` · `idea`; Api yeniden başlarsa süren iş kaybolur — B-93). Konseptler
sırayla, konsept başına tek çağrı; Durdur sıradakileri başlatmaz. Tüm çağrılar AiGateway.Contracts `IModelCaller` ile (rol → amaç:
konu önerisi, konsept, anlamca tekrar denetimi → `Review`), Günlük olayı Monitoring.Contracts `ISystemEventWriter` ile (başlık içermez).
Eşikler ve durak kelimeler gömülü `Rules/concept-rules.json`. Uçlar: `GET api/concepts/work`, `POST api/concepts/{topics,from-topics,
from-idea,jobs/stop,approve,unapprove}`, `GET api/concepts[/{id}]`. Web: `concept-work.js` + `/konsept-uretim/…`, `/konseptler/…`
(süren iş varken 2 sn yoklama). Web araması ve kaynak listesi yok (B-91).
**0015 düzenleme döngüsü:** `IConceptEditService` — `POST api/concepts/{id}/scenes` (elle düzenleme, çağrısız; yalnızca Üretildi/Onaylandı,
sahne sayısı aynı) · `POST {id}/regenerate` (talimatla yeniden üretim, arka planda `IConceptJobLauncher.LaunchRevision`) · `GET {id}/revision` ·
`POST {id}/revision/{approve,discard}`. Revizyon Api belleğindeki `IConceptRevisionRegistry`'dedir (kullanıcı × konsept başına tek;
süren varken ikinci istek 409 — G-3; Api yeniden başlarsa revizyon kaybolur, konsept değişmez). Çağrı 0014'ün konsept talimatıyla,
`ModelRole.ConceptGeneration` + profil; mevcut konsept ve talimat istemde veri bloklarında; başlık değişmez. Web: `concept-edit.js`
(`concept-work.js` ile `concept:before-open` / `concept:opened` / `concept:changed` olayları), `/konseptler/{id}/…`, `/?konsept=<id>` bağlantısı.
**0016 Silinenler:** `IConceptTrashService` — `POST api/concepts/{id}/delete` (soft delete, `deleted_at`; yalnızca Üretildi/Onaylandı) ·
`GET api/concepts/deleted[/{id}]` (en yeni silinen üstte, salt okunur) · `POST api/concepts/deleted/{id}/purge` (kalıcı; konudan gelmişse aynı
anahtarlı "seçildi" öneri de silinir) · `POST api/concepts/suggestions/reset` (yalnızca profilin `Rejected` önerileri). Silinmiş konsept öneri
geçmişinde "elenen" sayılır (`ListTitlesAsync`, zamanı `deleted_at`). Web: `deleted.js` (`/silinenler/konseptler/…`), `suggestion-reset.js`
(`/oneriler/sifirla`), ortak `confirm-dialog.js`; `concept-work.js` çöpü `concept:deleted` olayını yayar.

**Monitoring — hata bildirimleri (0022):** `monitoring.error_reports` (sıralı `Number`, kaynak web/ajan/API, durum "Yeni", süresiz). Web: `POST api/error-reports` + `GET api/error-reports/preview` (oturumlu; son 20 Günlük kaydını ve versiyonu sunucu ekler; kullanıcı başına saatte 10, web + ajan). Okuma `GET api/error-reports/{number}` yalnızca `admin.panel` (listeleme 0025). Ajan bildirimi ayrı HTTP ucu değil, kimlikli hub metodu `ReportError` (+ hesap için `WhoAmI`); AgentGateway Monitoring.Contracts `IErrorReportSubmitter`'a iletir. API'nin beklenmeyen istisnaları Monitoring'deki `IExceptionHandler` ile kaynak "API" kaydı olur (ProblemDetails sürer; 4xx kayıt oluşturmaz). Ajan bekleyen bildirimleri `pending-reports.json`'da (en çok 20, oluştuğu hesaba bağlı) tutar.

**Ajan dağıtımı ve ilk kurulum (0009):** Deploy ajanı win-x64 self-contained **tek exe** olarak yayımlar (Pi adresleri `AssemblyMetadata` ile gömülü; exe yanında `agentsettings*.json` varsa o önceliklidir) ve `/opt/anewconcept/agent/anewconcept-ajan-<sürüm>-win-x64.exe`'ye koyar; Web `GET /ajan/indir` (giriş ister; `AgentDownload:Directory`) ürün sürümündeki dosyayı verir — bağlantı Ayarlar'da ve durum alanında "Ajan bağlı değil"ken. Ajan verisi kullanıcı başına `%LOCALAPPDATA%\anewconcept` altındadır (`logs`, oturum belirteci, bekleyen bildirimler, `setup-state.json`, `components\<id>`); exe nerede durursa dursun. Kurulum motoru (`SetupCoordinatorBase`) gömülü `components.json`'daki sabit sürümlü bileşenleri resmi kaynaktan indirir, SHA-256 ile doğrular, `<id>.partial` üzerinden kurar; başarısız indirme 2 kez yeniden denenir, sonra Pi aynası (`/ajan-bilesenleri/<dosya>`) denenir. Kurulu exe'lerin hash'i her açılışta karşılaştırılır (uyuşmazsa "Onarılmalı"). Bugün çekirdek: Node.js, FFmpeg (LGPL), chrome-headless-shell; ertelenenler backlog B-73…B-78.

**Identity (0002):** `Konsept.Modules.Identity.{Contracts,Domain,Application,Infrastructure,Presentation}` — `identity` şeması (`users`, `sessions`), kendi `IdentityModuleDbContext`'i; `Contracts`: `ISessionRevoker` (tüm oturumları iptal; 0012 ajan, 0024 ban) ve `IAgentDeviceRevocationListener` (0012: iptal edilen ajan cihazını AgentGateway'e bildirir; AgentGateway canlı bağlantıya "oturum kapandı" gönderip keser). Ajan cihazları `identity.agent_devices`'ta (belirteç yalnızca SHA-256 hash; 90 gün kayan); eşleştirme kodları bellekte (10 dk, tek süreç). AgentGateway kullanıcı başına tek bağlı ajan tutar; durum, "Ajanı test et" ve ajan olayları sahibine göre süzülür. Şifre hash'i ASP.NET Core Identity'nin `PasswordHasher`'ı (V3); `UserManager`/`IdentityDbContext` kullanılmaz. Kilit sayacı ve refresh dönüşü tek ifadelik atomik SQL'dir.

Katmanların hangi projelere bölüneceği plan aşamasında ihtiyaca göre belirlenir; Y-kuralları her durumda architecture testleriyle doğrulanır. Ortak parçalar: `Konsept.Shared.Kernel` (base tipler, `Result`, generic repository arayüzleri), `Konsept.Shared.Infrastructure` (EF temeli, şifreleme, Api loglama kurulumu), `Konsept.Shared.Logging` (yalnızca Serilog: maskeleme, ortak log biçimi, dosya saklama; Web, Api ve ajan kullanır — 0008), `Konsept.Web.ApiClient` (Web'in typed API client'ı). Ajan da kendi içinde Domain, Application, Infrastructure ve UI (Avalonia) olarak bölünür. Testler `tests/` altında (`docs/testing.md`).

## Communication rules
1. **Modül → modül:** yalnızca karşı modülün `Contracts` arayüzü üzerinden (senkron, aynı süreç içinde) ya da bir olay üzerinden (ör. `ConceptApproved` → Production). Olaylar işlem commit edildikten sonra yayınlanır. Başka her yol yasaktır. Konseptin sonraki durumlarını (Sırada → Üretiliyor → Video oldu, BR-16) Production olaylarla (ya da Concepts `Contracts`'ı ile) bildirir, durumu Concepts günceller; olay adları walking skeleton ya da Konseptler spec'inde netleşir.
2. **Tarayıcı → Web:** yalnızca jQuery `$.ajax` (tek `api.js` sarmalayıcısı; `fetch` yasak, K-6). Tarayıcıdaki canlı güncellemeler (Günlük, ilerleme) **2–3 sn Ajax polling** ile; tarayıcıda SignalR yok (istemcisi `fetch` kullanıyor).
3. **Web → API:** HTTP + JWT (`Konsept.Web.ApiClient` üzerinden). Web veritabanına bağlanmaz.
4. **API → Ajan:** SignalR üzerinden komut ("şu aşamayı çalıştır", "klasörü aç", "sil"); bağlantıyı ajan açar (NAT arkasında), .NET istemcisi yalnızca ajanda. **Ajan → API:** ilerleme, sonuç, log, hata bildirimi. Büyük dosyalar (gerektiğinde aşama girdileri/çıktıları) HTTP ile taşınır; üretilen video dosyaları Pi'ye yüklenmez (ADR-0002). Ajan yeniden bağlanınca kendini yeniden kaydeder ve yarım işini bildirir.
5. **Online LLM çağrıları:** yalnızca Pi'deki AiGateway üzerinden; ajan online sağlayıcıya doğrudan gitmez.
6. **Tarayıcı → Ajan:** yalnızca video oynatma için, ajanın localhost-only, kısa ömürlü token'lı akış ucu.
7. **Veritabanı:** tek PostgreSQL 17; **her modülün kendi şeması ve kendi `DbContext`'i** var. Şemalar arası foreign key ya da join yok. Arka plan kuyruğu da PostgreSQL üzerinde.
8. **Kimlik gerektirmeyen uçlar (default-deny istisnaları, 0002 Req 11):** giriş, kayıt ve API'nin **sağlık kontrolü** ucu. Sağlık kontrolü yalnızca API'nin ve veritabanının ayakta olup olmadığını ve sürümü döndürür; ajan durumu dahil kullanıcıya ait hiçbir bilgi içermez; deploy betikleri bu ucu kullanır. Ajanın sunucu bağlantısı (hub) 0012'den beri yalnızca ajan oturum belirteciyle (`AgentToken` şeması, `Authorization` başlığı; JWT kabul edilmez) açılır; ajanın hata bildirimi de bu kimlikli hub'dadır (0022; kimliksiz bildirim ucu yok); kimliksiz ajan işlemleri yalnızca kod alma (`POST api/agent-pairing/codes`) ve kod durumu (`POST api/agent-pairing/status`, onay sırrıyla) uçlarıdır. `POST api/auth/refresh` da kimliksizdir ama yalnızca geçerli bir refresh token'la (kimlik bilgisinin kendisi) çalışır ve giriş istisnasının parçası sayılır (plan 0002 A-0). Api'de varsayılan yetki politikası (fallback) "geçerli oturumlu JWT"dir; Web'de "oturum cookie'si"dir — `[AllowAnonymous]` işaretlenmeyen her uç giriş ister. Kimliksiz uç listesi `AuthorizationSurfaceTests` (Api) ve `AuthAccessTests` (Web) ile tam liste olarak kilitlidir. Web'deki ajan bileşen aynası (`/ajan-bilesenleri/…`, 0009) bir uç değil, yalnızca `<AgentDownload:Directory>/components` klasörünü sunan statik dosya yoludur; orada yalnızca herkese açık üçüncü taraf arşivleri durur (ajan kurulum exe'si girişlidir).
9. **İç uçlar (Web → Api, ör. `POST /api/internal/logs`):** kullanıcıya değil servis anahtarı kimlik şemasına (`ServiceKey`, `AuthPolicies.InternalService`) bağlıdır; anonim değildir. Web'in giriş/kayıt çağrılarında ilettiği istemci IP başlığına (`X-Konsept-Client-Ip`) Api yalnızca geçerli servis anahtarıyla birlikte güvenir (`ClientAddress`).
10. **İsteği yapan kullanıcı:** Api'de servisler kullanıcıyı `Shared.Kernel`'deki `ICurrentUser`'dan (`sub`, `sid` claim'leri; `Shared.Infrastructure` → `HttpContextCurrentUser`) okur. Günlük şeridi ve geçmişi (`GET api/journal`) bu kullanıcıya göre süzülür: 0021'den beri **yalnızca kendi olayları** (kullanıcısız olay gösterilmez; ajan olayları 0012'den beri sahibine bağlıdır). Günlük günleri Europe/Istanbul gece yarısına göre ayrılır, kayıtlar süresiz saklanır; geçmişin ilk günü hesabın açıldığı gündür (Identity.Contracts `IUserAccountReader`).

## Forbidden dependencies (make them testable)
Referans kuralları `Konsept.ArchitectureTests` (ArchUnitNET) ile otomatik doğrulanır: Y-1…Y-6, Y-7'nin bağımlılık yarısı (LLM HTTP istemcilerine ve Data Protection çözücüsüne yalnızca AiGateway referans verir) ve Y-8'in "ajan API key sözleşmelerine referans vermez" yarısı. Y-8'in sandbox maddesi H-4 testleriyle (kasıtlı bozma kanıtı) doğrulanır; yalıtım yöntemi Karar 5 ile seçildi ve 0018'de tanımlı. Architecture testleri `scripts/check` içinde `unit [fast]` adımında çalışır (walking skeleton ile etkinleşti); her kuralın kasıtlı ihlali `tests/Konsept.ArchitectureTests.Fixtures` derlemesindedir ve kuralın onu yakaladığı test edilir.

- **Y-1:** Bir modül, başka bir modülün yalnızca `Contracts` katmanına referans verebilir.
- **Y-2:** `Domain` katmanı, `Shared.Kernel` dışında hiçbir şeye bağımlı olamaz (EF Core, ASP.NET yasak).
- **Y-3:** `Application`, `Infrastructure`'a referans veremez.
- **Y-4:** Hiçbir modül başka bir modülün şemasını ya da `DbContext`'ini kullanamaz.
- **Y-5:** `Konsept.Web` hiçbir modüle ve veritabanına referans veremez; yalnızca `Konsept.Web.ApiClient`'ı kullanır.
- **Y-6:** Controller'lar yalnızca validator'a ve Application servis arayüzüne bağımlıdır; repository ya da `DbContext` kullanamaz.
- **Y-7:** Dış LLM API'lerini çağırabilen ve çözülmüş API key'i görebilen **tek modül AiGateway**'dir (H-2 ve H-3 tek noktada).
- **Y-8:** Ajan, kullanıcının online API key'lerini hiç tutmaz. LLM'in ürettiği kod ajanda yalnızca kısıtlı render ortamında çalışır (H-4).

## Deliberately out of scope
- Yayınlama (yalnızca "Çok yakında sizlerleyiz" sayfası).
- Mobil uygulama.
- Linux/macOS ajanı (önce Windows).
- Birden fazla sunucuya ölçekleme, mikroservis.
- Redis/RabbitMQ gibi ayrı kuyruk/önbellek servisleri.
- Tarayıcıda SignalR (polling kullanılır).
- İnternete açılma (HTTPS + reverse proxy), e-posta doğrulaması → `specs/backlog.md`.
- Video motoru alternatifleri, yerel model çalıştırma yöntemi ve render sandbox yöntemi araştırması teslim edildi (`raporlar/b5-video-pipeline-arastirmasi`); kararlar (Karar 5) spec'lere işlendi (0006, 0009, 0017, 0018). Temel stack bu rapora göre değişmez.
