# Spec 0004 — Geçersiz tarihte görünür hata (mini)

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0004-plan.md`
- Self-critique: done
- Source: Kullanıcı raporu (ödev), 2026-11-21 — "Tarihler geçersiz olduğunda form sessizce başarısız oluyor"
- Design: none — mevcut ERR-412 hata sayfası kalıbı (sayfa düzeni içinde kırmızı mesaj) kullanılır
- Supersedes: 0001/AC-8

## Intent
Talep giren personel veya İK, tarihleri yanlış girdiğinde bunu fark etmiyor: form hata göstermeden
listeye dönüyor ve talep kayboluyor. Bitiş başlangıçtan önceyse talep negatif günle kaydediliyor.
Artık geçersiz tarihli gönderim kaydedilmez ve kullanıcı aynı sayfada neyin yanlış olduğunu söyleyen
bir mesaj görür. Geçerli talepler ve eşik davranışı değişmez.

Kararlar (2026-11-21, tahamucasiroglu): triyajda BUG değil CHANGE (0001/AC-8 sözleşmeyi
sabitlemişti); bug-fix disiplini korunur (önce kırmızı repro testi, sonra kök neden teşhisi, sonra
en dar düzeltme). Diğer yutulan hatalar kapsam dışı. `test_gecersiz_tarih_sessizce_yonlendirir`
adını korur, beklentisi değişir. Sensitive area: Talep oluşturma ve bakiye eşiği
(`docs/domain.md`); her kriter VERIFY'da kasıtlı bozmayla kanıtlanır. Dokümanlar: `docs/domain.md`'ye
BR-10 (tarih doğrulaması) yazılır; `docs/conventions.md`'deki not güncellenir (genel `except` diğer
hatalar için yerinde, tarih artık doğrulanıyor). Öz-eleştiri boşlukları G-1…G-4 önerildiği gibi işlendi.

## Changed behavior
- [x] CB-1 — Başlangıç veya bitiş tarihi çözülemezse (örn. `abc`), boşsa, hiç gönderilmemişse ya da
  takvimde yoksa (örn. `2026-02-30`): talep kaydedilmez (talep sayısı değişmez), yönlendirme olmaz,
  HTTP 400 ile sayfa düzeni içinde birebir şu mesaj döner:
  `HATA: Tarihler gecersiz. Baslangic ve bitis gecerli bir tarih olmali; bitis baslangictan once olamaz.`
  VERIFY önce (sessiz yönlendirme, `480d076`) ve sonra (hata sayfası) ekran görüntüsünü alır, SHIP'te
  değerlendirilir.
- [x] CB-2 — Bitiş başlangıçtan önceyse (örn. 2026-12-10 → 2026-12-05): talep kaydedilmez, CB-1
  ile aynı mesaj ve davranış (bugün −4 günle kaydediliyor). Tarih doğrulaması bakiye kontrolünden
  önce yapılır: geçersiz tarihte ERR-412 değil, tarih mesajı döner. Örnek: kalanı negatif olan
  personel (Elif Sahin'in reddedilen talebi onaylı sayılınca kalan −3), bitiş başlangıçtan bir gün
  önce (gün = 0, bakiye kontrolü tetiklenebilir). Revizyon: review round 1 R1-1 — eski örnek (kalan
  0, gün −4) bakiye kontrolünü hiç tetiklemediği için sırayı ayırt etmiyordu.
- [x] CB-3 — Kalıcı repro testi: düzeltmeden önce kırmızı olduğu gösterilir, düzeltmeden sonra
  yeşildir ve suite'te kalır.

## Preserved behavior
- [x] PB-1 — Geçerli talep akışı değişmez: kaydedilir, `Beklemede` doğar, no 9, listeye
  yönlendirilir (0001/AC-5). Tek günlük talep (başlangıç = bitiş) geçerlidir, 1 gün sayılır; bu
  sınır ayrı bir testle kanıtlanır.
- [x] PB-2 — Geçmiş tarihli talep kabul edilir (BR-6, 0001/AC-9).
- [x] PB-3 — Eşikler değişmez: ERR-412 (kalan < gün, metin `dahili 4130`) ve servis eşiği
  (kalan == gün sessizce kaydedilmez) aynen kalır (0001/AC-6 → 0003, 0001/AC-7).
- [x] PB-4 — Spec 0001, 0002 ve 0003'ün diğer tüm testleri değişmeden yeşil kalır. Değişen tek
  mevcut test `test_gecersiz_tarih_sessizce_yonlendirir`'dir (beklentisi yeni davranışa çevrilir).

## Out of scope
- Aynı `except` bloğunun yuttuğu diğer hatalar (olmayan veya sayı olmayan personel numarası),
  servisteki `except` bloğu.
- Formun girilen değerleri hatırlaması, istemci tarafı doğrulama, tarih biçimi değişikliği.
- Eşik kararı (Q-1), ERR-412 metni.

## Evidence (VERIFY, 2026-11-21 — bağımsız QA subagent'ı)
Full: `CHECK GREEN (full, 3 steps, 3 s)` @ce8a48e (VERIFY'da koşuldu, 32 test; fix-1 sonrası güncel
yeşil). Review: round 1 → R1-1 real (fix-1, A-1), R1-2 noise; round 2 temiz. Hassas alan: her kriter
repo dışında geçici kopyada kasıtlı bozmayla kırmızıya düşürüldü.

| Acceptance criterion | Evidence | Status |
|---|---|---|
| CB-1 | `tests/test_invalid_dates.py::test_repro_gecersiz_tarih_gorunur_hata` (5 parametre) · `tests/test_new_request.py::test_gecersiz_tarih_sessizce_yonlendirir` + S-1/S-2 — bozma: eski yutma geri → 6 red | PASS |
| CB-2 | `tests/test_invalid_dates.py::test_repro_bitis_baslangictan_once` · `tests/test_invalid_dates.py::test_tarih_dogrulamasi_bakiye_kontrolunden_once` — bozma: `end < start` kontrolü kaldırıldı → red; doğrulama bakiye kontrolünden sonraya → red (ERR-412) | PASS |
| CB-3 | Repro testleri `480d076` `app.py`'sine karşı → 8 failed, 1 passed (`302 == 400`); HEAD'de yeşil ve suite'te | PASS |
| PB-1 | `tests/test_invalid_dates.py::test_tek_gunluk_talep_gecerli` · `tests/test_new_request.py::test_gecerli_talep_beklemede_dogar` + S-3 — bozma: `end <= start` geçersiz → red | PASS |
| PB-2 | `tests/test_new_request.py::test_gecmis_tarihli_talep_kabul` — bozma: geçmiş tarih reddi → red | PASS |
| PB-3 | `tests/test_new_request.py::test_form_esigi_err412_dahili_4127` · `tests/test_new_request.py::test_servis_esigi_kalan_esit_gun_sessizce_kaydedilmez` — bozma: form `<`→`<=` → red | PASS |
| PB-4 | 32 testin hepsi yeşil; test diff'i `480d076..ce8a48e` yalnız `test_gecersiz_tarih_sessizce_yonlendirir` + yeni dosya; `specs/done/` diff'i boş — bozma: `calc_days` `+1` kaldırıldı → 8 red | PASS |

Ekran görüntüleri: S-1 önce (`480d076`, sessiz liste), S-2 sonra (hata sayfası), S-3 sonra (korunan
geçerli akış) — agent-captured (headless Edge, repo dışı), judged by tahamucasiroglu at SHIP,
2026-11-21. Repoda saklanmaz.

## Definition of Done
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Criterion ↔ evidence table complete for CB-* **and** PB-* (UI: before/after screenshots, agent-captured, judged at SHIP)
- [x] Spec moved to `specs/done/` (immutable there)
