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
| **Profiles** | Kanal profilleri ve hazır şablonlar | Profil, şablon |
| **Concepts** | Konu önerisi, konsept üretimi ve düzenleme döngüsü, onay (BR-16) | Konu önerisi, konsept, sahne, revizyon |
| **AiGateway** | Sağlayıcı ve model kataloğu, kullanıcının şifreli API key'leri, tüm online model çağrıları (key doğrulama çağrısı dahil), maliyet kaydı (BR-12) | Sağlayıcı, model, API key, model seçimleri, harcama |
| **Production** | Üretim sırası, işler, aşamalar, pipeline yönetimi, videolar ve sürümleri | Kuyruk, iş, aşama, aşama çıktısı, video, video sürümü |
| **AgentGateway** | Ajanla bağlantı: SignalR hub, ajanın çevrimiçi durumu, komut gönderme, dosya alışverişi | Ajan bağlantısı, komut kaydı |
| **Monitoring** | Günlük akışı, hata bildirimleri, teknik loglar, admin istatistikleri | Günlük, hata bildirimi, log |

- **Silinenler (Trash) modül değildir.** Konsept ve video kendi modüllerinde "silindi" durumuna geçer; Silinenler sayfası Concepts ve Production'ın `Contracts` arayüzlerinden derlenir.
- **Model kataloğu** sistemle gelen sürümlenmiş seed verisidir (0004). Online girişler: metin modelleri, online görsel olarak yalnızca OpenAI gpt-image (0004) ve online ses olarak ElevenLabs (0017). Yerel girişler (0006 Req 18): yerel görsel modelleri (Z-Image-Turbo, FLUX.2 klein 4B), yerel ses modelleri (Supertonic 3, Chatterbox Multilingual V3) ve video motorları (HyperFrames, Remotion) katalogda "yerel" olarak tanımlıdır (türleri sırasıyla "görsel", "ses" ve "video motoru"; "ses" ve "video motoru" türleri 0006 Req 18'de eklenir); API key gerektirmez, birim fiyat taşımaz ve AiGateway üzerinden çağrılmaz, ajanda çalışır. Katalogda Ollama/yerel LLM girişi yoktur (E-4). Ajanın bildirdiği kurulu isteğe bağlı bileşenler kullanıcı başına saklanır ve yalnızca Remotion'un listelenmesini ve yeni profilin görsel varsayılanını belirler (0006 Req 19).
- **İlk admin** Identity tarafından Pi'deki bir ortam ayarında verilen tek bir e-posta adresiyle belirlenir (gizli bilgi değil; kayıtta ve API açılışında eşleşen hesap admin rolü alır; ayar boşsa hiçbir hesap admin olmaz). Başka yol yoktur (0011 Req 5). Ayarın adı plan aşamasında belirlenir.
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

Katmanların hangi projelere bölüneceği plan aşamasında ihtiyaca göre belirlenir; Y-kuralları her durumda architecture testleriyle doğrulanır. Ortak parçalar: `Konsept.Shared.Kernel` (base tipler, `Result`, generic repository arayüzleri), `Konsept.Shared.Infrastructure` (EF temeli, şifreleme, loglama), `Konsept.Web.ApiClient` (Web'in typed API client'ı). Ajan da kendi içinde Domain, Application, Infrastructure ve UI (Avalonia) olarak bölünür. Testler `tests/` altında (`docs/testing.md`).

## Communication rules
1. **Modül → modül:** yalnızca karşı modülün `Contracts` arayüzü üzerinden (senkron, aynı süreç içinde) ya da bir olay üzerinden (ör. `ConceptApproved` → Production). Olaylar işlem commit edildikten sonra yayınlanır. Başka her yol yasaktır. Konseptin sonraki durumlarını (Sırada → Üretiliyor → Video oldu, BR-16) Production olaylarla (ya da Concepts `Contracts`'ı ile) bildirir, durumu Concepts günceller; olay adları walking skeleton ya da Konseptler spec'inde netleşir.
2. **Tarayıcı → Web:** yalnızca jQuery `$.ajax` (tek `api.js` sarmalayıcısı; `fetch` yasak, K-6). Tarayıcıdaki canlı güncellemeler (Günlük, ilerleme) **2–3 sn Ajax polling** ile; tarayıcıda SignalR yok (istemcisi `fetch` kullanıyor).
3. **Web → API:** HTTP + JWT (`Konsept.Web.ApiClient` üzerinden). Web veritabanına bağlanmaz.
4. **API → Ajan:** SignalR üzerinden komut ("şu aşamayı çalıştır", "klasörü aç", "sil"); bağlantıyı ajan açar (NAT arkasında), .NET istemcisi yalnızca ajanda. **Ajan → API:** ilerleme, sonuç, log, hata bildirimi. Büyük dosyalar (gerektiğinde aşama girdileri/çıktıları) HTTP ile taşınır; üretilen video dosyaları Pi'ye yüklenmez (ADR-0002). Ajan yeniden bağlanınca kendini yeniden kaydeder ve yarım işini bildirir.
5. **Online LLM çağrıları:** yalnızca Pi'deki AiGateway üzerinden; ajan online sağlayıcıya doğrudan gitmez.
6. **Tarayıcı → Ajan:** yalnızca video oynatma için, ajanın localhost-only, kısa ömürlü token'lı akış ucu.
7. **Veritabanı:** tek PostgreSQL 17; **her modülün kendi şeması ve kendi `DbContext`'i** var. Şemalar arası foreign key ya da join yok. Arka plan kuyruğu da PostgreSQL üzerinde.
8. **Kimlik gerektirmeyen uçlar (default-deny istisnaları, 0002 Req 11):** giriş, kayıt ve API'nin **sağlık kontrolü** ucu. Sağlık kontrolü yalnızca API'nin ve veritabanının ayakta olup olmadığını ve sürümü döndürür; ajan durumu dahil kullanıcıya ait hiçbir bilgi içermez; deploy betikleri bu ucu kullanır. Ajanın sunucu bağlantısı 0002'de geçici olarak kimliksizdir; 0012 bu istisnayı kaldırır ve yerine eşleştirme istisnalarını koyar.

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
