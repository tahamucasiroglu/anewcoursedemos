# Spec 0015 — Konsept düzenleme döngüsü

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0015-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0005-konseptler.md` (Req 17'nin kalem kısmı, Req 19–20; AC-15'in düzenleme talimatı kısmı, AC-18'in düzenleme kısmı, AC-25…AC-29, AC-34–35'in düzenlemeye düşen kısmı; S-5, G-2, G-3'ün düzenleme kısmı, G-7, G-8, G-20; Ç-11) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-3 Maliyet doğuran çağrılar (talimatla yeniden üretim, sınırsız "Tekrar düzenle" döngüsü, revizyonda BR-15)
- Depends on: 0001, 0002, 0003, 0004, 0013, 0005, 0014

## Intent
İçerik üreticisi, üretilen bir konseptte bir hata, hukuki risk ya da beğenmediği bir yer fark ettiğinde konsepti
düzeltmek istiyor: sahne metinlerini doğrudan değiştirerek ya da ne düzeltilmesini istediğini yazıp online modelle
yeniden ürettirerek (`ilk_promt.txt`). Talimatla gelen sonuç bir revizyon olarak ayrı bir modalda gösterilir; kullanıcı
onaylarsa konseptin yerini alır, silerse konsept aynen kalır, istemezse tekrar düzenler (BR-6). Başarı: kullanıcı
beğenmediği bir düzenlemeden kayıpsız geri dönebilir, döngü sınırsızdır ama her tur yalnızca kullanıcının açık
eylemiyle başlar ve her çağrının maliyeti kaydedilir (H-3). Konsept düzenleme döngüsü yalnızca bu spec'tedir (Ç-11);
diğer sayfalar buraya bağlantı verir. Bilinçli olarak yapılmayanlar: sahne ekleme/silme/sıralama, videosu yapılmış
konseptin yeniden üretimi (0020), silme (0016).

## Requirements
Kaynak: `ilk_promt.txt` (Konseptler bölümü, düzenleme), `docs/domain.md` (BR-1, BR-6, BR-12, BR-15, BR-16),
`Promtlar/0011` (LLM çıktısı güvenilmeyen veri); sabah karar özeti Ç-11.

**A. Düzenlenebilirlik**
1. Üretilenler satırında kalem (düzenle) ikonu vardır. Düzenleme yalnızca **Üretildi** ve **Onaylandı** durumundaki
   konseptlerde mümkündür (S-5). Sırada ve Üretiliyor konseptte kalem pasiftir ve "Önce üretim sırasından çıkarın"
   açıklaması görünür (sıra kilidi 0006 Req 12 / AC-14). Video oldu konseptte kalem pasiftir ve "Bu konseptin videosu var; metin değişikliği
   Üretilen Videolar'daki 'Yeniden üret' ile yapılır" açıklaması görünür (0020 bu döngüyü yeniden kullanır).
2. Konseptler sayfası belirli bir konsepte bağlantıyla açılınca (ör. Konseptten Video'daki "Konseptler'de düzenle")
   o konsept Üretilenler'de seçili ve düzenleme alanında açık gelir (Ç-11).

**B. Doğrudan düzenleme**
3. Kalem ikonuyla düzenleme alanı düzenlenebilir hale gelir. Kullanıcı sahne başlığını, ekran tarifini, seslendirme
   metnini ve süreyi değiştirip "Kaydet" der; değişiklik doğrudan konseptin yerini alır (modal yok, online çağrı yok; G-2).
4. Elle düzenlemede sahne başlığı ve seslendirme metni boş bırakılamaz; süre pozitif tam saniye olmalıdır; konsept en
   az bir sahne içermelidir. Sahne ekleme, silme ve sıralama v1'de yoktur (yalnızca metin ve süre düzenlenir; G-20).
5. Kaydedilmemiş elle değişiklik varken başka bir konsepte geçilirse ya da sayfadan ayrılınca "Kaydedilmemiş
   değişiklikler kaybolacak" uyarısı çıkar; vazgeçilirse düzenleme sürer.
6. Onaylandı durumundaki konsept düzenlenip kaydedilirse "Onaylandı" kalır (S-5).

**C. Talimatla yeniden üretim ve revizyon modalı**
7. Düzenleme alanının altındaki talimat alanına kullanıcı ne düzeltilmesini istediğini yazıp "Yeniden üret" der; bu,
   seçili profilin konsept üretim modeliyle online yeniden üretimdir. Kaydedilmemiş elle değişiklik varsa konsept bu
   değişikliklerle birlikte gönderilir (brief: "fikriyle beraber ya da değiştirerek"; G-2). Düzenleme talimatı model
   talimatına talimat olarak değil "veri" olarak işaretlenmiş bölümde girer (`docs/security.md`). Yeniden üretim de
   0014'ün talimat kurallarını (profil, ince ayar, doğruluk, web araması) uygular.
8. Sonuç bir **konsept revizyonu** olarak sayfanın üstünde ayrı bir modalda gösterilir. Modalda üç eylem vardır:
   **Onayla** (revizyon konseptin yerini alır), **Sil** (revizyon atılır, konsept aynen kalır), **Tekrar düzenle**
   (yeni talimatla yeni revizyon üretilir ve aynı modalda gösterilir). Döngünün sınırı yoktur; her tur kullanıcının açık
   eylemiyle başlar (BR-6). Onaylı konseptin revizyonu onaylanırsa konsept "Onaylandı" kalır (S-5).
9. Revizyon modalı dışına tıklamayla kapanmaz (ücretli revizyon kazara kaybolmasın); Esc "Revizyon silinsin mi?" onayı
   sorar; onaylanırsa revizyon atılır, vazgeçilirse modal açık kalır (G-7).
10. Kelime denetimi seçili profilde açıksa revizyon da 0014'ün eşiğiyle denetlenir ve en fazla 1 kez otomatik yeniden
    üretilir; yine aşılırsa revizyon "kelime tekrarı" uyarısıyla modalda gösterilir (G-8, BR-15).
11. Aynı konseptte aynı anda yalnızca bir yeniden üretim çalışır; çift tıklama ya da ikinci istek ikinci bir çağrı
    başlatmaz (G-3).
12. Bu spec'in her online çağrısı (yeniden üretim, revizyondaki BR-15 yeniden üretimi, web araması) harcama kaydına
    konsept üretimi rolüyle ve seçili profille girer (BR-12; biçim 0013). Yeniden deneme yalnızca 0013 kuralıyla yapılır;
    yarım kalan ya da biçimi bozuk çıktı revizyon olarak gösterilmez, hata olarak bildirilir ve otomatik tekrar edilmez (H-3).

**D. Yetki ve sahiplik**
13. Kullanıcı yalnızca kendi profillerinin konseptlerini düzenler ve yeniden ürettirir (BR-1).

## User experience
- Flow (doğrudan): Üretilenler'den konsepte tıklar → sağ altta sahneler görünür → kalem → metni/süreyi düzeltir →
  "Kaydet" → konsept güncellenir.
- Flow (talimatla): kalem → talimat alanına "şu kısmı düzelterek yeniden üret" yazar → "Yeniden üret" → revizyon modalı
  açılır → Onayla (konsept güncellenir) / Sil (konsept aynen kalır) / Tekrar düzenle (yeni talimat → yeni revizyon aynı
  modalda) → onaylayana ya da silene kadar.
- Flow (bağlantıdan): Konseptten Video'daki "Konseptler'de düzenle" → Konseptler sayfası o konsept seçili açılır.
- States:
  - Kalem: etkin (Üretildi, Onaylandı) · pasif + açıklama (Sırada, Üretiliyor, Video oldu).
  - Düzenleme alanı: görüntüleme · düzenleme · geçersiz girdi (alan yanında hata) · kaydediliyor · kaydedildi ·
    kaydetme hatası (sade mesaj, metin korunur) · kaydedilmemiş değişiklik uyarısı · yeniden üretiliyor (talimat alanı
    ve düğmeler pasif) · boş talimat (hata) · sağlayıcı hatası · üretilemedi (yarım kaldı / biçim bozuk).
  - Revizyon modalı: revizyon gösteriliyor · kelime tekrarı uyarılı revizyon · tekrar düzenle (talimat alanı) · yeniden
    üretiliyor · hata · Esc onayı ("Revizyon silinsin mi?").
  - Bağlantı: konsept bulundu (seçili açılır) · bulunamadı (sade mesaj, liste normal açılır).
  - Yetki yok: başka kullanıcının konseptine erişim "bulunamadı".
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü (sağ alt "üretilenleri düzenleme
  alanı"). Talimat alanı ve revizyon modalı için görsel yok — yukarıdaki akış tasarımdır. Renk: tema değişkenleri (K-7).
- Accessibility: kalem, Kaydet, Yeniden üret ve modal eylemleri klavyeyle erişilebilir; kalemin erişilebilir adı
  "Düzenle: <başlık>"; pasif kalemin açıklaması ekran okuyucuya okunur; modal açılınca odak modala gider, kapanınca açan
  kontrole döner; hata ve "yeniden üretiliyor" durumu ekran okuyucuya duyurulur; WCAG AA kontrast her iki temada.

## Constraints & out of scope
- Sensitive area: H-3 — AC-7, AC-10, AC-11, AC-12 kasıtlı bozma ile kanıtlanır (`docs/testing.md`). H-1 (BR-1) yalnızca
  tüketilir; AC-14 normal testle kanıtlanır (G-21).
- Online çağrılar yalnızca AiGateway üzerinden (Y-7); yeniden deneme, token sınırı ve harcama kaydının biçimi 0013'tedir.
- Testlerde gerçek LLM çağrısı yapılmaz (stub; `docs/testing.md`).
- Out of scope: sahne ekleme/silme/sıralama · revizyon geçmişi (önceki revizyonlara dönme) · videosu yapılmış konseptin
  metnini değiştirip yeniden üretme (0020 genişletir: "Yeniden üret" bağlamında *Video oldu* konsept bu döngüyle
  düzenlenebilir; Req 1 ve AC-1'deki ret bu bağlam dışında geçerlidir) · Konseptten Video'daki salt okunur ayrıntı alanı
  ve bağlantının kendisi (0006) · silme (0016) · konsept üretimi ve onay (0014).
- Split: done — 0015 Konsept düzenleme döngüsü / kardeşler: 0005, 0014, 0016

## Acceptance criteria
- [ ] AC-1 — Kalem ikonu Üretildi ve Onaylandı konseptlerde düzenleme alanını düzenlenebilir yapar; Sırada / Üretiliyor konseptte kalem pasiftir ve "Önce üretim sırasından çıkarın", Video oldu konseptte "… 'Yeniden üret' ile yapılır" açıklaması görünür; bu durumlardaki konsept için doğrudan kaydetme ya da yeniden üretim isteği reddedilir ve hiçbir online çağrı yapılmaz (S-5).
- [ ] AC-2 — Sahne metni ya da süresi elle değiştirilip kaydedilince "kaydediliyor" durumu görünür, sonra konsept güncellenir, yenilemede korunur ve hiçbir online çağrı yapılmaz; Onaylandı konsept "Onaylandı" kalır. Kaydetme başarısız olursa sade hata mesajı görünür ve düzenlenen metin kaybolmaz (G-2).
- [ ] AC-3 — Boş sahne başlığı, boş seslendirme metni, 0, negatif ya da ondalık süre içeren kaydetme reddedilir, alanın yanında hata görünür ve konsept değişmez; boş ekran tarifi kabul edilir; arayüzde sahne ekleme, silme ve sıralama kontrolü yoktur ve sahne sayısını değiştiren doğrudan istek reddedilir (G-20).
- [ ] AC-4 — Kaydedilmemiş elle değişiklik varken başka bir konsepte geçmek "Kaydedilmemiş değişiklikler kaybolacak" uyarısı gösterir; vazgeçilirse düzenleme ve değişiklikler kalır, onaylanırsa değişiklikler atılır. Kaydedilmemiş değişiklik varken "Yeniden üret" denince modele giden konsept bu değişiklikleri içerir (stub'a giden istekte doğrulanır; G-2).
- [ ] AC-5 — Talimat alanı boşken ya da yalnızca boşluk içerirken "Yeniden üret" reddedilir ve hata görünür; dolu talimatla yeniden üretim, mevcut konsepti ve talimatı modele gönderir — talimat "veri" olarak işaretlenmiş bölümde, profil/ince ayar/doğruluk kuralları 0014'teki gibi — ve sonucu sayfanın üstündeki revizyon modalında gösterir; bu sırada talimat alanı ve düğmeler pasiftir.
- [ ] AC-6 — Revizyon modalında "Onayla" revizyonu konseptin yerine koyar (Üretilenler ve detay yeni içeriği gösterir, durum değişmez; Onaylandı konsept Onaylandı kalır); "Sil" revizyonu atar ve konsept birebir aynı kalır (BR-6, S-5).
- [ ] AC-7 — "Tekrar düzenle" yeni talimatla yeni bir revizyon üretip aynı modalda gösterir; art arda en az 3 tur çalışır (döngü sınırı yok) ve her tur yalnızca kullanıcının açık eylemiyle başlar — kullanıcı eylemi olmadan hiçbir yeniden üretim çağrısı yapılmaz (H-3). Kasıtlı bozma ile kanıtlanır.
- [ ] AC-8 — Yeniden üretimde sağlayıcı hatası olursa ya da stub "yarım kaldı" veya biçimi bozuk çıktı döndürürse modal ya da düzenleme alanı sade bir hata mesajı gösterir, revizyon gösterilmez, otomatik yeni çağrı yapılmaz ve konsept değişmeden kalır.
- [ ] AC-9 — Revizyon modalı dışına tıklamak modalı kapatmaz; Esc "Revizyon silinsin mi?" onayı sorar — onaylanırsa revizyon atılır ve konsept aynen kalır, vazgeçilirse modal revizyonla açık kalır (G-7).
- [ ] AC-10 — Kelime denetimi açıkken stub eşiği aşan revizyon döndürürse tam 1 otomatik yeniden üretim yapılır; ikinci çıktı da aşarsa revizyon "kelime tekrarı" uyarısıyla modalda gösterilir ve üçüncü çağrı yapılmaz; kutu kapalıyken yeniden üretim yapılmaz (G-8, BR-15). Kasıtlı bozma: sınır kaldırılınca test kırmızıya döner.
- [ ] AC-11 — Yeniden üretim, revizyondaki BR-15 yeniden üretimi ve web araması çağrılarının her biri sağlayıcıya ulaştığında konsept üretimi rolü ve seçili profille bir harcama kaydı üretir; stub'a giden çağrı sayısı kayıt sayısına eşittir. Bir "Yeniden üret" ya da "Tekrar düzenle" eylemi, sürekli 5xx ve sürekli eşik aşımı dönen stub ile en fazla 2 × 3 = 6 konsept çağrısı yapar; 4xx'te tam 1 çağrı (BR-12, H-3). Kasıtlı bozma: kayıt atlanınca ya da sayfa düzeyinde tekrar eklenince test kırmızıya döner.
- [ ] AC-12 — Aynı konsept için "Yeniden üret"e art arda iki kez basılması ya da yeniden üretim sürerken gelen ikinci istek ikinci bir çağrı başlatmaz (stub'a tek çağrı gider) (G-3, H-3).
- [ ] AC-13 — Konseptler sayfası bir konsept bağlantısıyla açılınca o konsept Üretilenler'de seçili ve düzenleme alanında açık gelir (filtre dışındaki durumda olsa bile); konsept bulunamazsa ya da başka kullanıcıya aitse sade "Konsept bulunamadı" mesajı görünür ve sayfa normal açılır (Ç-11).
- [ ] AC-14 — Kullanıcı A, kullanıcı B'nin konseptini kaydetme, yeniden üretme ya da revizyonunu onaylama/silme girişiminde "bulunamadı" alır, hiçbir online çağrı yapılmaz ve B'nin konsepti değişmez (BR-1). Girişsiz istekler reddedilir.
- [ ] AC-15 — Tüm tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile gider (K-6); düzenleme ve revizyon modalı yalnızca klavyeyle kullanılabilir, modal açılınca odak modala geçer, kapanınca açan kontrole döner; kalemin erişilebilir adı "Düzenle: <başlık>"tır; hata ve ilerleme durumları ekran okuyucuya duyurulur.

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
