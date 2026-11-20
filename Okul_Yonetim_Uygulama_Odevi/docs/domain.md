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
| Sınıf (`Classroom`) | Bir şubede, bir seviyedeki öğrenci grubu | "Bahçelievler 5-A" ≠ "Avcılar 5-A" — sınıf her zaman bir şubeye bağlı |
| Şube-sınıf eşleşmesi | Bir dönemde hangi şubede hangi seviyede hangi sınıfların açık olduğu | — |
| Dönem (`Term`) | Eğitim yılı içindeki dönem | "2026-2027 Güz"; akademik takvim döneme bağlı |
| Ders programı (`Timetable`) | Sınıf bazlı haftalık ders düzeni | Elle giriş + Excel içe aktarma |
| Kullanıcı (`User`) | Bir giriş hesabı | Bir kullanıcının birden çok rolü olabilir |
| Rol ataması (`RoleAssignment`) | Kullanıcı + rol + kapsam (kurum veya şube) | Kurucu kurum düzeyinde; diğer roller şube düzeyinde |
| Okul üyeliği (`TenantMembership`) | Kişinin bir okuldaki durumu: etkin / pasif | Pasifleştirme okul başınadır; roller korunur, yeniden etkinleşince geri gelir. Kişi silinmez |
| Yenileme bağlantısı (`PasswordResetToken`) | "Şifremi unuttum" e-postasındaki tek kullanımlık, 30 dakikalık bağlantı | Yalnızca özeti saklanır; şifre değişince geçersizleşir |
| Hesabım | Kişinin kendi profil ve şifre ekranı (spec 0006) | Hesap kişiye aittir, bütün okullarında geçerli; e-posta değiştirilemez |
| Geçici şifre | Kurucunun kişi eklerken ya da sıfırlarken aldığı, bir kez gösterilen şifre | Kurucu kişiye kendisi iletir (e-posta gönderimi SMTP bilgisi girilince, 0006) |
| Veli (`Guardian`) | Öğrenciyle velilik bağı olan kişi | Tek hesapla tüm çocuklarını görür |
| Öğrenci kartı (`Student`) | Öğrencinin ana kaydı | Sağlık notu, alerji, teslim alabilecek kişiler dahil |
| Kardeş bağlantısı | Aynı veliye bağlı öğrenciler | Farklı şubelerde olabilirler |
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
