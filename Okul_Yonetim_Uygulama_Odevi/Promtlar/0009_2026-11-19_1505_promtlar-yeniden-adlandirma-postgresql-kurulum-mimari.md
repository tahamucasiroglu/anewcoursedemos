# 0009 — Promtlar yeniden adlandırma · PostgreSQL kurulumu · Konu 4 onayı · Konu 5/12: Mimari

**Tarih:** 2026-11-19 15:05

## İstek

> 1- promtlar klasmründe sıra sayısı başta olsun tarihlerle karışabilir eklenenleride yeniden isimlendir. postgre için winget install PostgreSQL.PostgreSQL  yazdım cmd ye ama olmadı galiba sen kurarsın veritabanı postgre olsun bu tabloyu onaylıyorum

## Cevap

### 1. Promtlar dosya adları
Yeni düzen: `NNNN_YYYY-AA-GG_SSDD_kisa-konu.md` (sıra numarası başta). 0001–0008 yeniden adlandırıldı, `Promtlar/README.md` güncellendi.

### 2. PostgreSQL kurulumu — TAMAM
- Senin komutun neden olmadı: winget'te paket adı sürümlü, ör. `PostgreSQL.PostgreSQL.18`. Yalın `PostgreSQL.PostgreSQL` diye bir paket yok.
- Kurulan sürüm: **PostgreSQL 18.7**, winget'teki en güncel kararlı sürüm.
- Karşılaşılan sorun: Windows bölge adı `Turkish_Türkiye.1254` ASCII dışı karakter (ü) içerdiği için `initdb` hata verdi ve veritabanı kümesi oluşmadı. Ayrıca kaldırma işlemi arka planda sürerken yeniden kurulum başladı, bu da dosyaları karıştırdı.
- Çözüm: Temiz kaldırma yapıldı, ardından `--locale C` ile yeniden kuruldu. Sonuç:
  - Servis: `postgresql-x64-18` — **Running, Automatic** (Windows ile otomatik başlar)
  - Port: `5432`, kullanıcı: `postgres`, kodlama: **UTF8**, collation: `C`
  - `psql` ile bağlantı doğrulandı: `PostgreSQL 18.7 on x86_64-windows`
- Not: Türkçe alfabetik sıralama (ç, ğ, ı, ö, ş, ü) gerektiğinde ilgili kolonlarda ICU `tr-TR` collation kullanılır. Veritabanının varsayılanı `C` kalır; bu, e-posta gibi alanlarda "I/ı" büyük-küçük harf tuzağını önler.
- Süper kullanıcı şifresi yalnızca yerel geliştirme içindir ve repoya yazılmaz. Uygulama, bağlantı bilgisini `dotnet user-secrets` veya ortam değişkeninden okuyacak (Konu 9).

### 3. Konu 4 — Stack: ONAYLANDI
.NET 10 LTS · ASP.NET Core Web API · EF Core 10 + Npgsql · **PostgreSQL 18** · React 19 + TS + Vite (web) · React Native + Expo (mobil) · dış servisler port arkasında.

### 4. Konu 5/12 — Mimari ve modül sınırları (`docs/architecture.md`)

**Araştırma bulgusu:** `NArchitecture.Core.*` NuGet paketlerinin **2.0.0** sürümü 23 Ekim 2026'da yayınlandı ve **`net10.0`** hedefliyor. EF Core 10.0.13 ve FluentValidation 12.2 kullanıyor, kendi `NArchitecture.Core.Mediator`'ı var (ücretli MediatR'a bağımlı değil). Yani .NET 10 uyumu sorun değil.

**Önerim — nArchitecture katmanlı, modüler monolit:**

```
backend/
  src/
    Domain/          → varlıklar, değer nesneleri (hiçbir şeye bağımlı değil)
    Application/     → Features/<Modül>/Commands|Queries|Rules|Dtos + port arayüzleri
    Persistence/     → EF Core DbContext, konfigürasyonlar, migration'lar, repository'ler (Npgsql)
    Infrastructure/  → port uygulamaları: sahte ödeme/SMS/WhatsApp/push/AI, dosya depolama
    WebAPI/          → controller'lar, kimlik doğrulama, OpenAPI
  tests/             → Unit + Integration
web/                 → React yönetim paneli
mobile/              → Expo uygulaması (veli + öğretmen)
packages/api-client/ → OpenAPI'den üretilen ortak TypeScript istemcisi (web + mobil)
```

- **Çekirdek:** `NArchitecture.Core.*` 2.0.0 paketleri doğrudan kullanılır: CQRS/Mediator, pipeline'lar (doğrulama, yetki, log, transaction, cache), repository, dinamik sorgu, JWT/OTP güvenlik. Bu kod yeniden yazılmaz.
- **Modüller (Application/Features altında):** `Organization` (kurum, şube, sınıf, dönem, takvim, ders programı), `Students` (öğrenci, veli, kardeş, içe aktarma), `Attendance`, `Communication` (duyuru, mesaj, onay formu, dosya), `Academics` (ödev, not), `Billing` (ücret, taksit, tahsilat, cari, makbuz), `Appointments`, `Dashboard`, `Ai`, `Identity` (kullanıcı, rol, 2FA), `Compliance` (KVKK rıza, audit log, saklama).
- **Kiracı izolasyonu (S1):** Kiracıya ait her varlıkta `TenantId`, şubeye bağlı olanlarda ayrıca `BranchId` bulunur. Filtreleme EF Core global query filter ile otomatik yapılır; şube yetkisi pipeline'da kontrol edilir. Her iki kural da entegrasyon testiyle ispatlanır. PostgreSQL Row-Level Security ikinci savunma hattı olarak `specs/backlog.md`'ye eklenir; MVP'de zorunlu değil.

**Yasak bağımlılıklar (review'da kontrol edilir):**
1. `Domain` hiçbir projeye ve hiçbir altyapı paketine (EF Core dahil) bağımlı olmaz.
2. `Application`, `Persistence`, `Infrastructure` veya `WebAPI` projelerine bağımlı olmaz. Yalnızca kendi tanımladığı arayüzleri kullanır.
3. `WebAPI` controller'ları iş mantığı içermez ve `DbContext` kullanmaz. Yalnızca Command/Query gönderir.
4. Bir modül, başka bir modülün repository'sine veya tablosuna doğrudan erişmez. Erişim o modülün Query/Command'ı veya domain event'i üzerinden olur.
5. Dış servisler (ödeme, SMS, WhatsApp, push, AI, ses-metin) yalnızca `Application`'daki port arayüzleri üzerinden çağrılır. SDK'lar yalnızca `Infrastructure`'da yer alır.
6. `Ai` modülü veliye hiçbir şey göndermez. Yalnızca `AiDraft` üretir; gönderimi onaydan sonra `Communication` yapar.
7. `web/` ve `mobile/` yalnızca HTTP API üzerinden konuşur, yalnızca `packages/api-client` aracılığıyla. Veritabanına veya backend koduna erişmez.

Alternatifler: (a) nArchitecture desenini Core paketleri olmadan kendimiz yazmak (daha fazla kod, daha az bağımlılık); (b) ayrı mikroservisler (MVP için gereksiz karmaşıklık).

**Dokümanda istenen:**
- "Multi-tenant SaaS, tenant bazlı veri izolasyonu (nArchitecture tabanı)"
- "Rol ve yetki yönetimi, şube bazlı görünürlük"
- "Audit log: kim, hangi öğrenci kaydını, ne zaman görüntüledi/değiştirdi"
- "AI, ayrı bir modül değil; öğretmenin ve yöneticinin iş yükünü azaltan, her modüle gömülü bir yetenek"
- "Mobil: veli ve öğretmen için tek uygulama (iOS + Android)"
- 9 MVP modülü (kurum, öğrenci/veli, yoklama, iletişim, ödev/not, tahsilat, randevu, yönetici paneli, mobil)

Not: Doküman AI'yı "ayrı bir modül değil" diye tanımlıyor. Önerideki `Ai` modülü bir ürün modülü değil, ortak bir yetenek katmanı: taslak üretimi, kaynak gösterme, sağlayıcı portu. Her modül bu katmanı kendi ekranında kullanır.

Cevap bekleniyor: Mimari, modül listesi ve 7 yasak bağımlılık uygun mu? Core paketleri doğrudan kullanılsın mı?
