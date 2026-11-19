# ADR 0001 — nArchitecture tabanlı modüler monolit, .NET 10 + PostgreSQL 18

- Status: Accepted
- Date: 2026-11-19

## Context
Kapsam dokümanı "Multi-tenant SaaS, tenant bazlı veri izolasyonu (nArchitecture tabanı)" istiyor. Tek ekip,
MVP odaklı; iOS + Android tek mobil uygulama, web yönetim paneli. `NArchitecture.Core.*` 2.0.0 (23.10.2026)
`net10.0` hedefliyor ve kendi Mediator'ını içeriyor (ücretli MediatR bağımlılığı yok). Makinede .NET 10 SDK var;
kullanıcı veritabanı olarak PostgreSQL istedi.

## Decision
Backend, `NArchitecture.Core.*` paketlerini doğrudan kullanan, Domain/Application/Persistence/Infrastructure/WebAPI
katmanlı **modüler monolit** .NET 10 Web API olarak; veritabanı PostgreSQL 18 (EF Core 10 + Npgsql); web React 19 +
Vite, mobil React Native + Expo olarak kurulur. Dış servisler port arayüzlerinin arkasındadır.

## Consequences
- Kazanç: CQRS, pipeline'lar (doğrulama, yetki, log, transaction), repository, JWT/OTP hazır; tek dağıtım birimi;
  modül sınırları architecture testleriyle korunur.
- Kazanç: PostgreSQL lisans ücretsiz, Türkiye'de barındırma yaygın; ileride RLS ile ikinci izolasyon hattı mümkün.
- Bedel: nArchitecture paket sürümlerine bağımlılık; dokümantasyonu sınırlı (README .NET 7 diyor, paketler net10.0).
- Bedel: Windows Türkçe locale'de PostgreSQL kurulum tuzağı (T2); yerel servis kurulumu gerekir (Docker yok).
- Bedel: web/mobil TypeScript, backend C#: iki dil.

## Alternatives considered
- nArchitecture desenini Core paketleri olmadan elle yazmak — daha çok kod, aynı sonuç; reddedildi.
- Mikroservisler — MVP ve tek ekip için gereksiz dağıtık karmaşıklık; kullanıcı açıkça reddetti.
- SQL Server 2025 (makinede kurulu, nArchitecture varsayılanı) — kullanıcı PostgreSQL tercih etti.
- Flutter (mobil) — ayrı dil (Dart), makinede kurulu değil; Expo web ile TS paylaşıyor.

## Revisit triggers
- `NArchitecture.Core` bakımı durursa veya bir .NET sürümünü desteklemezse.
- Faz 3 çok şubeli/çok kurumlu yük, tek veritabanının sınırını zorlarsa.
