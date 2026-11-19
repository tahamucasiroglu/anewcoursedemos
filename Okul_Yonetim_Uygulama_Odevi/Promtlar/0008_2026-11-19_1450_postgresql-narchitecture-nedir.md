# 0008 — Konu 4: PostgreSQL olur mu? · nArchitecture nedir?

**Tarih:** 2026-11-19 14:50

## İstek

> veritabanını postgre yapsak olur mu birde bana narchitecture nedir anlatırmısın kısaca

## Cevap

### PostgreSQL olur mu? — Evet, olur.

- EF Core, PostgreSQL'i resmi sağlayıcısı **Npgsql** ile tam destekler. nArchitecture da EF Core üzerine kurulu; SQL Server yalnızca varsayılan seçim. Sağlayıcı değişikliği tek bir yerde (Persistence katmanı) yapılır.
- Artıları: açık kaynak ve lisans ücreti yok; Türkiye'deki barındırma firmalarında ve bulutta yaygın ve ucuz; JSONB, güçlü indeksleme, **satır düzeyi güvenlik (Row-Level Security)** var. RLS, kurum izolasyonu (S1) için ikinci bir savunma hattı olabilir.
- Eksileri: Makinede kurulu değil (Docker da yok). Kurulum gerekir.
- Kurulum önerim: **PostgreSQL'in en güncel kararlı majör sürümü, Windows'a yerel kurulum** (`winget install PostgreSQL.PostgreSQL`). Docker yok, bu yüzden yerel kurulum en kısa yol. Kesin sürüm walking skeleton planında sabitlenir. `scripts/check.conf` içinde `requires` satırıyla "Postgres çalışıyor mu" kontrolü olur.
- Testlerde: Entegrasyon testleri gerçek PostgreSQL'e karşı, her test koşusu için ayrı/temizlenen bir test veritabanıyla çalışır (ayrıntısı Konu 7 Test'te).

### nArchitecture nedir? (kısaca)

**Kodlama.io'nun (Engin Demiroğ ekibi) açık kaynak .NET proje iskeleti.** Clean Architecture'dan esinlenmiş bir **monolit** başlangıç projesi. "Her projede tekrar yazılan altyapıyı" hazır verir.

**Katmanları** (bağımlılık içe doğru akar):
```
WebAPI          → HTTP uç noktaları (controller'lar)
Infrastructure  → dış servis adaptörleri (e-posta, dosya, ödeme…)
Persistence     → EF Core, DbContext, repository'ler, migration'lar
Application     → iş mantığı: Command/Query + handler, kurallar, doğrulama
Domain          → varlıklar (Entity) — hiçbir şeye bağımlı değil
```

**Hazır gelenler:**
- **CQRS:** Her işlem ya bir *Command* (değiştirir) ya da bir *Query* (okur). Her birinin kendi handler'ı olur (MediatR deseni).
- **Pipeline davranışları:** Doğrulama (FluentValidation), yetki kontrolü, loglama, önbellek, transaction. Her istekte otomatik çalışır, handler'a tek tek yazılmaz.
- **Business Rules sınıfları:** "Bu öğrenci zaten kayıtlı mı?" gibi kurallar ayrı ve test edilebilir.
- **Gelişmiş repository + dinamik sorgu:** Sayfalama, filtreleme, sıralama hazır.
- **Kimlik:** JWT, refresh token, OTP/2FA, Google/Microsoft girişi, rol tabanlı yetki.
- **Altyapı:** Serilog loglama, Redis önbellek, Elasticsearch, global hata yönetimi.
- **Kod üretici (nArchitecture.Gen):** Bir varlık için CRUD katmanlarını tek komutla üretir.

**Bizim için anlamı:** Doküman "nArchitecture tabanı" istiyor; yani katman yapısı, CQRS ve pipeline yaklaşımı bu projenin iskeleti olacak. **Core paketlerini doğrudan mı kullanacağız, yoksa aynı deseni kendi kodumuzla mı kuracağız?** Bu karar Konu 5 Mimari'de sorulacak. Not: Repo README'si hâlâ .NET 7 diyor, .NET 10 uyumu orada doğrulanmalı.

### Güncellenmiş stack önerisi

| Katman | Öneri |
|---|---|
| Backend | .NET 10 (LTS) + ASP.NET Core Web API, C# 14 |
| Veri erişimi | EF Core 10 + **Npgsql** |
| Veritabanı | **PostgreSQL (güncel kararlı majör), yerel Windows kurulumu** |
| Web paneli | React 19 + TypeScript + Vite |
| Mobil | React Native + Expo (TypeScript) |
| Dış servisler | Port arkasında; MVP'de sahte uygulama |

**Dokümanda istenen:** Veritabanı adı geçmiyor. İstenen: "Multi-tenant SaaS, tenant bazlı veri izolasyonu (nArchitecture tabanı)", "Yedekleme, Türkiye'de barındırma seçeneği". PostgreSQL ikisiyle de uyumlu.

Cevap bekleniyor: Güncel stack tablosu (PostgreSQL + Expo) onaylıyor musun?
