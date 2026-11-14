# Spec 0008 — gunluk-hata-bildirimi-loglama

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0008-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisi, sitede ne olup bittiğini sayfadan ayrılmadan görmek istiyor: ekranın en altındaki ince
"Günlük" şeridinde son işlemler akar (ör. "Video hazırlandı · 57 sn · başlangıç/bitiş saatleri"); hata
kırmızı, uyarı sarı, normal işlemler sade görünür ve geçmiş günlere bakılabilir. Geliştirici/admin ise
sorunları yakalayabilmek için Web, Api ve ajanda seviyeli teknik loglara ve kullanıcıların sağ üstteki
"Hata bildir" ile ya da ajanın "Bir hata oldu, bildir" uyarısıyla gönderdiği, API'de saklanan hata
bildirimlerine ihtiyaç duyuyor. Başarı: kullanıcı işlemlerini ve hatalarını Günlük'te anında görür; bir
hata birkaç adımda, gizli bilgi sızdırmadan bildirilir ve admin panelinin (0010) okuyabileceği şekilde
saklanır; yerel loglar bir hafta sonra kendiliğinden silinir. Bilinçli olarak yapılmayanlar: hata
bildirimlerinin admin tarafından listelenmesi/yönetilmesi ve istatistik grafikleri (0010), ajan kurulum
ekranının kendisi (0009), Günlük'ün ajan penceresinde gösterilmesi (Günlük yalnızca sitededir).

## Requirements
1. **Günlük şeridi:** Sitenin her sayfasının en altındaki ince "Günlük" şeridi, giriş yapmış kullanıcının
   en son işlemini gösterir (saat + metin). Yeni işlemler sayfa yenilenmeden şeride düşer. 0001'deki "son
   sistem olayı" gösterimini bu davranış devralır.
2. **Seviye görünümü:** Her Günlük kaydının bir seviyesi vardır: normal (Bilgi), Uyarı, Hata. Hata kırmızı,
   uyarı sarı, normal kayıt temanın olağan renginde görünür; renk tek işaret değildir, seviye ayrıca
   ikon/etiketle belirtilir.
3. **Süreli işlemler:** Başlangıcı ve bitişi olan işlemler (ör. video hazırlanması) Günlük'te süre ile
   birlikte başlangıç ve bitiş saatini gösterir (ilk_promt.txt: "video hazırlandı, süresi 57 saniye,
   başlangıç 10.30, bitiş …").
4. **Geçmişe bakış:** Kullanıcı şeritten Günlük geçmişini açabilir; kayıtlar gün gün ayrılmış, en yenisi
   üstte listelenir ve önceki günlere gidilebilir. (Dayanak: ilk_promt.txt "günlük bazda … ayrılacak" ve
   0001 Open questions 3 "geçmiş liste 0008'de" önerisi; geçmişin varlığı ve biçimi Open questions C-2.)
5. **Sahiplik:** Kullanıcı yalnızca kendi Günlük kayıtlarını görür (BR-1). Günlük kayıtları sunucuda
   süresiz tutulur ve gün bazında ayrılır (ilk_promt.txt: "bizim programda da sürekli tutulacak … günlük
   bazda ayrılacak"). Admin'in kullanıcı aktivitelerini hangi kayıtlardan ve hangi ayrıntıyla göreceği
   0010'da belirlenir (BR-1: admin yalnızca istatistik, kullanıcı ve hata bildirimi verisini görür).
6. **Günlük kaynakları:** Diğer özellikler (konsept üretimi, üretim aşamaları, ajan bağlantısı, giriş/çıkış
   vb.) kullanıcıya gösterilecek işlemleri Günlük'e yazabilir. Bu spec, en az ajan bağlandı/koptu ve hata
   bildirimi gönderildi kayıtlarını üretir; hangi işlemlerin yazılacağı ilgili özelliklerin spec'lerinde
   belirlenir (Open questions C-3).
7. **Teknik loglama:** Web, Api ve ajan teknik loglarını Information, Warning, Error, Critical
   seviyeleriyle yazar. Her kayıtta zaman, seviye, kaynak uygulama ve (varsa) korelasyon numarası bulunur.
   Teknik log, kullanıcıya gösterilen Günlük'ten ayrıdır (domain.md: Günlük ≠ Teknik log); ancak Günlük'e
   yazılan her işlem teknik loga da yazılır (ilk_promt.txt: "Bunlar loglara da kaydedilecek").
8. **Yerel log saklama:** Dosyaya yazılan teknik loglar (ajanın kullanıcı PC'sinde, Web ve Api'nin Pi'de)
   son 7 günü tutar; daha eski log dosyaları kendiliğinden silinir.
9. **Web'den hata bildirimi:** Sağ üstteki "Hata bildir" düğmesi (0001'de pasifti) etkinleşir. Kullanıcı
   bir açıklama yazar ve gönderir; bildirime gönderen kullanıcı, kaynak (web), zaman, ürün versiyonu ve
   bulunulan sayfa otomatik eklenir. Kullanıcı gönderilecek otomatik bilgiyi göndermeden önce görür.
   Başarılı gönderimde bir bildirim numarası gösterilir. (ilk_promt.txt yalnızca sağ üstte "hata bildirimi"
   düğmesini ve bildirimin API'de depolanmasını söyler; açıklama alanı, otomatik bilgiler ve bildirim
   numarası öneridir, onayda teyit edilir — Open questions C-5.)
10. **Ajandan hata bildirimi:** Ajanda kurulum ya da çalışma sırasında bir hata olduğunda ajan kullanıcıya
    "Bir hata oldu" der ve "Bildir" seçeneği sunar (ilk_promt.txt: "hata oldu bildir demeli"). "Bildir"
    seçilirse bildirim kaynak (ajan), ajan versiyonu, işletim sistemi ve hata özetiyle API'ye gönderilir;
    seçilmezse hiçbir şey gönderilmez. (Eklenen alanlar ve log satırları öneridir — Open questions C-4.)
11. **Gönderilemeyen ajan bildirimi:** Ajan API'ye bağlı değilken (ya da henüz eşleştirilmemişken) seçilen
    bildirim ajanda bekletilir ve bağlantı kurulunca gönderilir; ajan yeniden başlasa da kaybolmaz.
    (Eşleşme öncesi bekletme Open questions C-1 önerisidir; kimliksiz bildirim yolu seçilirse bu madde değişir.)
12. **Saklama:** Hata bildirimleri API'de süresiz saklanır ve "Yeni" durumunda başlar (durum listesi 0010 ile
    ortak karar — self-critique 8); listeleme ve okuma 0010'dadır. Normal kullanıcı başka kullanıcıların
    bildirimlerini göremez.
13. **Gizli bilgi maskeleme (H-2):** Teknik loglarda, Günlük'te ve hata bildirimlerinde şifre hiçbir zaman
    yer almaz; API key yalnızca maskeli (`sk-…a3f`) görünür; erişim/yenileme token'ları, cihaz eşleştirme
    kodu ve eşleştirme onay sırrı (0002), video akış token'ı ve veritabanı şifresi düz metin olarak görünmez.
14. **Kötüye kullanım sınırı (öneri):** Bir kullanıcının belirli bir sürede gönderebileceği hata bildirimi
    sayısı sınırlıdır; sınır aşılınca anlaşılır bir mesaj gösterilir. security.md rate limiting'i yalnızca
    login ve üretim uçları için sayar; bu sınırın varlığı ve değeri Open questions C-7'de onaya bağlıdır.

## User experience
- Flow (Günlük): Kullanıcı herhangi bir sayfadayken alttaki şeritte son işlemi görür → bir işlem
  (ör. ajan bağlandı) olunca birkaç saniye içinde şerit güncellenir → bir hata olunca kayıt kırmızı,
  uyarı olunca sarı görünür → şeride tıklar (ya da odaklanıp Enter) → Günlük geçmişi açılır, bugünün
  kayıtları en yenisi üstte görünür → önceki güne geçer → kapatır (Kapat, Esc ya da dışarı tıklama),
  odak şeride döner.
- Flow (web hata bildirimi): Sağ üstte "Hata bildir" → modal açılır: açıklama alanı + otomatik eklenecek
  bilgiler (sayfa, versiyon, zaman) → "Gönder" → gönderiliyor → "Bildiriminiz alındı (No: …)" → modal
  kapanır, Günlük'e "Hata bildirimi gönderildi" düşer.
- Flow (ajan hata bildirimi): Ajanda kurulum ya da çalışma hatası → ajan "Bir hata oldu" penceresi/bildirimi
  gösterir: maskelenmiş hata özeti + "Bildir" / "Kapat" → "Bildir" → gönderildi (ya da "Bağlantı kurulunca
  gönderilecek") → bağlantı gelince gönderilir.
- States:
  - Günlük şeridi: yükleniyor · boş ("Henüz bir işlem yok") · normal kayıt · uyarı kaydı (sarı) · hata
    kaydı (kırmızı) · güncellenemiyor (son bilinen kayıt kalır + "Günlük güncellenemiyor" işareti, kendiliğinden
    düzelir).
  - Günlük geçmişi: yükleniyor · kayıtlı gün listesi · boş gün ("Bu günde işlem yok") · hata ("Günlük
    yüklenemedi" + tekrar dene).
  - Web hata bildir modalı: açık (boş) · doğrulama hatası (açıklama boş/kısa/uzun) · gönderiliyor (düğme
    pasif) · başarılı (numara) · sunucu hatası (yazılan metin korunur, tekrar dene) · sınır aşıldı.
  - Ajan hata uyarısı: hata gösteriliyor · gönderiliyor · gönderildi · bekletiliyor (bağlantı yok) ·
    gönderilemedi (bekletmeye alınır).
  - Yetkisiz: giriş yapılmamışsa Günlük ve "Hata bildir" görünmez/çalışmaz (default-deny).
- Design reference: none — the flow above is the design. `source/*.png` (commit `61cc824`) yalnızca sağ
  üstteki "hata bildir" yerini gösterir; Günlük şeridi ve modallar için görsel yok (ilk_promt.txt: "en
  altında ince bir kısım"). Renk: tema değişkenleri, turuncu/kahve, açık mod esas (K-7).
- Accessibility: Şerit ve "Hata bildir" klavyeyle erişilebilir (Tab, Enter/Space); modal/geçmiş paneli
  açılınca odak içine alınır, Esc kapatır, odak açan kontrole döner. Seviye yalnızca renkle değil ikon +
  metin etiketiyle de belirtilir (Hata/Uyarı). Yeni kayıtlar ekran okuyucuya `polite` olarak duyurulur.
  Kırmızı/sarı/normal metin ve arka plan her iki temada WCAG AA kontrastında.

## Constraints & out of scope
- Sensitive area: H-2 Gizli bilgiler (log, Günlük ve hata bildirimlerinde maskeleme). H-1 Kimlik ve yetki
  yalnızca mevcut oturum/yetki kurallarını kullanır; ajanın eşleşme öncesi bildirimi için kimliksiz bir uç
  açılırsa H-1'e dokunur (Open questions C-1).
- Canlı güncelleme Ajax polling ile (2–3 sn, architecture.md Kural 2); tarayıcıda SignalR yok, `fetch` yok (K-6).
- Arayüz metinleri Türkçe, resx'te. Zamanlar UTC saklanır, kullanıcıya yerel saatle gösterilir.
- Kullanıcı ya da LLM içeriği Günlük'te düz metin gösterilir (`Html.Raw` yasak).
- Out of scope: admin'in hata bildirimlerini listelemesi, durum değiştirmesi, aktivite/istatistik
  grafikleri (0010); ajan kurulum/güncelleme ekranı (0009 — bu spec yalnızca ajan hata uyarısı ve bildirim
  mekanizmasını sağlar); Günlük'ün ajan penceresinde gösterilmesi; ekran görüntüsü ekleme (C-5); kullanıcının
  kendi bildirimlerinin durumunu takip etmesi; harici log/izleme servisleri.
- Split: proposed — 0008a Günlük şeridi + geçmiş + aktivite kaydı (Req 1–6) · 0008b Teknik loglama + 7 gün
  saklama + maskeleme (Req 7, 8, 13) · 0008c Hata bildirimi web + ajan (Req 9–12, 14). Sıra: 0008b → 0008a →
  0008c (maskeleme ve log altyapısı diğerlerinin önkoşulu). Karar insanda (Open questions C-8).

## Acceptance criteria
- [ ] AC-1 — Giriş yapmış kullanıcının herhangi bir sayfasında Günlük şeridi en son kaydı (saat + metin) gösterir; yeni bir kayıt oluştuğunda en geç 5 sn içinde sayfa yenilenmeden şeritte görünür.
- [ ] AC-2 — Hata kaydı kırmızı + "Hata" etiketi/ikonu, uyarı kaydı sarı + "Uyarı" etiketi/ikonu, normal kayıt olağan renkte görünür; renkler yalnızca tema değişkenlerinden gelir ve her iki temada WCAG AA kontrastını sağlar.
- [ ] AC-3 — Başlangıç ve bitişi olan bir kayıt süreyi ve başlangıç/bitiş saatini gösterir (ör. "Video hazırlandı · 57 sn · 10:30:12–10:31:09"); süre, başlangıç ile bitiş arasındaki farkla tutarlıdır.
- [ ] AC-4 — Şerit ilk veri gelene kadar yükleniyor durumunu gösterir; hiç kaydı olmayan kullanıcı şeritte "Henüz bir işlem yok" görür; güncelleme isteği başarısız olunca son bilinen kayıt kalır ve "Günlük güncellenemiyor" işareti görünür, bağlantı dönünce işaret kendiliğinden kalkar.
- [ ] AC-5 — Şeride tıklama ya da odaklanıp Enter Günlük geçmişini açar; veri gelene kadar yükleniyor durumu görünür; kayıtlar güne göre ayrılmış, en yenisi üstte; önceki güne geçilebilir; kaydı olmayan gün "Bu günde işlem yok", yükleme hatası "Günlük yüklenemedi" + tekrar dene gösterir; Esc kapatır ve odak şeride döner.
- [ ] AC-6 — Kullanıcı A, kullanıcı B'nin Günlük kayıtlarını hiçbir yoldan (şerit, geçmiş, doğrudan istek) göremez; giriş yapılmamış istek reddedilir; giriş yapılmamış ziyaretçiye (ör. giriş sayfasında) Günlük şeridi ve "Hata bildir" düğmesi gösterilmez.
- [ ] AC-7 — 8 gün ve 400 gün önceki Günlük kayıtları geçmişte hâlâ görünür (sunucuda süresiz saklama); kayıtlar kullanıcının yerel saatine göre gece yarısında günlere ayrılır (23:59:59'daki kayıt o güne, 00:00:00'daki ertesi güne düşer; saat dilimi self-critique 1).
- [ ] AC-8 — Web, Api ve ajan Information/Warning/Error/Critical seviyelerinde teknik log yazar; her kayıtta zaman, seviye, kaynak uygulama ve varsa korelasyon numarası bulunur; ProblemDetails ile kullanıcıya dönen korelasyon numarası Api logunda bulunabilir; Günlük'e yazılan bir işlem teknik logda da bulunur.
- [ ] AC-9 — Yerel log dosyalarından 7 günlük olan korunur, 8 günlük olan kendiliğinden silinir (ajan, Web ve Api için ayrı ayrı).
- [ ] AC-10 — Şifre, API key (maskesiz), erişim/yenileme token'ı, cihaz eşleştirme kodu, eşleştirme onay sırrı, video akış token'ı ve veritabanı şifresi; bunları içeren bir işlem/hata oluşturulduğunda teknik loglarda, Günlük'te ve hata bildiriminde düz metin olarak görünmez; API key `sk-…a3f` biçiminde görünür. Maskeleme kasıtlı olarak kaldırıldığında test kırmızıya döner (H-2).
- [ ] AC-11 — "Hata bildir" modalı açılır (klavyeyle de), otomatik eklenecek bilgileri (sayfa, versiyon, zaman) gösterir; İptal/Esc hiçbir kayıt oluşturmadan kapatır ve odak düğmeye döner; geçerli açıklamayla "Gönder" bildirim numarası gösterir; kayıt gönderen kullanıcı, kaynak "web" ve "Yeni" durumu ile saklanır; Günlük'e "Hata bildirimi gönderildi" düşer.
- [ ] AC-12 — Boş, alt sınırdan kısa ya da üst sınırdan uzun açıklama Türkçe doğrulama mesajıyla reddedilir ve kayıt oluşmaz; sınır değerlerinin tam kendisi kabul edilir; sunucu hatasında yazılan metin korunur; gönderiliyorken düğme pasiftir ve çift tıklama tek kayıt oluşturur.
- [ ] AC-13 — Ajanda bir kurulum ya da çalışma hatası olduğunda ajan maskelenmiş hata özetiyle "Bir hata oldu" uyarısı ve "Bildir"/"Kapat" seçenekleri gösterir; "Bildir" kaynak "ajan", ajan versiyonu, işletim sistemi ve hata özetiyle bir kayıt oluşturur; "Kapat" hiçbir kayıt oluşturmaz.
- [ ] AC-14 — Ajan API'ye bağlı değilken seçilen bildirim "Bağlantı kurulunca gönderilecek" ile bekletilir; ajan yeniden başlatılsa da kaybolmaz; bağlantı ve eşleşme kurulunca tam bir kez gönderilir.
- [ ] AC-15 — Çıkış yapmış ya da banlanmış kullanıcının (web ya da ajan) bildirimi reddedilir; normal kullanıcı başka kullanıcıların hata bildirimlerini hiçbir yoldan (doğrudan istek dahil) okuyamaz. (Admin'in `admin.panel` ile okuması 0010'un kriteridir.)
- [ ] AC-16 — Sınırı aşan sayıda hata bildirimi gönderen kullanıcı, sınır penceresi dolana kadar "Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin" mesajı alır ve kayıt oluşmaz; sınırın tam kendisi kabul edilir.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**(a) Clarify soruları (etkiye göre sıralı)**
- **C-1 — Ajanın eşleşme öncesi hataları:** Kurulum hataları çoğunlukla ajan eşleştirilmeden önce olur;
  o anda ajanın kimliği yok. Kimliksiz bir bildirim ucu mu açılsın, yoksa bekletilsin mi?
  Öneri: kimliksiz uç açılmasın; bildirim ajanda (maskeli) bekletilir, eşleşme kurulunca gönderilir;
  ayrıca kullanıcıya "Hata raporunu dosyaya kaydet" seçeneği sunulur. Gerekçe: default-deny kuralı
  (security.md) korunur, H-1'e yeni bir yüzey açılmaz; kurulum hiç bitmezse dosya yine elle iletilebilir.
- **C-2 — Günlük geçmişi olsun mu, biçimi ne?** ilk_promt.txt yalnızca "son yapılan işlem" ve "günlük bazda
  ayrılacak" diyor; geçmiş görünümü 0001'in önerisi. Geçmiş olsun mu, nerede ve nasıl açılsın?
  Öneri: şeride tıklayınca sayfanın üstünde modal açılır; gün başlıklı liste, seviye filtresi
  (Tümü/Uyarı/Hata), günde ilk 50 kayıt + "Daha fazla", önceki/sonraki gün düğmeleri. Gerekçe: ilk_promt.txt'te
  ayrıntılar için hep "ekranın üstünde ayrı bir modal" tercih edilmiş; filtre hataları hızla bulmayı sağlar.
- **C-3 — Günlük'e neler yazılır, profil filtresi var mı?** Öneri: her özellik spec'i kendi Günlük
  kayıtlarını tanımlar; bu spec ajan bağlandı/koptu ve hata bildirimi gönderildi kayıtlarıyla gelir
  (Req 6); giriş/çıkış kayıtları 0002'ye eklenmeli (0002 şu an tanımlamıyor). Şerit seçili profile göre filtrelenmez, profile ait kayıtlarda profil adı etiket olarak görünür.
  Gerekçe: üretim sırası farklı profillerden işleri karıştırabilir; filtre kullanıcıdan hata kaçırır.
- **C-4 — Ajan bildiriminin içeriği:** Ajan hata özetinin yanında yerel logdan ne göndersin?
  Öneri: son 15 dakikanın Warning+ log satırları (en fazla 200 satır), maskelenmiş; Windows kullanıcı adı
  içeren yollar `%USERPROFILE%` olarak kısaltılır; kullanıcı gönderilecek metni "Ayrıntıları göster" ile
  görebilir. Gerekçe: admin sorunu çözebilecek bağlamı alır, kişisel veri ve gizli bilgi sızmaz.
- **C-5 — Web bildiriminin içeriği:** Ekran görüntüsü, son Günlük kayıtları, son hata korelasyon numarası
  eklensin mi? Öneri: ekran görüntüsü yok (v1 dışı); son 20 Günlük kaydı ve varsa son hatanın korelasyon
  numarası otomatik eklenir; ayrıca ProblemDetails hata mesajında "Bildir" bağlantısı modalı korelasyon
  numarasıyla dolu açar. Gerekçe: korelasyon numarası Api loguyla birebir eşleşir, görüntü yükleme yükü ve
  istem dışı kişisel veri riski olmaz.
- **C-6 — Teknik logların veritabanında tutulması:** Serilog DB sink'e hangi seviyeler, ne kadar süre?
  Ajan teknik logları API'ye akmalı mı? Öneri: Api ve Web'in Warning+ logları DB'de 30 gün; ajan yalnızca
  Error/Critical özetlerini API'ye gönderir, tam log ajanda 7 gün kalır ve yalnızca hata bildirimiyle gider.
  Gerekçe: Pi'de disk/RAM tasarrufu; admin son bir ayın sorunlarını görebilir; ajan logunun tamamını
  taşımak gereksiz trafik ve gizlilik riski.
- **C-7 — Sınırlar:** Açıklama uzunluğu ve bildirim sıklığı sınırı. Öneri: açıklama 10–4000 karakter;
  kullanıcı başına saatte 10 bildirim (ajan + web toplam); ajan aynı hatayı aynı oturumda yalnızca bir kez
  sorar. Gerekçe: anlamlı bir açıklama için alt sınır, döngüye giren bir hatanın bildirim yağmuru
  oluşturmasını engelleyen üst sınır.
- **C-8 — Split:** Spec 3 kullanıcı yeteneği ve 16 kriter içeriyor. Öneri: 0008b (loglama + maskeleme) →
  0008a (Günlük) → 0008c (hata bildirimi) sırasıyla bölünsün. Gerekçe: her biri ayrı review/QA turunda
  daha güvenli; maskeleme (H-2) önce gelirse diğer ikisi onun üzerine kurulur.

**(b) Self-critique boşlukları (önerilerimle, onayda katlanır)**
1. Saat ve süre biçimi tanımsız. Öneri: yerel saat Europe/Istanbul, 24 saat `HH:mm:ss`; süre 60 sn altı
   "57 sn", 1 saate kadar "30 dk 12 sn", 1 saat üstü "1 sa 5 dk". İlk_promt.txt'teki örnek (57 sn ama 10:30–11:00)
   kendi içinde tutarsız; süre her zaman başlangıç–bitiş farkından hesaplansın.
2. Teknik seviye ↔ Günlük rengi eşlemesi. Öneri: Information → normal, Warning → sarı, Error ve Critical →
   kırmızı (Critical ayrıca "Kritik" etiketi). Günlük'e teknik log değil, kullanıcıya anlamlı işlemler yazılır.
3. Maskeleme listesi serbest metni kapsıyor mu? Kullanıcı açıklamaya API key yapıştırabilir. Öneri: bilinen
   key biçimleri (`sk-`, `sk-ant-`, `sk-or-` vb.) ve JWT biçimi serbest metinde de maskelenir; liste plan'da
   test tablosu olarak sabitlenir.
4. Ajan uyarısı nerede görünür (ajan tray'de ve penceresiz olabilir)? Öneri: küçük bir Avalonia penceresi
   öne gelir + Windows bildirimi; tray ikonunda bekleyen bildirim işareti.
5. AC-1 "5 sn" ölçülebilirliği: polling 3 sn, sınır 5 sn. Öneri: kalsın; şerit sekme arka plandayken
   yoklamayı yavaşlatabilir (ör. 15 sn), sekmeye dönünce hemen yeniler.
6. Ekran okuyucu duyurusu: hızlı akan üretim kayıtları sürekli okunursa rahatsız eder. Öneri: yalnızca Uyarı
   ve Hata kayıtları `polite` duyurulsun, Bilgi kayıtları duyurulmasın.
7. Yerel log klasörü ve adı: ajanın kendi, çakışmasız klasörü (0009 kuralı: başka "Konsept" programlarıyla
   çakışma yok). Öneri: ajanın kurulum klasörü altında `logs`; Pi'de `/opt/anewconcept` altında; ad plan'da.
8. Hata bildirimi durumları (Yeni/İncelendi/Çözüldü) ve kullanıcının kendi bildirimlerini görmesi
   tanımsız. Öneri: 0008 yalnızca "Yeni" ile saklar; durum yönetimi 0010; kullanıcı tarafı takibi v1 dışı.
9. Giriş ekranında (oturum yokken) web hata bildirimi yapılabilir mi? Öneri: hayır (default-deny);
   giriş sorunları için ileride ayrı karar.
10. Design reference yok: Günlük şeridi ve modallar için bir eskiz verilmek istenirse `source/` altına
    tarihli görsel eklenip commit'i bu spec'e yazılsın; yoksa yukarıdaki akış tasarım sayılır.
11. Bekleyen ajan bildirimi hangi hesaba gider? Ajan A hesabıyla eşleşmişken bekletilen bildirim, çıkış
    sonrası ajan B hesabıyla eşleşirse B adına gitmemeli. Öneri: bekleyen bildirim oluştuğu andaki hesaba
    bağlanır; farklı bir hesapla eşleşilirse gönderilmez ve kullanıcıya bilgi verilir; eşleşme öncesi oluşan
    bildirim ilk eşleşen hesapla gider. Gerekçe: BR-1 (her kayıt doğru kullanıcıya ait).
12. Bekleyen bildirim kuyruğunun sınırı yok. Öneri: ajanda en fazla 20 bekleyen bildirim (maskeli); dolarsa en
    eskisi düşer ve teknik loga Warning yazılır. Gerekçe: döngüye giren bir hata diski doldurmasın.
13. Geçmişte ileri tarih ve en eski gün tanımsız. Öneri: bugünden sonraki güne geçiş pasif; geçersiz/ileri
    tarih isteği bugüne döner; en eski gün hesabın açıldığı gün.
14. Web hata bildir modalı İptal/Esc ile kapanınca yazılan metin ne olur? Öneri: atılır; yalnızca "Gönder"de
    sunucu hatası olursa korunur (AC-12). Gerekçe: basit ve öngörülebilir.

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
