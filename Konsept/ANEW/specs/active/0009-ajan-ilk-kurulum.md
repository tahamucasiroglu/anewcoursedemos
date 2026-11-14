# Spec 0009 — ajan-ilk-kurulum

- Status: Approved
- Revision: R1 2026-11-14 — GPU hızlandırma (tahamucasiroglu: "gpu da çalışabilecek bir model gpu yerine cpu da çalışıyorsa … bu bir hatadır"); Req 2, 8, 8a, 17 ve AC-9, AC-24, AC-25 değişti; onay: tahamucasiroglu 2026-11-14 "onaylıyorum devam et"
- Revision: R2 2026-11-14 — tray menüsü sitenin temasında (tahamucasiroglu: "görev çubuğu kısmındaki görünümüde temaya uygun yapalım"); Req 21a ve AC-26 eklendi, Req 21a öne çekildi; onay: tahamucasiroglu 2026-11-14 "yap gitsin bitince bakarım"
- Mode: lite
- Plan: `specs/plans/0009-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0009-ajan-kurulum-guncelleme.md` (eski 0009a — İlk kurulum); bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: none — yok — kurulum, davranışı bizim koda bağlı bir ajan işidir (`docs/domain.md`: "Ajanın kendi işleri … H-4 kapsamı dışında"); yine de bileşen doğrulaması (AC-6) kasıtlı bozma ile kanıtlanır.
- Depends on: 0001 (ajan, tray, "Ajanı test et", sistem durum alanı), 0004 (Ayarlar sayfası temeli), 0008 (yerel teknik log, maskeleme), 0012 (cihaz eşleştirme), 0022 (ajan hata bildirimi akışı)

## Intent
İçerik üreticisi ANEW Konsept'in masaüstü ajanını siteden indirip bilgisayarına kurduğunda — format atılmış,
üzerinde hiçbir geliştirici aracı olmayan bir Windows PC'de bile — ajan, video üretimi için gereken yerel
araçları ve modelleri kendisi indirip kurar ve bunu profesyonel bir Avalonia ekranında "şu an bunları
kuruyoruz" diye adım adım gösterir. Her şey proje adıyla (**anewconcept**) ayrı bir yere, bilgisayardaki başka
programlarla (aynı aracın başka sürümü ya da "Konsept" adlı başka bir program) çakışmadan, yönetici izni
istemeden kurulur; doğrulamadan (hash) geçmeyen hiçbir dosya kurulmaz. Kurulumda bir hata olursa kullanıcı
hatayı görür, tekrar dener ya da bildirir; kurulum bitince ajan kendini sınar, "Hazır" der ve eşleştirmeye ya
da tray'e geçer. Başarı: temiz bir Windows makinesinde kurulum tek akışta biter ve ajan render yapmaya hazırdır.
Bilinçli olarak yapılmayanlar: güncelleme ve kaldırma (0023), video üretim aşamalarının kendisi (0017), render
sandbox'ı ve opsiyonel "güçlü mod" (sahibi 0018, H-4; seçeneği bu spec'in kurulum ekranına 0018 kendi AC'siyle ekler), cihaz eşleştirmenin kendisi (0012, H-1), hata bildiriminin
içeriği/gönderimi/bekletilmesi (0022), Günlük şeridi (0021) ve admin hata ekranı (0025), Linux/macOS ajanı,
Ollama (E-4: v1'de hiçbir rolde seçilemez, kurulmaz).

## Requirements
1. **Ajanı indir.** Sitede giriş yapmış kullanıcı için "Ajanı indir" bağlantısı iki yerde bulunur: Ayarlar
   sayfasında (temeli 0004) ve sistem durum alanı "Ajan bağlı değil" gösterirken (0001). Bağlantı Pi'den
   sunulan tek kurulum dosyasını (ürünün güncel sürümü) indirir (ilk_promt.txt: "minik bir tane de uygulamayı
   indirecek, bilgisayarına kuracak"). Windows kod imzalama sertifikası v1'de yoktur; SmartScreen uyarısı kabul
   edilmiştir.
2. **Bileşen listesi (Karar 5, E-4).** Ajan açıldığında gerekli bileşenler kurulu değilse (ilk kurulum)
   kurulum ekranı açılır. Ekran her bileşeni adı, ne işe yaradığı (ör. "Video render motoru", "Seslendirme
   modeli"), lisans adı, indirme boyutu ve durumuyla listeler.
   - **Zorunlu çekirdek** (işaretli, kaldırılamaz): Node.js 26 LTS (sabit sürüm); FFmpeg (LGPL build);
     headless Chromium (chrome-headless-shell, sabit sürüm); HyperFrames (tam sürüm sabit — bugün 0.9.43 —
     telemetrisi kurulumda kapatılır); Supertonic 3 ONNX ağırlıkları (yerel Türkçe TTS; ajanın içinde çalışır,
     Python gerektirmez; ekran kartı destekliyorsa GPU'da, Req 8a); **GPU çalışma zamanı** — yalnızca NVIDIA
     kartta: NVIDIA'nın resmi yeniden dağıtılabilir CUDA 12 çalışma zamanı ve cuDNN 9 dosyaları (Supertonic'in
     ONNX Runtime CUDA sağlayıcısı ve `sd-server` CUDA paketi bunları kullanır); diğer kartlarda indirme
     gerekmez (ONNX Runtime DirectML ajanla gelir).
   - **İsteğe bağlı** (varsayılan işaretsiz; kurulum ekranında ya da sonradan "Kurulum ve bileşenler"den
     eklenir): **Ses klonlama** — uv + izole Python ortamı + Chatterbox Multilingual V3 (ayrı yardımcı süreç; NVIDIA kartta PyTorch'un CUDA derlemesi kurulur, varsayılan CPU derlemesi değil — Req 8);
     **Yerel görsel** — stable-diffusion.cpp `sd-server` (ekran kartına uygun paket, Req 8; CUDA paketinin `cudart` eki dahil) + Z-Image-Turbo GGUF;
     **İkinci yerel görsel** — FLUX.2 klein 4B (yerel görsel paketini gerektirir); **Remotion** — varsayılan
     kapalı, satırda lisans notu (K-2).
   - Kurulmayanlar: Ollama (E-4), OpenAI TTS'e özel hiçbir bileşen. Video motoru/TTS/görsel motorunun üretimde
     kullanımı 0017/0018'in işidir; bu spec yalnızca bu bileşenlerin kurulmasını kapsar. 0017 altyazı için ek
     bir model ya da font gerektirirse çekirdek liste aynı mekanizmayla genişletilir.
3. **Temiz makine.** Kurulum, üzerinde yalnızca Windows bulunan bir bilgisayarda da tamamlanır: ajan her
   bileşeni kendisi indirir; kullanıcıdan önceden bir şey kurması istenmez (ilk_promt.txt: "format attı ve
   sonra bizim programı kurdu… bunu bile destekleyecek").
4. **Lisans onayı.** Kurulum başlamadan önce ekranda kısa lisans bilgisi bulunur: her satırda lisans adı;
   Supertonic için OpenRAIL-M kullanım kısıtlarının bir cümlelik özeti; Chatterbox seçiliyse ürettiği seslere
   filigran eklediği bilgisi. Tek bir "Okudum ve kabul ediyorum" onay kutusu işaretlenmeden **Kurulumu başlat**
   pasiftir.
5. **İlerleme ekranı.** Kurulum sırasında ekran o an işlenen bileşeni "Şu an <bileşen> kuruluyor" diye ve bir
   ilerleme göstergesiyle gösterir; yapılan işler sade Türkçe satırlar halinde ekranda akar (ilk_promt.txt: "şu
   an bunları kuruyoruz gibisinden bir log var" — sesten yazıda "login" olarak geçmiş). Her bileşenin durumu
   şu sırayla ilerler: Bekliyor → İndiriliyor → Doğrulanıyor → Kuruluyor → Kuruldu; ya da Hata / Atlandı
   (isteğe bağlı bileşen seçilmediyse). Toplam kurulum süresi için hedef yoktur (indirme hızına bağlı).
6. **Kurulum yeri ve adlar (çakışmasızlık).** Tüm bileşenler kullanıcı başına `%LOCALAPPDATA%\anewconcept`
   altına, Windows yönetici izni (UAC) istenmeden kurulur. Görünen ad "ANEW Konsept"; tüm teknik adlar
   (klasör, kısayol, başlangıç kaydı, kaldırma kaydı) `anewconcept`. Ajan, bilgisayarda zaten bulunan aynı
   araçların başka sürümlerini, "Konsept" adlı başka programları ve kullanıcının sistem ayarlarını (ör. sistem
   ya da kullanıcı genelindeki araç yolu ayarı) değiştirmez ve silmez (ilk_promt.txt: "projenin ismiyle
   beraber kurulum yap. Çakışma olmasın. Konsept adı altında başka bir program olabilir").
7. **Mevcut araçlar kullanılmaz.** Bilgisayarda Node/Python/FFmpeg/Chromium zaten kuruluysa ajan onları
   kullanmaz; her zaman kendi sabit sürümlerini anewconcept kurulum yerine kurar ve yalnızca onları çalıştırır
   (H-4 katmanları belirli sürümlere bağlıdır; kullanıcının güncellemesi ajanı bozmaz).
8. **Ekran kartı tespiti ve GPU paketleri.** Ajan ekran kartını, VRAM'i ve (NVIDIA'da) sürücü sürümünü tespit
   edip kurulum ekranında gösterir ("Ekran kartı: <ad>, <N> GB"). GPU'da çalışabilen her bileşen ekran kartına
   uygun paketle kurulur:

   | Bileşen | NVIDIA | Diğer kart (AMD/Intel) | Uygun kart yok |
   |---|---|---|---|
   | Supertonic 3 (ONNX) | ONNX Runtime CUDA + CUDA 12/cuDNN 9 | ONNX Runtime DirectML | CPU |
   | Yerel görsel `sd-server` (Z-Image-Turbo, FLUX.2 klein) | `win-cuda12` + `cudart` | `win-vulkan` | `win-cpu` |
   | Chatterbox (PyTorch) | PyTorch CUDA derlemesi (kartın mimarisine uygun; ör. RTX 30 → CUDA 12.x) | CPU derlemesi (*) | CPU derlemesi |
   | Video kodlama (FFmpeg) | NVENC | AMF / QSV | yazılım kodlayıcı |

   (*) AMD/Intel'de Windows için kararlı bir PyTorch GPU derlemesi yok; satırda "Bu ekran kartında Ses klonlama
   CPU'da çalışır; yavaş olabilir" görünür. Ekran kartı bunu desteklemediği için bu durum hata sayılmaz.
   NVIDIA sürücüsü CUDA 12 için gereken en düşük sürümün altındaysa ekranda "NVIDIA sürücünüz eski; ekran kartı
   kullanılamaz — sürücüyü güncelleyin" görünür ve GPU paketli bileşenlerin kurulumu başlamaz (sürücü
   güncellemesi yönetici izni ister, ajan bunu yapmaz — Req 6). VRAM 6 GB'ın altındaysa Yerel görsel ve Ses
   klonlama seçeneklerinin yanında "Ekran kartı belleği düşük; yavaş çalışabilir" uyarısı görünür (seçim
   engellenmez).
8a. **GPU varken CPU'ya düşmek hatadır.** Ekran kartı bir bileşeni destekliyorsa (Req 8 tablosu) o bileşen
   GPU'da çalışır; CPU'ya sessizce düşmek yasaktır. Ajan her bileşenin hangi aygıtta çalıştığını (GPU adı ya da
   CPU) "Kurulum ve bileşenler" ekranında satırında gösterir ve yerel loga yazar. GPU desteklenirken bileşen
   GPU'yu kullanamıyorsa (sağlayıcı yüklenemedi, CUDA hatası, yanlış paket) bileşen "Hata — Ekran kartı
   kullanılamıyor" olur; Tekrar dene / Hatayı bildir geçerlidir.
9. **Video klasörü.** Videoların duracağı klasör kurulum yerinden ayrıdır ve ilk kurulum ekranında
   değiştirilebilir; varsayılan: kullanıcının Videolar klasörü altında `anewconcept`.
10. **Ön kontrol.** **Kurulumu başlat** etkin olmadan önce internet bağlantısı ve disk alanı denetlenir. Gereken
    alan = seçili bileşenlerin indirme boyutu + açılmış boyutu toplamı × 1,2 (%20 pay); ekranda "Gereken: X GB,
    Boş: Y GB" gösterilir. İnternet yoksa ya da boş alan gerekenden azsa Başlat pasiftir ve sebep metni görünür;
    isteğe bağlı seçim değişince değerler yeniden hesaplanır.
11. **Doğrulama.** Üçüncü taraf bileşenler resmi kaynaklarından indirilir; indirilen her dosya kurulmadan önce
    ajana gömülü sabit SHA-256 listesiyle doğrulanır. Doğrulamadan geçmeyen bileşen kurulmaz ve "Hata — Dosya
    doğrulanamadı" durumuna düşer. Arşivlenme riski olan Supertonic ağırlıkları Pi'de aynalanır.
12. **Yeniden deneme.** Bir indirme başarısız olursa ajan o bileşen için en fazla 2 otomatik yeniden deneme
    yapar; resmi kaynak erişilemezse ve Pi'de aynalanmış kopya varsa ondan dener; hâlâ başarısızsa bileşen Hata
    durumuna düşer.
13. **Hata gösterimi.** Bir bileşen kurulamazsa satırı kırmızı ve "Hata" yazısıyla, sade Türkçe bir sebeple
    (ör. "İnternet bağlantısı koptu", "Diskte yeterli yer yok", "Dosya doğrulanamadı") gösterilir; teknik
    ayrıntı (stack trace) gösterilmez. Kullanıcı **Tekrar dene** ile yalnızca hatalı bileşeni yeniden dener;
    tamamlanmış bileşenler korunur.
14. **Hatayı bildir.** Hata durumundaki bileşen satırında **Hatayı bildir** bulunur (ilk_promt.txt: "kurulumlarda
    falan hata olduğu zaman… hata oldu bildir demeli. Bildirilen hatalar da API'ye gitmeli"). Seçilince 0022'nin
    ajan hata bildirimi akışı başlar ve bildirime hangi bileşenin hangi adımda (indirme / doğrulama / kurulum)
    hata verdiği eklenir. Kurulum ekranının eklediği bilgide Windows kullanıcı adı içeren dosya yolları 0022'nin
    biçimiyle `%USERPROFILE%\…` olarak kısaltılır ve makine adı yer almaz. Bildirimin içeriği, maskelenmesi, ajan eşleştirilmemişken
    bekletilmesi ve gönderimi 0022'de (maskeleme mekanizması 0008'de) tanımlıdır ve orada kanıtlanır.
15. **Kesinti.** Kurulum yarıda kesilirse (bilgisayar kapandı, ajan çöktü) bir sonraki açılışta Kuruldu olan
    bileşenler korunur, yarım kalan bileşen baştan indirilir ve ekran kaldığı yerden devam eder.
16. **Pencereyi kapatma.** Kurulum sürerken pencere kapatılırsa "Kurulum sürüyor, arka planda devam etsin mi?"
    onayı çıkar. **Evet** → pencere gizlenir, kurulum sürer, tray simgesinin ipucu ilerlemeyi gösterir
    ("Kuruluyor: %NN"). **Hayır** → pencere açık kalır. Kurulum sürerken tray'den "Çıkış" kurulumu güvenli
    durdurur: yarım bileşenin dosyaları silinir, tamamlananlar korunur; sonraki açılışta Req 15 uygulanır.
17. **Hazır denetimi.** Tüm seçili bileşenler Kuruldu olunca ajan her çekirdek araçla küçük bir örnek komut
    çalıştırır (sürüm sorgusu ve kısa bir deneme); GPU destekli her bileşen için denemenin gerçekten GPU'da
    çalıştığını doğrular (Supertonic: etkin ONNX sağlayıcısı CUDA/DirectML; Chatterbox: PyTorch GPU'yu görüyor;
    `sd-server`: CUDA/Vulkan arka ucu; FFmpeg: donanım kodlayıcıyla kısa bir kodlama). Sonuç olumluysa "Hazır" gösterir; olumsuzsa "Ajan kurulumu
    eksik: <araç>" sebebiyle ilgili bileşen Hata durumuna düşer (Tekrar dene / Hatayı bildir geçerlidir).
18. **Tamamlanma.** "Kurulum tamamlandı · Hazır" görünür. Ajan henüz eşleştirilmemişse cihaz eşleştirme adımına
    geçer (0012); eşleştirilmişse kurulum ekranı kapanır ve ajan tray'de çalışmaya devam eder.
19. **Yeniden açılış.** Kurulmuş ve doğrulanmış bir bileşen, ajan yeniden açıldığında tekrar indirilmez. Tüm
    bileşenler kuruluysa ajan kurulum ekranını göstermeden tray'e iner.
20. **Bütünlük denetimi.** Her açılışta ajan yalnızca kurulu bileşenlerin yürütülebilir dosyalarında hızlı hash
    denetimi yapar. Uyuşmazlık ya da eksik dosya varsa o bileşen "Onarılmalı" olur, kurulum ekranı açılır ve
    **Onar** tek tıkla yalnızca o bileşeni yeniden indirip kurar.
21. **Tray ekleri.** Tray davranışı 0001'deki gibidir ("Siteye git", "Çıkış", pencereyi kapatmak uygulamayı
    kapatmaz — 0001 AC-5). Bu spec menüye **"Kurulum ve bileşenler"** öğesini ekler: kurulum ekranını açar;
    kurulu bileşenleri durumuyla gösterir ve kurulmamış isteğe bağlı bileşenlerin sonradan eklenmesini sağlar
    (ön kontrol, lisans, doğrulama ve ilerleme kuralları aynen geçerlidir). 0012'deki oturum durumu satırı
    menünün en başında kalır; yeni öğe onu ve 0001'in öğelerini değiştirmez.
21a. **Tray menüsünün görünümü (R2).** Tray simgesine tıklanınca (sol ya da sağ tık) açılan menü, Windows'un
    düz yerel menüsü yerine ajanın kendi çizdiği küçük bir paneldir: sitenin turuncu/kahve paleti, açık/koyu
    tema Windows uygulama temasını izler (Req 23), üstte ikon + "ANEW Konsept" başlığı ve bağlantı durumu
    satırı ("Bağlı" / "Bağlı değil"), altında menü öğeleri (0001: "Siteye git", "Çıkış"; bu spec ve 0012'nin
    eklediği öğeler aynı panelde). Panel simgenin yanında (ekran kenarına taşmadan) açılır; dışarı
    tıklanınca, Esc'ye basınca ya da bir öğe seçilince kapanır; klavyeyle (ok tuşları, Enter) kullanılabilir.
    Bu madde 0009'un geri kalanından önce, bağımsız olarak uygulanabilir (öne çekildi, 2026-11-14).
22. **Log.** Kurulum olayları (başladı, bileşen kuruldu, otomatik yeniden deneme, hata, doğrulama başarısız,
    Onarılmalı, Hazır) ajanın yerel teknik loguna Information/Warning/Error seviyesinde yazılır; loglarda şifre,
    token ya da API key bulunmaz. Ajan logları kurulum yerinin altındaki `logs` klasörüne yazılır
    (`%LOCALAPPDATA%\anewconcept\logs`, 0008 Req 5). Log biçimi, saklama süresi (1 hafta) ve maskeleme 0008'de
    tanımlıdır.
23. **Dil ve görünüm.** Kurulum ekranındaki tüm metinler Türkçedir ve kaynak dosyalarından gelir
    (`docs/architecture.md`). Ekran sitenin turuncu/kahve paletini kullanır; açık/koyu tema Windows uygulama
    temasını izler; pencere ve tray ikonu `source/` içindeki ikondur (yoksa 0001'deki yer tutucu).

## User experience
- Flow (indirme ve ilk kurulum): Kullanıcı sitede Ayarlar'da ya da "Ajan bağlı değil" durumunda **Ajanı indir**'e
  tıklar → tek kurulum dosyasını çalıştırır (SmartScreen uyarısı çıkabilir) → kurulum ekranı açılır: ekran
  kartı/VRAM, video klasörü, çekirdek bileşenler (işaretli), isteğe bağlı bileşenler (işaretsiz), "Gereken: X GB,
  Boş: Y GB", lisans bilgisi ve "Okudum ve kabul ediyorum" kutusu → **Kurulumu başlat** → bileşenler sırayla
  "Şu an … kuruluyor" diye işlenir, yapılan işler ekranda satır satır akar → hepsi Kuruldu → Hazır denetimi →
  "Kurulum tamamlandı · Hazır" → eşleştirme adımı (0012) ya da doğrudan tray. Hata olursa: hatalı satır kırmızı +
  "Hata" yazısı, sebep metni, **Tekrar dene** / **Hatayı bildir** (0022 akışı).
- Flow (sonradan bileşen ekleme / onarma): tray → "Kurulum ve bileşenler" → isteğe bağlı bileşeni işaretle →
  **Kurulumu başlat**; ya da açılışta "Onarılmalı" satırında **Onar**.
- States:
  - Kurulum ekranı — hazır (bileşen listesi; onay kutusu işaretliyse Başlat etkin) · lisans onayı bekleniyor
    (Başlat pasif) · ön kontrol başarısız: internet yok / disk yetersiz (Başlat pasif, sebep metni) · kuruluyor
    (aktif bileşen + ilerleme + akan satırlar) · arka planda kuruluyor (pencere gizli, tray ipucu) · bileşen
    hatası · Hazır denetimi sürüyor · tümü tamamlandı / Hazır · onarım gerekli.
  - Bileşen satırı — Bekliyor · İndiriliyor · Doğrulanıyor · Kuruluyor · Kuruldu · Atlandı · Hata · Onarılmalı.
  - Pencere kapatma onayı — Evet / Hayır.
  - Zaten kurulu ve sağlam — ekran açılmaz, ajan doğrudan tray'e iner.
  - Hata bildirimi penceresinin durumları 0022'ye, eşleştirme ve ajan oturum durumları 0012'ye, güncelleme
    ekranı durumları 0023'e aittir.
- Design reference: none — the flow above is the design. (`source/*.png` görselleri, commit `61cc824`, yalnızca
  web sayfalarını çizer; ajan kurulum ekranı için tasarım yok.) Palet/tema Req 23'tedir.
- Accessibility: tüm düğmeler ve onay kutuları (Başlat, Tekrar dene, Hatayı bildir, Onar, Okudum, isteğe bağlı
  seçimler, Kapat, Evet/Hayır) klavyeyle erişilebilir (Tab, Enter/Space); her bileşen satırının durumu ekran
  okuyucuya metin olarak verilir (yalnızca renkle değil: Hata kırmızı + "Hata" yazısı); ilerleme göstergesinin
  erişilebilir adı ve değeri var; metin/arka plan kontrastı WCAG AA (açık ve koyu temada).

## Constraints & out of scope
- Sensitive area: none — yok. Bu spec LLM'in ürettiği kodu çalıştırmaz; kurulum, bizim yazdığımız ve kullanıcı emriyle
  çalışan ajan işidir ve `docs/domain.md` gereği H-4 kapsamı dışındadır. Bileşen doğrulaması (AC-6) yine de
  kasıtlı bozma ile kanıtlanır. Ajanın web oturumunu takip etmesi (H-1) 0012'dedir.
- Platform: yalnızca Windows 11 (öncelikli ajan; Linux/macOS kapsam dışı, `docs/architecture.md`).
- Performans: tüm bileşenler kurulu ve sağlamken açılıştan tray'e en fazla 10 sn (bütünlük denetimi dahil;
  0001'deki 10 sn ölçüsüyle aynı). Toplam kurulum süresi için hedef yok.
- Kanıt yöntemi: birim/headless testlerde sahte indirici ve sahte dosya sistemi; temiz makine kanıtı (AC-2,
  AC-4, AC-13) Windows Sandbox ya da temiz VM'de agent'ın aldığı ekran görüntüsü + ortam değişkeni/klasör
  karşılaştırması; insan SHIP'te değerlendirir (`docs/testing.md`).
- Bileşen seçimleri Karar 5'e (`raporlar/b5-video-pipeline-arastirmasi`) dayanır: HyperFrames sürümü sabit; Remotion opsiyonel ve varsayılan kapalı;
  Revideo ve Motion Canvas kurulmaz; yerel TTS Supertonic 3, ikinci yerel TTS Chatterbox (yardımcı süreç); yerel
  görsel Z-Image-Turbo, ikinci FLUX.2 klein 4B. Altyazı/ilerleme çubuğu ajanın sabit şablonundan gelir (0017).
- Out of scope: güncelleme, güncelleme imzası, sürüm uyumsuzluğu, geri dönüş, "Güncellemeleri denetle" ve
  kaldırma (0023); render, seslendirme ve görsel üretim aşamaları (0017); H-4 render sandbox'ı ve opsiyonel
  yönetici izinli "güçlü mod" güvenlik duvarı kuralı (sahibi 0018 — güçlü mod seçeneğini "Kurulum ve bileşenler"
  ekranına kendi AC'siyle 0018 ekler; bu spec güçlü modu sormaz ve kurmaz); cihaz eşleştirme ekranı, oturum iptali, ajanın "oturum kapandı / yeniden eşleştir" göstermesi ve tray
  menüsündeki oturum durumu satırı (0012); hata bildiriminin içeriği, maskelemesi, bekletilmesi ve gönderimi
  (0022); tray menüsünün temel davranışı (0001); web Günlük şeridi (0021), log saklama (0008), admin hata listesi
  (0025); Ollama ve yerel LLM (E-4); ajan sürümlerinin Pi'ye yayınlanma süreci (deploy betikleri); kod imzalama
  sertifikası satın alma.
- Split: done — 0009 Ajan ilk kurulum + "Ajanı indir" bağlantısı / kardeşler: 0023

## Acceptance criteria
- [ ] AC-1 — Giriş yapmış kullanıcı Ayarlar sayfasında ve sistem durum alanı "Ajan bağlı değil" gösterirken "Ajanı indir" bağlantısını görür; bağlantı Pi'den ürünün güncel sürümündeki tek kurulum dosyasını indirir (HTTP 200, dosya adı ve sürüm ürün versiyonuyla eşleşir); ajan bağlıyken durum alanında bağlantı görünmez, Ayarlar'da görünür.
- [ ] AC-2 — Bileşenleri kurulu olmayan bir bilgisayarda ajan ilk açılışta kurulum ekranını açar; Req 2'deki çekirdek bileşenler işaretli ve kaldırılamaz, isteğe bağlı bileşenler (Ses klonlama, Yerel görsel, İkinci yerel görsel, Remotion) işaretsiz listelenir; her satırda ad, amaç, lisans adı, indirme boyutu ve "Bekliyor" durumu vardır; listede Ollama yoktur.
- [ ] AC-3 — "Okudum ve kabul ediyorum" kutusu işaretli değilken **Kurulumu başlat** pasiftir; Supertonic OpenRAIL-M özeti görünür; Ses klonlama işaretlenince Chatterbox filigran bilgisi görünür; kutu işaretlenince (ön kontrol geçerliyse) Başlat etkinleşir.
- [ ] AC-4 — Üzerinde yalnızca Windows 11 bulunan temiz bir makinede (Node, Python, FFmpeg, Chromium yok) kurulum kullanıcı başka bir şey kurmadan tamamlanır; sonunda her çekirdek araç `%LOCALAPPDATA%\anewconcept` altından çalıştırıldığında sabit sürümünü verir ve HyperFrames telemetrisi kapalıdır.
- [ ] AC-5 — Kurulum sırasında ekran işlenen bileşeni "Şu an <bileşen> kuruluyor" metni ve ilerleme göstergesiyle gösterir ve yapılan işler Türkçe satırlar halinde akar; her bileşen Bekliyor → İndiriliyor → Doğrulanıyor → Kuruluyor → Kuruldu sırasını izler; seçilmeyen isteğe bağlı bileşen "Atlandı" olur.
- [ ] AC-6 — Beklenen SHA-256 ile uyuşmayan (kasıtlı bozulmuş) bir bileşen dosyası kurulmaz; satır "Hata — Dosya doğrulanamadı" gösterir; bozma kaldırılınca **Tekrar dene** bileşeni kurar.
- [ ] AC-7 — Kurulumdan önce makinede farklı sürümde Node ve Python (araç yolunda) ile "Konsept" adlı bir program/klasör varken, kurulumdan sonra bunların dosyaları, sürümleri ve kullanıcı/sistem ortam ayarları (araç yolu dahil) değişmemiştir; ajanın bileşenleri yalnızca `%LOCALAPPDATA%\anewconcept` altında bulunur ve ajan çalışırken araç yolundaki Node/Python'u değil kendi kopyalarını çalıştırır.
- [ ] AC-8 — Kurulum hiçbir adımda Windows yönetici izni (UAC) istemeden tamamlanır; oluşan klasör, kısayol, başlangıç kaydı ve kaldırma kaydının teknik adı `anewconcept`, görünen adı "ANEW Konsept"tir.
- [ ] AC-9 — Kurulum ekranı tespit edilen ekran kartı adını ve VRAM'i gösterir; Req 8 tablosundaki her bileşen için NVIDIA'da CUDA/NVENC, diğer kartta DirectML/Vulkan/AMF-QSV, uygun kart yoksa CPU paketi seçilip indirilir (sahte donanım bilgisiyle üç durum her bileşen için test edilir; NVIDIA'da Chatterbox için CUDA PyTorch derlemesi istenir, CPU derlemesi istenmez; CUDA çalışma zamanı ve cuDNN yalnızca NVIDIA'da indirilir); VRAM 6 GB'ın altındayken Yerel görsel ve Ses klonlama yanında "Ekran kartı belleği düşük; yavaş çalışabilir" uyarısı görünür ve seçim engellenmez.
- [ ] AC-10 — Video klasörü ilk kurulum ekranında varsayılan olarak Videolar\anewconcept gösterilir, değiştirilebilir ve seçilen klasör kurulumdan sonra ajan tarafından kullanılır; kurulum yeriyle aynı klasör olamaz.
- [ ] AC-11 — İnternet yokken ya da boş alan (seçili bileşenlerin indirme + açılmış boyutu × 1,2) değerinden azken **Kurulumu başlat** pasiftir ve sebep metni ile "Gereken: X GB, Boş: Y GB" görünür; isteğe bağlı seçim değişince değerler güncellenir; kurulum sürerken internet koparsa işlenen bileşen (otomatik denemelerden sonra) "Hata — İnternet bağlantısı koptu" olur, tamamlanmış bileşenler Kuruldu kalır.
- [ ] AC-12 — Başarısız bir indirme için ajan en fazla 2 otomatik yeniden deneme yapar (her deneme akan satırlarda ve logda görünür); resmi kaynak erişilemezken Pi'de aynası olan bileşen (Supertonic) aynadan kurulur; aynası olmayan bileşen 3. başarısızlıktan sonra Hata olur.
- [ ] AC-13 — Bir bileşen hata verdiğinde satır kırmızı ve "Hata" yazısıyla, sade Türkçe sebeple görünür (stack trace yok); **Tekrar dene** yalnızca o bileşeni yeniden dener; daha önce Kuruldu olan bileşenler yeniden indirilmez.
- [ ] AC-14 — Hata durumundaki bir bileşen satırında **Hatayı bildir** bulunur; seçilince 0022'nin ajan hata bildirimi akışı açılır ve bildirim özeti hatalı bileşenin adını ve hatanın adımını (indirme / doğrulama / kurulum) içerir; eklenen bilgide `C:\Users\<ad>\…` yolu `%USERPROFILE%\…` olarak görünür (0022 biçimi) ve makine adı geçmez; Kuruldu ya da Bekliyor satırlarında bu seçenek yoktur. (Gönderim, bekletme ve genel maskeleme 0022/0008'de kanıtlanır.)
- [ ] AC-15 — Kurulum ortasında ajan süreci zorla sonlandırılıp yeniden açıldığında Kuruldu olan bileşenler yeniden indirilmez, yarım kalan bileşen baştan indirilir ve ekran kaldığı yerden devam eder.
- [ ] AC-16 — Kurulum sürerken pencere kapatılınca "Kurulum sürüyor, arka planda devam etsin mi?" onayı çıkar; **Evet** → pencere gizlenir, kurulum sürer ve tray ipucu "Kuruluyor: %NN" gösterir; **Hayır** → pencere açık kalır; kurulum sürerken tray'den "Çıkış" seçilince yarım bileşenin dosyaları silinir, tamamlananlar korunur ve sonraki açılışta kurulum kaldığı yerden sürer.
- [ ] AC-17 — Tüm seçili bileşenler Kuruldu olunca Hazır denetimi çalışır ve "Kurulum tamamlandı · Hazır" görünür; bir çekirdek aracın yürütülebilir dosyası denetimden önce kasıtlı bozulduğunda "Ajan kurulumu eksik: <araç>" sebebiyle o bileşen Hata olur.
- [ ] AC-18 — Kurulum tamamlanınca ajan eşleştirilmemişse eşleştirme adımı (0012) açılır, eşleştirilmişse ekran kapanır ve ajan tray'de kalır.
- [ ] AC-19 — Tüm bileşenleri kurulu ve sağlam bir makinede ajan yeniden açıldığında hiçbir bileşen yeniden indirilmez, kurulum ekranı açılmaz ve ajan 10 sn içinde tray'e iner.
- [ ] AC-20 — Kurulu bir bileşenin yürütülebilir dosyası değiştirilip ya da silinip ajan açıldığında o bileşen "Onarılmalı" olarak kurulum ekranında görünür; **Onar** yalnızca o bileşeni yeniden indirip kurar, diğerleri yeniden indirilmez.
- [ ] AC-21 — Tray menüsünde "Kurulum ve bileşenler" bulunur; açılan ekran kurulu bileşenleri durumuyla gösterir; kurulmamış bir isteğe bağlı bileşen işaretlenip başlatılınca yalnızca o bileşen (ön kontrol, lisans onayı ve doğrulamayla) kurulur; 0001'deki "Siteye git" ve "Çıkış" davranışı değişmez; 0012'deki tıklanamayan oturum durumu satırı menünün en başında kalır ve durumu doğru göstermeye devam eder (0012 AC-15 bu öğe eklenmişken de geçer).
- [ ] AC-22 — Kurulum başlangıcı, her bileşenin kuruluşu, her otomatik yeniden deneme, her hata, her doğrulama başarısızlığı, Onarılmalı ve Hazır olayları yerel teknik loga uygun seviyede (Information/Warning/Error) yazılır; log dosyalarında şifre, token ya da API key geçmez.
- [ ] AC-23 — Kurulum ekranındaki tüm kontroller klavyeyle kullanılabilir ve erişilebilir ada sahiptir; bileşen durumları metin olarak okunur (yalnızca renk değil); kontrast açık ve koyu temada WCAG AA; tema Windows uygulama temasını izler; palet sitenin turuncu/kahve paletidir; tüm metinler Türkçe ve kaynak dosyasından gelir.
- [ ] AC-26 — (R2) Tray simgesine tıklanınca sitenin paletinde, Windows temasını (açık/koyu) izleyen özel bir menü paneli açılır: başlık, bağlantı durumu, "Siteye git" ve "Çıkış"; "Siteye git" siteyi açar, "Çıkış" ajanı kapatır (0001 AC-5 davranışı korunur); panel dışarı tıklanınca ya da Esc ile kapanır; öğeler klavyeyle seçilebilir; panel ekran kenarından taşmaz.
- [ ] AC-24 — GPU varken CPU'ya düşme yok (Req 8a, kasıtlı bozma ile): NVIDIA kartlı makinede Hazır denetimi Supertonic, Chatterbox (seçiliyse), `sd-server` (seçiliyse) ve FFmpeg kodlamasının GPU'da çalıştığını doğrular ve "Kurulum ve bileşenler" ekranı her satırda aygıtı (GPU adı) gösterir; GPU dosyaları kasıtlı silinince (ör. cuDNN) ya da Chatterbox'a CPU derlemesi kurulunca bileşen CPU'da sessizce çalışmaz, "Hata — Ekran kartı kullanılamıyor" olur ve olay loga Error seviyesinde yazılır. Geliştirici makinesinde (RTX 3060 Laptop, 6 GB) gerçek donanımla da gözlenir.
- [ ] AC-25 — NVIDIA sürücüsü CUDA 12 için gereken en düşük sürümün altındayken (sahte sürücü sürümüyle) ekran "NVIDIA sürücünüz eski; ekran kartı kullanılamaz — sürücüyü güncelleyin" gösterir ve GPU paketli bileşenlerin kurulumu başlamaz; AMD/Intel kartta Ses klonlama satırı "Bu ekran kartında Ses klonlama CPU'da çalışır; yavaş olabilir" gösterir ve bu durum hata sayılmaz.

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
