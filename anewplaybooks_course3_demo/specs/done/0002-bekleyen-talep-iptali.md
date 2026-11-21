# Spec 0002 — Bekleyen talep iptali

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: done

## Intent
İK, personelin henüz karara bağlanmamış (`Beklemede`) bir izin talebini geri çekebilmesini
istiyor. Bugün yanlış girilmiş veya vazgeçilmiş bir talep listede sonsuza kadar "bekleyen" olarak
kalıyor ve dashboard'daki Bekleyen sayısını şişiriyor. Başarı şu durumdur: bekleyen bir talep tek
adımda iptal edilir, listede iptal durumuyla görünmeye devam eder, Bekleyen sayısından düşer ve
mevcut davranışın geri kalanı değişmez (bakiye hesabı, hafta sonu sayımı, çelişkili eşik).
Bilerek **yapılmayan**: onaylanmış iznin iptali, onay/ret akışı, kimlik doğrulama, eşik kararı.

Netleştirme kararları (2026-11-21, tahamucasiroglu):
- İstek metnindeki "talebin günleri bakiyesine geri döner" ifadesi bugünkü kurala göre yorumlandı.
  Bekleyen talepler bakiyeden zaten düşülmüyor (BR-4), bu yüzden iptal kalan bakiyeyi
  **değiştirmez**. Bekleyenlerin bakiyeden düşülmesi (Q-2) bu işte karara bağlanmaz.
- Yalnız `Beklemede` talepler iptal edilebilir. İptal edilen talep silinmez, durumu `Iptal Edildi` olur.
- İptal, talepler listesinde bekleyen satırlardaki "Iptal" düğmesiyle yapılır.
- Bekleyen olmayan veya var olmayan bir talebin iptali sessizce yutulmaz, anlamlı bir mesaj gösterilir.
- Eşik çelişkisi (Q-1) için açık karar: **hangisinin doğru olduğunu bilmiyoruz**. İki davranış
  da korunur (form `<`, servis `<=`).
- Spec 0001'in karakterizasyon testleri değişmeden yeşil kalır.
- Öz-eleştiri boşlukları G-1…G-4 önerildiği gibi işlendi.

## Requirements
- R-1: Personel, durumu `Beklemede` olan bir izin talebini iptal edebilir. İptal edilen talebin
  durumu `Iptal Edildi` olur. Talep silinmez, diğer alanları (personel, tarihler, neden) değişmez.
- R-2: Yalnız `Beklemede` talepler iptal edilebilir. `Onaylandi`, `Reddedildi` veya
  `Iptal Edildi` durumundaki ya da var olmayan bir talep için yapılan iptal isteği hiçbir talebi
  değiştirmez ve kullanıcıya şu mesajı gösterir: `Yalniz bekleyen talepler iptal edilebilir.`
- R-3: İptal edilen talep dashboard'daki üç durum sayısının (Bekleyen / Onaylanan / Reddedilen)
  hiçbirinde sayılmaz. Dashboard'a yeni kutu eklenmez.
- R-4: İptal, hiçbir personelin hak, kullanılan veya kalan değerini değiştirmez (BR-4: bekleyen
  talepler zaten düşülmüyordu).
- R-5: Mevcut davranış korunur: `docs/domain.md` BR-1…BR-8 ve spec 0001'in kabul kriterleri.

## User experience
- Flow: Talepler sayfası (`/requests`) → bekleyen bir satırdaki "Iptal" düğmesine basılır →
  talepler sayfasına dönülür → satırın durumu `Iptal Edildi` görünür ve "Iptal" düğmesi kaybolur.
- States:
  - **Liste:** bekleyen satırlarda "Iptal" düğmesi vardır, diğer satırlarda yoktur.
  - **Başarı:** iptal sonrası liste. Satır yerinde durur, durum rozeti `Iptal Edildi` olur ve
    mevcut varsayılan gri renkle gösterilir (`status_badge` değişmez).
  - **Hata:** bekleyen olmayan veya var olmayan bir talep için iptal isteğinde sayfa düzeni
    içinde `Yalniz bekleyen talepler iptal edilebilir.` mesajı gösterilir.
  - **Boş / yükleniyor / yetkisiz:** yok. Seed'de her zaman talep var, sayfa sunucuda üretiliyor,
    kimlik doğrulama yok.
- Design reference: none — the flow above is the design. Mevcut sayfa düzeni (`PAGE`) ve tablo
  stili kullanılır.
- Accessibility: "Iptal" gerçek bir form düğmesidir. Klavyeyle (Tab + Enter) ulaşılır ve görünür
  etiketi `Iptal` metnidir.

## Constraints & out of scope
- Sensitive area: Bakiye hesabı; Talep oluşturma ve bakiye eşiği (`docs/domain.md`). Talep
  durumları ve bakiye etkisi değişiyor. Bu yüzden her kriter, kasıtlı bozmayla kırmızıya düştüğü
  gösterilerek kanıtlanır (VERIFY).
- Kimlik doğrulama yok (`docs/security.md`, AuthN / AuthZ). Sayfaya erişen herkes herhangi bir
  bekleyen talebi iptal edebilir. Bu mevcut duruşun devamıdır, yetkilendirme ayrı bir talep olur.
- Yeni kod kuralları geçerlidir: hata yutulmaz, kullanıcı girdisi kaçışlı yazılır, yeni `|safe`
  eklenmez (`docs/conventions.md`, `docs/security.md`). Mevcut `PAGE` şablonundaki `|safe` borçtur
  ve değişmez.
- Kapsam dışı: onaylanmış iznin iptali; onay/ret akışı; bekleyenlerin bakiyeden düşülmesi (Q-2);
  eşik kararı (Q-1); dashboard'a "İptal" kutusu eklenmesi; CSRF koruması (R-S3); iptal nedeni;
  geri alma (iptali geri çevirme).

## Acceptance criteria
- [x] AC-1 — Talepler listesinde "Iptal" düğmesi yalnız `Beklemede` satırlarda görünür: seed'de
  talep no 2, 3, 4 ve 5'te vardır; 1, 6, 7 ve 8'de yoktur.
- [x] AC-2 — Bekleyen bir talebin iptali (örn. talep no 2, Mehmet Demir): talebin durumu
  `Iptal Edildi` olur, diğer alanları değişmez, talepler sayfasına dönülür ve talep listede
  `Iptal Edildi` durumuyla, "Iptal" düğmesi olmadan görünür. Talep sayısı 8 kalır.
- [x] AC-3 — Bir talep iptal edildikten sonra dashboard: **Bekleyen 3 · Onaylanan 1 ·
  Reddedilen 3**. İptal edilen talep hiçbir kutuda sayılmaz.
- [x] AC-4 — Ayşe Yılmaz'ın bekleyen talebi (no 5, 3 gün) iptal edildiğinde bakiyesi
  **26 / 12 / 14** kalır. Diğer personelin satırları da değişmez (BR-4).
- [x] AC-5 — Bekleyen olmayan bir talebin iptal isteği (`Onaylandi` no 1, `Reddedildi` no 6 ve
  iptal edilmiş bir talebin ikinci kez iptali) talebi değiştirmez. Yönlendirme olmaz, sayfa düzeni
  içinde doğrudan `Yalniz bekleyen talepler iptal edilebilir.` mesajı döner.
- [x] AC-6 — Var olmayan (örn. 999) veya sayı olmayan (örn. `abc`) bir talep numarası için iptal
  isteği hiçbir talebi değiştirmez. Yönlendirme olmaz, aynı mesaj doğrudan döner.
- [x] AC-7 — Preserved: seed verisinde (iptal yapılmadan) dashboard 4/1/3, Ayşe 26/12/14,
  hafta sonu sayımı, form ve servis eşikleri, geçersiz tarih ve geçmiş tarih davranışı. Spec
  0001'in testleri **hiç değiştirilmeden** yeşil kalır.
- [x] AC-8 — "Iptal" bir `<button>` (form submit) öğesidir ve görünür metni `Iptal`'dir. Formu
  yalnız talep numarasını taşır, kullanıcı metni (ad, neden) düğmeye veya forma yazılmaz. İptal
  yalnız form gönderimiyle yapılır: bir adresi açmak (GET) hiçbir talebi iptal etmez.

## Evidence (VERIFY, 2026-11-21 — bağımsız QA subagent'ı)
Full: `CHECK GREEN (full, 3 steps, 3 s)` @1db3341 (HEAD'de koşuldu, 24 test). Residue: plan
`## Build evidence` @967b055. Hassas alan: her kriter repo dışında geçici kopyada kasıtlı bozmayla
kırmızıya düşürüldü, hiçbir bozma commit edilmedi.

| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-1 | `tests/test_cancel_request.py::test_iptal_dugmesi_yalniz_bekleyen_satirlarda` + S-1 — bozma: düğme koşulu kaldırıldı → red | PASS |
| AC-2 | `tests/test_cancel_request.py::test_bekleyen_talep_iptal_edilir` + S-2 — bozma: durum metni `Iptal` → red | PASS |
| AC-3 | `tests/test_cancel_request.py::test_iptal_sonrasi_dashboard_3_1_3` — bozma: Bekleyen filtresi iptali saydı → red | PASS |
| AC-4 | `tests/test_cancel_request.py::test_iptal_bakiyeyi_degistirmez` — bozma: `get_used_days` iptali saydı → red (26/15/11) | PASS |
| AC-5 | `tests/test_cancel_request.py::test_bekleyen_olmayan_talep_iptal_edilemez` + S-3 — bozma: durum kontrolü kaldırıldı → red | PASS |
| AC-6 | `tests/test_cancel_request.py::test_olmayan_veya_gecersiz_numara` — bozma: `ValueError` dalı yönlendirdi → red | PASS |
| AC-7 | spec 0001'in 15 testi değişmeden yeşil; test dosyalarının diff'i `6d4ed3f..1db3341` boş — bozma: `calc_days` `+1` kaldırıldı → 0001'in 4 testi red | PASS |
| AC-8 | `tests/test_cancel_request.py::test_iptal_dugmesi_form_yalniz_numara` · `tests/test_cancel_request.py::test_get_ile_iptal_olmaz` + S-1 — bozma: uca GET eklendi → red; forma neden alanı eklendi → red | PASS |

Ekran görüntüleri: S-1 liste, S-2 başarı, S-3 hata — agent-captured (headless Edge, repo dışı),
judged by tahamucasiroglu at SHIP, 2026-11-21. Görüntüler repoda saklanmaz (Evidence policy).

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
| Review findings: real / noise / deferred | 1 / 0 / 0 |
| Regressions introduced | 0 |
| Bugs escaped to production | 0 |
