# 0057 — /new-feature Giriş ve roller + kurum/şube · ANALYZE: amaç ve netleştirme soruları

**Tarih:** 2026-11-19 23:31

## İstek

> /new-feature Giriş ve roller + kurum/şube

## Cevap

### Durum
- Mod **lite**.
- Bu iş için açık bir spec yok; yeni numara **0004** olacak.
- Yol haritasında (`docs/roadmap.md`) 1. sıradaki iş.

### Bu iş "hassas alan"da
`docs/domain.md`'deki hassas alan listesine göre bu iş dört alana dokunuyor:
- **S1 — Kiracı ve şube izolasyonu:** bir okulun verisi başka okula asla görünmez; okul içinde kullanıcı yalnızca yetkili olduğu şubeyi görür.
- **S3 — Kimlik, rol, yetki ve gizli bilgiler:** şifre (yalnızca hash), oturum anahtarları, rol ataması.
- **S2 — Kişisel veri:** kullanıcının adı, e-postası, telefonu.
- **S6 — Audit kaydı:** kim, neyi, ne zaman yaptı.

Bu yüzden ANEW kuralı gereği spec yazılmadan önce sorular **ayrıca sorulur**. Lite modda bile bu adım atlanmaz. Ayrıca her düzeltme turundan sonra yeniden inceleme yapılır ve her kriter kasıtlı bir bozmayla ispatlanır.

### Amaç taslağı (iş dilinde)
Okulun kurucusu sistemde kendi okulunu (kurum) ve şubelerini tanımlamak, çalışanlarını ve velileri kullanıcı olarak ekleyip her birine rol vermek istiyor: kurucu/yönetici, öğretmen, veli, muhasebe/idari. Herkes kendi hesabıyla web'e (öğretmen ve veli mobile de) giriş yapar ve yalnızca kendi okulunun, kendi yetkili olduğu şubelerin verisini görür.

**Başarı:**
- Kurucu iki şubeli bir okul açıp bir öğretmen ekleyebiliyor.
- O öğretmen yalnızca kendi şubesini görüyor.
- Başka bir okulun kullanıcısı bu okula ait hiçbir şeyi göremiyor — denense bile.

**Bu iş yapmayacak:** sınıf/dönem, öğrenci/veli-çocuk bağı, KVKK rıza ekranı, e-posta/SMS gönderimi, Google/Microsoft ile giriş. Bunlar sonraki işlerde.

### Netleştirme soruları (önem sırasıyla; her birinde önerim var)

**1. İlk okul ve ilk kurucu hesabı nasıl oluşacak?**
Sistemde henüz hiç okul yok; birinin ilkini açması lazım.
- a) Senin çalıştırdığın bir komutla (sunucuda/Pi'de): "kurum adı + kurucu e-postası" verilir, kurum ve kurucu hesabı açılır.
- b) Herkese açık bir "okulunu kaydet" sayfası.
- c) Ayrı bir "platform yöneticisi" paneli.

**Önerim: a.** MVP'de satış birebir yapılıyor; açık kayıt sayfası saldırı yüzeyi ve sahte kayıt demek. Platform paneli ayrı bir ürün kadar iş. Komut basit ve güvenli.

**2. Giriş neyle yapılacak?**
- a) E-posta + şifre.
- b) Telefon + şifre.
- c) Telefon + SMS kodu.

**Önerim: a, herkes için.** SMS sağlayıcımız yok (SMS şimdilik yalnızca log'a yazılıyor), o yüzden c şimdilik mümkün değil. Veliler için telefonla giriş sonra eklenebilir. Telefon numarası yine de kullanıcı kartında tutulur.

**3. Kullanıcıyı kim ekler, ilk şifresini nasıl alır, şifresini unutursa ne olur?**
E-posta gönderme altyapımız yok.
- **Önerim:**
  - Kurucu web'den kullanıcıyı ekler: ad-soyad, e-posta, telefon, rol, şube.
  - Sistem **tek kullanımlık geçici bir şifre** üretir ve ekranda **bir kez** gösterir. Kurucu bunu kişiye kendisi iletir.
  - Kişi ilk girişte şifresini değiştirmek zorundadır.
  - Şifresini unutan kişi için kurucu "şifreyi sıfırla" der; yine bir kez gösterilen yeni geçici şifre üretilir.
  - E-postayla şifre sıfırlama, e-posta altyapısı gelince eklenir.

**4. Bir kişi iki farklı okulda hesap sahibi olabilir mi?**
Örnek: çocukları iki ayrı okulda olan veli, ya da iki okulda ders veren öğretmen.
- a) Hesap okula bağlı: aynı e-posta iki okulda iki ayrı hesap olur, girişte okul seçilir ya da okul adresi/kodu girilir.
- b) Tek hesap, birden çok okul: girişten sonra okul seçilir.

**Önerim: a.** Okullar arası duvar ("bir okulun verisi diğerine asla görünmez") en basit ve en güvenli hâliyle kalır. Bu durum MVP'de nadir. Aynı okul içinde birden çok rol (hem öğretmen hem veli) zaten serbest.

**5. İki adımlı doğrulama (2FA — telefondaki doğrulama uygulamasıyla 6 haneli kod) bu işte mi?**
Güvenlik kurallarımız (`docs/security.md`) kurucu/yönetici ve muhasebe için 2FA'yı **zorunlu** diyor.
- **Önerim:** 2FA'yı **hemen ardından gelen ayrı, küçük bir iş** yapmak; bu işi sade tutmak.
  - Pi şu an yalnızca ev ağında ve gerçek veri yok; risk düşük.
  - Kural değişmiyor. "Pilot okula açılmadan önce 2FA zorunlu" diye yol haritasına ve backlog'a yazılır.

**6. Mobilde giriş bu işte mi?**
- **Önerim: evet, giriş ekranı web ve mobilde.**
  - Girişten sonra iki tarafta da basit bir "Hoş geldin, <ad> — okul, rollerin ve şubelerin" ekranı ve "çıkış" düğmesi.
  - Yönetim ekranları (şube ekle, kullanıcı ekle, rol ver) **yalnızca web'de**; kapsam da kurucuya web'i veriyor.

**7. Hangi olaylar audit kaydına girecek?**
Güvenlik kurallarına göre rol değişiklikleri audit'e girmek zorunda. Audit kaydı değiştirilemez, silinemez.
- **Önerim:**
  - Başarılı giriş, başarısız giriş, hesap kilitlenmesi.
  - Kullanıcı ekleme, rol/şube verme ve alma, şifre sıfırlama.
  - Kurum ve şube ekleme/değiştirme.
- Her kayıtta: kim, ne, hangi kayıt, ne zaman, hangi IP.
- Audit kayıtlarını görüntüleme ekranı sonra; bu işte yalnızca yazılır.

### Sorulmadan varsaydığım ve spec'e yazacaklarım (itirazın varsa söyle)
- **Yanlış şifre kilidi:** 5 yanlış denemede hesap 15 dakika kilitlenir. Giriş uç noktasında hız sınırı olur.
- **Oturum:** erişim anahtarı 15 dakika geçerli, yenileme anahtarıyla uzar (güvenlik kuralı); çıkışta iptal edilir.
- **Şifre kuralı:** en az 10 karakter. Karmaşıklık zorlaması yok; çok bilinen şifreler reddedilir.
- **Başka okulun ya da yetkisiz şubenin kaydı istenirse** "bulunamadı" denir (varlığı da sızdırılmaz).
- **Kurucu** tüm şubeleri görür; diğer roller yalnızca atandıkları şubeleri görür.

### Karar
Yedi soruya cevabın; önerilerimle aynıysa "önerilerle devam" demen yeter.
