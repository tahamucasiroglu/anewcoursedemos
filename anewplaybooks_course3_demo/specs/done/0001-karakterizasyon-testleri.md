# Spec 0001 — Karakterizasyon testleri

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done

## Intent
Değişiklik yapacak ekip, testsiz LeaveManager'ın **bugünkü davranışını** otomatik testlerle kayda
almak istiyor. Böylece sonraki her özellik veya düzeltmede neyin bozulduğu anında görülür.
Başarı şu durumdur: sözleşme değerleri (dashboard Bekleyen 4 · Onaylanan 1 · Reddedilen 3; Ayşe
Yılmaz Hak 26 · Kullanılan 12 · Kalan 14), kıdem bantları ve tuhaflıklar (hafta sonu sayımı,
çelişkili eşik, yutulan hata, geçmiş tarih) tek komutla (`./scripts/check`) koşan testlerle
sabitlenmiştir. Bilerek **yapılmayan**: uygulama davranışında hiçbir değişiklik, tuhaflıkların
düzeltilmesi, refactor. Testler gözlenen davranışı yazar, olması gerekeni değil.

Netleştirme kararları (2026-11-21, tahamucasiroglu): kapsam (a)–(e) ve tuhaflıklar (f)–(i)
olduğu gibi sabitlenir; referans tarih 2026-11-21; `app.py` değişmez; tek spec. Öz-eleştiri
boşlukları G-1…G-6 önerildiği gibi işlendi.

## Requirements
- R-1: Uygulamanın mevcut davranışı, referans tarih **2026-11-21** ("bugün" sabit) ve açılış seed
  verisiyle otomatik testlerle kayda alınır.
- R-2: Testler birbirinden bağımsızdır: her test aynı başlangıç verisiyle başlar, bir testin
  oluşturduğu talep diğerini etkilemez (`docs/testing.md`, Determinism).
- R-3: Testler `./scripts/check`'in iki profilinde de (fast ve full) koşar. Check bir sunucu, süreç
  veya dosya artığı bırakmaz (`docs/testing.md`, Resource hygiene).
- R-4: Uygulamanın kendisi (`app.py`) değişmez. Test için gereken her düzenek test tarafındadır.
- R-5: Tuhaf davranışlar "doğru" kabul edilmeden, **olduğu gibi** kayda alınır. Her birinin
  testi, hangi iş kuralına veya açık soruya bağlı olduğunu adında veya açıklamasında söyler
  (BR-3, BR-6, BR-7 / Q-1, yutulan hata).

## User experience
none — no user interface. Testler mevcut ekranların (`/`, `/requests`, `/new`) HTML çıktısını
okur ama ekranlar değişmez. Ekran görüntüsü gerekmez.

## Constraints & out of scope
- Sensitive area: Bakiye hesabı; Talep oluşturma ve bakiye eşiği (`docs/domain.md`). Bu yüzden
  her kriter, ilgili kod kasıtlı olarak bozulduğunda kırmızıya döndüğü gösterilerek kanıtlanır
  (VERIFY).
- Kapsam dışı: tuhaflıkları düzeltmek (BR-3 iş günü, BR-7 eşik kararı Q-1, yutulan hatalar,
  ERR-412 metni), refactor, `recalculate_year_end`, güvenlik borçları (R-S1…R-S4), yeni özellik.
- Test bağımlılığı (pytest) ve sürüm sabitleme bu işte eklenir (`docs/security.md`,
  Dependencies). Uygulama bağımlılığı (Flask) değişmez, yalnız mevcut sürümü kayda geçer.
- Olmayan personel numarasıyla gönderim (yutulan hataya düşer) kapsam dışıdır: formdan gelmez;
  onu değiştirecek `/fix-bug` kendi testini getirir.
- Eşik çelişkisi (Q-1) bu spec'te **karara bağlanmaz**. İki davranış da korunur, karar onu
  değiştirecek spec'e aittir.

## Acceptance criteria
- [x] AC-1 — Referans tarihte dashboard durum sayıları: **Bekleyen 4 · Onaylanan 1 · Reddedilen 3**.
- [x] AC-2 — Referans tarihte dashboard bakiye tablosu (Hak / Kullanılan / Kalan): Ayse Yilmaz
  **26 / 12 / 14** · Mehmet Demir 20 / 0 / 20 · Zeynep Kaya 14 / 0 / 14 · Ahmet Celik 20 / 0 / 20 ·
  Elif Sahin 0 / 0 / 0. Ayşe'nin bekleyen 3 günlük talebi (2026-12-28–30) kullanılana ve kalana
  yansımaz (BR-4, Q-2). Ahmet Celik satırı 29 Şubat işe giriş tarihini de kapsar.
- [x] AC-3 — Talepler listesi seed'deki 8 talebi seed sırasıyla (talep no 1→8) gösterir. Gün sayıları takvim günüdür, hafta sonları
  dahil (BR-3): sırasıyla 12, 3, 1, 5, 3, 3, 12, 1. Ayşe'nin 2026-07-06–2026-07-17 talebi
  **12 gün** sayılır (iş günü olsaydı 10 olurdu).
- [x] AC-4 — Kıdem→hak sınırları (BR-1, BR-2: yıl farkı): kıdem 0 → 0 · 1 → 14 · 5 → 14 · 6 → 20 ·
  15 → 20 · 16 → 26 gün. Ay ve gün yok sayılır: işe giriş 2025-12-31 → referans tarihte kıdem 1 →
  14 gün.
- [x] AC-5 — Geçerli yeni talep (kalan > gün): kaydedilir, `Beklemede` durumunda doğar, sıradaki
  numarayı (9) alır, talepler listesine yönlendirilir ve listede görünür.
- [x] AC-6 — Form eşiği (BR-7, form tarafı): kalan < gün ise (örn. kalanı 0 olan Elif Sahin, 1 gün)
  talep kaydedilmez (talep sayısı değişmez), yönlendirme olmaz ve hata sayfası doğrudan döner;
  sayfa birebir şu metni gösterir: `HATA: ERR-412 NullReferenceGibi - bakiye
  yetersiz. IT'ye bildirin: dahili 4127`.
- [x] AC-7 — Servis eşiği (BR-7, servis tarafı, Q-1): kalan **eşittir** gün ise (Zeynep Kaya, kalan
  14, 14 günlük talep) hata gösterilmez, talepler listesine yönlendirilir ama talep
  **kaydedilmez**. Bir gün eksiği (13 gün) kaydedilir.
- [x] AC-8 — Geçersiz tarih (yutulan hata): tarih çözülemezse hata gösterilmez, talepler listesine
  yönlendirilir, talep kaydedilmez.
- [x] AC-9 — Geçmiş tarihli talep (BR-6): başlangıcı referans tarihten önce olan, bakiyesi yeterli
  talep kaydedilir.
- [x] AC-10 — Testler `./scripts/check`'in fast ve full profillerinde koşar, residue temizdir ve
  `app.py`'nin bu işteki diff'i boştur.

## Evidence (VERIFY, 2026-11-21 — bağımsız QA subagent'ı)
Full: `CHECK GREEN (full, 3 steps, 4 s)` @0bd5644 (HEAD'de koşuldu, 15 test). Fast ve residue:
plan `## Build evidence` @11bc27b. Hassas alan: her kriter repo dışında geçici kopyada kasıtlı
bozmayla kırmızıya düşürüldü, hiçbir bozma commit edilmedi.

| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-1 | `tests/test_dashboard.py::test_durum_sayilari_bekleyen4_onaylanan1_reddedilen3` — bozma: `app.py:134` filtre → red | PASS |
| AC-2 | `tests/test_dashboard.py::test_bakiye_tablosu_bes_personel` — bozma: `calc_days` `+1` kaldırıldı → red | PASS |
| AC-3 | `tests/test_requests_list.py::test_liste_sekiz_talep_takvim_gunu_hafta_sonu_dahil` — bozma: `+1` kaldırıldı → red | PASS |
| AC-4 | `tests/test_entitlement.py::test_kidem_bantlari_sinirlar` · `tests/test_entitlement.py::test_kidem_yalniz_yil_farki` — bozma: 14→15 → red; kıdem yıldönümüne göre → red | PASS |
| AC-5 | `tests/test_new_request.py::test_gecerli_talep_beklemede_dogar` — bozma: yeni durum `Onaylandi` → red | PASS |
| AC-6 | `tests/test_new_request.py::test_form_esigi_err412_dahili_4127` — bozma (A-1): `app.py:165` `<`→`>` → red | PASS |
| AC-7 | `tests/test_new_request.py::test_servis_esigi_kalan_esit_gun_sessizce_kaydedilmez` — bozma: `app.py:96` `<=`→`<` → red; sınır `+1` → red | PASS |
| AC-8 | `tests/test_new_request.py::test_gecersiz_tarih_sessizce_yonlendirir` — bozma: route `except` (`app.py:169`) kaldırıldı → red (500) | PASS |
| AC-9 | `tests/test_new_request.py::test_gecmis_tarihli_talep_kabul` — bozma: geçmiş tarih reddedildi → red | PASS |
| AC-10 | `./scripts/check` (full+fast) yeşil, RESIDUE CLEAN (fast + full) @11bc27b, `app.py` diff'i `course3-anew-bootstrapped..0bd5644` boş — bozma: tek boşluk diff'te görünür | PASS |

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 0 |
| Fix rounds | 1 (yalnız doküman: A-1) |
| Review findings: real / noise / deferred | 2 / 1 / 0 |
| Regressions introduced | 0 |
| Bugs escaped to production | 0 |
