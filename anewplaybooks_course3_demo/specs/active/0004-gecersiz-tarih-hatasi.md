# Spec 0004 — Geçersiz tarihte görünür hata (mini)

- Status: Approved
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
- [ ] CB-1 — Başlangıç veya bitiş tarihi çözülemezse (örn. `abc`), boşsa, hiç gönderilmemişse ya da
  takvimde yoksa (örn. `2026-02-30`): talep kaydedilmez (talep sayısı değişmez), yönlendirme olmaz,
  HTTP 400 ile sayfa düzeni içinde birebir şu mesaj döner:
  `HATA: Tarihler gecersiz. Baslangic ve bitis gecerli bir tarih olmali; bitis baslangictan once olamaz.`
  VERIFY önce (sessiz yönlendirme, `480d076`) ve sonra (hata sayfası) ekran görüntüsünü alır, SHIP'te
  değerlendirilir.
- [ ] CB-2 — Bitiş başlangıçtan önceyse (örn. 2026-12-10 → 2026-12-05): talep kaydedilmez, CB-1
  ile aynı mesaj ve davranış (bugün −4 günle kaydediliyor). Tarih doğrulaması bakiye kontrolünden
  önce yapılır: geçersiz tarihte ERR-412 değil, tarih mesajı döner (örn. kalanı 0 olan Elif Sahin,
  bitiş < başlangıç).
- [ ] CB-3 — Kalıcı repro testi: düzeltmeden önce kırmızı olduğu gösterilir, düzeltmeden sonra
  yeşildir ve suite'te kalır.

## Preserved behavior
- [ ] PB-1 — Geçerli talep akışı değişmez: kaydedilir, `Beklemede` doğar, no 9, listeye
  yönlendirilir (0001/AC-5). Tek günlük talep (başlangıç = bitiş) geçerlidir, 1 gün sayılır; bu
  sınır ayrı bir testle kanıtlanır.
- [ ] PB-2 — Geçmiş tarihli talep kabul edilir (BR-6, 0001/AC-9).
- [ ] PB-3 — Eşikler değişmez: ERR-412 (kalan < gün, metin `dahili 4130`) ve servis eşiği
  (kalan == gün sessizce kaydedilmez) aynen kalır (0001/AC-6 → 0003, 0001/AC-7).
- [ ] PB-4 — Spec 0001, 0002 ve 0003'ün diğer tüm testleri değişmeden yeşil kalır. Değişen tek
  mevcut test `test_gecersiz_tarih_sessizce_yonlendirir`'dir (beklentisi yeni davranışa çevrilir).

## Out of scope
- Aynı `except` bloğunun yuttuğu diğer hatalar (olmayan veya sayı olmayan personel numarası),
  servisteki `except` bloğu.
- Formun girilen değerleri hatırlaması, istemci tarafı doğrulama, tarih biçimi değişikliği.
- Eşik kararı (Q-1), ERR-412 metni.

## Definition of Done
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Criterion ↔ evidence table complete for CB-* **and** PB-* (UI: before/after screenshots, agent-captured, judged at SHIP)
- [ ] Spec moved to `specs/done/` (immutable there)
