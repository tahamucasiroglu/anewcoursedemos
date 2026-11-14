# Security

> Bootstrap 2026-11-14. Her plan ve review'da uyulması gereken temel kurallar.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- JWT imza anahtarı ve bağlantı dizesi repoya girmez: geliştirmede `dotnet user-secrets`; örnek değerler
  `appsettings.Development.example.json` / `.env.example` içinde, gerçek değer olmadan.
- Demo seed kullanıcılarının parolaları kodda düz metin tutulmaz; seed sırasında hash'lenir (parola ortam değişkeni/user-secrets'tan gelir).

## Input & output
- Sınırda (Api) DTO şekil doğrulaması (zorunlu alan, tip, format) → 400. İş kuralları (BR) Domain'de → 422.
- Hata yanıtları yalnız ProblemDetails + BR kodu taşır; stack trace, SQL, bağlantı dizesi, iç tip adı asla sızmaz.
- Loglara parola, token ve hastalık notu yazılmaz.
- Frontend: React'in varsayılan kaçışlaması korunur; `dangerouslySetInnerHTML` yasak (ESLint `react/no-danger`).
- EF Core parametreli sorgular; ham SQL string birleştirme yasak.

## AuthN / AuthZ
- **Varsayılan reddet:** her uç nokta kimlik doğrulama ister (fallback authorization policy); yalnız giriş uç noktası `AllowAnonymous`.
- Kimlik doğrulama: demo JWT (kısa ömürlü, ör. 1 saat; refresh token yok). Parolalar ASP.NET Core Identity `PasswordHasher` ile.
- Roller: `Employee`, `Manager`, `HR`.
- Yetki **Application katmanında** denetlenir (Api'deki öznitelikler yalnız ek kat): onay/ret yalnız talep sahibinin yöneticisi, yöneticisi yoksa İK (BR-5),
  kendi talebini onaylama yok; talep görüntüleme yalnız sahibi, yöneticisi ve İK. Frontend'de buton gizlemek güvenlik sayılmaz.
- Hassas alan: hastalık izni notu yalnız sahibine, yöneticisine ve İK'ya döner; diğer yanıtlarda alan hiç yer almaz.

## Dependencies
- Her yeni NuGet/npm paketi planda gerekçesiyle listelenir; plan onayı paketin onayıdır. Plan dışı paket eklenmez.
- Kontrol: lisans MIT / Apache-2.0 / BSD ailesi; aktif bakım (son 12 ayda sürüm); bilinen yüksek/kritik açık yok.
- Sürümler sabit: NuGet Central Package Management (`backend/Directory.Packages.props`), npm `package-lock.json`.
- `dotnet list package --vulnerable --include-transitive` ve `npm audit --audit-level=high` `scripts/check` full profilinde koşar; bulgu → kırmızı.

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
