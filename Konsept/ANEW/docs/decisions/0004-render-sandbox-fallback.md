# ADR 0004 — Ajanda kısıtlı render: AppContainer yerine ölçülmüş yedek yol

- Status: Accepted
- Date: 2026-11-17

## Context
Spec 0018, online modelin yazdığı video kodunun kullanıcının bilgisayarında render edilirken dışarı sızdıramamasını, kullanıcının
dosyalarına dokunamamasını ve bilgisayarı kilitleyememesini ister; yönetici yetkisi istenmez. Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`
§4) L3 için AppContainer'ı önerir ama önce iki spike ister (AppContainer içinde Chromium'un kendi sandbox'ı ve loopback). Kullanıcı
"çok detaylı yapmadan hızlıca" ile tüm kalan spec'lerin bitirilmesini istedi; spike'lar yapılmadı. HyperFrames 0.9.43 ölçümü (plan 0018):
yerel dosya sunucusu (127.0.0.1) Node'un `--allow-net` iznini ister, dışarıdan Chromium bayrağı alma yolu yoktur.

## Decision
Spec Req 2'nin izin verdiği yedek yolu kullanıyoruz: L0 sabit sözleşme · L1 ortak yasaklı yapı kapısı (Pi + ajan) · L2 iş klasörü
(sıfırdan ortam, bağlantı reddi) · L3 Chromium'un kendi sandbox'ı zorunlu + Node ön yükleme modülü (`render-guard.mjs`) ile ajanın
Chromium'una ölü proxy / DNS kapatma / WebRTC UDP kapatma bayrakları, Node tarafı ölü `HTTP(S)_PROXY` · L4 Job Object · L6 Node izin
modeli (dosya okuma/yazma sınırlı, ağ açık) · L7 ffprobe çıktı kapısı.

## Consequences
- Yönetici yetkisi hiçbir adımda istenmez; gerçek render bu PC'de çalıştı ve honeypot ölçümünde koruma açıkken 0, kapalıyken 9 isabet.
- Ağ koruması tarayıcı bayraklarına dayanır (çekirdek düzeyinde değil); bayrak atlatan bir Chromium hatası ya da motorun tarayıcıyı
  başka yoldan başlatması korumayı zayıflatır. Bu yüzden L1 kapısı ve isteğe bağlı güçlü mod (güvenlik duvarı, B-124) önemlidir.
- Node tarafı ağ (`--allow-net`) açıktır; motorun kendi isteklerini ortam proxy'si ve telemetri kapatma değişkenleri keser.
- Job Object ataması süreç başladıktan hemen sonra yapılır (askıda başlatma yok); ilk alt süreç ondan sonra doğar.

## Alternatives considered
- **AppContainer (rapor tercihi):** en güçlü admin'siz katman; Chromium iç içe sandbox ve loopback davranışı doğrulanmadan risklidir
  (render hiç çalışmayabilir). Spike'lar B-125'te.
- **Windows Sandbox:** yönetici özelliği, Home sürümünde yok, tek örnek (K-19) — varsayılan değil.
- **Node `--permission` ile ağ kapatma:** HyperFrames yerel sunucusunu da kapatıyor (ölçüldü: render 25 %'te takılıyor).

## Revisit triggers
- Spike 1–2 sonuçları (B-125) olumluysa L3'ü AppContainer'a taşı.
- HyperFrames sürüm yükseltmesi (tarayıcı başlatma yolu değişirse koruma modülü yeniden doğrulanmalı).
- Honeypot gözleminde herhangi bir isabet.
