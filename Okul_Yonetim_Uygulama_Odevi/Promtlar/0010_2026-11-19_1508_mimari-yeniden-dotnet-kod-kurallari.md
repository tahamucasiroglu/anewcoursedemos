# 0010 — Konu 5/12 yeniden: Mimari + kullanıcının .NET kod kuralları

**Tarih:** 2026-11-19 15:08

## İstek

> bağımlılıklara kendim bir kural eklemek istiyorum. Benim içinde iyi oluyor. react bilgim yok sadece .net backend olarak incelerken kolay oluyor birde ona alıştım. 1- her dosyada bir nesne olacak. 1 interface olabilir başka olamaz. 1 class olur başka olamaz gibi 1 enum olabilir sadece filan. bunun sln kodu vardı galiba onu ekleyerek sağlarsın. 2- Uyarıları hata yap eklentisini kuralım sln ye yine tüm projelerde geçerli olsun 3- nesnelerin başına kendinin ve bizim anlayacağımız kısa ne summaryler yazılsın. bağımlılık kontrollere filan ekle. mikroservice düşünmüyorum monolit narch tarzı devam edelim. buna göre tekrar bi inceleyeyim burayı

## Cevap

Kararlar netleşti: **monolit, nArchitecture tarzı, mikroservis yok.** Senin 3 kuralın derleme sırasında **otomatik** uygulanacak; yani kural ihlal edilirse build kırılır ve `scripts/check` kırmızı olur. Kurallar ayrıca review kontrol listesine de girer.

### Senin kuralların nasıl uygulanır

"sln kodu" dediğin şey: Çözüm kökündeki **`Directory.Build.props`** dosyası ve **`.editorconfig`**. Bu iki dosya, altındaki **tüm** `.csproj` projelerine otomatik uygulanır. Yeni eklenen bir proje de kuralları kendiliğinden alır; projelere tek tek yazmaya gerek kalmaz.

| # | Kural | Nasıl zorlanır |
|---|---|---|
| K1 | **Her dosyada tek bir tip:** 1 class *veya* 1 interface *veya* 1 enum *veya* 1 record/struct. Dosya adı = tip adı. | **StyleCop.Analyzers** paketi (Directory.Build.props ile tüm projelere): `SA1402` (dosyada tek tip), `SA1649` (dosya adı = tip adı). `.editorconfig`'de **error** seviyesinde. Enum ve delegate de ayrı dosyaya gider (`SA1201`/`SA1402` ayarı). |
| K2 | **Uyarılar = hata** (tüm projeler) | `Directory.Build.props`: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` + `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` + `<Nullable>enable</Nullable>` + `<AnalysisLevel>latest-recommended</AnalysisLevel>`. Tek bir uyarı bile build'i kırar. |
| K3 | **Her tipin başında kısa `/// <summary>`** — insanın ve AI'ın anlayacağı, "bu ne işe yarar" cümlesi; **Türkçe** (doküman dili tr) | `<GenerateDocumentationFile>true</GenerateDocumentationFile>` + StyleCop `SA1600`/`SA1602` (belgelenmemiş öğe) **error**. Kapsam önerim: **tüm tipler** (class/interface/enum/record) + **public üyeler** (metot, property). Private alanlar hariç (gürültü olmasın). |

Örnek (K1+K3):
```csharp
// Application/Features/Students/Rules/StudentBusinessRules.cs
/// <summary>Öğrenci kaydıyla ilgili iş kurallarını kontrol eder (aynı dönemde çift kayıt, kurum sınırı).</summary>
public sealed class StudentBusinessRules { ... }
```

Ek notlar:
- Migration dosyaları ve OpenAPI'den üretilen kod gibi **otomatik üretilen dosyalar** `.editorconfig`'de `generated_code = true` ile işaretlenir ve K1/K3'ten muaf tutulur. Bunlar elle yazılmadığı için kuralı sağlayamazlar; muafiyet bu yüzden.
- **Web ve mobil (TypeScript) tarafı:** React bilmediğini söyledin. Bu yüzden oraya K2'nin karşılığını önerim: `tsc --strict` + `eslint --max-warnings 0` (uyarı = hata). Ayrıca "her dosyada tek bileşen" kuralı. Senin incelemen ağırlıklı olarak .NET backend'de olacak.

### Mimari (güncel)

```
backend/
  Directory.Build.props   → K1/K2/K3 + ortak ayarlar (tüm projeler)
  .editorconfig           → kural seviyeleri (error)
  OkulYonetim.sln
  src/  Domain · Application · Persistence · Infrastructure · WebAPI
  tests/ Unit · Integration · Architecture
web/ · mobile/ · packages/api-client/
```

- `NArchitecture.Core.*` 2.0.0 (net10.0) paketleri doğrudan kullanılır.
- Modüller: Organization, Students, Attendance, Communication, Academics, Billing, Appointments, Dashboard, Ai, Identity, Compliance.
- Kiracı izolasyonu: `TenantId`/`BranchId` + EF Core global query filter + testlerle ispat. PostgreSQL RLS backlog'a.

### Bağımlılık kuralları (güncel — 7 + senin 3'ün)

Mimari bağımlılıklar:
1. `Domain` hiçbir projeye ve altyapı paketine bağımlı olmaz.
2. `Application` → `Persistence`/`Infrastructure`/`WebAPI` bağımlılığı yasak. Yalnızca kendi arayüzlerini kullanır.
3. `WebAPI` controller'ında iş mantığı ve `DbContext` yasak. Controller yalnızca Command/Query gönderir.
4. Bir modül başka bir modülün repository'sine veya tablosuna doğrudan erişemez.
5. Dış servis SDK'ları yalnızca `Infrastructure` içinde olur.
6. `Ai` modülü veliye gönderemez. Yalnızca `AiDraft` üretir.
7. `web/` ve `mobile/` yalnızca `packages/api-client` üzerinden HTTP API'ye erişir.

Kod kuralları (senin):

8. **K1** Dosya başına tek tip, dosya adı = tip adı.
9. **K2** Uyarılar hatadır, tüm projelerde.
10. **K3** Her tipte ve her public üyede kısa Türkçe `<summary>`.

**Nasıl kontrol edilir:**
- 8–10 derleyici ve analizörlerle zorlanır (build kırılır).
- 1–5 ve 7, **`tests/Architecture`** projesindeki mimari testlerle zorlanır (**NetArchTest** kütüphanesi: "Domain, EntityFrameworkCore'a bağımlı olmamalı" gibi testler). Kural bozulursa test kırmızı olur.
- 6, Communication/Ai testleriyle ve review kontrol listesiyle kontrol edilir.
- Hepsi `scripts/check` içinde çalışır.

**Dokümanda istenen:**
- "Multi-tenant SaaS, tenant bazlı veri izolasyonu (**nArchitecture tabanı**)"
- "Rol ve yetki yönetimi, şube bazlı görünürlük"; "Audit log"
- "AI … her modüle gömülü bir yetenek"; "AI hiçbir mesajı onaysız veliye göndermez"
- "Mobil: veli ve öğretmen için tek uygulama"
- Doküman kod kuralı (dosya düzeni, uyarı politikası, yorum) belirtmiyor. K1–K3 senin eklediğin proje kuralları.

Cevap bekleniyor: Güncel mimari ve 10 kural uygun mu? K3 kapsamı (tüm tipler + public üyeler, Türkçe) doğru mu?
