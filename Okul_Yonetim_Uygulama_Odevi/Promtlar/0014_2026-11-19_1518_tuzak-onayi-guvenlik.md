# 0014 — Konu 8 onayı · Konu 9/12: Güvenlik

**Tarih:** 2026-11-19 15:18

## İstek

> onaylıyorum

## Cevap

**Konu 8 — Bilinen tuzaklar: ONAYLANDI.** T1–T7, `docs/conventions.md` "Known traps" bölümüne yazılacak.

**Konu 9/12 — Güvenlik** (`docs/security.md`)

**A. Gizli bilgiler (secrets)**
1. Şifre, bağlantı cümlesi, JWT imza anahtarı, API anahtarı **asla** repoya, spec'e, prompt'a veya sohbete girmez.
2. Geliştirmede `dotnet user-secrets` kullanılır (repo dışında, kullanıcı profilinde). Sunucuda ortam değişkenleri veya secret store kullanılır. Repoda yalnızca değeri boş `appsettings.Example.json` / `.env.example` bulunur.
3. Ajan gizli değerleri hata ayıklarken bile ekrana yazmaz.

**B. Hassas veri koruması (S2–S4)**
1. **Şifre:** ASP.NET Core Identity `PasswordHasher` (PBKDF2) veya Argon2id ile hash'lenir. Düz metin hiçbir yerde tutulmaz (DB, log, hata mesajı, yanıt).
2. **TC kimlik no, sağlık notu, alerji, IBAN:** Veritabanında **alan düzeyinde şifreli** saklanır (AES-GCM; anahtar ASP.NET Core Data Protection ile yönetilir, repoda olmaz). TC ile arama gerektiğinde ayrı bir **HMAC parmak izi** kolonu kullanılır; böylece şifreyi çözmeden eşleşme yapılır.
3. **Telefon, e-posta, adres:** Veritabanında düz metin (iletişim için gerekli), ama **loglarda maskeli**, listelerde yalnızca yetkili rollere görünür.
4. **Kart bilgisi:** Sistemde **hiç** saklanmaz ve **hiç** sistemden geçmez. Ödeme sağlayıcısının barındırılan sayfası/token'ı kullanılır (PCI-DSS kapsamı dışında kalmak için).
5. **Loglar:** Serilog'da kişisel veri için maskeleme kuralı tanımlanır. Ham istek/yanıt gövdesi loglanmaz.
6. **Dosyalar ve fotoğraflar:** Herkese açık URL ile değil, kısa ömürlü imzalı bağlantıyla sunulur. Fotoğraf paylaşımında öğrenci bazlı izin kontrol edilir.

**C. Girdi ve çıktı**
1. Her API girdisi sınırda FluentValidation ile doğrulanır (nArchitecture validation pipeline).
2. SQL yalnızca EF Core üzerinden (parametreli) çalıştırılır. Ham SQL T5 kuralına tabidir.
3. Excel/e-Okul içe aktarmada: dosya boyutu ve tip sınırı uygulanır, formül hücreleri değer olarak okunur. Dışa aktarmada CSV/Excel formül enjeksiyonuna karşı `=`, `+`, `-`, `@` ile başlayan hücreler kaçışlanır.
4. Hata yanıtlarında stack trace, SQL ve kişisel veri bulunmaz (Konu 6-D).
5. AI'a giden veride yalnızca görev için gereken alanlar gönderilir (veri minimizasyonu). AI sağlayıcısıyla "eğitimde kullanılmaz" koşulu zorunludur.

**D. Kimlik doğrulama ve yetkilendirme**
1. **Varsayılan: reddet.** Her uç nokta kimlik doğrulama ister. Anonim uçlar (giriş, şifre sıfırlama) açıkça listelenir.
2. JWT erişim token'ı kısa ömürlü (15 dk) + refresh token (döndürmeli, iptal edilebilir) kullanılır. nArchitecture.Core.Security'de mevcut.
3. **2FA:** Kurucu/Yönetici ve Muhasebe için **zorunlu** (TOTP). Öğretmen ve Veli için isteğe bağlı.
4. **Yetki üç katmanda kontrol edilir:** (a) rol (nArchitecture `ISecuredRequest`), (b) kurum + şube (oturumdan, global filtre), (c) **ilişki**: veli yalnızca kendi çocuğunu, öğretmen yalnızca kendi sınıflarını görür.
5. Giriş denemesi hız sınırı ve hesap kilitleme uygulanır (kaba kuvvet koruması).
6. **Audit log:** Öğrenci kartı görüntüleme/değiştirme, sağlık verisi okuma, tahsilat işlemleri, rol değişiklikleri ve rıza değişiklikleri yazılır: kim, ne, hangi kayıt, ne zaman, hangi IP. Audit log yalnızca eklenir, değiştirilemez/silinemez.

**E. KVKK**
1. Veli ilk girişte aydınlatma metnini görür ve açık rızayı onaylar. Rıza sürümü ve zamanı kaydedilir.
2. Fotoğraf paylaşımı, öğrenci bazlı rıza olmadan yapılamaz.
3. Mezun/ayrılan öğrencinin verisi, kurumun tanımladığı saklama süresi dolunca anonimleştirilir (zamanlanmış iş).
4. Barındırma Türkiye'de yapılabilecek şekilde tasarlanır; yurt dışı servis bağımlılığı port arkasında, değiştirilebilir durumdadır.

**F. Bağımlılık (3. parti paket) politikası**
1. Yeni bir NuGet/npm paketi eklemek **plan onayında** açıkça listelenir ve insan onaylar.
2. Kontrol edilenler: lisans (MIT/Apache/BSD uygun; GPL/ücretli lisans → ayrı karar), son 12 ayda bakım görmüş olması, bilinen güvenlik açığı (CVE) olmaması.
3. `scripts/check` içinde `dotnet list package --vulnerable --include-transitive` ve `npm audit --audit-level=high` çalışır. Bulgu varsa check **kırmızı** olur (K2'nin paket karşılığı).
4. Sürümler merkezi olarak sabitlenir (`Directory.Packages.props`, Central Package Management).

Önerim: A–F'nin tamamı. En kritikleri B2 (TC/sağlık şifreli), B4 (kart hiç tutulmaz), D4 (üç katmanlı yetki), D6 (audit log) ve F3 (açık paket = kırmızı).

**Dokümanda istenen:**
- "Çocuk verisi işlendiği için KVKK ve güvenlik … MVP'den itibaren tam olmalı."
- "Rol ve yetki yönetimi, şube bazlı görünürlük" → D4
- "Audit log: kim, hangi öğrenci kaydını, ne zaman görüntüledi/değiştirdi" → D6
- "Veli açık rıza ve aydınlatma metni akışı, rıza kayıtları" → E1
- "Fotoğraf paylaşımında öğrenci bazlı izin" → B6, E2
- "Mezun/ayrılan öğrenci verisi için saklama süresi ve anonimleştirme" → E3
- "2FA (yönetici zorunlu), şifreli veri saklama" → D3, B2
- "Yedekleme, Türkiye'de barındırma seçeneği" → E4
- "Çocuklara dair veriler model eğitiminde kullanılmaz" → C5
- Faz 2: "Google / Microsoft SSO — öğretmen girişi" → MVP'de yok, nArchitecture altyapısı hazır.

Cevap bekleniyor: A–F güvenlik kuralları uygun mu? Özellikle 2FA kapsamı (Yönetici + Muhasebe zorunlu) doğru mu?
