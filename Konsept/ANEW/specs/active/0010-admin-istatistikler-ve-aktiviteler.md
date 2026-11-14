# Spec 0010 — Admin: istatistikler ve aktiviteler

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0010-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0010-admin-paneli.md` (eski 0010a; bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun")
- Sensitive area: H-1 Kimlik ve yetki — yalnızca panel verisinin `admin.panel` yetkisiyle korunması (yetki iskeleti 0011'de)
- Depends on: 0001, 0002, 0011, 0021, 0022, 0014, 0017

## Intent
Sistemin yöneticisi (admin), ayrı bir yetkiyle girdiği yönetim alanında platformun genel durumunu özet
sayılar ve grafiklerle görmek ve kullanıcı aktivitelerini zaman sırasıyla izlemek istiyor (ilk_promt.txt:
"istatistikleri göreceğiz… kullanıcı listesi, üretilen toplam klip sayısı, kullanıcıların aktiviteleri";
"charts olarak da güzel olabilir"). Başarı: `admin.panel` yetkisi olan kullanıcı paneli açıp İstatistikler
ve Aktiviteler bölümlerini kullanabiliyor; yetkisi olmayan hiçbir kullanıcı panele ya da verisine hiçbir
yoldan ulaşamıyor. Admin, kullanıcıların konsept, video, profil ya da API key içeriklerini görmez (BR-1);
aktivite yalnızca "kim, ne zaman, ne tür işlem" bilgisidir. Öncelik düşüktür; önce ürün akışı çalışır.
Bilinçli olarak yapılmayanlar: kullanıcı listesi ve ban (0024), hata bildirimi yönetimi (0025), yayınlama
yönetimi (yalnızca "Çok yakında" kalır), rol/yetki düzenleme ekranı, kullanıcı silme, kullanıcı adına
işlem yapma, kullanıcı başına harcama gösterimi.

## Requirements
1. Admin paneli yalnızca `admin.panel` yetkisi olan, giriş yapmış kullanıcıya açılır (rol ve yetki
   iskeleti, "Bu sayfaya erişim yetkiniz yok" durumu ve ilk admin'in oluşması 0011'den gelir). Giriş
   noktası üst barda, "Ayarlar"ın yanında yalnızca bu yetkiye sahip kullanıcıya görünen "Admin"
   ikonudur (0011 kurar, bu spec içini doldurur). Asıl karar her zaman sunucudadır — yetkisiz kullanıcı
   doğrudan adresi açsa ya da veriyi istese de reddedilir (`docs/security.md`, default-deny).
2. Panel ayrı bir sayfadır ve sekmelerden oluşur. Sekme sırası: İstatistikler · Kullanıcılar ·
   Aktiviteler · Hata bildirimleri. Bu spec İstatistikler ve Aktiviteler sekmelerini kurar; Kullanıcılar
   sekmesini 0024, Hata bildirimleri sekmesini 0025 ekler. Panel açılınca İstatistikler sekmesi seçilidir.
3. **İstatistikler** bölümü özet sayıları gösterir: toplam kullanıcı sayısı, üretilen toplam klip sayısı
   ve açık hata bildirimi sayısı. (Banlı kullanıcı sayısı kartını 0024 ekler.)
   - "Klip" = üretimi tamamlanan video (0017). Videonun sonradan silinmesi sayıyı düşürmez; aynı videonun
     yeni sürümleri ayrı klip sayılmaz.
   - "Açık hata bildirimi" = durumu "Çözüldü" ya da "Yok sayıldı" olmayan bildirim. 0022 bildirimleri
     "Yeni" durumunda saklar ve bu spec anında başka durum yoktur; dolayısıyla burada açık bildirim =
     0022'deki tüm bildirimler. Durum yönetimi ve Çözüldü/Yok sayıldı bildirimin sayıdan düşmesi 0025'tedir
     (0025 AC-6 kanıtlar).
4. İstatistikler bölümü zaman içindeki değişimi **üç grafikle** gösterir: günlük yeni kayıt, günlük
   üretilen klip, günlük aktif kullanıcı. Aralık seçici 7 / 30 / 90 gün, varsayılan 30 gün. "Aktif
   kullanıcı" = o gün en az bir aktivitesi (Req 5) olan kullanıcı. Veri olmayan günler 0 değeriyle
   görünür. Her grafiğin aynı değerleri içeren erişilebilir bir tablo alternatifi vardır.
5. **Aktiviteler** bölümü kullanıcı aktivitelerini zaman sırasıyla (en yeni üstte) gösterir: zaman,
   kullanıcı (ad + e-posta), aktivite türü. Aktivite = Günlük olaylarının sunucudaki kaydıdır (aynı veri;
   0001'in olay altyapısı, 0021'in kaydı). Türler, yazan spec'lerin kendi kriterleriyle ürettiği
   olaylardır: ajan bağlandı / koptu (0001 yazar, 0012 sahiplikle kullanıcıya bağlar; 0021 Req 10 ile
   aynı kaynak); giriş, çıkış, başarısız giriş (0002); ajan eşleştirildi (0012); konsept üretti (adet ile,
   0014); video tamamlandı (0017); hata bildirdi (0022); banlandı / ban kaldırıldı (0024).
6. Aktivite kaydı kullanıcının içeriğini (konsept başlığı/metni, prompt, profil içeriği, API key, Günlük
   metninin kendisi) **göstermez**; yalnızca tür + zaman + kullanıcı görünür (BR-1).
7. Aktiviteler kullanıcıya ve tarih aralığına göre filtrelenebilir. Kullanıcı filtresi ad ya da e-postada
   büyük/küçük harf duyarsız "içerir" aramasıdır, en az 2 karakterle uygulanır. Başlangıç tarihi bitişten
   sonraysa filtre uygulanmaz ve uyarı görünür.
8. Panelin tüm listeleri sayfalıdır: sayfa başına 25 kayıt (0024 ve 0025 aynı değeri kullanır).
9. Veri sayfa açılışında, sekme değişiminde, aralık/filtre değişiminde gelir; canlı (polling) yenileme yok.
10. Panel sayfaları arayüz metinlerini Türkçe gösterir; tarih ve saatler yerel saatle (Europe/Istanbul)
    gösterilir.
11. Panelde yayınlamaya dair bir yönetim yoktur; yayınlama yalnızca mevcut "Çok yakında sizlerleyiz"
    sayfası olarak kalır.

## User experience
- Flow: Admin giriş yapar → üst barda (yalnızca ona görünen) "Admin" ikonuna tıklar → panel İstatistikler
  sekmesiyle açılır: üstte özet sayılar, altında üç grafik ve 7/30/90 gün seçici → aralığı değiştirir,
  grafikler yenilenir → Aktiviteler sekmesine geçer → kullanıcıya ve tarih aralığına göre filtreler →
  sayfalar arasında gezinir.
- States:
  - Yetkisiz: `admin.panel` yetkisi olmayan kullanıcıda giriş noktası görünmez; adres doğrudan açılırsa
    "Bu sayfaya erişim yetkiniz yok" sayfası (0011); giriş yapmamış kullanıcı giriş sayfasına yönlenir.
  - Her bölüm: yükleniyor · yüklendi · yükleme hatası (Türkçe sade mesaj + korelasyon numarası + "Tekrar dene").
  - İstatistikler boş: hiç veri yoksa sayılar 0, grafikler "Bu aralıkta veri yok" boş durumu.
  - Aktiviteler: liste · boş ("Henüz aktivite yok") · filtre sonucu boş ("Filtreye uyan aktivite yok") ·
    tarih filtresi hatası ("Başlangıç tarihi bitişten sonra olamaz", filtre uygulanmaz).
- Design reference: none — the flow above is the design. (Tasarım görselleri `source/*.png`, commit
  `61cc824`, admin panelini içermiyor; üst bar, tema ve renkler 0001'deki ortak iskeletten, giriş noktası
  0011'den gelir.)
- Accessibility: sekmeler, tablolar, aralık seçici, filtreler ve sayfalama klavyeyle kullanılabilir
  (Tab, Enter/Space); her grafiğin aynı veriyi gösteren erişilebilir bir tablo alternatifi vardır; durumlar
  yalnızca renkle değil metinle de gösterilir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki (panel verisinin yetkiyle korunması). Lite modda da netleştirme
  sorulur, her düzeltme turu yeniden review edilir ve AC-2 kasıtlı bozma ile (yetki kontrolü kaldırılınca
  kırmızı) kanıtlanır.
- Veri kaynağı sınırı: panel ayrı bir modül değildir; veriyi Monitoring, Identity ve Production'ın açık
  sözleşmelerinden okur (`docs/architecture.md`). Admin; konsept, video içeriği, profil, API key ve
  harcama ayrıntısını görmez (BR-1).
- Admin rolü normal kullanıcı yetkilerine eklenir (0011); admin aynı zamanda içerik üreticisidir ve kendi
  konsept/videoları BR-1 ile normal şekilde kendine aittir.
- Ölçek: tek Pi, az kullanıcı; panel sayfaları sıradan bir LAN bağlantısında birkaç saniyede açılır.
  Panel uçlarına rate limit uygulanmaz (yalnızca admin, LAN).
- Out of scope: kullanıcı listesi, ban ve banlı sayısı (0024) · hata bildirimi listesi/detayı/durumu
  (0025) · yayınlama yönetimi ve `publish.use` yetkisinin verilmesi · rol/yetki atama ve düzenleme ekranı
  (ilk admin 0011'de; model/yayınlama yetkisi ataması ihtiyaç doğunca ayrı spec) · kullanıcı başına
  harcama (BR-12) gösterimi · kullanıcı silme, şifre sıfırlama, kullanıcı adına giriş · teknik log
  (Serilog) görüntüleyici · admin'e bildirim (e-posta vb.) · dışa aktarma (CSV) · canlı yenileme · HTTPS (B-3).
- Split: done — 0010 İstatistikler ve aktiviteler / kardeşler: 0024, 0025

## Acceptance criteria
- [ ] AC-1 — `admin.panel` yetkisi olan kullanıcı üst barda "Admin" ikonunu görür ve paneli açar; panel İstatistikler sekmesi seçili açılır ve Aktiviteler sekmesine geçilebilir; yetkisi olmayan giriş yapmış kullanıcı ikonu görmez, panel adresini doğrudan açınca "Bu sayfaya erişim yetkiniz yok" görür; giriş yapmamış kullanıcı giriş sayfasına yönlenir.
- [ ] AC-2 — İstatistik ve aktivite verisi istekleri `admin.panel` yetkisi olmayan bir kullanıcının token'ıyla sunucuya doğrudan gönderildiğinde reddedilir ve hiçbir veri dönmez; yetki kontrolü kaldırıldığında bu test kırmızıya döner.
- [ ] AC-3 — İstatistikler bölümü toplam kullanıcı, toplam üretilen klip ve açık hata bildirimi sayılarını bilinen bir veri setiyle birebir doğru gösterir: silinmiş bir video klip sayısını düşürmez, bir videonun yeni sürümü ayrı klip sayılmaz, açık hata bildirimi sayısı 0022'de saklanan tüm bildirimlerin (hepsi "Yeni") sayısına eşittir; hiç veri yokken tüm sayılar 0'dır. ("Çözüldü"/"Yok sayıldı" bildirimin sayıdan düşmesi 0025 AC-6'da kanıtlanır.)
- [ ] AC-4 — Grafikler seçilen aralığın (7 / 30 / 90 gün, varsayılan 30) günlük yeni kayıt, üretilen klip ve aktif kullanıcı (o gün en az bir aktivitesi olan) değerlerini bilinen veri setiyle doğru gösterir; aralık değişince değerler yenilenir; veri olmayan günler 0 olarak görünür; aralıkta hiç veri yoksa "Bu aralıkta veri yok" boş durumu; her grafiğin aynı değerleri içeren tablo alternatifi vardır.
- [ ] AC-5 — İstatistikler ve Aktiviteler bölümleri yüklenirken yükleniyor durumunu, sunucu hatasında Türkçe sade mesaj + korelasyon numarası + "Tekrar dene"yi gösterir; stack trace, SQL ya da gizli bilgi görünmez.
- [ ] AC-6 — Aktiviteler bölümü aktiviteleri en yeni üstte zaman, kullanıcı ve tür ile, sayfa başına 25 kayıtla listeler ve sayfalama tüm kayıtlara erişim sağlar; kullanıcı filtresi (ad/e-posta, büyük/küçük harf duyarsız "içerir", en az 2 karakter) ve tarih aralığı filtresi doğru sonuç verir; başlangıcı bitişten sonra olan aralıkta filtre uygulanmaz ve "Başlangıç tarihi bitişten sonra olamaz" uyarısı görünür; hiç aktivite yoksa "Henüz aktivite yok", filtreye uyan yoksa "Filtreye uyan aktivite yok" görünür.
- [ ] AC-7 — İstatistikler ve Aktiviteler ekranlarında ve bunları besleyen yanıtlarda hiçbir kullanıcının konsept başlığı/metni, prompt'u, profil içeriği, Günlük metni ya da API key'i yer almaz (bilinen içerikli bir veri setinde arama ile kanıtlanır).
- [ ] AC-8 — Bu spec'teki tüm eylemler (sekme, aralık seçici, filtreler, sayfalama) klavyeyle yapılabilir; durumlar metinle de gösterilir; her iki temada kontrast WCAG AA; tarih/saatler yerel saatle (Europe/Istanbul) gösterilir.

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
