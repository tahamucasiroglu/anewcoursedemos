# Spec 0006 — uretim-konseptten-video

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0006-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisi, Konseptler sayfasında video için seçtiği (Onaylandı) konseptleri Konseptten Video
sayfasında üretim sırasına alıyor, sırayı düzenliyor ve tek tuşla yukarıdan aşağı videoya çevirtiyor;
yüzlerce konsepti bir gecede üretebilmesi hedefleniyor. Videonun nasıl üretileceğini (altyazı, ilerleme
çubuğu, video kodu modeli ve eforu, video motoru, görsel modeli ya da "koddan çiz", ses modeli ya da
"sessiz") bu sayfadan seçiyor; her işin aşama aşama nerede olduğunu görüyor, durdurup kaldığı yerden
devam ettirebiliyor. Video kodu Pi'de online modelle üretiliyor, Pi bunu kullanıcının bilgisayarındaki
ajana iletiyor, ajan yalnızca kendi iş klasörüne erişebilen ve ağı kapalı bir ortamda render ediyor;
biten videolar sağ panelde listelenip izlenebiliyor. Başarı: seçilen ayarlar videoya gerçekten yansıyor
(altyazı ve ilerleme çubuğu "gerçekten çalışmalı"), durdur/devam tamamlanmış aşamaları boşa harcamıyor,
her online çağrının maliyeti kaydediliyor. Bilinçli olarak yapılmayanlar: Üretilen Videolar sayfası,
yeni sürüm / kısmi yeniden üretim (BR-9 akışı), videonun silinmesi ve Silinenler'in video tarafı (0007);
ajanın kurulum ve bağımlılık indirme ekranı (0009); Günlük şeridinin geçmiş akışı (0008); yayınlama.

## Requirements
**A. Üretim ayarları ve model seçimi**
1. Sayfanın sağ üstünde iki aç/kapa seçimi vardır: **Altyazı** ve **İlerleme çubuğu**. Altyazı açıksa
   üretilen videoda seslendirilen metin ekranda, sesle zamanlı olarak görünür ve o an okunan kısım
   vurgulanır (rengi değişir). İlerleme çubuğu açıksa videonun üst kenarında, videonun başından sonuna
   kadar geçen süreyle orantılı olarak dolan bir çubuk görünür. İkisi de kapalıysa videoda bunlardan
   hiçbiri yoktur. Seçim, işin ürettiği videoya gerçekten yansır (ilk_promt: "gerçekten çalışması lazım").
2. Sayfanın sol üstünde, Konseptler sayfasındaki model bilgisi alanıyla aynı biçimde bir **model seçimi**
   özeti durur (seçili modellerin adı ve efor seviyesi). Tıklanınca sayfanın üstünde ayrı bir modal açılır;
   modal, dışına tıklanınca ya da Kapat ile kapanır ve seçimler kaydedilir. Modalda dört seçim vardır:
   - **Video kodu modeli** + **efor seviyesi** (modelin desteklediği seviyeler, low … max);
   - **Video motoru** (varsayılan HyperFrames; listenin geri kalanı Open question S-2'ye bağlı);
   - **Görsel modeli** ya da **Kapalı — koddan çiz** (görsel üretilmez, sahneler yalnızca video koduyla çizilir);
   - **Ses modeli** ya da **Kapalı — sessiz video** (seslendirme yapılmaz).
   Yalnızca tek dili destekleyen ses ya da görsel modellerinin yanında "Yalnız Türkçe" / "Yalnız İngilizce"
   etiketi vardır (ilk_promt). Yerel ve online seçeneklerin nasıl ayırt edileceği Self-critique 15'e bağlıdır.
3. Listelerde yalnızca kullanıcının kullanma yetkisi olan modeller seçilebilir (BR-13). Online bir model
   için kullanıcının o sağlayıcıya ait kayıtlı API key'i yoksa model, 0004'teki katalog etiketiyle
   ("Key yok") gösterilir ve o modeli kullanan bir üretim başlatılamaz (Requirements 10); açıklama
   Ayarlar'a yönlendirir (0005 AC-5 ile aynı davranış).
4. Ses modeli "Kapalı" iken Altyazı seçimi pasiftir ve videoya altyazı eklenmez (seslendirilen metin yok).
   *(Öneri — brief'te yok; sessiz videoda metnin yazı olarak gösterilmesi de mümkün. Self-critique 16.)*
5. Ayarlar (altyazı, ilerleme çubuğu, dört model seçimi) seçili profil için saklanır ve sayfa yeniden
   açıldığında son seçimle gelir. Bir iş başladığında o işin ayarları sabitlenir; sonradan yapılan
   değişiklik çalışan işi değil, henüz başlamamış işleri etkiler.

**B. Onaylanan konsept listesi ve üretim sırası**
6. **Onaylanan konsept listesi** (sol üst), üst barda seçili profilin (BR-2) yalnızca *Onaylandı*
   durumundaki konseptlerini gösterir (BR-16); Konseptler sayfasında onaylanan konsept buraya kendiliğinden
   gelir. Her satırda konseptin başlığı ve bir seçim kutusu vardır.
7. Listede bir konseptin satırına tıklamak, altındaki **konsept ayrıntısı** alanında o konseptin başlığını,
   sahnelerini (sahne başlığı, ekranda gösterilecek şey, seslendirme metni, süre) düz metin olarak gösterir.
   Ayrıntı alanında düzenleme yapılıp yapılamayacağı Open question S-7'ye bağlıdır.
8. Seçim kutuları işaretlenip **Üretim sırasına al** denince seçilen konseptler, listedeki görünüş
   sırasıyla üretim sırasının sonuna eklenir; durumları *Sırada* olur ve onaylanan listeden kalkarlar.
   Hiçbir kutu işaretli değilse eylem pasiftir.
9. **Üretim sırası** (orta sütun) her satırda konsept başlığını, sıra numarasını, işin durumunu ve sağda
   **Yukarı**, **Aşağı** ve **Çıkar (×)** düğmelerini gösterir. En üstteki satırda Yukarı, en alttakinde
   Aşağı pasiftir. Çıkar, konsepti sıradan alır; durumu *Onaylandı*'ya döner ve onaylanan listeye geri
   gelir. Şu anda üretilmekte olan iş taşınamaz ve çıkarılamaz; önce durdurulmalıdır *(öneri — brief'te
   yok; Self-critique 4–5)*. Sıraya alma yalnızca *Onaylandı* durumundaki konseptler için geçerlidir;
   başka durumdaki (ör. zaten *Sırada*) ya da silinmiş bir konsepti sıraya alma girişimi reddedilir.
10. Sıranın üstünde **Sırayla üret** düğmesi vardır. Sıra boşsa, seçili modellerden biri kullanılamıyorsa
    (yetki yok ya da API key yok) ya da bir üretim zaten çalışıyorsa düğme pasiftir ve nedenini gösterir.

**C. Üretim hattı (sırayla üret, aşamalar, durdur/devam, ajan)**
11. Sırayla üret denince sıra yukarıdan aşağı işlenir (BR-10): en üstteki iş başlar, bitince bir sonraki
    başlar. Aynı anda kullanıcı başına tek iş çalışır. Çalışan işin konsepti *Üretiliyor*, video bittiğinde
    *Video oldu* durumuna geçer (BR-16) ve iş sıradan kalkar.
12. Her iş, aşamalarını sırayla ve her birinin durumuyla gösterir: **Konsept inceleniyor** (analiz) →
    **Video kodu üretiliyor** → **Görseller üretiliyor** → **Seslendirme yapılıyor** → **Video kodu videoya
    dönüştürülüyor (render)** → **Birleştiriliyor**. Aşama durumları: bekliyor · çalışıyor · tamamlandı ·
    atlandı · hata. Görsel modeli kapalıysa görsel aşaması, ses modeli kapalıysa seslendirme aşaması
    "atlandı" görünür. Sayfadaki durum, değişiklikten sonra en geç birkaç saniye içinde güncellenir
    (en geç 5 sn, AC-19).
13. Üretim tarayıcıdan bağımsızdır: sayfa kapatılsa ya da başka sayfaya gidilse de sürer; sayfa yeniden
    açıldığında sıranın ve çalışan işin güncel durumu görünür.
14. **Durdur**, çalışan işi o anki aşamada keser ve sıranın ilerlemesini durdurur; tamamlanmış aşamaların
    çıktıları korunur. **Kaldığı yerden devam et**, yarım kalan aşamayı baştan, kalan aşamaları sırayla
    çalıştırır ve ardından sıraya devam eder (BR-10). Tamamlanmış hiçbir aşama yeniden çalıştırılmaz.
15. Online aşamalar (analiz, video kodu, online görsel ya da online ses modeli seçildiyse onlar) Pi'de
    yapılır; kullanıcının API key'leri hiçbir zaman ajana gitmez (Y-7, Y-8). Üretilen video kodu ve
    gerekli aşama çıktıları Pi'den ajana iletilir; yerel aşamalar (yerel görsel, yerel ses, render,
    birleştirme) ajanda çalışır.
16. Masaüstü ajanı çevrimdışıyken yerel aşamalar başlamaz; iş **Ajan bekleniyor** durumunda kalır ve
    sayfada "Ajan bağlı değil" uyarısı görünür (BR-11). Ajan yeniden bağlanınca iş kendiliğinden devam
    eder; ajan bir yerel aşamanın ortasında koparsa o aşama bağlantı dönünce baştan çalışır.
    (Online aşamaların ajan yokken ilerleyip ilerlemeyeceği Open question S-6'ya bağlıdır.)
17. Bir aşama başarısız olursa iş **Hata** durumuna geçer, hatanın kısa ve sade açıklaması (iç detay,
    API key, stack trace olmadan) işin satırında görünür; tamamlanmış aşamalar korunur ve iş "Devam et"
    ile hatalı aşamadan yeniden denenebilir. Sıranın bir hatadan sonra ne yapacağı Open question S-5'e bağlıdır.
18. Her online model çağrısının token sayısı ve maliyeti kaydedilir (BR-12); üst bardaki aylık harcama
    bu çağrılarla güncellenir; işin satırında o işin o ana kadarki toplam maliyeti görünür. Online
    çağrılarda yeniden deneme yalnızca geçici hatalarda ve sınırlıdır (`docs/conventions.md`, H-3).

**D. Ajanda render (H-4)**
19. Ajan, online modelin yazdığı video kodunu yalnızca o işin kendi iş klasörüne erişebilen ve ağ erişimi
    kapalı bir ortamda render eder: render sırasında hiçbir ağ isteği dışarı çıkmaz, iş klasörü dışında
    hiçbir dosya okunamaz ya da yazılamaz, render sınırsız kaynak tüketemez (zaman, bellek, süreç sayısı
    aşılırsa render ve tüm alt süreçleri sonlandırılır ve iş Hata'ya düşer).
20. Bu kurallara aykırı olduğu render öncesinde anlaşılan kod hiç çalıştırılmaz; iş Hata'ya düşer
    (modele düzeltme için geri gönderilip gönderilmeyeceği S-5'e bağlıdır).
21. Render çıktısı beklenen biçimde değilse (oynatılamayan dosya, beklenenden çok farklı süre ya da
    boyut) iş Hata'ya düşer; render çıktısı hiçbir zaman çalıştırılmaz.
22. Video, profilin biçimine (yatay / dikey) uygun boyutta üretilir ve kullanıcının bilgisayarında,
    ajanın klasöründe kalır; Pi'ye yüklenmez (ADR-0002).

**E. Üretilenler paneli**
23. Sayfanın sağ sütunu (**Üretilenler**), seçili profilde bu sayfadan üretilip biten videoları en yeni
    üstte listeler. Bir videoya tıklanınca kesik çizginin öbür yanında bir izleme alanı açılır ve video
    oradan oynatılır (hangi yan olduğu S-7'de). Video ajanın yalnızca bu bilgisayardan erişilebilen
    akışından gelir; ajan çevrimdışıysa izleme alanı "Videoyu izlemek için ajan bağlı olmalı" der.

**F. Yetki**
24. Kullanıcı yalnızca kendi profillerinin konseptlerini, sırasını, işlerini ve videolarını görür ve
    yönetir (BR-1); başka bir kullanıcının konseptini sıraya alma, sırasını değiştirme, işini durdurma ya
    da videosunu açma girişimleri reddedilir. İşler yalnızca o kullanıcının kendi eşleştirilmiş ajanına
    gönderilir.

## User experience
- Flow: Kullanıcı üst bardan profilini seçer → Konseptten Video'yu açar → sol üstte seçili modelleri
  görür, tıklayıp modalda video kodu modelini ve eforunu, motoru, görsel ve ses modelini seçer, kapatır →
  sağ üstte Altyazı ve İlerleme çubuğu seçimlerini yapar → onaylanan konsept listesinde bir konsepte
  tıklayıp ayrıntısını alttaki alanda okur → istediklerini işaretleyip "Üretim sırasına al" der → orta
  sütunda Yukarı/Aşağı ile sıralar, istemediğini × ile çıkarır → "Sırayla üret" der → en üstteki işin
  aşamalarının tek tek ilerlediğini görür → gerekirse "Durdur", sonra "Kaldığı yerden devam et" der →
  biten video sağ sütundaki Üretilenler'e düşer → tıklayınca kesik çizginin yanındaki alanda izler.
- States:
  - Profil seçili değil (hiç profil yok): sayfa "Önce bir profil oluşturun" gösterir, tüm eylemler pasif.
  - Onaylanan liste boş: "Onaylanmış konsept yok — Konseptler sayfasından video için konsept seçin".
  - Ayrıntı alanı: hiçbir konsept seçili değil → "Ayrıntı için bir konsepte tıklayın"; seçili → içerik.
  - Üretim sırası boş: "Sıra boş"; Sırayla üret pasif.
  - Sırayla üret pasif nedenleri: sıra boş · model yetkisi yok · API key yok · üretim zaten çalışıyor.
  - İş durumları: Sırada · Üretiliyor (aşama göstergesiyle) · Durduruldu · Ajan bekleniyor · Hata · (bitince sıradan kalkar).
  - Aşama durumları: bekliyor · çalışıyor · tamamlandı · atlandı · hata.
  - Ajan bağlı değil uyarısı (sayfa üstünde, ajan bekleyen iş varken ve izleme alanında).
  - Model seçimi modalı: açık · kapalı; model listesi yüklenemedi (hata) · "Key yok" / "Yetki yok" etiketli seçenek (0004).
  - Üretilenler boş: "Henüz üretilmiş video yok"; izleme alanı: kapalı · oynatılıyor · ajan çevrimdışı · dosya bulunamadı.
  - Yükleniyor (listeler ilk açılışta) ve sunucuya ulaşılamadı (hata, korelasyon numarasıyla).
  - Yetkisiz: oturum yoksa giriş sayfasına yönlenir (0002).
- Design reference: `source/konseptten video.png`, commit `61cc824` sürümü: üstte "modeller seçimi"
  (sol) ve "altyazı var ya da yok ayarı" (sağ) şeridi; altında üç sütun: sol üstte "onaylanan
  konseptlerin listesi", sol altta "konsept ayrıntısı alanı (düzenleyebilir de)", ortada "üretim sırası",
  sağda "üretilenlerin gösterildiği yer" ve dikey kesik çizgi ("seçilenlerde kesikli yerin solunda video
  izleme yeri açılır" — ilk_promt ise "sağında" diyor; çelişki S-7'de). Üst bar 0001'den gelir. İlerleme
  çubuğu seçimi görselde yok; ilk_promt'a göre altyazı seçiminin yanına konur. Renk: turuncu/kahve, açık mod esas.
- Accessibility: tüm seçim kutuları, aç/kapa düğmeleri, Yukarı/Aşağı/× ve Sırayla üret/Durdur/Devam
  klavyeyle (Tab, Enter/Space) kullanılabilir; ikon düğmelerinin erişilebilir adı vardır ("<başlık>'ı
  yukarı taşı" gibi); modal açılınca odak modala geçer, Esc kapatır, kapanınca odak tetikleyen öğeye
  döner; aşama durumu yalnızca renkle değil metin/ikonla da belirtilir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-3 Maliyet doğuran çağrılar (toplu kuyruk, video kodu çağrıları, yeniden deneme ve
  düzeltme döngüleri, token limitleri, maliyet kaydı).
- Sensitive area: H-4 LLM'in ürettiği video kodunun kullanıcı bilgisayarında çalıştırılması (render
  ortamı). Yöntem B-5 raporunun **önerisi**dir, karar değildir (S-4); AppContainer spike'ları 1–2 (rapor §4)
  plan'dan önce yapılmalıdır. H-4 kriterleri kasıtlı bozma ile kanıtlanır (rapor §4 T1–T11 örnek alınır).
- Bağımlılıklar: 0002 (oturum, ajan eşleştirme), 0003 (profil: biçim, dil, hedef süre), 0004 (model
  kataloğu, efor seviyeleri, API key, harcama kaydı ve aylık gösterge), 0005 (Onaylandı durumu ve seçim
  kontrolü). Ajandaki araçların (Node, FFmpeg, Chromium, yerel modeller) kurulumu 0009'dadır; 0006,
  bunların kurulu olduğunu varsayar ve yoksa işi sade bir "Ajan kurulumu eksik: <araç>" hatasıyla durdurur.
- Token limitleri model kataloğundan okunur, kodda sabit değer olmaz (`docs/conventions.md`); 15 dakikalık
  yatay videoya kadar her uzunluk desteklenmelidir (`docs/domain.md`, Temel değer).
- Testlerde gerçek online çağrı yapılmaz (stub); gerçek render ajan render entegrasyonu olarak full
  profilde çalışır (`docs/testing.md`).
- Out of scope: Üretilen Videolar sayfası, klasörde aç, sistem oynatıcısı, yeniden üret, yeni sürüm /
  kısmi yeniden üretim (BR-9), video silme ve Silinenler'in video tarafı (0007) · ajan kurulum/güncelleme
  ekranı (0009) · Günlük şeridinin akışı (0008; bu spec yalnızca olay üretir) · yayınlama · Remotion'un
  ticari lisans incelemesi · Linux/macOS ajanı · Manim (rapor K-3, faz 2) · aynı anda birden fazla iş.
- Split: proposed — bu spec dört ayrı kullanıcı yeteneği taşıyor ve 37 kriter içeriyor. Önerilen bölünme
  ve sıra: **0006 Üretim ayarları + sıra yönetimi** (A, B, F; hassas değil) → **0011 Üretim hattı**
  (C, E; H-3) → **0012 Ajanda kısıtlı render** (D; H-4, spike 1–2 sonrasında) — 0007'den önce. Ayrıntı ve
  numaralandırma önerisi S-1'de. Bölünmezse bu satıra "kept as one — <kim>, <neden>" yazılır.

## Acceptance criteria
<!-- Parantez içindeki harf, önerilen bölünmedeki parçayı gösterir (S-1). -->
**A. Ayarlar ve model seçimi**
- [ ] AC-1 — (A) Altyazı açık, ses modeli açıkken üretilen videoda seslendirme metni ekranda sesle zamanlı görünür (her sahnede metin, o sahnenin ses aralığında) ve o an okunan kısım farklı renkte; Altyazı kapalıyken aynı konseptten üretilen videoda hiçbir altyazı karesi yoktur.
- [ ] AC-2 — (A) İlerleme çubuğu açıkken videonun üst kenarındaki çubuk ilk karede boş, son karede dolu ve videonun yarısında yaklaşık yarı doludur (±%5); kapalıyken çubuk hiç görünmez.
- [ ] AC-3 — (A) Ses modeli "Kapalı — sessiz video" iken Altyazı seçimi pasiftir, üretilen videoda ses izi sessizdir ve seslendirme aşaması "atlandı" görünür.
- [ ] AC-4 — (A) Görsel modeli "Kapalı — koddan çiz" iken görsel aşaması "atlandı" görünür, hiçbir görsel modeli (yerel ya da online) çağrılmaz ve video yine üretilir.
- [ ] AC-5 — (A) Model seçimi özetine tıklanınca modal açılır; dört seçim (video kodu modeli + efor, video motoru, görsel modeli/Kapalı, ses modeli/Kapalı) görünür; dışına tıklama, Kapat ve Esc modalı kapatır ve seçimi kaydeder; özet yeni seçimi gösterir; varsayılan video motoru HyperFrames'tir.
- [ ] AC-6 — (A) Efor listesi yalnızca seçili modelin katalogda desteklediği seviyeleri gösterir; model değişince desteklenmeyen efor seçimi modelin varsayılanına döner.
- [ ] AC-7 — (A) Kullanıcının yetkisi olmayan bir model seçilemez (listede pasif); API isteğiyle zorlanırsa reddedilir (BR-13). API key'i olmayan online model "Key yok" etiketiyle görünür ve seçiliyken Sırayla üret pasiftir, nedeni ("API key yok — Ayarlar") yazılıdır.
- [ ] AC-8 — (A) Ayarlar profil başına saklanır: A profilinde yapılan seçim sayfa yenilenince A'da korunur, B profiline geçince B'nin kendi seçimi gelir; hiç seçim yapılmamış profilde varsayılanlar gelir. Çalışan bir işin ayarı sonradan değiştirilirse o işin çıktısı değişmez, sıradaki bir sonraki iş yeni ayarla üretilir.

**B. Onaylanan liste ve sıra**
- [ ] AC-9 — (B) Onaylanan konsept listesinde seçili profilin yalnızca *Onaylandı* durumundaki konseptleri görünür; başka profilin ya da Üretildi / Sırada / Üretiliyor / Video oldu durumundaki konseptler görünmez. Konseptler sayfasında yeni onaylanan konsept, sayfa yenilenince listede görünür.
- [ ] AC-10 — (B) Listede bir konsepte tıklanınca ayrıntı alanında başlığı ve tüm sahneleri (başlık, ekran tarifi, seslendirme metni, süre) düz metin olarak görünür; içerikteki HTML/script çalışmaz, metin olarak görünür. Hiç seçim yokken "Ayrıntı için bir konsepte tıklayın" görünür.
- [ ] AC-11 — (B) 3 konsept işaretlenip "Üretim sırasına al" denince üçü listedeki görünüş sırasıyla sıranın sonuna eklenir, durumları *Sırada* olur, onaylanan listeden kalkar; hiçbir kutu işaretli değilken eylem pasiftir. *Onaylandı* olmayan (Üretildi, zaten Sırada, Video oldu) ya da silinmiş bir konsepti sıraya alma isteği reddedilir, sıra ve konsept durumu değişmez; aynı konsept iki kez sıraya giremez.
- [ ] AC-12 — (B) Yukarı/Aşağı bir işi bir sıra taşır ve yeni sıra yenilemeden sonra korunur; en üstteki işte Yukarı, en alttakinde Aşağı pasiftir; tek elemanlı sırada ikisi de pasiftir.
- [ ] AC-13 — (B) × ile çıkarılan konsept *Onaylandı*'ya döner ve onaylanan listede yeniden görünür; diğer işlerin göreli sırası değişmez.
- [ ] AC-14 — (B) Üretilmekte olan işin Yukarı/Aşağı/× düğmeleri pasiftir; API isteğiyle zorlanırsa reddedilir ve sıra değişmez.
- [ ] AC-15 — (B) Boş durumlar: hiç profil yokken "Önce bir profil oluşturun" ve tüm eylemler pasif; onaylanan liste boşken "Onaylanmış konsept yok — Konseptler sayfasından…"; sıra boşken "Sıra boş" ve Sırayla üret pasif.
- [ ] AC-16 — (B) 250 konseptlik onaylanan liste ve 250 işlik sıra açılır, kaydırılabilir ve sıraya alma / taşıma eylemleri çalışır; ölçülebilir süre eşiği Self-critique 18'deki karara göre (öneri: ilk görünüm ≤ 2 sn, eylem sonrası güncelleme ≤ 1 sn).
- [ ] AC-17 — (F) Başka bir kullanıcının konseptini sıraya alma, sırasını değiştirme, işini durdurma/devam ettirme ve videosunu açma istekleri reddedilir ve hiçbir veri değişmez; listeler yalnızca oturumdaki kullanıcının verisini içerir (BR-1).

**C. Üretim hattı (H-3)**
- [ ] AC-18 — (C) Sırayla üret denince en üstteki iş başlar, konsepti *Üretiliyor* olur; iş bitince konsepti *Video oldu* olur, sıradan kalkar ve bir sonraki iş kendiliğinden başlar; aynı anda hiçbir zaman iki iş çalışmaz.
- [ ] AC-19 — (C) Çalışan işte aşamalar Requirements 12'deki sırayla ve durumlarıyla görünür; bir aşamanın durumu değiştikten sonra en geç 5 sn içinde sayfaya yansır; sayfa kapatılıp açıldığında üretim sürmüş ve güncel durum görünür.
- [ ] AC-20 — (C) Görsel aşamasında Durdur denince iş *Durduruldu* olur, sıradaki iş başlamaz; Devam et denince analiz ve video kodu aşamaları yeniden çalışmaz (online çağrı sayısı artmaz), görsel aşaması baştan, kalan aşamalar sırayla çalışır ve ardından sıra devam eder (BR-10).
- [ ] AC-21 — (C) Ajan çevrimdışıyken iş yerel bir aşamaya gelince *Ajan bekleniyor* olur, hiçbir yerel aşama başlamaz ve sayfada "Ajan bağlı değil" uyarısı görünür; ajan bağlanınca iş kendiliğinden devam eder (BR-11).
- [ ] AC-22 — (C) Ajan bir yerel aşamanın ortasında koparsa iş *Ajan bekleniyor* olur; bağlantı dönünce o aşama baştan çalışır, tamamlanmış aşamalar tekrarlanmaz.
- [ ] AC-23 — (C) Bir aşama hata verince iş *Hata* olur, satırda sade bir açıklama görünür (stack trace, SQL, API key yok); tamamlanmış aşamalar korunur; Devam et yalnızca hatalı aşamadan yeniden başlar. Sıranın davranışı S-5 kararına göre test edilir.
- [ ] AC-24 — (C) Her online çağrı (analiz, video kodu, varsa online görsel/ses) için token sayısı ve maliyet kaydı oluşur (BR-12); işin satırındaki toplam bu kayıtların toplamına eşittir; üst bardaki aylık harcama aynı tutar kadar artar. Sağlayıcı 4xx döndürünce yeniden deneme yapılmaz; 429/5xx'te en fazla 2 yeniden deneme yapılır (kasıtlı bozma: sınır kaldırılınca test kırmızı).
- [ ] AC-25 — (C) Video kodu çağrısının çıktı token limiti seçili modelin katalog değerinden gelir; katalog değeri değiştirilince yapılan çağrının limiti de değişir (kodda sabit değer yok).
- [ ] AC-26 — (C) Kullanıcının API key'i ajana hiçbir aşamada gönderilmez: ajana giden tüm komut ve dosyalar kaydedildiğinde hiçbirinde key (ya da maskesiz parçası) bulunmaz; ajan online sağlayıcıya hiç istek atmaz (Y-7, Y-8).
- [ ] AC-27 — (C) Ajan bağlıyken kurulumu eksikse (ör. render aracı yok) iş, hiçbir online çağrı yapılmadan "Ajan kurulumu eksik: <araç>" açıklamasıyla *Hata* olur (harcama kaydı oluşmaz). Ajan bağlı değilken davranış S-6 kararına göre test edilir. *(Kontrolün zamanı öneri — Self-critique 14.)*

**D. Kısıtlı render (H-4) — her kriter kasıtlı bozma ile kanıtlanır**
- [ ] AC-28 — (D) Render edilen kod dışarıya ağ isteği atmaya çalışırsa (HTTP isteği, IP adresine istek, uzak görsel/ses kaynağı, WebSocket, eşler arası bağlantı) hiçbir istek hedefe (test honeypot'u) ulaşmaz; render ya başarısız olur ya da kaynak karede görünmez.
- [ ] AC-29 — (D) Render edilen kod iş klasörü dışındaki bir dosyayı okumaya (ör. `file://`, `..` ile klasör dışına çıkma, iş klasörü içinden dışarıyı gösteren kısayol/sembolik bağlantı, ajanın kendi oturum dosyaları) ya da kullanıcının ev klasörüne yazmaya çalışırsa okuma başarısız olur, içerik karede görünmez ve dosya oluşmaz.
- [ ] AC-30 — (D) Sonsuz döngü, aşırı bellek ya da yüzlerce alt süreç açan kod belirlenen süre/bellek/süreç sınırında sonlandırılır; render bitince ya da öldürülünce geride hiçbir render süreci kalmaz; iş *Hata* olur.
- [ ] AC-31 — (D) Yasaklı yapılar içeren kod (dinamik kod çalıştırma, ağ API'leri, dolaylı erişimle gizlenmiş hâlleri) render öncesinde reddedilir ve hiç çalıştırılmaz.
- [ ] AC-32 — (D) Zararsız örnek şablon kod sorunsuz render edilir ve çıktı kapısından geçer (aşırı sıkılaştırma koruması); çıktı oynatılamaz bir dosyaysa ya da süresi/boyutu beklenenden saparsa iş *Hata* olur ve çıktı çalıştırılmaz.
- [ ] AC-33 — (D) Üretilen video profilin biçimine uygundur (dikey profilde dikey, yatay profilde yatay) ve kullanıcının bilgisayarında ajanın klasöründedir; Pi'ye hiçbir video dosyası yüklenmez.

**E. Üretilenler paneli**
- [ ] AC-34 — (E) Biten video, seçili profilde Üretilenler sütununda en üstte görünür; tıklanınca kesik çizginin yanındaki izleme alanında oynar; boşken "Henüz üretilmiş video yok" görünür.
- [ ] AC-35 — (E) Ajan çevrimdışıyken bir videoya tıklanınca izleme alanı "Videoyu izlemek için ajan bağlı olmalı" der; dosya ajan klasöründe yoksa "Video dosyası bulunamadı" der; video akışı yalnızca kullanıcının kendi bilgisayarından ve kısa ömürlü yetkiyle açılır.
- [ ] AC-36 — (A–E) Erişilebilirlik: tüm kontroller klavyeyle kullanılabilir ve erişilebilir ada sahiptir; modal açılınca odak modala geçer, Esc kapatır, odak geri döner; aşama durumu renk dışında metin/ikonla da belirtilir.
- [ ] AC-37 — (A–E) Diğer sayfa durumları: listeler ilk açılışta "Yükleniyor" gösterir; sunucuya ulaşılamazsa sade Türkçe hata ve korelasyon numarası görünür (stack trace yok); model listesi yüklenemezse modal hata mesajı gösterir ve Sırayla üret pasif kalır; bir üretim çalışırken Sırayla üret pasiftir ve nedeni ("Üretim sürüyor") yazılıdır; yetkisi olmayan modeli seçili bırakan ayar için neden "Model yetkiniz yok" yazılıdır; oturum yokken sayfa giriş sayfasına yönlenir.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir. B-5 raporundaki maddeler **öneri**dir,
insan kararı olmadan karar sayılmaz.

**Clarify soruları (etkiye göre sıralı)**
1. **S-1 — Bölünme.** Spec 4 yetenek ve 37 kriter taşıyor. **Öneri:** üçe böl: **0006 Üretim ayarları ve
   sıra yönetimi** (A, B, F, AC-1…17'nin ayar/sıra kısmı; ayarların videoya yansıma kanıtı AC-1…4 ise
   render olmadan kanıtlanamadığı için 0011'e taşınır) → **0011 Üretim hattı ve Üretilenler paneli**
   (C, E; H-3) → **0012 Ajanda kısıtlı render** (D; H-4) ve sıralama 0006 → 0012 → 0011 → 0007.
   *Gerekçe:* her parça tek başına gösterilebilir ve kanıtlanabilir; H-4 parçası spike sonucuna bağlı
   olduğu için diğerlerini bekletmesin; hassas kriterler (kasıtlı bozma) küçük bir spec'te daha iyi
   gözden geçirilir. Yeni numara (0011, 0012) önerilir çünkü roadmap'teki 0007–0010 numaraları dolu
   ve `0006a` gibi ekler doctor'ın numara kalıbına uymayabilir.
2. **S-2 — Video motoru, altyazı/ilerleme çubuğu yöntemi, render birimi (rapor K-1…K-5, K-17).**
   ilk_promt "HyperFrames varsayılan, Remotion ve Revideo olsun … başka önerdiğin varsa … eklersin.
   Değiştirmeyelim de" diyor — yani senin isteğin bu üçünün listede kalması; rapor ise Revideo'yu bırakmayı ve
   Remotion'u lisans/telemetri nedeniyle varsayılan kapalı tutmayı öneriyor. **Öneri:** listede
   **HyperFrames** (varsayılan, sürümü sabit, telemetri kapalı) ve **Remotion** ("lisans uyarısı" etiketli,
   seçilebilir); Revideo listede yok (bakım durumu belirsiz, yalnızca üçüncü taraf kaynak) — "değiştirmeyelim"
   dersen Revideo "deneysel" etiketiyle listede kalır ve H-4 testleri onun için de koşar. Altyazı ve
   ilerleme çubuğunu LLM değil **ajan sabit bir şablonla** ekler (rapor K-4); render **sahne başına**
   yapılıp birleştirilir (K-5); Node 26 LTS (K-17). *Gerekçe:* altyazı/çubuğun "gerçekten çalışması"
   şablonla deterministik ve token harcamaz; sahne başına render 15 dk videoda yarıda kalan işi ucuzlatır
   (BR-10) ve 0007'deki kısmi yeniden üretime zemin hazırlar.
3. **S-3 — Görsel ve ses model listeleri (rapor K-6…K-15).** **Öneri:** Görsel: **Z-Image-Turbo** (yerel,
   varsayılan), **FLUX.2 klein 4B** (yerel), online **fal Z-Image-Turbo**, Nano Banana 2 Lite, gpt-image-2
   (brief'teki "GLM ile" görsel seçeneği raporda değerlendirilmedi; öneri: ilk sürümde yok, backlog'a);
   FLUX.2 klein 9B lisans nedeniyle yok; görselde yazı çizdirilmez, yazı video katmanında (K-12).
   Ses: **Supertonic 3** (yerel, varsayılan, CPU), **Chatterbox Multilingual V3** (yerel, GPU, ~4–4,5 GB),
   online **ElevenLabs**; OpenAI TTS'e yeni iş yok (2027-01-06 kapanış). Yerel GPU işleri sıralı çalışır
   (6 GB VRAM, K-14). Supertonic lisansı (OpenRAIL-M) gereği videolara varsayılan "Yapay zekâ ile
   üretilmiş ses" beyanı (K-7) — bu beyanın nerede/nasıl görüneceği (video açıklaması, dışa aktarma notu,
   videoda yazı) senin kararın; öneri: videoya gömülmez, 0007'de video ayrıntısında ve dosya yanında not.
   *Gerekçe:* Türkçe destekli, ticari serbest lisanslı, 6 GB'ta çalışan seçenekler; brief'teki "Supersonic"
   = Supertonic, "Z-Mesh/Zmesh Turbo" = Z-Image-Turbo, "Flux 2 Klein 4B" = FLUX.2 klein 4B olarak okundu.
4. **S-4 — H-4 sandbox yöntemi (rapor K-16, K-18, K-19, spike 1–2).** **Öneri:** rapordaki katmanlı
   yapı, çalışma zamanında yönetici yetkisi istemeden: sabit şablon sözleşmesi → render öncesi yasaklı
   yapı kontrolü (Pi'de ve ajanda) → iş klasörü → **AppContainer** (yalnızca spike 1–2 geçerse; geçmezse
   kısıtlı token + Chromium kendi sandbox'ı + ağ bayrakları) → Job Object kaynak sınırı → Node izin modeli
   → çıktı kapısı. Kurulumda isteğe bağlı yönetici yetkili güvenlik duvarı kuralı ("güçlü mod") 0009'da
   sorulur; Windows Sandbox varsayılan değil. **Spike 1–2 plan'dan önce yapılır**, sonucu bir ADR olur.
   *Gerekçe:* admin'siz en güçlü yalıtım; tek katmana güvenilmez; spike olmadan AppContainer içinde
   Chromium/loopback davranışı belirsiz.
5. **S-5 — Hata sonrası sıra ve LLM düzeltme döngüsü (H-3).** **Öneri:** (a) Render öncesi kontrolden ya
   da render'dan dönen hata, hata özetiyle birlikte video kodu modeline **en fazla 1 kez** düzeltme için
   geri gönderilir (BR-15'teki "en fazla 1" ile aynı mantık); yine başarısızsa iş Hata olur. (b) Bir iş
   Hata olunca sıra bir sonraki işe geçer; **art arda 3 iş** hata verirse sıra kendiliğinden durur ve
   uyarı verir. *Gerekçe:* gece 200 videoluk sırada tek bozuk konsept tüm sırayı durdurmamalı; ama
   sistematik bir hata (geçersiz key, kotası dolan sağlayıcı) sırayı boşuna yakmamalı.
6. **S-6 — Ajan çevrimdışıyken online aşamalar.** **Öneri:** online aşamalar (analiz, video kodu, online
   görsel/ses) ajan yokken de çalışır, iş ilk yerel aşamada *Ajan bekleniyor*'da durur; ama sıradaki
   **bir sonraki iş** ajan gelene kadar başlamaz. Ajan geri gelince kendiliğinden devam eder.
   *Gerekçe:* PC kapalıyken zaman kazandırır, ama ajan hiç gelmezse en fazla tek işin online maliyeti
   harcanmış olur (H-3).
7. **S-7 — Tasarım ile ilk_promt arasındaki iki çelişki.** (a) İzleme alanı kesik çizginin **sağında mı
   solunda mı** açılır? ilk_promt "sağ tarafında", görsel "solunda" yazıyor. **Öneri:** Üretilenler listesi
   kesik çizginin solunda, izleme alanı sağında (görseldeki geniş boş alan orası). (b) Görsel, konsept
   ayrıntısı alanının "düzenleyebilir de" olduğunu söylüyor; ilk_promt yalnızca "ayrıntılar açılır" diyor.
   **Öneri:** ayrıntı alanı bu spec'te salt okunur ve "Konseptler'de düzenle" bağlantısı verir; düzenleme
   döngüsü (BR-6) 0005'te tek yerde kalır. *Gerekçe:* aynı düzenleme akışının iki sayfada iki kez
   yazılması tutarsızlık ve çift test demek; Sırada olan konseptin metni değişirse üretim karışır.

**Self-critique boşlukları (hostile developer okuması; önerilerimle, onayda katlanır)**
1. **Ayar kapsamı ve anlık görüntü:** ayarların profil başına saklanması ve işin başladığı anda
   sabitlenmesi (Req 5) brief'te açık değil. Öneri: böyle kalsın (kanal başına üslup farklı).
2. **Sıraya alma sırası:** birden çok seçimde ekleme sırası belirsizdi. Öneri: listedeki görünüş sırası (Req 8).
3. **Sıra profil bazında mı?** BR-2 sayfaların seçili profilin verisini göstermesini istiyor. Öneri: her
   profilin kendi sırası vardır; kullanıcı başına aynı anda tek iş çalışır; başka profilde Sırayla üret
   denirse o sıra mevcut üretim bitince başlar ("Bekliyor" durumu) — ya da daha basit: üretim çalışırken
   diğer profillerde Sırayla üret pasiftir. Öneri: ikincisi (basit, tek GPU).
4. **Durdur'un kapsamı:** yalnızca çalışan işi mi, sırayı mı durdurur? Öneri: ikisini de (Req 14); tek
   iş atlatmak için "×" yerine ileride "Atla" eklenebilir, şimdilik yok.
5. **Durdurulmuş işin sıradan çıkarılması:** tamamlanmış aşama çıktıları ne olur? Öneri: konsept
   Onaylandı'ya döner, aşama çıktıları konsept metni değişmedikçe saklanır ve yeniden sıraya alınınca
   kullanılır (BR-9 mantığı).
6. **Sırada/Üretiliyor konseptin Konseptler sayfasında silinmesi ya da düzenlenmesi:** Öneri: bu
   durumlardaki konseptler Konseptler sayfasında silinemez/düzenlenemez, önce sıradan çıkarılır (0005
   ile tutarlı yazılmalı; 0005'e not).
7. **"Konsept inceleniyor" (analiz) aşaması** ayrı bir online çağrı mı? Öneri: video kodu modeliyle
   yapılan kısa bir planlama çağrısı (sahne süreleri, görsel promptları, ses metni bölümleri); ayrı model
   seçimi yok. Maliyeti AC-24 kapsamında kaydedilir.
8. **Uzun videolar ve token limiti:** 15 dk videonun kodu tek çağrıya sığmayabilir. Öneri: video kodu
   sahne başına üretilir (S-2'deki sahne başına render ile uyumlu); limit katalogdan (AC-25).
9. **Render zaman/bellek sınırlarının değerleri** belirsiz (AC-30). Öneri: değerler plan'da, geliştirici
   PC'sindeki benchmark'a göre (rapor §6) belirlenip ayarlara yazılır; sahne başına süre sınırı.
10. **Durum yenilenme süresi:** "birkaç saniye" ölçülebilir değildi. Öneri: en geç 5 sn (AC-19; mimari 2–3 sn polling).
11. **İşin maliyet gösterimi** (Req 18) brief'te bu sayfa için açık değil; 0007'de "ücret" isteniyor.
    Öneri: iş satırında küçük toplam kalsın (gece kuyruğunda harcamayı görmek H-3 açısından değerli).
12. **Altyazı vurgusu stili** ("okunan kısım rengi değişir"): kelime mi, cümle mi? Öneri: kelime bazında
    vurgu, renk tema değil sabit şablondan; stil seçimi yok (ileride eklenebilir).
13. **Günlük şeridi olayları:** iş başladı/bitti/hata ("video hazırlandı, süresi 57 sn, 10.30–11.00")
    ilk_promt'ta var. Öneri: bu spec olayları üretir, gösterim 0008'de.
14. **Ajan kurulumu eksik kontrolü (AC-27)** online çağrıdan önce yapılmalı mı? Öneri: evet, ajan
    bağlıysa iş başlamadan araçların varlığı sorulur; ajan bağlı değilse S-6 kuralı geçerli.
15. **Yerel / online ayrımı modalda nasıl görünür?** Brief yalnızca "Yalnız Türkçe / Yalnız İngilizce"
    etiketini istiyor. Öneri: listede "yerel" / "online" etiketi; online olanlarda 0004'teki "Key yok" /
    "Yetki yok" etiketi. *Gerekçe:* yerel model ücretsiz ama ajan ister, online model ücretli ama ajan
    gerektirmez — kullanıcı maliyeti seçim anında görmeli (H-3).
16. **Sessiz videoda altyazı (Req 4):** brief'te yok. Öneri: ses kapalıyken Altyazı pasif. *Gerekçe:*
    altyazı zamanlaması sese bağlı (BR-9); sessiz videoda metin göstermek ayrı bir "ekran yazısı" özelliğidir.
17. **Profil dili ile tek dilli model uyuşmazlığı:** İngilizce profilde "Yalnız Türkçe" ses/görsel modeli
    seçilirse ne olur? Öneri: seçilebilir ama uyarı gösterilir ve Sırayla üret pasif olur, nedeni
    "Seçili ses modeli profil dilini desteklemiyor" yazar. *Gerekçe:* yanlış dilde seslendirme tüm işin
    online maliyetini boşa harcar (H-3); profil dili 0003'te tanımlı.
18. **250'lik liste performansı (AC-16)** "kilitlemeden" ölçülebilir değildi. Öneri: Pi üzerinde ilk görünüm
    ≤ 2 sn, eylem sonrası güncelleme ≤ 1 sn; gerekirse liste sayfalı/sanal kaydırmalı. *Gerekçe:* brief
    "250 tane koydu" diyor; Pi 5'te ölçülebilir bir eşik olmadan test yazılamaz.
19. **Üretim sırasında çıkış ya da ban (BR-13, BR-14):** çalışan iş ne olur? Öneri: web'den çıkışta ya da
    banda çalışan iş *Durduruldu*'ya geçer, sıra durur, devam eden online çağrı bittikten sonra yenisi
    başlamaz; yeniden girişte kullanıcı "Kaldığı yerden devam et" der. *Gerekçe:* banlı kullanıcının
    sistemi kullanmaya devam etmemesi H-1 gereği, oturumsuz harcama H-3 riski; tamamlanmış aşamalar korunur.
20. **Yerel akışın sahibi:** Üretilenler panelinde oynatma (Req 23) ajanın kısa ömürlü yetkili yerel
    akışına dayanıyor; 0007 AC-5 de aynı akışı tanımlıyor ve 0007 bunu H-1 saymayı öneriyor. Öneri: akış bu
    spec'te (0006, ya da bölünürse 0011'de) kurulur ve kanıtlanır, 0007 yeniden kullanır; 0007 S-önerisi
    kabul edilirse buraya da "Sensitive area: H-1 (yerel akış yetkisi)" eklenir. *Gerekçe:* 0006, 0007'den
    önce gelir; akış iki spec'te ayrı ayrı yazılırsa çift iş ve çelişkili kriter olur.
21. **Durdurulmuş ya da Hata'daki işin onaylanan listeye dönüşü:** × ile çıkarılan *Durduruldu*/*Hata*
    işinin konsepti Onaylandı'ya mı döner? Öneri: evet (Req 9), aşama çıktıları Self-critique 5'teki gibi
    saklanır. *Gerekçe:* kullanıcı bozuk bir işi sıradan atıp konsepti 0005'te düzenleyebilmeli.

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
