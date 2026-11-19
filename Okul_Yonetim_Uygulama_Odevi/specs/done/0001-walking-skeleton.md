# Spec 0001 — Walking skeleton (yürüyen iskelet)

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done

## Intent
Okul Yönetim Platformu'nun ilk özelliği yazılmadan önce, sistemin tüm katmanlarının uçtan uca birbirine
bağlandığını ve otomatik kontrolün (`scripts/check`) gerçekten derleyip test ettiğini göstermek istiyoruz.
Başarı: Web panelinde ve mobil uygulamada "sistem çalışıyor" bilgisi, API üzerinden veritabanına kadar giden
gerçek bir kontrolün sonucuyla görünür; proje kuralları (dosya başına tek tip, uyarıların hata sayılması, her
tipin başında Türkçe açıklama, yasak bağımlılıklar) ilk koddan itibaren otomatik zorlanır; `scripts/check`
iki hızda (fast ve full) yeşil olur ve arkasında çalışan süreç bırakmaz. Bilinçli olarak yapılmayan: hiçbir iş
özelliği (giriş, kurum, öğrenci, yoklama), kimlik doğrulama, iş tablosu, gerçek dış servis bağlantısı.

## Requirements
1. **Sistem durumu sorgusu:** Sistem, kimlik doğrulaması gerektirmeyen tek bir "sistem durumu" sorgusu sunar.
   Sonuç yalnızca iki durumdan biridir: "çalışıyor" (veritabanına ulaşıldı) veya "hizmet dışı" (veritabanına
   ulaşılamadı). Sonuçta sürüm, sunucu adı, bağlantı bilgisi, hata ayrıntısı gibi hiçbir iç bilgi bulunmaz.
2. **Türkçe metin kaynağı:** Durum mesajları koda gömülü değildir; Türkçe metin kaynak dosyasından gelir
   (backend'de kaynak tablosu, web/mobilde Türkçe metin dosyası).
3. **Web paneli:** Açıldığında sistem durumunu sorgular ve sonucu Türkçe gösterir.
4. **Mobil uygulama (veli + öğretmen için tek uygulama):** Açıldığında sistem durumunu sorgular ve sonucu
   Türkçe gösterir.
5. **Ortak istemci:** Web ve mobil, sistem durumuna aynı ortak istemci üzerinden ulaşır; backend koduna veya
   veritabanına doğrudan erişmez.
6. **Proje kuralları otomatik zorlanır** (ihlal → derleme veya test kırmızı):
   - Dosya başına tek tip; dosya adı tip adıyla aynı.
   - Tüm projelerde uyarılar hata sayılır.
   - Her tipin başında boş olmayan Türkçe açıklama (summary) bulunur; üyeler için zorunlu değildir.
   - Yasak bağımlılıklar: Domain hiçbir şeye bağımlı değil; Application alt katmanlara bağımlı değil; API
     katmanı veritabanı bağlamına doğrudan bağımlı değil; dış servis kütüphaneleri yalnızca Infrastructure'da.
   - Web/mobil: tip kontrolü sıkı modda; lint uyarısı hata sayılır.
7. **Test veritabanı yaşam döngüsü:** Veritabanına dokunan testler her koşuda kendine ait yeni bir test
   veritabanı açar, şema geçişlerini uygular, koşu sonunda siler. Önceki yarıda kalmış koşulardan artakalan
   test veritabanları temizlenir. Geliştirme veritabanına hiç dokunulmaz.
8. **Şema geçişi (migration) altyapısı:** İş tablosu içermeyen ilk şema geçişi bulunur; geliştirme ve test
   veritabanları bununla kurulabilir.
9. **Loglama altyapısı (ADR-0003):** API çalışırken her isteği (yöntem, yol, durum, süre, izleme numarası) ve
   her beklenmeyen hatayı (izleme numarasıyla, bir kez) loglar. Loglar günlük dosyalara bölünür, 30 gün
   saklanır; seviye yeniden derlemeden ayardan değişir, varsayılan Information ve üstü. Sistem durumu
   sorgusunun sonucu da loglanır ("hizmet dışı" Warning olarak). Kişisel veri ve istek gövdesi loglanmaz.
10. **Ortamdan bağımsız ayarlar (ADR-0004, Raspberry Pi hazırlığı):** API adresi/portu, veritabanı bağlantısı,
   web'in izinli adresi (CORS) ve log klasörü ortam ayarından verilir; web ve mobil API adresini ortam
   ayarından alır. Koda gömülü adres veya Windows'a özgü yol yoktur.
11. **Otomatik kontrol:** `scripts/check` önkoşulları (araçlar, PostgreSQL ayakta mı) kontrol eder; hızlı
   profil derleme + birim + mimari + web/mobil testlerini, tam profil bunlara ek olarak veritabanı testlerini
   ve paket güvenlik açığı taramasını çalıştırır. İki profil de arkada çalışan süreç bırakmaz.

## User experience
- **Flow:** Kullanıcı web panelini (tarayıcı) veya mobil uygulamayı açar → uygulama sistem durumunu sorgular →
  sonucu görür. Başka etkileşim yok.
- **States** (web ve mobil için aynı):
  - **Yükleniyor:** "Sistem durumu kontrol ediliyor…"
  - **Başarılı:** "Sistem çalışıyor" (olumlu işaretle)
  - **Hizmet dışı:** "Sistem şu anda hizmet dışı" (API yanıt verdi ama veritabanına ulaşamadı)
  - **Hata (ulaşılamıyor):** "Sunucuya ulaşılamıyor. Lütfen daha sonra tekrar deneyin." (API'ye ulaşılamadı)
  - "Hizmet dışı" ve "ulaşılamıyor" durumlarında **"Tekrar dene"** düğmesi; basınca yeniden "yükleniyor"a döner.
  - Boş durum ve yetkisiz durum: yok (veri listesi yok, sorgu anonim).
- **Design reference:** none — the flow above is the design. (Ürün adı başlıkta "Okul Yönetim Platformu".)
- **Accessibility:** Durum metni ekran okuyucuya okunabilir (canlı bölge / erişilebilirlik etiketi); durum
  yalnızca renkle değil metinle de anlatılır.

## Constraints & out of scope
- Teknoloji kararları sabittir (ADR-0001): .NET 10 + `NArchitecture.Core` 2.0.0, EF Core 10 + Npgsql,
  PostgreSQL 18, React 19 + Vite, React Native + Expo, TypeScript.
- Sistem durumu: "hizmet dışı" yanıtı **503** durum koduyla döner; veritabanı kontrolü en fazla **3 saniye**
  bekler, aşarsa "hizmet dışı" sayılır.
- Web ve mobil API adresini ortam ayar dosyasından (`.env`) alır; varsayılan yerel adres. API yalnızca ayarda
  tanımlı web adres(ler)inden tarayıcı isteğine izin verir (CORS, varsayılan reddet).
- Pi'ye kurulum ve dağıtım bu spec'te değil, sonraki spec'te (0002); bu spec yalnızca ortamdan bağımsızlığı sağlar.
- Sistem durumu sorgusu bilinçli olarak anonimdir (`docs/security.md` "varsayılan reddet" kuralının açıkça
  listelenen istisnası); iç bilgi sızdırmaz.
- Sensitive area: none — kişisel veri, kimlik, para, veliye iletişim veya kiracı verisi yok.
- Yeni paketler (NuGet/npm) planda listelenir ve plan onayıyla onaylanır (`docs/security.md` Dependencies).
- Mobil doğrulama: davranış bileşen testleriyle; ekran görüntüsü Expo'nun web çıktısından alınır. Gerçek
  cihazda deneme insan tarafından isteğe bağlıdır.
- Ekran görüntüleri ve Promtlar kaynakları `Promtlar/Sources/` altında saklanır (kullanıcı kararı; ANEW
  kanıtı değil, sohbet arşivi — kanıt tablosu testlere ve `scripts/check`'e referans verir).
- **Out of scope:** giriş/kimlik, kurum/şube/öğrenci, iş tabloları, kiracı filtresi (ilk kiracıya ait tabloyla
  gelecek), gerçek dış servisler (portlar ilgili özelliklerde), push bildirim, mağaza yayını, Docker, CI.
- Split: proposed 0001a backend + kontrol / 0001b web + mobil + ortak istemci; kept as one — tahamucasiroglu,
  2026-11-19, iskeletin amacı uçtan uca bağlantıyı tek seferde kanıtlamak.
- Hızlı profil süresi kriter değildir; plan/build'de 3 koşunun ortancası raporlanır.

## Acceptance criteria
- [x] AC-1 — Veritabanı ayaktayken sistem durumu sorgusu "çalışıyor" sonucunu ve kaynak dosyasındaki Türkçe mesajı döndürür; yanıtta sürüm, sunucu, bağlantı veya hata ayrıntısı bulunmaz.
- [x] AC-2 — Veritabanına ulaşılamadığında veya 3 saniyede yanıt alınamadığında sorgu "hizmet dışı" sonucunu 503 durum koduyla ve Türkçe mesajla döndürür; iç hata ayrıntısı yanıtta yer almaz.
- [x] AC-3 — Web paneli dört durumu doğru metinle gösterir: yükleniyor, çalışıyor, hizmet dışı, sunucuya ulaşılamıyor; son ikisinde "Tekrar dene" yeniden sorgular.
- [x] AC-4 — Mobil uygulama aynı dört durumu ve "Tekrar dene" davranışını gösterir.
- [x] AC-5 — Web ve mobil durumu yalnızca ortak istemci üzerinden sorgular; ortak istemci API tanımından üretilir.
- [x] AC-6 — Bir dosyaya ikinci bir tip eklemek veya dosya adını tip adından farklı yapmak derlemeyi kırar.
- [x] AC-7 — Herhangi bir derleme uyarısı (ör. kullanılmayan değişken) derlemeyi kırar — tüm backend projelerinde.
- [x] AC-8 — Açıklaması (summary) olmayan veya boş olan bir tip mimari testi kırmızıya düşürür; üye açıklaması istenmez.
- [x] AC-9 — Yasak bağımlılıklardan birini eklemek (ör. Domain'den EF Core'a referans) mimari testi kırmızıya düşürür.
- [x] AC-10 — Veritabanı testleri kendi test veritabanını açar ve koşu sonunda siler; önceden kalmış test veritabanları temizlenir; geliştirme veritabanı değişmez.
- [x] AC-11 — `./scripts/check --fast` ve `./scripts/check` yeşildir; önkoşul eksikse (ör. PostgreSQL kapalı) kırmızıdır ve hangi önkoşulun eksik olduğunu söyler.
- [x] AC-12 — Residue testi iki profilde de temizdir (kontrol sonrası arkada çalışan süreç kalmaz).
- [x] AC-13 — API çalışırken her istek, o günün tarihli log dosyasına yöntem, yol, durum kodu, süre ve izleme numarasıyla yazılır; beklenmeyen bir hata, kullanıcıya dönen izleme numarasıyla aynı numarayla bir kez Error olarak yazılır; istek gövdesi yazılmaz.
- [x] AC-14 — Log seviyesi ayardan değiştirilebilir: varsayılanda Debug satırları yazılmaz; seviye Debug yapılınca yazılır. Log klasörü ayardan verilir; 30 günden eski dosyalar tutulmaz.
- [x] AC-15 — Tarayıcı istekleri yalnızca ayarda tanımlı web adresinden kabul edilir; başka bir adresten gelen istek izin almaz. Web ve mobil API adresini ortam ayarından alır.

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 0 (onaydan sonra; loglama ve ortam gereksinimleri onaydan önce eklendi) |
| Fix rounds | 3 (fix-1: review round 1, fix-2: review round 2, fix-3: VERIFY V-1) |
| Review findings: real / noise / deferred | 19 / 1 / 3 (round 1: 16/0/2, round 2: 3/1/1); VERIFY: 1 real |
| Regressions introduced | 0 |
| Bugs escaped to production | 0 (henüz üretim yok) |

## Verification evidence
VERIFY 2026-11-19, QA (lite, ana oturum). Tam yeşil: `CHECK GREEN (full, 10 steps, 75 s) @085f19f`; ship commit'i ayrıca tam koşuyla doğrulandı.
Kasıtlı bozmalar repo dışı geçici kopyada yapıldı, hiçbiri commit edilmedi (betikler: `Promtlar/Sources/0001/kaynak/`).

| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-1 | `SystemHealthEndpointTests.cs::Get_DatabaseUp_ReturnsHealthyWithTurkishMessage`, `GetSystemHealthQueryHandlerTests.cs::Handle_DatabaseReachable_ReturnsHealthyWithTurkishMessage` | PASS |
| AC-2 | `SystemHealthEndpointTests.cs::Get_DatabaseUnreachable_Returns503WithoutDetails`; `Handle_ProbeTimesOut_ReturnsUnavailable` + `Handle_ProbeAnswersJustBeforeTimeout_ReturnsHealthy` (spec değeri 3 sn); bozma: süre 30 sn → ilk test kırmızı, 2 sn → ikinci test kırmızı (fix-3 sonrası) | PASS |
| AC-3 | `web/src/features/system-status/SystemStatus.test.tsx` (9 test); ekran S-1…S-4 web | PASS |
| AC-4 | `mobile/src/features/system-status/SystemStatusScreen.test.tsx` (9 test); ekran S-1…S-4 mobil (Expo web çıktısı) | PASS |
| AC-5 | ESLint `no-restricted-globals` / `no-restricted-properties` (deneme dosyasıyla kırmızı görüldü); `tools/check/api-client-fresh.mjs` (fix-1'de eski istemciyi yakaladı) | PASS |
| AC-6 | Bozma: dosyaya ikinci enum → `SA1402`; ikinci record → `SA1402`; dosya adı ≠ tip adı → `SA1649` (derleme kırıldı) | PASS |
| AC-7 | Bozma: kullanılmayan değişken Domain'de ve Unit test projesinde → `CS0219` (derleme kırıldı) | PASS |
| AC-8 | Bozma: boş summary ve silinmiş summary → `TypeSummaryTests.cs::EveryType_HasNonEmptySummary` kırmızı | PASS |
| AC-9 | Bozma: API → OkulDbContext (`WebApi_DoesNotTouchDbContextOrEfCore`), Application → Serilog (`Application_DoesNotDependOnLowerLayersOrExternalSdks`), Domain → EF Core (`Domain_DoesNotUseEfCoreOrOuterLayers`) kırmızı | PASS |
| AC-10 | `TestDatabaseLifecycleTests.cs::Run_CreatesOwnDatabase_DropsItAndRemovesLeftovers` (eşzamanlı koşunun veritabanı korunur) | PASS |
| AC-11 | Tam/hızlı check yeşil; `OKUL_DB_PORT=5999 ./scripts/check` → `CHECK FAILED (full) prerequisite not met: node tools/check/pg-ready.mjs` + "PostgreSQL'e ulaşılamıyor (localhost:5999)" | PASS |
| AC-12 | `RESIDUE CLEAN (full, 2 runs)` ve `(fast, 2 runs)` @5ae5f6b; sonrasında `check.conf` değişmedi | PASS |
| AC-13 | `RequestLoggingTests.cs::Request_WritesDailyFileLine_WithTraceId_NoBody`, `RequestLoggingTests.cs::UnhandledError_LoggedOnce_WithResponseTraceId`, `RequestCancellationTests.cs::ClientAbort_IsLoggedAsInformation_NotError` | PASS |
| AC-14 | `LoggingSetupTests.cs::DefaultLevel_IsInformation_DebugSuppressed`, `ConfiguredDebug_WritesDebug`, `FileSink_Retains30Days_DailyRolling` | PASS |
| AC-15 | `CorsTests.cs::AllowedOrigin_GetsCorsHeader_OtherOrigin_DoesNot`; `web/src/config.test.ts`, `mobile/src/config.test.ts` | PASS |

Ekran görüntüleri S-1 yükleniyor · S-2 çalışıyor · S-3 hizmet dışı · S-4 ulaşılamıyor (web 1280×720, mobil 414×800; sahte API, başsız Chrome) — agent-captured, judged by tahamucasiroglu at SHIP, 2026-11-19. Kullanıcı isteğiyle `Promtlar/Sources/0001/` altında saklanır.
