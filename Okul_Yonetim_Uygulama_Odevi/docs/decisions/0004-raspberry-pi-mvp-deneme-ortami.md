# ADR 0004 — Raspberry Pi, MVP deneme ortamı

- Status: Accepted
- Date: 2026-11-19
- Revised: 2026-11-20 — açık sorular spec 0003 ile cevaplandı (Pi 5 8 GB, Debian 13 64-bit; Pi'deki PostgreSQL 17; SSH `tmpi`; web nginx ile ayrı birim)

## Context
Kullanıcının bir Raspberry Pi'si var. MVP için kısa süreli denemelerde API ve web bu cihaza kurulacak ve yerel
ağdan uzaktan erişilerek test edilecek. Geliştirme Windows makinede sürüyor. Remote/CI yok (ADR-0002).

## Decision
MVP deneme ortamı Raspberry Pi'dir: API (.NET 10, `linux-arm64`) ve web (statik dosyalar) Pi'de çalışır.
Uygulama ortamdan bağımsız yazılır: adres, port, bağlantı cümlesi, CORS izinli adresi ve log klasörü ortam
değişkeniyle verilir; Windows'a özgü yol veya davranış kullanılmaz. Pi kurulumu ve dağıtım ayrı bir spec ile yapılır.

## Consequences
- Kazanç: gerçeğe yakın, ağ üzerinden erişilen bir deneme ortamı; mobil uygulama gerçek cihazdan bağlanabilir.
- Bedel: ARM64 uyumluluğu (paketler, PostgreSQL) doğrulanmalı; SD kart ömrü için log saklama sınırı (ADR-0003).
- Bedel: Pi'ye dağıtım adımları (yayımlama, kopyalama, servis olarak çalıştırma) ayrıca yazılmalı.
- Cevaplandı (spec 0003, 2026-11-20): PostgreSQL Pi'de (17, mevcut kurulum); Pi 5 / Debian 13 arm64; SSH anahtarı (`tmpi`). Önceki açık soru: PostgreSQL Pi'de mi, Windows makinede mi; Pi modeli/OS; erişim (SSH) bilgisi.

## Alternatives considered
- Yalnızca Windows makinede deneme — ağdan/mobil cihazdan erişim ve Linux uyumu sınanmaz.
- Bulut sunucu — maliyet; MVP'nin kısa denemeleri için gereksiz.

## Revisit triggers
- Pilot okul ile gerçek kullanım başlarsa (Türkiye'de barındırılan sunucuya geçiş).
