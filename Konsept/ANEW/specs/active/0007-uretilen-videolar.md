# Spec 0007 — uretilen-videolar

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0007-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisi, ürettiği videoları tek bir sayfada izlemek, incelemek ve gerekirse para harcamadan
iyileştirmek istiyor. Üretilen Videolar sayfasında seçili profilin videoları solda listelenir; seçilen
video ortada ajanın yerel akışından oynatılır (ADR-0002), sağda seslendirme ve sahneler, konsept metni,
ayrıntılar ve ücret görünür. Kullanıcı videoyu klasörde ya da sistem oynatıcısında açabilir, siler
(Silinenler, BR-7) ya da "yeni sürüm" ile yalnızca ses motorunu veya video motorunu değiştirip yalnızca
etkilenen aşamaları yeniden ürettirir (BR-9); konsepti düzenleyip baştan "yeniden üret" de diyebilir.
Başarı: yerel ses motoru değişikliği hiçbir online çağrı yapmadan yeni bir sürüm üretir, her sürümün
gerçek maliyeti görünür, ajan kapalıyken sayfa anlaşılır biçimde çalışmaya devam eder. Bilinçli olarak
yapılmayanlar: yayınlama, videonun başka cihazdan (ajansız) izlenmesi, videoların Pi'ye yüklenmesi,
sahne/cümle bazında kısmi yeniden üretim (aşağıdaki Open questions).

## Requirements
1. Sayfa, üst bardan seçili profile ait (BR-2), silinmemiş videoları solda bir liste olarak gösterir.
   Liste öğesinin alanları ve sıralaması Open questions'taki karara göre belirlenir.
2. Listeden bir video seçilince ortadaki izleme yerinde videonun seçili sürümü (varsayılan: en son
   tamamlanan sürüm) oynatılabilir hale gelir; sağ panel aynı sürümün bilgilerini gösterir.
3. Oynatıcı videoyu kullanıcının bilgisayarındaki ajanın yalnızca o bilgisayardan erişilebilen, kısa
   ömürlü ve yalnızca bu kullanıcının bu videosu için geçerli yetkiyle açılan yerel akışından oynatır
   (ADR-0002). Video dosyası sunucuya yüklenmez. Oynatıcıda oynat/duraklat, ilerleme konumu, ses ayarı
   ve tam ekran vardır.
4. İzleme yerinin altında dört eylem vardır: **Klasörde aç**, **Sistem oynatıcısında aç**,
   **Yeniden üret**, **Sil**.
   - Klasörde aç: ajan, video dosyasının bulunduğu klasörü kullanıcının bilgisayarında dosya
     gezgininde açar.
   - Sistem oynatıcısında aç: ajan, video dosyasını bilgisayarın varsayılan video uygulamasında açar.
   - Yeniden üret: üretimden önce konsept düzenlenir ("Yeniden üretle düzenleyeceksin yeniden
     üretmeden önce"); düzenleme onaylanmadan hiçbir üretim başlamaz. Konsept metni değiştiği için tüm
     aşamalar baştan çalışır (BR-9). Sonucun aynı videonun yeni bir sürümü mü olacağı yoksa eskisinin
     yerini mi alacağı henüz kararlaştırılmadı (Open questions 1; öneri: yeni sürüm, eskiler korunur).
   - Sil: onaydan sonra video listeden kalkar ve Silinenler'in video tarafına gider (BR-7, soft delete);
     dosyalar diskte kalır. Sırada ya da üretimde sürümü olan bir videonun silinmesi henüz
     kararlaştırılmadı (Open questions, self-critique 15).
5. Sağ panelde üç bölüm vardır:
   - **Seslendirme ve sahneler:** sahneler sırasıyla, her birinin kısa başlığı ve seslendirme metni.
   - **Konsept metni:** seçili sürümün üretildiği konsept metni.
   - **Ayrıntılar ve ücret:** sürümün online model maliyeti (BR-12; yerel aşamalar ücretsiz) ve ek
     bilgiler. Ek bilgi alanlarının listesi brief'te yok; önerilen liste (Open questions, self-critique
     13): kullanılan modeller ve motorlar (video kodu modeli, video motoru, görsel, ses), altyazı ve
     ilerleme çubuğu seçimleri, video süresi ve biçimi, üretimin başlangıç ve bitiş zamanı (brief'teki
     Günlük örneği: "süresi 57 saniye, başlangıç 10.30, bitiş 11").
   LLM'in ürettiği metinler düz metin olarak gösterilir (`docs/security.md`).
6. **Video sürümleri** (Open questions 1'deki "eski sürümler korunur" önerisine bağlı; brief'te sürüm
   listesi yok, domain'de `Video sürümü` var): sağ panelde videonun tüm sürümleri listelenir (sürüm
   numarası, üretim zamanı, önceki sürüme göre neyin değiştiği, ücreti, durumu). Bir sürüm seçilince
   oynatıcı ve sağ panel o sürüme geçer.
7. **Yeni sürüm:** kullanıcı seçili sürümün ayarlarından **ses motorunu (seslendirme modelini)** ya da
   **video motorunu** değiştirir. Başlatmadan önce sistem, BR-9'a göre hangi aşamaların yeniden
   üretileceğini ve online model çağrısı gerekip gerekmediğini gösterir:
   - Ses değişirse → seslendirme + birleştirme; altyazı açıksa render da.
   - Video motoru değişirse → video kodu + render + birleştirme; ses korunur. Video kodu online
     modelle üretildiği için (brief: "video kod üretimi ... online LLM'le olacak") video motoru
     değişimi her zaman online model çağrısı gerektirir.
   Ses motoru yerelden yerele (ör. Supertonic → Chatterbox) değiştirildiğinde para harcanmaz; kullanıcı
   yerel modelleri beğenmezse online bir ses modeline de geçebilir (brief). Yalnızca bu aşamalar
   çalışır; diğer aşamaların çıktıları önceki sürümden aynen kullanılır. Hiçbir ayar değişmediyse yeni
   sürüm başlatılamaz. Değiştirilebilecek diğer ayarlar (görsel modeli, altyazı, ilerleme çubuğu, video
   kodu modeli, sessiz video) Open questions 2'deki karara bağlıdır.
8. Her online çağrının token ve maliyeti kaydedilir; sürümün ücretine ve aylık harcamaya eklenir
   (BR-12). Online model çağrısı gerektiren bir yeni sürüm ya da yeniden üretimin, kullanıcı çalışacak
   aşamaları ve "online model çağrısı yapılacak" uyarısını gösteren bir onayı vermeden başlamaması
   önerilir (H-3; brief'te onay adımı yok — Open questions 4).
9. Tekrarlanan başlatma isteği (çift tıklama, aynı isteğin yeniden gönderilmesi) ikinci bir üretim
   oluşturmaz (H-3). Bir video için aynı anda yalnızca bir sürümün sırada ya da üretimde olabilmesi
   önerilir (Open questions, self-critique 14).
10. Üretimi süren sürümün durumu sürüm listesinde görünür ve birkaç saniyede bir yenilenir. Durum adları
    0006'daki üretim işi durumlarıyla aynıdır (öneri: Sırada · Üretiliyor + o anki aşama · Ajan
    bekleniyor · Hata · Hazır; kesin liste 0006'da belirlenir, burada yeniden tanımlanmaz). Başarısız
    bir sürüm önceki sürümleri etkilemez.
11. Ajan çevrimdışıyken: liste, sağ panel ve sürüm listesi görünmeye devam eder; oynatıcı "Ajan bağlı
    değil" durumunu gösterir; Klasörde aç ve Sistem oynatıcısında aç pasiftir ve nedenini söyler; yeni
    sürüm ve yeniden üretim başlatılabilir ama yerel aşamalar başlamaz, sürüm "Ajan bekleniyor"
    durumunda kalır (BR-11).
12. Video dosyası ajanın bilgisayarında bulunamazsa oynatıcı "Video dosyası bulunamadı" durumunu
    gösterir; Klasörde aç ve Sistem oynatıcısında aç pasiftir; silme normal çalışır.
13. **Silinenler — video tarafı:** silinen videolar Silinenler sayfasında listelenir ve süresiz saklanır.
    Buradan **kalıcı sil** dendiğinde onaydan sonra video kaydı ve tüm sürümlerinin dosyaları ajan
    aracılığıyla kullanıcının bilgisayarından silinir; işlem geri alınamaz (BR-7). Silinenler'de video
    yokken boş durum metni görünür. Ajan çevrimdışıyken kalıcı silmenin davranışı Open questions 5'teki
    karara, dosyaların bir kısmı silinemezse (ör. dosya açık) ne olacağı self-critique 16'ya bağlıdır.
14. Kullanıcı yalnızca kendi videolarını görür ve yalnızca onlar üzerinde işlem yapabilir (BR-1);
    başka bir kullanıcının videosu için yapılan her istek "bulunamadı" olarak sonuçlanır ve ajana hiçbir
    komut gitmez.

## User experience
- Flow: Kullanıcı üst bardan profili seçer → Üretilenler sayfasını açar → soldaki listeden bir videoya
  tıklar → video ortada oynamaya hazır olur, sağda sahneler, konsept metni, ayrıntılar ve ücret, sürümler
  görünür → oynatır, sesi ayarlar, tam ekrana geçer → gerekirse "Klasörde aç" ya da "Sistem oynatıcısında
  aç" der → sesi beğenmezse "Yeni sürüm"de ses motorunu değiştirir → yeniden üretilecek aşamaları ve
  online çağrı uyarısını görür → onaylar → sürüm listesinde yeni sürüm "Sırada → Üretiliyor (aşama) →
  Hazır" ilerler → yeni sürümü seçip izler. Videoyu istemezse "Sil" → onay → video Silinenler'e gider →
  Silinenler'de "Kalıcı sil" → onay → kayıt ve dosyalar gider.
- States:
  - Liste: yükleniyor · hata (yeniden dene) · profil yok ("Önce bir profil oluşturun") · profilde video
    yok (boş durum metni) · dolu.
  - İzleme yeri: video seçilmedi (yer tutucu) · yükleniyor · oynatılabilir · ajan bağlı değil · ajan bu
    bilgisayarda değil / yerel akışa ulaşılamıyor · video dosyası bulunamadı · akış hatası.
  - Dosya eylemleri (Klasörde aç / Sistem oynatıcısında aç): gönderiliyor · başarılı · pasif (ajan yok /
    dosya yok) · zaman aşımı (hata).
  - Sağ panel: video seçilmedi · yükleniyor · dolu; ücret: online çağrı yok ("Yerel — ücretsiz") · tutar.
  - Sürümler: tek sürüm · çok sürüm · sürüm durumu Sırada / Üretiliyor (aşama) / Ajan bekleniyor / Hata /
    Hazır.
  - Yeni sürüm: değişiklik yok (başlat pasif) · etkilenen aşama önizlemesi · onay penceresi (online çağrı
    uyarısıyla / uyarısız) · başlatıldı · reddedildi (bu video için zaten üretimde bir sürüm var).
  - Yeniden üret: düzenleme açık · düzenleme iptal (hiçbir şey olmaz) · onaylandı (yeni sürüm sıraya
    girer).
  - Sil / Kalıcı sil: onay penceresi · iptal · başarılı · kalıcı silme yapılamıyor (ajan bağlı değil).
  - Silinenler (video tarafı): boş · dolu.
  - Yetkisiz: başka kullanıcının videosu → "bulunamadı"; giriş yapılmamış → giriş sayfası.
- Design reference: `source/üretilen videolar.png` — commit `61cc824` sürümü: solda "üretilen listesi",
  ortada "izleme yeri", sağda "seslendirme metni - sahne - ayrıntılar ücretler ve ek bilgiler" olmak üzere
  üç sütun; üst bar 0001'deki gibi. Eylem düğmelerinin, sürüm listesinin ve Yeni sürüm alanının görseli
  yok — bunlar için "none — the flow above is the design". Silinenler'in video tarafı için tasarım yok:
  "none — the flow above is the design". Renk: turuncu/kahve, açık mod esas, koyu mod destekli.
- Accessibility: liste öğeleri, oynatıcı kontrolleri (oynat/duraklat, ilerleme, ses, tam ekran), dört
  eylem, sürüm satırları ve Yeni sürüm kontrolleri klavyeyle erişilebilir (Tab, Enter/Space); ikon
  düğmelerinin erişilebilir adı var; pasif düğmeler nedenini ipucu olarak hem fareyle hem klavye
  odağında söyler; onay pencereleri odağı içine alır ve Esc ile iptal edilir; metin/arka plan kontrastı
  her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-3 Maliyet doğuran çağrılar (yeni sürüm ve yeniden üretimin online çağrıları,
  tekrarlanan başlatma, maliyet kaydı).
- Sensitive area: H-1 Kimlik ve yetki (yerel akış yetkisi, başka kullanıcının videosuna erişimin
  engellenmesi — BR-1; H-1'in "bir kullanıcı başkasının verisine erişebilir" bedeli doğrudan burada).
  Reviewer tarafından eklendi; insan H-1'i gereksiz bulursa kaldırılabilir (Open questions, self-critique 1).
- H-4: bu spec yeni bir kod çalıştırma yolu açmaz; yeni sürüm ve yeniden üretimin render'ı 0006'daki
  kısıtlı render ortamından aynen geçer (self-critique 17).
- Bağımlılık: 0006 (üretim işi, aşamalar, aşama çıktıları, üretim sırası, ajan render) shipped olmadan
  bu spec plan'a geçmez; 0004 (maliyet kaydı, model seçimleri), 0005 (konsept düzenleme döngüsü BR-6),
  0009 (ajan kurulumu) varsayılır. Video motoru ve ses motoru seçenek listeleri B-5 raporundaki kararlar
  verilince kesinleşir; rapor yalnızca öneridir.
- BR-9 aşama düzeyinde uygulanır; sahne ya da cümle bazında kısmi yeniden üretimin kapsam dışı
  kalması önerilir (Open questions, self-critique 5).
- Out of scope: yayınlama, videonun ajansız/başka cihazdan izlenmesi ve sunucuya yüklenmesi (ADR-0002),
  Günlük şeridi kayıtları (0008), konsept tarafındaki Silinenler (0005), üretim sırasının ve aşamaların
  kendisi (0006). Önerilen ek kapsam dışılar (insan onayı bekliyor): videonun indirilmesi, video düzenleme
  (kesme, kırpma), tahmini maliyet hesabı (Open questions 4).
- Split: proposed 0007-a Üretilen videolar: liste, oynatıcı, dosya eylemleri, sağ panel, sürümleri
  görüntüleme (H-1) · 0007-b Video silme ve Silinenler'in video tarafı (kalıcı silme) · 0007-c Yeni sürüm
  ve yeniden üret (H-3). Sıra: a → b → c. Bu spec 3 kullanıcı yeteneği ve 25 kriter içeriyor; insan karar
  verene kadar tek dosya olarak tutuluyor (Open questions).

## Acceptance criteria
- [ ] AC-1 — Liste yalnızca üst barda seçili profilin silinmemiş videolarını gösterir; profil değişince liste yenilenir ve önceki seçim temizlenir; başka profilin videosu listede görünmez.
- [ ] AC-2 — Boş durumlar: profil yokken "Önce bir profil oluşturun", profilde video yokken boş durum metni, video seçilmemişken izleme yerinde ve sağ panelde yer tutucu görünür.
- [ ] AC-3 — Listeden bir video fareyle ya da klavyeyle (Enter) seçilince oynatıcı en son tamamlanan sürümü yükler; sağ panel aynı sürümün sahnelerini (sırasıyla başlık + seslendirme metni), konsept metnini, ayrıntılarını ve ücretini gösterir.
- [ ] AC-4 — Ajan bağlı ve dosya mevcutken video oynatılır; oynat/duraklat, ilerleme konumu, ses ayarı ve tam ekran çalışır; video dosyası sunucuya hiç yüklenmez.
- [ ] AC-5 — Yerel akış yalnızca o bilgisayardan erişilebilir; yetkisi olmayan, süresi dolmuş ya da başka bir videoya ait yetkiyle yapılan akış isteği reddedilir; oynatma sürerken uzun bir video yetki süresi yüzünden kesilmez. (H-1, kasıtlı bozma ile kanıt)
- [ ] AC-6 — Ajan çevrimdışıyken oynatıcı "Ajan bağlı değil" durumunu gösterir, Klasörde aç ve Sistem oynatıcısında aç pasiftir ve nedenini ipucuyla söyler; liste, sağ panel ve sürümler görünmeye devam eder; ajan bağlanınca en geç 10 sn içinde oynatma yeniden mümkün olur.
- [ ] AC-7 — Video dosyası diskte yoksa oynatıcı "Video dosyası bulunamadı" gösterir, iki dosya eylemi pasiftir, Sil çalışır.
- [ ] AC-8 — "Klasörde aç" ajanın dosya gezgininde videonun klasörünü, "Sistem oynatıcısında aç" videoyu varsayılan uygulamada açmasıyla sonuçlanır; ajan 10 sn içinde cevap vermezse "Zaman aşımı" hatası görünür.
- [ ] AC-9 — Ayrıntılar ve ücret bölümü sürümün online maliyetini USD, sent hanesiyle (2 hane) ve Gereksinim 5'te onaylanan ek bilgi alanlarını (öneri: modeller ve motorlar, altyazı/ilerleme çubuğu seçimi, süre, biçim, başlangıç/bitiş zamanı) gösterir; online çağrısı olmayan sürümde "Yerel — ücretsiz" yazar; HTML ya da script içeren LLM metni düz metin olarak görünür ve çalışmaz.
- [ ] AC-10 — Sürüm listesi her sürümü numara, üretim zamanı, değişiklik özeti, ücret ve durumla gösterir; bir sürüm seçilince oynatıcı ve sağ panel o sürüme geçer ve konsept metni o sürümün üretildiği metindir; tek sürümlü videoda yalnızca "Sürüm 1" görünür.
- [ ] AC-11 — Yeni sürüm önizlemesi BR-9 tablosuna uyar (tablo tabanlı test): ses motoru değişimi → seslendirme + birleştirme; altyazı açıkken ses değişimi → + render; video motoru değişimi → video kodu + render + birleştirme ve önizleme "online model çağrısı yapılacak" der; yerel → yerel ses değişiminde önizleme "online model çağrısı yapılmayacak" der; hiçbir ayar değişmemişse başlatma pasiftir.
- [ ] AC-12 — Yeni sürüm üretiminde yalnızca önizlenen aşamalar çalışır, diğer aşamaların çıktıları önceki sürümden aynen kullanılır; önceki sürümün dosyaları ve kaydı değişmez. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-13 — Yerel bir ses motorundan başka bir yerel ses motoruna geçişte hiçbir online model çağrısı yapılmaz (stub sayacı 0) ve sürümün ücreti "Yerel — ücretsiz" olur. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-14 — (Open questions 4 onaylanırsa) Online çağrı gerektiren yeni sürüm ya da yeniden üretim, çalışacak aşamaları ve "online model çağrısı yapılacak" uyarısını gösteren onay olmadan başlamaz; onay penceresinde iptal edilirse hiçbir iş oluşmaz ve hiçbir çağrı yapılmaz. (H-3)
- [ ] AC-15 — Yapılan her online çağrının token ve maliyeti kaydedilir; sürümün ücreti bu kayıtların toplamına, aylık harcama da bu tutar kadar artar (BR-12). (H-3)
- [ ] AC-16 — Aynı yeni sürüm/yeniden üret isteğinin tekrarı (çift tıklama, yeniden gönderim) ikinci bir iş oluşturmaz; (self-critique 14 onaylanırsa) bir video için sırada ya da üretimde bir sürüm varken gelen yeni istek de iş oluşturmaz ve "Bu video için zaten üretimde bir sürüm var" mesajı görünür. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-17 — Üretimi süren sürümün durumu Sırada / Üretiliyor (aşama adı) / Ajan bekleniyor / Hata / Hazır olarak görünür ve en geç 5 sn içinde güncellenir; başarısız sürüm Hata gösterir, önceki sürümler oynatılabilir kalır.
- [ ] AC-18 — Ajan çevrimdışıyken başlatılan yeni sürüm "Ajan bekleniyor" durumunda kalır ve hiçbir yerel aşama başlamaz; ajan bağlanınca üretim kendiliğinden devam eder (BR-11).
- [ ] AC-19 — "Yeniden üret" önce konsept düzenlemesini açar; düzenleme iptal edilirse hiçbir iş oluşmaz; düzenleme onaylanınca tüm aşamalar baştan çalışır; sonuç Open questions 1'deki karara göre (öneri) yeni bir sürüm olarak eklenir ve eski sürümler korunur.
- [ ] AC-20 — "Sil" onaydan sonra videoyu tüm sürümleriyle listeden kaldırır ve Silinenler'in video tarafında gösterir; dosyalar diskte kalır; onay penceresinde iptal edilirse hiçbir şey değişmez.
- [ ] AC-21 — Silinenler'de "Kalıcı sil" onaydan sonra video kaydını ve tüm sürüm dosyalarını kullanıcının bilgisayarından siler; kayıt bir daha hiçbir yerde görünmez; onay penceresinde iptal edilirse hiçbir şey değişmez; Silinenler'de video yokken boş durum metni görünür; ajan çevrimdışıyken kalıcı silme Open questions 5'teki karara göre davranır (öneri: yapılamaz ve "Ajan bağlı değil" der).
- [ ] AC-22 — Başka bir kullanıcının videosu için görüntüleme, akış, dosya eylemleri, yeni sürüm, yeniden üret, sil ve kalıcı sil istekleri "bulunamadı" döner ve ajana komut gitmez. (H-1, kasıtlı bozma ile kanıt)
- [ ] AC-23 — Giriş yapmamış kullanıcı sayfayı açmak istediğinde giriş sayfasına yönlenir; liste yüklenemezse sade bir Türkçe hata mesajı ve "Yeniden dene" görünür, "Yeniden dene" listeyi tekrar yükler (stack trace yok).
- [ ] AC-24 — Liste öğeleri, oynatıcı kontrolleri, dört eylem, sürüm satırları, Yeni sürüm kontrolleri ve onay pencereleri yalnızca klavyeyle (Tab, Enter/Space, Esc) kullanılabilir; ikon düğmelerinin erişilebilir adı vardır; pasif düğmelerin nedeni fare ve klavye odağında ipucu olarak okunur.
- [ ] AC-25 — Yükleme ve akış durumları: liste, izleme yeri ve sağ panel yüklenirken yükleniyor göstergesi görünür; ajan bağlı olduğu hâlde yerel akışa ulaşılamazsa (site başka cihazdan açıldı vb.) izleme yeri Open questions 7'deki metni (öneri: "Bu video yalnızca ajanın kurulu olduğu bilgisayarda (cihaz adı) izlenebilir"), oynatma sırasında akış koparsa sade bir "akış hatası" mesajı ve yeniden deneme gösterir; dosya eylemi gönderilirken düğme "gönderiliyor" durumundadır ve başarıda kısa bir onay görünür.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**Clarify soruları (etkiye göre sıralı):**
1. **"Yeniden üret" ile "Yeni sürüm" arasındaki fark ve yeniden üret akışı.** Brief: "Yeniden üretle
   düzenleyeceksin yeniden üretmeden önce." Öneri: Yeniden üret = konsept metni değişir; bu sayfada
   0005'teki düzenleme döngüsü (doğrudan metin düzenleme ya da fikir yazıp online düzenleme → revizyon
   modalı → onay, BR-6) açılır; onaylanan revizyon konseptin yerini alır ve aynı videonun **yeni bir
   sürümü** olarak her şey baştan üretilir; eski sürümler silinmez. Yeni sürüm = konsept aynı, yalnızca
   ayar değişir (kısmi). Brief sürümlerin saklanıp listelenmesinden söz etmiyor; Gereksinim 6 (sürüm
   listesi) ve AC-10 bu öneriye bağlıdır — "yeni sürüm eskisinin yerini alır" denirse ikisi sadeleşir. Gerekçe: tek düzenleme davranışı (BR-6) iki yerde tutarlı kalır; eski sürümleri
   korumak kullanıcının karşılaştırmasına imkân verir ve hiçbir şey kaybolmaz.
2. **Yeni sürümde hangi ayarlar değiştirilebilir ve seçenekler neler?** BR-9 yalnızca ses, video motoru
   ve konsept metnini tanımlıyor. Öneri: v1'de ses motoru/sesi ve video motoru; ek olarak altyazı ve
   ilerleme çubuğu aç/kapa (→ render + birleştirme, B-5 K-4 kabul edilirse yalnızca son birleştirme) ve
   görsel modeli (→ görsel + render + birleştirme) — bunlar için BR-9 tablosu `docs/domain.md`'de
   genişletilmeli. Seçenek listeleri B-5 raporundaki **bekleyen** kararlara bağlı: video motoru K-1
   (HyperFrames varsayılan), K-2 (Remotion opsiyonel, kapalı, lisans uyarılı); brief Revideo'yu da
   sayıyor ama rapor Revideo'yu "bırak" diyor — insan kararı gerekir; ses K-6 (Supertonic 3
   yerel varsayılan), K-8 (Chatterbox ikinci yerel), K-9 (ElevenLabs online); brief'teki "kapalı / sessiz video" seçeneğine
   yeni sürümde geçilebilir mi (öneri: evet → yalnızca birleştirme, altyazı açıksa render). Gerekçe: brief'teki
   örnek tam olarak ses motoru değişimi; ek ayarlar aynı "yalnız etkilenen aşama" ilkesine ucuzca uyuyor,
   ama kural değişikliği insan kararı ister.
3. **Yeni sürüm/yeniden üretim nerede çalışır?** Öneri: 0006'daki üretim sırasına bir iş olarak, sıranın
   sonuna eklenir; BR-10 (durdur/devam) ve BR-11 geçerlidir; Konseptten Video sayfasında da görünür,
   bu sayfada sürüm durumu olarak izlenir. Gerekçe: GPU işleri sıralı (B-5 K-14), ikinci bir kuyruk
   karmaşası ve eşzamanlı render riski olmaz.
4. **Maliyet gösterimi ve onayı (H-3).** Öneri: başlatmadan önce onay penceresi: çalışacak aşamalar +
   "online model çağrısı yapılacak / yapılmayacak"; tahmini tutar gösterilmez (güvenilir tahmin verisi
   yok, BR-12 bilgi amaçlı); online çağrı yoksa onay sorulmadan başlar. Ücret: sürüm başına gerçek tutar,
   ayrıca video toplamı (tüm sürümler). Gerekçe: yanlış bir tahmin güveni bozar; gerçek kayıt BR-12 ile
   zaten var; yerel değişimde gereksiz tıklama olmaz.
5. **Silme kapsamı, kalıcı silme ve geri alma.** Öneri: (a) "Sil" videonun tamamını tüm sürümleriyle
   Silinenler'e gönderir; tek sürüm silme v1'de yok (backlog). (b) Ajan çevrimdışıyken kalıcı silme
   yapılamaz, "Ajan bağlı değil" der. (c) Silinenler'den "Geri al" — brief'te yok; öneri: eklensin
   (kayıt ve dosyalar yerinde olduğu için ucuz) ama yalnızca sen onaylarsan. (d) Video silinince konsept
   "Video oldu" durumunda kalır, BR-4 engeli sürer. Gerekçe: (b) dosyası silinmemiş yetim kayıt ya da
   gizli "bekleyen silme" durumu oluşmaz; (a) en sade ve test edilebilir.
6. **Liste içeriği, sıralama, üretimdeki videolar.** Öneri: öğe = başlık, süre, üretim tarihi, sürüm
   sayısı, üretimde sürüm varsa küçük durum rozeti; en yeni üstte; listede yalnızca en az bir sürümü
   tamamlanmış videolar (ilk üretim süren konsept 0006'da izlenir); 50'şer yükleme. Gerekçe: 250 videoluk
   toplu üretim senaryosu (brief) uzun liste demek; tamamlanmamış video izlenemez.
7. **Ajan farklı bir bilgisayarda/telefondan açılan site.** Öneri: video hangi cihazın ajanında
   üretildiyse yalnızca orada oynatılır; site başka cihazdan açıldığında ya da yerel akışa ulaşılamazsa
   "Bu video yalnızca ajanın kurulu olduğu bilgisayarda (cihaz adı) izlenebilir" yazar. Gerekçe: ADR-0002
   bedeli; kullanıcıya nedenini söylemek "bozuk" algısını önler.

**Self-critique boşlukları (hostile developer okuması, önerilerimle):**
1. Yerel akış yetkisi H-1 mi? Hata, başka birinin videoyu izlemesine yol açar. Reviewer Constraints'e
   "Sensitive area: H-1" ekledi; AC-5 ve AC-22 kasıtlı bozma ile kanıtlanacak. Öneri: kalsın. Gerekçe:
   H-1'in bedeli (başkasının verisine erişim) birebir bu kriterlerde.
2. AC-5 "uzun video kesilmez": yetki ömrü plan'da belirlenir; kanıt: yetki ömründen uzun bir test
   videosunun kesintisiz oynaması — öneri: böyle.
3. AC-6/AC-8 "10 sn" ve AC-17 "5 sn": 0001'deki 10 sn ve 2–3 sn polling ile tutarlı — öneri: kalsın.
4. Oynatma başlangıç süresi ölçüsü yok. Öneri: yerel akışta seçimden sonra en geç 3 sn içinde oynatmaya
   hazır olma kriteri eklensin.
5. Kısmi yeniden üretim granülerliği: B-5 K-5 (sahne başına klip) ve cümle bazlı ses önbelleği daha
   fazla tasarruf sağlar. Öneri: v1'de BR-9 aşama düzeyinde kalsın; sahne/cümle bazlı kısmi üretim
   backlog'a. Gerekçe: test edilebilir sınır, 0006 karmaşıklığına bağımlılık.
6. Başarısız sürüm için eylemler tanımsız. Öneri: Hata durumundaki sürümde "Tekrar dene" (yarım aşamadan,
   BR-10) ve "Sürümü kaldır" olsun; hata nedeni sade metinle görünsün.
7. Ücret biçimi: $0,01'in altındaki tutarlar. Öneri: "<$0,01" gösterilsin, ayrıntıda tam değer
   (sent altı hassasiyetle).
8. Ses düzeyi tercihi hatırlanmalı mı? Öneri: tarayıcıda hatırlansın (tema gibi).
9. Konsept metni sürümler arasında değiştiyse sağ paneldeki "konsept metni" hangisi? Öneri: her sürüm
   kendi üretildiği metni gösterir (AC-10'da yazıldı), güncel konsept ayrıca işaretlenmez.
10. Video dosyası kullanıcı tarafından elle taşınırsa/silinirse kayıt ne olur? Öneri: kayıt kalır, AC-7
    durumu görünür; kullanıcı silebilir; otomatik temizleme yok.
11. AI ses beyanı (B-5 K-7, bekleyen karar): kabul edilirse "Ayrıntılar"da "Yapay zekâ ile üretilmiş
    ses" bilgisi görünsün — öneri: K-7 kararıyla birlikte eklensin.
12. Spec 25 kriterle büyük. Öneri: Constraints'teki 0007-a/b/c bölünmesi; 0007-c H-3 olduğu için ayrı
    review ve kasıtlı bozma turu daha net izlenir.
13. Ayrıntılar bölümünün alan listesi (Gereksinim 5, AC-9) brief'te yok; brief yalnızca "ayrıntılar ve
    ücret", tasarım "ek bilgiler" diyor. Öneri: modeller/motorlar, altyazı ve ilerleme çubuğu seçimi,
    süre, biçim, başlangıç/bitiş zamanı. Gerekçe: Günlük örneğindeki süre/başlangıç/bitiş bilgisiyle ve
    yeni sürüm kararının (hangi motorla üretildi) ihtiyaç duyduğu bilgiyle örtüşüyor.
14. "Bir video için aynı anda yalnızca bir sürüm" (Gereksinim 9) brief'te yok. Öneri: kalsın. Gerekçe:
    aynı videonun iki sürümünün paralel üretimi H-3 tekrar riskini ve aşama çıktısı paylaşım
    karmaşasını artırır; GPU işleri zaten sıralı (B-5 K-14).
15. Sırada/üretimde sürümü olan videoyu silmek. Öneri: Sil düğmesi pasif olsun ve "Önce üretimi
    durdurun" desin. Gerekçe: arka planda çalışan bir işin silinmiş bir videoya yazması ve fark
    edilmeden para harcaması (H-3) engellenir.
16. Kalıcı silmede dosyaların bir kısmı silinemezse (sistem oynatıcısında açık, izin yok). Öneri: kayıt
    Silinenler'de kalır, "Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin" görünür; yetim
    dosya ya da yetim kayıt bırakılmaz. Gerekçe: BR-7 "dosyalarıyla birlikte" diyor.
17. H-4: yeni sürümde video motoru değişince LLM yeni video kodu yazar ve ajan render eder. Öneri: bu
    spec render için 0006'daki kısıtlı ortamı aynen kullanır; plan'da "ayrı render yolu yok" architecture
    testiyle kanıtlansın. Gerekçe: H-4'ü bu spec'te ikinci kez açmamak.
18. "Yeniden üret"te düzenleme onaylandı ama metin hiç değişmedi. Öneri: başlat pasif olsun ("Konsept
    değişmedi"); kullanıcı aynı ayarlarla gerçekten tekrar üretmek isterse açık bir "Yine de baştan
    üret" onayı ve online çağrı uyarısı istensin. Gerekçe: farkında olmadan tam maliyetli tekrar (H-3).

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
