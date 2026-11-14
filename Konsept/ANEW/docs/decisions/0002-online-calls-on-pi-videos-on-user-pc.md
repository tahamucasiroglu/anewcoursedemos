# ADR 0002 — Online model çağrıları Pi'de (AiGateway), üretilen videolar kullanıcı PC'sinde

- Status: Accepted
- Date: 2026-11-14

## Context
Sistem iki fiziksel yerde çalışıyor: sunucu (Raspberry Pi 5, Konsept.Web + Konsept.Api +
PostgreSQL) ve kullanıcının Windows bilgisayarı (Konsept.Agent). İki soru mimariyi doğrudan
etkiliyordu:
1. **Online LLM çağrıları nerede yapılsın?** Çağrılar kullanıcının kendi API key'leriyle
   yapılıyor ve para harcatıyor (H-2 Gizli bilgiler, H-3 Maliyet doğuran çağrılar). Her çağrının
   token ve maliyeti kaydedilmeli (BR-12, bilgi amaçlı aylık harcama).
2. **Üretilen video dosyaları nerede dursun?** Kısa dikey videodan 15 dakikalık yatay videoya
   kadar üretiliyor; 15 dakikalık yatay bir video 0,5–1 GB tutabiliyor. Pi'nin diski ve ağı
   sınırlı; "klasörde aç" ve "sistem oynatıcısı" özellikleri yerel dosya istiyor.

Kaynak: `Promtlar/0008_*.md` (açık nokta 1 ve 2, öneriler), `Promtlar/0009_*.md` (kullanıcı
kararı: "api iletişimi raspiden yapılsın… üretim kodları raspiye gelir raspi masaüstü
uygulamasına iletir" ve "kullanıcı bilgisayarında").

## Decision
Tüm online model çağrıları **Pi'de, AiGateway modülünde** yapılır: AiGateway LLM'i çağırır,
üretilen video kodu Pi'ye gelir, Production modülü kodu AgentGateway (SignalR) üzerinden ajana
iletir, ajan render edip sonucu bildirir; çözülmüş API key'leri gören ve dış LLM API'lerini
çağıran tek modül AiGateway'dir (Y-7), ajan online API key'i hiç tutmaz (Y-8). Üretilen
videolar **kullanıcının PC'sinde, ajanın klasöründe** kalır; web oynatıcısı videoyu ajanın
yalnızca `localhost`'tan erişilebilen, kısa ömürlü token'lı yerel akışından oynatır.

## Consequences
Kazandırdıkları:
- API key'ler Pi'den hiç çıkmıyor; şifreleme (ASP.NET Data Protection), maskeleme ve erişim tek
  noktada (H-2). Y-7'nin bağımlılık yarısı ve Y-8'in key yarısı architecture testiyle, Y-8'in
  sandbox maddesi yöntem seçilince H-4 testleriyle zorlanıyor (Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`)).
- Maliyet kaydı, yeniden deneme sınırı (yalnızca 429/5xx, en fazla 2 kez) ve token limitleri tek
  modülde (H-3); BR-12 harcama gösterimi tek kaynaktan besleniyor.
- Kullanıcının PC'si kapalıyken de konu önerisi ve konsept üretilebiliyor; yalnızca render için
  ajan gerekiyor.
- Büyük video dosyaları Pi'nin diskini ve ağı tüketmiyor; "klasörde aç" ve "sistem oynatıcısı"
  doğrudan yerel dosyayla çalışıyor.

Bedelleri:
- Video yalnızca ajanın kurulu ve çalışır olduğu bilgisayardan izlenebiliyor; başka bir
  cihazdan (telefon, başka PC) izlenemiyor. Ajan kapalıysa oynatıcı çalışmaz.
- Videoların yedeği sunucuda yok; kullanıcının diski giderse videolar gider.
- Üretim akışı Pi → ajan arasında bir aktarım adımı ekliyor; SignalR bağlantı kopmaları
  (bilinen tuzak: yeniden bağlanınca kendini kaydetme, yarım işi bildirme) ele alınmak zorunda.
- LLM'in ürettiği kod kullanıcının makinesinde çalıştırılıyor; bu, kısıtlı render ortamı
  gerektiriyor (H-4, Y-8). Sandbox yöntemi henüz seçilmedi (`specs/backlog.md`, araştırma
  raporu).
- Localhost akışı + kısa ömürlü token, web sayfasının kullanıcı tarayıcısında ajanın yerel
  portuna erişmesini gerektiriyor; bu yerel uç da güvenlik açısından korunmalı.
- Tüm online çağrı trafiği Pi'nin ağından geçiyor.

## Alternatives considered
- **Online çağrıları ajanda (kullanıcı PC'sinde) yapmak:** API key'lerin kullanıcı PC'sine
  taşınması gerekirdi (H-2 riski), PC kapalıyken konsept üretilemezdi ve maliyet kaydı dağınık
  olurdu. Kullanıcı Pi'yi seçti.
- **Videoları Pi'ye yüklemek ve oradan oynatmak:** her cihazdan izlenebilirdi ama 0,5–1 GB'lık
  yüzlerce video Pi'nin diskini ve ağı tüketir; yerel dosya isteyen özelliklerle de çelişir.
  Kullanıcı "kullanıcı bilgisayarında" dedi.

## Revisit triggers
- Videoların ajanın kurulu olmadığı cihazlardan (telefon, başka PC) izlenmesi gerçek bir ihtiyaç
  haline gelirse.
- Yayınlama özelliği ("çok yakında") kapsama alınırsa: videonun bir platforma yüklenmesi
  dosyanın nerede durduğunu yeniden sorgulatır.
- Pi'nin ağı ya da CPU'su online çağrı trafiği veya kod aktarımında darboğaz olursa.
- Render sandbox araştırma raporu, LLM kodunu kullanıcı PC'sinde güvenle çalıştırmanın makul
  maliyetle mümkün olmadığını gösterirse.
- Sistem internete açılırsa (localhost akışı ve token modeli yeniden değerlendirilir).
