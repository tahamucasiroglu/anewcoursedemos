# Spec 0027 — Eksik bileşen indirme onayı

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0027-plan.md`
- Self-critique: done (Approved under the user's explicit request (2026-11-17))
- Source: kullanıcı bildirimi 2026-11-17 (/new-feature) — "model bulunamadı hatası var. oysa yoksa o sıra indirmek için onay kutusu
  açılmalı onaylarsa indirip işleme başlamalı şuan ise model yok diye hata veriyor loglardan bakabilirisn ama modele özel çözüm değil
  genel çözümm laızm"
- Approval: Approved under the user's explicit request (2026-11-17)
- Sensitive area: yok (key/yetki değişmez; ajana yalnızca bileşen kimlikleri gider, indirme adresi ve SHA-256 ajana gömülü listeden gelir)
- Depends on: 0009 (ajan kurulumu), 0017 (üretim hattı, araç denetimi), 0023 (sürüm kapısı)

## Kök neden (loglar, 2026-11-17)
Pi `api-20261117.log` 12:18:18Z: üç video işi başlar başlamaz (0 sn) "Video üretimi hata verdi: … Ajan kurulumu eksik: Z-Image-Turbo,
Supertonic 3". Ajan 0.7.0 bağlıydı; ajan logunda iş hatası yok (araç denetimi cevabı). Ajan kurulumu yalnızca Node.js, FFmpeg,
Chromium, HyperFrames kurmuştu (`setup-state.json`). Online model kimliği hatası (0026) değil: hiçbir sağlayıcı çağrısı yapılmadı.
Neden iki katmanlı:
1. Hat eksik aracı gördüğünde kullanıcıya hiçbir seçenek sunmadan işi Hata yapıyor (bu spec düzeltir — genel akış).
2. Seçilen iki yerel model (Z-Image-Turbo, Supertonic 3) için ajanda ne indirme girdisi (`components.json`) ne de çalıştırıcı var
   (B-116, High). Bu modeller bu sürümde indirilemez; spec bunu açık mesajla söyler, çalıştırıcıları eklemez (kapsam dışı).

## Intent
Üretim, ajan bilgisayarında kurulu olmayan ama ajanın kurabildiği bir bileşene (yerel araç ya da yerel model) ihtiyaç duyduğunda
iş hata vermek yerine durup kullanıcıya ne indirileceğini (ad, yaklaşık boyut) gösteren bir onay penceresi açmalı. Kullanıcı
onaylarsa ajan mevcut kurulum motoruyla (0009) indirir, doğrular, kurar ve iş kendiliğinden kaldığı yerden sürer; reddederse iş
açık bir mesajla durur. Akış genel: ajanın gömülü bileşen listesindeki her giriş için çalışır, modele özel kod yoktur — listeye
yeni bir giriş eklemek o bileşeni bu akışa sokmaya yeter. Ajanın kuramadığı (listede olmayan) bir şey eksikse iş eskisi gibi hemen
durur ama mesaj ne yapılacağını söyler. Yerel model çalıştırıcıları (B-116) bu işin konusu değildir.

## Requirements
1. Hattın araç denetimi (0017 Req 14) eksik araçları ikiye ayırır: ajanın **kurabildikleri** (ajanın gömülü bileşen listesinde
   olanlar; ajan her biri için ad, indirme ve kurulu boyutunu bildirir) ve **kuramadıkları**.
2. Kuramadığı en az bir araç varsa iş, hiçbir online çağrı yapılmadan Hata olur; mesaj eksikleri adlarıyla sayar ve kullanıcıya
   başka model seçmesini ya da ajanı güncellemesini söyler. Sürüm uyumsuzluğu (ajan güncellenmeli) mesajı değişmez.
3. Eksiklerin hepsi kurulabiliyorsa iş Hata olmaz: **Onay bekleniyor — indirme gerekli** durumuna geçer (sıra durumu "Ajan
   bekleniyor" ailesinde: kullanıcının tek çalışan işi sayılır, Durdur çalışır, sıra ilerlemez, art arda hata sayacına girmez).
   İşin ilerlemesinde indirilecek bileşenler (kimlik, ad, indirme ve kurulu boyut) saklanır. Günlük'e bilgi olayı yazılır.
4. Onay bekleyen iş, ajan bağlı olsa da kendiliğinden sürmez.
5. **Onayla**: yalnızca o işin bekleyen indirmesi onaylanır; iş hatta döner. Hat yeniden denetler; eksikler onaylananların
   içindeyse ajana kurulum komutu gider ve sonuç beklenir (iş Üretiliyor; satırda "İndiriliyor ve kuruluyor: …"). Başarıda iş
   kaldığı aşamadan sürer. Onaydan sonra yeni bir eksik çıkarsa yeniden onay istenir.
6. **Reddet**: iş Hata olur, mesaj "İndirme onaylanmadı: <adlar>. Üretim durdu." Günlük'e hata olayı yazılır; sıradaki iş
   kendiliğinden başlamaz. Kaldığı yerden devam et yeniden denetler (yeniden onay ister).
7. Kurulum başarısızsa iş Hata olur, mesaj başarısız bileşenleri sayar ve ayrıntının ajan penceresinde (Kurulum ve bileşenler)
   olduğunu söyler. Kurulum sırasında ajan koparsa iş Ajan bekleniyor olur ve onay korunur; ajan dönünce kurulum yeniden denenir.
8. Ajan kurulum komutunu yalnızca kendi oturumunun sunucusundan alır, yalnızca gömülü listedeki kimlikleri kurar (adres/hash
   sunucudan gelmez); ilk kurulum sürüyorsa bitmesini bekler. İlerleme ajanın tray ipucunda görünür.
9. Ajan bir bileşen kurulduğunda kurulu bileşen listesini sunucuya yeniden bildirir (B-149: yalnızca kayıtta bildirme sorunu).

## User experience
- Flow: Kullanıcı "Sırayla üret"e basar → iş başlar → eksik kurulabilir bileşen varsa satır "Onay bekleniyor — indirme gerekli"
  olur ve sayfada **İndirme onayı** penceresi açılır: başlık, açıklama ("Bu video için ajan bilgisayarına şunların indirilmesi
  gerekiyor"), liste (ad — yaklaşık indirme boyutu, diskte kaplayacağı alan), toplam, **İndir ve devam et** ve **Vazgeç**
  düğmeleri. Onay → pencere kapanır, satır "İndiriliyor ve kuruluyor: …", sonra aşamalar ilerler. Vazgeç → satır Hata, mesaj
  görünür, Kaldığı yerden devam et düğmesi çıkar.
- States: pencere kapalı (Esc/Vazgeç değil — Esc yalnızca pencereyi kapatır, iş beklemeye devam eder; satırdaki **İndirmeyi
  onayla…** düğmesi pencereyi yeniden açar); işleniyor (düğmeler pasif); hata (komut reddedildi → pencerede mesaj, korelasyon
  kimliği); ajan bağlı değilken onay (kabul edilir, ajan bağlanınca iner).
- Design reference: none — the flow above is the design (mevcut model seçimi penceresiyle aynı görünüm).
- Accessibility: pencere `role="dialog"`, `aria-modal`, başlık `aria-labelledby`; açılınca odak ilk düğmeye, Tab pencere içinde
  döner, Esc kapatır, kapanınca odak açan kontrole döner; boyutlar metin olarak.

## Constraints & out of scope
- Yerel görsel/ses model çalıştırıcıları ve onların indirme girdileri (Z-Image-Turbo, FLUX.2 klein, Supertonic 3, Chatterbox,
  Remotion) — B-116. Bu sürümde bu modeller "kuramıyor" mesajıyla durur.
- Ajan penceresinde ayrı bir indirme onayı yok (onay web'de verilir; ajan tray'de ilerleme gösterir).
- Yeni veritabanı şeması yok (bekleyen indirme işin ilerleme `jsonb`'sinde; durum Ajan bekleniyor).

## Acceptance criteria
- [ ] AC-1 — Ajanın kurabildiği bir araç eksikse iş Hata olmaz: Ajan bekleniyor + bekleyen indirme (ad, boyutlar) durumda görünür,
  hiçbir online çağrı ve harcama yok, sayaç artmaz, iş kendiliğinden sürmez.
- [ ] AC-2 — Onay → ajana yalnızca o kimliklerle kurulum komutu gider; başarıda iş kendiliğinden tamamlanır; bekleyen indirme silinir.
- [ ] AC-3 — Reddet → iş Hata, mesaj "İndirme onaylanmadı: …", ajana kurulum komutu gitmez, sıradaki iş başlamaz, Günlük'te hata olayı.
- [ ] AC-4 — Kurulum başarısız → iş Hata, mesaj başarısız bileşeni sayar; kurulum sırasında ajan koparsa Ajan bekleniyor ve onay korunur.
- [ ] AC-5 — Kuramadığı araç eksikse iş online çağrı olmadan Hata, mesaj adları sayar ve başka model/ajan güncellemesi önerir.
- [ ] AC-6 — Onay/Reddet yalnızca sahibinin, bekleyen indirmesi olan işinde çalışır (başkasınınki 404, beklemeyen iş 409).
- [ ] AC-7 — Ajan: araç denetimi eksikler için gömülü listeden kurulabilir bilgisini döndürür; kurulum komutu kurulum motoruyla
  yalnızca listedeki kimlikleri kurar, bilinmeyen kimliği reddeder, sonucu bildirir; bileşen kurulunca liste sunucuya yeniden bildirilir (B-149).
- [ ] AC-8 — Web: bekleyen indirmede pencere açılır (ad, boyut, toplam), İndir ve devam et onay ucunu, Vazgeç reddetme ucunu
  çağırır; Esc yalnızca kapatır, satırdaki düğme yeniden açar; odak/Tab/Esc kuralları.

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
