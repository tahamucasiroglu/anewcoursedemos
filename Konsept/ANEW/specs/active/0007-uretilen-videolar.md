# Spec 0007 — uretilen-videolar

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0007-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: specs/active/0007-uretilen-videolar.md (eski 0007-a: liste, oynatıcı, dosya eylemleri, sürümleri görüntüleme) — bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun'
- Depends on: 0001, 0002, 0011, 0003, 0004, 0013, 0009, 0006, 0017

## Intent
İçerik üreticisi, ürettiği videoları tek bir sayfada izlemek ve incelemek istiyor. Üretilen Videolar
sayfasında seçili profilin videoları solda listelenir; seçilen video ortada ajanın yerel akışından
oynatılır (ADR-0002; akışın kendisi üretim tarafında tanımlıdır, bu spec onu kullanır); sağda
seslendirme ve sahneler, konsept metni, ayrıntılar ve ücret, videonun sürümleri görünür. Kullanıcı
videoyu bilgisayarındaki klasörde ya da sistem oynatıcısında açabilir ("klasörden açıp kendi
yükleyebilir"). Başarı: her sürümün gerçek maliyeti görünür, ajan kapalıyken sayfa anlaşılır biçimde
çalışmaya devam eder. Bilinçli olarak yapılmayanlar: yayınlama, videonun ajansız/başka cihazdan
izlenmesi, videoların sunucuya yüklenmesi, videonun indirilmesi ve düzenlenmesi; silme 0019'da,
yeni sürüm ve yeniden üret 0020'de.

## Requirements
1. Sayfa, üst bardan seçili profile ait (BR-2), silinmemiş ve **en az bir sürümü tamamlanmış**
   videoları solda bir liste olarak gösterir (ilk üretimi süren konsept 0006'da izlenir). Liste öğesi:
   başlık, video süresi, üretim tarihi (ilk tamamlanan sürümün bitiş zamanı), sürüm sayısı. Sıralama:
   üretim tarihine göre en yeni üstte. İlk 50 öğe yüklenir; "Daha fazla göster" ile 50'şer ek öğe gelir.
2. Listeden bir video seçilince ortadaki izleme yerinde videonun seçili sürümü (varsayılan: en son
   tamamlanan sürüm) oynatılabilir hale gelir; sağ panel aynı sürümün bilgilerini gösterir.
3. Oynatıcı videoyu kullanıcının bilgisayarındaki ajanın yerel akışından oynatır. Akışın yetkisi,
   yalnızca o bilgisayardan erişilebilmesi ve uzun videolarda kesilmemesi üretim tarafındaki akış
   sahibinde (0017 Req 21 ve AC-25, Ç-1) tanımlıdır ve kanıtlanır; bu spec yeniden tanımlamaz. Video dosyası
   sunucuya yüklenmez. Oynatıcıda oynat/duraklat, ilerleme konumu, ses ayarı ve tam ekran vardır.
   Ajan bağlı ve dosya mevcutken seçimden sonra en geç 3 sn içinde video oynatmaya hazır olur. Ses
   düzeyi tercihi tarayıcıda hatırlanır (tema gibi).
4. İzleme yerinin altında iki dosya eylemi vardır (aynı satıra 0019 **Sil**, 0020 **Yeniden üret**
   düğmesini ekler; brief'teki dört eylem böylece tamamlanır):
   - **Klasörde aç:** ajan, video dosyasının bulunduğu klasörü kullanıcının bilgisayarında dosya
     gezgininde açar.
   - **Sistem oynatıcısında aç:** ajan, video dosyasını bilgisayarın varsayılan video uygulamasında açar.
   Ajan 10 sn içinde cevap vermezse "Zaman aşımı" hatası görünür.
5. Sağ panelde şu bölümler vardır:
   - **Seslendirme ve sahneler:** sahneler sırasıyla, her birinin kısa başlığı ve seslendirme metni.
   - **Konsept metni:** seçili sürümün üretildiği konsept metni (her sürüm kendi metnini gösterir;
     güncel konsept ayrıca işaretlenmez).
   - **Ayrıntılar ve ücret:** sürümün online model maliyeti (BR-12; yerel aşamalar ücretsiz) ve
     videonun tüm sürümlerinin toplam maliyeti; ek bilgiler: kullanılan modeller ve motorlar (video
     kodu modeli, video motoru, görsel modeli, ses motoru), altyazı ve ilerleme çubuğu seçimleri,
     video süresi ve biçimi (yatay/dikey), üretimin başlangıç ve bitiş saati; sürümün seslendirmesinde
     AI ses beyanı açıksa "Yapay zekâ ile üretilmiş ses" satırı (Karar 5, beyan varsayılan açık).
   - Para yalnızca USD olarak, 2 ondalık haneyle gösterilir (E-3); $0,01'in altındaki tutar
     "<$0,01" olarak görünür ve ayrıntı ipucunda sent altı hassasiyetle tam değer okunur. Online
     çağrısı olmayan sürümde ücret "Yerel — ücretsiz" yazar.
   - Saatler Europe/Istanbul, HH:mm; süreler "57 sn", "30 dk 12 sn" biçiminde (E-1).
   LLM'in ürettiği metinler düz metin olarak gösterilir (`docs/security.md`).
6. **Video sürümleri:** sağ panelde videonun tüm sürümleri listelenir: sürüm numarası, üretim zamanı,
   önceki sürüme göre neyin değiştiği (ilk sürümde "İlk sürüm"), ücreti, durumu (durum adları 0006 Req
   13'teki tek listeden, Ç-5). Bir sürüm seçilince oynatıcı ve sağ panel o sürüme geçer.
7. Ajan çevrimdışıyken: liste, sağ panel ve sürüm listesi görünmeye devam eder; oynatıcı "Videoyu
   izlemek için ajan bağlı olmalı" mesajını gösterir (Ç-1, tek mesaj); Klasörde aç ve Sistem
   oynatıcısında aç pasiftir ve nedenini söyler. Ajan bağlanınca en geç 10 sn içinde oynatma yeniden
   mümkün olur.
8. Ajan bağlı olduğu hâlde yerel akışa ulaşılamazsa (site başka bir cihazdan açıldı vb.) izleme yeri
   "Bu video yalnızca ajanın kurulu olduğu bilgisayarda (cihaz adı) izlenebilir" yazar.
9. Video dosyası ajanın bilgisayarında bulunamazsa (ör. kullanıcı elle taşıdı/sildi) oynatıcı "Video
   dosyası bulunamadı" durumunu gösterir; iki dosya eylemi pasiftir; kayıt kalır, otomatik temizleme
   yapılmaz.
10. Kullanıcı yalnızca kendi videolarını görür ve yalnızca onlar üzerinde işlem yapabilir (BR-1);
    başka bir kullanıcının videosu için yapılan her istek "bulunamadı" olarak sonuçlanır ve ajana hiçbir
    komut gitmez.

## User experience
- Flow: Kullanıcı üst bardan profili seçer → Üretilenler sayfasını açar → soldaki listeden bir videoya
  tıklar → video ortada oynamaya hazır olur, sağda sahneler, konsept metni, ayrıntılar ve ücret, sürümler
  görünür → oynatır, sesi ayarlar, tam ekrana geçer → gerekirse "Klasörde aç" ya da "Sistem oynatıcısında
  aç" der → başka bir sürümü seçip onu izler.
- States:
  - Liste: yükleniyor · hata (yeniden dene) · profil yok ("Önce bir profil oluşturun") · profilde video
    yok (boş durum metni) · dolu · "Daha fazla göster" yükleniyor.
  - İzleme yeri: video seçilmedi (yer tutucu) · yükleniyor · oynatılabilir · ajan bağlı değil ("Videoyu
    izlemek için ajan bağlı olmalı") · yerel akışa ulaşılamıyor / başka cihaz · video dosyası bulunamadı
    · akış hatası (yeniden dene).
  - Dosya eylemleri: gönderiliyor · başarılı (kısa onay) · pasif (ajan yok / dosya yok) · zaman aşımı.
  - Sağ panel: video seçilmedi · yükleniyor · dolu; ücret: "Yerel — ücretsiz" · tutar · "<$0,01".
  - Sürümler: tek sürüm ("Sürüm 1") · çok sürüm.
  - Yetkisiz: başka kullanıcının videosu → "bulunamadı"; giriş yapılmamış → giriş sayfası.
- Design reference: `source/üretilen videolar.png` — commit `61cc824` sürümü: solda "üretilen listesi",
  ortada "izleme yeri", sağda "seslendirme metni - sahne - ayrıntılar ücretler ve ek bilgiler" olmak üzere
  üç sütun; üst bar 0001'deki gibi. Eylem düğmelerinin ve sürüm listesinin görseli yok — bunlar için
  "none — the flow above is the design". Renk: turuncu/kahve, açık mod esas, koyu mod destekli.
- Accessibility: liste öğeleri, "Daha fazla göster", oynatıcı kontrolleri (oynat/duraklat, ilerleme,
  ses, tam ekran), dosya eylemleri ve sürüm satırları klavyeyle erişilebilir (Tab, Enter/Space); ikon
  düğmelerinin erişilebilir adı var; pasif düğmeler nedenini ipucu olarak hem fareyle hem klavye
  odağında söyler; metin/arka plan kontrastı her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: none — yerel akış yetkisi (H-1) Ç-1 gereği akışın sahibi olan üretim spec'inde
  (0017) tanımlanır ve kasıtlı bozma ile kanıtlanır; bu spec yalnızca kullanır. Başka kullanıcının
  videosuna erişimin engellenmesi (BR-1) 0002/0011'deki genel filtreye dayanır ve AC-11 ile test edilir.
- Bu spec yeni bir kod çalıştırma yolu açmaz (render yok).
- Bağımlılık: 0006 (iş durumları, sıra), 0017 (üretim hattı, aşama çıktıları, video ve sürüm kaydı —
  konsept metni kopyası, modeller ve motorlar, altyazı/ilerleme seçimi, AI ses beyanı, başlangıç–bitiş
  saati; Gereksinim 5 ve AC-9/AC-10 bu kayda dayanır —, yerel akış — Ç-1), 0009 (ajan), 0013 (harcama kaydı), 0004 (model kataloğu) shipped olmadan bu spec
  plan'a geçmez.
- Zaman ölçüleri 0001 ile tutarlı: ajan durumu 10 sn; oynatmaya hazır olma 3 sn.
- Split: done — 0007 Üretilen videolar: liste, oynatıcı, dosya eylemleri, sürümleri görüntüleme / kardeşler: 0019, 0020
- Out of scope: video silme ve Silinenler'in Videolar sekmesi (0019); yeni sürüm, yeniden üret ve
  üretimi süren sürüm durumları (0020); yayınlama; videonun ajansız/başka cihazdan izlenmesi ve
  sunucuya yüklenmesi (ADR-0002); videonun indirilmesi; video düzenleme (kesme, kırpma); Günlük şeridi
  kayıtları (0021, 0017); üretim sırasının ve aşamaların kendisi (0006, 0017); yerel akış mekanizması
  (0017).

## Acceptance criteria
- [ ] AC-1 — Liste yalnızca üst barda seçili profilin silinmemiş videolarını gösterir; profil değişince liste yenilenir ve önceki seçim temizlenir; başka profilin videosu listede görünmez.
- [ ] AC-2 — Liste öğesi başlık, süre, üretim tarihi ve sürüm sayısını gösterir; öğeler üretim tarihine göre en yeni üstte sıralanır; hiç tamamlanmış sürümü olmayan video listede görünmez; 51 videolu profilde ilk açılışta 50 öğe ve "Daha fazla göster" görünür, tıklanınca 51. öğe eklenir.
- [ ] AC-3 — Boş durumlar: profil yokken "Önce bir profil oluşturun", profilde video yokken boş durum metni, video seçilmemişken izleme yerinde ve sağ panelde yer tutucu görünür.
- [ ] AC-4 — Listeden bir video fareyle ya da klavyeyle (Enter) seçilince oynatıcı en son tamamlanan sürümü yükler; sağ panel aynı sürümün sahnelerini (sırasıyla başlık + seslendirme metni), konsept metnini, ayrıntılarını ve ücretini gösterir.
- [ ] AC-5 — Ajan bağlı ve dosya mevcutken video seçimden sonra en geç 3 sn içinde oynatmaya hazır olur; oynat/duraklat, ilerleme konumu, ses ayarı ve tam ekran çalışır; ayarlanan ses düzeyi sayfa yeniden açıldığında aynı tarayıcıda korunur; video dosyası sunucuya hiç yüklenmez.
- [ ] AC-6 — Ajan çevrimdışıyken oynatıcı "Videoyu izlemek için ajan bağlı olmalı" gösterir, Klasörde aç ve Sistem oynatıcısında aç pasiftir ve nedenini ipucuyla söyler; liste, sağ panel ve sürümler görünmeye devam eder; ajan bağlanınca en geç 10 sn içinde oynatma yeniden mümkün olur.
- [ ] AC-7 — Video dosyası diskte yoksa oynatıcı "Video dosyası bulunamadı" gösterir, iki dosya eylemi pasiftir; video kaydı listede kalır ve otomatik silinmez.
- [ ] AC-8 — "Klasörde aç" ajanın dosya gezgininde videonun klasörünü, "Sistem oynatıcısında aç" videoyu varsayılan uygulamada açmasıyla sonuçlanır; gönderim sırasında düğme "gönderiliyor" durumundadır, başarıda kısa bir onay görünür; ajan 10 sn içinde cevap vermezse "Zaman aşımı" hatası görünür.
- [ ] AC-9 — Ayrıntılar ve ücret bölümü sürümün online maliyetini ve videonun tüm sürümlerinin toplamını USD, 2 ondalık haneyle gösterir; $0,01'in altındaki tutar "<$0,01" görünür ve ipucunda tam değer okunur; online çağrısı olmayan sürümde "Yerel — ücretsiz" yazar; ek bilgi olarak modeller ve motorlar, altyazı/ilerleme çubuğu seçimi, süre, biçim, başlangıç/bitiş saati (Europe/Istanbul, HH:mm) ve AI ses beyanı açıksa "Yapay zekâ ile üretilmiş ses" görünür; HTML ya da script içeren LLM metni düz metin olarak görünür ve çalışmaz.
- [ ] AC-10 — Sürüm listesi her sürümü numara, üretim zamanı, değişiklik özeti ("İlk sürüm" dahil), ücret ve durumla (0006 Req 13'teki adlarla) gösterir; bir sürüm seçilince oynatıcı ve sağ panel o sürüme geçer ve konsept metni o sürümün üretildiği metindir (sürümler arasında metin farklıysa her biri kendi metnini gösterir); tek sürümlü videoda yalnızca "Sürüm 1" görünür.
- [ ] AC-11 — Başka bir kullanıcının videosu için görüntüleme, sürüm ayrıntısı ve dosya eylemi istekleri "bulunamadı" döner ve ajana komut gitmez (kasıtlı bozma ile kanıt: kullanıcı filtresi kaldırılınca test kırılır).
- [ ] AC-12 — Giriş yapmamış kullanıcı sayfayı açmak istediğinde giriş sayfasına yönlenir; liste yüklenemezse sade bir Türkçe hata mesajı ve "Yeniden dene" görünür, "Yeniden dene" listeyi tekrar yükler (stack trace yok).
- [ ] AC-13 — Liste öğeleri, "Daha fazla göster", oynatıcı kontrolleri, iki dosya eylemi ve sürüm satırları yalnızca klavyeyle (Tab, Enter/Space) kullanılabilir; ikon düğmelerinin erişilebilir adı vardır; pasif düğmelerin nedeni fare ve klavye odağında ipucu olarak okunur.
- [ ] AC-14 — Yükleme ve akış durumları: liste, izleme yeri ve sağ panel yüklenirken yükleniyor göstergesi görünür; ajan bağlı olduğu hâlde yerel akışa ulaşılamazsa izleme yeri "Bu video yalnızca ajanın kurulu olduğu bilgisayarda (cihaz adı) izlenebilir" yazar; oynatma sırasında akış koparsa sade bir "akış hatası" mesajı ve "Yeniden dene" görünür.

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
