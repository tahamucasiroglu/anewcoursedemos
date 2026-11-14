# Spec 0020 — yeni-surum-ve-yeniden-uret

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0020-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: specs/active/0007-uretilen-videolar.md (eski 0007-c: yeni sürüm ve yeniden üret) — bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun'
- Depends on: 0001, 0002, 0011, 0003, 0004, 0013, 0005, 0015, 0009, 0006, 0017, 0018, 0007, 0019

## Intent
İçerik üreticisi, beğenmediği bir videoyu "boşa para harcamadan, komple baştan yapmak zorunda
kalmadan" iyileştirmek istiyor. Üretilen Videolar sayfasındaki **Yeni sürüm** ile seçili sürümün
ayarlarından yalnızca birini (ör. ses motorunu Supertonic'ten Chatterbox'a) değiştirir; sistem yalnızca
etkilenen aşamaları yeniden üretir (BR-9) ve sonuç aynı videonun yeni bir sürümü olur, eski sürümler
korunur. **Yeniden üret** ise konsept metnini değiştirmek içindir: kullanıcı önce Konseptler
sayfasındaki düzenlemeye gider ("yeniden üretle düzenleyeceksin yeniden üretmeden önce"), onaylanan
revizyonla her şey baştan yeni bir sürüm olarak üretilir. Başarı: yerelden yerele ses değişimi hiçbir
online çağrı yapmaz; online çağrı gerektiren her başlatma açık bir onay ister; hiçbir tekrar istek ikinci
bir üretim ya da harcama doğurmaz. Bilinçli olarak yapılmayanlar: sahne/cümle bazında kısmi üretim,
tahmini maliyet, video kodu modelinin yeni sürümde değiştirilmesi.

## Requirements
1. **Yeni sürüm:** sağ panelde, seçili (tamamlanmış) sürümün ayarlarından başlayan bir "Yeni sürüm"
   alanı vardır. v1'de değiştirilebilen ayarlar ve yeniden üretilen aşamalar (BR-9, genişletilmiş):
   | Değişen ayar | Yeniden üretilen aşamalar | Online çağrı |
   |---|---|---|
   | Ses motoru / ses | seslendirme + birleştirme (altyazı açıksa birleştirmede yeni sese göre zamanlanır; render gerekmez, Karar 5) | yalnızca online ses motoru seçilirse |
   | Video motoru | video kodu + render + birleştirme (ses korunur) | her zaman (video kodu online LLM'le üretilir) |
   | Görsel modeli | görsel + render + birleştirme | yalnızca online görsel modeli (OpenAI gpt-image) seçilirse |
   | Altyazı aç/kapa, ilerleme çubuğu aç/kapa | yalnızca birleştirme (ajanın sabit şablonu, Karar 5) | hayır |
   | Sessiz videoya geçiş (ses kapalı) | yalnızca birleştirme (altyazı kaldırılır; render gerekmez, Karar 5) | hayır |
   Birden fazla ayar değişirse etkilenen aşamaların birleşimi çalışır. Diğer aşamaların çıktıları önceki
   sürümden aynen kullanılır. Hiçbir ayar değişmediyse yeni sürüm başlatılamaz.
2. Seçenek listeleri (Karar 5): video motoru — HyperFrames (varsayılan); Remotion yalnızca ajanda
   kuruluysa listelenir (0009'daki isteğe bağlı bileşen, varsayılan kurulu değil); Revideo ve Motion
   Canvas yok. Ses motoru — Supertonic 3 (yerel, varsayılan), Chatterbox (yerel), ElevenLabs (online).
   Görsel modeli — Z-Image-Turbo (yerel), FLUX.2 klein 4B (yerel), OpenAI gpt-image (online; 0004
   kataloğundaki tek online görsel modeli). Ollama hiçbir rolde seçilemez (E-4). Kullanıcının yetkisi olmayan model
   listelenmez (BR-13). Ses motoru yerelden yerele değiştirildiğinde para harcanmaz; kullanıcı yerel
   modelleri beğenmezse online ses motoruna geçebilir (brief).
3. Başlatmadan önce sistem çalışacak aşamaları ve "online model çağrısı yapılacak / yapılmayacak"
   bilgisini önizler. Online çağrı gerekiyorsa kullanıcı çalışacak aşamaları ve uyarıyı gösteren bir
   onay penceresini onaylamadan iş oluşmaz (H-3); online çağrı yoksa onay sorulmadan başlar. Tahmini
   tutar gösterilmez (güvenilir tahmin verisi yok, BR-12 bilgi amaçlı).
4. **Yeniden üret:** eylem satırına **Yeniden üret** düğmesi eklenir. Tıklanınca kullanıcı Konseptler
   sayfasında bu videonun konseptinin düzenlemesine götürülür (düzenleme döngüsü yalnızca 0015'te,
   Ç-11; Video oldu istisnası Gereksinim 15'te; doğrudan metin düzenleme ya da fikir yazıp online düzenleme → revizyon modalı → onay, BR-6).
   Düzenleme ya da revizyon iptal edilirse hiçbir iş oluşmaz. Revizyon onaylanınca konseptin yerini
   alır ve aynı videonun yeni bir sürümü olarak tüm aşamalar baştan çalışır (BR-9); eski sürümler
   silinmez. Konsept metni her durumda online LLM'le video kodu ürettiği için başlatma Gereksinim 3'teki
   onayı ister.
5. Yeniden üretimde düzenleme onaylandı ama metin hiç değişmediyse başlat pasiftir ve "Konsept
   değişmedi" der; kullanıcı aynı metinle gerçekten tekrar üretmek isterse açık bir "Yine de baştan
   üret" seçeneği ve online çağrı uyarılı onay istenir.
6. Yeni sürüm ve yeniden üretim, 0006'daki üretim sırasının sonuna bir iş olarak eklenir; Konseptten
   Video sayfasında da görünür; BR-10 (durdur/devam), BR-11 ve çıkış/ban anında durdurma (Ç-4, sahibi
   0006 Req 16: çalışan sürüm işi Durduruldu olur, Sırada sürüm işi Sırada kalır ve sıra durur) aynen
   geçerlidir.
7. Her online çağrının token ve maliyeti kaydedilir (0013); sürümün ücretine ve aylık harcamaya eklenir
   (BR-12).
8. Tekrarlanan başlatma isteği (çift tıklama, aynı isteğin yeniden gönderilmesi) ikinci bir iş
   oluşturmaz (H-3). Bir video için aynı anda yalnızca bir sürüm sırada, üretimde, durdurulmuş ya da
   ajan bekliyor olabilir; bu sırada gelen yeni istek iş oluşturmaz ve "Bu video için zaten üretimde bir
   sürüm var" mesajı görünür.
9. Üretimi süren sürümün durumu sürüm listesinde ve Üretilen Videolar listesindeki öğede küçük bir
   rozetle görünür, en geç 5 sn içinde yenilenir. Durum adları 0006 Req 13'teki tek listedendir (Ç-5: Sırada ·
   Üretiliyor + o anki aşama · Durduruldu · Ajan bekleniyor · Hata · Tamamlandı). Başarısız bir sürüm
   önceki sürümleri etkilemez.
10. Hata durumundaki sürümde hata nedeni sade Türkçe metinle görünür ve iki eylem vardır: **Tekrar dene**
    (yarım kalan aşamadan devam, BR-10) ve **Sürümü kaldır** (başarısız sürüm kaydı ve yarım aşama
    çıktıları kaldırılır; önceki sürümler ve yapılmış harcama kayıtları değişmez).
11. Ajan çevrimdışıyken yeni sürüm ve yeniden üretim başlatılabilir ama yerel aşamalar başlamaz, sürüm
    "Ajan bekleniyor" durumunda kalır; ajan bağlanınca kendiliğinden devam eder (BR-11).
12. Videonun Sırada, Üretiliyor ya da Ajan bekleniyor durumunda bir sürümü varken 0019'daki **Sil**
    pasiftir ve "Önce üretimi durdurun" der. Durduruldu ya da Hata durumundaki sürümü olan video
    silinebilir; silmeyle o iş sıradan kalkar, hiçbir aşama ve online çağrı çalışmaz.
13. Yeni sürüm ve yeniden üretimin render'ı 0018'deki kısıtlı render ortamından aynen geçer; ayrı bir
    render yolu açılmaz (H-4 bu spec'te ikinci kez açılmaz).
14. Kullanıcı yalnızca kendi videoları için yeni sürüm ve yeniden üretim başlatabilir (BR-1); başka bir
    kullanıcının videosu için istek "bulunamadı" döner, iş oluşmaz ve ajana komut gitmez.
15. **"Video oldu" ret kurallarının istisnası:** 0015 Req 1 (Video oldu konsepti kaydetme/yeniden üretim
    reddi) ve 0006 Req 10 (Onaylandı dışındaki konsepti sıraya alma reddi) yalnızca bu spec'in iki
    bağlamında genişletilir:
    - **Yeniden üret bağlamı:** bu videonun Yeniden üret eyleminden gelindiğinde, videoya bağlı "Video
      oldu" konsept 0015 döngüsüyle düzenlenebilir (doğrudan metin düzenleme ya da online düzenleme →
      revizyon → onay); revizyon onayından sonra kullanıcı Gereksinim 3'teki önizleme ve onay adımına
      döner. Konseptler sayfasında bu bağlam dışında kalem pasif kalır.
    - **Sürüm işi:** yeni sürüm ve yeniden üretim işi, konsept "Video oldu" durumunda kalarak 0006
      sırasına kabul edilir; konsept durumu değişmez ve onaylanan konsept listesinde görünmez. Sürüm işi
      sıradan çıkarılırsa ya da video silinirse (Gereksinim 12) konsept yine "Video oldu" kalır.
    Bu iki bağlam dışında gelen kaydetme, yeniden üretim ve sıraya alma istekleri 0015 ve 0006'daki
    gibi reddedilir.

## User experience
- Flow (yeni sürüm): Kullanıcı Üretilen Videolar'da bir sürümü seçer → sesi beğenmez → "Yeni sürüm"de ses
  motorunu değiştirir → yeniden üretilecek aşamaları ve "online model çağrısı yapılmayacak" bilgisini
  görür → başlatır (online çağrı olsaydı onay penceresi çıkardı) → sürüm listesinde yeni sürüm "Sırada →
  Üretiliyor (aşama) → Tamamlandı" ilerler → yeni sürümü seçip izler.
- Flow (yeniden üret): "Yeniden üret" → Konseptler sayfasında konseptin düzenlemesi açılır → metni
  düzenler ya da fikir yazıp online düzenletir → revizyonu onaylar → çalışacak aşamalar (hepsi) ve online
  çağrı uyarısıyla onay penceresi → onaylar → yeni sürüm sıraya girer.
- States:
  - Yeni sürüm: değişiklik yok (başlat pasif) · etkilenen aşama önizlemesi · onay penceresi (online çağrı
    uyarısıyla) · onaysız başlatma (online çağrı yok) · başlatıldı · reddedildi ("Bu video için zaten
    üretimde bir sürüm var").
  - Yeniden üret: düzenleme açık · düzenleme iptal (hiçbir şey olmaz) · "Konsept değişmedi" (başlat
    pasif) · "Yine de baştan üret" onayı · onaylandı (yeni sürüm sıraya girer).
  - Sürüm durumu: Sırada · Üretiliyor (aşama) · Durduruldu · Ajan bekleniyor · Hata (neden + Tekrar dene /
    Sürümü kaldır) · Tamamlandı; listedeki öğede durum rozeti.
  - Sil: "Önce üretimi durdurun" (pasif).
  - Yetkisiz: başka kullanıcının videosu → "bulunamadı".
- Design reference: none — the flow above is the design (Yeni sürüm alanının ve onay penceresinin görseli
  yok; sayfa yerleşimi 0007'deki `source/üretilen videolar.png`).
- Accessibility: Yeni sürüm kontrolleri, Yeniden üret, Tekrar dene, Sürümü kaldır ve onay pencereleri
  klavyeyle erişilebilir (Tab, Enter/Space); onay pencereleri odağı içine alır ve Esc ile iptal edilir;
  pasif düğmelerin nedeni fare ve klavye odağında ipucu olarak okunur; durum rozetinin metin karşılığı
  vardır (yalnızca renk değil); kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-3 Maliyet doğuran çağrılar (yeni sürüm ve yeniden üretimin online çağrıları, onay,
  tekrarlanan başlatma, maliyet kaydı).
- H-4: bu spec yeni bir kod çalıştırma yolu açmaz; render 0018'deki kısıtlı ortamdan geçer; plan'da
  "ayrı render yolu yok" architecture testiyle kanıtlanır (AC-16).
- Bağımlılık: 0006 (sıra, iş durumları, durdur/devam, çıkışta durdurma), 0017 (aşamalar ve aşama
  çıktıları, ses kataloğu), 0004 (katalog: OpenAI gpt-image), 0009 (Remotion isteğe bağlı bileşeni), 0018 (kısıtlı render), 0015 (konsept düzenleme döngüsü), 0013 (harcama
  kaydı), 0007 (sayfa ve sürüm listesi), 0019 (Sil) shipped olmadan bu spec plan'a geçmez.
- BR-9 aşama düzeyinde uygulanır.
- Zaman ölçüleri: durum yenileme en geç 5 sn (0001'deki 2–3 sn polling ile tutarlı).
- Split: done — 0020 Yeni sürüm ve yeniden üret / kardeşler: 0007, 0019
- Out of scope: sahne ya da cümle bazında kısmi yeniden üretim (backlog); tahmini maliyet hesabı; video
  kodu modelinin yeni sürümde değiştirilmesi; tamamlanmış tek bir sürümün silinmesi (backlog); konsept
  düzenleme döngüsünün kendisi (0015); üretim sırasının ve aşamaların kendisi (0006, 0017); OpenAI TTS
  (Karar 5); fal Z-Image-Turbo ve Nano Banana gibi ek online görsel modelleri (backlog).

## Acceptance criteria
- [ ] AC-1 — Yeni sürüm önizlemesi Gereksinim 1'deki tabloya uyar (tablo tabanlı test): ses motoru değişimi → seslendirme + birleştirme (altyazı açıkken de render çalışmaz); video motoru değişimi → video kodu + render + birleştirme ve "online model çağrısı yapılacak"; görsel modeli değişimi → görsel + render + birleştirme (yerel modele geçişte "online model çağrısı yapılmayacak", OpenAI gpt-image'a geçişte "yapılacak"); altyazı ya da ilerleme çubuğu değişimi → yalnızca birleştirme; sessiz videoya geçiş → yalnızca birleştirme (altyazı açık olsa da render çalışmaz); iki ayar birlikte değişince aşamaların birleşimi; yerel → yerel ses değişiminde "online model çağrısı yapılmayacak"; hiçbir ayar değişmemişse başlatma pasiftir.
- [ ] AC-2 — Seçenek listeleri: video motorunda HyperFrames listelenir, Remotion yalnızca ajanda kuruluysa (0009 isteğe bağlı bileşen) listelenir, kurulu değilken listelenmez, Revideo ve Motion Canvas hiç listelenmez; ses motorunda Supertonic 3, Chatterbox (yerel) ve ElevenLabs (online) listelenir; görsel modelinde Z-Image-Turbo, FLUX.2 klein 4B (yerel) ve OpenAI gpt-image (online) listelenir; Ollama ve kullanıcının yetkisi olmayan model listelenmez ve istekle gönderilse de reddedilir.
- [ ] AC-3 — Yeni sürüm üretiminde yalnızca önizlenen aşamalar çalışır, diğer aşamaların çıktıları önceki sürümden aynen kullanılır; önceki sürümün dosyaları ve kaydı değişmez. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-4 — Yerel bir ses motorundan başka bir yerel ses motoruna geçişte hiçbir online model çağrısı yapılmaz (stub sayacı 0), onay penceresi çıkmaz ve sürümün ücreti "Yerel — ücretsiz" olur. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-5 — Online çağrı gerektiren yeni sürüm ya da yeniden üretim, çalışacak aşamaları ve "online model çağrısı yapılacak" uyarısını gösteren onay olmadan başlamaz; onay penceresinde tahmini tutar yoktur; iptal edilirse hiçbir iş oluşmaz ve hiçbir çağrı yapılmaz. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-6 — Yapılan her online çağrının token ve maliyeti kaydedilir; sürümün ücreti bu kayıtların toplamına, aylık harcama da bu tutar kadar artar (BR-12). (H-3)
- [ ] AC-7 — Aynı yeni sürüm/yeniden üret isteğinin tekrarı (çift tıklama, yeniden gönderim) ikinci bir iş oluşturmaz; bir video için Sırada, Üretiliyor, Durduruldu ya da Ajan bekleniyor durumunda bir sürüm varken gelen yeni istek de iş oluşturmaz ve "Bu video için zaten üretimde bir sürüm var" mesajı görünür. (H-3, kasıtlı bozma ile kanıt)
- [ ] AC-8 — Üretimi süren sürümün durumu 0006'daki adlarla (Sırada / Üretiliyor + aşama adı / Durduruldu / Ajan bekleniyor / Hata / Tamamlandı) sürüm listesinde ve liste öğesindeki rozette görünür ve en geç 5 sn içinde güncellenir; başarısız sürüm Hata gösterir, önceki sürümler oynatılabilir kalır.
- [ ] AC-9 — Yeni sürüm ve yeniden üretim işi 0006'daki sıranın sonuna eklenir ve Konseptten Video sayfasında da görünür; durdurulduğunda tamamlanan aşamaları korunur ve devamda yarım aşamadan sürer (BR-10); kullanıcı çıkış yaptığında çalışan sürüm işi "Durduruldu" olur, Sırada sürüm işi Sırada kalır ve sıra durur (Ç-4, 0006 Req 16).
- [ ] AC-10 — Ajan çevrimdışıyken başlatılan yeni sürüm "Ajan bekleniyor" durumunda kalır ve hiçbir yerel aşama başlamaz; ajan bağlanınca üretim kendiliğinden devam eder (BR-11).
- [ ] AC-11 — "Yeniden üret" kullanıcıyı Konseptler sayfasında bu videonun konseptinin düzenlemesine götürür; düzenleme ya da revizyon iptal edilirse hiçbir iş oluşmaz; revizyon onaylanıp online çağrı onayı verilince tüm aşamalar baştan çalışır ve sonuç yeni bir sürüm olarak eklenir; eski sürümler ve onların konsept metinleri korunur. (H-3)
- [ ] AC-12 — Yeniden üretimde onaylanan metin öncekiyle aynıysa başlat pasiftir ve "Konsept değişmedi" yazar; "Yine de baştan üret" seçilip online çağrı uyarılı onay verilmeden hiçbir iş oluşmaz. (H-3)
- [ ] AC-13 — Hata durumundaki sürümde hata nedeni sade Türkçe metinle görünür (stack trace yok); "Tekrar dene" yarım kalan aşamadan devam eder ve tamamlanmış aşamaları yeniden çalıştırmaz; "Sürümü kaldır" onaydan sonra başarısız sürümü listeden ve yarım çıktılarını diskten kaldırır, önceki sürümler ve harcama kayıtları değişmez.
- [ ] AC-14 — Videonun Sırada, Üretiliyor ya da Ajan bekleniyor durumunda sürümü varken Sil pasiftir ve "Önce üretimi durdurun" ipucunu gösterir; Durduruldu ya da Hata durumundaki sürümü olan video silinince iş sıradan kalkar ve sonrasında hiçbir aşama ya da online çağrı çalışmaz. (H-3)
- [ ] AC-15 — Başka bir kullanıcının videosu için yeni sürüm, yeniden üret, tekrar dene ve sürümü kaldır istekleri "bulunamadı" döner, iş oluşmaz ve ajana komut gitmez (kasıtlı bozma ile kanıt).
- [ ] AC-16 — Yeni sürüm ve yeniden üretimin render'ı yalnızca 0018'deki kısıtlı render yolundan geçer; ayrı bir render yolu yoktur (architecture testi).
- [ ] AC-17 — Yeni sürüm kontrolleri, Yeniden üret, Tekrar dene, Sürümü kaldır ve onay pencereleri yalnızca klavyeyle (Tab, Enter/Space, Esc) kullanılabilir; onay penceresi odağı içine alır; pasif düğmelerin nedeni fare ve klavye odağında ipucu olarak okunur; durum rozetinin erişilebilir metni vardır.
- [ ] AC-18 — "Video oldu" istisnası (Gereksinim 15): Yeniden üret'ten gelindiğinde videoya bağlı Video oldu konsept 0015 döngüsüyle düzenlenip revizyonu onaylanabilir ve kullanıcı önizleme/onay adımına döner; aynı konsept için Yeniden üret bağlamı dışından gelen kaydetme ya da yeniden üretim isteği reddedilir ve online çağrı yapılmaz (0015 AC-1 korunur). Yeni sürüm ve yeniden üretim işi konsept Video oldu iken sıraya kabul edilir, konsept durumu Video oldu kalır ve onaylanan konsept listesinde görünmez; sürüm işi sıradan çıkarılınca konsept yine Video oldu kalır; Video oldu konsepti video bağlamı olmadan "Üretim sırasına al" ile sıraya alma isteği reddedilir (0006 AC-10 korunur).

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
