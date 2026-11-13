# Architecture

> Agents read this file before planning. Burada yazan, *karar verilmiş* mimaridir.

## System overview

Leave Manager, **katmanlı bir modüler monolittir**: tek bir .NET 10 çözümü (`LeaveManager.slnx`),
dört üretim projesi, sunucu tarafında render edilen ASP.NET Core MVC (Razor) arayüz ve SQLite
veritabanı. İş kuralları saf C# Domain katmanında izole edilir; arayüz düz HTML/CSS/JS'tir
(frontend framework'ü yok). Gerekçe: en az hareketli parça, tek araç zinciri (.NET SDK), iş
kurallarının milisaniyelik birim testleriyle kanıtlanması ve katman sınırlarının test edilebilir
olması — bkz. `docs/decisions/0001-stack-and-architecture.md`.

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `LeaveManager.Domain` | Varlıklar, değer nesneleri, iş kuralları (BR-1…BR-8) ve değişmezler | İzin talebi, durum geçişleri, bakiye, gün sayımı kavramları |
| `LeaveManager.Application` | Kullanım senaryoları (talep oluştur, onayla, reddet, iptal et, listele) ve kaynak bazlı yetki kontrolü; dış dünya için arayüzler (repository'ler, `TimeProvider` kullanımı, mevcut kullanıcı) | Senaryo akışları, yetki kararları |
| `LeaveManager.Infrastructure` | EF Core `DbContext`, SQLite, repository implementasyonları, ASP.NET Core Identity depolaması, seed verisi | Veritabanı şeması ve kalıcılık |
| `LeaveManager.Web` | MVC Controller'lar, Razor view'lar, ViewModel'ler, `wwwroot` (CSS/JS), kimlik doğrulama ve DI yapılandırması (`Program.cs`) | HTTP, HTML, cookie oturumu |

Test projeleri: `tests/LeaveManager.Domain.Tests`, `tests/LeaveManager.Application.Tests`,
`tests/LeaveManager.Web.Tests`, `tests/LeaveManager.ArchitectureTests` (`docs/testing.md`).

## Communication rules

- Akış tek yönlüdür: Controller → Application servisi → Domain. Doğrudan (senkron) metot çağrısı.
- Application, kalıcılığa yalnızca kendi tanımladığı arayüzler üzerinden erişir;
  implementasyonlar Infrastructure'dadır ve DI ile verilir.
- Event, mesaj kuyruğu, arka plan işi yok (v1). Gerekirse ADR ile eklenir.

## Forbidden dependencies (make them testable)

`LeaveManager.ArchitectureTests` (ArchUnitNET) her birini assert eder:

1. `Domain` hiçbir projeye ve hiçbir çerçeveye (EF Core, ASP.NET Core, Identity) bağımlı değildir.
2. `Application`, `Infrastructure`'a ve `Web`'e bağımlı değildir; yalnızca `Domain`'i ve kendi
   arayüzlerini bilir.
3. `Web` içinde `DbContext` ve `Microsoft.EntityFrameworkCore` tipleri kullanılmaz; veriye yalnızca
   `Application` servisleri üzerinden erişilir. `Web` → `Infrastructure` referansı yalnızca DI
   kaydı içindir (`Program.cs` / kayıt uzantı metodu).
4. Domain varlıkları view'a doğrudan gitmez: `Web` view'ları yalnızca `Web` içindeki ViewModel
   tiplerini model olarak alır.
5. `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now/UtcNow` üretim kodunda
   çağrılmaz; saat yalnızca enjekte edilen `TimeProvider` üzerinden okunur.

## Deliberately out of scope

v1 için bilinçli olarak yok: resmî tatil takvimi, e-posta / bildirim, çoklu şirket / kiracı,
SSO / harici kimlik sağlayıcı, kullanıcı kayıt ekranı ve admin paneli, REST API, yarım gün izin,
raporlama, PostgreSQL (SQLite yeterli; geçiş ADR konusudur), çoklu dil (arayüz yalnız Türkçe).
