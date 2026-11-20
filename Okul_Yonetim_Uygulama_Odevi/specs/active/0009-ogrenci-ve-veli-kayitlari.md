# Spec 0009 — Öğrenci ve veli kayıtları

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0009-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri (aşağıda), spec ve plan onayı önceden verildi; hassas alan olduğu için clarify kararları tek tek yazıldı

## Intent
Okulun kurucusu/yöneticisi, öğrencilerini sisteme web'den kaydetmek istiyor: her öğrencinin bir **öğrenci kartı**
(ad-soyad, doğum tarihi, okul numarası, şube ve sınıf), **birden çok velisi**, **sağlık notu ve alerjisi** ve okuldan
**teslim alabilecek kişiler** listesi olur. Veli, sisteme giriş yapan bir kişidir (veli rolü olan kullanıcı): kurucu onu
öğrencinin kartından ekler; yeni kişiyse bir kez gösterilen geçici şifreyle (spec 0005 akışı) mobil uygulamaya girer ve
yalnızca **kendi çocuklarını** görür; aynı veliye bağlı öğrenciler **kardeş** olarak görünür, farklı şubelerde olsalar
bile tek hesapla. Başarı: kurucu bir öğrenci kaydeder, iki veli bağlar, birine geçici şifreyi iletir; veli mobilde
"Çocuklarım"da iki çocuğunu (farklı şubelerde) görür, kartta alerji ve teslim alabilecek kişileri okur; başka çocuğu
göremez. Yoklama (0010) ve veli iletişimi (0011) bu kayıtlara dayanır. Bilinçli olarak yapılmayanlar: Excel ve e-Okul
çıktısından içe aktarma (yol haritası 3'ün "sonra" adımı), TC kimlik no, nakil (şube değişikliği), velinin kendi
bilgisini/çocuğunun kartını düzenlemesi, öğretmenin öğrenci görünümü (0010 sınıf listesiyle), KVKK veli rıza akışı.

### Clarify kararları
Her karar: analistin önerisi seçildi — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Öğrenci kaydını kim yapar?** → Yalnızca seçili okulun kurucusu/yöneticisi, yalnızca web'de. Neden: kayıt
  yönetimin işi; muhasebe/idari personel ve öğretmen yetkisi ayrı bir kararla sonra eklenebilir.
  Dokümanda istenen: "Kurucu / Yönetici | Kayıt, tahsilat, raporlar, ayarlar | Web".
- **C-2 — Öğrenci kartında hangi bilgiler?** → Ad-soyad (zorunlu), doğum tarihi (zorunlu), okul numarası (isteğe
  bağlı, okulda tekil), şube (zorunlu), sınıf (isteğe bağlı; o şubenin sınıfı — spec 0008), sağlık notu ve alerji
  (isteğe bağlı). TC kimlik no yok. Neden: e-Okul alma gelene kadar TC'nin (şifreli + parmak izi) maliyeti gereksiz;
  sınıfsız kayıt, sınıflar açılmadan öğrencinin girilebilmesini sağlar.
  Dokümanda istenen: "Öğrenci kartı, birden fazla veli, kardeş bağlantısı" · "Sağlık notu, alerji, teslim alabilecek
  kişiler".
- **C-3 — Veli nasıl eklenir?** → Veli bir kişi hesabıdır (veli rolü olan kullanıcı). Kurucu öğrencinin kartından veli
  ekler: ad-soyad, e-posta (giriş için zorunlu), telefon (isteğe bağlı), yakınlık (Anne / Baba / Diğer). Spec 0005
  kuralları geçerli: sistemde olmayan e-postada hesap açılır ve geçici şifre bir kez gösterilir; var olan e-postada
  hesap açılmaz, şifre değişmez, kişi bağlanır. Okulda zaten başka rolü olan kişi (ör. öğretmen) de veli olabilir.
  Veli rolü öğrencinin şubesinde kendiliğinden verilir. Bir öğrencinin en fazla 4 velisi olur. Neden: 0005'teki
  doğrulanmış hesap akışını yeniden kullanmak en sade ve güvenli yol.
  Dokümanda istenen: "Birden fazla veli" · "Bir kişi birden fazla role sahip olabilir (örneğin hem öğretmen hem veli)".
- **C-4 — Kardeş bağlantısı nasıl kurulur?** → Ayrı bir kayıt yok: aynı veliye bağlı öğrenciler kendiliğinden kardeştir;
  öğrenci kartında "Kardeşler" listelenir. Neden: ikinci bir bağ elle tutulursa veli bağıyla çelişebilir.
  Dokümanda istenen: "Kardeşi olan veli tek hesapla tüm çocuklarını görür."
- **C-5 — Teslim alabilecek kişiler nedir?** → Hesap değil, öğrenci kartındaki bir liste: ad-soyad, yakınlık (serbest
  metin), telefon — üçü de zorunlu; en fazla 10 kişi. Veliler listeye kendiliğinden girmez (kurucu isterse ekler).
  Neden: velayet gibi durumlarda okul listeyi açıkça yönetmeli; sisteme giriş gerekmiyor.
  Dokümanda istenen: "Sağlık notu, alerji, teslim alabilecek kişiler".
- **C-6 — Sağlık notu ve alerjiyi kim görür?** → Kurucu/yönetici (web) ve o öğrencinin velileri (mobil). Alanlar
  veritabanında şifreli durur, loglara yazılmaz; kartın her açılışı ve sağlık bilgisinin her okunuşu audit'e geçer.
  Öğretmenin görmesi yoklama sınıf listesiyle (spec 0010) gelir. Neden: özel nitelikli veri; en dar erişimle başla.
  Dokümanda istenen: "Audit log: kim, hangi öğrenci kaydını, ne zaman görüntüledi/değiştirdi" · "2FA (yönetici
  zorunlu), şifreli veri saklama".
- **C-7 — Veli ne görür?** → Mobilde "Çocuklarım": bağlı çocuklarının listesi ve salt okunur kartı (ad-soyad, doğum
  tarihi, okul numarası, şube, sınıf, sağlık notu, alerji, teslim alabilecek kişiler). Diğer velilerin bilgisini
  görmez, hiçbir şeyi düzenleyemez. Neden: boşanmış aileler gibi durumlarda veliler birbirinin iletişim bilgisini
  görmemeli; düzenleme okulun sorumluluğunda kalır.
  Dokümanda istenen: "Veli | Takip, bildirim, ödeme, randevu | Mobil".
- **C-8 — Ayrılan öğrenci ve şube değişikliği?** → Öğrenci silinmez, "ayrıldı" (pasif) yapılır ve geri alınabilir;
  ayrılan öğrenci velinin listesinden düşer. Şube değişikliği (nakil) bu spec'te yok; sınıf yalnızca aynı şubenin
  sınıflarıyla değiştirilebilir. Neden: BR-9 nakli geçmişiyle ister — ayrı spec; saklama süresi ve anonimleştirme
  pilot öncesi KVKK işinde.
  Dokümanda istenen: "Mezun/ayrılan öğrenci verisi için saklama süresi ve anonimleştirme" · BR-9 "şube değişikliği
  nakil kaydıdır, geçmiş korunur".

## Requirements
1. **Yetki:** Öğrenci yönetimi (ekleme, düzenleme, ayrıldı/geri al, veli bağlama/kaldırma, teslim alabilecek kişiler)
   yalnızca **web'de** ve yalnızca seçili okulun **kurucu/yöneticisine** açıktır; diğer roller doğrudan denerse
   reddedilir, hiçbir şey değişmez. "Çocuklarım" yalnızca seçili okulda veli rolü olan kişiye açıktır.
2. **Okul ve ilişki duvarı:** Her işlem yalnızca seçili okulun kayıtlarına uygulanır; başka okulun öğrencisi
   "bulunamadı" olur. Veli yalnızca kendisine bağlı ve "ayrıldı" olmayan öğrencileri görür; aynı şubedeki başka bir
   öğrencinin kartı ona "bulunamadı"dır.
3. **Öğrenci ekleme/düzenleme:** Ad-soyad (zorunlu, en fazla 100), doğum tarihi (zorunlu; bugünden ileri ve 100 yıldan
   eski olamaz), okul numarası (isteğe bağlı, en fazla 20, harf ve rakam; okulda harf farkı gözetmeden tekil), şube
   (zorunlu; okulun etkin şubesi), sınıf (isteğe bağlı; seçilen şubenin etkin sınıfı — spec 0008), sağlık notu (isteğe bağlı, en fazla
   1000) ve alerji (isteğe bağlı, en fazla 500). Kayıttan sonra şube değiştirilemez (nakil yok); sınıf aynı şubenin
   sınıflarıyla değiştirilir ya da boşaltılır.
4. **Liste:** Kurucu öğrencileri ad sırasıyla (Türkçe harf kuralı) listeler: ad-soyad, okul numarası, şube, sınıf,
   veli sayısı, durum (etkin/ayrıldı). Ad veya okul numarasıyla arama; şube, sınıf ve duruma göre süzme; sayfa başına
   25 öğrenci. Listede sağlık bilgisi görünmez.
5. **Ayrıldı / geri al:** Kurucu onaydan sonra öğrenciyi "ayrıldı" yapar ve geri alabilir; öğrenci silinmez. Ayrılan
   öğrenci velilerin "Çocuklarım" listesinden düşer, kurucunun listesinde "ayrıldı" etiketiyle kalır.
6. **Veli bağlama:** Kurucu kartta "Veli ekle" ile ad-soyad, e-posta, telefon (spec 0005 telefon kuralı) ve yakınlık
   (Anne / Baba / Diğer) girer. Yeni e-postada hesap açılır, geçici şifre (spec 0005 kuralı) bir kez gösterilir. Var
   olan e-postada hesap açılmaz, şifre ve ad-soyad değişmez, "Bu kişinin zaten bir hesabı var; mevcut şifresiyle
   girer." gösterilir. Kişi bu okulda pasifse bağlama reddedilir ("Bu kişi bu okulda pasif; önce Yönetim'den
   etkinleştirin."). Kişiye öğrencinin şubesinde veli rolü yoksa verilir. Aynı veli aynı öğrenciye ikinci kez
   bağlanamaz; 5. veli reddedilir.
7. **Veli bağını kaldırma:** Kurucu onaydan sonra bağı kaldırır; veli o öğrenciyi bir sonraki isteğinde artık göremez.
   Velinin hesabı, rolleri ve diğer çocuklarıyla bağı değişmez. Velisiz öğrenci olabilir.
8. **Kardeşler:** Öğrenci kartında aynı veliye bağlı diğer öğrenciler (ad-soyad, şube, sınıf, durum) listelenir. Veli
   tek hesapla bağlı bütün çocuklarını görür, farklı şubelerde olsalar bile (BR-2).
9. **Sağlık notu ve alerji:** Veritabanında şifreli saklanır; şifreleme anahtarı repoda değil, ortam ayarındadır.
   Loglarda, hata yanıtlarında ve audit kayıtlarında geçmez. Anahtar ayarlı değilse öğrenci kartı yine çalışır;
   sağlık alanlarının yerinde "Sağlık bilgisi şu anda gösterilemiyor." yazar ve dolu sağlık bilgisi kaydetme
   "Sağlık bilgisi şu anda kaydedilemiyor." ile reddedilir (sunucu logunda "yapılandırılmamış" hatası).
10. **Teslim alabilecek kişiler:** Kurucu öğrencinin listesine kişi ekler, düzenler ve kaldırır: ad-soyad (en fazla
    100), yakınlık (en fazla 50), telefon (spec 0005 kuralı) — üçü zorunlu; en fazla 10 kişi. Veliler kartta bu
    listeyi görür.
11. **Veli görünümü (mobil):** "Çocuklarım" bağlı çocukları ad-soyad, şube ve sınıfla listeler; çocuğa dokununca salt
    okunur kart açılır (clarify C-7'deki alanlar). Diğer veliler ve kardeşlerin kartları bu kartta görünmez
    (kardeş zaten kendi satırında). Hoş geldin ekranında "Çocuklarım" yalnızca veli rolü olan kişiye görünür.
12. **Audit:** Öğrenci ekleme, düzenleme, ayrıldı/geri al, kart görüntüleme (kurucu ve veli), sağlık bilgisi okuma,
    veli bağlama/kaldırma ve teslim alabilecek kişi değişikliği audit'e geçer (kim, ne, hangi öğrenci, ne zaman, IP).
    Veli için açılan hesap ve verilen rol spec 0005'teki audit olaylarıyla yazılır. Geçici şifre audit'e, loga ve
    yanıt dışında hiçbir yere yazılmaz.

## User experience
- **Flow (web, kurucu):** Hoş geldin'de "Öğrenciler" bağlantısı (yalnızca kurucuya) → `#/ogrenciler`: liste (arama,
  süzgeç, sayfalama) → "Öğrenci ekle" formu → öğrenci kartı. Kart bölümleri: **Bilgiler** (düzenle), **Sağlık** (sağlık
  notu, alerji), **Veliler** ("Veli ekle" formu → sonuç: geçici şifre bir kez ya da "zaten hesabı var"; satırda
  "Kaldır" onaylı), **Kardeşler** (salt okunur; satıra tıklayınca o kardeşin kartı), **Teslim alabilecek kişiler**
  (ekle/düzenle/kaldır), "Ayrıldı olarak işaretle" / "Geri al" (onaylı).
- **Flow (mobil, veli):** Okul seç → Hoş geldin'de "Çocuklarım" → çocuk listesi → çocuğun kartı (salt okunur) → geri.
- **States:**
  - Web listeleri: yükleniyor; boş ("Henüz öğrenci yok" / "Aramaya uyan öğrenci yok"); dolu; hata + tekrar dene.
  - Web formları: alan hataları (istek gönderilmez); gönderiliyor (düğme kilitli); sunucu hatası (ör. "Bu okul
    numarası başka bir öğrencide", "Bu veli bu öğrenciye zaten bağlı", "Bir öğrencinin en fazla 4 velisi olabilir").
  - Veli ekleme sonucu: geçici şifre (spec 0005 kalıbı: "bir daha gösterilmeyecek" uyarısı, kopyala, kopyalandı) ya
    da "zaten hesabı var" mesajı.
  - Kart: sınıf yoksa "Sınıf atanmamış"; velisi yoksa "Henüz veli yok"; kardeş yoksa "Kardeş yok"; teslim kişisi yoksa
    "Henüz kimse eklenmedi"; sağlık anahtarı yoksa "Sağlık bilgisi şu anda gösterilemiyor."; ayrılan öğrencide
    "Ayrıldı" etiketi; sınırda (4 veli / 10 kişi) ekle düğmesi pasif ve nedeni yazılı.
  - Onay iletişimi: veli kaldırma ve ayrıldı işaretleme (Vazgeç / Onayla, Esc kapatır).
  - Yetkisiz: kurucu olmayan `#/ogrenciler`'de "Bu sayfaya erişiminiz yok."
  - Mobil: "Çocuklarım" yükleniyor; boş ("Henüz bağlı öğrenci yok"); dolu; hata + tekrar dene; kart (sınıf yoksa
    "Sınıf atanmamış", teslim kişisi yoksa "Henüz kimse eklenmedi", sağlık anahtarı yoksa gösterilemiyor mesajı);
    kart artık görülemiyorsa (bağ kaldırıldı / ayrıldı) "Bu öğrenci artık görüntülenemiyor." ve listeye dönüş.
- **Design reference:** none — the flow above is the design; spec 0005 yönetim sayfası kalıbı (liste, form, kart, onay
  iletişimi, geçici şifre sonucu), spec 0002 ortak tema, spec 0004 mobil ekran düzeni.
- **Accessibility:** web akışı yalnızca klavyeyle; alanların görünür etiketi; hatalar alanla ilişkili ve ekran
  okuyucuya duyurulur; mobilde dokunma hedefleri ve ekran okuyucu etiketleri (alerji ayrı başlıkla okunur); iki temada
  kontrast spec 0002 eşiklerinde; web'de 320 px'te yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S1 (okul/şube izolasyonu ve veli-çocuk ilişki duvarı), S2 (öğrenci ve veli kişisel verisi, sağlık
  notu, alerji, teslim alabilecek kişiler), S3 (veli-öğrenci bağı, rol ataması, geçici şifre), S6 (audit kaydı).
- **Bağımlılık:** spec 0008 (sınıf seviyesi, sınıf, dönem) bitmeden build başlamaz; sınıf seçimi 0008'in sınıfını
  kullanır.
- **Sonra (yol haritası 3'ün devamı):** Excel ve e-Okul çıktısından öğrenci/veli içe aktarma (dosya sınırları ve formül
  hücresi kuralı — `docs/security.md`); TC kimlik no (şifreli + arama parmak izi) içe aktarmayla birlikte.
- Nakil (şube değişikliği, geçmişiyle — BR-9) → ayrı spec. Öğrenci fotoğrafı yok (fotoğraf rızasıyla gelir).
- Öğretmenin öğrenci/sağlık görünümü → spec 0010 (yoklama sınıf listesi). Muhasebe/idari personelin öğrenci yetkisi yok.
- Velinin kendi bilgisini veya çocuğun kartını düzenlemesi yok (velinin ad/telefonu: Hesabım — spec 0006).
- Velinin web görünümü yok; kurucunun mobil öğrenci ekranı yok.
- KVKK veli aydınlatma metni + açık rıza akışı (`docs/security.md` KVKK 1) bu spec'te yok; gerçek okul verisinden
  (pilot) önce ayrı spec olmalı. Saklama süresi ve anonimleştirme de pilot öncesi KVKK işinde.
- Kalıcı öğrenci silme yok. Audit görüntüleme ekranı yok (yalnızca yazılır).
- Pi'ye bu spec'te dağıtım yapılmaz; sağlık şifreleme anahtarı dosyasını hazırlayan betik repoda hazırlanır,
  çalıştırma ship'te kullanıcıda.
- Split: proposed 0009a "Öğrenci kartı, veliler, kardeşler, sağlık, teslim kişileri (web)" ve 0009b "Veli mobil
  görünümü (Çocuklarım)"; kept as one — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20: yol haritası
  3 tek satır, veli görünümü olmadan veli ilişki duvarı (S1) uçtan uca kanıtlanamaz; 13 kriter sınırında.

## Acceptance criteria
- [ ] AC-1 — Yetki: öğretmen, veli ve muhasebe rolündeki kişi öğrenci yönetimi işlemlerini doğrudan denerse reddedilir (403) ve hiçbir şey değişmez; Hoş geldin'de "Öğrenciler"i görmez, `#/ogrenciler`'de "Bu sayfaya erişiminiz yok." görür; veli rolü olmayan kişi "Çocuklarım" uçlarında reddedilir; girişsiz istek 401 alır.
- [ ] AC-2 — Okul ve ilişki duvarı: A okulunun kurucusu B okulunun öğrencisini (kimliğini bilerek bile) görüntüleyemez, düzenleyemez, veli bağlayamaz — "bulunamadı"; A'nın listesi yalnızca A'nın öğrencilerini içerir; veli, aynı şubedeki bağlı olmadığı öğrencinin kartını açamaz (404) ve listesinde yalnızca kendi çocukları vardır; başka okuldaki çocuğu o okul seçilmeden görünmez.
- [ ] AC-3 — Öğrenci ekleme: geçerli bilgilerle öğrenci oluşur ve listede görünür; boş ad-soyad, 100'den uzun ad, gelecekteki ya da 100 yıldan eski doğum tarihi, 20'den uzun ya da harf/rakam dışı okul numarası, pasif ya da başka okulun şubesi, pasif ya da seçilen şubeye ait olmayan sınıf, 1000'den uzun sağlık notu, 500'den uzun alerji alan hatası verir ve hiçbir şey oluşmaz; aynı okulda aynı okul numarası (harf farkıyla bile) "Bu okul numarası başka bir öğrencide" ile reddedilir, başka okulda serbesttir.
- [ ] AC-4 — Liste: öğrenciler ad sırasıyla (Türkçe harf kuralı) ad-soyad, okul numarası, şube, sınıf, veli sayısı ve durumla listelenir; ad veya okul numarası araması, şube/sınıf/durum süzgeci çalışır; 25'ten fazla öğrencide sayfalama; boş sonuçta "Aramaya uyan öğrenci yok"; liste yanıtında sağlık bilgisi yoktur.
- [ ] AC-5 — Düzenleme ve ayrıldı: kurucu bilgileri ve sınıfı (aynı şubenin sınıfı ya da boş) değiştirir; şube değiştirme denemesi reddedilir; "ayrıldı" yapılan öğrenci silinmez, kurucu listesinde "ayrıldı" görünür, velinin listesinden düşer ve kartı veliye 404 olur; "geri al" ile her şey eski hâline döner.
- [ ] AC-6 — Yeni veli: sistemde olmayan e-postayla veli eklenince hesap açılır, geçici şifre (12 karakter, harf+rakam, 0/O/1/l/I yok) bir kez gösterilir ve sonra hiçbir yanıtta alınamaz; kişi öğrencinin şubesinde veli rolü alır, o şifreyle mobilde girip "Çocuklarım"da çocuğu görür; eksik/geçersiz alanlar (ad, e-posta, telefon, yakınlık) alan hatası verir ve hiçbir şey oluşmaz.
- [ ] AC-7 — Var olan kişi ve sınırlar: başka okulda ya da bu okulda başka rolle kayıtlı e-posta veli olarak bağlanınca yeni hesap açılmaz, şifre ve ad-soyad değişmez, "Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer." gösterilir ve mevcut şifresiyle çocuğu görür; bu okulda pasif kişi "önce Yönetim'den etkinleştirin" ile, aynı velinin ikinci kez bağlanması "Bu veli bu öğrenciye zaten bağlı" ile, 5. veli "en fazla 4 veli" ile reddedilir ve hiçbir şey değişmez.
- [ ] AC-8 — Veli bağını kaldırma: onaydan sonra bağ kalkar; veli o öğrencinin kartını bir sonraki isteğinde açamaz (404) ve listesinde görmez; velinin hesabı, rolleri ve diğer çocuğuyla bağı değişmez; velisi kalmayan öğrencinin kartında "Henüz veli yok" görünür.
- [ ] AC-9 — Kardeşler: aynı veliye bağlı iki öğrenci (farklı şubelerde bile) birbirinin kartında "Kardeşler" altında görünür; bağ kaldırılınca kardeşlik de kalkar; veli tek hesapla iki çocuğunu (farklı şubelerde) "Çocuklarım"da görür ve ikisinin kartını açar.
- [ ] AC-10 — Sağlık bilgisi gizliliği: sağlık notu ve alerji veritabanında düz metin olarak bulunmaz (şifreli), kurucunun ve bağlı velinin kartında doğru okunur; loglarda, hata yanıtlarında ve audit kayıtlarında geçmez; şifreleme anahtarı repoda yoktur; anahtar ayarlı değilse kart çalışır, sağlık alanları "Sağlık bilgisi şu anda gösterilemiyor." gösterir ve dolu sağlık bilgisi kaydetme reddedilir, diğer alanlar kaydedilir.
- [ ] AC-11 — Teslim alabilecek kişiler: kurucu kişi ekler, düzenler, kaldırır; ad-soyad, yakınlık ve geçerli telefon zorunludur (eksik/geçersiz → alan hatası, hiçbir şey değişmez); 11. kişi reddedilir; liste kurucunun ve bağlı velinin kartında görünür.
- [ ] AC-12 — Audit ve gizlilik: öğrenci ekleme, düzenleme, ayrıldı/geri al, kart görüntüleme (kurucu ve veli ayrı ayrı), sağlık bilgisi okuma, veli bağlama/kaldırma ve teslim kişisi değişikliği audit kaydı oluşturur (kim, ne, hangi öğrenci, ne zaman, IP); veli için hesap açılışı ve rol verilmesi audit'e geçer; geçici şifre, öğrenci adı ve sağlık bilgisi loglarda ve audit kayıtlarında geçmez (yalnızca kimlikler).
- [ ] AC-13 — Ekran durumları: web (iki tema) — öğrenci listesi yükleniyor / boş / dolu / hata + tekrar dene; formlarda alan hataları, gönderiliyor (düğme kilitli), sunucu hatası; kartta bölümlerin boş durumları, sınırda pasif ekle düğmesi + neden, "Ayrıldı" etiketi, geçici şifre sonucu (uyarı, kopyala, kopyalandı), onay iletişimi (Esc kapatır), yetkisiz mesajı; akış yalnızca klavyeyle, hatalar duyurulur, 320 px'te yatay kayma yok. Mobil (iki tema) — "Çocuklarım" yalnızca velide; liste yükleniyor / boş / dolu / hata + tekrar dene; kart (sınıf atanmamış, teslim kişisi yok, sağlık gösterilemiyor) ve "Bu öğrenci artık görüntülenemiyor." durumu; ekran okuyucu etiketleri.

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
