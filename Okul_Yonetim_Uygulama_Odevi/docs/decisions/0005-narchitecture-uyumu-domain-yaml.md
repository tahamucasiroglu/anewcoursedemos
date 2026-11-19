# ADR 0005 — nArchitecture'a uyum: Domain'in temel varlık paketi, YAML metinleri, log adaptörü

- Status: Accepted
- Date: 2026-11-19

## Context
Plan 0001 sırasında üç noktada bootstrap kararları ile nArchitecture'ın kendi yapısı çakıştı:
(1) "Domain hiçbir pakete bağımlı olmaz (EF Core dahil)" kuralı — nArchitecture'da temel `Entity<TId>`,
EF Core'u da getiren `NArchitecture.Core.Persistence` paketinde ve Domain bu pakete bağlı;
(2) metinler için `.resx` kararı — nArchitecture YAML yerelleştirmesi kullanıyor;
(3) loglama — nArchitecture'ın hazır Serilog dosya logger'ı yalnızca klasör ayarı veriyor (ADR-0003'ün istediği
saklama süresi, boyut sınırı, seviye ve izleme numarası ayarlanamıyor). Kullanıcı kararı: "nArchitecture'a uyalım"
(2026-11-19, plan 0001 tur 1).

## Decision
(1) Domain yalnızca `NArchitecture.Core.Persistence` paketine referans verir; Domain kodunda EF Core tipleri
kullanılamaz — ArchUnitNET testi tip düzeyinde denetler. (2) Metinler nArchitecture YAML yerelleştirmesiyle:
`Application/Features/<Modül>/Resources/Locales/<ad>.<kültür>.yaml`; bölüm adı özellik klasörünün adıdır.
(3) nArchitecture'ın log arayüzü ve işlem log pipeline'ı kullanılır; arkasına kendi Serilog yapılandırmamız
`SerilogLoggerAdapter` (Infrastructure) ile bağlanır.

## Consequences
- Kazanç: nArchitecture'ın repository/dinamik sorgu/pipeline altyapısı engelsiz kullanılır; kod nArchitecture örnekleriyle aynı düzende.
- Bedel: Domain projesi derleme zamanında EF Core'u görür; yanlış kullanım derleyiciyle değil mimari testle yakalanır.
- Bedel: YAML bölüm adı = klasör adı kuralı bilinmezse metin yerine anahtar döner (testler yakalar).

## Alternatives considered
- Domain'de kendi `Entity` tabanımız — nArchitecture repository'leri onun `Entity<TId>`'sini istiyor; reddedildi.
- `.resx` + `IStringLocalizer` — nArchitecture'dan sapma; kullanıcı "fark yoksa YAML" dedi.
- nArchitecture'ın hazır dosya logger'ı — ADR-0003 ayarlarını sağlamıyor.

## Revisit triggers
- nArchitecture temel varlığı ayrı (EF Core'suz) bir pakete taşırsa → Domain kuralı yeniden sıkılaştırılır.
