# Domain

> Ortak dil: iş, insanlar ve ajanlar aynı kelimeyi aynı anlamda kullanır. Kaynak:
> `okul-yonetim-platformu-kapsam.md` (28 Ekim 2026). Bootstrap 2026-11-19.

## Ürün
Küçük ve orta ölçekli (tek şubeli veya 2–5 şubeli) özel okullar, kolejler, kurs ve etüt merkezleri için
AI-native, çok kiracılı SaaS okul işletim sistemi. Değer önerisi: "Veliyle iletişimi ve tahsilatı tek
uygulamada toplayın, öğretmenin günde 1 saatini geri kazanın." Karar verici kurucu / genel müdür.
**Bu reponun odağı Faz 1 (MVP).** Faz 2/3 yalnızca yol haritasıdır; kapılar (Kapı 1: pilotlar bir dönemi
tamamladı, Kapı 2: ilk ücretli yenilemeler) geçilmeden spec yazılmaz.

**Dış servisler henüz yok:** ödeme sağlayıcı, SMS, WhatsApp hesabı yok. Sistem bunlara port arayüzleriyle
hazır olur; MVP'de sahte/kayıt tutan uygulamalar çalışır (ödeme = manuel tahsilat kaydı, SMS = log).

Roller (Faz 1): Kurucu/Yönetici (web), Öğretmen (mobil + web), Veli (mobil), Muhasebe/İdari (web).
Faz 2: Öğrenci (mobil), Rehber öğretmen (web).

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Kurum (`Tenant`) | Sistemi kullanan müşteri okul/kurs; sözleşme ve fatura muhatabı; verisi diğer kurumlardan tamamen izole | Örnek: "A Okulları". Tek şubeli okul = 1 kurum + 1 şube. Pasif okula (spec 0007) kimse giremez; veri silinmez |
| Platform yöneticisi | Okulları açan/pasifleştiren proje sahibi (spec 0007); okul rollerinden ayrı yetki | Yalnızca komutla verilir; okulların içini görmez; iki adımlı doğrulama zorunlu |
| İki adımlı doğrulama | Şifreden sonra doğrulayıcı uygulamadaki 6 haneli kod | Şimdilik yalnızca platform yöneticisinde (diğer roller B-17) |
| Şube (`Branch`) | **Aynı kurumun farklı fiziksel lokasyonu** | Örnek: "A Okulları İstanbul Bahçelievler Şubesi". 7-A gibi sınıf şubesi **değildir** → o "Sınıf" |
| Sınıf seviyesi (`GradeLevel`) | Kademe | 5. sınıf, hazırlık, 4 yaş grubu |
| Sınıf (`Classroom`) | Bir şubede, bir seviyedeki öğrenci grubu | "Bahçelievler 5-A" ≠ "Avcılar 5-A" — sınıf her zaman bir şubeye bağlı; her dönem için ayrı kayıt (dönem + şube + seviye + ad, spec 0008) |
| Şube-sınıf eşleşmesi | Bir dönemde hangi şubede hangi seviyede hangi sınıfların açık olduğu | Ayrı kayıt değil: o dönemde o şubede açılmış sınıfların listesi (spec 0008 C-1) |
| Dönem (`Term`) | Eğitim yılı içindeki dönem | "2026-2027 Güz"; okul geneli; akademik takvim döneme bağlı |
| Güncel dönem | Bugünü (Türkiye saatiyle) kapsayan dönem; yoksa henüz başlamamış en yakın dönem ("Yaklaşan dönem") | Elle seçilmez; tarihten hesaplanır (spec 0008) |
| Akademik takvim kaydı (`CalendarEntry`) | Döneme bağlı, okul geneli tatil / sınav / etkinlik günü ya da aralığı | Silinince hiçbir ekranda görünmez (yumuşak silme); yoklama "ders yok" gününü buradan okur |
| Ders programı (`Timetable`) | Sınıf bazlı haftalık ders düzeni | Elle giriş + Excel içe aktarma |
| Kullanıcı (`User`) | Bir giriş hesabı | Bir kullanıcının birden çok rolü olabilir |
| Rol ataması (`RoleAssignment`) | Kullanıcı + rol + kapsam (kurum veya şube) | Kurucu kurum düzeyinde; diğer roller şube düzeyinde |
| Okul üyeliği (`TenantMembership`) | Kişinin bir okuldaki durumu: etkin / pasif | Pasifleştirme okul başınadır; roller korunur, yeniden etkinleşince geri gelir. Kişi silinmez |
| Yenileme bağlantısı (`PasswordResetToken`) | "Şifremi unuttum" e-postasındaki tek kullanımlık, 30 dakikalık bağlantı | Yalnızca özeti saklanır; şifre değişince geçersizleşir |
| Hesabım | Kişinin kendi profil ve şifre ekranı (spec 0006) | Hesap kişiye aittir, bütün okullarında geçerli; e-posta değiştirilemez |
| Geçici şifre | Kurucunun kişi eklerken ya da sıfırlarken aldığı, bir kez gösterilen şifre | Kurucu kişiye kendisi iletir (e-posta gönderimi SMTP bilgisi girilince, 0006) |
| Veli (`Guardian`) | Veli rolü olan kişi (kullanıcı) + öğrenciyle velilik bağı (`StudentGuardian`: yakınlık Anne / Baba / Diğer; spec 0009) | Tek hesapla tüm çocuklarını görür; yalnızca bağlı olduğu ve ayrılmamış çocukları. Kurucu öğrencinin kartından ekler (yeni kişiye geçici şifre — spec 0005 akışı); bir öğrencinin en fazla 4 velisi |
| Öğrenci kartı (`Student`) | Öğrencinin ana kaydı: ad-soyad, doğum tarihi, okul numarası (okulda tekil), şube (kayıttan sonra değişmez), sınıf (isteğe bağlı, aynı şubenin) | Sağlık notu ve alerji şifreli; silinmez, "ayrıldı" yapılır ve geri alınabilir (spec 0009) |
| Teslim alabilecek kişi (`PickupPerson`) | Öğrenciyi okuldan alabilecek kişi: ad-soyad, yakınlık (serbest metin), telefon | Hesap değil, kartta bir liste; en fazla 10 kişi; veliler kendiliğinden girmez (spec 0009) |
| Kardeş bağlantısı | Aynı veliye bağlı öğrenciler | Ayrı kayıt yok: veli bağından çıkar (bağ kalkınca kardeşlik de kalkar); farklı şubelerde olabilirler |
| Nakil (`Transfer`) | Öğrencinin şube/sınıf değiştirmesi kaydı | Geçmiş korunur |
| Yoklama (`Attendance`) | Bir sınıfın bir günlük devam kaydı (`AttendanceSheet` + öğrenci başına Var / Yok / Geç; spec 0010) | Yalnızca bugün (Türkiye günü) alınır ve aynı gün düzeltilir; ertesi gün kilitlenir. Dönem dışı ve takvimde tatil olan gün okul günü değildir; hafta sonu engellenmez. Ders saati bazlı yoklama ders programıyla gelir |
| Sınıf öğretmeni (`ClassroomTeacher`) | Kurucunun bir sınıfa atadığı, o sınıfın şubesinde Öğretmen rolü olan kişi (spec 0010) | Öğretmen yalnızca atandığı sınıfların yoklamasını alır; bir sınıfın birden çok öğretmeni olabilir. Rolü alınan ya da pasifleştirilen kişinin ataması bir sonraki istekte geçersizdir |
| Veli bildirimi (`GuardianNotice`) | "Yok" kaydedilen öğrencinin okulda etkin her velisine giden uygulama içi bildirim (+ e-posta kuyruğu; spec 0010) | Veli + öğrenci + gün başına bir tane; "Yok" düzeltilince "düzeltildi" olur ve veliye düzeltme bildirimi gider. Yalnızca öğrenci adı, gün, okul adı ve durum içerir |
| Duyuru (`Announcement`) | Kurum/şube/sınıf/öğrenci kapsamlı yayın; okundu bilgisi (spec 0011) | Kurucu her kapsamda, öğretmen yalnızca kendi sınıfı ve o sınıfın öğrencisi; yayınlandıktan sonra değiştirilmez/silinmez. Alıcılar yayın anında belirlenir (`AnnouncementRecipient`, veli başına bir satır); okundu = velinin ayrıntıyı ilk açışı |
| Mesaj (`ConversationMessage`) | Öğretmen-veli birebir yazışma; bir öğrenci bağlamında, öğretmen–veli–öğrenci üçlüsü için tek konuşma (`Conversation`, spec 0011) | Konuşmayı yalnızca iki taraf görür; ilişki bitince eski konuşma okunur, yeni mesaj gönderilemez. Veliler arası grup sohbeti **yok** (kapsam dışı) |
| Onay formu (`ConsentResponse`) | Gezi izni, etkinlik katılımı gibi veli onayı: duyurunun "onay iste" hâli — tek soru, son gün (spec 0011) | Cevap **öğrenci başına** (son cevap geçerli, kim/ne zaman kayıtlı); son gün Türkiye saatiyle 23:59'a kadar. KVKK rızasından (`ConsentRecord`) farklı |
| Mesai saatleri (`QuietHours`) | Öğretmenin tek mesai aralığı (Türkiye saati, 15 dk adım) ve iş günleri (spec 0011) | Tanımsızsa sessiz mod kapalı. Mesai dışında veli mesajı iletilir, öğretmene bildirim gitmez; birikmiş bildirim de sonra gönderilmez (BR-8) |
| Bildirim kapısı | Veliye (ve mesai içinde öğretmene) giden her bildirimin tek çıkışı: Communication'ın `GuardianNotifier`'ı (spec 0011) | Kayıttan sonra çalışır; gönderici hatası işlemi bozmaz. MVP'de gerçek push yok (B-6) |
| Okul bildirimi | Diğer modüllerin kapıdan verdiği öğrenciye özel bildirim (yoklamanın `GuardianNotice`'i; spec 0013: yeni ödev — kayda bağlı, hazır metinli); velinin duyuru akışında "Okul bildirimi" olarak görünür | Yayınlayan "okul"; yalnızca o öğrencinin bağlı velilerine görünür |
| Ödev (`Assignment`) | Öğretmenin atandığı sınıfın tamamına verdiği iş: ders adı (serbest), başlık, isteğe bağlı açıklama, teslim tarihi (spec 0013) | Değiştirilmez, silinmez (yanlışsa yeni ödev); öğrencileri veriliş anındaki sınıf öğrencileridir. Veliye "Okul bildirimi" gider (ders, başlık, teslim) |
| Teslim durumu (`AssignmentSubmission`) | Ödevde öğrenci başına İşaretlenmedi / Teslim etti / Teslim etmedi (spec 0013) | Öğrenci uygulaması yok: öğretmen işaretler; teslim tarihi geçince kendiliğinden "etmedi" olmaz |
| Değerlendirme (`Assessment`) | Sınıf için açılan yazılı ya da sözlü not: ders, tür, başlık, gün (sınıfın dönemi içinde, bugünden sonra değil; spec 0013) | Ortalama, ağırlık, karne, harf notu yok; e-Okul resmî kayıt olarak kalır |
| Puan (`Grade`) | Değerlendirmede öğrencinin 0–100 tam sayı puanı ya da boş ("girilmedi") (spec 0013) | Düzeltilir ve silinir; log ve audit'e değeri yazılmaz |
| Ücret tarifesi (`FeeSchedule`) | Okulun adlandırılmış yıllık tutarı ("2026-2027 Eğitim Ücreti"; spec 0015) | Ad etkinler arasında harf farkı gözetmeden tekil; tutar 0,01–10.000.000,00; pasif tarife yeni planda seçilemez; düzenleme açılmış planları değiştirmez |
| İndirim / burs tanımı (`DiscountDefinition`) | Ad + yüzde (1–100 tam sayı) (spec 0015) | Plana en fazla bir indirim; tutarı tarife × yüzde / 100, kuruşa yarım yukarı. Sabit tutarlı indirim ve birden çok indirim sonra |
| Taksit planı (`InstallmentPlan`) / Taksit (`Installment`) | Öğrenci başına tarife + isteğe bağlı indirim + 1–12 aylık eşit taksit (spec 0015) | Plan açılıştaki tarife ve indirim bilgisini saklar; aynı tarifeden geçerli ikinci plan olmaz; vadeler ilk vadeden aylık aynı gün, yoksa ayın son günü; geçerli tahsilatı yoksa iptal edilir |
| Taksit durumu | Ödendi / Kısmen ödendi / Bekliyor / Gecikti — saklanmaz, Türkiye gününe göre hesaplanır (spec 0015) | **Gecikme:** vade gününün bittiği andan (Türkiye saati, ertesi gün 00:00) itibaren kalan tutar kadar; kısmen ödenmiş taksit de gecikebilir |
| Tahsilat (`Payment`) | Okulda alınan ödemenin elle kaydı: tutar, tarih, yöntem (Nakit / Havale-EFT / okul POS'u), not (spec 0015) | Sağlayıcı yok (B-1); kart ve IBAN bilgisi girilmez. Silinmez, gerekçeyle iptal edilir |
| Cari (`Ledger`) | **Öğrenci başına** borç (geçerli planların neti), geçerli tahsilatlar, kalan, geciken ve tarih sıralı hareket dökümü (spec 0015) | Velinin ekranında çocuklarının carileri ayrı ayrı ve toplamıyla; veli başına birleşik cari sonra |
| Makbuz (`Receipt`) / Makbuz no | Tahsilat belgesi; okul başına 1'den başlayan boşluksuz ve tekrarsız sıra numarası (spec 0015) | Yazdırılabilir web sayfası, "Bu belge fatura yerine geçmez."; iptal edilen makbuz "İPTAL EDİLDİ" kalır, numarası yeniden verilmez. e-Fatura/e-Arşiv Faz 2 |
| Gecikme hatırlatması (`PaymentReminder`) | Vadesi geçmiş taksit için velilere giden otomatik okul bildirimi (spec 0015) | Türkiye saatiyle 10:00 sonrası, vade+1 / +8 / +15. gün, en fazla 3; kaçırılan günler birikmez; ödenen taksit, iptal plan ve ayrılmış öğrenci için gitmez |
| Müsaitlik dilimi (`AvailabilitySlot`) | Öğretmenin açtığı tarihli aralıktan (gün bugün–60 gün, 15 dk adımlı başlangıç/bitiş, en fazla 8 saat) 15/20/30 dakikalık görüşme saati (spec 0016) | Okul başınadır (iki okulun takvimi birbirini görmez); sona kalan kısa parça ve başlamış saat dilim olmaz. Boş dilim kaldırılır; talebi olan kaldırılamaz; öğretmen onaylı randevuyu iptal edince dilim kapanır. Haftalık şablon yok |
| Randevu (`Appointment`) | Velinin, çocuğunun sınıfının öğretmeninin boş dilimi için konulu (≤ 300) talebi (spec 0016): Onay bekliyor → Onaylandı / Reddedildi (+ isteğe bağlı neden) / İptal edildi (veli ya da öğretmen) | Yüz yüze, okulda. "Süresi doldu" saklanmaz: dilim başlayınca yanıtlanmamış talep öyle görünür. "Başlamış" = başlangıç şu an ya da önce (Türkiye saati, sunucu saati). Çocuğun öteki velisi görür, iptali yalnızca talep eden yapar |
| AI taslağı (`AiDraft`) | AI'ın ürettiği, **insan onayı bekleyen** çıktı | Kaynak veriyle birlikte saklanır. Yaşam döngüsü (spec 0012): oluşturulur "bekliyor" (kaynak = sesli notun dökümü, taslak ≤ 1000 karakter) → öğretmen gönderince "onaylandı" (son metin, onaylayan, zaman, gönderilen mesaj) — bir kez; "Vazgeç" kaydı (döküm dahil) siler. Kullanılmamış taslaklar MVP'de silinmez (saklama: Compliance). Spec 0014 haftalık özet: kaynak = dayanak, taslak ≤ 800, öğrenci-hafta başına tek kayıt; yeniden oluşturma bekleyeni günceller; onaylayan sınıfın herhangi bir atanmış öğretmeni olabilir |
| Haftalık gelişim özeti | Sınıf öğretmeninin bir öğrenci-hafta için yerel modelle ürettirip düzelttiği ve onayladığı 3–5 cümlelik özet (spec 0014); veliye "Haftalık gelişim özeti — 16–22 Kasım" okul bildirimi olarak, "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." etiketi ve dayanak satırıyla gider | Dayanak: o haftanın yoklaması, teslim günü o haftada olan ödevler, günü o haftada olan notlar ve öğretmenin isteğe bağlı notu (≤ 500). Öğrenci-hafta başına bir özet; gönderilmiş özet düzeltilmez/geri çekilmez. Zamanlanmış ya da sınıfa toplu üretim yok |
| Okul haftası (`SchoolWeek`) | Türkiye günüyle Pazartesi 00:00 dahil – sonraki Pazartesi 00:00 hariç (spec 0014) | "Bu hafta" bugünü kapsayan, "Geçen hafta" ondan önceki Pazartesi–Pazar; aralık metni "16–22 Kasım" / "30 Kasım – 6 Aralık" |
| KVKK rıza kaydı (`ConsentRecord`) | Aydınlatma metni sürümü + açık rıza + zaman | Fotoğraf rızası öğrenci bazlı |
| Audit kaydı (`AuditEntry`) | Kim, neyi, hangi kayıtta, ne zaman, hangi IP | Değiştirilemez, silinemez |

## Business rules
- **BR-1:** Bir kişi birden fazla role sahip olabilir. Kurucu kurum düzeyinde, diğer roller şube düzeyinde atanır.
- **BR-2:** Veli tek hesapla tüm çocuklarını görür, çocuklar farklı şubelerde olsa bile.
- **BR-3:** Bir öğrencinin birden fazla velisi olabilir.
- **BR-4:** Her veri bir kuruma aittir; hiçbir sorgu kurum sınırını geçemez. Kurum içinde kullanıcı yalnızca yetkili olduğu şubelerin verisini görür.
- **BR-5:** AI hiçbir mesajı onaysız veliye göndermez: AI çıktısı önce `AiDraft` olur, bir insan onaylar.
- **BR-6:** Her AI çıktısında dayandığı kaynak veri görünür.
- **BR-7:** Para tutarları TRY ve `decimal`; taksit bölmede kuruş farkı son taksite eklenir (taksitlerin toplamı = ana tutar).
- **BR-8:** Mesai dışı sessiz mod: öğretmenin tanımladığı saatler dışında veli mesajı iletilir ama öğretmene bildirim gitmez.
- **BR-10 (spec 0005):** Şube silinmez, pasifleştirilir; okulun son etkin şubesi pasifleştirilemez; aynı okulda iki etkin şube aynı adı (büyük/küçük harf duyarsız) taşıyamaz. Pasif şube atamada seçilemez ve kurucu dışındakilere görünmez.
- **BR-11 (spec 0005):** Bir okulda en az bir etkin kurucu/yönetici kalır; kurucu kendi yönetici rolünü alamaz ve kendini pasifleştiremez.
- **BR-12 (spec 0008):** Aynı okulda iki dönemin günleri çakışamaz (bir dönemin bitiş günü öbürünün başlangıç günüyse çakışır); dönem en fazla 366 gün sürer; dönem silinmez ve pasifleştirilmez; tarih düzenlemesi takvim kayıtlarını dönemin dışında bırakamaz; takvim kaydının iki günü de dönemin içindedir.
- **BR-13 (spec 0008):** Sınıf bir döneme, bir şubeye ve bir seviyeye bağlıdır; dönemi ve şubesi sonradan değişmez; aynı dönem ve şubede aynı adlı (harf farkı gözetmeden) iki etkin sınıf, aynı okulda aynı adlı iki etkin seviye olamaz; seviye ve sınıf silinmez, pasifleştirilir; pasif şube ve pasif seviye yeni sınıfta seçilemez. Kurucu olmayan kişi yalnızca atandığı etkin şubelerin sınıflarını görür.
- **BR-14 (spec 0009):** Veli yalnızca kendisine bağlı ve "ayrıldı" olmayan öğrencileri görür; aynı şubedeki başka öğrenci ona "bulunamadı"dır (veli rolü şube düzeyinde olsa da). Öğrencinin şubesi kayıttan sonra değişmez (nakil ayrı spec); okul numarası okulda harf farkı gözetmeden tekildir (ayrılan öğrenci numarasını tutar); bir öğrencinin en fazla 4 velisi, 10 teslim alabilecek kişisi olur; öğrenci silinmez.
- **BR-9:** Bir öğrenci bir dönemde tek bir şubede ve tek bir sınıfta kayıtlıdır; şube değişikliği nakil kaydıdır, geçmiş korunur.
- **BR-15 (spec 0011):** Duyurunun alıcıları yayın anında kapsamdaki öğrencilerin okulda etkin velileridir (sonradan sınıfa giren öğrencinin velisi o duyuruyu görmez); okundu veli başına sayılır. Onay formu cevabı öğrenci başınadır: öğrencinin herhangi bir velisi verir, son güne kadar değiştirir, son cevap geçerlidir. Alıcılar yayın anında sabitlenir: bağı sonradan kaldırılan veli, daha önce aldığı öğrenci kapsamlı duyurunun metnini görmeye devam eder ama o öğrenci için onay cevabı veremez (cevap için güncel bağ gerekir).
- **BR-17 (spec 0014):** Haftalık gelişim özetini yalnızca öğrencinin bugünkü sınıfına atanmış öğretmen üretir ve gönderir; öğrenci-hafta başına bir özet gönderilir (ikinci gönderim ve gönderilmiş haftada yeniden üretim reddedilir, eşzamanlı iki gönderimde tek özet gider); öğrencinin okulda etkin velisi yoksa gönderilmez; veli yalnızca kendi bağlı çocuklarının özetini görür (farklı şubedeki kardeş dahil). Dayanak yoksa özet üretilmez; model yalnızca verilen veriye dayanır, öğretmen onaylamadan hiçbir şey gitmez (BR-5).
- **BR-16 (spec 0013):** Ödevin ve değerlendirmenin öğrencileri veriliş/açılış anındaki sınıf öğrencileridir (sonradan gelen öğrenci o kayıtlarda yoktur; sınıftan ayrılanın kaydı silinmez, velinin özetinde kalır). Teslim durumunu ve puanı yalnızca kaydı oluşturan öğretmen, o sınıfa hâlâ atanmışken değiştirir; sınıfın diğer öğretmenleri yalnızca görür. Ödev değiştirilmez ve silinmez; puan 0–100 tam sayı ya da boştur.
- **BR-19 (spec 0016):** Bir dilimde aynı anda tek açık (onay bekleyen ya da onaylı) randevu olur; aynı çocuk ve aynı öğretmen için aynı anda en fazla bir açık ve başlamamış randevu olur (çocuğun hangi velisi isterse istesin). Öğretmenin başlamamış dilimleriyle çakışan aralık açılamaz (bitişik olabilir). Ret ve veli iptali dilimi yeniden boşaltır; öğretmen iptali kapatır; başlamış randevu yanıtlanamaz ve iptal edilemez. Veli yalnızca çocuğunun sınıfının öğretmeninden randevu alır.
- **BR-18 (spec 0015):** Tahsilat vadesi en eski ödenmemiş taksitten başlayarak dağıtılır (kısmi ödeme olur); kalan borçtan fazla tahsilat reddedilir (avans/alacak yok). Tahsilat silinmez, gerekçeyle iptal edilir ve dağılımı geri alınır. İptal yalnızca o tahsilatın dağılımını geri alır; sonraki tahsilatlar yeniden dağıtılmaz — bu yüzden önceki bir taksit "Bekliyor" iken sonraki "Kısmen ödendi" görünebilir; toplamlar ve geciken tutar yine doğrudur (spec 0015 C-4). Plan geçerli tahsilatı yoksa iptal edilir; iptal plan cariye, gecikmeye ve hatırlatmaya girmez. Cari öğrenci başınadır. Tarife/indirimi yalnızca kurucu tanımlar; muhasebe yalnızca Muhasebe rolü olduğu şubelerin öğrencileri için plan, tahsilat, iptal, makbuz ve cari işler; veli mobilde yalnızca kendi çocuklarının ödeme durumunu salt okunur görür.

## Key domain invariants
- Bir kurumun verisi başka bir kuruma **asla** görünmez (BR-4).
- Veliye giden hiçbir AI çıktısı insan onayı olmadan gönderilmez (BR-5).
- Çocuklara dair veriler model eğitiminde kullanılmaz.
- Taksitlerin toplamı her zaman ana tutara eşittir (BR-7).
- Her taksit en az 0,01 TL'dir; net tutar < taksit sayısı × 0,01 TL ise plan reddedilir ("Taksit tutarı 0,01 TL'den az olamaz; taksit sayısını azaltın." — spec 0015, review tur 1).
- Audit kayıtları değiştirilemez ve silinemez.

## Sensitive areas
Bu alanlara dokunan spec, Constraints bölümüne `Sensitive area: <ad>` yazar (lite modda da clarify kapısı,
her düzeltme turunda yeniden review, her kriter kasıtlı bir bozmayla kırmızıya düşürülerek ispat).

- **S1 — Kiracı ve şube izolasyonu:** kurum/şube sınırı, sorgu filtreleri, yetkili şube kontrolü.
- **S2 — Kişisel ve özel nitelikli veri (öğrenci, veli, personel):** TC kimlik no, telefon, e-posta, adres, doğum tarihi, ad-soyad + öğrenci eşleşmesi, sağlık notu, alerji, fotoğraf, teslim alabilecek kişiler.
- **S3 — Kimlik, rol, yetki ve gizli bilgiler:** şifre (yalnızca hash), 2FA sırları, oturum/JWT/refresh token'ları, API anahtarları, rol ataması, veli-öğrenci bağı.
- **S4 — Para ve finansal veri:** taksit, indirim/burs, tahsilat, makbuz, cari, IBAN; kart bilgisi sistemde hiç tutulmaz.
- **S5 — Veliye giden iletişim ve AI çıktıları:** duyuru, mesaj, AI taslağı, otomatik bildirim.
- **S6 — KVKK rıza, audit log ve saklama süresi:** rıza kayıtları, erişim logu, anonimleştirme.
