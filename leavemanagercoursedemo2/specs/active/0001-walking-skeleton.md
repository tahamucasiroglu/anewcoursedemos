# Spec 0001 — Walking skeleton

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done

## Intent
Geliştirici (ve kursu izleyenler), tek bir iş kuralı yazılmadan önce uçtan uca çalışan en küçük dilimi istiyor:
backend'in dört katmanı ve test projeleri, frontend iskeleti, mimari kurallarının araçla denetlenmesi ve tek bir
doğrulama komutu (`scripts/check`) gerçekten bir şey denetler hale gelsin. Başarı: kullanıcı frontend'i açtığında
backend'in ayakta olduğunu görür; `scripts/check` her iki profilde yeşildir ve kasıtlı bir mimari ihlali kırmızıya düşürür.
Bilinçli olarak yapılmayan: izin domain'i (talep, bakiye, onay), giriş/kimlik, veritabanı ve kalıcılık.

## Requirements
- R-1: Backend, `docs/architecture.md`'deki dört katman ve üç test projesi olarak kurulur; aralarındaki izinli bağımlılıklar dışında bağımlılık yoktur.
- R-2: Sistem, kimlik istemeden "çalışıyor musun?" sorusuna sağlık durumu ve uygulama sürümüyle cevap verir. Bu cevap bir kullanım senaryosu olarak Application katmanından gelir; sürüm bilgisi bir port üzerinden okunur.
- R-2a: Domain, kural ihlallerini taşıyacak sonuç tipini (`Result` / `Result<T>`, `docs/conventions.md` Error handling) içerir.
- R-3: Kullanıcı frontend'i açtığında backend'in sağlık durumunu görür: yüklenirken, ulaşıldığında ve ulaşılamadığında ayrı ayrı.
- R-4: `docs/architecture.md` yasak bağımlılık 1–4'ün ihlali ve frontend'de bir feature'ın başka bir feature'ı import etmesi (5) `scripts/check`'i kırmızıya düşürür.
- R-5: "Şimdi/bugün" bilgisini `TimeProvider` dışından okuyan kod (`docs/conventions.md` banned API listesi) derlenmez.
- R-6: `scripts/check.conf` adımları açılır; `./scripts/check` ve `./scripts/check --fast` bu makinede yeşil koşar, ardında süreç bırakmaz.

## User experience
- Flow: kullanıcı frontend adresini açar → sayfa backend'in sağlık durumunu sorar → sonucu gösterir.
- States:
  - Yükleniyor: "Sistem durumu kontrol ediliyor…"
  - Başarı: "Sistem çalışıyor" + backend sürümü.
  - Hata (backend kapalı/ulaşılamıyor/beklenmeyen yanıt): "Sunucuya ulaşılamıyor" + "Tekrar dene" düğmesi.
  - Boş ve yetkisiz durum yok (sağlık sorgusu kimlik istemez, liste yok).
- Design reference: none — the flow above is the design.
- Frontend sürümü kendi tutmaz, yalnız backend yanıtından okur; geliştirmede frontend backend'e aynı kökten (`/api` proxy) ulaşır.
- Accessibility: durum metni bir `role="status"` bölgesinde (ekran okuyucu duyurur); "Tekrar dene" klavyeyle (Tab + Enter) erişilebilir; sayfa dili `tr`.

## Constraints & out of scope
- Out of scope: izin domain'i, giriş/JWT, kullanıcı/rol, veritabanı (EF Core/SQLite ve migration ilk feature ile gelir), seed verisi, stil/tasarım sistemi, dağıtım.
- Test kapsamı `docs/testing.md`'ye uyar: yalnız birim + mimari testleri. R-2/R-3'ün HTTP ve UI tarafı test değil, VERIFY'da çalışan uygulamanın gözlemi ve ekran görüntüleriyle kanıtlanır.
- Sağlık sorgusu, `docs/security.md` "varsayılan reddet" kuralının ikinci istisnasıdır (giriş dışında); yanıt sürüm dışında iç bilgi (ortam, yol, bağımlılık) sızdırmaz.
- Paketler planda listelenir ve plan onayıyla onaylanır (`docs/security.md`, Dependencies).
- Sensitive area: none.
- Bu spec `docs/security.md`'ye sağlık sorgusu istisnasını ekler (Docs/ADR güncellemesi, DoD).
- Yükleniyor durumu (AC-6) ekran görüntüsü için yalnız çekim sırasında yapay gecikmeyle yakalanır; üretim kodunda gecikme yoktur.
- Split: değerlendirildi; tek tutuldu — uçtan uca dilim bölünürse amacını kaybeder (9 kriter, tek yetenek).

## Acceptance criteria
- [ ] AC-1 — Backend çözümü dört katman + üç test projesi olarak derlenir; her test projesi en az bir gerçek test koşar (Domain: `Result` davranışı, Application: sağlık senaryosu, Architecture: kurallar); sıfır test ile geçen adım yok.
- [ ] AC-2 — Mimari testleri yasak bağımlılık 1–4'ü denetler; her biri için kasıtlı bir ihlal `./scripts/check`'i kırmızıya düşürür, ihlal geri alınınca yeşile döner.
- [ ] AC-3 — Frontend'de `features/<a>` içinden `features/<b>` import edilmesi (ve `shared/`'dan bir feature import edilmesi) lint adımını kırmızıya düşürür.
- [ ] AC-4 — Banned API listesindeki bir çağrı (ör. `DateTime.Now`) backend derlemesini kırmızıya düşürür.
- [ ] AC-5 — Kimliksiz sağlık sorgusu başarılı yanıt döner: durum "sağlıklı" ve uygulama sürümü; yanıtta başka iç bilgi yoktur.
- [ ] AC-6 — Frontend açıldığında, backend sorgusu sürerken yükleniyor durumu görünür.
- [ ] AC-7 — Backend ayaktayken frontend "Sistem çalışıyor" ve backend sürümünü gösterir.
- [ ] AC-8 — Backend kapalıyken frontend "Sunucuya ulaşılamıyor" ve "Tekrar dene" gösterir; backend açıldıktan sonra "Tekrar dene" başarı durumuna geçer.
- [ ] AC-9 — `./scripts/check` (full) ve `./scripts/check --fast` yeşil; residue testi iki profilde temiz (`Residue: clean (fast + full)`).

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
