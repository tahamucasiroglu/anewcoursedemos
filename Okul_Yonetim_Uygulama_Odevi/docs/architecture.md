# Architecture

> Agents read this file before planning. Bootstrap 2026-11-19.

## System overview
Okul Yönetim Platformu, **nArchitecture tabanlı modüler monolit** bir .NET 10 Web API, bir React yönetim
paneli ve tek bir Expo mobil uygulamasından (veli + öğretmen) oluşur. Tek veritabanı PostgreSQL 18; kiracı
izolasyonu satır düzeyinde `TenantId`/`BranchId` + EF Core global query filter ile yapılır. Mikroservis yok:
tek ekip, MVP ve kapsam dokümanının "nArchitecture tabanı" kararı (ADR-0001). Dış servisler (ödeme, SMS,
WhatsApp, push, AI, ses→metin) port arayüzlerinin arkasındadır; MVP'de sahte uygulamalar çalışır.

**Stack ve sürümler** (kesin paket sürümleri walking skeleton planında sabitlenir; merkezi `Directory.Packages.props`):

| Katman | Seçim |
|---|---|
| Backend | .NET 10 (LTS), ASP.NET Core Web API, C# 14 |
| Çekirdek | `NArchitecture.Core.*` 2.0.0 (net10.0): Mediator/CQRS, pipeline'lar, repository, dinamik sorgu, güvenlik |
| Veri erişimi | EF Core 10 + Npgsql; snake_case isimlendirme |
| Veritabanı | PostgreSQL 18 (yerel: servis `postgresql-x64-18`, port 5432, locale C / UTF8) |
| Web paneli | React 19 + TypeScript + Vite |
| Mobil | React Native + Expo (TypeScript) |
| Ortak istemci | `packages/api-client` — OpenAPI'den üretilen TypeScript istemcisi |
| Ortak tema | `packages/theme` — açık/koyu renk sözlüğü (`tokens.json`), Sistem/Açık/Koyu seçim mantığı, kontrast hesabı (spec 0002) |

**Klasör düzeni**
```
backend/
  Directory.Build.props · Directory.Packages.props · .editorconfig · OkulYonetim.sln
  src/   Domain · Application · Persistence · Infrastructure · WebAPI
  tests/ Unit · Integration · Architecture
web/ · mobile/ · packages/api-client/ · packages/theme/
```

## Modules / components and ownership
Modüller `Application/Features/<Modül>` altında (Commands, Queries, Rules, Dtos); varlıklar `Domain`'de.

| Module | Single responsibility | Owns |
|---|---|---|
| Organization | Kurum, şube, sınıf seviyesi, sınıf, dönem, akademik takvim, ders programı | Tenant, Branch, GradeLevel, Classroom, Term, Timetable |
| Students | Öğrenci kartı, veli, kardeş bağı, nakil, Excel/e-Okul içe aktarma | Student, Guardian, Transfer |
| Attendance | Yoklama ve devamsızlık bildirimi tetikleme | Attendance |
| Communication | Duyuru, birebir mesaj, onay formu, dosya/fotoğraf paylaşımı, okundu bilgisi, sessiz mod; **veliye giden her şeyin tek çıkışı** | Announcement, Message, ConsentForm |
| Academics | Ödev, teslim durumu, not | Assignment, Grade |
| Billing | Ücret tarifesi, taksit planı, indirim/burs, tahsilat, cari, makbuz, gecikme hatırlatması | FeeSchedule, InstallmentPlan, Payment, Ledger, Receipt |
| Appointments | Öğretmen müsaitliği, veli randevu talebi | Availability, Appointment |
| Dashboard | Yönetici günlük özeti, temel raporlar, Excel dışa aktarma (yalnızca okur) | — |
| Ai | Ortak AI yeteneği: taslak üretimi, kaynak gösterme, sağlayıcı portu (sesli not → mesaj taslağı, haftalık gelişim özeti) | AiDraft |
| Identity | Kullanıcı, rol ataması, okul üyeliği (etkin/pasif), giriş, 2FA, token | User, RoleAssignment, TenantMembership |
| Administration | Kurucunun yönetim işlemleri (spec 0005): şube ekle/düzenle/pasifleştir, kişi ekle, rol ve şube ver/al, pasifleştir, şifre sıfırla. Organization ve Identity varlıklarını kendi komutlarıyla değiştirir; her işlem okul başına sıralı ve audit'li | — |
| Account | Kişinin kendi hesabı (spec 0006): profil (ad, telefon), şifremi değiştir, şifremi unuttum (yenileme bağlantısı + e-posta). Okul seçimi gerektirmez; yalnızca oturumdaki kişi | PasswordResetToken |
| Compliance | KVKK rıza kayıtları, audit log, saklama süresi ve anonimleştirme | ConsentRecord, AuditEntry |

"AI ayrı bir modül değil, her modüle gömülü yetenek" (kapsam dokümanı): `Ai` bir ürün modülü değil, diğer
modüllerin ekranlarında kullandığı ortak katmandır.

## Communication rules
- Modül içi: Command/Query → handler → repository.
- Modüller arası okuma: diğer modülün **Query**'si (Mediator) üzerinden; yazma etkisi: diğer modülün **Command**'ı
  veya domain event'i (ör. `StudentMarkedAbsent` → Communication bildirim taslağı/bildirimi).
- Dış dünya: yalnızca `Application`'da tanımlı port arayüzleri (`IPaymentProvider`, `ISmsSender`,
  `IMessagingChannel`, `IPushNotifier`, `IAiTextGenerator`, `ISpeechToText`, `IFileStorage`); uygulamaları `Infrastructure`'da.
- İstemciler (web, mobil): yalnızca HTTP API, yalnızca `packages/api-client` üzerinden.
- Oturum (spec 0004): istemciler oturumu `packages/api-client`'taki `AuthSession` ile yürütür (giriş, okul seçimi, 401'de
  tek yenileme, çıkış); web ve mobil aynı akış durumunu (`authFlow`) çizer, yalnızca yenileme anahtarının taşınması farklı
  (web: HttpOnly çerez, mobil: güvenli depo). Geliştirmede web `/api`'yi vite vekiliyle yerel API'ye iletir (Pi'deki
  gibi aynı adres). Kurum/şube duvarı Persistence'ta global query filter'dır ve oturumdan (`ICurrentSession`) beslenir;
  filtreyi delen okuma (`IgnoreQueryFilters`) yalnızca izinli iki depoda (architecture testi denetler).
- Yönetim (spec 0005): `api/admin/*` uçları `TenantFounder` politikasıyla korunur (kurucu bilgisi her istekte
  veritabanından); rol/üyelik/şube değiştiren her komut okul başına veritabanı kilidiyle sıraya girer. Web'de yönetim
  sayfası bağımlılıksız hash yönlendirmesiyle `#/yonetim` adresindedir; mobilde yönetim yok. Kişi araması ve şube adı
  karşılaştırması Türkçe harf kuralıyla uygulamada yapılır (okul başına kayıt sayısı küçük).
- E-posta (spec 0006): uygulama e-postayı `IEmailOutbox` kuyruğuna bırakıp hemen döner; arka plan gönderici
  `IEmailSender` ile yollar (SMTP ayarlıysa sunucuya, yalnızca geliştirmede klasöre, değilse "SMTP ayarlı değil" logu).
  Kuyruk bellektedir; uygulama yeniden başlarsa bekleyen e-posta kaybolur (kişi yeniden ister). Bağlantı adresleri
  `IAppLinks` (web adresi ayardan); yenileme anahtarı adresin `#` sonrasında, sunucuya ve loglara gitmez.
- Renkler (web, mobil): yalnızca `packages/theme` (`tokens.json`). Mobil doğrudan kullanır; web aynı değerleri
  `web/src/theme/theme.css` değişkenlerinde tutar (test birebir eşitliği doğrular). Tema seçimi yalnızca cihazda saklanır.
- Port çağrıları zaman aşımı + yeniden deneme ile yapılır; başarısızlık "beklemede" durumuna düşer, isteği çökertmez.

## Logging (ADR-0003)
- **Kapsamlı loglama** ilk spec'ten itibaren: Serilog (nArchitecture'ın log altyapısı), `ILogger<T>` ile.
- **Günlük dosyalar:** `logs/okul-api-YYYYMMDD.log` — her gün yeni dosya; tek dosya 100 MB'ı aşarsa aynı gün içinde
  bölünür; **30 gün** saklanır, eskisi otomatik silinir. Klasör yolu ayardan; `logs/` repoya girmez.
- **Seviye ayarlanabilir:** ayar dosyası / ortam değişkeni (`Serilog__MinimumLevel__Default`); uygulama yeniden
  derlenmeden değişir. Varsayılan **Information ve üstü**.
- **Biçim:** insanın okuyacağı tek satır: zaman (yerel saat + ofset), seviye, `traceId`, kaynak sınıf, mesaj,
  hata varsa exception. Geliştirmede konsola da yazılır.
- **Ne loglanır:** her HTTP isteği (yöntem, yol, durum kodu, süre, `traceId`, kiracı/kullanıcı **kimliği**);
  her Command'ın başlangıç/bitişi (nArchitecture logging pipeline); iş olayları (Information); iş kuralı ihlali
  ve dış servis aksaklığı (Warning); her beklenmeyen hata, bir kez, `traceId` ile (Error). Kullanıcıya gösterilen
  `traceId` log satırını bulmayı sağlar.
- **Ne loglanmaz:** istek/yanıt gövdesi, şifre, token, TC kimlik no, sağlık verisi, telefon/e-posta/adres
  (maskelenir) — `docs/security.md`.
- Web/mobil istemci hataları MVP'de yalnızca geliştirici konsolunda; sunucuya toplanması backlog'da.

## Runtime environments (ADR-0004)
- **Geliştirme:** Windows makine — API, web (Vite), PostgreSQL 18 yerel servis.
- **MVP deneme ortamı: Raspberry Pi** (yerel ağda, uzaktan erişim). **API ve web orada çalışır**; kısa süreli
  denemeler için kurulup kaldırılır. Bu yüzden: API `linux-arm64` için yayımlanabilir olmalı; tüm adres/port/
  bağlantı/log yolu ayarları ortam değişkeniyle verilir (koda veya Windows yoluna bağlı değil); web statik
  dosya olarak sunulur. Pi kurulumu ve dağıtımı spec 0003 ile geldi (`docs/deploy-pi.md`): web nginx ile ayrı birim
  (port 8080, `/api` içeride API'ye iletilir), API systemd servisi (`okul` kullanıcısı, yalnızca `127.0.0.1:5080`),
  Pi'deki PostgreSQL 17, kısıtlı veritabanı kullanıcısı `okul_app`; dağıtım/geri alma `tools/pi/`.
- Mobil uygulama, Pi'deki API'ye yerel ağ adresiyle bağlanır (adres ortam ayarından).

## Forbidden dependencies (make them testable)
`tests/Architecture` (ArchUnitNET) ve derleme kuralları bunları zorlar; hepsi `scripts/check` içinde.

1. `Domain` hiçbir projeye bağımlı olmaz; paket olarak yalnızca nArchitecture'ın temel varlık paketine
   (`NArchitecture.Core.Persistence`, `Entity<TId>`) referans verir. Domain **kodu** EF Core tiplerini kullanamaz
   (mimari test tip düzeyinde denetler) — ADR-0005.
2. `Application` → `Persistence` / `Infrastructure` / `WebAPI` bağımlılığı yasak; yalnızca kendi arayüzlerini kullanır.
3. `WebAPI` katmanı (controller'lar dahil) `OkulDbContext`'e, EF Core'a ve Npgsql'e bağımlı olmaz; controller'lar iş mantığı
   içermez, yalnızca Command/Query gönderir. Şema geçişi Persistence'taki `MigrateOkulDatabase()` ile çağrılır.
4. Bir modül başka bir modülün repository'sine, varlık konfigürasyonuna veya tablosuna doğrudan erişmez.
5. Dış servis SDK'ları (ödeme, SMS, WhatsApp, push, AI) yalnızca `Infrastructure`'da referans edilir.
6. `Ai` modülü veliye hiçbir şey göndermez (`Communication` portlarına bağımlı olmaz); yalnızca `AiDraft` üretir.
7. `web/` ve `mobile/` backend koduna veya veritabanına erişmez; HTTP'yi yalnızca `packages/api-client` ile kullanır.
8. **K1** Her `.cs` dosyasında tek tip; dosya adı = tip adı (StyleCop SA1402, SA1649 — error).
9. **K2** Uyarılar hatadır, tüm projelerde (`Directory.Build.props`: `TreatWarningsAsErrors`).
10. **K3** Her tipin başında kısa Türkçe `/// <summary>`; üyeler için ayrı summary yazılmaz (architecture testi XML belgesinden doğrular).
11. `IgnoreQueryFilters()` / ham SQL yalnızca izinli listedeki yerlerde (architecture testi; T5).
12. `web/` ve `mobile/` kodunda renk değeri (`#…`, `rgb(…)`, `hsl(…)`) yazılmaz; renk yalnızca ortak temadan (ESLint
    `no-restricted-syntax`; web CSS'i için `cssVariables.test.ts`).

## Deliberately out of scope
- Mikroservisler, ayrı veritabanları.
- Faz 2/3 modülleri (ölçme-değerlendirme, etüt, servis, yemek/sağlık, öğrenci uygulaması, rehberlik, personel,
  ön kayıt CRM, çok şubeli konsolide yönetim, markalı uygulama, pazaryeri) — kapılar geçilene kadar.
- Gerçek ödeme/SMS/WhatsApp entegrasyonu (portlar hazır; sağlayıcı yok — `specs/backlog.md`).
- e-Fatura/e-Arşiv, Google/Microsoft SSO, WhatsApp Business (Faz 2).
- Tam LMS / canlı ders, tam muhasebe / bordro, MEB süreçlerinin yerine geçmek, devlet okulları, veliler arası grup sohbeti (kapsam dokümanı "Kapsam dışı").
- Çoklu dil: MVP yalnızca Türkçe (metinler yine de YAML / `tr.json` dosyalarında).
- PostgreSQL Row-Level Security (ikinci savunma hattı — backlog).
