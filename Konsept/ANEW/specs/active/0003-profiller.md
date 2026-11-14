# Spec 0003 — profiller

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0003-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisinin birden fazla sosyal medya kanalı olabiliyor (ör. biri tarih, biri teknoloji, biri futbol);
her kanal için ayrı video konsepti üretmek istiyor. Bu spec, kullanıcının kanallarını **profil** olarak
tanımlamasını sağlar: kanal adı, video dili, biçim (yatay/dikey), hedef süre ve kanal içeriği. Profil, üst
bardaki seçimle tüm sayfaların bağlamı olur (BR-2); kanal içeriği, kullanıcının görmediği arka planda konsept
üretimine giden düzgün bir sistem talimatına dönüşür. Yeni kullanıcı ne yazacağını görebilsin diye hazır
şablonlar sunulur. Başarı: kullanıcı üst bardaki artıyla profil ekler, kalemle seçili profili düzenler, profiller
arasında geçiş yapar ve seçimi sayfalar arasında korunur. Bilinçli olarak yapılmayanlar: örnek görsel alanı
(kullanıcı vazgeçti), ayrı bir "Profil" sayfası (üst bardan kaldırıldı, yerine Yayınlama geldi), konsept
üretiminin kendisi (0005).

## Requirements
1. Profil, giriş yapmış kullanıcıya aittir; kullanıcı yalnızca kendi profillerini görür, seçer, düzenler (BR-1).
   Bir kullanıcının birden çok profili olabilir.
2. Bir profilin alanları: **kanal adı**, **video dili**, **biçim** (yatay ya da dikey), **hedef süre**,
   **kanal içeriği** (kanalı tanıtan serbest metin: odak konusu, dönem/tarih aralığı gibi). Örnek görsel alanı yoktur.
3. Üst bardaki **artı** düğmesi, sayfanın üstünde açılan bir **profil modalı** açar (yeni profil). Kullanıcı
   alanları doldurup **Kaydet** derse profil kaydedilir, modal kapanır ve yeni profil üst bardaki profil
   seçiminde görünür. Yeni profilin kendiliğinden seçili profil olması bir öneridir (brief: "kaydedilecek ve
   sağ üstten profili seçecek"; Self-critique 11).
4. Üst bardaki **kalem** düğmesi, seçili profili aynı modalda, mevcut değerleriyle açar; **Kaydet** değişiklikleri
   kaydeder. Seçili profil yokken kalem kullanılamaz.
5. Yeni profil modalında **hazır şablonlar** sunulur. Bir şablon seçilince modalın alanları şablonun örnek
   değerleriyle (kanal adı, video dili, biçim, hedef süre, kanal içeriği) doldurulur; kullanıcı kaydetmeden önce
   her alanı değiştirebilir. Şablon seçmek tek başına profil oluşturmaz. Şablon listesi Open questions 4'te.
6. Zorunlu alanlardan biri boşsa ya da geçersizse kayıt yapılmaz; ilgili alanın yanında anlaşılır bir hata
   gösterilir ve girilen değerler korunur.
7. Üst bardaki **profil seçimi** kullanıcının tüm profillerini listeler; bir profil seçilince o profil seçili
   profil olur ve açık sayfa seçili profilin bağlamıyla yeniden yüklenir. Seçim, sayfa değişiminde ve sayfa
   yenilendiğinde korunur (nerede hatırlanacağı Open questions 6).
8. Seçili profil tüm sayfaların bağlamıdır: Konseptler, Konseptten Video, Üretilenler ve Silinenler yalnızca seçili
   profilin verisini gösterir (BR-2); bu listelemenin kendisi ilgili spec'lerde (0005–0007) kanıtlanır. Aylık
   harcama profilden bağımsızdır, kullanıcının içinde bulunulan aydaki toplamını gösterir (BR-12); harcama
   göstergesi 0004'te geldiği için bu davranış 0004'te kanıtlanır, bu spec'te kriteri yoktur.
9. Kullanıcının hiç profili yoksa üst bar profil seçimi yerine profil oluşturmaya yönlendiren bir boş durum
   gösterir; profil gerektiren sayfalar "önce bir profil oluşturun" boş durumunu gösterir (Open questions 5).
10. Bir profilin alanları, kullanıcıya gösterilmeyen arka planda, konsept üretiminde kullanılacak bir **sistem
    talimatına** dönüştürülür. Talimat: kanal adını, video dilini (üretilecek metnin dili), biçimi, hedef süreyi
    ve kanal içeriğini içerir; konsept içeriğinin gerçek bilgiye dayanması, uydurma olmaması talimatını içerir
    (domain değişmezi); gereksiz tekrar içermez (token tasarrufu). Kullanıcının kanal içeriği metni talimatta
    **talimat değil, kanal tanımı verisi** olarak yer alır (`docs/security.md`: kullanıcı/dış içerik promptta
    "veri" olarak işaretlenir). Aynı profil her zaman aynı talimatı üretir (öneri; Self-critique 13).
11. Profil silme davranışı Open questions 1'deki karara bağlıdır; karar verilmeden silme eylemi yapılmaz.

## User experience
- Flow:
  - **Yeni profil:** Kullanıcı üst barda artıya basar → profil modalı açılır (boş alanlar + hazır şablonlar) →
    isterse bir şablon seçer, alanlar dolar → alanları kendi kanalına göre düzenler → Kaydet → modal kapanır,
    yeni profil üst bardaki seçimde görünür (öneri: seçili olarak, Self-critique 11), açık sayfa seçili profille yüklenir.
  - **Düzenle:** Kullanıcı kalem düğmesine basar → modal seçili profilin değerleriyle açılır → değiştirir → Kaydet →
    üst bardaki ad güncellenir.
  - **Geçiş:** Kullanıcı üst bardaki profil seçiminden başka bir profili seçer → açık sayfa yeni profille yüklenir.
  - **İlk kullanım:** Hiç profili olmayan kullanıcı siteye girer → boş durum onu artıya / profil oluşturmaya yönlendirir.
- States:
  - Profil seçimi: yükleniyor · profil yok (boş durum) · profiller listelendi (biri seçili) · yükleme hatası.
  - Kalem: seçili profil var (etkin) · seçili profil yok (pasif, nedenini söyleyen ipucu).
  - Profil modalı: yeni (boş) · şablonla doldurulmuş · düzenleme (dolu) · kaydediliyor (Kaydet tekrar basılamaz) ·
    doğrulama hatası (alan bazında) · kayıt hatası (sunucu/bağlantı; değerler korunur) · başarı (modal kapanır).
  - Profil gerektiren sayfa: profil yok boş durumu · seçili profil bağlamında yüklendi.
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü: üst barın sağında
  "profil seçimi dropdown" ve "+" yer alır. Görseldeki "profil" sayfa bağlantısı **kaldırılmıştır** (yerine
  Yayınlama; ilk_promt.txt), artının yanına **kalem** eklenmiştir (ilk_promt.txt; sıra spec 0001 Requirements 1:
  profil seçimi · kalem · artı · hata bildir · ayarlar · tema). Modalın kendisi için tasarım yok — none, the flow
  above is the design; tema renkleri 0001'deki tema değişkenlerinden gelir.
- Accessibility: profil seçimi, kalem ve artı klavyeyle erişilebilir (Tab, Enter/Space) ve erişilebilir ada
  sahiptir ("Profil seç", "Seçili profili düzenle", "Yeni profil"); modal açılınca odak modalın ilk alanına gider,
  odak modal içinde kalır, Esc modalı kapatır, kapanınca odak açan düğmeye döner; her alanın görünür etiketi
  vardır ve hata metni alana bağlıdır; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki — yalnızca **sahiplik** (BR-1, AC-12). Gerekçe: profil, kullanıcıya ait
  yeni bir veri türüdür ve H-1'in tanımlı zararı ("bir kullanıcı başkasının verisine erişebilir") doğrudan bu
  kritere karşılık gelir; AC-12 için kasıtlı bozma kanıtı (sahiplik filtresi kaldırılınca test kırmızı) istenir.
  Giriş/oturum/ban mekanizmalarına dokunulmaz, yalnızca kullanılır (Self-critique 12). Sistem talimatı bu spec'te
  yalnızca oluşturulur; maliyet doğuran çağrıda kullanımı 0005'te H-3 kapsamındadır (bkz. Self-critique 5).
- Bağımlılık: 0002 (Kimlik) — profil bir kullanıcıya aittir, giriş gerektirir. 0001'deki pasif profil seçimi,
  kalem ve artı bu spec'le etkinleşir.
- Out of scope: örnek görsel alanı; ayrı profil sayfası; konsept üretimi ve profil talimatının bir modele
  gönderilmesi (0005); profile özel model seçimi ya da ince ayar varsayılanları (karar yok); profiller arası
  konsept taşıma; kullanıcının kendi şablonunu kaydetmesi; şablonların admin tarafından yönetilmesi; arayüzün
  çok dilli olması (arayüz yalnızca Türkçe, metinler resx'te — video dili ayrı konudur).
- Split: proposed 0003a "Profil modalı, şablonlar ve üst bar seçimi" (R1–R9, R11) → 0003b "Profil sistem
  talimatı" (R10; 0005'ten hemen önce ya da 0005'in içine katlanarak). Önerim: **tek spec kalsın** — talimat
  küçük, kullanıcı arayüzü yok ve profil alanlarıyla aynı anda değişiyor; ayırmak alan tanımını iki spec'e
  böler. Karar insanın (Open questions 7).

## Acceptance criteria
- [ ] AC-1 — Yeni profil: artıya basılınca modal boş alanlarla açılır; geçerli değerlerle Kaydet denince profil kaydedilir, modal kapanır, yeni profil üst bardaki seçimde görünür (Self-critique 11 onaylanırsa seçili olarak) ve sayfa yenilendikten sonra da listede durur.
- [ ] AC-2 — Modalda kanal adı, video dili, biçim (yatay/dikey), hedef süre ve kanal içeriği alanları vardır; örnek görsel alanı yoktur.
- [ ] AC-3 — Şablon: yeni profil modalında her hazır şablon listelenir; bir şablon seçilince beş alan şablonun değerleriyle dolar; kullanıcı alanları değiştirip kaydedince profil değiştirilmiş değerlerle kaydedilir; şablon seçip Kaydet'e basmadan modal kapatılırsa hiçbir profil oluşmaz.
- [ ] AC-4 — Düzenle: kalem, seçili profili mevcut değerleriyle açar; Kaydet sonrası değişiklikler kaydedilir, üst bardaki ad güncellenir ve başka hiçbir profil değişmez.
- [ ] AC-5 — Kaydetmeden kapatma (Esc, kapat düğmesi): yeni modalda profil oluşmaz; düzenleme modalında profil değişmeden kalır.
- [ ] AC-6 — Geçersiz giriş: zorunlu bir alan boşken, alan uzunluk sınırı aşılmışken, hedef süre izin verilen aralığın dışındayken ya da video dili/biçim listede olmayan bir değerken kayıt yapılmaz; ilgili alanın yanında hata görünür ve girilen değerler korunur. Aynı kontroller arayüzü atlayan doğrudan isteklerde de sunucuda uygulanır.
- [ ] AC-7 — Sınırlar: her alan, Open questions'ta onaylanan alt ve üst sınır değerlerinde kabul edilir, sınırın bir dışında reddedilir (sınır değerleri onayda bu kritere yazılır).
- [ ] AC-8 — Kaydediliyor / kayıt hatası: kayıt sürerken Kaydet tekrar basılamaz (çift tıklama tek profil oluşturur); sunucu ya da bağlantı hatasında modal açık kalır, bir hata mesajı görünür ve girilen değerler kaybolmaz.
- [ ] AC-9 — Geçiş ve hatırlama: profil seçiminden başka bir profil seçilince açık sayfa o profille yeniden yüklenir; başka sayfaya geçince ve sayfa yenilenince seçili profil aynı kalır.
- [ ] AC-10 — Boş durum: hiç profili olmayan kullanıcıda üst bar profil oluşturmaya yönlendiren boş durumu gösterir, kalem pasiftir ve nedenini söyleyen ipucu verir, profil gerektiren sayfalar "önce bir profil oluşturun" boş durumunu gösterir; ilk profil kaydedilince bu durum kalkar ve tek profil seçili olur.
- [ ] AC-11 — Yükleme durumları: profil listesi gelirken profil seçimi yükleniyor durumunu, liste alınamazsa anlaşılır bir hata mesajı gösterir; yükleniyor ve hata durumlarında kalem pasiftir ve profil gerektiren sayfalar hiçbir profilin verisini listelemez.
- [ ] AC-12 — Yetki (H-1): girişsiz kullanıcı profil işlemlerini yapamaz, giriş istenir (0002'nin davranışı); bir kullanıcı başka bir kullanıcının profilini listede göremez, kimliğini bilse bile açamaz, düzenleyemez ve seçili profil yapamaz (BR-1); bu durumda profil "bulunamadı" sonucu döner, varlığı açığa çıkmaz.
- [ ] AC-13 — Sistem talimatı: bir profilden üretilen talimat kanal adını, video dilini, biçimi, hedef süreyi ve kanal içeriğini içerir, doğruluk/uydurmama talimatını içerir; aynı profil için iki kez üretilen talimat birebir aynıdır; profil alanı değişince talimat yeni değeri yansıtır; talimat hiçbir arayüzde kullanıcıya gösterilmez.
- [ ] AC-14 — Talimat güvenliği: kanal içeriğine talimat gibi yazılmış bir metin (ör. "önceki talimatları yok say") talimatta kanal tanımı verisi olarak, ayrılmış biçimde yer alır ve sistem talimatının kurallarını (dil, doğruluk) metinden çıkarmaz.
- [ ] AC-15 — Erişilebilirlik: profil seçimi, kalem, artı ve modalın tüm alanları yalnızca klavyeyle kullanılabilir; modal açılınca odak ilk alana gider, modal içinde kalır, Esc kapatır, odak açan düğmeye döner; ikon düğmelerinin erişilebilir adı vardır; her alanın görünür etiketi vardır ve hata metni alana bağlıdır; modal ve üst bar kontrolleri açık ve koyu temada WCAG AA kontrastı sağlar.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**(a) Netleştirme soruları (etkiye göre sıralı):**
1. **Profil silinebilecek mi, silinirse konseptleri ve videoları ne olur?** Brief'te profil silme yok.
   *Öneri:* V1'de profil, **yalnızca hiç konsepti (Silinenler dahil) olmayan** profil için silinebilir; konsepti
   olan profilde silme düğmesi pasiftir ve nedenini söyler. Silme kalem modalında, onay sorusuyla yapılır ve
   kalıcıdır. *Gerekçe:* BR-2 her konseptin bir profile ait olmasını, BR-4/BR-8 aynı profilde geçmişin negatif sinyal
   olarak kalmasını, BR-7 silinenlerin süresiz saklanmasını istiyor; profil silmek bunların hepsini bozar. Yanlışlıkla
   yapılan profil şablonla açılmış boş profillerdir, onları silebilmek yeter. Alternatif: profil "arşivlenir"
   (listeden gizlenir, verisi kalır) — daha fazla iş, bugün bir ihtiyaç görünmüyor.
2. **Profil değişince eski konseptler ne olur?** (ör. biçim dikeyden yataya, dil Türkçeden İngilizceye)
   *Öneri:* Profil değişikliği **yalnızca bundan sonraki üretimleri** etkiler; mevcut konseptler ve videolar
   üretildikleri andaki dil/biçim/süreyle kalır (konsept bunları kendisi taşır). *Gerekçe:* sahneler ve seslendirme
   metni belirli bir biçim ve dile göre yazıldı; geriye dönük değiştirmek yarım bir video üretir ve BR-9'daki
   "konsept metni değişirse her şey baştan" maliyetini habersiz tetikler. (0005 ve 0006 bunu devralır.)
3. **Video dili listesi ne olacak?** *Öneri:* **Türkçe** ve **İngilizce**, varsayılan Türkçe; liste genişletilebilir
   tutulur. *Gerekçe:* brief seslendirme modellerini "sadece Türkçe / sadece İngilizce / çok dilli" diye etiketliyor;
   ses tarafının desteklemediği bir dili profilde sunmak üretimde hata çıkarır. Yeni dil, B-5 raporu ve ses modeli
   desteği netleşince eklenir.
4. **Hazır şablonlar hangileri ve içerikleri kim yazar?** Brief: "tarih Bizans, tarih Osmanlı, teknoloji, antik
   teknoloji, teknoloji günümüz teknolojisi". *Öneri:* 5 şablon: **Tarih – Bizans**, **Tarih – Osmanlı**,
   **Teknoloji**, **Antik teknoloji**, **Günümüz teknolojisi**; her birinin örnek kanal adı, dil (Türkçe), biçim,
   hedef süre ve 2–4 cümlelik kanal içeriği metnini plan aşamasında ben taslak yazarım, sen onaylarsın. Şablonlar
   tüm kullanıcılar için aynıdır, kullanıcı değiştiremez. *Gerekçe:* konuşma-yazı dökümünde "teknoloji günümüz
   teknolojisi" tek mi iki şablon mu belirsiz; "Teknoloji" genel, "Günümüz teknolojisi" dar odaklı iki örnek olarak
   kullanıcıya kanal içeriğinin ne kadar dar yazılabileceğini de gösterir. Tek şablon istersen "Teknoloji"yi çıkarırım.
5. **Hiç profil yokken ne olur?** *Öneri:* Otomatik profil oluşturulmaz; üst bar ve profil gerektiren sayfalar
   "önce bir profil oluşturun" boş durumu gösterir (R9, AC-10), yönlendirme artıyı açar. *Gerekçe:* BR-2 her konsepti
   bir profile bağlıyor; uydurma bir "Varsayılan" profil yanlış dil/biçimle üretim yaptırabilir. Alternatif: ilk
   girişte profil modalı kendiliğinden açılır — daha hızlı ama kayıttan hemen sonra rahatsız edici olabilir.
6. **Seçili profil nerede hatırlansın?** *Öneri:* **Kullanıcı hesabında** (sunucuda) "son seçilen profil" olarak;
   başka bir tarayıcıdan girişte de aynı profil seçili gelir. Seçili profil silinmiş ya da erişilemezse ilk oluşturulan
   profil seçilir. *Gerekçe:* site yerel ağda birden çok cihazdan açılabiliyor; yalnızca tarayıcıda tutmak her cihazda
   farklı profil demek ve yanlış kanala üretim riski doğurur.
7. **Spec bölünsün mü?** (15 kriter) *Öneri:* **Tek spec kalsın** (Constraints, Split). *Gerekçe:* sistem talimatı
   (R10, AC-13/14) küçük, arayüzsüz ve profil alanlarıyla birlikte değişiyor; ayırmak alan tanımını iki yere böler.
   Bölünmesini istersen 0003a (modal, şablon, seçim) → 0003b (talimat, 0005'ten önce) sırasıyla yazarım.

**(b) Self-critique boşlukları (düşmanca geliştirici gözüyle, önerilerimle):**
1. **Alan sınırları tanımsız (AC-6, AC-7).** *Öneri:* kanal adı 1–80 karakter; kanal içeriği 1–2000 karakter
   (token tasarrufu, talimata olduğu gibi girer); baştaki/sondaki boşluklar kırpılır, yalnızca boşluk = boş.
2. **Hedef süre biçimi ve aralığı yok.** *Öneri:* dakika + saniye girişi; aralık **15 sn – 15 dk** (domain: "kısa
   dikey videodan 15 dakikalık yatay videoya kadar"); biçimle süre arasında zorunlu bağ yok (dikey 15 dk da kaydedilir).
3. **Kanal adı benzersiz mi?** *Öneri:* aynı kullanıcı içinde benzersiz, büyük/küçük harf duyarsız ve Türkçe kurala
   göre (tr-TR: "İstanbul" = "istanbul"); farklı kullanıcılar aynı adı kullanabilir. Aksi halde profil seçimi ayırt edilemez.
4. **Profil sayısı sınırı yok.** *Öneri:* kullanıcı başına en fazla 20 profil; 21.'de artı bir uyarı verir.
   Gerekçe: üst bar listesi okunabilir kalsın; gerçek ihtiyaç 3–5 kanal. Sınır istemezsen kaldırırım.
5. **Talimat uzunluğu ve maliyet.** Talimat her konsept çağrısına girdiği için uzunluğu doğrudan token maliyeti
   (H-3). *Öneri:* AC-13'e "boş/gereksiz alan eklemez" ve sabit metin kısmı için bir üst sınır (ör. kanal içeriği
   hariç ~150 token) eklenir; 0005 bu talimatı kullanırken H-3 kapsamında test eder.
6. **Şablon düzenleme modunda var mı?** *Öneri:* yalnızca yeni profil modalında; düzenlemede şablon seçmek kullanıcının
   kanal içeriğini sessizce ezebilir. Yeni modalda dolu alan varken şablon seçilirse onay sorulur.
7. **Kaydedilmemiş değişiklikle kapatma uyarısı.** *Öneri:* alanlarda değişiklik varken Esc/kapat "Değişiklikler
   kaydedilmeyecek" onayı sorar; değişiklik yoksa doğrudan kapanır (AC-5'e eklenir).
8. **Profil seçimi sırası.** *Öneri:* profiller kanal adına göre alfabetik (tr-TR) listelenir.
9. **Profil değiştirince süren işler.** Kuyrukta başka profilin işi üretilirken profil değiştirmek ne yapar?
   *Öneri:* profil seçimi yalnızca **görüntülenen** bağlamı değiştirir; çalışan üretim sırası ve işler etkilenmez.
   Kuyruğun profiller arası mı profil başına mı olacağı 0006'nın Open questions'ına taşınır.
10. **Eşzamanlı düzenleme (iki sekme).** *Öneri:* son kaydeden kazanır; V1 tek kullanıcı ve yerel ağ, kilit gereksiz.
11. **Yeni profil kendiliğinden seçilsin mi? (R3, AC-1, AC-10)** Brief yalnızca "kaydedilecek ve sağ üstten profili
    seçecek" diyor; seçimi kullanıcının mı yapacağı, otomatik mi olacağı açık değil. *Öneri:* Kaydet sonrası yeni
    profil kendiliğinden seçili olur. *Gerekçe:* profil eklemenin hemen ardından o kanal için çalışmak beklenir; ilk
    profilde başka seçenek zaten yok.
12. **Hassas alan sınıflandırması.** Yazar "yok" demişti; bu inceleme H-1 (yalnızca sahiplik) olarak değiştirdi.
    *Öneri:* H-1 kalsın; AC-12 için kasıtlı bozma kanıtı ve her düzeltme turunda yeniden review. *Gerekçe:*
    `docs/domain.md` H-1 zararı birebir "başkasının verisine erişim"; profil, sonraki tüm sayfaların bağlamı olduğu
    için burada sızan bir sahiplik hatası 0005–0007'ye de yayılır.
13. **Talimatın birebir aynı olması (R10, AC-13) brief'te yok.** *Öneri:* kalsın. *Gerekçe:* brief token
    optimizasyonu istiyor; sabit talimat sağlayıcı tarafı prompt önbelleğine uygun olur ve testte talimatın
    beklenmedik değişmesini yakalar. İstemezsen AC-13'ten "birebir aynıdır" kısmı çıkar.

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
