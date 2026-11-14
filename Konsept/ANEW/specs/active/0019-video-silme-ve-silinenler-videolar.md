# Spec 0019 — video-silme-ve-silinenler-videolar

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0019-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: specs/active/0007-uretilen-videolar.md (eski 0007-b: video silme ve Silinenler'in video tarafı) — bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun'
- Depends on: 0001, 0002, 0011, 0003, 0009, 0014, 0016, 0006, 0017, 0007

## Intent
İçerik üreticisi, istemediği bir videoyu Üretilen Videolar sayfasından kaldırmak, ama yanlışlıkla
kaybetmemek istiyor. "Sil" videoyu tüm sürümleriyle Silinenler sayfasının Videolar sekmesine gönderir;
orada süresiz saklanır ve konu önerisinde "bu video beğenilmedi" sinyali olarak kullanılır ("negatif
prompt gibi düşün"). Silinenler'den "Kalıcı sil" denince kayıt ve bilgisayardaki dosyalar geri
alınamaz biçimde gider (BR-7). Başarı: silinen video bir daha listede görünmez, kalıcı silmede yetim
dosya ya da yetim kayıt kalmaz. Bilinçli olarak yapılmayanlar: geri yükleme (Ç-2), tek sürüm silme,
Silinenler sayfasının kendisi ve Konseptler sekmesi (0016).

## Requirements
1. Üretilen Videolar sayfasında izleme yerinin altındaki eylem satırına **Sil** düğmesi eklenir. Sil,
   onaydan sonra videonun **tamamını, tüm sürümleriyle** listeden kaldırır ve Silinenler'in Videolar
   sekmesine gönderir (BR-7, soft delete); dosyalar diskte kalır. Tek sürüm silme v1'de yoktur.
2. Sil sunucu tarafında bir işlemdir; ajan çevrimdışıyken ve video dosyası bulunamadığında da çalışır.
3. **Silinenler — Videolar sekmesi:** 0016'da kurulan Silinenler sayfasının Videolar sekmesi silinen
   videoları listeler (0016'daki Konseptler sekmesiyle aynı satır biçiminde; en az başlık ve silinme
   tarihi, en yeni silinen üstte). Videolar süresiz saklanır. Sekmede video yokken 0016'nın boş durum
   metni görünür.
4. **Kalıcı sil:** Silinenler'de bir video için "Kalıcı sil" onaydan sonra video kaydını ve tüm
   sürümlerinin dosyalarını ajan aracılığıyla kullanıcının bilgisayarından siler; işlem geri alınamaz
   (BR-7).
   - Ajan çevrimdışıyken kalıcı silme yapılamaz; düğme pasiftir ve "Ajan bağlı değil" der; kayıt
     Silinenler'de kalır (gizli "bekleyen silme" durumu oluşmaz).
   - Dosyaların bir kısmı silinemezse (sistem oynatıcısında açık, izin yok vb.) kayıt Silinenler'de
     kalır ve "Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin" görünür; tekrar denendiğinde
     kalan dosyalar silinir. Yetim dosya ya da yetim kayıt bırakılmaz. Zaten diskte olmayan dosya
     silinmiş sayılır.
5. Video silinince (ve kalıcı silinince) bağlı konsept "Video oldu" durumunda kalır; BR-4 engeli sürer
   (konu tekrar önerilmez).
6. Silinenler'deki video, aynı profilde konu önerisi üretiminde "beğenilmedi" negatif sinyali olarak
   kullanılır (E-2, BR-8 genişler). Kalıcı silinen video Silinenler'den çıktığı için bu sinyalden de
   çıkar; BR-4 engeli konsept üzerinden sürer.
7. Kullanıcı yalnızca kendi videolarını silebilir ve kalıcı silebilir (BR-1); başka bir kullanıcının
   videosu için yapılan istek "bulunamadı" döner ve ajana komut gitmez.

## User experience
- Flow: Kullanıcı Üretilen Videolar'da bir videoyu seçer → "Sil" → onay penceresi → video listeden kalkar
  → Silinenler sayfasında Videolar sekmesini açar → videoyu görür → "Kalıcı sil" → onay penceresi
  ("geri alınamaz") → kayıt ve dosyalar gider.
- States:
  - Sil: onay penceresi · iptal (hiçbir şey değişmez) · başarılı (video listeden kalkar, seçim temizlenir).
  - Videolar sekmesi: yükleniyor · boş (0016'nın metni) · dolu · hata (yeniden dene).
  - Kalıcı sil: onay penceresi · iptal · siliniyor · başarılı · pasif ("Ajan bağlı değil") · kısmi hata
    ("Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin").
  - Yetkisiz: başka kullanıcının videosu → "bulunamadı".
- Design reference: none — the flow above is the design (Silinenler sayfasının yerleşimi 0016'da).
- Accessibility: Sil ve Kalıcı sil düğmeleri klavyeyle erişilebilir (Tab, Enter/Space); onay pencereleri
  odağı içine alır, Esc ile iptal edilir; pasif Kalıcı sil düğmesinin nedeni fare ve klavye odağında
  ipucu olarak okunur; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: none — kalıcı silme geri alınamaz ama BR-7 ve normal testler yeterli (`docs/domain.md`);
  silme ve kalıcı silme online model çağrısı yapmaz.
- Bağımlılık: 0016 (Silinenler sayfası, iki sekme, sekme başına boş durum — Ç-2), 0007 (Üretilen
  Videolar sayfası), 0014 (konu önerisi üretimi ve negatif sinyal girdisi), 0009 (ajan) shipped olmadan
  bu spec plan'a geçmez.
- Sırada/üretimde sürümü olan videonun silinmesi kuralı, üretimi süren sürümleri getiren 0020'dedir.
- Split: done — 0019 Video silme ve Silinenler'in Videolar sekmesi / kardeşler: 0007, 0020
- Out of scope: Silinenler'den geri yükleme (Ç-2, v1'de yok); tek sürüm silme (backlog); Silinenler
  sayfasının kendisi ve Konseptler sekmesi (0016); otomatik temizleme ya da saklama süresi (süresiz
  saklanır).

## Acceptance criteria
- [ ] AC-1 — "Sil" onaydan sonra videoyu tüm sürümleriyle Üretilen Videolar listesinden kaldırır ve Silinenler'in Videolar sekmesinde gösterir; dosyalar diskte kalır; onay penceresinde iptal edilirse hiçbir şey değişmez.
- [ ] AC-2 — Sil, ajan çevrimdışıyken ve video dosyası diskte yokken de başarılı olur ve ajana komut gönderilmez.
- [ ] AC-3 — Videolar sekmesi silinen videoları başlık ve silinme tarihiyle, en yeni silinen üstte listeler; sekmede video yokken 0016'nın boş durum metni görünür; silinen video süre sınırı olmadan sekmede kalır.
- [ ] AC-4 — "Kalıcı sil" onaydan sonra video kaydını ve tüm sürüm dosyalarını kullanıcının bilgisayarından siler; kayıt bir daha hiçbir yerde görünmez; onay penceresinde iptal edilirse hiçbir şey değişmez.
- [ ] AC-5 — Ajan çevrimdışıyken "Kalıcı sil" pasiftir ve "Ajan bağlı değil" ipucunu gösterir; kayıt ve dosyalar değişmez.
- [ ] AC-6 — Kalıcı silmede dosyalardan biri silinemezse (ör. açık dosya) kayıt Silinenler'de kalır ve "Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin" görünür; tekrar denendiğinde kalan dosyalar silinip kayıt gider; diskte zaten olmayan dosya hata saymaz; işlem sonunda yetim dosya ya da yetim kayıt kalmaz.
- [ ] AC-7 — Video silindikten ve kalıcı silindikten sonra bağlı konsept "Video oldu" durumunda kalır ve aynı profilde konusu tekrar önerilmez (BR-4).
- [ ] AC-8 — Silinenler'deki bir videonun konsept başlığı, aynı profilde "Konudan üret" isteğinin negatif sinyal girdisinde "beğenilmedi" olarak yer alır; başka profilin isteğinde yer almaz; video kalıcı silinince bu girdiden çıkar.
- [ ] AC-9 — Başka bir kullanıcının videosu için sil ve kalıcı sil istekleri "bulunamadı" döner, hiçbir kayıt değişmez ve ajana komut gitmez (kasıtlı bozma ile kanıt: kullanıcı filtresi kaldırılınca test kırılır).
- [ ] AC-10 — Sil ve Kalıcı sil düğmeleri ile onay pencereleri yalnızca klavyeyle (Tab, Enter/Space, Esc) kullanılabilir; onay penceresi odağı içine alır; pasif Kalıcı sil düğmesinin nedeni fare ve klavye odağında ipucu olarak okunur; Videolar sekmesi yüklenirken gösterge, yüklenemezse sade Türkçe hata ve "Yeniden dene" görünür.

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
