# Spec 0009 — ajan-kurulum-guncelleme

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0009-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisi ANEW Konsept'in masaüstü ajanını bilgisayarına kurduğunda — format atılmış, üzerinde
hiçbir geliştirici aracı olmayan bir Windows PC'de bile — ajan, video üretimi için gereken yerel araçları
ve modelleri kendisi indirip kurar ve bunu profesyonel bir Avalonia ekranında "şu an bunları kuruyoruz"
diye adım adım gösterir. Her şey proje adıyla (**anewconcept**) ayrı bir yere, bilgisayardaki başka
programlarla (aynı aracın başka sürümü ya da "Konsept" adlı başka bir program) çakışmadan kurulur.
Güncellemelerde aynı ekran kullanılır; doğrulamadan (imza/hash) geçmeyen hiçbir paket kurulmaz. Kurulumda
bir hata olursa kullanıcı hatayı görür, tekrar dener ya da bildirir. Kurulum bitince ajan eşleştirmeye ya
da tray'e geçer. Başarı: temiz bir Windows makinesinde kurulum tek akışta biter ve ajan render yapmaya
hazırdır. Bilinçli olarak yapılmayanlar: video üretim aşamalarının kendisi (0006), render sandbox'ı
(0006, H-4), cihaz eşleştirme ve ajanın web oturumunu takip etmesi (0002, H-1), hata bildiriminin
içeriği/gönderimi/bekletilmesi (0008), Günlük şeridi ve admin hata ekranı (0008, 0010), Linux/macOS ajanı.

## Requirements
1. Ajan açıldığında gerekli bileşenler kurulu değilse (ilk kurulum) kurulum ekranı açılır. Ekran, kurulacak
   her bileşeni adı, ne işe yaradığı (ör. "Video render motoru", "Seslendirme modeli") ve durumuyla
   listeler. Dokümanlarda bugün kararlaştırılmış liste Node.js LTS, FFmpeg, headless Chromium, Ollama ve
   izole Python ortamıdır (`docs/conventions.md`, `docs/architecture.md`); yerel ses ve görsel modelleri
   ilk_promt.txt'den gelir. Kesin liste ve zorunlu/isteğe bağlı ayrımı Q-1 kararına bağlıdır (B-5 raporu
   önerileri; karar değildir).
2. Kurulum, üzerinde yalnızca Windows bulunan bir bilgisayarda da tamamlanır: ajan her bileşeni kendisi
   indirir; kullanıcıdan önceden bir şey kurması istenmez (ilk_promt.txt: "format attı ve sonra bizim
   programı kurdu… bunu bile destekleyecek").
3. Kurulum sırasında ekran o an işlenen bileşeni "Şu an <bileşen> kuruluyor" diye ve bir ilerleme
   göstergesiyle gösterir; yapılan işler sade Türkçe satırlar halinde ekranda akar (ilk_promt.txt: "şu an
   bunları kuruyoruz gibisinden bir log var" — sesten yazıda "login" olarak geçmiş). Her bileşenin durumu
   şu sırayla ilerler: Bekliyor → İndiriliyor → Doğrulanıyor → Kuruluyor → Kuruldu; ya da Hata / Atlandı
   (isteğe bağlı bileşen seçilmediyse; Q-1'e bağlı).
4. Tüm bileşenler proje adıyla (**anewconcept**) ayrılmış kendi kurulum yerine kurulur. Ajan, bilgisayarda
   zaten bulunan aynı araçların başka sürümlerini, "Konsept" adlı başka programları ve kullanıcının sistem
   ayarlarını (ör. sistem genelindeki araç yolu ayarı) değiştirmez ve silmez (ilk_promt.txt: "projenin
   ismiyle beraber kurulum yap. Çakışma olmasın. Konsept adı altında başka bir program olabilir";
   `docs/conventions.md`: "Ajan kendi klasörüne, çakışmasız kurar"). Mevcut kurulumların yeniden kullanılıp
   kullanılmayacağı Q-2'ye, kurulum yerinin kendisi Q-3'e bağlıdır.
5. İndirilen her bileşen kurulmadan önce beklenen parmak iziyle (hash) doğrulanır; doğrulamadan geçmeyen
   bileşen kurulmaz ve Hata durumuna düşer.
6. Kurulmuş ve doğrulanmış bir bileşen, ajan yeniden açıldığında tekrar indirilmez. Tüm bileşenler
   kuruluysa ajan kurulum ekranını göstermeden tray'e iner.
7. Bir bileşen kurulamazsa ekranda o bileşen Hata durumunda, sade Türkçe bir sebeple (ör. "İnternet
   bağlantısı koptu", "Diskte yeterli yer yok", "Dosya doğrulanamadı") gösterilir; teknik ayrıntı (stack
   trace) gösterilmez. Kullanıcı **Tekrar dene** ile yalnızca hatalı bileşeni yeniden dener. Tamamlanmış
   bileşenler korunur.
8. Hata durumundaki bileşen için **Hatayı bildir** seçeneği bulunur (ilk_promt.txt: "kurulumlarda falan
   hata olduğu zaman… hata oldu bildir demeli. Bildirilen hatalar da API'ye gitmeli"). Seçilince 0008'deki
   ajan hata bildirimi akışı başlar ve bildirime hangi bileşenin hangi adımda (indirme / doğrulama /
   kurulum) hata verdiği eklenir. Bildirimin içeriği, maskelenmesi, ajan bağlı ya da eşleştirilmiş değilken
   bekletilmesi ve gönderimi 0008'de tanımlıdır ve orada kanıtlanır (0008 Req 10–11, C-1).
9. Kurulum tamamlanınca ajan henüz eşleştirilmemişse cihaz eşleştirme adımına geçer (0002); eşleştirilmişse
   kurulum ekranı kapanır ve ajan tray'de çalışmaya devam eder.
10. Ajanın kendisi ya da bir bileşeni için güncelleme olduğunda aynı ekran "Şu an bunları güncelliyoruz"
    başlığıyla açılır ve yalnızca güncellenecek bileşenleri listeler (ilk_promt.txt: "Gerektiği zaman
    ileride update'lerde de bunları gösterebiliriz"). Güncellemenin ne zaman ve nasıl başlayacağı Q-4'e
    bağlıdır.
11. Güncelleme paketleri imza ya da hash doğrulamasından geçmeden kurulmaz (`docs/security.md`). Doğrulama
    başarısız olursa paket kurulmaz, ajan mevcut sürümüyle çalışmaya devam eder ve kullanıcıya hata
    gösterilir. Doğrulama yöntemi Q-5'e bağlıdır.
12. Kurulum ya da güncelleme bitince ajan tray'e iner; tray davranışı 0001'deki gibidir ve bu spec'te
    değişmez ("Siteye git", "Çıkış", pencereyi kapatmak uygulamayı kapatmaz — 0001 AC-5). Ek menü öğeleri
    self-critique 10'a bağlıdır.
13. Kurulum ve güncelleme olayları (başladı, bileşen kuruldu, hata, doğrulama başarısız) ajanın yerel
    teknik loguna Information/Warning/Error seviyesinde yazılır; loglarda gizli bilgi bulunmaz. Log biçimi,
    saklama süresi (1 hafta) ve maskeleme 0008'de tanımlıdır.
14. Kurulum ekranındaki tüm metinler Türkçedir ve kaynak dosyalarından gelir (`docs/architecture.md`).

## User experience
- Flow (ilk kurulum): Kullanıcı ajan kurulum dosyasını çalıştırır (nereden indirileceği Q-6) → kurulum
  ekranı açılır, kurulacak bileşenler ve (Q-1 kabul edilirse) isteğe bağlı bileşen seçimleri görünür →
  **Kurulumu başlat** → bileşenler sırayla "Şu an … kuruluyor" diye işlenir, yapılan işler ekranda satır
  satır akar → hepsi Kuruldu olur → "Kurulum tamamlandı" → eşleştirme adımı (0002) ya da doğrudan tray.
  Hata olursa: hatalı satır kırmızı + "Hata" yazısı, sebep metni, **Tekrar dene** / **Hatayı bildir**
  (0008 akışı).
- Flow (güncelleme): Güncelleme bulunur (Q-4) → aynı ekran "Şu an bunları güncelliyoruz" başlığıyla
  açılır → yalnızca değişen bileşenler işlenir → "Güncelleme tamamlandı" → tray. Doğrulama başarısızsa
  "Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor" hatası.
- States:
  - Kurulum ekranı — hazır (bileşen listesi, Başlat etkin) · ön kontrol başarısız: internet yok / disk
    yetersiz (Başlat pasif, sebep metni) · kuruluyor (aktif bileşen + ilerleme + akan satırlar) · bileşen
    hatası · tümü tamamlandı.
  - Bileşen satırı — Bekliyor · İndiriliyor · Doğrulanıyor · Kuruluyor · Kuruldu · Atlandı · Hata.
  - Güncelleme ekranı — güncelleniyor · güncelleme tamamlandı · güncelleme doğrulanamadı.
  - Zaten kurulu — ekran açılmaz, ajan doğrudan tray'e iner.
  - Hata bildirimi penceresinin durumları (gönderiliyor, gönderildi, bekletiliyor, gönderilemedi) 0008'e,
    ajan oturum durumları (eşleşti, oturum kapandı) 0002'ye aittir.
- Design reference: none — the flow above is the design. (`source/*.png` görselleri, commit `61cc824`,
  yalnızca web sayfalarını çizer; ajan kurulum ekranı için tasarım yok.) Renk/tema önerisi self-critique 6'da.
- Accessibility: tüm düğmeler (Başlat, Tekrar dene, Hatayı bildir, Kapat) klavyeyle erişilebilir (Tab,
  Enter/Space); her bileşen satırının durumu ekran okuyucuya metin olarak verilir (yalnızca renkle değil:
  Hata kırmızı + "Hata" yazısı); ilerleme göstergesinin erişilebilir adı ve değeri var; metin/arka plan
  kontrastı WCAG AA.

## Constraints & out of scope
- Sensitive area: yok (tartışmalı → self-critique 14). Bu spec LLM'in ürettiği kodu çalıştırmaz; kurulum ve
  güncelleme, bizim yazdığımız ve kullanıcı emriyle çalışan ajan işleridir ve `docs/domain.md` gereği H-4
  kapsamı dışındadır. Ancak `docs/security.md` güncelleme imza/hash kuralını H-4 başlığı altında sayıyor;
  bu yüzden AC-6 ve AC-12 yine de kasıtlı bozma ile kanıtlanır. Ajanın web oturumunu takip etmesi (H-1)
  bu spec'te değil, 0002'dedir.
- Platform: yalnızca Windows (öncelikli ajan; Linux/macOS kapsam dışı, `docs/architecture.md`).
- Bileşenlerin hangileri olduğu: dokümanlardaki liste (Node.js LTS, FFmpeg, headless Chromium, Ollama,
  izole Python) bugünkü karardır; B-5 raporunun önerdiği değişiklikler (Supertonic ONNX, Chatterbox, sd-server,
  Z-Image, FLUX.2 klein, Ollama'nın isteğe bağlı olması) **öneridir**, karar değildir → Q-1. Video
  motoru/TTS/görsel seçimlerinin kendisi 0006'nın kararıdır; bu spec yalnızca seçilenlerin kurulmasını
  kapsar.
- Out of scope: render, seslendirme ve görsel üretim aşamaları (0006); H-4 render sandbox'ı ve opsiyonel
  "güçlü mod" firewall kuralının kendisi (0006, B-5 K-16/K-18); cihaz eşleştirme ekranı, oturum iptali ve
  ajanın "oturum kapandı / yeniden eşleştir" göstermesi (0002 Req 15, 19, 21–25); hata bildiriminin
  içeriği, maskelemesi, bekletilmesi ve gönderimi (0008 Req 10–11); tray menüsünün temel davranışı (0001);
  web Günlük şeridi, log saklama, admin hata listesi (0008, 0010); ajan sürümlerinin Pi'ye yayınlanma süreci
  (deploy betikleri); kod imzalama sertifikası satın alma (Q-5).
- Split: proposed **0009a — İlk kurulum** (Req 1–9, 12–14; AC-1…AC-10, AC-14…AC-16) ve **0009b — Güncelleme
  ve kaldırma** (Req 10–11; AC-11…AC-13 + Q-6'daki indirme/kaldırma). Sıra: 0009a önce (0006'nın ajan
  render'ı bileşenlerin kurulu olmasına dayanır), 0009b sonra. Gerekçe: iki ayrı kullanıcı yeteneği, 16
  kriter. Karar insanın (Open questions, Q-7).

## Acceptance criteria
- [ ] AC-1 — Bileşenleri kurulu olmayan bir bilgisayarda ajan ilk açılışta kurulum ekranını açar; Q-1'de onaylanan her bileşen adı, amacı ve "Bekliyor" durumuyla listelenir; **Kurulumu başlat** etkindir.
- [ ] AC-2 — Üzerinde yalnızca Windows 11 bulunan temiz bir makinede (Node, Python, FFmpeg, Chromium yok) kurulum kullanıcı başka bir şey kurmadan tamamlanır; sonunda kurulan her araç anewconcept kurulum yerinden çalıştırıldığında sürümünü verir (gözlem; ajanın kendi "hazır" denetimi self-critique 16'ya bağlı).
- [ ] AC-3 — Kurulum sırasında ekran işlenen bileşeni "Şu an <bileşen> kuruluyor" metni ve ilerleme göstergesiyle gösterir ve yapılan işler Türkçe satırlar halinde akar; her bileşen Bekliyor → İndiriliyor → Doğrulanıyor → Kuruluyor → Kuruldu sırasını izler; seçilmeyen isteğe bağlı bileşen (Q-1 kabul edilirse) "Atlandı" olur.
- [ ] AC-4 — Kurulumdan önce makinede farklı sürümde Node ve Python ile "Konsept" adlı bir program/klasör varken, kurulumdan sonra bunların dosyaları, sürümleri ve kullanıcı/sistem ortam ayarları (araç yolu dahil) değişmemiştir; ajanın bileşenleri yalnızca anewconcept kurulum yerinde bulunur.
- [ ] AC-5 — Tüm bileşenleri kurulu bir makinede ajan yeniden açıldığında hiçbir bileşen yeniden indirilmez, kurulum ekranı açılmaz ve ajan 10 sn içinde tray'e iner.
- [ ] AC-6 — Beklenen hash'le uyuşmayan (kasıtlı bozulmuş) bir bileşen dosyası kurulmaz; satır "Hata — Dosya doğrulanamadı" gösterir; bozma kaldırılınca **Tekrar dene** bileşeni kurar.
- [ ] AC-7 — Bir bileşen hata verdiğinde satır kırmızı ve "Hata" yazısıyla, sade Türkçe sebeple görünür (stack trace yok); **Tekrar dene** yalnızca o bileşeni yeniden dener; daha önce Kuruldu olan bileşenler yeniden indirilmez.
- [ ] AC-8 — Hata durumundaki bir bileşen satırında **Hatayı bildir** bulunur; seçilince 0008'in ajan hata bildirimi akışı açılır ve bildirim özeti hatalı bileşenin adını ve hatanın adımını (indirme / doğrulama / kurulum) içerir; Kuruldu ya da Bekliyor satırlarında bu seçenek yoktur. (Gönderim, bekletme ve maskeleme 0008 AC-13/AC-14/AC-10'da kanıtlanır.)
- [ ] AC-9 — İnternet yokken ya da diskte seçili bileşenler için gereken yerden az yer varken **Kurulumu başlat** pasiftir ve sebep metni görünür (eşik self-critique 1); kurulum sürerken internet koparsa işlenen bileşen "Hata — İnternet bağlantısı koptu" olur, tamamlanmış bileşenler Kuruldu kalır.
- [ ] AC-10 — Tüm bileşenler Kuruldu olunca "Kurulum tamamlandı" görünür; ajan eşleştirilmemişse eşleştirme adımı (0002) açılır, eşleştirilmişse ekran kapanır ve ajan tray'de kalır.
- [ ] AC-11 — Yeni bir ajan/bileşen sürümü yayınlandığında güncelleme aynı ekranda "Şu an bunları güncelliyoruz" başlığıyla açılır ve yalnızca değişen bileşenleri listeler; tamamlanınca "Güncelleme tamamlandı" görünür, ajan yeni sürümle çalışır ve sitedeki "Ajanı test et" (0001) yeni ajan sürümünü gösterir.
- [ ] AC-12 — İmzası/hash'i geçersiz (kasıtlı bozulmuş) bir güncelleme paketi kurulmaz; ekranda "Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor" görünür; ajan önceki sürümle çalışmaya ve API'ye bağlı kalmaya devam eder; olay yerel loga Error olarak yazılır.
- [ ] AC-13 — Güncelleme yarıda kalırsa (hata ya da bilgisayar kapanması) ajan bir sonraki açılışta önceki çalışan sürümle açılır (self-critique 5 kararına bağlı).
- [ ] AC-14 — Kurulum başlangıcı, her bileşenin kuruluşu, her hata ve her doğrulama başarısızlığı yerel teknik loga uygun seviyede (Information/Warning/Error) yazılır; log dosyalarında şifre, token ya da API key geçmez.
- [ ] AC-15 — Kurulum ekranındaki tüm kontroller klavyeyle kullanılabilir ve erişilebilir ada sahiptir; bileşen durumları metin olarak okunur (yalnızca renk değil); kontrast WCAG AA; tüm metinler Türkçe ve kaynak dosyasından gelir.
- [ ] AC-16 — Kurulum ve güncelleme Windows yönetici izni (UAC) istemeden tamamlanır (Q-3 kararına bağlı; opsiyonel güçlü mod bu kriterin dışında).

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**(a) Netleştirme soruları (etkiye göre sıralı)**

1. **Q-1 — Hangi bileşenler kurulacak; hangileri zorunlu, hangileri isteğe bağlı?** (B-5 K-1, K-2, K-6,
   K-8, K-10, K-11, K-15, K-17; dokümanlarda geçen Ollama ve izole Python)
   - Öneri: **Zorunlu çekirdek:** Node 26 LTS, FFmpeg (LGPL build), sabit sürüm headless Chromium
     (chrome-headless-shell), HyperFrames tam sürüm sabit (bugün 0.9.43, telemetri kurulumda kapalı),
     Supertonic 3 ONNX ağırlıkları (ajanın içinde çalışır, Python gerekmez); 0006 altyazı için Whisper.net
     kelime zamanlamasını (K-10) seçerse onun modeli ve Türkçe glifli fontlar da çekirdeğe girer.
     **İsteğe bağlı (kurulum ekranında işaretlenir, sonradan aynı ekrandan eklenebilir):** uv + izole Python
     + Chatterbox Multilingual V3 (ses klonlama); stable-diffusion.cpp `sd-server` (ekran kartına uygun
     paket) + Z-Image-Turbo GGUF, ikinci olarak FLUX.2 klein 4B (yerel görsel — ilk_promt.txt'de "Z-Image
     Turbo, Flux 2 Klein 4B" olarak geçiyor); Ollama yalnızca kullanıcı yerel LLM sağlayıcısı kullanmak
     isterse. Remotion varsayılan kurulmaz (K-2 lisans).
   - Gerekçe: Rapor Ollama'nın Windows'ta TTS ve görsel üretimi desteklemediğini söylüyor; zorunlu
     tutmak boşa GB'larca indirme demek. Çekirdek tek başına "koddan çizilmiş görüntü + yerel Türkçe ses"
     videosu üretebilir; ağır modeller yalnızca isteyene. **Not:** `docs/conventions.md` ve
     `docs/architecture.md` bugün Ollama ve izole Python'u ajanın kurduğu araçlar arasında sayıyor; bu öneri
     kabul edilirse o iki doküman (gerekirse ADR ile) güncellenir. B-5 kararları 0006'da verilecek; bu liste
     onlara göre güncellenir.
2. **Q-2 — Bilgisayarda zaten kurulu Node/Python/FFmpeg varsa kullanılsın mı?**
   - Öneri: Hayır; ajan her zaman kendi sabit sürümlerini anewconcept kurulum yerine kurar ve yalnızca
     onları kullanır.
   - Gerekçe: H-4 katmanları belirli sürümlere bağlı (Node 26 `--permission`, sabit Chromium; B-5 L5/L6);
     kullanıcının sürümü güncellenince ajan bozulmaz; "çakışma olmasın" isteğiyle birebir uyumlu.
3. **Q-3 — Kurulum yeri, yönetici izni ve ad.**
   - Öneri: Kullanıcı başına kurulum, yönetici izni istemeden (`%LOCALAPPDATA%\anewconcept`); görünen ad
     "ANEW Konsept", tüm teknik adlar (klasör, kısayol, başlangıç kaydı, kaldırma kaydı) `anewconcept`.
     Videoların durduğu klasör ayrı ve ilk kurulumda değiştirilebilir. Opsiyonel güçlü mod (K-18 firewall
     kuralı) tek seferlik yönetici izniyle, varsayılan kapalı, kararı 0006'da.
   - Gerekçe: B-5 "çalışma zamanında admin yok" öneriyor; UAC'sız kurulum ofis/aile PC'lerinde de çalışır;
     "Konsept" adlı başka bir programla ad çakışmaz (Pi'deki `anewconcept` kararıyla da tutarlı, D3). B-5
     raporu iş klasörü için `%LOCALAPPDATA%\Konsept\jobs` örneği veriyor; bu, ilk_promt.txt'deki "Konsept
     adı altında başka bir program olabilir" uyarısıyla çakışır — 0006 sandbox'ı da `anewconcept` altını
     kullanmalı.
4. **Q-4 — Güncelleme ne zaman ve nasıl başlar?**
   - Öneri: Ajan açılışta ve 6 saatte bir API'ye yeni sürüm sorar; varsa tray bildirimi gösterir, kullanıcı
     "Şimdi güncelle" der. Üretim işi sürerken güncelleme başlamaz (iş bitince ya da kullanıcı durdurunca,
     BR-10). Ajan sürümü sunucuyla uyumsuzsa (tek ürün versiyonu, `docs/conventions.md`) ajan komut almaz,
     web durum alanı "Ajan güncellemesi gerekli" gösterir ve güncelleme ekranı açılır.
   - Gerekçe: Kullanıcı habersizken render yarıda kesilmez; Web/Api/Ajan tek versiyon olduğu için uyumsuz
     ajanın iş alması yanlış sonuç üretebilir.
5. **Q-5 — "İmza ya da hash": hangisi, paketler nereden?**
   - Öneri: Ajan güncellemeleri Pi'deki API'den sunulur; bir sürüm listesi (manifest) bizim özel anahtarımızla
     imzalanır (ör. Ed25519), açık anahtar ajana gömülüdür; listedeki her dosya SHA-256 ile doğrulanır.
     Üçüncü taraf bileşenler (Node, FFmpeg, Chromium, modeller) resmi kaynaklardan indirilir ama ajana
     gömülü sabit SHA-256 listesiyle doğrulanır; arşivlenme riski olan Supertonic ağırlıkları aynalanır
     (B-5 K-6). Windows kod imzalama sertifikası şimdilik alınmaz (maliyet), SmartScreen uyarısı kabul.
   - Gerekçe: İmzalı liste, Pi ele geçirilse bile sahte güncellemeyi engeller; yalnızca hash kullanmak,
     hash'leri sunan kaynak ele geçirilirse koruma sağlamaz.
6. **Q-6 — Ajanın ilk indirilmesi ve kaldırılması bu spec'te mi?**
   - Öneri: Web'de (Ayarlar sayfası — temeli 0004 — ve "Ajan bağlı değil" durumunda) "Ajanı indir"
     bağlantısı; tek kurulum dosyası Pi'den sunulur. Kaldırma Windows "Uygulamalar" listesinden yapılır,
     anewconcept kurulum yerini ve bileşenleri siler, video klasörünü silmeden önce sorar. İkisi de 0009b'ye
     gitsin.
   - Gerekçe: Brief "minik bir tane de uygulamayı indirecek, bilgisayarına kuracak" diyor ama nereden
     indirileceği hiçbir spec'te tanımlı değil; kaldırmasız profesyonel kurulum olmaz, ama videolar
     kullanıcının emeği, sormadan silinmemeli.
7. **Q-7 — Split onayı:** 0009a İlk kurulum → 0009b Güncelleme ve kaldırma (Constraints, Split).
   - Öneri: Böl.
   - Gerekçe: iki kullanıcı yeteneği, 16 kriter; 0006'nın ihtiyacı yalnızca 0009a.

(Önceki taslaktaki "eşleştirilmemiş ajanda Hatayı bildir" sorusu 0008 C-1 ile aynı karardır; tekrar
edilmez — 0008'de ne karar verilirse kurulum ekranı onu kullanır.)

**(b) Self-critique boşlukları (hostile developer okuması, önerilerimle)**

1. AC-9 disk ön kontrolü eşiği belirsiz — Öneri: seçili bileşenlerin indirme + açılmış boyutu toplamı
   %20 payla; ekranda "Gereken: X GB, Boş: Y GB" (bu da bileşen boyutlarının ekranda gösterilmesini
   gerektirir — insan onayı gerekli).
2. Kurulum yarıda kesilirse (bilgisayar kapandı) ne olur? — Öneri: yeniden açılışta Kuruldu olanlar
   korunur, yarım kalan bileşen baştan indirilir, ekran kaldığı yerden devam eder; bir AC eklensin.
3. Kurulum sürerken pencere kapatılırsa? — Öneri: "Kurulum sürüyor, arka planda devam etsin mi?" onayı;
   devam ederse tray ipucunda ilerleme; "Çıkış" kurulumu güvenli durdurur (bileşen yarımsa silinir).
4. Ekran kartı tespiti — Öneri: NVIDIA → CUDA paketi, diğerleri → Vulkan, yoksa CPU; ekranda tespit edilen
   kart ve VRAM gösterilir; 6 GB altı VRAM'de yerel görsel/Chatterbox seçeneğinde uyarı (B-5 K-14).
5. Güncelleme yarıda kalınca geri dönüş (AC-13) — Öneri: bir önceki çalışan sürüm saklanır, yeni sürüm
   ilk açılışta API'ye bağlanamazsa otomatik eski sürüme dönülür; yalnızca bir önceki sürüm tutulur.
6. Kurulum ekranının görünümü tanımsız — Öneri: sitenin turuncu/kahve paleti; açık/koyu, Windows
   uygulama temasını izler; ikon `source/` içindeki ikon (B-6).
7. Lisanslar ve AI ses beyanı — Öneri: bileşen satırında lisans adı; Supertonic OpenRAIL-M kısıtları
   (B-5 K-7) ve Chatterbox filigranı için kısa bilgi; kurulum öncesi tek "Okudum" onay kutusu.
8. Hata bildirimindeki kişisel veri — Öneri: dosya yollarındaki Windows kullanıcı adı maskelenir
   (`C:\Users\***\…`); makine adı gönderilmez. Karar 0008 C-4 ile birlikte verilir (bildirim içeriği 0008'in).
9. Kurulumun toplam süresi için hedef yok — Öneri: süre hedefi koyma (indirme hızına bağlı); yalnızca
   AC-5'teki "kuruluysa 10 sn içinde tray" sınırı ölçülsün (0001'deki 10 sn sınırlarıyla aynı ölçü).
10. Tray menüsüne ekler — Öneri: "Kurulum ve bileşenler" (ekranı açar, isteğe bağlı bileşen ekleme),
    "Güncellemeleri denetle"; menü başında oturum durumu satırı 0002'nin ajan durumlarıyla birlikte.
11. Kurulu dosyaların sonradan bozulması/değiştirilmesi — Öneri: açılışta yalnızca yürütülebilir
    dosyalarda hızlı hash denetimi; uyuşmazsa bileşen "Onarılmalı" olur ve tek tıkla yeniden kurulur.
12. AC-2/AC-4 kanıtı nasıl alınır? — Öneri: birim/headless testlerde sahte indirici ve sahte dosya sistemi;
    temiz makine kanıtı Windows Sandbox ya da temiz VM'de agent'ın aldığı ekran görüntüsü + ortam
    değişkeni/klasör karşılaştırması, insan SHIP'te değerlendirir (`docs/testing.md`).
13. Kurulum sırasında indirme kaynağı (ör. Hugging Face, nodejs.org) erişilemezse — Öneri: her bileşen
    için en fazla 2 otomatik yeniden deneme, sonra Hata; Pi'de aynalanmış kopya varsa ondan denenir.
14. Hassas alan belirsizliği — `docs/domain.md` H-4'ü yalnızca "LLM kodunun çalıştırılması" olarak
    daraltıp kurulumu dışarıda bırakıyor, `docs/security.md` ise güncelleme imza/hash kuralını H-4
    başlığı altında sayıyor. Bu spec "yok" dedi ama dokümanlar çelişiyor. Öneri: "Sensitive area: yok"
    kalsın, AC-6/AC-12 yine kasıtlı bozmayla kanıtlansın; `docs/security.md`'de bu madde H-4 başlığından
    ayrı bir "Ajan güncellemeleri" başlığına taşınsın (insan kararı, docs değişikliği).
15. Eski sürüme düşürme (downgrade) — imzası geçerli ama eski bir sürüm listesi sunulursa (tekrar oynatma)
    ajan onu kurar mı? Öneri: ajan kendi sürümünden eski ya da aynı sürümü kurmaz; bir AC eklensin
    (kasıtlı bozma: eski imzalı liste sunulur → kurulmaz).
16. Taslakta yazarın kendi önerdiği, kaynakta geçmeyen arayüz ayrıntıları — **Kurulumu başlat** düğmesi
    (otomatik başlama yerine), internet/disk ön kontrolü, **Tekrar dene**, durum adları, 10 sn sınırı.
    Ayrıca kurulum sonunda ajanın küçük bir örnek komut çalıştırıp "Hazır" göstermesi önerilmişti; kaynakta
    yok, AC-2'den çıkarıldı. Öneri: hepsi kalsın ("profesyonel görünüm" ve "hata oldu bildir" isteklerinin
    doğal parçası), "Hazır" denetimi de eklensin (0006'nın "Ajan kurulumu eksik: <araç>" hatasından önce
    yakalar); insan onaylarsa AC'lere işlenir.

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
