# Architecture

> Bootstrap sırasında 2026-11-14'te dolduruldu (kaynak: `Promtlar/0007`–`0009`). Burada karar verilmiş mimari anlatılır, hedef değil.
> Agents read this file before planning; vague answers here become vague code.

## System overview
ANEW Konsept **3 ayrı uygulamadan** oluşur: **Konsept.Web** (ASP.NET Core MVC/Razor, yalnızca ön yüz/BFF, veritabanına dokunmaz), **Konsept.Api** (ASP.NET Core Web API, **modüler monolit**; asıl uygulama, online model çağrıları ve arka plan işleri burada) ve **Konsept.Agent** (Avalonia 12 tray uygulaması, Windows öncelikli; yalnızca yerel işler: render, yerel ses/görsel üretimi, dosya işlemleri, kurulum). Web, Api ve PostgreSQL 18 Raspberry Pi 5 (`linux-arm64`) üzerinde çalışır; ajan kullanıcının bilgisayarındadır. Arka plan kuyruğu PostgreSQL üzerindedir; Redis/RabbitMQ yok (Pi'de daha az RAM ve bakım). Gerekçe: tek geliştirici ve tek Pi için mikroservisin bedeli yok, modül sınırları ise architecture testleriyle korunuyor → [ADR-0001](decisions/0001-modular-monolith-three-apps.md). Online LLM çağrılarının Pi'de, videoların kullanıcı PC'sinde olması → [ADR-0002](decisions/0002-online-calls-on-pi-videos-on-user-pc.md).

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
| **AiGateway** | Sağlayıcı ve model kataloğu, kullanıcının şifreli API key'leri, tüm online model çağrıları, maliyet kaydı (BR-12) | Sağlayıcı, model, API key, model seçimleri, harcama |
| **Production** | Üretim sırası, işler, aşamalar, pipeline yönetimi, videolar ve sürümleri | Kuyruk, iş, aşama, aşama çıktısı, video, video sürümü |
| **AgentGateway** | Ajanla bağlantı: SignalR hub, ajanın çevrimiçi durumu, komut gönderme, dosya alışverişi | Ajan bağlantısı, komut kaydı |
| **Monitoring** | Günlük akışı, hata bildirimleri, teknik loglar, admin istatistikleri | Günlük, hata bildirimi, log |

- **Silinenler (Trash) modül değildir.** Konsept ve video kendi modüllerinde "silindi" durumuna geçer; Silinenler sayfası Concepts ve Production'ın `Contracts` arayüzlerinden derlenir.
- **Admin paneli modül değildir.** Veriyi Monitoring ve Identity'nin `Contracts` arayüzlerinden okur; üretim istatistikleri (ör. toplam klip sayısı) için Production `Contracts`'ı da okuyabilir. Tam kaynak listesi admin paneli spec'inde netleşir (düşük öncelik).

Diğer uygulamalar:

| Component | Single responsibility | Owns |
|---|---|---|
| **Konsept.Web** | Razor sayfaları, `$.ajax` uçları, oturum cookie'si (BFF); her şeyi API'ye sorar | Yalnızca şifreli HttpOnly cookie içindeki token'lar; veri yok |
| **Konsept.Agent** | Yerel kurulum (Node.js LTS, FFmpeg, headless Chromium, Ollama, izole Python), render, yerel ses/görsel modelleri, dosya işlemleri, localhost video akışı | Yerel video dosyaları ve aşama çıktıları, kendi refresh token'ı |

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
7. **Veritabanı:** tek PostgreSQL 18; **her modülün kendi şeması ve kendi `DbContext`'i** var. Şemalar arası foreign key ya da join yok. Arka plan kuyruğu da PostgreSQL üzerinde.

## Forbidden dependencies (make them testable)
Referans kuralları `Konsept.ArchitectureTests` (ArchUnitNET) ile otomatik doğrulanır: Y-1…Y-6, Y-7'nin bağımlılık yarısı (LLM HTTP istemcilerine ve Data Protection çözücüsüne yalnızca AiGateway referans verir) ve Y-8'in "ajan API key sözleşmelerine referans vermez" yarısı. Y-8'in sandbox maddesi, yöntem seçilince H-4 testleriyle (kasıtlı bozma kanıtı) doğrulanır (`specs/backlog.md` B-5). Architecture testleri `scripts/check` içinde (`[fast]`) çalışır; adım greenfield olduğu için `scripts/check.conf`'ta şimdilik yorum satırıdır, walking skeleton ile etkinleşir.

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
- Ertelenen araştırma: video motoru alternatifleri, yerel model çalıştırma yöntemi (Python/ONNX/ComfyUI) ve render sandbox yöntemi → `raporlar/` altında rapor, `specs/backlog.md`. Temel stack bu rapora göre değişmez.
