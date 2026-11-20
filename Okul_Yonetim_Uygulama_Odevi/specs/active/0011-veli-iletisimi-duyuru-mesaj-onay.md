# Spec 0011 — Veli iletişimi: duyuru, okundu, onay formu, birebir mesaj ve sessiz mod

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0011-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Okullar bugün veliye WhatsApp grupları, kâğıt izin formları ve öğretmenin kişisel telefonuyla ulaşıyor; kurucu kimin
neyi okuduğunu bilmiyor, öğretmen gece yarısı gelen mesajlarla bölünüyor. Bu spec yol haritasının 5. satırının çekirdeğini
getirir: kurucu ve öğretmen **duyuru** yayınlar (okul, şube, sınıf ya da tek öğrenci kapsamında) ve kimin okuduğunu görür;
duyuru gerekiyorsa **onay formu** olur (gezi izni, etkinlik katılımı) ve veli her çocuğu için onaylar ya da reddeder;
öğretmen ile veli, çocuk bağlamında **birebir mesajlaşır** ve öğretmen **mesai saatlerini** tanımlayarak mesai dışında
bildirimle bölünmez. Başarı: kurucu web'den gezi izni duyurusu yayınlar, veliler mobilde okuyup cevaplar, kurucu
"18/22 veli okudu, 15 onay, 2 ret, 5 cevapsız" görür; veli akşam öğretmene yazar, öğretmene bildirim gitmez, mesaj sabah
gelen kutusunda durur. Bilinçli olarak yapılmayanlar: fotoğraf ve dosya paylaşımı (dosya deposu ve öğrenci bazlı fotoğraf
rızası ister — ayrı spec), gerçek push/SMS gönderimi (B-6, B-2), duyuru/mesaj düzenleme ve silme, veliler arası grup sohbeti
(kapsam dışı), sesli nottan mesaj (yol haritası 6).

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Bu spec'e ne girer?** Duyuru + okundu, onay formu, birebir mesaj + sessiz mod bu spec'te; **fotoğraf/dosya
  paylaşımı ayrı spec** (dosya deposu portu `IFileStorage`, kısa ömürlü imzalı bağlantı, öğrenci bazlı fotoğraf rızası —
  S2/S6; 13 kriter sınırını tek başına aşar). Neden: geri kalanı yalnızca metin ve aynı alıcı/ilişki kurallarını paylaşır.
  - Dokümanda istenen: "Veli iletişimi — Duyuru (okul, sınıf, öğrenci bazında), okundu bilgisi · Öğretmen-veli birebir
    mesajlaşma, mesai saati dışı sessiz mod · Onay formları (gezi izni, etkinlik katılımı) · Fotoğraf ve dosya paylaşımı";
    KVKK: "Fotoğraf paylaşımında öğrenci bazlı izin".
- **C-2 — Duyuruyu kim, hangi kapsamda yayınlar?** Kurucu: okulun tamamı, bir şube, bir sınıf ya da tek öğrenci.
  Öğretmen: yalnızca kendi sınıfları ya da kendi sınıfındaki tek öğrenci. Veli ve muhasebe yayınlayamaz. Yayınlanan duyuru
  düzenlenmez/silinmez (yanlışsa yeni duyuru). Neden: en sade yetki modeli; düzenleme okundu bilgisini anlamsızlaştırır.
  - Dokümanda istenen: "Duyuru (okul, sınıf, öğrenci bazında), okundu bilgisi"; domain: "Kurum/şube/sınıf/öğrenci kapsamlı
    yayın"; güvenlik: "öğretmen yalnızca kendi sınıflarını görür".
- **C-3 — Alıcılar ve okundu bilgisi nasıl sayılır?** Alıcı veliler **yayın anında** belirlenir (kapsamdaki öğrencilerin
  etkin velileri); sonradan sınıfa giren öğrencinin velisi o duyuruyu görmez. Okundu = velinin duyuru ayrıntısını ilk
  açışı; her veli ayrı sayılır. Okundu sayısını ve okumayanların listesini yayınlayan ve kurucu görür. Neden: sabit payda
  ("18/22") ve ileride yönetici panelinin "okunmayan duyurular" özeti için tutarlı sayı.
  - Dokümanda istenen: "okundu bilgisi"; Yönetici paneli: "Günlük özet: … okunmayan duyurular".
- **C-4 — Onay formu nasıl çalışır?** Onay formu, duyurunun "onay iste" seçilmiş hâlidir: tek soru, son tarih. Cevap
  **öğrenci başınadır** (Onaylıyorum / Onaylamıyorum); öğrencinin herhangi bir velisi cevaplar, son tarihe kadar
  değiştirebilir, son cevap geçerlidir ve kimin ne zaman verdiği görünür. KVKK rızasından (`ConsentRecord`) ayrıdır.
  Neden: gezi izni çocuk içindir; iki velinin çelişen cevabı yerine tek geçerli cevap sade ve denetlenebilir.
  - Dokümanda istenen: "Onay formları (gezi izni, etkinlik katılımı)"; domain: "Onay formu (`ConsentForm`) … KVKK
    rızasından (`ConsentRecord`) farklı".
- **C-5 — Kim kiminle mesajlaşır?** Yalnızca öğretmen ↔ veli, bir öğrenci bağlamında: veli çocuğunun sınıfının
  öğretmen(ler)ine, öğretmen kendi sınıfındaki öğrencinin velisine yazar. Konuşmayı yalnızca iki taraf görür (kurucu
  dahil başka kimse görmez). İlişki biterse (öğrenci sınıftan ayrıldı, öğretmen sınıftan alındı, pasifleşti) eski
  konuşma okunur ama yeni mesaj gönderilemez. Kurucu/muhasebe mesajı ve veliler arası mesaj yok. Neden: kapsamın
  istediği tam bu; veri en az kişiye görünür.
  - Dokümanda istenen: "Öğretmen-veli birebir mesajlaşma"; Kapsam dışı: "Sosyal ağ / veliler arası grup sohbeti".
- **C-6 — Sessiz mod nasıl tanımlanır?** Öğretmen tek bir mesai aralığı (başlangıç–bitiş saati, Türkiye saati) ve iş
  günlerini seçer. Tanımlamadıysa sessiz mod kapalıdır (her mesajda bildirim). Mesai dışında gelen veli mesajı iletilir
  ve okunmamış sayısına eklenir ama öğretmene bildirim gönderilmez; mesai başlayınca birikmiş bildirim de gönderilmez
  (öğretmen uygulamayı açınca görür). Veli gönderirken "Öğretmen şu an mesai saatleri dışında; mesajınız iletildi."
  bilgisini görür. Sessiz mod yalnızca öğretmeni korur; öğretmenden veliye giden mesaj her zaman bildirim üretir.
  Neden: BR-8'in en sade karşılığı; gece biriken bildirimin sabah topluca düşmesi de bölücüdür.
  - Dokümanda istenen: "mesai saati dışı sessiz mod"; BR-8: "öğretmenin tanımladığı saatler dışında veli mesajı
    iletilir ama öğretmene bildirim gitmez".
- **C-7 — Bildirim ve yenileme nasıl olur (push yok)?** Yalnızca uygulama içi: duyuru listesi, okunmamış işaretleri ve
  sayılar. Her bildirim (duyuru, onay formu, öğretmen mesajı, diğer modüllerin veli bildirimi) iletişim modülünün tek
  **bildirim kapısından** geçer; kapı MVP'de yalnızca kimlikleri loglayan sahte göndericiyi çağırır (gerçek push B-6,
  SMS B-2). Ekranlar açılınca ve aşağı çekince yenilenir; anlık (canlı) güncelleme yok. Neden: sağlayıcı hesabı yok;
  kapı hazır olunca push yalnızca gönderici değiştirerek gelir.
  - Dokümanda istenen: "Push bildirim, kurum logosuyla markalı açılış"; SMS: "Uygulamayı kurmayan veliye ulaşma";
    mimari: "Communication … veliye giden her şeyin tek çıkışı".
- **C-8 — Hangi ekran hangi kanalda?** Duyuru yazma, duyuru listesi ve sonuçlar (okundu, onay) **web**'de kurucu ve
  öğretmen için. Veli tarafı (duyurular, onay cevabı, mesajlar) ve öğretmenin mesajları + mesai saatleri **mobil**de.
  Öğretmenin web'den mesajlaşması ve mobilden duyuru yazması sonraki adım. Neden: veli yalnızca mobil (kapsam), öğretmenin
  mesajı telefonda doğal; duyuru ve sonuç tablosu geniş ekranda okunur; çift ekran yazmamak boyutu yarıya indirir.
  - Dokümanda istenen: rol tablosu — "Öğretmen: Yoklama, not, ödev, veli mesajı — Mobil + Web"; "Veli: Takip, bildirim,
    ödeme, randevu — Mobil"; "Kurucu / Yönetici — Web".

## Requirements
1. **Duyuru yayınlama:** Kurucu ve öğretmen başlık (zorunlu, en fazla 150 karakter) ve metin (zorunlu, en fazla 4000
   karakter) ile duyuru yayınlar; kapsam seçer: okul (yalnız kurucu), şube (yalnız kurucu), sınıf, öğrenci. Öğretmen
   yalnızca kendi sınıflarını ve o sınıflardaki öğrencileri seçebilir. Alıcı veliler yayın anında kapsamdaki öğrencilerin
   etkin velilerinden belirlenir; kapsamda hiç veli yoksa "Bu kapsamda duyuru alacak veli yok." ile reddedilir. Duyuru
   yayınlandıktan sonra değiştirilemez ve silinemez.
2. **Veli duyuru akışı:** Veli, seçili okulda yalnızca alıcısı olduğu duyuruları görür (çocukları farklı şubelerde olsa
   bile, BR-2): yeniden eskiye, sayfa başına 20; her satırda başlık, yayınlayan, tarih, ilgili çocuk adı (öğrenci/sınıf
   kapsamında) ve okunmadıysa "yeni" işareti. Boşken "Henüz duyuru yok".
3. **Okundu bilgisi:** Veli duyuru ayrıntısını ilk açtığında okundu kaydedilir (tekrar açmak değiştirmez). Yayınlayan ve
   kurucu, duyuru ayrıntısında "X / Y veli okudu" ve okumayan velilerin listesini (veli ad-soyad + çocuğun adı) görür;
   başka öğretmen göremez.
4. **Onay formu:** Yayınlarken "Onay iste" seçilirse soru (zorunlu, en fazla 300) ve son tarih (bugünden sonra, en fazla
   180 gün) girilir. Veli ayrıntıda, duyurunun kapsadığı her çocuğu için "Onaylıyorum" / "Onaylamıyorum" seçer; son
   tarihe (o gün 23:59, Türkiye saati) kadar değiştirebilir; son cevap geçerlidir. Öğrencinin öteki velisi aynı cevabı
   (kim, ne zaman) görür ve değiştirebilir. Süre dolunca cevap "Onay süresi doldu." ile reddedilir. Yayınlayan ve kurucu
   öğrenci bazında onay / ret / cevapsız sayısını ve listesini görür.
5. **Birebir mesaj:** Veli çocuğunun sınıfının öğretmenlerinden birine, öğretmen kendi sınıfındaki öğrencinin velisine,
   o öğrenci bağlamında yazar; aynı öğretmen–veli–öğrenci üçlüsü için tek konuşma vardır. Mesaj boş olamaz, en fazla 2000
   karakter. Konuşma listesi son mesaja göre sıralanır, karşı taraf, öğrenci adı ve okunmamış sayısıyla; konuşma açılınca
   okunmamışlar okunmuş olur; konuşmada son 50 mesaj, "Daha eski" ile önceki 50. İlişki biterse konuşma okunur, yeni mesaj
   "Bu konuşmaya artık mesaj gönderilemez." ile reddedilir. Konuşmayı yalnızca iki taraf görür.
6. **Sessiz mod:** Öğretmen mobilde mesai başlangıç ve bitiş saatini (başlangıç bitişten önce, 15 dakikalık adımlarla) ve iş
   günlerini (en az bir gün) kaydeder ya da sessiz modu kapatır. Bir veli mesajı, Türkiye saatiyle seçili bir günde ve
   [başlangıç, bitiş) aralığındaysa mesai içidir. Mesai içinde öğretmene bildirim gönderilir; mesai dışında mesaj iletilir,
   okunmamış sayısı artar, bildirim gönderilmez ve veli "Öğretmen şu an mesai saatleri dışında; mesajınız iletildi."
   bilgisini görür. Tanımsızsa her mesajda bildirim.
7. **Bildirim kapısı (tek çıkış):** Duyuru ve onay formu yayınlanınca alıcı velilere, öğretmen mesajında veliye, mesai
   içi veli mesajında öğretmene bildirim kapısından bildirim gider. Diğer modüller (ör. yoklama, spec 0010) veliye
   bildirimi yalnızca bu kapıdan yollar; bu bildirimler velinin duyuru akışında o çocuğa özel "Okul bildirimi" olarak
   görünür (yayınlayan: okul). Kapının göndericisi hata verirse asıl işlem başarılı kalır, hata Warning olarak loglanır.
8. **Gizlilik ve izolasyon:** Başka okulun, yetkisiz şubenin, ilişkisi olmayan öğrencinin duyurusu, konuşması ya da onay
   formu "bulunamadı" (404) döner. Duyuru, onay ve mesaj metinleri ile kişi adları log satırlarına ve bildirim log'una
   yazılmaz (yalnızca kimlikler).
9. **Audit:** Duyuru yayınlama, onay cevabı (verme/değiştirme) ve mesai saatleri değişikliği audit'e yazılır (kim, ne,
   hangi kayıt, ne zaman, IP). Tek tek mesajlar audit'e değil, Information log'una (yalnızca kimlikler) yazılır.

## User experience
- **Flow — web (kurucu, öğretmen):** Hoş geldin → "İletişim" bağlantısı (yalnızca kurucu ve öğretmende) → `#/iletisim`:
  duyuru listesi (yayınladıklarım; kurucuda okulun bütün duyuruları) → "Yeni duyuru" formu (kapsam: okul/şube/sınıf/
  öğrenci — öğretmende yalnızca sınıf/öğrenci; başlık, metin; "Onay iste" → soru + son tarih) → yayınla → ayrıntı:
  okundu "X / Y", okumayanlar listesi, onay formunda onay/ret/cevapsız sayıları ve öğrenci listesi.
- **Flow — mobil (veli):** Hoş geldin → "Duyurular" (okunmamış sayısıyla) → liste → ayrıntı (okundu olur; onay formunda
  her çocuk için iki düğme, mevcut cevap ve kim verdiği) · "Mesajlar" → konuşma listesi → konuşma → yaz/gönder; "Yeni
  mesaj" → çocuk seç → öğretmen seç → konuşma.
- **Flow — mobil (öğretmen):** Hoş geldin → "Mesajlar" (okunmamış sayısıyla) → konuşma listesi → konuşma; "Yeni mesaj" →
  sınıf → öğrenci → veli → konuşma · "Mesai saatlerim" → başlangıç, bitiş, günler → kaydet / sessiz modu kapat.
- **States:** her liste: yükleniyor / boş ("Henüz duyuru yok", "Henüz mesaj yok") / dolu / hata + "Tekrar dene"; formlar:
  alan hataları (istek gönderilmez) / gönderiliyor (düğme kilitli) / sunucu hatası (ör. "Bu kapsamda duyuru alacak veli
  yok.", "Onay süresi doldu.", "Bu konuşmaya artık mesaj gönderilemez."); başarı (yayınlandı → ayrıntı; cevap kaydedildi;
  mesaj gönderildi, mesai dışında bilgi notu); onay formu süresi dolmuş (düğmeler kapalı, "Süre doldu"); yetkisiz kişi
  `#/iletisim`'de "Bu sayfaya erişiminiz yok."; mobilde rolü olmayan menü görünmez.
- **Design reference:** none — the flow above is the design (spec 0005 yönetim sayfası ve 0006 mobil hesap ekranı
  kalıpları, ortak tema).
- **Accessibility:** web akışı yalnızca klavyeyle; form hataları alanla ilişkili ve duyurulur; mobilde düğme ve
  "yeni"/okunmamış işaretleri için erişilebilirlik etiketi ("okunmadı", "3 okunmamış mesaj"); renk tek başına anlam
  taşımaz; iki temada kontrast spec 0002 eşiklerinde; web 320 px'te yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S5 (veliye giden iletişim — duyuru, mesaj, otomatik bildirim), S1 (okul/şube izolasyonu, ilişki
  kontrolü), S2 (öğrenci-veli eşleşmesi, ad-soyad, mesaj içeriği), S3 (veli-öğrenci bağına dayalı yetki), S6 (audit).
- **Bağımlılık:** spec 0008 (sınıf, şube-sınıf, dönem), 0009 (öğrenci, birden çok veli; veli = Guardian rolündeki kişi)
  ve 0010 (yoklama; öğretmen–sınıf ilişkisi ve devamsızlık bildirimi) önce biter. 0010 en küçük bir uygulama içi bildirim
  getirdiyse bu spec onu bildirim kapısına bağlar.
- **Kapsam dışı / sonra:**
  - Fotoğraf ve dosya paylaşımı (duyuruya ek, mesajda fotoğraf) → ayrı spec "Veli iletişimi: fotoğraf ve dosya" —
    `IFileStorage` portu, herkese açık adres olmadan kısa ömürlü imzalı bağlantı, boyut/tip sınırı, öğrenci bazlı
    fotoğraf rızası (KVKK, `docs/security.md` KVKK 2) gerektirir.
  - Gerçek push (B-6) ve SMS (B-2); okunmayanlara hatırlatma; anlık (canlı) güncelleme.
  - Duyuru/mesaj düzenleme, silme, geri çekme; mesaj arama; okundu ("görüldü") işareti mesaj başına.
  - Öğretmenin web'den mesajlaşması ve mobilden duyuru yazması; kurucu/muhasebe ile veli mesajlaşması; veliler arası
    mesaj (kapsam dışı).
  - Duyuru saklama süresi/anonimleştirme (Compliance spec'i), yönetici panelindeki "okunmayan duyurular" özeti (yol
    haritası 11), sesli nottan mesaj (yol haritası 6 — bu modülün mesaj gönderme yolunu kullanacak).
- Split: proposed 0011a "Duyuru, okundu ve onay formu" ve 0011b "Birebir mesaj ve sessiz mod"; kept as one —
  tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20, yol haritası satırı tek spec, ortak alıcı/ilişki
  kuralları ve bildirim kapısı; fotoğraf/dosya ayrıldığı için 13 kriterde kalıyor.

## Acceptance criteria
- [ ] AC-1 — Duyuru yayınlama (kurucu): okul, şube, sınıf ve öğrenci kapsamlarında duyuru yayınlanır; alıcılar yayın anındaki kapsamın etkin velileridir (sonradan sınıfa eklenen öğrencinin velisi görmez); kapsamda veli yoksa "Bu kapsamda duyuru alacak veli yok." ile reddedilir; boş başlık/metin, 150/4000 sınırını aşan alan alan hatası verir ve hiçbir şey oluşmaz (sınırdaki 150/4000 kabul edilir).
- [ ] AC-2 — Duyuru yetkisi: öğretmen yalnızca kendi sınıflarına ve o sınıflardaki öğrencilere yayınlar; okul/şube kapsamı 403, başka sınıf ya da başka sınıfın öğrencisi 404 alır ve hiçbir şey oluşmaz; veli ve muhasebe 403 alır; girişsiz istek 401.
- [ ] AC-3 — Veli duyuru akışı: veli seçili okulda yalnızca alıcısı olduğu duyuruları yeniden eskiye, sayfa başına 20 görür (farklı şubelerdeki çocuklarının duyuruları dahil); başka velinin çocuğuna özel duyuru ve başka okulun duyurusu listede yok, doğrudan istendiğinde 404; okunmamışlar işaretli; boşken "Henüz duyuru yok".
- [ ] AC-4 — Okundu bilgisi: veli ayrıntıyı açınca okundu kaydedilir (ikinci açış ilk okuma zamanını değiştirmez); yayınlayan ve kurucu "X / Y veli okudu" ile okumayanların listesini (veli ad-soyad + çocuk adı) görür; yayınlayan olmayan öğretmen ve veli bu bilgiyi istediğinde 403/404 alır.
- [ ] AC-5 — Onay formu yayınlama ve sonuçlar: "Onay iste" ile soru (zorunlu, en fazla 300) ve son tarih (yarından en geç 180 gün sonrasına) girilir; geçmiş/bugün/181. gün tarihi alan hatası verir; yayınlayan ve kurucu öğrenci bazında onay / ret / cevapsız sayılarını ve listesini görür.
- [ ] AC-6 — Onay cevabı: veli kapsamdaki her çocuğu için Onaylıyorum/Onaylamıyorum verir ve son tarihe kadar değiştirir (son cevap geçerli; kim ve ne zaman kayıtlı); öğrencinin öteki velisi aynı cevabı görür ve değiştirebilir; son tarih günü 23:59'dan (Türkiye saati) sonra "Onay süresi doldu." ile reddedilir; kapsam dışındaki ya da kendi çocuğu olmayan öğrenci için cevap 404; onay formu olmayan duyuruya cevap reddedilir.
- [ ] AC-7 — Mesaj yetkisi: veli çocuğunun sınıfının öğretmenine, öğretmen kendi sınıfındaki öğrencinin velisine o öğrenci bağlamında yazar; aynı üçlü için ikinci konuşma açılmaz (var olan döner); ilişkisiz öğretmen–veli–öğrenci, veli–veli, kurucu/muhasebe başlatması reddedilir (403/404); konuşmayı üçüncü kişi (kurucu dahil) okuyamaz (404); ilişki bitince eski mesajlar okunur, yeni mesaj "Bu konuşmaya artık mesaj gönderilemez." ile reddedilir.
- [ ] AC-8 — Mesaj akışı: boş ya da 2000 karakteri aşan mesaj reddedilir (2000 kabul); konuşma listesi son mesaja göre, karşı taraf, öğrenci adı ve okunmamış sayısıyla; konuşma açılınca okunmamış sayısı sıfırlanır; son 50 mesaj ve "Daha eski" ile önceki 50 gelir; konuşma yokken "Henüz mesaj yok".
- [ ] AC-9 — Sessiz mod: öğretmen mesai saatlerini kaydeder/kapatır (başlangıç ≥ bitiş, 15 dk adımı dışı ya da gün seçilmemesi alan hatası); mesai içinde (Türkiye saati, seçili gün, başlangıç dahil – bitiş hariç) veli mesajı öğretmene bildirim üretir; mesai dışında mesaj iletilir, öğretmenin okunmamış sayısı artar, bildirim üretilmez ve veli "Öğretmen şu an mesai saatleri dışında; mesajınız iletildi." bilgisini alır; sessiz mod tanımsızken her mesajda bildirim; öğretmenden veliye mesaj saat ne olursa olsun bildirim üretir.
- [ ] AC-10 — Bildirim kapısı: duyuru/onay formu yayınlanınca her alıcı veliye, öğretmen mesajında veliye birer bildirim kapıdan geçer; başka modülün (yoklama) kapıya verdiği öğrenci bildirimi o öğrencinin velilerinin duyuru akışında "Okul bildirimi" olarak görünür ve başka veliye görünmez; gönderici hata verirse duyuru/mesaj yine kaydedilir ve Warning log yazılır.
- [ ] AC-11 — Gizlilik, izolasyon ve audit: başka okulun ya da yetkisiz şubenin duyuru, konuşma ve onay kayıtları 404; log ve bildirim log satırlarında duyuru/onay/mesaj metni ve kişi adı geçmez (yalnızca kimlikler); duyuru yayınlama, onay cevabı ve mesai saatleri değişikliği audit kaydı oluşturur (kim, ne, hangi kayıt, ne zaman, IP).
- [ ] AC-12 — Web ekranları (kurucu, öğretmen; iki tema): "İletişim" bağlantısı yalnızca kurucu ve öğretmende; duyuru listesi (yükleniyor / boş / dolu / hata + tekrar dene), yeni duyuru formu (öğretmende yalnızca sınıf/öğrenci kapsamı; onay iste alanları; alan hataları — istek gitmez; gönderiliyor — düğme kilitli; sunucu hatası duyurulur), ayrıntı (okundu X / Y, okumayanlar, onay sonuçları); veli/muhasebe `#/iletisim`'de "Bu sayfaya erişiminiz yok."; akış yalnızca klavyeyle; 320 px'te yatay kayma yok.
- [ ] AC-13 — Mobil ekranları (veli, öğretmen; iki tema): veli — duyurular (yükleniyor / boş / dolu / hata + tekrar dene, "yeni" işareti ve okunmamış sayısı), ayrıntı + onay düğmeleri (mevcut cevap, süresi dolmuş durumda kapalı), mesajlar listesi ve konuşma (gönderiliyor, hata, mesai dışı bilgi notu, gönderilemez durumu); öğretmen — mesajlar + "Mesai saatlerim" (alan hataları, kaydedildi); menüler yalnızca ilgili rolde; düğme ve işaretlerde erişilebilirlik etiketleri.

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Okul/şube kapsamında hangi öğrenciler?" → seçili dönemde o okulda/şubede kayıtlı etkin öğrenciler (0008/0009 tanımı).
- "İki velisi olan öğrenci için okundu payda kaçtır?" → veli başına sayılır (Y = alıcı veli sayısı); onay öğrenci başına.
- "Aynı veli iki çocuğu aynı sınıftaysa iki bildirim mi?" → veli başına tek alıcı kaydı, tek bildirim; ayrıntıda iki çocuk.
- "Öğretmen iki sınıfa birden yayın yapabilir mi?" → hayır, tek kapsam; iki duyuru yayınlar (sade).
- "Son tarih saat dilimi?" → Türkiye saati, son gün 23:59 dahil.
- "Mesai saati gece yarısını geçebilir mi (22:00–02:00)?" → hayır, başlangıç < bitiş aynı gün (sade); gerek çıkarsa sonra.
- "Pasif veli/öğretmen?" → pasif kişi giremez (spec 0005); alıcı belirlemede pasif üyelik sayılmaz; ilişki kontrolünde
  pasif öğretmene yeni mesaj gönderilemez.
- "Kurucu aynı zamanda öğretmense?" → kurucu yetkisi geçerli (okul geneli); mesajlaşma öğretmen rolü ve sınıf ilişkisiyle.
- "Mesajda okundu işareti?" → yalnızca okunmamış sayısı; mesaj başına "görüldü" yok.

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
