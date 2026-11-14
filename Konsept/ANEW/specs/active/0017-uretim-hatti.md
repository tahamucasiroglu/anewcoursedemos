# Spec 0017 — Üretim hattı

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0017-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0006-uretim-konseptten-video.md` (bölüm C, E; Req 1, 4, 5'in videoya yansıması, Req 11–18, 23; AC-1–4, AC-8 çıktı kısmı, AC-18–27, AC-34–35, AC-36/37 hat-panel kısmı) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-3 Maliyet doğuran çağrılar (sırayla üretim, analiz ve video kodu çağrıları, online görsel/ses, yeniden deneme ve düzeltme döngüsü, token limiti, maliyet kaydı).
- Sensitive area: H-1 Kimlik ve yetki — yalnızca ajanın yerel video akışı yetkisi (Ç-1; kısa ömürlü, yalnızca sahibinin bilgisayarından).
- Depends on: 0001, 0004, 0008, 0012, 0013, 0021, 0009, 0006

## Intent
İçerik üreticisi "Sırayla üret" dediğinde (0006) sıra yukarıdan aşağı gerçekten videoya dönüşüyor: her iş
aşama aşama (konsept inceleniyor → video kodu → görseller → seslendirme → render → birleştirme) ilerliyor,
kullanıcı nerede olduğunu birkaç saniye içinde görüyor, durdurup kaldığı yerden devam ettirdiğinde tamamlanmış
hiçbir aşama yeniden çalışmıyor ve para boşa gitmiyor. Online aşamalar Pi'de kullanıcının API key'iyle
yapılıyor, key hiçbir zaman ajana gitmiyor; yerel aşamalar kullanıcının bilgisayarındaki ajanda çalışıyor;
seçilen altyazı ve ilerleme çubuğu videoya gerçekten yansıyor. Biten videolar sayfanın sağındaki
Üretilenler panelinde listelenip ajanın yerel akışından izleniyor. Başarı: 200 işlik bir gece sırası tek bir
bozuk konsepte takılmadan, sistematik bir hatada ise boşuna harcamadan ilerliyor; her online çağrının
maliyeti kaydediliyor. Bilinçli olarak yapılmayanlar: LLM kodunun çalıştığı kısıtlı render ortamı (0018);
Üretilen Videolar sayfası, yeni sürüm, video silme (0007, 0019, 0020); Günlük geçmişi (0021); yayınlama.

## Requirements
**A. Ayarların videoya yansıması**
1. İş, 0006'da işe sabitlenen ayarlarla üretilir; sonradan yapılan ayar değişikliği çalışan işin çıktısını
   değiştirmez.
2. **Altyazı** açıksa üretilen videoda seslendirilen metin ekranda, sesle zamanlı olarak görünür ve o an
   okunan **kelime** vurgulanır (rengi değişir). Altyazı LLM tarafından üretilmez; ajanın sabit şablonundan
   gelir (Karar 5: ASS karaoke, FFmpeg katmanı); vurgu rengi temadan değil şablondan gelir, stil seçimi yoktur.
   Altyazı kapalıysa videoda altyazı yoktur. Seçim gerçekten çalışmalıdır (ilk_promt).
3. **İlerleme çubuğu** açıksa videonun üst kenarında, videonun başından sonuna kadar geçen süreyle orantılı
   dolan bir çubuk görünür; ajanın sabit şablonundan gelir (LLM üretmez). Kapalıysa çubuk yoktur.
4. Ses modeli "Kapalı" ise seslendirme yapılmaz, videonun ses izi sessizdir ve altyazı eklenmez. Görsel modeli
   "Kapalı — koddan çiz" ise hiçbir görsel modeli çağrılmaz, sahneler yalnızca video koduyla çizilir.

**B. Aşamalar ve sıranın işlenmesi**
5. Sırayla üret (0006) ile sıra yukarıdan aşağı işlenir (BR-10): en üstteki iş başlar, bitince iş
   *Tamamlandı* olur, konsepti *Video oldu*'ya geçer (BR-16), iş sıradan kalkar ve bir sonraki iş kendiliğinden
   başlar. Aynı anda kullanıcı başına tek iş çalışır; ajandaki yerel GPU işleri sıralı çalışır (6 GB VRAM).
6. Her iş aşamalarını sırayla ve her birinin durumuyla gösterir: **Konsept inceleniyor** (analiz) → **Video
   kodu üretiliyor** → **Görseller üretiliyor** → **Seslendirme yapılıyor** → **Video kodu videoya
   dönüştürülüyor (render)** → **Birleştiriliyor**. Aşama durumları: bekliyor · çalışıyor · tamamlandı ·
   atlandı · hata. Görsel modeli kapalıysa görsel aşaması, ses modeli kapalıysa seslendirme aşaması
   "atlandı" görünür. Sayfadaki durum, değişiklikten sonra en geç 5 sn içinde güncellenir.
7. **Konsept inceleniyor** aşaması, seçili video kodu modeliyle yapılan tek bir kısa planlama çağrısıdır
   (sahne süreleri, görsel promptları, ses metni bölümleri); ayrı model seçimi yoktur. Video kodu **sahne
   başına** üretilir; render sahne başına yapılır ve birleştirme aşamasında tek videoya dönüşür (Karar 5, K-5).
8. Üretim tarayıcıdan bağımsızdır: sayfa kapatılsa ya da başka sayfaya gidilse de sürer; sayfa yeniden
   açıldığında sıranın ve çalışan işin güncel durumu görünür.

**C. Durdur, devam, hata**
9. 0006'daki **Durdur** (ya da çıkış, ya da ban — Ç-4) çalışan işi o anki aşamada keser; tamamlanmış
   aşamaların çıktıları korunur. O anda süren bir online çağrı kesilmez, tamamlanır ve maliyeti kaydedilir;
   ama yeni hiçbir online çağrı başlamaz. **Kaldığı yerden devam et**, yarım kalan aşamayı baştan, kalan
   aşamaları sırayla çalıştırır ve ardından sıraya devam eder (BR-10). Tamamlanmış hiçbir aşama yeniden çalışmaz.
10. *Durduruldu* ya da *Hata* durumundaki iş sıradan çıkarıldığında (0006) tamamlanmış aşama çıktıları,
    konseptin metni değişmediği sürece saklanır ve konsept yeniden sıraya alınıp üretildiğinde kullanılır
    (BR-9 mantığı). Konseptin metni değiştiyse (0015) tüm aşamalar baştan çalışır.
11. Bir aşama başarısız olursa iş **Hata** durumuna geçer; hatanın kısa ve sade açıklaması (iç detay, API key,
    stack trace olmadan) işin satırında görünür; tamamlanmış aşamalar korunur ve iş "Kaldığı yerden devam et"
    ile hatalı aşamadan yeniden denenebilir. Bir iş Hata olunca sıra bir sonraki işe geçer; **art arda 3 iş**
    hata verirse sıra kendiliğinden durur ve "Art arda 3 iş hata verdi — sıra durduruldu" uyarısı görünür.
12. Render aşaması hata döndürürse hata özeti, video kodu modeline **en fazla 1 kez** düzeltme için geri
    gönderilir (BR-15'teki "en fazla 1" ile aynı mantık); düzeltilen kodla da başarısız olursa iş Hata olur.
    Render öncesi kontrolden dönen ret de bu döngüyü kullanır (0018).

**D. Ajan ve online/yerel ayrımı**
13. Online aşamalar (analiz, video kodu, online görsel ya da online ses seçildiyse onlar) Pi'de yapılır;
    kullanıcının API key'leri hiçbir zaman ajana gitmez (Y-7, Y-8). Üretilen video kodu ve gerekli aşama
    çıktıları Pi'den yalnızca o kullanıcının kendi eşleştirilmiş ajanına (0012) iletilir; yerel aşamalar
    (yerel görsel, yerel ses, render, birleştirme) ajanda çalışır. Ajan online sağlayıcıya hiç istek atmaz.
14. Ajan bağlıysa iş başlamadan, hiçbir online çağrı yapılmadan önce ajandan gerekli araçların (Node, FFmpeg,
    Chromium, seçili yerel modeller — kurulumu 0009) varlığı sorulur; eksikse iş "Ajan kurulumu eksik: <araç>"
    açıklamasıyla Hata olur ve harcama kaydı oluşmaz.
15. Ajan çevrimdışıyken online aşamalar yine çalışır; iş ilk yerel aşamaya gelince **Ajan bekleniyor**
    durumunda kalır, sayfada "Ajan bağlı değil" uyarısı görünür (BR-11) ve sıradaki bir sonraki iş ajan gelene
    kadar başlamaz. Ajan yeniden bağlanınca iş kendiliğinden devam eder; ajan bir yerel aşamanın ortasında
    koparsa iş Ajan bekleniyor olur ve o aşama bağlantı dönünce baştan çalışır.

**E. Maliyet ve katalog**
16. Her online model çağrısının token sayısı (seste karakter sayısı) ve maliyeti kaydedilir (BR-12, 0013);
    üst bardaki aylık harcama bu çağrılarla güncellenir; işin satırında o işin o ana kadarki toplam maliyeti
    (USD, sent — E-3) görünür. Online çağrılarda yeniden deneme yalnızca geçici hatalarda ve sınırlıdır
    (`docs/conventions.md`). Video kodu çağrısının çıktı token limiti seçili modelin katalog değerinden gelir.
    Her online çağrıdan önce o sağlayıcı için kullanıcının kayıtlı API key'i olup olmadığına bakılır; key yoksa
    (hiç girilmemiş ya da 0004'te silinmiş) çağrı yapılmaz, iş harcama kaydı oluşmadan "API key yok — Ayarlar"
    açıklamasıyla *Hata* olur (0004 Req 11'deki silme onayının "sıradaki işler duracak" sözünü çalışma anında
    bu kural uygular; sıra Req 11'deki gibi ilerler ya da art arda 3 hatada durur).
17. Katalog karakter başı fiyatla genişletilir (Ç-8); online ses modeli **ElevenLabs**, 0006 Req 18'de tanımlanan
    "ses" türünde online bir giriş olarak kataloğa ve ses listesine "online" etiketiyle eklenir ve 0006'daki
    key/yetki kurallarına uyar. "ses" türü burada yeniden tanımlanmaz.

**F. Günlük ve AI beyanı**
18. İş başladı, iş bitti ve iş hata olayları 0001'in olay altyapısına yazılır ve Günlük şeridinde görünür
    (Ç-7, E-1): bitti/hata olayı konsept başlığını, süreyi ("57 sn", "30 dk 12 sn") ve başlangıç–bitiş
    saatini (Europe/Istanbul, HH:mm, ör. "10:30–11:00") içerir.
19. Seslendirmesi Supertonic 3 ile yapılan videonun yanına ajan "Yapay zekâ ile üretilmiş ses" beyanını
    içeren bir not dosyası yazar (lisans gereği, varsayılan açık); beyan video karesine gömülmez. Beyanın
    video ayrıntısında gösterimi 0007'dedir.

**G. Üretilenler paneli ve yerel video akışı (Ç-1)**
20. Sayfanın sağ sütunu (**Üretilenler**), seçili profilde bu sayfadan üretilip biten videoları en yeni üstte
    listeler. Liste dikey kesik çizginin solundadır; bir videoya tıklanınca kesik çizginin **sağında** bir
    izleme alanı açılır ve video oradan oynatılır.
21. Video, ajanın yalnızca bu bilgisayardan erişilebilen, kısa ömürlü yetkiyle açılan yerel akışından gelir;
    akış yalnızca videonun sahibine ve yalnızca o videoya açılır; yetkinin kısa ömrü oynatılan videoyu kesmez
    (yetki süresinden uzun bir video sonuna kadar izlenebilir). Akış yetkisi (token) loglarda, hata
    bildirimlerinde ve olay kaydında düz metin görünmez (0008 Req 8). Ajan çevrimdışıysa izleme alanı "Videoyu izlemek için ajan bağlı
    olmalı" der; dosya ajan klasöründe yoksa "Video dosyası bulunamadı" der. Bu akış burada kurulur; 0007 ve
    sonrakiler yeniden kullanır, yeniden tanımlamaz.

**H. Video sürüm kaydı**
22. Her tamamlanan video (ve 0020'deki her yeni sürümü) için bir sürüm kaydı saklanır: üretildiği konsept
    metninin o anki kopyası, kullanılan video kodu/görsel/ses modelleri ve video motoru, altyazı ve ilerleme
    çubuğu seçimi, AI ses beyanının yazılıp yazılmadığı (Req 19), başlangıç ve bitiş saati. Kayıt, konsept
    ya da profil ayarları sonradan değişse de değişmez. 0007'nin ayrıntılar bölümü ve 0020 bu kaydı okur;
    video dosyası yine ajanda kalır (Pi'ye yalnızca kayıt yazılır).

**I. Yeniden eşleştirme (0012 Req 6)**
23. Kullanıcı yeni bir ajan eşleştirip eski ajanın oturumu düştüğünde, eski ajanda o anda çalışan yerel aşama
    Req 15'teki ajan kopmasıyla aynı kurala göre ele alınır: iş *Ajan bekleniyor* olur ve eski ajana hiçbir komut
    gitmez. Yeni ajan bağlanınca (Req 14'teki araç sorgusu yeni ajana yapılır) iş kendiliğinden kaldığı aşamadan
    devam eder: yarım aşama baştan çalışır, tamamlanmış online aşamalar yeniden çalışmaz (online çağrı sayısı
    artmaz). Önceki yerel aşamaların çıktıları yeni ajanda yoksa (ör. başka bilgisayar) yalnızca o yerel aşamalar
    yeniden çalışır.

## User experience
- Flow: Kullanıcı 0006'da "Sırayla üret" der → en üstteki işin aşamalarının tek tek ilerlediğini ve iş
  satırındaki maliyetin arttığını görür → gerekirse "Durdur", sonra "Kaldığı yerden devam et" der → biten iş
  sıradan kalkar, video sağ sütundaki Üretilenler'e düşer → tıklayınca kesik çizginin sağındaki alanda izler.
- States:
  - Aşama durumları: bekliyor · çalışıyor · tamamlandı · atlandı · hata (metin/ikonla, yalnızca renk değil).
  - İş satırı: aşama göstergesi · toplam maliyet · Hata'da sade açıklama.
  - "Ajan bağlı değil" uyarısı (sayfa üstünde, Ajan bekleniyor işi varken).
  - "Art arda 3 iş hata verdi — sıra durduruldu" uyarısı.
  - "Ajan kurulumu eksik: <araç>" hatası.
  - "API key yok — Ayarlar" hatası (key'i olmayan sağlayıcı, Req 16).
  - Üretilenler: yükleniyor · boş ("Henüz üretilmiş video yok") · liste · sunucuya ulaşılamadı (korelasyon numarasıyla).
  - İzleme alanı: kapalı · oynatılıyor · ajan çevrimdışı ("Videoyu izlemek için ajan bağlı olmalı") · dosya bulunamadı.
- Design reference: `source/konseptten video.png`, commit `61cc824` sürümü: sağda "üretilenlerin gösterildiği
  yer" ve dikey kesik çizgi. Görseldeki "kesikli yerin solunda video izleme yeri açılır" notu ile ilk_promt'taki
  "sağ tarafında" çelişkisi: liste solda, izleme alanı sağda (görseldeki geniş boş alan) — onaylandı 2026-11-14.
- Accessibility: Durdur/Devam ve Üretilenler listesi klavyeyle (Tab, Enter/Space) kullanılabilir; oynatıcı
  denetimleri klavyeyle çalışır ve erişilebilir ada sahiptir; aşama durumu renk dışında metin/ikonla belirtilir;
  durum değişiklikleri ekran okuyucuya kibarca duyurulur; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- H-3: yeniden deneme yalnızca 429/5xx'te ve en fazla 2 kez; 4xx'te yok. Düzeltme döngüsü iş başına en fazla 1.
  Token limitleri katalogdan okunur, kodda sabit değer olmaz (`docs/conventions.md`). 15 dakikalık yatay
  videoya kadar her uzunluk desteklenmelidir (`docs/domain.md`, Temel değer) — sahne başına üretim bunun içindir.
- Teslim kısıtı: render aşamasında LLM'in yazdığı kod kullanıcı bilgisayarında yalnızca 0018'in kısıtlı
  ortamında çalıştırılır. 0017'nin testlerinde render aşaması stub'dır (LLM kodu çalıştırılmaz); 0017, 0018
  Shipped olmadan son kullanıcıya açılmaz (aynı sürümde yayınlanır). Altyazı ve ilerleme çubuğu birleştirme
  aşamasındaki sabit şablondan geldiği için kendi kriterleri stub sahne videosuyla kanıtlanır.
- Testlerde gerçek online çağrı yapılmaz (stub); gerçek ajan entegrasyonu full profilde çalışır (`docs/testing.md`).
- Ajandaki araçların kurulumu 0009'dadır; bu spec bunların kurulu olduğunu varsayar ve yoksa Req 14'teki
  hatayla durdurur.
- Yerel TTS Supertonic 3 (.NET içinde ONNX), Chatterbox sidecar'da; görsel Z-Image-Turbo yerel, FLUX.2 klein 4B
  ikinci (Karar 5). OpenAI TTS üzerine iş yapılmaz (2027-01-06 kapanış).
- Out of scope: kısıtlı render ortamı, render öncesi yasaklı yapı kontrolü, çıktı kapısı, video biçimi ve
  yerinin kanıtı (0018) · ayarlar, sıra yönetimi, iş durum listesi, çıkışta durdurma mekanizması (0006) ·
  Üretilen Videolar sayfası, klasörde aç, sistem oynatıcısı (0007) · yeni sürüm / kısmi yeniden üretim,
  yeniden üret (0020) · video silme ve Silinenler'in video sekmesi (0019) · Günlük geçmişi ve filtre (0021) ·
  ban (0024) · TL gösterimi (backlog) · Linux/macOS ajanı · Manim (faz 2) · aynı anda birden fazla iş · yayınlama.
- Split: done — 0017 Üretim hattı / kardeşler: 0006, 0018

## Acceptance criteria
**Ayarların videoya yansıması**
- [ ] AC-1 — Altyazı açık, ses modeli açıkken üretilen videoda seslendirme metni ekranda sesle zamanlı görünür (her sahnede metin, o sahnenin ses aralığında) ve o an okunan kelime farklı renkte; Altyazı kapalıyken aynı konseptten üretilen videoda hiçbir altyazı karesi yoktur. Altyazı için hiçbir LLM çağrısı yapılmaz.
- [ ] AC-2 — İlerleme çubuğu açıkken videonun üst kenarındaki çubuk ilk karede boş, son karede dolu ve videonun yarısında yaklaşık yarı doludur (±%5); kapalıyken çubuk hiç görünmez.
- [ ] AC-3 — Ses modeli "Kapalı — sessiz video" iken üretilen videoda ses izi sessizdir, altyazı yoktur ve seslendirme aşaması "atlandı" görünür.
- [ ] AC-4 — Görsel modeli "Kapalı — koddan çiz" iken görsel aşaması "atlandı" görünür, hiçbir görsel modeli (yerel ya da online) çağrılmaz ve video yine üretilir.
- [ ] AC-5 — Çalışan bir işin profil ayarı (ör. Altyazı) sonradan değiştirilirse o işin çıktısı işe sabitlenen ayarla üretilir; sıradaki bir sonraki iş yeni ayarla üretilir.

**Aşamalar ve sıra**
- [ ] AC-6 — Sırayla üret denince en üstteki iş başlar; iş bitince *Tamamlandı* olur, konsepti *Video oldu* olur, sıradan kalkar ve bir sonraki iş kendiliğinden başlar; aynı anda hiçbir zaman iki iş ya da iki yerel GPU aşaması çalışmaz.
- [ ] AC-7 — Çalışan işte aşamalar Req 6'daki sırayla ve durumlarıyla görünür; bir aşamanın durumu değiştikten sonra en geç 5 sn içinde sayfaya yansır; sayfa kapatılıp açıldığında üretim sürmüş ve güncel durum görünür.
- [ ] AC-8 — Analiz aşaması seçili video kodu modeliyle tam 1 online çağrı yapar; video kodu sahne başına üretilir (N sahneli konseptte N video kodu çağrısı, düzeltme hariç) ve sahneler birleştirme aşamasında tek videoya dönüşür.

**Durdur, devam, hata**
- [ ] AC-9 — Görsel aşamasında Durdur denince iş *Durduruldu* olur, sıradaki iş başlamaz; Devam et denince analiz ve video kodu aşamaları yeniden çalışmaz (online çağrı sayısı artmaz), görsel aşaması baştan, kalan aşamalar sırayla çalışır ve ardından sıra devam eder (BR-10).
- [ ] AC-10 — Bir online çağrı sürerken Durdur denince (ya da kullanıcı çıkış yapınca) o çağrı tamamlanır ve maliyeti kaydedilir, ama durdurmadan sonra yeni hiçbir online çağrı başlamaz (stub sağlayıcının çağrı sayısıyla kanıtlanır).
- [ ] AC-11 — Video kodu aşaması tamamlanmış *Durduruldu* bir iş × ile çıkarılıp konsept metni değişmeden yeniden sıraya alınınca analiz ve video kodu yeniden çalışmaz (online çağrı sayısı artmaz); konsept metni 0015'te değiştirildikten sonra yeniden sıraya alınınca tüm aşamalar baştan çalışır.
- [ ] AC-12 — Bir aşama hata verince iş *Hata* olur, satırda sade bir açıklama görünür (stack trace, SQL, API key yok); tamamlanmış aşamalar korunur; Devam et yalnızca hatalı aşamadan yeniden başlar. Sıra bir sonraki işe geçer; art arda 3 iş Hata olunca sıra durur, 4. iş başlamaz ve "Art arda 3 iş hata verdi — sıra durduruldu" görünür; aradaki başarılı bir iş sayacı sıfırlar.
- [ ] AC-13 — Render aşaması hata döndürünce video kodu modeline hata özetiyle tam 1 düzeltme çağrısı yapılır ve maliyeti kaydedilir; düzeltilen kod da başarısız olursa iş *Hata* olur ve ikinci bir düzeltme çağrısı yapılmaz (kasıtlı bozma: sınır kaldırılınca test kırmızı).

**Ajan**
- [ ] AC-14 — Ajan çevrimdışıyken online aşamalar tamamlanır, iş ilk yerel aşamada *Ajan bekleniyor* olur, hiçbir yerel aşama başlamaz, sayfada "Ajan bağlı değil" uyarısı görünür ve sıradaki bir sonraki iş başlamaz (online çağrısı yapılmaz); ajan bağlanınca iş kendiliğinden devam eder (BR-11).
- [ ] AC-15 — Ajan bir yerel aşamanın ortasında koparsa iş *Ajan bekleniyor* olur; bağlantı dönünce o aşama baştan çalışır, tamamlanmış aşamalar tekrarlanmaz.
- [ ] AC-16 — Ajan bağlıyken kurulumu eksikse (ör. render aracı yok) iş, hiçbir online çağrı yapılmadan "Ajan kurulumu eksik: <araç>" açıklamasıyla *Hata* olur ve harcama kaydı oluşmaz.
- [ ] AC-17 — Kullanıcının API key'i ajana hiçbir aşamada gönderilmez: ajana giden tüm komut ve dosyalar kaydedildiğinde hiçbirinde key (ya da maskesiz parçası) bulunmaz; ajan online sağlayıcıya hiç istek atmaz (Y-7, Y-8); iş yalnızca sahibinin eşleştirilmiş ajanına gönderilir, başka kullanıcının ajanına hiçbir komut gitmez.

**Maliyet ve katalog**
- [ ] AC-18 — Her online çağrı (analiz, video kodu, düzeltme, varsa online görsel/ses) için token (seste karakter) sayısı ve maliyet kaydı oluşur (BR-12); işin satırındaki toplam bu kayıtların toplamına eşittir; üst bardaki aylık harcama aynı tutar kadar artar. Sağlayıcı 4xx döndürünce yeniden deneme yapılmaz; 429/5xx'te en fazla 2 yeniden deneme yapılır (kasıtlı bozma: sınır kaldırılınca test kırmızı).
- [ ] AC-19 — Video kodu çağrısının çıktı token limiti seçili modelin katalog değerinden gelir; katalog değeri değiştirilince yapılan çağrının limiti de değişir (kodda sabit değer yok).
- [ ] AC-20 — ElevenLabs katalogda 0006'da tanımlı "ses" türünde online bir giriş olarak ve karakter başı fiyatla bulunur; ElevenLabs ses listesinde "online" etiketiyle görünür, key yokken "Key yok" etiketlidir; ElevenLabs ile 1.000 karakterlik seslendirmenin maliyet kaydı 1.000 × karakter başı fiyata eşittir.

**Günlük ve AI beyanı**
- [ ] AC-21 — İş başlayınca Günlük'e "iş başladı" olayı, bitince konsept başlığı, süre ("57 sn" / "30 dk 12 sn" biçiminde) ve başlangıç–bitiş saatini (Europe/Istanbul, HH:mm) içeren "video hazırlandı" olayı, hata verince aynı bilgilerle "hata" olayı yazılır ve Günlük şeridinde görünür.
- [ ] AC-22 — Supertonic 3 ile seslendirilen videonun yanında ajan klasöründe "Yapay zekâ ile üretilmiş ses" beyanını içeren not dosyası bulunur; video karelerinde bu yazı yoktur; başka ses modeliyle ya da sessiz üretilen videoda bu not yoktur.

**Üretilenler paneli ve yerel akış**
- [ ] AC-23 — Biten video, seçili profilde Üretilenler sütununda en üstte görünür (başka profilin videosu görünmez); tıklanınca kesik çizginin sağındaki izleme alanında oynar; boşken "Henüz üretilmiş video yok", ilk açılışta "Yükleniyor", sunucuya ulaşılamazsa sade hata ve korelasyon numarası görünür.
- [ ] AC-24 — Ajan çevrimdışıyken bir videoya tıklanınca izleme alanı "Videoyu izlemek için ajan bağlı olmalı" der; dosya ajan klasöründe yoksa "Video dosyası bulunamadı" der.
- [ ] AC-25 — Yerel video akışı yalnızca kısa ömürlü yetkiyle açılır (H-1, kasıtlı bozma ile): yetkisiz istek, süresi dolmuş yetki, başka kullanıcıya ait yetki, aynı kullanıcının başka bir videosuna ait yetki ve başka bir bilgisayardan gelen istek reddedilir; başka kullanıcının videosunu açma isteği reddedilir. Yetki süresinden uzun bir test videosu baştan sona kesilmeden oynar.
- [ ] AC-26 — Erişilebilirlik: Durdur/Devam, Üretilenler listesi ve oynatıcı denetimleri klavyeyle kullanılabilir ve erişilebilir ada sahiptir; aşama durumu renk dışında metin/ikonla da belirtilir.

**Ek kriterler (tutarlılık denetimi sonrası, 2026-11-14)**
- [ ] AC-27 — Sıradaki bir işin ihtiyaç duyduğu sağlayıcının API key'i yokken (0004'te silinmiş) iş o sağlayıcıya hiçbir çağrı yapılmadan, harcama kaydı oluşmadan "API key yok — Ayarlar" açıklamasıyla *Hata* olur (stub sağlayıcının çağrı sayısı 0); key yeniden girilip "Kaldığı yerden devam et" denince iş hatalı aşamadan sürer.
- [ ] AC-28 — Yerel video akışı açılırken, oynatılırken ve reddedilirken üretilen hiçbir log, hata mesajı, hata bildirimi ya da olay kaydı video akış token'ını düz metin içermez (kasıtlı bozma ile kanıtlanır; mekanizma 0008'de).
- [ ] AC-29 — Tamamlanan bir videonun sürüm kaydı konsept metninin üretim anındaki kopyasını, kullanılan modelleri ve video motorunu, altyazı/ilerleme çubuğu seçimini, AI ses beyanı bilgisini ve başlangıç–bitiş saatini içerir; konsept metni (0015) ya da profil ayarları sonradan değiştirilince kayıt değişmez.
- [ ] AC-30 — Bir iş eski ajanda render aşamasındayken kullanıcı yeni bir ajan eşleştirir (0012 Req 6): eski ajanın oturumu düşer, iş *Ajan bekleniyor* olur ve eski ajana bu andan sonra hiçbir komut gitmez; yeni ajan bağlanınca iş kendiliğinden render aşamasından devam eder, analiz ve video kodu yeniden çalışmaz (stub sağlayıcının online çağrı sayısı artmaz) ve iş *Tamamlandı* olur (Req 23, Req 15 ile aynı mekanizma).

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
