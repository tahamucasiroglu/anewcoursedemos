# Spec 0008 — Kurum ve akademik yapı

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0008-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri (aşağıda, her biri kayıtlı); spec ve plan onayı önceden verildi; ship ve Pi yayını kullanıcıda

## Intent
Okulun kurucusu, okulunun akademik iskeletini web'deki Yönetim sayfasından kendisi kurmak istiyor: sınıf seviyelerini
(5. sınıf, hazırlık, 4 yaş grubu), eğitim dönemlerini ("2026-2027 Güz") ve her dönemde hangi şubede hangi seviyede hangi
sınıfların açık olduğunu (şube-sınıf eşleşmesi: "Bahçelievler 5-A") tanımlar; dönemin akademik takvimine tatil, sınav ve
etkinlik günlerini işler. Okuldaki herkes (öğretmen, veli, muhasebe, kurucu) güncel dönemi ve takvimini web'de ve mobilde
görür. Öğrenci kaydı (spec 0009) ve yoklama (spec 0010) bu sınıflara ve dönemlere bağlanacağı için bu spec onların
temelidir. Başarı: kurucu bir dönem açar, iki şubesinde sınıfları kurar, takvime bir tatil ekler; veli mobilde o tatili
görür; başka okul ve atanmadığı şube bu kayıtları göremez. Bilinçli olarak yapılmayanlar: ders programı (elle ve Excel'den
alma — bu modülün sonraki adımı), Excel'den sınıf alma, önceki dönemden sınıf kopyalama, öğretmen-sınıf bağı, şubeye özel
takvim, öğrenci kaydı.

### Clarify kararları
Hepsi analistin önerisi; karar — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Şube-sınıf eşleşmesi nasıl tutulur?** Karar: sınıf her dönem için ayrı bir kayıttır ve doğrudan bir döneme, bir
  şubeye ve bir seviyeye bağlıdır; "eşleşme" = o dönemde o şubede açılmış sınıfların listesi. Kalıcı sınıf + ayrı eşleşme
  kaydı yok. Neden: tek kavram, tek ekran; "öğrenci bir dönemde tek sınıfta" kuralı (BR-9) dönem bilgisini sınıftan alır;
  geçmiş dönemin sınıfları olduğu gibi kalır.
  Dokümanda istenen: "Şube, sınıf, şube-sınıf eşleşmesi, dönem ve akademik takvim" (kapsam, MVP modülü 1); domain.md:
  "Şube-sınıf eşleşmesi — Bir dönemde hangi şubede hangi seviyede hangi sınıfların açık olduğu"; "Sınıf — Bir şubede, bir
  seviyedeki öğrenci grubu; 'Bahçelievler 5-A' ≠ 'Avcılar 5-A' — sınıf her zaman bir şubeye bağlı".
- **C-2 — Dönem okul geneli mi, şubeye mi ait? "Güncel dönem" nasıl belirlenir?** Karar: dönem okul genelidir (bütün
  şubeler aynı dönemleri kullanır); aynı okulda iki dönemin tarihleri çakışamaz; güncel dönem bugünü (Türkiye saati)
  kapsayan dönemdir, yoksa henüz başlamamış en yakın dönem. Elle "etkin dönem" seçimi yok. Neden: elle seçim unutulur ve
  sonraki spec'ler için belirsiz kalır; çakışma yasağıyla tarih tek başına yeterli.
  Dokümanda istenen: domain.md: "Dönem (Term) — Eğitim yılı içindeki dönem — '2026-2027 Güz'; akademik takvim döneme
  bağlı". Kapsam dokümanı şubeye özel dönemden söz etmiyor.
- **C-3 — Sınıf seviyeleri hazır mı gelir, okul mu tanımlar?** Karar: okul kendi seviyelerini tanımlar (ad + sıra
  numarası); hazır MEB listesi yüklenmez. Neden: hedef okullar kurs, etüt ve anaokulu da içeriyor ("4 yaş grubu",
  "hazırlık"); hazır liste çoğu okulda silinecek kayıt üretir.
  Dokümanda istenen: domain.md: "Sınıf seviyesi (GradeLevel) — Kademe — 5. sınıf, hazırlık, 4 yaş grubu"; hedef segment:
  "özel okullar, kolejler, kurs ve etüt merkezleri".
- **C-4 — Akademik takvim neyi tutar?** Karar: döneme bağlı, okul geneli kayıtlar: başlık, tür (Tatil, Sınav, Etkinlik),
  başlangıç ve bitiş günü (tek gün olabilir), dönemin içinde. Saat, açıklama, şubeye ya da sınıfa özel kayıt, bildirim
  yok. Neden: yoklamanın (0010) bilmesi gereken "ders yok" günü ve velinin görmek istediği tarihler için yeterli en sade
  model.
  Dokümanda istenen: "dönem ve akademik takvim" (kapsam, MVP modülü 1); domain.md: "akademik takvim döneme bağlı".
  Kapsam dokümanı takvimin içeriğini ayrıntılandırmıyor.
- **C-5 — Silme mi, pasifleştirme mi?** Karar: seviye ve sınıf silinmez, pasifleştirilir ve yeniden etkinleştirilir
  (spec 0005'teki şube kalıbı); dönem silinmez ve pasifleştirilmez, yalnızca adı ve tarihleri düzenlenir; takvim kaydı
  onayla silinir (tekrar görünmez). Neden: sonraki spec'ler (öğrenci, yoklama) sınıfa ve döneme bağlanacak; silinen sınıf
  geçmişi koparır. Takvim kaydına hiçbir şey bağlanmaz.
  Dokümanda istenen: kapsam dokümanında silme konusu geçmiyor; conventions.md veri kuralı 5: "iş kayıtları soft delete";
  domain.md BR-10: "Şube silinmez, pasifleştirilir".
- **C-6 — Kim yönetir, kim görür, hangi kanalda?** Karar: seviye, dönem, sınıf ve takvim yönetimi yalnızca kurucu/yönetici,
  yalnızca web (mevcut Yönetim sayfasına yeni sekmeler). Okul takvimi (güncel dönem + kayıtları) okulun bütün rollerine
  web ve mobilde salt okunur açılır. Sınıf listesi kurucu dışındaki rollere yalnızca atandıkları etkin şubeler için açılır
  (bu spec'te bu roller için sınıf ekranı yok; öğrenci ve yoklama spec'leri kullanacak). Neden: kapsamda kurucu kanalı
  "Web"; takvim veli için de anlamlı; şube bazlı görünürlük güvenlik kuralı.
  Dokümanda istenen: rol tablosu "Kurucu / Yönetici — Kayıt, tahsilat, raporlar, ayarlar — Web"; altyapı "Rol ve yetki
  yönetimi, şube bazlı görünürlük"; mobil "Veli ve öğretmen için tek uygulama, role göre arayüz".
- **C-7 — Ders programı, Excel'den alma, önceki dönemden kopyalama bu spec'te mi?** Karar: hayır; hepsi "sonra". Ders
  programı modülün sonraki adımı olarak ayrı spec; öğretmen-sınıf bağı da onunla (ya da yoklama spec'inin ihtiyacıyla)
  gelir. Neden: 13 kriter sınırı ve "basit çözümler" tercihi; ders programı tek başına bir yetenek.
  Dokümanda istenen: "Ders programı (manuel giriş + Excel içe aktarma)" (kapsam, MVP modülü 1); yol haritası satır 2:
  "Sonra: ders programı (elle + Excel'den alma)".
- **C-8 — Audit kaydı tutulur mu?** Karar: evet; seviye, dönem, sınıf ve takvimdeki her değişiklik audit'e yazılır (kim,
  ne, hangi kayıt, ne zaman, IP); okumalar yazılmaz (kişisel veri yok). Neden: spec 0005'te her yönetim işlemi audit'li;
  aynı kalıp, ek maliyet yok.
  Dokümanda istenen: "Audit log: kim, hangi öğrenci kaydını, ne zaman görüntüledi/değiştirdi" (kapsam, altyapı).

## Requirements
1. **Yetki:** Seviye, dönem, sınıf ve takvim yönetimi yalnızca **web'de** ve yalnızca seçili okulda kurucu/yönetici rolü
   olan kişiye açıktır; diğer roller bu ekranları görmez, işlemleri doğrudan denerlerse reddedilir ve hiçbir şey
   değişmez. Okul takvimini görüntüleme seçili okulda rolü olan herkese açıktır. Girişsiz istek reddedilir.
2. **Okul ve şube duvarı:** Her kayıt bir okula aittir; başka okulun seviyesi, dönemi, sınıfı ya da takvim kaydı (kimliği
   bilinse bile) "bulunamadı" olur. Sınıf her zaman bir şubeye bağlıdır: kurucu okulun bütün sınıflarını görür; diğer
   roller yalnızca atandıkları **etkin** şubelerin sınıflarını görür, öbürleri "bulunamadı" olur.
3. **Sınıf seviyeleri:** Kurucu seviye ekler ve düzenler: ad zorunlu (en fazla 50 karakter), sıra numarası zorunlu (1–99
   arası tam sayı; formda varsayılanı mevcut en büyük sıra + 1). Liste sıra numarasına, eşitse ada göre dizilir. Aynı
   okulda aynı adla (harf farkı ve Türkçe harf kuralı gözetmeden) iki etkin seviye olamaz; başka okulda aynı ad serbesttir.
   Seviye pasifleştirilir ve yeniden etkinleştirilir (etkinleştirmede ad çakışması yeniden denetlenir); pasif seviye yeni
   sınıfta ve sınıf düzenlemede seçilemez, onu kullanan mevcut sınıflar olduğu gibi kalır.
4. **Dönemler:** Kurucu dönem ekler ve düzenler: ad zorunlu (en fazla 100 karakter, okulda benzersiz — harf farkı
   gözetmeden), başlangıç ve bitiş günü zorunlu; başlangıç bitişten önce olmalı, dönem en fazla 366 gün sürer. Aynı okulda
   iki dönemin günleri çakışamaz (bir dönemin bitiş günü öbürünün başlangıç günüyse çakışır; ertesi gün başlıyorsa
   çakışmaz). Düzenleme, dönemin takvim kayıtlarından birini yeni tarihlerin dışında bırakacaksa reddedilir. Dönem
   silinmez ve pasifleştirilmez. Liste başlangıç gününe göre yeniden eskiye dizilir; güncel dönem işaretlidir.
5. **Güncel dönem:** Bugünü (Türkiye saatiyle) kapsayan dönemdir — başlangıç ve bitiş günü dahil. Böyle bir dönem yoksa
   henüz başlamamış en yakın dönem gösterilir ("Yaklaşan dönem" olarak). O da yoksa güncel dönem yoktur.
6. **Sınıflar (şube-sınıf eşleşmesi):** Kurucu bir dönem seçip o dönemde sınıf açar: etkin şube, etkin seviye ve ad
   (zorunlu, en fazla 30 karakter, ör. "5-A") zorunlu. Aynı dönemde aynı şubede aynı adla (harf farkı gözetmeden) iki etkin
   sınıf olamaz; başka şubede ya da başka dönemde aynı ad serbesttir. Pasif şube ve pasif seviye seçilemez; doğrudan
   denenirse reddedilir. Sınıfın adı ve seviyesi düzenlenir; dönemi ve şubesi değişmez (yanlışsa sınıf pasifleştirilip
   doğrusu açılır). Sınıf pasifleştirilir ve yeniden etkinleştirilir (etkinleştirmede ad çakışması yeniden denetlenir).
   Liste seçilen dönem için gösterilir (varsayılan: güncel dönem, yoksa en yeni dönem), şubeye ve duruma göre süzülür;
   şube adı, seviye sırası ve sınıf adıyla dizilir; satırda şube, seviye, ad ve durum görünür (pasif şubenin sınıfı
   "şube pasif" etiketiyle). Okul başına sınıf sayısı küçük olduğundan sayfalama yoktur.
7. **Akademik takvim:** Kurucu bir dönemin takvimine kayıt ekler, düzenler ve onayla siler: başlık zorunlu (en fazla 100
   karakter), tür zorunlu (Tatil, Sınav, Etkinlik), başlangıç ve bitiş günü zorunlu (bitiş başlangıçtan önce olamaz; aynı
   gün = tek günlük kayıt); iki gün de dönemin içinde olmalıdır. Kayıtlar okul geneli (bütün şubeler) için geçerlidir ve
   başlangıç gününe göre dizilir. Silinen kayıt hiçbir ekranda görünmez.
8. **Okul takvimi (herkes, salt okunur):** Okulda rolü olan herkes web'de ve mobilde güncel dönemin adını, tarihlerini
   (yaklaşan dönemse bunu belirterek) ve takvim kayıtlarını (tür, başlık, tarih ya da tarih aralığı) tarih sırasıyla görür.
   Dönem yoksa "Henüz dönem tanımlanmadı."; dönemde kayıt yoksa "Bu dönemde takvim kaydı yok." yazar. Yalnızca seçili
   okulun takvimi görünür.
9. **Eşzamanlılık:** Aynı okulda aynı anda gelen değişiklikler (ör. iki yöneticinin aynı adlı sınıfı ya da çakışan iki
   dönemi aynı anda eklemesi) benzersizlik ve çakışma kurallarını bozamaz.
10. **Audit:** Seviye, dönem, sınıf ve takvim kaydındaki her ekleme, düzenleme, pasifleştirme/etkinleştirme ve silme audit
    kaydına geçer (kim, ne, hangi kayıt, ne zaman, IP).

## User experience
- **Flow (web, kurucu):** Hoş geldin → "Yönetim" → mevcut **Şubeler** ve **Kullanıcılar** sekmelerinin yanında üç yeni
  sekme:
  - **Sınıf seviyeleri:** liste (sıra, ad, durum) → "Seviye ekle" formu (ad, sıra) → listede görünür; satırda "Düzenle",
    "Pasifleştir" (onaylı) / "Etkinleştir".
  - **Dönemler ve takvim:** dönem listesi (ad, tarihler, "Güncel" / "Yaklaşan" etiketi) → "Dönem ekle" formu (ad,
    başlangıç, bitiş — tarih alanları) → satırda "Düzenle", "Takvim". "Takvim" o dönemin kayıtlarını açar: liste (tarih,
    tür, başlık) → "Kayıt ekle" formu (başlık, tür seçimi, başlangıç, bitiş) → satırda "Düzenle", "Sil" (onaylı).
  - **Sınıflar:** üstte dönem seçimi (varsayılan güncel dönem) ve şube/durum süzgeci → liste (şube, seviye, ad, durum) →
    "Sınıf ekle" formu (şube, seviye, ad; dönem seçili dönem) → satırda "Düzenle" (ad, seviye), "Pasifleştir" (onaylı) /
    "Etkinleştir".
- **Flow (web, herkes):** Hoş geldin'de "Okul takvimi" bağlantısı → `#/takvim`: güncel dönem başlığı + tarihleri, altında
  kayıtlar; "Geri" bağlantısı.
- **Flow (mobil, herkes):** Hoş geldin'de "Okul takvimi" düğmesi → takvim ekranı (aynı içerik) → "Geri".
- **States:**
  - Listeler (web sekmeleri, web ve mobil takvim): yükleniyor; boş ("Henüz sınıf seviyesi yok", "Henüz dönem yok — önce
    bir dönem ekleyin", "Bu dönemde sınıf yok", "Bu dönemde takvim kaydı yok.", "Henüz dönem tanımlanmadı."); dolu; hata +
    tekrar dene.
  - Sınıflar sekmesinde dönem yoksa "Önce bir dönem ekleyin"; etkin seviye yoksa "Sınıf ekle" pasif ve nedeni yazılı
    ("Önce bir sınıf seviyesi ekleyin").
  - Formlar: alan hataları (zorunlu alan, uzunluk, sıra 1–99, bitiş başlangıçtan önce, dönem 366 günden uzun, kayıt dönem
    dışında) istek göndermeden; gönderiliyor (düğme kilitli); sunucu hatası (ör. "Bu adla bir sınıf zaten var", "Bu tarihler
    başka bir dönemle çakışıyor", "Dönemin dışında kalan takvim kayıtları var").
  - Onay iletişimi: seviye/sınıf pasifleştirme ve takvim kaydı silme için "Emin misiniz?" (Vazgeç / Onayla; spec 0005
    iletişimi).
  - Pasif kayıtlar: listede "pasif" etiketiyle; pasif şube ve seviye formlarda seçenek olarak çıkmaz.
  - Yetkisiz: kurucu olmayan kişi yeni sekmeleri görmez (Yönetim sayfasının mevcut "Bu sayfaya erişiminiz yok." durumu).
- **Design reference:** none — the flow above is the design (spec 0005 Yönetim sayfası kalıbı, spec 0002 ortak tema).
- **Accessibility:** bütün akış yalnızca klavyeyle (Tab, Enter, Esc onay iletişimini kapatır); alanların görünür
  etiketleri; tarih alanları tarayıcının yerel tarih girişi (elle de yazılabilir); hatalar alanla ilişkili ve ekran
  okuyucuya duyurulur; takvim kayıtlarında tür yalnızca renkle değil metinle de belirtilir; iki temada kontrast spec 0002
  eşiklerinde; web'de 320 px genişlikte yatay kayma yok; mobilde düğme ve satırlar erişilebilirlik etiketli, başlık
  `header` rolünde.

## Constraints & out of scope
- Sensitive area: S1 (kurum ve şube izolasyonu — dört yeni okula ait kayıt türü ve şubeye bağlı sınıf görünürlüğü), S6
  (audit kaydı yazımı). Kişisel veri yok (S2 dışı): seviye, dönem, sınıf ve takvim kayıtları kişi bilgisi taşımaz.
- **Sonra (aynı modülün sonraki adımı, ayrı spec):** ders programı (elle giriş + Excel'den alma), Excel'den seviye/sınıf
  alma, önceki dönemin sınıflarını yeni döneme kopyalama.
- Öğretmen-sınıf bağı (sınıf öğretmeni, branş öğretmeni) yok — ders programıyla ya da yoklama spec'inin ihtiyacıyla gelir.
- Öğrenci kaydı ve sınıfa öğrenci yerleştirme yok (spec 0009). Sınıf kapasitesi yok.
- Şubeye ya da sınıfa özel takvim kaydı, takvim kaydına saat/açıklama, takvim bildirimi, takvimin Excel/iCal dışa aktarımı
  yok.
- Kurucu dışındaki roller için sınıf ekranı yok (yalnızca görünürlük kuralı ve okuma; ekranlar öğrenci/yoklama
  spec'lerinde). Mobilde yönetim yok.
- Dönem silme/pasifleştirme, seviye ve sınıf kalıcı silme yok.
- Şube yönetimi (ekle/düzenle/pasifleştir) spec 0005'te yapıldı; burada değişmez.
- **Split:** proposed 0008a "Seviye, dönem, sınıf" + 0008b "Akademik takvim ve okul takvimi ekranı"; kept as one —
  tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 (yol haritası satır 2 tek spec; 13 kriter sınırında;
  takvim döneme sıkı bağlı ve sade).
- **Self-critique (kör okur geçişi, öneriler katlandı):** (a) "güncel dönem" saat dilimi → Türkiye saati, uç günler dahil
  (gereksinim 5); (b) bitişik dönemler → bitiş günü = başlangıç günü çakışır, ertesi gün çakışmaz (gereksinim 4); (c) dönem
  tarihi düzenlenince takvim kayıtları dışarıda kalırsa → reddedilir (gereksinim 4); (d) pasif şubenin sınıfları → kurucu
  listesinde "şube pasif" etiketiyle, diğer rollere görünmez (gereksinim 2, 6); (e) seviye pasifleşince mevcut sınıflar →
  etkilenmez (gereksinim 3); (f) sınıf listesi hacmi → sayfalama yok (gereksinim 6); (g) iki yöneticinin aynı anda ekleme
  yapması → okul başına sıra (gereksinim 9); (h) sınıfın şubesi/dönemi değişir mi → hayır (gereksinim 6).

## Acceptance criteria
- [ ] AC-1 — Yetki: öğretmen, veli ve muhasebe Yönetim'de yeni sekmeleri görmez; seviye, dönem, sınıf ve takvim kaydı ekleme/düzenleme/pasifleştirme/silme ve yönetim listelerini doğrudan denerse reddedilir (403) ve hiçbir şey değişmez; girişsiz istek 401 alır. Okul takvimini okulda rolü olan her rol (kurucu, öğretmen, veli, muhasebe) görebilir; okul seçilmemiş oturum takvimi alamaz.
- [ ] AC-2 — Okul ve şube duvarı: A okulunun kurucusu B okulunun seviyesini, dönemini, sınıfını ve takvim kaydını (kimliğini bilerek bile) görüntüleyemez, düzenleyemez, pasifleştiremez, silemez — "bulunamadı" (404) alır; A'nın listeleri ve okul takvimi yalnızca A'nın kayıtlarını içerir; A'da B'nin şubesiyle ya da seviyesiyle sınıf açılamaz. Kurucu olmayan kişi yalnızca atandığı etkin şubelerin sınıflarını alır; atanmadığı ya da pasif şubedeki sınıf 404'tür.
- [ ] AC-3 — Sınıf seviyeleri: ad (zorunlu, ≤ 50) ve sıra (zorunlu, 1–99) ile seviye eklenir ve düzenlenir; boş ad, 51 karakter, sıra 0 ve 100 alan hatası verir ve hiçbir şey oluşmaz; liste sıra numarasıyla dizilir; aynı okulda aynı adla ikinci etkin seviye (ör. "HAZIRLIK" = "Hazırlık") "Bu adla bir seviye zaten var" ile reddedilir, başka okulda aynı ad serbesttir; pasifleştirilen seviye yeni sınıfta seçilemez (doğrudan denenirse reddedilir), onu kullanan mevcut sınıf değişmez; yeniden etkinleştirilebilir.
- [ ] AC-4 — Dönem ekleme ve düzenleme: ad (zorunlu, ≤ 100, okulda benzersiz) + başlangıç + bitiş ile dönem oluşur; başlangıç = bitiş ya da bitiş başlangıçtan önce, 367 günlük dönem, eksik tarih alan hatası verir ve hiçbir şey oluşmaz; 366 günlük dönem kabul edilir; dönem silme/pasifleştirme işlemi yoktur.
- [ ] AC-5 — Dönem çakışması: aynı okulda günleri çakışan dönem (bir dönemin bitiş günü öbürünün başlangıç günü olması dahil) "Bu tarihler başka bir dönemle çakışıyor" ile reddedilir; ertesi gün başlayan dönem kabul edilir; başka okulun dönemiyle çakışma serbesttir; takvim kayıtlarından birini dışarıda bırakan tarih düzenlemesi "Dönemin dışında kalan takvim kayıtları var" ile reddedilir ve dönem değişmez.
- [ ] AC-6 — Güncel dönem: Türkiye saatine göre bugünü kapsayan dönem güncel sayılır (başlangıç ve bitiş günü dahil; UTC'de önceki gün olan Türkiye gece yarısı sonrası saatlerde de doğru gün kullanılır); dönem arasında henüz başlamamış en yakın dönem "Yaklaşan dönem" olarak gelir; geçmiş dönemler dışında dönem yoksa güncel dönem yoktur; yönetim dönem listesinde güncel/yaklaşan etiketi doğru dönemdedir.
- [ ] AC-7 — Sınıf açma: seçili dönemde etkin şube + etkin seviye + ad (zorunlu, ≤ 30) ile sınıf oluşur ve o dönemin listesinde şube, seviye ve adla görünür; aynı dönem ve şubede aynı adla ikinci etkin sınıf (harf farkı gözetmeden) "Bu adla bir sınıf zaten var" ile reddedilir; aynı ad başka şubede ve başka dönemde kabul edilir; pasif şube seçilemez (doğrudan denenirse reddedilir); boş/uzun ad alan hatası verir ve hiçbir şey oluşmaz.
- [ ] AC-8 — Sınıf düzenleme, pasifleştirme ve liste: ad ve seviye düzenlenir, dönem ve şube değişmez; sınıf onayla pasifleştirilir, listede "pasif" kalır ve yeniden etkinleştirilir (o arada aynı adla etkin sınıf açılmışsa etkinleştirme reddedilir); liste seçilen döneme göre gelir (varsayılan güncel dönem), şube ve durum süzgeci çalışır, şube–seviye sırası–ad düzeninde dizilir; pasif şubenin sınıfı "şube pasif" etiketiyle görünür; sınıfı olmayan dönemde "Bu dönemde sınıf yok".
- [ ] AC-9 — Akademik takvim yönetimi: dönemin takvimine başlık (zorunlu, ≤ 100) + tür (Tatil/Sınav/Etkinlik) + başlangıç + bitiş ile kayıt eklenir (tek günlük kayıt dahil), düzenlenir ve onayla silinir; bitiş başlangıçtan önce, dönem dışında kalan gün, boş/uzun başlık, geçersiz tür reddedilir ve hiçbir şey değişmez; kayıtlar başlangıç gününe göre dizilir; silinen kayıt hiçbir listede ve okul takviminde görünmez.
- [ ] AC-10 — Okul takvimi (salt okunur): okulda rolü olan kişi güncel dönemin adını, tarihlerini (yaklaşansa "Yaklaşan dönem" ibaresiyle) ve kayıtlarını (tür metni, başlık, tek gün ya da tarih aralığı) tarih sırasıyla görür; dönem yoksa "Henüz dönem tanımlanmadı.", kayıt yoksa "Bu dönemde takvim kaydı yok." görünür; web'de Hoş geldin'deki "Okul takvimi" bağlantısı her rolde vardır.
- [ ] AC-11 — Audit ve eşzamanlılık: seviye, dönem, sınıf ve takvim kaydındaki her ekleme, düzenleme, pasifleştirme/etkinleştirme ve silme audit kaydı oluşturur (kim, ne, hangi kayıt, ne zaman, IP); reddedilen işlem audit'e başarı olarak yazılmaz; aynı adla iki sınıf ya da çakışan iki dönem aynı anda eklenirse yalnızca biri oluşur, öbürü kural mesajıyla reddedilir.
- [ ] AC-12 — Web ekran durumları (iki tema): üç yeni sekmede ve okul takviminde yükleniyor / boş / dolu / hata + tekrar dene; formlarda alan hataları (istek gönderilmez), gönderiliyor (düğme kilitli), sunucu hatası; "Önce bir dönem ekleyin" ve etkin seviye yokken pasif "Sınıf ekle" + neden; onay iletişimi (Vazgeç/Onayla, Esc kapatır, odak açan düğmeye döner); pasif kayıtlar etiketli ve formlarda seçilemez; akış yalnızca klavyeyle tamamlanır, hatalar ekran okuyucuya duyurulur; 320 px'te yatay kayma yok.
- [ ] AC-13 — Mobil okul takvimi: Hoş geldin'deki "Okul takvimi" düğmesi takvim ekranını açar; ekran yükleniyor / boş (iki boş mesajı) / dolu / hata + tekrar dene durumlarını iki temada çizer; kayıt türü metinle yazılır; düğmeler ve başlık erişilebilirlik rolü/etiketiyle; "Geri" Hoş geldin'e döner.

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
