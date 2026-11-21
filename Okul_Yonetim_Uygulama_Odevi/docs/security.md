# Security

> Her plan ve review'da uyulması zorunlu temel kurallar. Bootstrap 2026-11-19. Hassas alanlar S1–S6: `docs/domain.md`.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Geliştirmede `dotnet user-secrets` (repo dışında, kullanıcı profilinde); sunucuda ortam değişkeni / secret store.
  Repoda yalnızca değerleri boş `appsettings.Example.json` / `.env.example`. Bu kapsam: DB bağlantı cümlesi,
  JWT imza anahtarı, Data Protection anahtarları, HMAC anahtarı, ileride sağlayıcı API anahtarları.
- Yerel PostgreSQL süper kullanıcı şifresi yalnızca geliştirme makinesindedir; repoya yazılmaz.
- Veritabanı kullanıcısı (karar: plan 0001, Risks 5): yerel geliştirme ve testler `postgres` kullanıcısıyla çalışır
  (test veritabanı oluşturup silmek için); ayrı, kısıtlı bir uygulama kullanıcısı açılmadı. Bilgi yalnızca
  user-secrets'ta (`WebAPI`: `ConnectionStrings:OkulDb`, `tests/Integration`: `ConnectionStrings:TestServer`).
  Pilot/üretim ortamında uygulama süper kullanıcıyla çalıştırılmaz.
- **Raspberry Pi deneme ortamı (spec 0003):** uygulama `okul_app` ile bağlanır — süper kullanıcı değil, veritabanı/rol
  açamaz, `pg_hba.conf`'taki işaretli blokla yalnızca `okul` veritabanına ve yalnızca `127.0.0.1`'den bağlanabilir;
  `okul` veritabanının sahibidir (migration'ları açılışta uygular). Şifre Pi'de üretilir (`openssl rand`), psql'e
  stdin'den verilir, yalnızca `/etc/okul/okul-api.env` (root:root 600) içinde durur; oturum imza anahtarı ayrı
  `/etc/okul/okul-jwt.env` (root:root 600) dosyasında, yalnızca yoksa üretilir (spec 0004); systemd servise ortam değişkeni
  olarak verir. Betiklerde `set -x` yasak, şifre ekrana basılmaz (`tools/check/pi-config.mjs` denetler — sezgisel bir
  denetimdir: şifre taşıyan değişken adları ve şifre dosyasını okuyan komutlar aranır; asıl güvence review ve VERIFY'daki
  çıktı/log taramasıdır). Şifreli SQL'de sunucu komut logu oturum için kapatılır. Root'un geçici dosyaları tahmin
  edilemez adlıdır (`mktemp`), dağıtım hazırlık klasörünün sahibi ve bağlantı olmadığı doğrulanır. API root
  olmayan `okul` sistem kullanıcısıyla, sertleştirilmiş systemd ayarlarıyla ve yalnızca `127.0.0.1`'de çalışır.

## Sensitive data protection
1. **Şifre:** ASP.NET Core Identity `PasswordHasher` (PBKDF2) veya Argon2id ile hash; düz metin hiçbir yerde (DB, log, hata, yanıt) yok.
2. **TC kimlik no, sağlık notu, alerji, IBAN:** alan düzeyinde şifreli (AES-GCM, repo dışında anahtar). TC araması ayrı HMAC parmak izi kolonu ile.
   **Karar (plan 0009 Risks 2, R-10 kapanışı):** anahtar ASP.NET Core Data Protection anahtar halkasıyla değil, spec 0007'deki
   gibi **ortam ayarındaki 32 baytlık AES-GCM anahtarıyla** verilir (Data Protection halkası Pi'de kalıcı ve yedekli dizin
   ister — B-16). Kişisel veri anahtarı iki adımlı doğrulama anahtarından **ayrıdır** (`PersonalData:EncryptionKey`; Pi'de
   `/etc/okul/okul-data.env`, root 600, yalnızca yoksa üretilir): biri sızarsa öteki korunur. Ortak çekirdek
   `AesGcmCipher` (rastgele 12 baytlık nonce + 16 baytlık etiket). Anahtar yoksa ya da bozuksa uygulama açılır; öğrenci kartı
   çalışır, sağlık alanları "gösterilemiyor", dolu sağlık bilgisi kaydetme reddedilir (sunucu logunda "yapılandırılmamış").
   **Anahtar kaybı = kayıtlı sağlık bilgisi okunamaz:** anahtar dosyası veritabanı yedeğiyle birlikte ama ayrı yerde
   saklanmalı (B-14). Sağlık metni log, hata yanıtı ve audit kaydına yazılmaz; komutları `ILoggableRequest` değildir (T9).
3. **Telefon, e-posta, adres:** DB'de düz (iletişim için gerekli); loglarda maskeli; yalnızca yetkili rollere görünür.
4. **Kart bilgisi:** sistemde **hiç** saklanmaz ve sistemden geçmez; sağlayıcının barındırılan ödeme sayfası/token'ı (PCI-DSS kapsamı dışı).
5. **Loglar:** Serilog maskeleme kuralları; ham istek/yanıt gövdesi loglanmaz; S2/S3 verisi loga yazılmaz.
6. **Dosya/fotoğraf:** herkese açık URL yok; kısa ömürlü imzalı bağlantı. Fotoğraf paylaşımında öğrenci bazlı rıza kontrolü.

## Input & output
1. Her API girdisi sınırda FluentValidation ile doğrulanır (nArchitecture validation pipeline).
2. SQL yalnızca EF Core (parametreli); ham SQL/`IgnoreQueryFilters` yalnızca izinli listede (T5).
3. Excel/e-Okul içe aktarma: dosya boyutu ve tip sınırı; formül hücreleri değer olarak okunur. Dışa aktarma: `=`, `+`, `-`, `@` ile başlayan hücreler kaçışlanır (CSV/formül enjeksiyonu).
4. Hata yanıtlarında stack trace, SQL, kişisel veri yok (`docs/conventions.md` Error handling).
4a. Web'de tek satır içi betik var: tema ön betiği (`web/index.html`, spec 0002) — yalnızca tarayıcıdaki tema seçimini okur,
    ağ isteği yapmaz. İçerik Güvenlik Politikası (CSP) eklendiğinde bu betik `script-src` için özeti (hash) ile izinlenir;
    `unsafe-inline` açılmaz.
5. AI'a yalnızca görev için gereken alanlar gönderilir (veri minimizasyonu); sağlayıcı sözleşmesinde "eğitimde kullanılmaz" şartı zorunlu.
6. **Yerel yapay zekâ (spec 0012):** MVP'de dış (bulut) AI servisi yok; metin (Ollama `gemma3:4b`) ve ses→metin
   (faster-whisper, `tools/ai/stt_server.py`) bu Windows bilgisayarda çalışır — "eğitimde kullanılmaz" şartı yerel çalıştığı
   için kendiliğinden sağlanır. API model adreslerini yalnızca ayardan alır ve **yalnızca yerel adresleri** kabul eder
   (`localhost`, `127.x`, `10.x`, `172.16–31.x`, `192.168.x`); başka adres yazılırsa özellik kapalı kalır. Varsayılan
   (Pi/üretim) ayarlarda adresler boştur. Modele yalnızca sabit talimat ve sesli notun dökümü gider (öğrenci/veli adı, sağlık
   bilgisi gitmez). **Ses saklanmaz:** API sesi bellekte tutar (form tamponu eşiği istek sınırının üstünde — geçici dosya yok),
   ses servisi `BytesIO`'da işler; veritabanında ses kolonu yok. Döküm (kaynak) taslakla birlikte `ai_drafts`'ta saklanır;
   döküm, taslak ve mesaj metni loglanmaz (yalnızca kimlik, süre, karakter sayısı, ölçülen süreler).
   **Yerel ağ demosu (Pi):** yalnızca demo süresince Ollama (`OLLAMA_HOST=0.0.0.0`) ve ses servisi (`-ListenHost 0.0.0.0`)
   ağa açılır; ikisi de kimlik doğrulamasızdır, bu yüzden Windows güvenlik duvarı kuralı 11434 ve 8765 portlarına **yalnızca
   Pi'nin adresinden** (`192.168.1.102`) izin verir; trafik düz HTTP'dir ve okulun yerel ağından çıkmaz (B-13 ile aynı sınır).
   Windows'un `ollama.exe`/`python.exe` için daha önce oluşturduğu program izin kuralları tüm adreslere açıktır ve dar kuralı
   boşa çıkarır: demo öncesi bulunup (`Get-NetFirewallApplicationFilter` → `Get-NetFirewallRule`) kapatılır ve Pi dışındaki
   bir cihazdan 11434 ile 8765'in **kapalı** olduğu denenir (`Test-NetConnection`); açıksa demo başlamaz. Demo bitince servisler yeniden yalnızca `127.0.0.1`'e döner ve kural kaldırılır (`tools/ai/README.md`).

## AuthN / AuthZ
1. **Varsayılan: reddet.** Her uç nokta kimlik doğrulama ister (genel yetki kuralı); anonim uçlar açıkça listelenir ve
   bir test listeyi denetler (spec 0004: `system-health`, `auth/login`, `auth/refresh`).
2. **Oturum (spec 0004):** JWT erişim anahtarı 15 dk (HS256, `sub`/`sid`/`tid`); yenileme anahtarı 32 rastgele bayt,
   veritabanında yalnızca SHA-256 özeti, her kullanımda döner, 7 gün kullanılmazsa düşer. Her istekte oturum
   veritabanından denetlenir (çıkış/iptal anında etkili). Aynı e-postanın girişleri ve aynı anahtarın yenilemeleri
   veritabanı kilidiyle sıraya girer (eşzamanlı isteklerle kilit atlatılamaz). Döndürülen eski anahtar 30 sn geçer
   (birden çok sekme aynı anda yeniler); daha sonra gelirse çalınmış sayılır: oturum iptal, audit `RefreshTokenReused`.
   Bilinen sınırlar: (a) tolerans içinde kullanılan çalıntı eski anahtar en fazla bir erişim anahtarı süresi (15 dk)
   erişim sağlar, süre uzatılamaz; (b) mobilde yenileme yanıtı yolda kaybolursa sonraki yenileme oturumu kapatır ve
   audit'e yanlış bir "çalıntı anahtar" kaydı düşer (B-19). Sıra beklemesi anahtar başına en fazla 5 sn (yönetimde okul + kişi anahtarı: toplam ~10 sn); aşılırsa "Çok fazla istek". Web yenileme anahtarını **HttpOnly, SameSite=Strict çerezde**
   (`/api/auth` yolu) tutar, JavaScript görmez; mobil telefonun **güvenli deposunda** (`expo-secure-store`). Erişim
   anahtarı yalnızca bellekte. Şifre özeti ASP.NET Identity PBKDF2; kullanıcı yoksa da özet doğrulanır (zamanlama farkı
   yok). Uygulama kendi küçük oturum kodunu kullanır (`NArchitecture.Core.Security` kullanılmadı — plan 0004).
3. **2FA (TOTP):** Kurucu/Yönetici ve Muhasebe için **zorunlu**; Öğretmen ve Veli için isteğe bağlı. **MVP demosuna kadar
   ertelendi** (kullanıcı kararı 2026-11-19, spec 0004: "önce mvp olarak okullara gösterip fikir alalım"); gerçek okul
   verisiyle pilota açılmadan önce zorunlu (backlog B-17). Çok bilinen şifre listesi de aynı adıma ertelendi; MVP'de şifre
   kuralı en az 8 karakter. İstisna: platform yöneticisi için iki adımlı doğrulama **zorunlu** ve yapıldı (spec 0007, madde 10).
4. **Üç katmanlı yetki:** (a) rol (`ISecuredRequest`), (b) kurum + şube (oturumdan, global query filter), (c) ilişki: veli yalnızca kendi çocuğunu, öğretmen yalnızca kendi sınıflarını görür.
5. Giriş denemesi hız sınırı (IP başına dakikada 20) + hesap kilitleme (3 hata → 15 dk; bilinmeyen e-posta da aynı
   yanıtı ve kilidi alır, hesap varlığı sızdırılmaz). Giriş/çıkış/kilit/okul seçimi audit'e yazılır.
6. **Audit log:** öğrenci kartı görüntüleme/değiştirme, sağlık verisi okuma, tahsilat, rol ve rıza değişiklikleri — kim, ne, hangi kayıt, ne zaman, IP. Yalnızca eklenir; değiştirilemez/silinemez.
7. Kiracı/şube dışı kayıt → 404 (varlık bilgisi sızdırılmaz).
8. **Yönetim (spec 0005):** yalnızca seçili okulun kurucusu/yöneticisi (`TenantFounder`, değilse 403). Geçici şifre
   12 karakter (harf+rakam, 0/O/1/l/I yok), kriptografik rastgele; yalnızca ekleme/sıfırlama yanıtında bir kez döner,
   log/audit'te yok, veritabanında yalnızca özet. Kurucu şifre sıfırlaması kişinin bütün oturumlarını kapatır; kişi
   başka okulda da kayıtlıysa yapılamaz (bir okulun yöneticisi öbür okuldaki erişimi ele geçiremez); aynı kişinin ad ve
   telefonu da o durumda düzenlenemez (ortak bilgi). Rol/üyelik/şube değiştiren işlemler sırası gelince istek sahibinin
   hâlâ yönetici olduğunu yeniden denetler (değilse 403); sıfırlama ve profil kişi başına da sıraya girer. Kendi yönetici
   rolünü alma / kendini pasifleştirme yok; okulda en az bir etkin yönetici kalır (eşzamanlı isteklerde de — okul
   başına kilit). Pasif üyelik o okulda girişi, okul seçimini ve oturum yenilemeyi hemen keser. Her yönetim işlemi
   audit'e yazılır (kim, ne, hangi kayıt, ne zaman, IP).
9. **Hesabım ve şifre işlemleri (spec 0006):** şifre değiştirmede mevcut şifre zorunlu; yanlış mevcut şifre girişle
   aynı kilit sayacına eklenir (3 → 15 dk); başarıda kişinin diğer oturumları kapanır. "Şifremi unuttum" her durumda
   aynı yanıtı verir (hesap varlığı sızmaz), e-posta arka planda gider ve kayıtsız e-posta yolu kayıtlıyla aynı
   veritabanı okumalarını yapar (SMTP süresi istekte değil; ek kayıt yazma farkı küçüktür, IP sınırı yavaşlatır); aynı e-postaya 15 dk'da en
   fazla 3 bağlantı; IP başına sınır girişle aynı. Bağlantı 32 rastgele bayt, veritabanında yalnızca SHA-256 özeti,
   30 dk ve tek kullanımlık; yeni bağlantı ve her şifre değişikliği (kendisi, kurucu, bağlantı) açıktakileri geçersiz
   kılar; kişinin şifresini değiştiren üç yol (kendi değişikliği, kurucu sıfırlaması, bağlantı) aynı kişi sırasına
   girer ve kararlarını sıra içinde taze veriyle verir (tek kullanım ve "kurtarma geri alınamaz" eşzamanlı isteklerde de
   geçerli); bağlantıyla yenileme bütün oturumları kapatır ve kilidi açar. SMTP şifresi yalnızca ortam ayarında
   (`Email__Password`); şifre, bağlantı, e-posta adresi ve gövde loglanmaz (yalnızca kişi kimliği).
10. **Platform yöneticisi (spec 0007):** yetki okul rollerinden ayrı, yalnızca komut satırından verilir/alınır
    (`grant-platform-admin`, `revoke-platform-admin`; açık oturumlar kapanır, audit'e komut kaynağıyla). Girişte şifre
    doğruysa oturum değil 5 dk'lık, tek kullanımlık bekleme (yalnızca SHA-256 özeti) döner; oturum doğrulayıcı uygulama
    kodu (RFC 6238, HMAC-SHA1, 6 hane, 30 sn, ±1 dilim) doğrulanınca açılır ve "kodla açıldı" işareti taşır. Yanlış kod
    şifre hatalarıyla aynı kilit sayacına eklenir (3 → 15 dk); aynı kod (dilim) ikinci kez kabul edilmez. Doğrulama
    anahtarı veritabanında AES-GCM ile şifreli; şifreleme anahtarı yalnızca ortam ayarında (`TwoFactor__EncryptionKey`,
    32 bayt; Pi'de `/etc/okul/okul-2fa.env`, root 600). Anahtar yoksa platform yöneticisi girişi reddedilir, diğer
    girişler etkilenmez. Panel yetkisi her istekte veritabanından okunur (yetki alınınca bir sonraki istek reddedilir).
    Panel okulların içine erişmez; yalnızca özet (ad, tarih, durum, sayılar, kurucu ad + e-posta). Doğrulama anahtarı, 6 haneli
    kodlar, geçici şifre ve bekleme anahtarı log, hata yanıtı ve audit kaydında geçmez. Kurtarma: `reset-two-factor` komutu.
11. **Öğrenci ve veli (spec 0009):** öğrenci yönetimi yalnızca seçili okulun kurucusu/yöneticisi (`TenantFounder` + sıra
    içinde yeniden denetim); "Çocuklarım" yalnızca seçili okulda veli rolü olan kişi, öğrenciyi yalnızca kendi bağı
    üzerinden görür (ayrılan ya da bağı kalkan öğrenci bir sonraki istekte 404). Veli bağlama spec 0005'in hesap
    kurallarını kullanır (yeni kişiye bir kez gösterilen geçici şifre; var olan kişinin şifresi ve adı değişmez; bu okulda
    pasif kişi bağlanamaz). Öğrenci ekleme/düzenleme/ayrıldı/geri al, kart görüntüleme (kurucu ve veli), sağlık okuma ve
    değişikliği, veli bağlama/kaldırma ve teslim kişisi değişikliği audit'e yazılır; loglarda ve audit'te yalnızca
    kimlikler (öğrenci adı, sağlık metni, geçici şifre yok). KVKK veli rıza akışı bu spec'te yok — gerçek veriyle pilottan
    önce zorunlu (`docs/roadmap.md`).
12. **Yoklama ve veli bildirimi (spec 0010):** öğretmen-sınıf ilişkisi her istekte veritabanından denetlenir — Öğretmen rolü
    yoksa 403; sınıfa atanmamış, sınıfın şubesinde artık öğretmen değil ya da sınıf pasif/başka şubede/başka okulda ise 404;
    pasifleştirilen kişinin oturumu doğrulanmaz (401). Sınıf öğretmeni ataması yalnızca kurucu (`TenantFounder` + okul sırası
    içinde yeniden denetim) ve audit'li. Bildirim ve e-posta yalnızca öğrenci adı, gün, okul adı ve durumu içerir (sağlık,
    sınıf arkadaşı, sınıf adı, öğretmen notu yok; velinin adı yalnızca e-posta alıcı alanında); e-posta adresi, gövde ve
    öğrenci adı loglanmaz (kaydetme komutu `ILoggableRequest` değildir — T9; loglarda yalnızca sınıf, yoklama ve kişi
    kimlikleri ve sayılar). Velinin bildirim listesi yalnızca kendi **bağı** olan çocuklarınındır. Yoklama kaydı ve düzeltmesi
    audit'te: kim, yoklama, ayrıntıda sınıf + gün + değişen öğrenci sayısı, IP.
13. **Veli iletişimi (spec 0011):** rol ve ilişki her istekte veritabanından (`CommunicationGuard`): duyuru yayını kurucu ya da
    öğretmen (öğretmen okul/şube kapsamında 403, atanmadığı sınıf ya da o sınıfta olmayan öğrenci 404); veli yalnızca alıcısı
    olduğu duyuruyu görür ve yalnızca kapsanan ve kendisine bağlı çocuğu için onay verir; sonuçları (okundu, okumayanlar,
    onay) yalnızca yayınlayan ve kurucu görür. Konuşma öğretmen–veli–öğrenci ilişkisi sürerken açılır ve yazılır (öğretmen
    sınıfa atanmış ve şubesinde etkin Öğretmen, veli öğrenciye bağlı ve okulda etkin); konuşmayı yalnızca iki taraf okur —
    kurucu dahil başkası 404. Duyuru, onay sorusu ve mesaj metni düz saklanır (alan şifrelemesi TC/sağlık/IBAN için; plan 0011
    Risks 5), **loglanmaz** ve audit'e yazılmaz: metin taşıyan komutlar `ILoggableRequest` değildir (T9), handler logları ve
    sahte bildirim göndericisinin satırları yalnızca tür ve kimlik içerir. Duyuru yayını, onay cevabı (verme/değiştirme) ve
    mesai saatleri değişikliği audit'te (kim, hangi kayıt, ayrıntıda yalnızca kapsam/sayı/cevap/saat, IP); tek tek mesajlar
    audit'e değil Information loguna (kimliklerle) yazılır.
14. **AI sesli not (spec 0012, BR-5):** sesli not yalnızca konuşmanın öğretmen tarafınca ve ilişki sürerken kullanılır (veli
    403; taraf olmayan — başka öğretmen, kurucu, muhasebe, başka okul — 404; ilişki bitmişse "Bu konuşmaya artık mesaj
    gönderilemez."). AI yalnızca `AiDraft` üretir, veliye hiçbir şey göndermez (Ai → Communication yazma bağımlılığı yok —
    mimari test). Mesaj yalnızca öğretmenin "Gönder"iyle 0011'in mesaj yolundan gider; taslak yalnızca oluşturan öğretmen
    tarafından ve oluşturulduğu konuşmada, bir kez kullanılır (aynı taslakla eşzamanlı gönderim taslak sırasıyla tekilleşir).
15. **Ödev ve not (spec 0013):** öğretmen–sınıf ilişkisi her istekte veritabanından, yoklamayla aynı kaynaktan
    (`TeacherClassroomAccess`): Öğretmen rolü yoksa 403; atanmadığı, başka şubenin/okulun ya da pasif sınıf ve o sınıfların ödev ve
    değerlendirmeleri 404; teslim durumunu ve puanı yalnızca kaydı oluşturan öğretmen değiştirir (değilse 403). Veli yalnızca bağlı
    olduğu çocuğun özetini görür (bağ yoksa 404, veli rolü yoksa 403). Öğrenci bazlı okuma sorgusunun uç noktası yoktur ve erişim
    denetlemez — çağıran denetler. Öğrenci adı, ödev başlığı/açıklaması, ders adı ve **puan loglanmaz ve audit'e yazılmaz**: komutlar
    `ILoggableRequest` değildir (T9), handler logları yalnızca kimlik ve sayı; ödev verme, teslim kaydı, değerlendirme açma ve puan
    kaydı audit'te (kim, hedef kayıt, ayrıntıda yalnızca sınıf + öğrenci/değişen sayısı, IP). Yeni ödev bildirimi yalnızca ders,
    başlık ve teslim tarihini taşır (açıklama ve öğrenci adı yok); bildirim log'unda yalnızca tür ve kimlik.

16. **AI haftalık gelişim özeti (spec 0014, S1/S2/S5/S6):** yalnızca öğrencinin bugünkü sınıfına atanmış öğretmen (`WeeklySummaryGuard`, her istekte
    veritabanından — `TeacherClassroomAccess`): Öğretmen rolü yoksa 403 (kurucu, veli, muhasebe); atanmamış öğretmen, başka sınıfın öğrencisi ve başka
    okul 404 — reddedilen istekte model çağrılmaz, taslak/bildirim yazılmaz. Öğrenci bazlı okuma sorguları (yoklama, ödev/not) erişim denetlemez; dayanak
    yalnızca guard'dan geçen öğrenci için toplanır (plan 0014 Risks 9). **Modele giden alanlar:** sabit talimat + öğrencinin adı (son kelime — soyadı —
    atılır) + o haftanın yoklama sayıları ve günleri + ödev (ders, başlık, teslim günü, durum) ve not (ders, tür, başlık, gün, puan) satırları + öğretmenin
    notu; soyad, sağlık notu, alerji, veli bilgisi, başka öğrenci ve kimlikler gitmez; model yalnızca yerel adreste (0012 kuralı). Sınırlama (0014 review R1-4): öğrencide ayrı ad alanı yok, "ad" = ad-soyadın son kelimesi dışındaki kelimeler — çift soyadlı öğrencide (ör. "Ali Yılmaz Kaya") soyadlardan biri yerel modele gider (backlog B-37). AI hiçbir şeyi
    veliye göndermez: gönderim öğretmenin "Onayla ve gönder"iyle Communication'dan (BR-5). Özet, öğretmen notu, dayanak ve öğrenci adı **loglanmaz**
    ve audit'e yazılmaz (komutlar `ILoggableRequest` değildir — T9; loglarda kimlik, karakter sayısı, süre); dayanak taslakla `ai_drafts`'ta, özet
    metni velinin bildiriminde düz saklanır. Gönderim audit'te (`WeeklySummarySent`: kim, taslak, ayrıntıda öğrenci + hafta + veli sayısı, IP). Veli
    özeti yalnızca kendi bağlı çocuğu için açar (`GET api/notices/{id}` — değilse 404, veli rolü yoksa 403).
17. **Tahsilat ve ön muhasebe (spec 0015, S4/S1/S2/S5/S6):** varsayılan reddet; rol ve şube her istekte veritabanından (`BillingGuard`): kurucu ya da
    Muhasebe rolü yoksa 403 (öğretmen, veli personel uçlarında); tarife/indirim yazma yalnızca kurucu (muhasebe okur — plan formu); muhasebe yalnızca
    **Muhasebe rolü olduğu** şubelerin öğrencisini, planını, tahsilatını, makbuzunu ve carisini görür (o şubede başka rolü olsa da diğer şubeler 404);
    başka okul 404; veli yalnızca kendi bağlı çocuklarının ödeme durumunu salt okunur görür (`api/my/payments`; başka çocuk 404; not, iptal gerekçesi ve
    kaydeden kişi veliye dönmez). **Kart bilgisi ve IBAN sistemde hiç tutulmaz** (C-1, C-9: yalnızca elle tahsilat; havale/EFT yalnızca yöntem adı) —
    alan şifrelemesi gerektiren yeni veri yok. Para: `decimal`/`numeric(18,2)`, istekte ham metin + sunucu doğrulaması (Türkçe), veritabanında `CHECK`;
    kalan borç ve makbuz sayacı sıra (`BeginExclusiveAsync`) içinde taze okunur — çift gönderim ya da eşzamanlı iki tahsilat kalanı aşamaz, aynı makbuz
    no iki kez verilmez (okul + no tekil dizini ikinci katman). Tarife ve indirim ekleme/düzenleme/pasifleştirme/etkinleştirme, plan açma/iptal,
    tahsilat kaydı/iptali audit'te (`FeeSchedule*`, `Discount*`, `InstallmentPlanCreated/Cancelled`, `PaymentRecorded/Cancelled`: kim, hangi kayıt, ne
    zaman, IP; ayrıntıda yalnızca öğrenci/plan kimliği, taksit sayısı ve makbuz no — tutar, ad, not, gerekçe yok); reddedilen işlem audit'e yazılmaz.
    Para/not/gerekçe taşıyan komutlar `ILoggableRequest` değildir (T9); loglarda yalnızca kimlikler. Hatırlatma metni (öğrenci adı + vade + kalan) yalnızca
    o öğrencinin etkin velilerinin uygulama içi akışında; gönderim Information log'unda kimliklerle. Arka plan hatırlatması okul başına **sistem
    oturumuyla** çalışır (kişisiz, okulla sınırlı filtre — `CurrentSession.AuthenticateSystem` yalnızca `OverdueReminderRunner`'da, architecture testi);
    yeni izinli dosya yok; `PlatformDirectory`'ye yalnızca etkin okul kimliklerini döndüren bir filtre delme eklendi — T5 gerekçesi
    (işçinin oturumu yok; okul başına kapsamı açmak için yalnızca kimlik gerekir, başka alan dönmez). `OverdueReminderRunner`'a yalnızca
    işçi ve DI kaydı bağımlı olabilir ve çalıştırıcı yalnızca okulu seçilmemiş taze kapsamda açılır (architecture testi + `TenantId is null`).
18. **Randevu (spec 0016, S1/S2/S3/S5/S6):** rol ve ilişki her istekte veritabanından (`AppointmentsGuard`): müsaitlik, takvim ve yanıt Öğretmen rolü,
    talep ve "Randevularım" Veli rolü ister (yoksa 403, Türkçe gövde); veli yalnızca **çocuğunun sınıfının öğretmenlerinin** dilimlerini görür ve talep eder
    (0010 sınıf öğretmeni bağı — mesajlaşmayla aynı kural: sınıf etkin, öğretmen atanmış ve sınıfın şubesinde etkin Öğretmen; veli bağlı ve okulda etkin),
    başka öğretmen, bağlı olmadığı çocuk, başka okul 404; öğretmen yalnızca kendi dilimini ve randevusunu görür/değiştirir (başkasınınki 404). Veli bağlı
    çocuklarının ve kendi talep ettiği randevuları görür; çocuğun öteki velisi görür ama iptal edemez (403); ilişki bitince yeni talep reddedilir, var olan
    randevu iki tarafta görünür ve iptal edilebilir (plan 0016 A-6). Dilim ve randevu yalnızca kurum filtreli (dilimde kaldırılmamış); şube/ilişki
    duvarı guard'da, filtre delme yok — iki okulda öğretmen olan kişinin takvimi okul başınadır. Bütün değişiklikler öğretmen sırasında
    (`appointments:teacher:<id>`) taze okumayla karar verir; dilimde tek açık randevuyu kısmi tekil dizin ikinci katman olarak korur (ihlal "Bu saat
    artık dolu."). **Konu, ret/iptal nedeni ve kişi adları loglanmaz ve audit'e yazılmaz**: komutlar `ILoggableRequest` değildir (T9), loglarda yalnızca
    kimlikler; talep, onay, ret ve iptal audit'te (`AppointmentRequested/Approved/Rejected/Cancelled`: kim, hangi randevu, ne zaman, IP; ayrıntıda
    yalnızca dilim/öğrenci/öğretmen kimliği ve durum), müsaitlik açma/kaldırma Information log'unda (kimlik + sayı). Veliye bildirim metni öğretmen adı,
    gün, saat ve varsa neden taşır (konu yok) ve yalnızca o çocuğun okulda etkin velilerine gider; öğretmene giden bildirim metni yalnızca tür, gün ve
    saattir (`PushNotification.Text`; sahte gönderici metni loglamaz) ve mesai saatleri dışında gönderilmez (0011 kapısı, `NotifyTeacherCommand`).
    Metinli bildirim yalnızca `PushNotificationKind.Appointment` türünde kurulabilir (başka türde metin `ArgumentException`; review tur 1 R1-4 —
    gerçek push gelince metin cihaza gider, backlog B-6). Commit sonrası bildirim adımları hata verirse randevu kayıtlı kalır, istek başarılı döner;
    hata yalnızca türü ve kimliklerle Warning olarak loglanır (hata mesajı da yazılmaz; review tur 1 R1-2).

19. **Yönetici paneli ve dışa aktarma (spec 0017, S1/S2/S4/S6):** panel, raporlar ve dışa aktarma yalnızca seçili okulun kurucusu
    (`TenantFounder` + `DashboardGuard`, her istekte veritabanından; öğretmen, veli, muhasebe 403; girişsiz 401); şube süzgeci oturumun
    gördüğü etkin şubelerden değilse 404. Dashboard depo kullanmaz; okul + şube filtresi okunan modüllerin depolarında oturumdan uygulanır
    (yeni filtre delme yok). Panel ve dosyalarda kişisel veri olarak yalnızca öğrenci ad-soyadı ve okul numarası (+ personelin adı: duyuruyu
    yayınlayan, tahsilatı kaydeden); TC, telefon, e-posta, sağlık notu, alerji, tahsilat notu ve iptal gerekçesi yok. **CSV/formül
    enjeksiyonu:** `Features/Dashboard/Export/CsvWriter` — `=`, `+`, `-`, `@`, sekme, satır başı ya da satır sonu ile başlayan **metin**
    hücresinin başına `'` eklenir, `;`/`"`/satır kırığı içeren metin tırnaklanır; tutar, sayı ve gün hücreleri kaçışlanmaz (sayı kalır).
    En fazla 10.000 veri satırı; aşan sonuç "Sonuç çok büyük…" ile reddedilir. Her başarılı dışa aktarma audit'e yazılır
    (`ReportExported`: kim, ne zaman, IP; ayrıntıda rapor türü, aralık, şube/sınıf kimliği, eşik, "yalnızca gecikenler" ve satır sayısı — ad
    yok); reddedilen yazılmaz. Dosya bellekte kurulur, sunucuda saklanmaz, paylaşım bağlantısı yok; içerik ve adlar loglanmaz (Information
    satırı yalnızca rapor türü, satır sayısı, kişi ve okul kimliği); dışa aktarma komutu `ILoggableRequest` değildir (T9).

## KVKK
1. Veli ilk girişte aydınlatma metnini görür, açık rıza verir; rıza metni sürümü ve zamanı kaydedilir.
2. Fotoğraf paylaşımı öğrenci bazlı rıza olmadan yapılamaz.
3. Mezun/ayrılan öğrencinin verisi, kurumun tanımladığı saklama süresi dolunca zamanlanmış işle anonimleştirilir.
4. Türkiye'de barındırılabilir tasarım; yurt dışı servis bağımlılıkları port arkasında, değiştirilebilir.
5. Çocuklara dair veriler model eğitiminde kullanılmaz.

## Dependencies
1. Yeni NuGet/npm paketi **plan onayında** açıkça listelenir; insan onaylar.
2. Kontrol: lisans (MIT/Apache/BSD uygun; GPL/ticari → ayrı karar), son 12 ayda bakım, bilinen CVE yok.
3. `scripts/check`: `dotnet list package --vulnerable --include-transitive` ve `npm audit --audit-level=high`; bulgu → check kırmızı.
4. Sürümler merkezi: `Directory.Packages.props` (Central Package Management) ve npm lock dosyası.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
