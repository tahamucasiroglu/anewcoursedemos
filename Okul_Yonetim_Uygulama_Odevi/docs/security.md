# Security

> Her plan ve review'da uyulması zorunlu temel kurallar. Bootstrap 2026-11-19. Hassas alanlar S1–S6: `docs/domain.md`.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Geliştirmede `dotnet user-secrets` (repo dışında, kullanıcı profilinde); sunucuda ortam değişkeni / secret store.
  Repoda yalnızca değerleri boş `appsettings.Example.json` / `.env.example`. Bu kapsam: DB bağlantı cümlesi,
  JWT imza anahtarı, Data Protection anahtarları, HMAC anahtarı, ileride sağlayıcı API anahtarları.
- Yerel PostgreSQL süper kullanıcı şifresi yalnızca geliştirme makinesindedir; repoya yazılmaz.

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
5. AI'a yalnızca görev için gereken alanlar gönderilir (veri minimizasyonu); sağlayıcı sözleşmesinde "eğitimde kullanılmaz" şartı zorunlu.

## AuthN / AuthZ
1. **Varsayılan: reddet.** Her uç nokta kimlik doğrulama ister; anonim uçlar (giriş, şifre sıfırlama) açıkça listelenir.
2. JWT erişim token'ı 15 dk + döndürmeli, iptal edilebilir refresh token (`NArchitecture.Core.Security`).
3. **2FA (TOTP):** Kurucu/Yönetici ve Muhasebe için **zorunlu**; Öğretmen ve Veli için isteğe bağlı.
4. **Üç katmanlı yetki:** (a) rol (`ISecuredRequest`), (b) kurum + şube (oturumdan, global query filter), (c) ilişki: veli yalnızca kendi çocuğunu, öğretmen yalnızca kendi sınıflarını görür.
5. Giriş denemesi hız sınırı + hesap kilitleme.
6. **Audit log:** öğrenci kartı görüntüleme/değiştirme, sağlık verisi okuma, tahsilat, rol ve rıza değişiklikleri — kim, ne, hangi kayıt, ne zaman, IP. Yalnızca eklenir; değiştirilemez/silinemez.
7. Kiracı/şube dışı kayıt → 404 (varlık bilgisi sızdırılmaz).

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
