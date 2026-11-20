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
| Yoklama (`Attendance`) | Ders/gün bazlı devam kaydı | Devamsızlıkta veliye otomatik bildirim |
| Duyuru (`Announcement`) | Kurum/şube/sınıf/öğrenci kapsamlı yayın; okundu bilgisi | — |
| Mesaj (`Message`) | Öğretmen-veli birebir yazışma | Veliler arası grup sohbeti **yok** (kapsam dışı) |
| Onay formu (`ConsentForm`) | Gezi izni, etkinlik katılımı gibi veli onayı | KVKK rızasından (`ConsentRecord`) farklı |
| Ödev / Not (`Assignment` / `Grade`) | Ödev, teslim durumu; yazılı/sözlü not | — |
| Ücret tarifesi / Taksit planı (`FeeSchedule` / `InstallmentPlan`) | Yıllık ücret ve taksitlere bölünmesi | İndirim ve burs tanımları dahil |
| Tahsilat (`Payment`) | Alınan ödeme | MVP'de manuel kayıt (sağlayıcı yok) |
| Cari (`Ledger`) | Velinin borç/alacak hesabı | — |
| Makbuz (`Receipt`) | Tahsilat belgesi | e-Fatura/e-Arşiv Faz 2 |
| Randevu (`Appointment`) | Öğretmen müsaitliğinden veli talebi | — |
| AI taslağı (`AiDraft`) | AI'ın ürettiği, **insan onayı bekleyen** çıktı | Kaynak veriyle birlikte saklanır |
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

## Key domain invariants
- Bir kurumun verisi başka bir kuruma **asla** görünmez (BR-4).
- Veliye giden hiçbir AI çıktısı insan onayı olmadan gönderilmez (BR-5).
- Çocuklara dair veriler model eğitiminde kullanılmaz.
- Taksitlerin toplamı her zaman ana tutara eşittir (BR-7).
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
