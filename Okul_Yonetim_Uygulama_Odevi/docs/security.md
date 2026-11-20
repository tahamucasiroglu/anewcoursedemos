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
2. **TC kimlik no, sağlık notu, alerji, IBAN:** alan düzeyinde şifreli (AES-GCM; anahtar ASP.NET Core Data Protection ile, repo dışında). TC araması ayrı HMAC parmak izi kolonu ile.
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
   kuralı en az 8 karakter.
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
