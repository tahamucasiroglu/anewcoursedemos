# Spec 0003 — Hata sayfasında dahili numara 4130 (mini)

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0003-plan.md`
- Self-critique: done
- Source: İK talebi (e-posta), 2026-11-21 — "Hata sayfalarındaki dahili numara 4127 yerine 4130 olmalı"
- Design: none — yalnız metin değişir; mevcut ERR-412 sayfası düzeni korunur
- Supersedes: 0001/AC-6

## Intent
İK, yetersiz bakiye hata sayfasındaki IT dahili numarasının güncellenmesini istiyor: 4127 artık
geçerli değil, doğru numara 4130. Kullanıcı ERR-412 sayfasında `dahili 4130` görür. Mesajın geri
kalanı ve eşik davranışı aynen kalır.

Kararlar (2026-11-21, tahamucasiroglu): yalnız sayı değişir; spec 0001'in testi adını korur
(`test_form_esigi_err412_dahili_4127`, ship edilmiş kayıtlar bu adı gösteriyor), yalnız beklenen
metni ve açıklaması güncellenir. Sensitive area: Talep oluşturma ve bakiye eşiği (`docs/domain.md`).
Değişen satır eşik dalının içinde, eşik mantığı değişmez. Her kriter VERIFY'da kasıtlı bozmayla
kanıtlanır. Öz-eleştiri boşlukları G-1…G-4 önerildiği gibi işlendi.

## Changed behavior
- [x] CB-1 — Form eşiğinde (kalan < gün, örn. Elif Sahin, 1 gün) gösterilen hata metni birebir:
  `HATA: ERR-412 NullReferenceGibi - bakiye yetersiz. IT'ye bildirin: dahili 4130`. VERIFY, ERR-412
  sayfasının önce (4127, `fae8d4f`) ve sonra (4130) ekran görüntüsünü alır, SHIP'te değerlendirilir.
- [x] CB-2 — `app.py`'de ve uygulamanın ürettiği sayfalarda `4127` kalmaz; ERR-412 sayfasında
  `4127` görünmez. Test adı tarihsel olduğu için hariçtir.

## Preserved behavior
- [x] PB-1 — ERR-412 sayfası aynı koşulda (kalan < gün) ve aynı biçimde döner: yönlendirme yok,
  talep kaydedilmez (talep sayısı değişmez), sayfa düzeni (`PAGE`) aynı.
- [x] PB-2 — Eşikler değişmez: form `<`, servis `<=` (Q-1). Kalan == gün olan talep hata vermeden
  kaydedilmez, bir gün eksiği kaydedilir (spec 0001 AC-7).
- [x] PB-3 — Spec 0001 ve spec 0002'nin diğer tüm testleri değişmeden yeşil kalır. Değişen tek test
  `test_form_esigi_err412_dahili_4127`'dir: beklenen metin, 4127'nin görünmediği assert'i (yeni
  beklenti, hiçbir beklenti zayıflamaz) ve açıklaması.

## Out of scope
- Mesajın geri kalanı ("ERR-412", "NullReferenceGibi", "IT'ye bildirin") ve renk/biçim.
- Eşik kararı (Q-1), yutulan hatalar, başka hata metinleri.
- Spec 0001'in test adı (tarihsel olarak 4127 içerir, korunur).

## Evidence (VERIFY, 2026-11-21 — bağımsız QA subagent'ı)
Full: `CHECK GREEN (full, 3 steps, 4 s)` @778bde2 (24 test). Review round 1 temiz, fix turu yok.
Hassas alan: her kriter repo dışında geçici kopyada kasıtlı bozmayla kırmızıya düşürüldü.

| Acceptance criterion | Evidence | Status |
|---|---|---|
| CB-1 | `tests/test_new_request.py::test_form_esigi_err412_dahili_4127` (beklenen metin 4130) + S-1/S-2 — bozma: metin 4127'ye döndü → red | PASS |
| CB-2 | `tests/test_new_request.py::test_form_esigi_err412_dahili_4127` (`"4127" not in`) + `app.py`'de `4127` araması boş — bozma: `PAGE`'e 4127 eklendi → red | PASS |
| PB-1 | `tests/test_new_request.py::test_form_esigi_err412_dahili_4127` (200, yönlendirme yok, talep sayısı 8) — bozma: yönlendirme → red; kayıt → red. Düzen: `PAGE` diff'te yok + S-1/S-2 birebir aynı (yalnız sayı) | PASS |
| PB-2 | `tests/test_new_request.py::test_servis_esigi_kalan_esit_gun_sessizce_kaydedilmez` · `tests/test_new_request.py::test_gecerli_talep_beklemede_dogar` — bozma: form `<`→`<=` → red | PASS |
| PB-3 | 24 testin hepsi yeşil; test diff'i `fae8d4f..778bde2` yalnız `ERR_412` ve bu test; `specs/done/` diff'i boş — bozma: `calc_days` `+1` kaldırıldı → 6 test red | PASS |

Ekran görüntüleri: S-1 ERR-412 önce (4127, `fae8d4f`), S-2 ERR-412 sonra (4130) — agent-captured
(headless Edge, repo dışı), judged by tahamucasiroglu at SHIP, 2026-11-21. Repoda saklanmaz.

## Definition of Done
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Criterion ↔ evidence table complete for CB-* **and** PB-* (UI: before/after screenshots, agent-captured, judged at SHIP)
- [x] Spec moved to `specs/done/` (immutable there)
