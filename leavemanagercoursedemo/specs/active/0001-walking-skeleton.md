# Spec 0001 — Walking skeleton

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done

## Intent
Geliştirici ve kurs izleyicisi, ilk iş özelliği yazılmadan önce çalışan, uçtan uca ama içi boş bir
Leave Manager uygulaması görmek istiyor: tarayıcıda açılan Türkçe bir ana sayfa, tüm katmanlardan
geçen (arayüz → uygulama → veritabanı) bir iskelet ve tek komutla (`./scripts/check`) yapılan,
GitHub CI'da da aynen koşan doğrulama. Başarı: check hem hızlı hem tam profilde gerçek adımlarla
yeşil, CI aynı sonucu veriyor, uygulama açıldığında ana sayfa görünüyor. Bilinçli olarak yapılmayan:
kullanıcı girişi ve kimlik doğrulama (spec 0002), hiçbir iş kuralı, izin talebi ekranı veya veri
modeli. Clarify kararları (2026-11-07): "giriş sayfası" = anonim ana sayfa; veritabanı minimal
dahil; güvenlik başlıkları ve genel hata sayfası dahil; katman sınırları şimdiden test edilir;
CI'a .NET kurulumu eklenir.

## Requirements
- R-1: Uygulama, oturum açmamış bir ziyaretçiye kök adreste Türkçe bir ana sayfa gösterir.
- R-2: Sayfalar yalnızca uygulamanın kendi kaynaklarını (CSS, ileride JS) kullanır; sayfaya gömülü
  betik veya olay işleyici yoktur.
- R-3: Her yanıt, tarayıcıya güvenlik kısıtlarını bildiren başlıklar taşır (`docs/security.md`,
  Input & output): içerik yalnızca aynı kaynaktan, MIME koklama kapalı, referrer gönderilmez,
  çerçeve içine alınamaz.
- R-4: Beklenmeyen bir sunucu hatasında ziyaretçi genel bir Türkçe hata sayfası görür; teknik
  ayrıntı (istisna mesajı, stack trace, SQL) gösterilmez.
- R-5: Var olmayan bir adres istendiğinde ziyaretçi Türkçe bir "sayfa bulunamadı" sayfası görür.
- R-6: Uygulama açılışta veritabanı yoksa onu oluşturur ve veritabanına bağlanabilir durumdadır.
- R-7: `docs/architecture.md`'deki beş yasak bağımlılığın her biri otomatik bir testle denetlenir.
- R-8: `./scripts/check` gerçek build, format ve test adımlarıyla çalışır; hızlı ve tam profil
  `docs/testing.md`'deki bölmeye uyar.
- R-9: GitHub CI, her push'ta tam profili .NET 10 ile çalıştırır.

## User experience
- Flow: ziyaretçi uygulamanın kök adresini açar → "İzin Yönetimi" ana sayfasını görür. Başka
  etkileşim yoktur.
- States:
  - Başarı: ana sayfa — başlık "İzin Yönetimi", tek bir ana başlık (`h1`) ve uygulamayı tanıtan
    kısa bir cümle.
  - Hata: genel hata sayfası — "Beklenmeyen bir hata oluştu" anlamında Türkçe mesaj, teknik ayrıntı yok.
  - Bulunamadı: "Sayfa bulunamadı" anlamında Türkçe mesaj ve ana sayfaya bağlantı.
  - Boş / yükleniyor / yetkisiz: uygulanamaz (veri yok, sunucu tarafı render, anonim erişim).
- Design reference: none — the flow above is the design.
- Accessibility: sayfa dili `tr` olarak bildirilir; her sayfada tek `h1`; metin/arka plan kontrastı
  en az WCAG AA (4.5:1); bulunamadı sayfasındaki bağlantı klavyeyle (Tab + Enter) erişilebilir.

## Constraints & out of scope
- Sensitive area: none — bu spec kimlik doğrulama, yetkilendirme veya bakiyeye dokunmaz
  (`docs/domain.md`); tüm sayfalar anonimdir.
- Out of scope: kullanıcı girişi, ASP.NET Core Identity, varsayılan-reddet politikası (spec 0002);
  her türlü domain varlığı ve iş kuralı; navigasyon menüsü; JS dosyası; resmî tatil; e-posta.
- HTTPS yönlendirmesi ve HSTS yalnızca Production ortamındadır; test sunucusu HTTP konuştuğu için
  bu spec'te kanıtlanmaz.
- Yalnızca `docs/security.md` "Dependencies" listesindeki paketler kullanılır.
- Hata tetikleme (AC-4): istisna fırlatan adres yalnızca test host'unda tanımlanır; üretim kodunda
  hata tetikleyen bir adres yoktur. Entegrasyon testleri `Testing` ortamında koşar; Development'ta
  geliştirici hata sayfası kalır.
- Veritabanı dosyasının yolu yapılandırmadan okunur (varsayılan `App_Data/leavemanager.db`);
  veritabanı dosyası repoya girmez (`.gitignore`). Testler bellek içi SQLite kullanır, dosya
  oluşturmaz (`docs/testing.md`, Resource hygiene kural 5).
- `vulnerable` adımı ağ ister; çevrimdışı makinede tam profil kırmızıdır. Bu gereksinim PLAN
  gate'inde doctor'ın profil kararıyla (R-13) karara bağlanır.
- AC-9 yalnızca branch push edildikten sonra kanıtlanabilir; kanıt CI çalıştırmasının linki ve
  HEAD'e eşit SHA'sıdır (`docs/testing.md`, Two speeds kural 6).

## Acceptance criteria
- [ ] AC-1 — Oturumsuz bir ziyaretçinin kök adrese isteği 200 döner; sayfa `lang="tr"`, başlığında
  "İzin Yönetimi" ve tam olarak bir `h1` ("İzin Yönetimi") içerir.
- [ ] AC-2 — Ana sayfa uygulamanın kendi CSS dosyasına bağlanır ve o dosya 200 döner; sayfada
  inline `<script>` bloğu ve `on…=` olay özniteliği yoktur.
- [ ] AC-3 — Ana sayfa, bulunamadı sayfası, hata sayfası ve CSS dosyası yanıtlarının her biri
  `Content-Security-Policy` (`default-src 'self'` içerir), `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: no-referrer` ve `X-Frame-Options: DENY` başlıklarını taşır; CSP `'unsafe-inline'`
  ve `'unsafe-eval'` içermez.
- [ ] AC-4 — `Testing` ve `Production` ortamlarında istek işlenirken beklenmeyen bir istisna oluşursa yanıt
  500 döner, Türkçe genel hata mesajını içerir; istisnanın mesajı ve stack trace yanıtta geçmez.
- [ ] AC-5 — Var olmayan bir adres 404 döner ve Türkçe "Sayfa bulunamadı" mesajı ile ana sayfaya
  bir bağlantı içerir.
- [ ] AC-6 — Uygulama boş bir veritabanıyla başlatıldığında açılış tamamlandıktan sonra veritabanı
  vardır ve uygulamanın veritabanı bağlamı üzerinden yapılan bir bağlantı sorgusu başarılı olur.
- [ ] AC-7 — `docs/architecture.md` yasak bağımlılık 1–5'in her biri ayrı bir otomatik testle
  denetlenir ve her test, kendi kuralını ihlal eden bir kod karşısında kırmızıya düşer (VERIFY'da
  kural başına bir kez kasıtlı kırma ile kanıtlanır: ihlal geçici eklenir, test kırmızı, geri alınır;
  repoya ihlal eden kod girmez).
- [ ] AC-8 — `./scripts/check` (tam) ve `./scripts/check --fast` gerçek adımlarla yeşildir; hızlı
  profil yalnızca `[fast]` adımlarını (build, format, birim + mimari testler) çalıştırır; hızlı
  profildeki testler ile tam profildeki entegrasyon testlerinin toplamı, filtresiz `dotnet test`'in
  çalıştırdığı test sayısına eşittir.
- [ ] AC-9 — GitHub'daki `check` CI işi bu branch'in HEAD commit'inde .NET 10 ile tam profili
  çalıştırır ve yeşildir.

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
