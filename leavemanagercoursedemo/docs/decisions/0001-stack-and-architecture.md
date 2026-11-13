# ADR 0001 — .NET 10 + ASP.NET Core MVC + SQLite, katmanlı modüler monolit

- Status: Accepted
- Date: 2026-11-07
- Revised: 2026-11-07 — mimari test kütüphanesi NetArchTest → ArchUnitNET (NetArchTest 2021'den beri bakımsız, `docs/security.md` Dependencies; plan 0001 Risk 1); karar aynı: katman sınırları bir mimari test kütüphanesiyle test edilir

## Context
Leave Manager bir kurs demosudur: ANEW iş akışını küçük ama gerçekçi bir izin yönetimi alanında
göstermelidir. `scripts/check` her makinede, harici servis olmadan, hızlı ve kalıntısız çalışmalı;
iş kuralları ve yetkilendirme (hassas alanlar) şeffaf biçimde test edilebilmelidir. Tek geliştirici,
düşük operasyonel risk.

## Decision
.NET 10 LTS üzerinde ASP.NET Core MVC (Razor view, düz HTML/CSS/JS), EF Core 10 + SQLite ve
ASP.NET Core Identity kullanırız; çözüm dört katmanlı bir modüler monolittir (Domain, Application,
Infrastructure, Web) ve katman sınırları bir mimari test kütüphanesiyle (ArchUnitNET) test edilir.

## Consequences
- Tek araç zinciri (.NET SDK); Node/npm yok, check yalnızca `dotnet` adımlarıdır.
- İş kuralları saf Domain'de milisaniyelik birim testleriyle kanıtlanır; UI kriterleri
  `WebApplicationFactory` + AngleSharp ile dönen HTML üzerinden — e2e gerekmez.
- SQLite bellek içi veritabanı her test için temiz durum verir (Resource hygiene kural 5).
- Bedeli: zengin istemci etkileşimi (anlık doğrulama, SPA hissi) yoktur; SQLite eşzamanlı yazma ve
  bazı tiplerde (`DateTimeOffset` sıralama) sınırlıdır (`docs/conventions.md`, Known traps);
  dört proje, tek projeye göre daha fazla iskelet demektir.

## Alternatives considered
- **React + Vite + TypeScript frontend:** ikinci araç zinciri (Node), ikinci test çerçevesi ve daha
  uzun check; kursun odağını ANEW'den uzaklaştırır.
- **Blazor (.NET 10):** tek dil ve bUnit avantajı var; ancak render mode seçimi (Static SSR / Server /
  WASM), SignalR devreleri ve iki yerde yetkilendirme kurs için gereksiz karmaşa.
- **Angular frontend:** daha ağır öğrenme eğrisi ve build.
- **PostgreSQL:** gerçekçi ama check için container runtime gerektirir; v1 için kazanç yok.
- **Tek Web projesi + Domain klasörü:** daha az iskelet, ama sınırlar konvansiyona kalır ve mimari
  test öğretilemez.

## Revisit triggers
- Zengin istemci etkileşimi gerektiren bir özellik (→ Blazor bileşeni veya küçük JS modülü ADR'si).
- Çoklu kullanıcıyla eşzamanlı yazma veya gerçek dağıtım ihtiyacı (→ PostgreSQL).
- Harici istemci (mobil, entegrasyon) ihtiyacı (→ REST API katmanı).
