# ADR 0003 — Kapsamlı loglama, günlük dosyalar, ayarlanabilir seviye

- Status: Accepted
- Date: 2026-11-19

## Context
Kullanıcı, geliştirme sırasında hataların hızlı bulunup çözülmesi için ilk spec'ten itibaren kapsamlı loglama
istedi: loglar günlük dosyalara bölünerek tutulacak, seviye ayarlanabilir olacak, şimdilik Information ve üstü
kaydedilecek, kodun içinde de loglama yapılacak. nArchitecture zaten Serilog tabanlı log altyapısı ve bir
logging pipeline'ı içeriyor. Uygulama hem Windows'ta hem Raspberry Pi'de (ADR-0004) çalışacak.

## Decision
Serilog; dosya hedefi günlük döner (`logs/okul-api-YYYYMMDD.log`, 100 MB'ta aynı gün içinde bölünür, 30 gün
saklama), konsol hedefi geliştirmede; minimum seviye ayar/ortam değişkeninden, varsayılan Information. Her HTTP
isteği, her Command, iş olayları ve beklenmeyen hatalar (`traceId` ile) loglanır; kişisel veri loglanmaz
(`docs/conventions.md` "Logging", `docs/security.md`).

## Consequences
- Kazanç: kullanıcı ekranındaki `traceId` ile ilgili log satırı saniyede bulunur; seviye yeniden derlemeden açılır.
- Kazanç: günlük dosya + saklama süresi diski (özellikle Pi'nin SD kartını) doldurmaz.
- Bedel: her handler'da log yazma disiplini (review'da kontrol); log hacmi Information seviyesinde artar.
- Bedel: kişisel veri sızıntısı riski — maskeleme kuralı ve review lensi ile yönetilir.

## Alternatives considered
- Yalnızca konsol logu — Pi'de kalıcı değil, geriye dönük arama yok; reddedildi.
- JSON (yapısal) dosya formatı — makineyle aranması kolay ama elle okuması zor; MVP'de insan okur → düz metin.
  Merkezi log aracı (Seq, Elasticsearch) gelince yeniden değerlendirilir.
- Veritabanına log yazmak — veritabanı arızasında log da kaybolur; reddedildi.

## Revisit triggers
- Birden fazla sunucu/ortam olursa (merkezi log toplama ihtiyacı).
- Log hacmi disk veya performans sorunu yaratırsa.
