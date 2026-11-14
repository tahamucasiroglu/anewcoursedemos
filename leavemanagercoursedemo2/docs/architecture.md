# Architecture

> Kararlaştırılan mimari (bootstrap, 2026-11-14). Planlamadan önce okunur.

## System overview
İzin yönetimi demosu; tek bir backend (ASP.NET Core Web API, .NET 10) ve tek bir SPA frontend (React 19 + Vite + TypeScript).
Backend **Clean Architecture** ile dört projeye ayrılır: iş kuralları (BR-1…BR-7) saf `Domain`'de durur, böylece veritabanı
olmadan birim testleriyle kanıtlanır; katman sınırlarını mimari testleri denetler. Veri SQLite dosyasında (EF Core 10) tutulur:
kurulum ve servis gerektirmez. Frontend API'yi HTTP/JSON üzerinden kullanır; kimlik doğrulama demo JWT ile yapılır.

## Directory layout
```
backend/
  LeaveManager.slnx (çözüm) · Directory.Build.props · Directory.Packages.props
  src/   LeaveManager.Domain · LeaveManager.Application · LeaveManager.Infrastructure · LeaveManager.Api
  tests/ LeaveManager.Domain.Tests · LeaveManager.Application.Tests · LeaveManager.ArchitectureTests
frontend/
  src/features/<özellik>/ · src/shared/
```

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `LeaveManager.Domain` | Varlıklar, değer nesneleri, iş kuralları ve değişmezler (BR-1…BR-7) | Talep, bakiye, izin türü, çalışan kavramları |
| `LeaveManager.Application` | Kullanım senaryoları (talep aç, onayla, reddet, iptal, listele), yetki kontrolü, port arayüzleri (repository, kimlik, saat) | Senaryo akışı ve yetki kararları |
| `LeaveManager.Infrastructure` | Port uygulamaları: EF Core + SQLite, parola hash, JWT üretimi, `TimeProvider` | Kalıcılık şeması, migration'lar |
| `LeaveManager.Api` | HTTP uç noktaları, DTO doğrulama, ProblemDetails eşlemesi, DI kurulumu | HTTP sözleşmesi |
| `frontend/src/features/*` | Bir ekran/özellik akışı (ör. `leave-requests`, `approvals`, `auth`) | O özelliğin UI durumu |
| `frontend/src/shared` | Ortak bileşenler, API istemcisi, tipler, yardımcılar | Ortak UI ve istemci kodu |

## Communication rules
- Api → Application: yalnız kullanım senaryosu servisleri/handler'ları çağrılır; Api domain nesnesini doğrudan değiştirmez.
- Application → Domain: doğrudan çağrı. Application → dış dünya: yalnız kendi tanımladığı arayüzler (port) üzerinden.
- Domain dışarıya hiçbir şey çağırmaz; olay/mesaj altyapısı yok (ilk sürüm).
- Frontend → backend: yalnız `shared/` içindeki API istemcisi üzerinden.

## Forbidden dependencies (make them testable)
Mimari testleri (`LeaveManager.ArchitectureTests`, NetArchTest) ilk dördünü; ESLint (`import/no-restricted-paths`) beşincisini denetler.
1. `Domain` başka hiçbir projeye ve altyapı paketine (`Microsoft.EntityFrameworkCore*`, `Microsoft.AspNetCore*`) bağımlı olamaz.
2. `Application` yalnız `Domain`'e bağlıdır; `Infrastructure` ve `Api`'ye bağımlı olamaz, EF Core/ASP.NET tiplerini kullanamaz.
3. `Infrastructure` → yalnız `Application` + `Domain`; `Api`'ye bağımlı olamaz.
4. `Api` → `Application` + `Infrastructure` (Infrastructure yalnız DI kurulumu için); `Api` içinde `DbContext` doğrudan kullanılmaz.
5. Frontend'de `features/<a>` başka bir `features/<b>`'den import edemez; ortak kod `shared/`'dadır. `shared/` hiçbir feature'ı import edemez.

## Deliberately out of scope
Resmî tatiller, yarım gün izin, yıl sonu devri, çok kademeli onay, departmanlar; refresh token ve harici kimlik sağlayıcı;
e-posta/bildirim; çoklu şirket; konteyner ve dağıtım; olay tabanlı iletişim.
