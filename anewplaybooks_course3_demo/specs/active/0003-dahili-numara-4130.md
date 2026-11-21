# Spec 0003 — Hata sayfasında dahili numara 4130 (mini)

- Status: Approved
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
- [ ] CB-1 — Form eşiğinde (kalan < gün, örn. Elif Sahin, 1 gün) gösterilen hata metni birebir:
  `HATA: ERR-412 NullReferenceGibi - bakiye yetersiz. IT'ye bildirin: dahili 4130`. VERIFY, ERR-412
  sayfasının önce (4127, `fae8d4f`) ve sonra (4130) ekran görüntüsünü alır, SHIP'te değerlendirilir.
- [ ] CB-2 — `app.py`'de ve uygulamanın ürettiği sayfalarda `4127` kalmaz; ERR-412 sayfasında
  `4127` görünmez. Test adı tarihsel olduğu için hariçtir.

## Preserved behavior
- [ ] PB-1 — ERR-412 sayfası aynı koşulda (kalan < gün) ve aynı biçimde döner: yönlendirme yok,
  talep kaydedilmez (talep sayısı değişmez), sayfa düzeni (`PAGE`) aynı.
- [ ] PB-2 — Eşikler değişmez: form `<`, servis `<=` (Q-1). Kalan == gün olan talep hata vermeden
  kaydedilmez, bir gün eksiği kaydedilir (spec 0001 AC-7).
- [ ] PB-3 — Spec 0001 ve spec 0002'nin diğer tüm testleri değişmeden yeşil kalır. Değişen tek test
  `test_form_esigi_err412_dahili_4127`'dir: beklenen metin, 4127'nin görünmediği assert'i (yeni
  beklenti, hiçbir beklenti zayıflamaz) ve açıklaması.

## Out of scope
- Mesajın geri kalanı ("ERR-412", "NullReferenceGibi", "IT'ye bildirin") ve renk/biçim.
- Eşik kararı (Q-1), yutulan hatalar, başka hata metinleri.
- Spec 0001'in test adı (tarihsel olarak 4127 içerir, korunur).

## Definition of Done
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Criterion ↔ evidence table complete for CB-* **and** PB-* (UI: before/after screenshots, agent-captured, judged at SHIP)
- [ ] Spec moved to `specs/done/` (immutable there)
