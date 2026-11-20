# Spec 0016 — Randevu: öğretmen müsaitliği ve veli talebi

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0016-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Veli bugün öğretmenle görüşmek için okulu arıyor, sekreter öğretmene not bırakıyor, öğretmen geri dönüyor; çoğu zaman
birkaç telefon trafiğinden sonra bir saat bulunuyor ya da veli kapıda bekliyor. Bu spec yol haritasının 10. satırını
getirir: öğretmen telefondan **müsait olduğu saatleri** açar (ör. "Salı 14:00–16:00, 20 dakikalık görüşmeler"), veli
çocuğunun öğretmenlerinden birinin **boş saatini seçip randevu talep eder**, öğretmen **onaylar ya da reddeder**; iki
taraf da yaklaşan randevularını görür ve gerekirse iptal eder. Başarı: veli akşam iki dokunuşla çarşamba 14:20'yi ister,
öğretmen sabah mesai başında talebi görüp onaylar, veli "Randevunuz onaylandı" bildirimini alır; aynı saati ikinci bir
veli alamaz. Bilinçli olarak yapılmayanlar: haftalık tekrarlayan müsaitlik şablonu, kurucu/müdür/rehber randevusu, online
görüşme bağlantısı, hatırlatma bildirimi, takvim uygulamasına aktarma, web ekranları.

**Bağımlılık:** spec 0008 (Türkiye günü, okul), 0009 (öğrenci, veli bağı, "Çocuklarım"), 0010 (sınıf öğretmeni bağı:
velinin çocuğunun öğretmenleri) ve 0011 (bildirim kapısı, velinin akışında "Okul bildirimi", öğretmenin mesai saatleri /
sessiz mod) bu spec'ten önce yapılır. Adlar o spec'ler bitince plan değişikliğiyle (amendment) eşlenir.

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Müsaitlik nasıl tanımlanır?** Öğretmen **tarihli bir zaman aralığı** açar: gün (bugün ile 60 gün sonrası
  arası), başlangıç ve bitiş saati (aynı gün, 15 dakikalık adımlarla, en fazla 8 saat) ve görüşme süresi (15, 20 ya da 30
  dakika). Uygulama aralığı bu süreli **dilimlere** böler; sona kalan kısa parça dilim olmaz. Haftalık tekrarlayan şablon
  sonraki adım. Neden: tek form, tek kural; öğretmen önümüzdeki haftaları birkaç kayıtla açar, tekrar kuralının istisnaları
  (tatil, sınav haftası) hiç gerekmez.
  - Dokümanda istenen: "Randevu — Öğretmen müsaitlik takvimi, veliden randevu talebi" (kapsam, MVP modülü 7).
- **C-2 — Veli kimden randevu alır?** Yalnızca **çocuğunun sınıfının öğretmenlerinden** (0010 sınıf öğretmeni bağı; 0011
  mesajlaşmayla aynı ilişki), o çocuk adına. Kurucu, müdür, rehber öğretmen ve muhasebe randevusu yok. Neden: ilişki
  kuralı mesajlaşmayla aynı kalır; veri en az kişiye görünür.
  - Dokümanda istenen: rol tablosu "Veli | Takip, bildirim, ödeme, randevu | Mobil"; güvenlik "veli yalnızca kendi
    çocuğunu, öğretmen yalnızca kendi sınıflarını görür".
- **C-3 — Talep nasıl işler?** Veli boş bir dilimi seçer ve kısa bir **konu** yazar (zorunlu, en fazla 300) → talep
  **"Onay bekliyor"** olur ve dilim başka velilere kapanır. Öğretmen **onaylar** ya da **reddeder** (ret nedeni isteğe
  bağlı, en fazla 300); ret dilimi yeniden boşaltır. Dilim başlayana kadar yanıtlanmayan talep **"Süresi doldu"** olur.
  Aynı çocuk ve aynı öğretmen için aynı anda en fazla **bir açık** (onay bekleyen ya da onaylı, başlamamış) randevu olur
  (çocuğun hangi velisi isterse istesin); çocuğun öteki velisi randevuyu görür, iptali yalnızca talep eden veli yapar. Neden: kapsam "talep" diyor — öğretmen son sözü söyler; tek açık randevu kuralı saat kapatmayı önler.
  - Dokümanda istenen: "veliden randevu talebi".
- **C-4 — İptal ve kaldırma:** Veli kendi açık randevusunu başlamadan iptal eder → dilim yeniden boşalır. Öğretmen onaylı
  randevuyu başlamadan iptal eder (neden isteğe bağlı) → dilim kapanır (öğretmen o saatte yok). Öğretmen boş dilimi kaldırır;
  talebi olan dilim kaldırılamaz ("Önce talebi yanıtlayın ya da randevuyu iptal edin."). Başlamış randevu iptal edilemez.
  Neden: en az durumla iki tarafın da çıkış yolu var.
  - Dokümanda istenen: kapsam ayrıntı vermiyor; en sade akış seçildi.
- **C-5 — Bildirimler:** Onay, ret ve öğretmen iptali veliye 0011'in **bildirim kapısından** "Okul bildirimi" olarak (o
  çocuk için) gider. Yeni talep ve veli iptali öğretmene kapıdan gider, ama 0011'in **mesai saatleri** kuralıyla: mesai
  dışında bildirim gitmez, öğretmen hoş geldin'deki "Bekleyen talepler (n)" sayısında görür. Hatırlatma bildirimi yok.
  Neden: BR-8'in amacı öğretmeni mesai dışında bölmemek; aynı kural randevu için de geçerli olmalı.
  - Dokümanda istenen: BR-8 "öğretmenin tanımladığı saatler dışında veli mesajı iletilir ama öğretmene bildirim gitmez";
    mimari "Communication … veliye giden her şeyin tek çıkışı".
- **C-6 — Hangi kanal?** Öğretmen ve veli ekranları **mobilde**. Web sonraki adım; kurucu randevuları görmez (yönetici
  paneli, yol haritası 11). Neden: iki taraf da işi telefonda yapar; tek kanal boyutu yarıya indirir.
  - Dokümanda istenen: rol tablosu "Öğretmen — Mobil + Web", "Veli — Mobil".
- **C-7 — Görüşme biçimi:** Yüz yüze, okulda; yer ya da çevrim içi görüşme bağlantısı alanı yok. Neden: MVP okulunun
  ihtiyacı veli toplantısı saati; bağlantı paylaşımı ayrı güvenlik konusu (dış adres).
  - Dokümanda istenen: kapsam ayrıntı vermiyor.

## Requirements
1. **Müsaitlik açma:** Öğretmen mobilde gün, başlangıç, bitiş ve görüşme süresiyle (sınırlar C-1) müsaitlik açar; uygulama
   aralığı dilimlere böler ve "6 görüşme saati açıldı" der. Öğretmenin başlamamış bir dilimiyle çakışan aralık "Bu saatlerde
   zaten müsaitliğiniz var." ile reddedilir. Müsaitlik okul başınadır (öğretmenin birden çok şubesi olsa da tek takvim).
2. **Takvimim (öğretmen):** Öğretmen bugünden itibaren dilimlerini gün gün görür: boş, onay bekliyor (veli adı, çocuk adı,
   konu), onaylı. En üstte onay bekleyen talepler. Boş dilimi kaldırır; talebi olan dilim kaldırılamaz. Hiç dilim yoksa
   "Henüz müsaitlik açmadınız". Hoş geldin'de "Randevular (n bekleyen)".
3. **Talep (veli):** Veli çocuğunu, sonra o çocuğun sınıfının öğretmenlerinden birini seçer; öğretmenin başlamamış boş
   dilimlerini gün gün görür (boş yoksa "Bu öğretmenin boş görüşme saati yok"); birini seçip konuyu yazar ve gönderir.
   Talep "Onay bekliyor" olur; dilim başka velilerin listesinden düşer.
4. **Talep kuralları:** Başlamış dilim, boş olmayan dilim ("Bu saat artık dolu."), aynı çocuk ve öğretmen için açık
   randevu varken yeni talep ("Bu öğretmenle bu çocuk için zaten açık bir randevunuz var.") ve boş ya da 300 karakteri
   aşan konu reddedilir; hiçbir şey değişmez.
5. **Onay ve ret:** Dilimin öğretmeni onay bekleyen talebi onaylar ya da (isteğe bağlı nedenle) reddeder. Ret dilimi
   yeniden boşaltır. Başlamış talep yanıtlanamaz ve "Süresi doldu" görünür.
6. **İptal:** Veli açık randevusunu başlamadan iptal eder (dilim boşalır); öğretmen onaylı randevuyu başlamadan iptal eder
   (dilim kapanır). Başlamış randevu iptal edilemez ("Başlamış randevu iptal edilemez.").
7. **Bildirimler:** Veliye: "Randevunuz onaylandı — {öğretmen}, {gün.ay.yıl} {SS:DD}", "Randevu talebiniz yanıtlanamadı —
   {öğretmen}, {gün.ay.yıl} {SS:DD}" (ret; neden varsa eklenir), "Randevunuz öğretmen tarafından iptal edildi — …".
   Öğretmene: "Yeni randevu talebi — {gün.ay.yıl} {SS:DD}" ve "Randevu iptal edildi — {gün.ay.yıl} {SS:DD}", yalnızca mesai
   içinde (0011 kuralı; tanımsızsa her zaman). Konu metni bildirimde yer almaz. Gönderici hata verirse işlem başarılı kalır.
8. **Randevularım (veli):** Veli bütün çocuklarının randevularını (öteki velinin talep ettikleri dahil, "talep eden" ile)
   görür: yaklaşan randevuları (en yakın üstte) ve son 30 günün geçmiş
   randevularını görür: öğretmen, çocuk, gün ve saat, durum (Onay bekliyor / Onaylandı / Reddedildi + neden / İptal edildi
   / Süresi doldu). Boşken "Henüz randevunuz yok". İptal düğmesi yalnızca talep eden velide.
9. **Gizlilik ve izolasyon:** Başka okulun, ilişkisi olmayan öğretmenin dilimleri ve bağlı olunmayan çocuğun randevusu
   "bulunamadı" (404). İlişki biterse (öğrenci sınıftan ayrıldı, öğretmen sınıftan alındı, veli bağı kalktı, kişi pasif) yeni talep
   reddedilir; var olan randevu görünür ve iptal edilebilir. Konu, ret/iptal nedeni ve kişi adları log satırlarına ve
   bildirim log'una yazılmaz (yalnızca kimlikler).
10. **Audit:** Talep, onay, ret ve iptal audit'e yazılır (kim, hangi randevu, ne zaman, IP); müsaitlik açma/kaldırma
    Information log'una (yalnızca kimlikler).

## User experience
- **Flow — mobil (öğretmen):** Hoş geldin → "Randevular (2 bekleyen)" (yalnızca öğretmende) → Takvimim (üstte onay
  bekleyenler; altında gün gün dilimler) → talebe dokun → konu, veli, çocuk → "Onayla" / "Reddet" (neden alanı) · onaylı
  randevuda "İptal et" · boş dilimde "Kaldır" · "Müsaitlik aç" → gün (GG.AA.YYYY + "Bugün/Yarın"), başlangıç, bitiş
  (SS:DD), süre (15/20/30) → "Aç" → "6 görüşme saati açıldı" → Takvimim.
- **Flow — mobil (veli):** Hoş geldin → "Randevular" (yalnızca velide) → Randevularım (yaklaşan, geçmiş) → "Randevu al" →
  [birden çok çocuk] çocuk seç → öğretmen seç → boş saatler (gün gün) → saat seç → konu → "Talep gönder" → "Talebiniz
  öğretmene iletildi" → Randevularım. Açık randevuda "İptal et" (onay iletişimi).
- **States:** Takvimim: yükleniyor / boş / dolu / hata + tekrar dene. Müsaitlik formu: alan hataları (istek gitmez) /
  gönderiliyor (düğme kilitli) / çakışma hatası / başarı (açılan dilim sayısı). Onay/ret/iptal/kaldır: işleniyor (düğmeler
  kilitli) / hata (ör. "Başlamış randevu iptal edilemez.") / başarı. Veli: çocuk/öğretmen seçimi boş ("Kayıtlı çocuğunuz
  yok", "Bu sınıfa atanmış öğretmen yok"), boş saat yok, talep gönderiliyor, "Bu saat artık dolu." (liste yenilenir),
  açık randevu var hatası, Randevularım yükleniyor / boş / dolu / hata + tekrar dene; iptal onay iletişimi. Rolü olmayana menü
  görünmez.
- **Design reference:** none — the flow above is the design (spec 0010/0011 mobil liste ve form kalıpları, ortak tema).
- **Accessibility:** dilim düğmesi etiketi gün, saat ve durumu içerir ("Çarşamba 26 Kasım 14:20, boş"); durumlar yalnızca
  renkle değil yazıyla; dokunma alanı en az 44×44; hatalar ve sonuçlar duyurulur; iki temada kontrast spec 0002 eşiklerinde.

## Constraints & out of scope
- Sensitive area: S1 (okul sınırı, ilişki denetimi), S2 (veli ve öğrenci adı, görüşme konusu), S3 (veli-öğrenci ve
  öğretmen-sınıf bağına dayalı yetki), S5 (veliye giden bildirim), S6 (audit).
- Bağımlılık: 0008, 0009, 0010 ve 0011 tamamlanmadan build başlamaz.
- Zaman: bütün saatler Türkiye saati; "başlamış" = dilim başlangıcı şu andan önce ya da şu an.
- Kapsam dışı / sonra: haftalık tekrarlayan müsaitlik şablonu; aralığı toplu kaldırma; kurucu/müdür/rehber/muhasebe
  randevusu; birden çok öğretmenle ortak görüşme; görüşme yeri ve çevrim içi bağlantı; hatırlatma bildirimi (ör. 1 saat
  önce); takvime (.ics) aktarma; randevu notu/görüşme kaydı (Faz 2 rehberlik); öğretmenin onaylı randevuyu yeniden
  zamanlaması (iptal + yeni talep); web ekranları; kurucu görünümü ve raporlar (yol haritası 11); push (B-6), SMS (B-2).
- Split: önerilmedi — tek yetenek (müsaitlik → talep → yanıt); 13 kriter, web ekranı yok.

## Acceptance criteria
- [ ] AC-1 — Müsaitlik açma: öğretmen 14:00–16:00 aralığını 20 dakikalık süreyle açınca 6 dilim oluşur, 14:00–14:50'yi 20 dakikayla açınca 2 dilim (kalan 10 dakika dilim olmaz); geçmiş gün, 61 gün sonrası, başlangıç ≥ bitiş, 15 dakika adımı dışı saat, 8 saati aşan aralık ve 15/20/30 dışı süre alan hatası verir ve hiçbir şey oluşmaz (bugünün gelecek saati ve 60. gün kabul edilir); başlamış saatlere düşen dilim oluşmaz.
- [ ] AC-2 — Çakışma ve kaldırma: öğretmenin başlamamış bir dilimiyle çakışan aralık "Bu saatlerde zaten müsaitliğiniz var." ile reddedilir (bitişik aralık kabul edilir); boş dilim kaldırılır; onay bekleyen ya da onaylı dilim "Önce talebi yanıtlayın ya da randevuyu iptal edin." ile reddedilir.
- [ ] AC-3 — Takvimim: öğretmen bugünden itibaren dilimlerini gün ve saat sırasıyla durumlarıyla (boş / onay bekliyor + veli, çocuk, konu / onaylı) görür, onay bekleyenler üstte; hoş geldin'deki bekleyen sayısı doğru; dilim yoksa "Henüz müsaitlik açmadınız".
- [ ] AC-4 — Talep: veli yalnızca çocuğunun sınıfının öğretmenlerini ve onların başlamamış boş dilimlerini görür; dilim seçip konuyla talep gönderince randevu "Onay bekliyor" olur ve dilim öteki velilerin boş listesinden düşer; boş dilim yoksa "Bu öğretmenin boş görüşme saati yok".
- [ ] AC-5 — Talep kuralları: başlamış dilime talep, boş olmayan dilime talep ("Bu saat artık dolu."), aynı çocuk ve öğretmen için açık randevu varken (öteki veli istemiş olsa bile) ikinci talep ("Bu öğretmenle bu çocuk için zaten açık bir randevunuz var.") ve boş ya da 301 karakterlik konu reddedilir, hiçbir şey değişmez (300 karakter kabul); aynı veli başka çocuğu ya da başka öğretmen için talep edebilir.
- [ ] AC-6 — Eşzamanlılık: iki veli aynı dilime aynı anda talep gönderirse yalnızca biri "Onay bekliyor" olur, öteki "Bu saat artık dolu." alır; aynı velinin aynı anda iki talebi tek açık randevu bırakır.
- [ ] AC-7 — Onay ve ret: dilimin öğretmeni onay bekleyen talebi onaylar ya da nedenli/nedensiz reddeder; ret dilimi yeniden boşaltır ve başka veli talep edebilir; başka öğretmen yanıtlamaya çalışırsa 404; yanıtlanmış ya da başlamış talep yanıtlanamaz; dilim başlayınca yanıtlanmamış talep veliye ve öğretmene "Süresi doldu" görünür.
- [ ] AC-8 — İptal: veli onay bekleyen ya da onaylı randevusunu başlamadan iptal eder ve dilim yeniden boşalır; öğretmen onaylı randevuyu başlamadan iptal eder ve dilim takvimden kalkar; başlamış randevunun iptali "Başlamış randevu iptal edilemez." ile reddedilir.
- [ ] AC-9 — Bildirimler: onay, ret ve öğretmen iptali bildirim kapısından velilere (o çocuk için, gereksinim 7 metniyle; ret nedeni varsa ekli) geçer; yeni talep ve veli iptali öğretmene mesai içinde (Türkiye saati, seçili gün, başlangıç dahil – bitiş hariç) ya da sessiz mod tanımsızken bildirim üretir, mesai dışında üretmez ama bekleyen sayısı artar; bildirimde konu metni yoktur; gönderici hata verirse işlem kaydedilir.
- [ ] AC-10 — Randevularım: veli bütün çocuklarının (öteki velinin talep ettikleri dahil) yaklaşan randevularını en yakın üstte ve son 30 günün geçmiş randevularını öğretmen, çocuk, gün-saat ve doğru durumla (Onay bekliyor / Onaylandı / Reddedildi + neden / İptal edildi / Süresi doldu) görür; 31 gün önceki randevu görünmez; boşken "Henüz randevunuz yok".
- [ ] AC-11 — Yetki ve izolasyon: veli ilişkisi olmayan öğretmenin dilimlerini göremez ve talep edemez (404), bağlı olmadığı öğrenci adına talep edemez (404), bağlı olmadığı çocuğun randevusunu göremez ve iptal edemez (404), çocuğun öteki velisinin talep ettiği randevuyu görür ama iptal edemez (403); başka okulun kayıtları 404; öğretmen rolü olmayan müsaitlik işlemlerinde, veli rolü olmayan talep işlemlerinde 403; ilişki bitince (öğrenci sınıftan ayrıldı, öğretmen sınıftan alındı, veli bağı kalktı) yeni talep reddedilir, var olan randevu görünür ve iptal edilebilir; girişsiz istek 401.
- [ ] AC-12 — Gizlilik ve audit: log ve bildirim log satırlarında konu, ret/iptal nedeni ve kişi adı geçmez (yalnızca kimlikler); talep, onay, ret ve iptal audit kaydı oluşturur (kim, ne, hangi randevu, ne zaman, IP).
- [ ] AC-13 — Mobil ekranlar (iki tema): "Randevular" menüsü yalnızca öğretmen ve velide (öğretmende bekleyen sayısıyla); Takvimim, müsaitlik formu, talep yanıtı, veli çocuk/öğretmen/saat seçimi, talep formu ve Randevularım ekranlarının User experience'taki bütün durumları (yükleniyor, boş, dolu, alan hataları, gönderiliyor-düğme kilitli, "Bu saat artık dolu." sonrası liste yenilenmesi, hata + tekrar dene, iptal onay iletişimi); dilim etiketleri gün, saat ve durumu içerir.

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Öğretmenin iki okulu varsa?" → Müsaitlik okul başınadır (seçili okul); iki okulun takvimi birbirini görmez, çakışmayı
  öğretmen kendisi yönetir (MVP).
- "Ret sonrası aynı veli aynı saati yeniden isteyebilir mi?" → Evet, dilim boşalır; açık randevu kuralı ret sonrası engel
  değildir.
- "Onaylı randevu başladıktan sonra durumu?" → "Onaylandı" kalır (gerçekleşti/gelmedi işareti kapsam dışı); veli geçmişte
  30 gün görür.
- "Veli iptalinden sonra dilim hemen başkasına açılır mı?" → Evet, başlamamışsa (AC-8).
- "Öğretmen dilimi açtıktan sonra sınıftan alınırsa?" → Dilimleri kalır ama velilerin listesinde görünmez (ilişki yok);
  var olan randevular iki tarafta görünür ve iptal edilebilir.
- "Gün sınırı?" → Türkiye saati; 60 gün bugünden sayılır; "başlamış" anı sunucu saatiyle (`TimeProvider`).
- "Bildirim hangi çocuğa bağlı?" → Randevunun çocuğu; 0011 okul bildirimi o öğrencinin velilerine gider — öğrencinin
  öteki velisi de görür; Randevularım'da da görür (aynı çocuk, tutarlı).

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
