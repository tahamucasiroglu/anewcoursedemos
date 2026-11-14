# Spec 0010 — admin-paneli

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0010-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
Sistemin yöneticisi (admin), ayrı bir yetkiyle girdiği bir yönetim alanında platformun genel durumunu
grafiklerle görmek, kullanıcıları listelemek ve gerektiğinde banlamak, kullanıcıların ve ajanların
gönderdiği hata bildirimlerini incelemek istiyor (ilk_promt.txt: "istatistikleri göreceğiz… kullanıcı
listesi, üretilen toplam klip sayısı, kullanıcıların aktiviteleri… banlayabiliriz"; "charts olarak da güzel
olabilir"; "API'ye giden hatalar depolanmalı ve bir adet admin panel gerekecek bunlara bakmak ve
yönetmek için"; `Promtlar/0004`: "masaüstü ile web'den bildirilen hataların incelenmesi"). Başarı: `admin.panel` yetkisi olan kullanıcı paneli açıp bu dört işi
yapabiliyor; yetkisi olmayan hiçbir kullanıcı panele ya da verisine hiçbir yoldan ulaşamıyor; banlanan
kullanıcı web'de de ajanda da anında sistem dışı kalıyor (BR-13). Admin, kullanıcıların konsept, video,
profil ya da API key içeriklerini görmez (BR-1). Öncelik düşüktür; önce ürün akışı çalışır. Bilinçli olarak
yapılmayanlar: yayınlama yönetimi (yalnızca "Çok yakında" kalır), rol/yetki düzenleme ekranı, kullanıcı
silme, kullanıcı adına işlem yapma.

## Requirements
1. Admin paneli yalnızca `admin.panel` yetkisi olan, giriş yapmış kullanıcıya açılır (rol ve yetki
   modeli 0002'den gelir). Panele giriş noktası yalnızca bu yetkiye sahip kullanıcıya görünür; asıl
   karar her zaman sunucudadır — yetkisiz kullanıcı doğrudan adresi açsa ya da veriyi istese de
   reddedilir (`docs/security.md`, default-deny).
2. Panelin **İstatistikler** bölümü özet sayıları gösterir: toplam kullanıcı sayısı ve üretilen toplam
   klip (video) sayısı (brief). Banlı kullanıcı sayısı ve açık hata bildirimi sayısı öneridir (Open
   question 3).
3. İstatistikler bölümü zaman içindeki değişimi **grafiklerle** gösterir (brief: "charts olarak da güzel
   olabilir"). Hangi grafikler ve aralık Open question 3'te; öneri: günlük yeni kayıt, günlük üretilen
   klip ve günlük aktif kullanıcı sayısı, varsayılan aralık son 30 gün. AC-3/AC-4 bu öneriye göre
   yazılmıştır, karar değişirse onlar da değişir.
4. Panelin **Kullanıcılar** bölümü tüm kullanıcıları listeler (brief: "kullanıcı listesi"): en az ad,
   e-posta ve durum (aktif / banlı). Ek sütunlar (kayıt tarihi, son giriş zamanı, rol, kullanıcının
   ürettiği klip sayısı), e-posta/ada göre arama ve sayfalama öneridir (Self-critique 1, 2, 11).
5. Admin bir kullanıcıyı **banlayabilir** (brief: "kullanıcıları sistemden banlayabiliriz"): onay
   penceresinde onaylar (sebep alanı ve isteğe bağlı olması öneri, Open question 5). Var olmayan bir
   kullanıcıya ban isteği reddedilir.
   Ban anında etkili olur (BR-13): kullanıcının web oturumları ve ajan oturumu dahil tüm token'ları
   iptal edilir, ajanın canlı bağlantısı sunucu tarafından kapatılır, kullanıcı tekrar giriş yapamaz ve
   ajanı yeniden bağlanamaz/eşleştirilemez.
6. (Öneri, Open question 5 — brief'te yok.) Admin banlı bir kullanıcının **banını kaldırabilir**; kullanıcı yeniden giriş yapabilir (eski
   oturumları geri gelmez, yeniden giriş ve gerekirse ajan eşleştirmesi gerekir).
7. Banlı kullanıcı giriş denediğinde hesabının askıya alındığını söyleyen sade bir mesaj görür;
   bu denemenin 3 hak sayacını tüketmemesi ve mesaj metni öneridir (Open question 5).
8. (Öneri, Open question 6.) Admin kendi hesabını banlayamaz; başka bir admini banlayabilme kuralı da
   Open question 6'dadır.
9. (Öneri, Open question 5.) Her ban ve ban kaldırma işlemi kim tarafından, ne zaman, hangi kullanıcıya ve hangi sebeple
   yapıldığıyla birlikte kaydedilir ve kullanıcının satırından/detayından görülebilir.
10. Panelin **Aktiviteler** bölümü kullanıcı aktivitelerini zaman sırasıyla (en yeni üstte) gösterir:
    zaman, kullanıcı, aktivite türü (ör. giriş, çıkış, konsept üretti, video üretimi tamamlandı, hata
    bildirdi, banlandı). Kullanıcıya ve tarih aralığına göre filtrelenebilir, sayfalıdır. Aktivite
    kaydı kullanıcının içeriğini (konsept başlığı/metni, prompt, profil içeriği, API key) **göstermez**
    (BR-1; kapsam Open question 4).
11. Panelin **Hata bildirimleri** bölümü 0008'de toplanan hata bildirimlerini (kullanıcının web'den ve
    ajandan bildirdikleri ile brief'teki "API'ye giden hatalar"; hangilerinin otomatik toplanacağı 0008'de
    karara bağlanır) listeler: zaman, kaynak
    (web / ajan / API), bildiren kullanıcı, kısa mesaj, durum. Kaynağa ve duruma göre filtrelenebilir,
    sayfalıdır; varsayılan görünüm açık (çözülmemiş) bildirimlerdir.
12. Admin bir hata bildiriminin detayını açar (kullanıcının yazdığı açıklama, teknik bilgi, korelasyon
    numarası, ajan/uygulama versiyonu) ve durumunu değiştirir (durum listesi Open question 7). Detayda
    şifre hiç, API key yalnızca maskeli görünür (`docs/security.md`).
13. Panel sayfaları arayüz metinlerini Türkçe gösterir; tarih ve saatler admin'in yerel saatiyle
    gösterilir.
14. Panelde yayınlamaya dair bir yönetim yoktur; yayınlama yalnızca mevcut "Çok yakında sizlerleyiz"
    sayfası olarak kalır.

## User experience
- Flow: Admin giriş yapar → (yalnızca ona görünen) Admin giriş noktasına tıklar → İstatistikler
  bölümü açılır: üstte özet sayılar, altında grafikler → Kullanıcılar sekmesine geçer, e-postayla
  arar → bir kullanıcının satırında "Banla" der → onay penceresinde sebebi yazar, onaylar → satırın
  durumu "Banlı" olur, ban kaydı görünür → aynı satırda "Banı kaldır" ile geri alabilir → Aktiviteler
  sekmesinde kullanıcıya göre filtreler → Hata bildirimleri sekmesinde açık bildirimlerden birini açar,
  detayı okur, durumunu değiştirir → liste güncellenir.
- States:
  - Yetkisiz: `admin.panel` yetkisi olmayan kullanıcıda giriş noktası görünmez; adres doğrudan açılırsa
    "Bu sayfaya erişim yetkiniz yok" sayfası; giriş yapmamış kullanıcı giriş sayfasına yönlenir.
  - Her bölüm: yükleniyor · yüklendi · yükleme hatası (Türkçe sade mesaj + korelasyon numarası + tekrar dene).
  - İstatistikler boş: hiç veri yoksa sayılar 0, grafikler "Bu aralıkta veri yok" boş durumu.
  - Kullanıcılar: liste · arama sonucu yok ("Eşleşen kullanıcı yok") · ban onay penceresi · ban işleniyor
    · ban başarılı · ban başarısız (hata mesajı, durum değişmez) · kendini banlama engeli (eylem pasif,
    ipucu) · banı kaldır onayı/başarılı/başarısız.
  - Aktiviteler: liste · boş ("Henüz aktivite yok" / "Filtreye uyan aktivite yok").
  - Hata bildirimleri: liste · boş ("Açık hata bildirimi yok") · filtre sonucu boş ("Filtreye uyan
    bildirim yok") · detay · durum değiştiriliyor · durum değiştirme başarılı/başarısız.
  - Aktiviteler tarih filtresi: başlangıç bitişten sonraysa filtre uygulanmaz, "Başlangıç tarihi
    bitişten sonra olamaz" uyarısı.
  - Banlı kullanıcının gördüğü: giriş ekranında askıya alındı mesajı; açık sayfası varsa bir sonraki
    istekte oturumu düşer ve giriş sayfasına döner; ajan "oturum kapandı" durumuna geçer.
- Design reference: none — the flow above is the design. (Tasarım görselleri `source/*.png`, commit
  `61cc824`, admin panelini ve giriş noktasını içermiyor; üst bar, tema ve renkler 0001'deki ortak
  iskeletten gelir. Giriş noktasının yeri Open question 1.)
- Accessibility: sekmeler, tablolar, filtreler, ban/durum eylemleri ve onay pencereleri klavyeyle
  kullanılabilir (Tab, Enter/Space, Esc pencereyi kapatır, odak pencereye taşınır ve kapanınca geri
  döner); her grafiğin aynı veriyi gösteren erişilebilir bir tablo alternatifi vardır; durum (aktif/banlı,
  bildirim durumu) yalnızca renkle değil metinle de gösterilir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki (admin yetkisi, ban, token iptali, ajan bağlantısının düşürülmesi).
- Sensitive area: H-2 Gizli bilgiler (yalnızca hata bildirimi detayında API key'in maskeli görünmesi;
  maskelemenin kendisi 0008'de yapılır — Self-critique 13).
  Lite modda da netleştirme sorulur, her düzeltme turu yeniden review edilir ve H-1/H-2 kriterleri (AC-1,
  AC-2, AC-8…AC-12, AC-16) kasıtlı bozma ile kırmızıya döndürülerek kanıtlanır.
- Bağımlılıklar: 0002 (roller, `admin.panel` yetkisi, oturumlar, ajan eşleştirme, 3 hak) ve 0008 (hata
  bildirimleri, Günlük/aktivite kayıtları) önce ship edilmiş olmalı; klip istatistikleri 0006/0007'nin
  ürettiği video kayıtlarına dayanır.
- Veri kaynağı sınırı: panel ayrı bir modül değildir; veriyi Monitoring, Identity ve Production'ın
  açık sözleşmelerinden okur (`docs/architecture.md`). Admin; konsept, video içeriği, profil, API key ve
  harcama ayrıntısını görmez (BR-1).
- Ölçek: tek Pi, az kullanıcı; panel sayfaları sıradan bir LAN bağlantısında birkaç saniyede açılır.
  Canlı (polling) yenileme yok; veri sayfa açılışında ve filtre değişiminde gelir.
- Out of scope: yayınlama yönetimi ve `publish.use` yetkisinin verilmesi · rol/yetki atama ve düzenleme
  ekranı (ilk admin'in nasıl atandığı Open question 2) · kullanıcı silme, şifre sıfırlama, kullanıcı
  adına giriş · süreli ban · teknik log (Serilog) görüntüleyici · admin'e bildirim (e-posta vb.) ·
  dışa aktarma (CSV) · HTTPS (B-3).
- Split: proposed 0010a İstatistikler + Aktiviteler (salt okunur, hassas değil) → 0010b Kullanıcılar ve
  ban (H-1) → 0010c Hata bildirimi yönetimi (0008'e bağlı). Gerekçe: üç ayrı kullanıcı yeteneği ve 10'dan
  fazla kriter; H-1 içeren 0010b'nin ayrı ve küçük olması kasıtlı bozma kanıtını ve review'u
  kolaylaştırır. Karar insanda (Open question 8).

## Acceptance criteria
- [ ] AC-1 — `admin.panel` yetkisi olan kullanıcı panel giriş noktasını görür ve paneli açar; yetkisi olmayan giriş yapmış kullanıcı giriş noktasını görmez, panel adresini doğrudan açınca "Bu sayfaya erişim yetkiniz yok" görür; giriş yapmamış kullanıcı giriş sayfasına yönlenir.
- [ ] AC-2 — Panelin her veri isteği (istatistik, kullanıcı listesi, aktiviteler, hata bildirimleri, ban/ban kaldırma, durum değiştirme) `admin.panel` yetkisi olmayan bir kullanıcının token'ıyla sunucuya doğrudan gönderildiğinde reddedilir ve hiçbir veri dönmez / hiçbir değişiklik olmaz; yetki kontrolü kaldırıldığında bu test kırmızıya döner.
- [ ] AC-3 — İstatistikler bölümü toplam kullanıcı, banlı kullanıcı, toplam üretilen klip ve açık hata bildirimi sayılarını bilinen bir veri setiyle birebir doğru gösterir; hiç veri yokken tüm sayılar 0'dır.
- [ ] AC-4 — Grafikler son 30 günün günlük yeni kayıt, üretilen klip ve aktif kullanıcı değerlerini bilinen veri setiyle doğru gösterir; veri olmayan günler 0 olarak görünür; aralıkta hiç veri yoksa "Bu aralıkta veri yok" boş durumu; her grafiğin aynı değerleri içeren tablo alternatifi vardır.
- [ ] AC-5 — Kullanıcılar listesi Requirements 4'teki alanları gösterir; e-posta/ada göre arama eşleşenleri getirir, eşleşme yoksa "Eşleşen kullanıcı yok" görünür; sayfa boyutundan fazla kullanıcı varken sayfalama tüm kullanıcılara erişim sağlar.
- [ ] AC-6 — Her bölüm yüklenirken yükleniyor durumunu, sunucu hatasında Türkçe sade mesaj + korelasyon numarası + "Tekrar dene"yi gösterir; stack trace, SQL ya da gizli bilgi görünmez.
- [ ] AC-7 — Banla eylemi onay penceresi açar; vazgeçilince hiçbir şey değişmez; onaylanınca istek sürerken onay düğmesi pasif ve "işleniyor" görünür (çift tıklama tek ban üretir); başarılıysa satır "Banlı" olur ve ban kaydı (yapan admin, zaman, sebep) görünür; sebep boş bırakılabilir; sunucu hatasında durum değişmez ve hata mesajı görünür; var olmayan bir kullanıcıya ban isteği reddedilir.
- [ ] AC-8 — Banlanan kullanıcının o anda açık web oturumu bir sonraki isteğinde geçersizdir ve giriş sayfasına döner; ban öncesi alınmış erişim ve yenileme token'larının hiçbiri artık kabul edilmez.
- [ ] AC-9 — Banlanan kullanıcının bağlı ajanının canlı bağlantısı ban anından en geç 10 sn içinde sunucu tarafından kapatılır ve ajan "oturum kapandı" durumunu gösterir; ajan yeniden bağlanamaz ve yeni cihaz eşleştirmesi reddedilir.
- [ ] AC-10 — Banlı kullanıcı doğru şifreyle giriş denediğinde giriş reddedilir ve askıya alındı mesajı görünür; (Open question 5 önerisi kabul edilirse) bu deneme 3 hak sayacını azaltmaz.
- [ ] AC-11 — "Banı kaldır" onay penceresi açar, vazgeçilince durum değişmez; onaylanınca banı kaldırılan kullanıcı yeniden giriş yapabilir; ban öncesi token'ları geçersiz kalmaya devam eder; ban kaldırma kaydı (yapan admin, zaman) görünür; sunucu hatasında durum "Banlı" kalır ve hata mesajı görünür.
- [ ] AC-12 — Admin kendi satırında banla eylemini kullanamaz (pasif, ipucu "Kendinizi banlayamazsınız"); aynı istek sunucuya doğrudan gönderilse de reddedilir.
- [ ] AC-13 — Aktiviteler bölümü aktiviteleri en yeni üstte zaman, kullanıcı ve tür ile listeler; kullanıcı ve tarih aralığı filtreleri doğru sonuç verir; başlangıcı bitişten sonra olan aralıkta filtre uygulanmaz ve uyarı görünür; hiç aktivite yoksa "Henüz aktivite yok", filtreye uyan yoksa "Filtreye uyan aktivite yok" görünür.
- [ ] AC-14 — Aktiviteler, kullanıcılar ve hata bildirimleri ekranlarında ve bunları besleyen yanıtlarda hiçbir kullanıcının konsept başlığı/metni, prompt'u, profil içeriği ya da API key'i yer almaz (bilinen içerikli bir veri setinde arama ile kanıtlanır).
- [ ] AC-15 — Hata bildirimleri bölümü varsayılan olarak açık bildirimleri listeler; kaynak ve durum filtreleri doğru sonuç verir; açık bildirim yoksa "Açık hata bildirimi yok", filtreye uyan yoksa "Filtreye uyan bildirim yok" görünür.
- [ ] AC-16 — Bir hata bildiriminin detayı açıklama, teknik bilgi, korelasyon numarası ve versiyonu gösterir; içinde API key geçen bir bildirimde key yalnızca maskeli (`sk-…a3f`) görünür.
- [ ] AC-17 — Admin bildirimin durumunu değiştirirken istek sürerken eylem pasiftir; başarılıysa liste ve açık bildirim sayısı güncellenir; sunucu hatasında durum değişmez ve hata mesajı görünür; geçersiz bir durum değeri ya da var olmayan bir bildirim reddedilir ve durum değişmez.
- [ ] AC-18 — Panelin tüm eylemleri klavyeyle yapılabilir (Tab, Enter/Space, Esc onay penceresini kapatır, odak geri döner); durumlar metinle de gösterilir; her iki temada kontrast WCAG AA; tarih/saatler yerel saatle gösterilir.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**Netleştirme soruları (etkiye göre):**
1. **Panelin giriş noktası nerede?** Tasarım görsellerinde admin yok.
   Öneri: Üst barın sağında, "Ayarlar"ın yanında yalnızca `admin.panel` yetkisi olanlara görünen bir
   "Admin" ikonu; panel ayrı bir sayfa, içinde dört sekme (İstatistikler · Kullanıcılar · Aktiviteler ·
   Hata bildirimleri). Gerekçe: Ana sayfa menüsü (Konseptler…Yayınlama) içerik üreticisi için; admin
   işini oraya karıştırmamak tasarımı bozmaz, yetkisiz kullanıcı hiçbir iz görmez.
2. **İlk admin nasıl oluşur, admin yetkisini kim verir?** Bu spec rol atama ekranı içermiyor.
   Brief ayrıca "herkes publish'i kullanamayacak, herkes her modeli kullanamayacak" diyor; `publish.use`
   ve `model.use:<model>` yetkilerini kimin, nereden vereceği de tanımsız.
   Öneri: İlk admin, Pi'deki yapılandırmada belirtilen e-postayla kayıt olan kullanıcıya başlangıçta
   atanır (0002'de karara bağlanır); v1'de panelden rol/yetki verme-alma yok, yeni kullanıcılar varsayılan
   rolle gelir; model/yayınlama yetkisi ataması ihtiyaç doğunca ayrı spec. Gerekçe: Tek geliştirici +
   LAN; yetki yükseltme ekranı H-1'de en riskli yüzeydir ve şu an ihtiyaç yok.
3. **Hangi grafikler ve hangi zaman aralığı?** Brief yalnızca "basit istatistikler, charts" diyor.
   Öneri: Üç çizgi/çubuk grafik (günlük yeni kayıt, günlük üretilen klip, günlük aktif kullanıcı) +
   7 / 30 / 90 gün seçici, varsayılan 30. "Klip" = üretimi tamamlanan video; sonradan silinmesi sayıyı
   düşürmez (yeni sürümler ayrı sayılmaz). "Aktif kullanıcı" = o gün en az bir aktivitesi olan kullanıcı.
   Gerekçe: Brief'teki üç metrikle birebir; tanımlar sabit olmazsa sayılar testlenemez.
4. **"Kullanıcı aktiviteleri" neyi kapsar?** BR-1 admin'in yalnızca istatistik, kullanıcı ve hata
   bildirimi verisini görmesini söylüyor; Günlük kayıtları içerik (ör. konsept başlığı) taşıyabilir.
   Öneri: Aktivite = olay türü + zaman + kullanıcı (giriş, çıkış, başarısız giriş, konsept üretti (adet),
   video tamamlandı, hata bildirdi, ajan bağlandı/koptu, banlandı/ban kaldırıldı); başlık/metin yok.
   Gerekçe: Admin'in ihtiyacı "kim ne zaman ne yaptı"; içerik gösterimi BR-1 ve gizlilik ihlali olur.
5. **Banın kapsamı: süre, sebep, kullanıcıya mesaj, devam eden işler?**
   Brief yalnızca "banlayabiliriz" diyor; ban kaldırma (Req 6), ban/ban kaldırma kaydı (Req 9) ve 3 hak
   sayacına etkisi (Req 7) de karar bekliyor.
   Öneri: Süresiz ban, admin banı kaldırabilir; her ban/ban kaldırma yapan admin, zaman ve sebeple
   kaydedilir; banlı kullanıcının giriş denemesi 3 hak sayacını tüketmez (hesap zaten kapalı, kilit
   sayacı anlamsız); sebep isteğe bağlı ve yalnızca admin görür; giriş mesajı
   "Hesabınız askıya alınmıştır." (sebep gösterilmez); kullanıcının kuyruktaki ve çalışan üretim işleri
   ban anında durdurulur (tamamlanan aşamalar BR-10 gereği korunur), hiçbir verisi silinmez.
   Gerekçe: Banlı kullanıcı adına arka planda online çağrı sürmesi H-3 maliyeti ve BR-13 ihlalidir;
   veri silme geri alınamaz, ban ise geri alınabilir olmalı. İşleri durdurma önerisi kabul edilirse bu
   spec Production'a (0006) dokunur ve H-3 de "Sensitive area" satırına eklenir.
6. **Admin başka bir admini banlayabilir mi?**
   Öneri: v1'de hayır — admin olan kullanıcılarda banla eylemi yok (kendisi dahil, AC-12'ye eklenir).
   Gerekçe: Tek admin varken anlamsız, birden çok admin olunca "son admin kilitlendi" riskini sıfırlar.
7. **Hata bildirimi durumları ve yönetim eylemleri neler?** (0008 ile ortak karar.)
   Öneri: Yeni → İnceleniyor → Çözüldü | Yok sayıldı; "açık" = Yeni + İnceleniyor; isteğe bağlı admin
   notu; silme yok. Gerekçe: Az ve net durum filtrelemeyi ve açık sayısını testlenebilir yapar; silme
   yerine "Yok sayıldı" iz bırakır.
8. **Spec bölünsün mü?** (Constraints, Split.)
   Öneri: Evet — 0010a İstatistikler + Aktiviteler → 0010b Kullanıcılar ve ban (H-1) → 0010c Hata
   bildirimi yönetimi. Gerekçe: 18 kriter ve üç yetenek; H-1 kısmı küçük ve ayrı kalınca review ve kasıtlı
   bozma kanıtı daha güvenilir olur; düşük öncelikli olduğundan parça parça ship edilebilir.

**Self-critique boşlukları (önerilerimle, onayda katlanır):**
1. Sayfa boyutu tanımsız (AC-5, AC-13, AC-15) — öneri: 25 kayıt/sayfa, tüm listelerde aynı.
2. Arama davranışı (AC-5) — öneri: büyük/küçük harf duyarsız "içerir" araması, en az 2 karakter.
3. AC-8 "bir sonraki istek" — 15 dk'lık JWT süresi dolmadan da reddedilmeli; öneri: her istekte ban/
   oturum geçerliliği kontrol edilir, kanıt testi ban sonrası henüz süresi dolmamış JWT ile yapılır.
4. AC-9 10 sn sınırı 0001'deki ajan durum süresiyle uyumlu — öneri: kalsın.
5. Ban/ban kaldırma kaydının Aktiviteler'de de görünmesi — öneri: evet, "banlandı/ban kaldırıldı" türüyle.
6. Banlı kullanıcının listede ayırt edilmesi ve "yalnızca banlılar" filtresi — öneri: durum filtresi
   (Tümü / Aktif / Banlı) eklensin.
7. Hata bildirimindeki teknik bilgi admin'e gösterilebilir mi (security: "stack trace kullanıcıya asla")
   — öneri: admin detayında ajan/web'in gönderdiği teknik bilgi gösterilir (admin son kullanıcı değil),
   ama API key maskeli, şifre ve token hiç görünmez; içerik HTML olarak yorumlanmadan düz metin gösterilir
   (bildirimdeki `<script>` gibi metin çalışmaz, olduğu gibi görünür).
8. Admin aynı zamanda içerik üreticisi mi? — öneri: evet; admin rolü normal kullanıcı yetkilerine
   eklenir, kendi konsept/videoları BR-1 ile normal şekilde kendine aittir.
9. Panel uçlarına rate limit — öneri: gerek yok (yalnızca admin, LAN); ban eylemi tekrar gönderilirse
   idempotent (zaten banlı kullanıcı için ikinci kayıt oluşmaz).
10. Kullanıcı başına harcama (BR-12) panelde gösterilsin mi — öneri: v1'de hayır (BR-1 kapsamı dışı);
    istenirse ayrı karar.
11. Kullanıcı listesinin sütunları (Req 4) brief'te yok — öneri: ad, e-posta, kayıt tarihi, son giriş,
    durum, rol, klip sayısı; durum metinle gösterilir.
12. Ban sebebinin uzunluk sınırı tanımsız — öneri: en fazla 500 karakter; aşılırsa onay düğmesi pasif ve
    "En fazla 500 karakter" uyarısı (kabul edilirse AC-7'ye eklenir).
13. H-2 eklendi: hata bildirimi detayı API key içerebilir; admin ekranında maskesiz key görünmesi H-2
    ihlalidir — öneri: AC-16 kasıtlı bozma ile kanıtlanır (maskeleme kapatılınca test kırmızı).

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
