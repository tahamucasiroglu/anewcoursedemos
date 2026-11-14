# Spec 0018 — Ajanda kısıtlı render

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0018-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0006-uretim-konseptten-video.md` (bölüm D; Req 19–22; AC-28–33) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-4 LLM'in ürettiği video kodunun kullanıcı bilgisayarında çalıştırılması (render ortamı).
- Depends on: 0003, 0012, 0009, 0006, 0017

## Intent
İçerik üreticisinin bilgisayarında, online modelin yazdığı video kodu render edilirken bu kod hiçbir şeyi
dışarı sızdıramamalı, kullanıcının dosyalarına dokunamamalı ve bilgisayarı kilitleyememeli. Ajan, video kodunu
yalnızca o işin kendi iş klasörüne erişebilen, ağı kapalı ve kaynakları sınırlı bir ortamda, yönetici yetkisi
istemeden render ediyor; kurallara aykırı olduğu önceden anlaşılan kodu hiç çalıştırmıyor; çıktıyı kullanmadan
önce denetliyor. Başarı: rapordaki kasıtlı bozma testlerinin (Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`) §4 T1–T11) hepsi kırmızıdan yeşile döner ve
zararsız şablon kod sorunsuz render edilir. Bilinçli olarak yapılmayanlar: üretim hattının kendisi ve düzeltme
döngüsü (0017); araç ve bileşen kurulumu (0009); Remotion'un ticari lisans incelemesi. İsteğe bağlı "güçlü mod"
(yönetici izinli güvenlik duvarı kuralı) bu spec'e aittir ve 0009'un "Kurulum ve bileşenler" ekranına seçenek
olarak buradan eklenir.

## Requirements
1. Ajan, online modelin yazdığı video kodunu yalnızca o işin kendi iş klasörüne (ajanın kurulum yeri
   `%LOCALAPPDATA%\anewconcept` altında, 0009) erişebilen ve ağ erişimi
   kapalı bir ortamda render eder: render sırasında hiçbir ağ isteği dışarı çıkmaz, iş klasörü dışında hiçbir
   dosya okunamaz ya da yazılamaz, render sınırsız kaynak tüketemez (sahne başına süre, bellek, süreç sayısı
   sınırı aşılırsa render ve tüm alt süreçleri sonlandırılır ve iş Hata'ya düşer).
2. Yalıtım katmanlıdır ve çalışma zamanında yönetici yetkisi istemez (Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`) K-16/K-18/K-19): sabit
   şablon sözleşmesi → render öncesi yasaklı yapı kontrolü (hem Pi'de hem ajanda) → iş klasörü →
   **AppContainer** (yalnızca spike 1–2 geçerse; geçmezse kısıtlı token + Chromium'un kendi sandbox'ı + ağ
   bayrakları) → Job Object kaynak sınırı → Node izin modeli → çıktı kapısı. Tek katmana güvenilmez.
   Windows Sandbox varsayılan değildir.
3. Kurallara aykırı olduğu render öncesinde anlaşılan kod (dinamik kod çalıştırma, ağ API'leri, dolaylı
   erişimle gizlenmiş hâlleri) hiç çalıştırılmaz; ret, 0017'deki düzeltme döngüsüne (en fazla 1 düzeltme)
   gider; yine reddedilirse iş Hata'ya düşer.
4. Render çıktısı beklenen biçimde değilse (oynatılamayan dosya, beklenenden çok farklı süre ya da boyut) iş
   Hata'ya düşer; render çıktısı hiçbir zaman çalıştırılmaz.
5. Render sahne başına yapılır (0017); tamamlanmış sahnelerin render çıktısı korunur, devam ya da düzeltmede
   yalnızca render'ı tamamlanmamış sahneler render edilir.
6. Video, konseptin kaydındaki biçime (yatay / dikey; 0014 Req 27, işe sabitlenmiş hâli 0006 Req 20) uygun
   boyutta üretilir; profilin güncel biçimi kullanılmaz. Video kullanıcının bilgisayarında,
   ajanın klasöründe kalır; Pi'ye yüklenmez (ADR-0002).
7. Kurallar, 0006'daki video motoru listesindeki her motor için (HyperFrames, sürümü sabit, telemetri kapalı;
   Remotion — yalnızca ajanda 0009'un isteğe bağlı bileşeni olarak kuruluysa listelenir —, telemetri kapalı)
   aynen geçerlidir.
8. **Güçlü mod (isteğe bağlı ek katman).** 0009'un kurulum ekranına ve "Kurulum ve bileşenler" ekranına
   varsayılan kapalı bir "Güçlü mod" seçeneği eklenir; seçeneğin yanında ne yaptığı (render süreçlerinin dışarı
   bağlantısını Windows güvenlik duvarıyla da engeller) ve yönetici izni isteyeceği yazar. Açılınca yönetici
   izni (UAC) bir kez istenir; onaylanırsa render süreçlerinin giden bağlantısını engelleyen güvenlik duvarı
   kuralı eklenir; reddedilirse mod kapalı kalır ve kurulum ya da render etkilenmez. Kapatılınca kural
   kaldırılır. Güçlü mod hiçbir korumanın yerine geçmez; Req 1–7 güçlü mod kapalıyken de tam sağlanır.

## User experience
- Render'ın kendi arayüzü yoktur. Kullanıcının gördüğü sonuç 0017'deki aşama durumu ve iş satırındaki sade
  Hata açıklamasıdır (ör. "Video kodu güvenlik denetiminden geçmedi", "Render süre sınırını aştı", "Render
  çıktısı geçersiz"); bu metinler 0017'nin hata gösterimini kullanır.
- Güçlü mod (Req 8): 0009'un kurulum / "Kurulum ve bileşenler" ekranında açıklamalı bir onay kutusu.
  States: kapalı (varsayılan) · yönetici izni bekleniyor · açık · izin reddedildi ("Güçlü mod açılamadı —
  yönetici izni verilmedi; render yine korumalı çalışır").
- Accessibility: onay kutusu klavyeyle (Tab, Space) kullanılabilir, erişilebilir adı ve açıklaması vardır;
  durum metinle belirtilir.

## Constraints & out of scope
- **Spike 1–2 (Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`) §4: AppContainer içinde Chromium/loopback davranışı) plan'dan önce yapılır**; sonucu
  bir ADR olur ve Req 2'deki AppContainer ya da yedek yol seçimi o ADR'ye göre kesinleşir.
- Her kriter kasıtlı bozma ile kanıtlanır (rapor §4 T1–T11 örnek alınır): koruma kaldırılınca ilgili test kırmızıya döner.
- Sahne başına süre, bellek ve süreç sayısı sınır değerleri plan'da geliştirici PC'sindeki benchmark'a göre
  (rapor §6) belirlenir ve ayarlara yazılır; kodda sabit değer olmaz.
- Kriterler, isteğe bağlı yönetici yetkili güvenlik duvarı kuralı ("güçlü mod", Req 8 — bu spec 0009'un
  kurulum ekranına ekler) **kapalıyken** sağlanmalıdır; güçlü mod ek katmandır.
- Node 26 LTS (Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`) K-17); HyperFrames sürümü sabittir.
- Gerçek render, ajan render entegrasyonu olarak full profilde çalışır (`docs/testing.md`).
- Out of scope: üretim hattı, aşamalar, düzeltme döngüsünün kendisi ve maliyeti (0017) · araç ve model kurulumu
  ve "Kurulum ve bileşenler" ekranının kendisi (0009) · Remotion'un ticari lisans incelemesi · Revideo, Motion Canvas · Linux/macOS ajanı ·
  Manim (rapor K-3, faz 2).
- Split: done — 0018 Ajanda kısıtlı render / kardeşler: 0006, 0017

## Acceptance criteria
<!-- Her kriter kasıtlı bozma ile kanıtlanır; her kriter listedeki her video motoru için koşar (Req 7). -->
- [ ] AC-1 — Render edilen kod dışarıya ağ isteği atmaya çalışırsa (HTTP isteği, IP adresine istek, uzak görsel/ses kaynağı, WebSocket, eşler arası bağlantı) hiçbir istek hedefe (test honeypot'u) ulaşmaz; render ya başarısız olur ya da kaynak karede görünmez. Motorun kendi telemetri isteği de honeypot'a ulaşmaz.
- [ ] AC-2 — Render edilen kod iş klasörü dışındaki bir dosyayı okumaya (ör. `file://`, `..` ile klasör dışına çıkma, iş klasörü içinden dışarıyı gösteren kısayol/sembolik bağlantı, ajanın kendi oturum dosyaları) ya da kullanıcının ev klasörüne yazmaya çalışırsa okuma başarısız olur, içerik karede görünmez ve dosya oluşmaz.
- [ ] AC-3 — Sonsuz döngü, aşırı bellek ya da yüzlerce alt süreç açan kod ayarlardaki sahne başına süre/bellek/süreç sınırında sonlandırılır; render bitince ya da öldürülünce geride hiçbir render süreci kalmaz; iş *Hata* olur.
- [ ] AC-4 — Yasaklı yapılar içeren kod (dinamik kod çalıştırma, ağ API'leri, dolaylı erişimle gizlenmiş hâlleri) render öncesinde hem Pi'de hem ajanda reddedilir ve hiç çalıştırılmaz; ret 0017'deki düzeltme döngüsüne gider (en fazla 1 düzeltme çağrısı), düzeltilen kod da reddedilirse iş *Hata* olur.
- [ ] AC-5 — Zararsız örnek şablon kod sorunsuz render edilir ve çıktı kapısından geçer (aşırı sıkılaştırma koruması); çıktı oynatılamaz bir dosyaysa ya da süresi/boyutu beklenenden saparsa iş *Hata* olur ve çıktı çalıştırılmaz.
- [ ] AC-6 — Üretilen video konseptin kaydındaki biçime uygundur (dikey konseptte dikey, yatay konseptte yatay; konsept dikey üretildikten sonra profil yatay yapılsa da video dikeydir) ve kullanıcının bilgisayarında ajanın klasöründedir; Pi'ye hiçbir video dosyası yüklenmez.
- [ ] AC-7 — 5 sahneli bir işte 3. sahnenin render'ı sırasında durdurulup devam edilince 1. ve 2. sahne yeniden render edilmez; yalnızca 3–5. sahneler render edilir ve birleştirilmiş video 5 sahnenin tamamını içerir.
- [ ] AC-8 — Render, yönetici yetkisi (UAC) istemeden ve güçlü mod kapalıyken çalışır; AC-1–AC-5 bu koşulda sağlanır.
- [ ] AC-9 — 0009'un kurulum ekranında ve "Kurulum ve bileşenler" ekranında "Güçlü mod" seçeneği varsayılan kapalı görünür; açılınca yönetici izni (UAC) bir kez istenir, onaylanınca render süreçlerinin giden bağlantısını engelleyen güvenlik duvarı kuralı oluşur ve AC-1 bu koşulda da sağlanır; izin reddedilince mod kapalı kalır, "Güçlü mod açılamadı" uyarısı görünür ve kurulum/render AC-8'deki gibi sürer; kapatılınca kural kaldırılır.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
