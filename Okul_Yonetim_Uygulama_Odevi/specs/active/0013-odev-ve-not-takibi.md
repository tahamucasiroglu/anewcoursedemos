# Spec 0013 — Ödev ve not takibi

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0013-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Öğretmen bugün ödevi tahtaya yazıyor, kimin getirdiğini defterde işaretliyor, yazılı ve sözlü notları ayrı bir çizelgede
tutuyor; veli çocuğunun ödevini ve notunu ancak çocuk söylerse ya da karne gelince öğreniyor. Bu spec yol haritasının 7.
satırını getirir: öğretmen telefondan kendi sınıfına **ödev verir** (ders, başlık, açıklama, teslim tarihi), teslim
günü **kim getirdi / kim getirmedi** işaretler ve sınıfı için **yazılı ya da sözlü not** girer; veli telefonda her çocuğu
için **sade bir özet** görür: bekleyen ödevler, son ödevlerin teslim durumu, son notlar. Yeni ödev verilince veliye
iletişim modülünün bildirim kapısından haber gider. Başarı: öğretmen 30 kişilik sınıfa bir ödevi 20 saniyede verir,
teslim günü 3 "getirmedi" işaretini 5 dokunuşla kaydeder; veli akşam uygulamayı açınca "Matematik — Kesirler alıştırması,
teslim 25.11.2026" ve "Türkçe 1. yazılı: 85" görür. Bilinçli olarak yapılmayanlar: ödeve dosya/fotoğraf eki, öğrencinin
kendisinin teslim etmesi (öğrenci uygulaması Faz 2), ortalama/karne/ağırlıklı not hesabı ve e-Okul'a not aktarma, ders
kataloğu (ders programıyla gelir), web ekranları, kurucu raporları (yol haritası 11).

**Bağımlılık:** spec 0008 (sınıf, dönem, Türkiye günü), 0009 (öğrenci, sınıfa kayıt, veli bağı, "Çocuklarım"), 0010
(sınıf öğretmeni bağı: öğretmen yalnızca atandığı sınıfları görür) ve 0011 (bildirim kapısı ve velinin duyuru akışında
"Okul bildirimi") bu spec'ten önce yapılır. Adlar o spec'ler bitince plan değişikliğiyle (amendment) eşlenir. Spec 0014
(AI haftalık gelişim özeti) bu spec'in ödev ve not verisini buradaki öğrenci bazlı okuma sorgusuyla okuyacak.

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Ödev nedir, kim verir?** Öğretmen yalnızca atandığı (0010) sınıfa ödev verir: ders adı (serbest metin, zorunlu,
  en fazla 60), başlık (zorunlu, en fazla 150), açıklama (isteğe bağlı, en fazla 2000), teslim tarihi (bugün ile bugünden 90
  gün sonrası arası, Türkiye saati). Ödev sınıfın tamamına verilir; tek öğrenciye ödev, dosya eki, ödev düzenleme ve silme
  yok (yanlışsa yeni ödev). Neden: ders kataloğu ve ders programı henüz yok; serbest ders adı öğretmeni bekletmez; düzenleme
  ve silme veliye gitmiş bildirimi anlamsızlaştırır (0011 duyuru kararıyla aynı).
  - Dokümanda istenen: "Ödev ve not takibi — Ödev verme, teslim durumu, yazılı/sözlü not girişi" (kapsam, MVP modülü 5);
    rol tablosu "Öğretmen | Yoklama, not, ödev, veli mesajı | Mobil + Web".
- **C-2 — Teslim durumu nasıl tutulur?** Öğrenci uygulaması olmadığı için **öğretmen işaretler**: her öğrenci için
  "İşaretlenmedi" (başlangıç) / "Teslim etti" / "Teslim etmedi". Yoklama kalıbı: liste, tek dokunuş, tek seferde kaydet;
  istenen zaman düzeltilebilir (gün kilidi yok). Neden: öğretmenin defterdeki işinin birebir karşılığı; "işaretlenmedi"
  velinin yanlışlıkla "getirmedi" görmesini önler.
  - Dokümanda istenen: "teslim durumu"; "Öğrenci uygulaması — Ödev, sınav sonucu, program — Faz 2".
- **C-3 — Not nasıl girilir?** Öğretmen sınıfı için bir **değerlendirme** açar: ders adı (serbest, en fazla 60), tür
  (Yazılı / Sözlü), başlık (ör. "1. yazılı", en fazla 100), tarih (etkin dönem içinde, bugünden sonra olamaz); sonra her
  öğrenciye 0–100 arası tam sayı puan girer ya da boş bırakır (girilmedi). Puan sonradan düzeltilebilir. Ortalama, ağırlık,
  karne ve harf notu yok. Neden: e-Okul resmî kayıt olarak kalır (kapsam "MEB süreçlerinin yerine geçmek" kapsam dışı);
  veliye en çok gereken tek tek puanlardır.
  - Dokümanda istenen: "yazılı/sözlü not girişi"; kapsam dışı: "MEB süreçlerinin yerine geçmek: e-Okul resmi kayıt sistemi
    olarak kalır".
- **C-4 — Kim görür, kim değiştirir?** Sınıfa atanmış her öğretmen sınıfın ödevlerini ve değerlendirmelerini görür;
  teslim durumunu ve puanları **yalnızca oluşturan öğretmen** değiştirir. Veli yalnızca kendi çocuklarınınkini görür
  (farklı şubedeki kardeş dahil, BR-2). Kurucu ve muhasebe bu spec'te görmez (raporlar yol haritası 11). Neden: branş
  öğretmeni ayrımı ders programına kadar yok; "oluşturan değiştirir" kuralı başkasının notunu ezmeyi en sade yolla önler.
  - Dokümanda istenen: güvenlik "öğretmen yalnızca kendi sınıflarını görür", "veli yalnızca kendi çocuğunu"; BR-2.
- **C-5 — Veliye bildirim gider mi?** Yalnızca **yeni ödevde**: 0011'in bildirim kapısından, ödevin verildiği andaki
  sınıf öğrencilerinin etkin velilerine "Okul bildirimi" (ders, başlık, teslim tarihi). Teslim durumu ve not için bildirim
  yok; veli özette görür. Neden: ödev haberinin zamanı önemli (teslim tarihi), not ve teslim bilgisi özet ekranında ve
  haftalık özette (0014) yeterli; bildirim yorgunluğu önlenir.
  - Dokümanda istenen: "Veli tarafında sade özet görünüm"; mimari "Communication … veliye giden her şeyin tek çıkışı".
- **C-6 — Hangi kanal?** Öğretmen ekranları ve veli özeti **mobilde**. Öğretmenin web'den ödev/not girmesi sonraki adım.
  Neden: hızlı işler (ödev ver, işaretle, puan yaz) telefonda; veli yalnızca mobil (kapsam); tek kanal boyutu yarıya indirir.
  - Dokümanda istenen: rol tablosu "Öğretmen — Mobil + Web", "Veli — Mobil".
- **C-7 — Velinin özeti ne gösterir?** Çocuk başına tek ekran: **Bekleyen ödevler** (teslim tarihi bugün ya da sonra; en
  yakın üstte), **Son ödevler** (teslim tarihi son 30 günde geçmiş; durumuyla, "Teslim etmedi" yazıyla belirgin), **Notlar**
  (çocuğun etkin dönem içindeki bütün değerlendirmeleri; yeniden eskiye; ders, tür, başlık, tarih, puan ya da
  "girilmedi"). Neden: "sade özet" — üç soruya cevap: ne yapacak, ne yaptı, nasıl gidiyor.
  - Dokümanda istenen: "Veli tarafında sade özet görünüm"; "Kardeşi olan veli tek hesapla tüm çocuklarını görür".

## Requirements
1. **Ödev verme:** Öğretmen mobilde atandığı bir sınıf için ders adı, başlık, isteğe bağlı açıklama ve teslim tarihiyle
   ödev verir (sınırlar C-1). Ödev, verildiği andaki sınıf öğrencileri için "İşaretlenmedi" durumuyla başlar; kim, ne zaman
   verdi kayıtlıdır. Ödev değiştirilemez ve silinemez.
2. **Yeni ödev bildirimi:** Ödev kaydedilince, ödevin öğrencilerinin okulda etkin her velisine bildirim kapısından bir "Okul
   bildirimi" gider: "Yeni ödev — {ders}: {başlık}. Teslim: {gün.ay.yıl}." Bildirim yalnızca ders, başlık ve teslim tarihini
   taşır (açıklama yok). Gönderici hata verirse ödev yine kaydedilir.
3. **Ödev listesi (öğretmen):** Sınıfın ödevleri teslim tarihine göre yeniden eskiye, her satırda ders, başlık, teslim
   tarihi, veren öğretmen ve "x / y teslim etti" sayısıyla. Boşken "Henüz ödev yok".
4. **Teslim durumu:** Ödevi veren öğretmen, ödevin öğrencilerini (Türkçe ad sırasıyla) görür; her öğrenci için Teslim
   etti / Teslim etmedi / İşaretlenmedi seçer ve tek seferde kaydeder; sonuç "Kaydedildi — 27 teslim etti, 3 etmedi."
   Başka öğretmen listeyi yalnızca okur. Sınıftan ayrılan (kaydı biten) öğrenci listede görünmez, kaydı silinmez.
5. **Değerlendirme (not):** Öğretmen atandığı sınıf için ders adı, tür (Yazılı/Sözlü), başlık ve tarihle değerlendirme
   açar (sınırlar C-3); değerlendirme sınıfın o andaki öğrencilerini kapsar.
6. **Puan girişi:** Değerlendirmeyi açan öğretmen her öğrenciye 0–100 arası tam sayı puan girer ya da boş bırakır; tek
   seferde kaydedilir; bir puan geçersizse hiçbiri kaydedilmez (alan hatası, ilgili öğrenci belirtilir). Puan sonradan
   düzeltilebilir ya da silinebilir (boş). Değerlendirme listesi tarihe göre yeniden eskiye, "x / y puan girildi" ile.
7. **Veli özeti:** Veli mobilde her çocuğu için C-7'deki üç bölümü görür; yalnızca kendi çocukları. Bölüm boşsa "Bekleyen
   ödev yok", "Son 30 günde ödev yok", "Henüz not yok".
8. **Öğrenci bazlı okuma (spec 0014 için):** Uygulama içinde bir öğrenci ve tarih aralığı verilince o öğrencinin ödevlerini
   (ders, başlık, teslim tarihi, durum) ve notlarını (ders, tür, başlık, tarih, puan) döndüren tek bir okuma vardır; veli
   özeti de aynı okumayı kullanır. Yalnızca o öğrencinin kayıtları gelir; okul sınırı geçerlidir.
9. **Gizlilik ve izolasyon:** Başka okulun, yetkisiz şubenin, atanmadığı sınıfın ödevi/değerlendirmesi ve ilişkisi olmayan
   öğrencinin özeti "bulunamadı" (404). Öğrenci adı, ödev metni ve puan log satırlarına ve bildirim log'una yazılmaz
   (yalnızca kimlikler).
10. **Audit:** Ödev verme, teslim durumu kaydı ve değerlendirme açma, puan kaydı/düzeltmesi audit'e yazılır (kim, hangi
    sınıf ve kayıt, kaç öğrencinin durumu/puanı değişti, ne zaman, IP); puan değerleri audit'e yazılmaz.

## User experience
- **Flow — mobil (öğretmen):** Hoş geldin → "Ödev ve not" (yalnızca öğretmende) → [birden çok sınıf] sınıf seç (0010'un
  Sınıflarım kalıbı; tek sınıfta doğrudan) → iki sekme: **Ödevler** | **Notlar**. Ödevler: liste → "Yeni ödev" formu (ders,
  başlık, açıklama, teslim tarihi — varsayılan yarın) → kaydet → listeye döner · ödeve dokun → teslim listesi (hepsi
  "İşaretlenmedi") → işaretle → "Kaydet (3 etmedi)" → sonuç. Notlar: liste → "Yeni değerlendirme" (ders, Yazılı/Sözlü,
  başlık, tarih — varsayılan bugün) → puan listesi (sayı klavyesi, sonraki öğrenciye geç) → "Kaydet" → sonuç.
- **Flow — mobil (veli):** Hoş geldin → "Ödev ve notlar" (yalnızca velide) → [birden çok çocuk] çocuk seç → özet (üç
  bölüm); ayrıca 0009'un çocuk kartından "Ödev ve notlar" bağlantısı. Yeni ödev bildirimi 0011 duyuru akışında görünür.
- **States:** sınıf/çocuk seçimi: yükleniyor / boş ("Size atanmış sınıf yok", "Kayıtlı çocuğunuz yok") / hata + tekrar dene.
  Ödev ve değerlendirme listeleri: yükleniyor / boş / dolu / hata + tekrar dene. Formlar: alan hataları (istek gitmez) /
  kaydediliyor (düğme kilitli) / sunucu hatası duyurulur (girilenler kaybolmaz) / başarı. Teslim ve puan listeleri:
  yükleniyor / öğrenci yok ("Bu ödevde öğrenci yok") / salt okunur (başka öğretmenin kaydı: düğmeler yok, "Yalnızca
  {öğretmen} değiştirebilir") / kaydediliyor / kaydedildi / hata (işaretler ve puanlar kaybolmaz). Veli özeti: yükleniyor /
  bölüm bazında boş / dolu / hata + tekrar dene / "Bu öğrenci artık görüntülenemiyor." Rolü olmayana menü görünmez.
- **Design reference:** none — the flow above is the design (spec 0010 yoklama listesi ve 0009 "Çocuklarım" kalıbı, ortak tema).
- **Accessibility:** durum düğmesi ve puan alanı etiketleri öğrenci adını içerir ("Ayşe Yılmaz: Teslim etmedi", "Ayşe
  Yılmaz puanı"); seçili durum ekran okuyucuya bildirilir; dokunma alanı en az 44×44; "Teslim etmedi" yalnızca renkle değil
  yazıyla; sonuç ve hatalar duyurulur; iki temada kontrast spec 0002 eşiklerinde.

## Constraints & out of scope
- Sensitive area: S1 (okul/şube sınırı, öğretmen yalnızca atandığı sınıf), S2 (öğrenci adı + not ve ödev durumu), S3 (rol,
  sınıf öğretmeni ve veli-öğrenci bağına dayalı yetki), S5 (veliye giden yeni ödev bildirimi), S6 (audit).
- Bağımlılık: 0008, 0009, 0010 ve 0011 tamamlanmadan build başlamaz.
- Performans: 40 öğrencilik sınıfta teslim listesi ve puan listesinin açılması ve kaydı her biri en fazla 1 sn (yerel
  geliştirme makinesi, test verisi); Pi ölçümü ship'te kullanıcıda.
- Kapsam dışı / sonra: ödev düzenleme, silme, geri çekme; tek öğrenciye ya da birden çok sınıfa ödev; ödeve dosya/fotoğraf eki
  (dosya spec'i); öğrencinin kendisinin teslim etmesi (Faz 2 öğrenci uygulaması); geç teslim durumu; ortalama, ağırlık,
  karne, harf notu, 0–100 dışı ölçekler (ör. anaokulu gözlem ölçeği); ders kataloğu (ders programı spec'i); not ve teslim
  bildirimi, teslim hatırlatması; öğretmenin web ekranları; kurucu/muhasebe görünümü ve raporlar, Excel'e aktarma (yol
  haritası 11); e-Okul'a not aktarma ya da e-Okul'dan alma; geçmiş dönemlerin özeti; push (B-6), SMS (B-2).
- Split: proposed 0013a "Ödev ve teslim durumu" + 0013b "Yazılı/sözlü not"; kept as one — tahamucasiroglu (önceden
  verilen onay, Promtlar 0082), 2026-11-20: yol haritası satırı tek, ikisi aynı sınıf/yetki kalıbını ve aynı veli özetini
  paylaşıyor; web ekranları çıkarıldığı için 13 kriterde kalıyor.

## Acceptance criteria
- [ ] AC-1 — Ödev verme: öğretmen atandığı sınıfa ders, başlık, açıklama ve teslim tarihiyle ödev verir ve ödev listede görünür; boş ders/başlık, 60/150/2000 sınırını aşan alan, dünkü ya da 91 gün sonraki teslim tarihi alan hatası verir ve hiçbir şey oluşmaz (sınırdaki 60/150/2000, bugün ve 90. gün kabul edilir); ödevin öğrencileri verildiği andaki sınıf öğrencileridir, hepsi "İşaretlenmedi".
- [ ] AC-2 — Yeni ödev bildirimi: ödev kaydedilince ödevin her öğrencisinin okulda etkin her velisine bildirim kapısından bir "Okul bildirimi" geçer, metin gereksinim 2'deki gibidir ve o velinin duyuru akışında yalnızca o çocuk için görünür; pasif veliye ve başka sınıfın velisine gitmez; gönderici hata verirse ödev yine kaydedilir.
- [ ] AC-3 — Ödev listesi: sınıfa atanmış öğretmen sınıfın bütün ödevlerini teslim tarihine göre yeniden eskiye, ders, başlık, teslim tarihi, veren öğretmen ve doğru "x / y teslim etti" sayısıyla görür; ödevi olmayan sınıfta "Henüz ödev yok".
- [ ] AC-4 — Teslim durumu: ödevi veren öğretmen öğrencileri Türkçe ad sırasıyla görür, Teslim etti / Teslim etmedi / İşaretlenmedi seçip tek seferde kaydeder; sonuç sayıları doğru; yeniden açınca kayıtlı durumlar gelir ve düzeltilebilir; sınıftan ayrılan öğrenci listede yoktur ama kaydı velinin özetinde kalır.
- [ ] AC-5 — Değerlendirme açma: öğretmen atandığı sınıf için ders, Yazılı/Sözlü, başlık ve tarihle değerlendirme açar; boş alan, 60/100 sınırını aşan alan, geçersiz tür, gelecek tarih ya da etkin dönem dışı tarih alan hatası verir ve hiçbir şey oluşmaz (bugün ve dönemin ilk günü kabul edilir).
- [ ] AC-6 — Puan girişi: değerlendirmeyi açan öğretmen 0–100 tam sayı puanları tek seferde kaydeder, boş bırakılan "girilmedi" kalır; -1, 101, ondalık ya da sayı olmayan değer o öğrenciyi belirten alan hatası verir ve hiçbir puan kaydedilmez (0 ve 100 kabul); kayıtlı puan düzeltilir ve silinir; liste "x / y puan girildi" sayısını doğru gösterir.
- [ ] AC-7 — Değiştirme yetkisi: aynı sınıfın başka öğretmeni ödevin teslim durumunu ve değerlendirmenin puanlarını görür ama değiştirmeye çalışırsa 403 alır ve hiçbir şey değişmez.
- [ ] AC-8 — Veli özeti: veli her çocuğu için bekleyen ödevleri (teslim tarihi bugün ya da sonra, en yakın üstte), teslim tarihi son 30 günde geçmiş ödevleri durumlarıyla ve etkin dönemdeki notları (yeniden eskiye; puan ya da "girilmedi") görür; 31 gün önceki ödev görünmez; farklı şubedeki kardeşin özeti de açılır; boş bölümlerde "Bekleyen ödev yok", "Son 30 günde ödev yok", "Henüz not yok".
- [ ] AC-9 — Yetki ve izolasyon: öğretmen atanmadığı sınıfın ve başka şube/okulun ödev ve değerlendirmelerini okuyamaz ve yazamaz (404); öğretmen rolü olmayan (veli, muhasebe, atanmamış kurucu) öğretmen işlemlerinde 403; veli bağlı olmadığı öğrencinin ve başka okulun öğrencisinin özetinde 404, veli rolü olmayan veli özetinde 403; sınıftan alınan ya da pasifleştirilen öğretmenin bir sonraki isteği reddedilir; girişsiz istek 401.
- [ ] AC-10 — Öğrenci bazlı okuma: verilen öğrenci ve tarih aralığı için okuma, aralıktaki ödevleri (teslim tarihine göre) durumlarıyla ve notları (değerlendirme tarihine göre) puanlarıyla döndürür; aralık dışı kayıt, aynı sınıfın başka öğrencisinin durumu/puanı ve başka okulun kaydı dönmez.
- [ ] AC-11 — Gizlilik ve audit: log ve bildirim log satırlarında öğrenci adı, ödev başlığı/açıklaması ve puan geçmez (yalnızca kimlikler); ödev verme, teslim kaydı, değerlendirme açma ve puan kaydı/düzeltmesi audit kaydı oluşturur (kim, sınıf, kayıt, değişen öğrenci sayısı, ne zaman, IP), audit kaydında puan değeri yoktur.
- [ ] AC-12 — Hız: 40 öğrencilik sınıfta teslim listesi ve puan listesinin açılması ve kaydı her biri ≤ 1 sn (yerel ölçüm); 3 "getirmedi" işareti tek sınıflı öğretmende listeden ödeve dokunma + 3 dokunuş + "Kaydet" = 5 dokunuşla kaydedilir (onay iletişimi yok).
- [ ] AC-13 — Mobil ekranlar (iki tema): "Ödev ve not" yalnızca öğretmende, "Ödev ve notlar" yalnızca velide; sınıf/çocuk seçimi, ödev ve değerlendirme listeleri, formlar, teslim ve puan listeleri ve veli özetinin User experience'taki bütün durumları (yükleniyor, boş, dolu, alan hataları, kaydediliyor-düğme kilitli, kaydedildi, hata + tekrar dene ve girilenlerin korunması, salt okunur, artık görüntülenemiyor); etiketler öğrenci adını içerir ve seçili durum bildirilir.

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Ödevden sonra sınıfa katılan öğrenci?" → Ödevin ve değerlendirmenin öğrencileri oluşturulduğu andaki sınıf öğrencileridir
  (0011'in "alıcılar yayın anında" kuralıyla aynı); sonradan gelen öğrenci o kayıtlarda yoktur (AC-1).
- "Teslim tarihi geçince otomatik 'etmedi' olur mu?" → Hayır; öğretmen işaretlemedikçe "İşaretlenmedi" kalır, veli de öyle görür.
- "Aynı öğretmen iki telefondan aynı anda kaydederse?" → Öğrenci başına son kayıt geçerli; ödev başına tek satır kuralı
  (ödev + öğrenci tekil) çift satırı önler; ayrı bir eşzamanlılık kriteri yok (tek sahipli kayıt).
- "Değerlendirmenin dönemi?" → Sınıfın dönemi; tarih o dönemin içinde olmalı (AC-5). Veli özeti öğrenci bazlıdır (gereksinim 8):
  etkin dönemin notları, sınıf değiştirse bile; geçmiş dönem özeti kapsam dışı.
- "Öğretmen sınıftan alınınca verdiği kayıtlar?" → Kayıtlar kalır, sınıfın yeni öğretmenleri görür; kimse değiştiremez
  (sahip artık atanmış değil). Bu MVP'de kabul; gerekirse "devral" sonra.
- "Bildirim kaç tane?" → Velinin her çocuğu için bir (0011 okul bildirimi öğrenci başınadır); aynı sınıfta iki çocuğu
  olan veli iki bildirim görür.
- "Puan ölçeği okul başına değişebilir mi?" → Hayır, 0–100 sabit; başka ölçekler kapsam dışı.

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
