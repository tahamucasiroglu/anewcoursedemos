# ADR 0001 — Modüler monolit + 3 uygulama (Web BFF, Api, Agent), modül başına clean architecture

- Status: Accepted
- Date: 2026-11-14

## Context
ANEW Konsept; konu önerisi, konsept üretimi ve konseptten video üretimi yapan bir sistem. Sunucu
tarafı tek bir **Raspberry Pi 5** (`linux-arm64`) üzerinde çalışıyor; RAM ve bakım kapasitesi
sınırlı. Video render'ı, yerel ses/görsel modelleri ve dosya işlemleri ise kullanıcının Windows
bilgisayarında yapılmak zorunda (Node.js + FFmpeg + headless Chromium, Ollama, izole Python).
Kullanıcı web tarafı için ASP.NET Core MVC (Razor) + jQuery `$.ajax`, ayrı bir ASP.NET Core Web
API ve Avalonia masaüstü ajanı istedi; katmanların ayrı projeler olmasını tercih etti. Tek
geliştirici var, sistem şimdilik yerel ağda tek kullanıcılı ama kod çok kullanıcılı kuruluyor
(BR-1). Kaynak: `Promtlar/0007_*.md` (stack), `Promtlar/0008_*.md` (mimari önerisi),
`Promtlar/0009_*.md` (onay ve düzeltmeler).

## Decision
Sistem 3 ayrı uygulamadan oluşur: **Konsept.Web** (MVC ön yüzü, BFF; veritabanına dokunmaz, her
şeyi API'ye sorar), **Konsept.Api** (7 modüllü modüler monolit: Identity, Profiles, Concepts,
AiGateway, Production, AgentGateway, Monitoring) ve **Konsept.Agent** (Avalonia 12, önce
Windows; yalnızca yerel işler). Her modül içeride Domain / Application / Infrastructure /
Presentation katmanlarına ve dışa açık tek yüzey olan `Contracts`'a ayrılır; proje sayısı sabit
değildir ("gerektiği kadar proje"), sınırları Y-1…Y-8 kuralları ve ArchUnitNET architecture
testleri (`Konsept.ArchitectureTests`) korur.

Bağlayıcı ayrıntılar (`docs/architecture.md`'de tam hali):
- Modül → modül iletişimi yalnızca karşı modülün `Contracts` arayüzü (aynı süreç içinde) ya da
  commit sonrası yayınlanan olay üzerinden.
- Tek PostgreSQL 17 (Pi'deki mevcut kurulum; spec 0001 D1); her modülün kendi şeması ve kendi `DbContext`'i; şemalar arası FK/join yok
  (Y-4).
- Arka plan kuyruğu PostgreSQL üzerinde; Redis/RabbitMQ yok.
- SignalR yalnızca API ↔ ajan arasında (.NET istemcisi, bağlantıyı ajan açar); tarayıcı canlı
  güncellemeleri 2–3 sn Ajax polling ile (K-6 `fetch` yasağı nedeniyle).
- Silinenler ve Admin paneli ayrı modül değil; ilgili modüllerin `Contracts` arayüzlerinden
  derlenir.
- Arayüz dili şimdilik yalnızca Türkçe; metinler resx kaynak dosyalarında tutulur (ileride
  çoklu dil eklenebilsin diye). Video dili ayrıca profil başına seçilir.

## Consequences
Kazandırdıkları:
- Pi'de tek API süreci + tek veritabanı: mikroservise göre çok daha az RAM, ağ ve bakım yükü.
- Modül sınırları baştan net; şema ve `DbContext` ayrımı sayesinde bir modül ileride ayrı
  servise çıkarılmak istenirse veri zaten ayrık.
- Web'in BFF olması (Y-5): veritabanı ve gizli bilgiler tek otoriteye (API) toplanıyor; Web
  yalnızca `Web.ApiClient` üzerinden konuşuyor.
- Katman ihlalleri insan dikkatine değil architecture testlerine bağlı; `scripts/check`
  kırmızıya döner.
- Ajanın ayrı uygulama olması yerel işleri (render, yerel modeller, kurulum) sunucudan
  yalıtıyor.

Bedelleri:
- Proje sayısı yüksek (modül × katman + host + ortak projeler); çözüm açılışı, derleme süresi ve
  dosya gezinmesi ağırlaşıyor.
- Proje sayısı sabit bırakıldığı için sınırları derleyici değil testler koruyor; architecture
  testleri eksik yazılırsa bir ihlal derlenip geçebilir. Testlerin kendisi bakım gerektiriyor.
- Şemalar arası join yasağı, bazı ekranlarda (ör. Silinenler, Admin) verinin birden fazla
  modülün `Contracts`'ından uygulama katmanında birleştirilmesini gerektiriyor; tek SQL'den daha
  fazla kod.
- Üç uygulamanın ayrı dağıtılması: Web ve Api Pi'ye, Agent kullanıcı PC'sine; tek ürün
  versiyonu (`Directory.Build.props`) ile birlikte sürümlenmeleri gerekiyor.
- Tarayıcıda polling, gerçek zamanlı push'a göre biraz gecikmeli ve gereksiz istek üretiyor; bu
  ölçekte kabul edildi.

## Alternatives considered
- **Modül başına sabit 5 proje (Contracts/Domain/Application/Infrastructure/Presentation):**
  önerilen buydu; sınırları derleyici koruyordu. Kullanıcı "proje sınırına gerek yok, gerektiği
  kadar proje" dedi (`Promtlar/0009_*.md`); sabit sayı kaldırıldı, katman ayrımı korunup
  zorlama architecture testlerine verildi.
- **Mikroservis / birden fazla sunucuya ölçekleme:** tek Pi, tek geliştirici ve tek kullanıcı
  için gereksiz operasyon yükü; bilinçli olarak kapsam dışı bırakıldı.
- **Ayrı mesaj kuyruğu (Redis/RabbitMQ):** Pi'de ek servis = ek RAM ve bakım; PostgreSQL
  üzerinde kuyruk yeterli görüldü.
- **Tarayıcıya SignalR:** SignalR'ın tarayıcı istemcisi içeride `fetch` kullandığı için K-6'yı
  dolaylı olarak deliyordu; yerine Ajax polling seçildi.

## Revisit triggers
- Sistem internete açılıp çok kullanıcıya hizmet vermeye başlarsa ve tek Pi CPU/RAM sınırına
  dayanırsa (bir modülü ayırmak gündeme gelir).
- PostgreSQL üzerindeki kuyruk, toplu üretimde (ör. yüzlerce videoluk sıra) gecikme veya kilit
  sorunu yaratırsa.
- Polling, Günlük/ilerleme ekranlarında kullanıcının fark ettiği gecikme ya da Pi'de belirgin
  yük oluşturursa.
- Architecture testlerinin kaçırdığı bir sınır ihlali review'da yakalanırsa (testler
  güçlendirilir; tekrar ederse modül başına sabit proje ayrımı yeniden değerlendirilir).
- Linux/macOS ajanı ya da mobil uygulama kapsama alınırsa.
