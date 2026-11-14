# Taha Konsept — Ana Plan

> Kaynak: `promptlar/0001`, `raporlar/0001-teknoloji-arastirmasi/`, `memory/`. Tarih: 2026-11-14. Hedef: **tüm özellikler eksiksiz → 1.0.0** (kullanıcı kararı: "hepsi tam olacak"). Aşağıdaki fazlar sadece yapım sırasıdır, kapsamı daraltmaz.

## 1. Kesinleşen kararlar
| Konu | Karar |
|---|---|
| Kullanım | **Tek kullanıcı, şahsi, ticari değil.** Pi dışarı açılmaz, sadece yerel ağdan ben erişirim. "Şu an sadece ben gibi" geliştirilir: kayıt kapalı, kurulumda tek hesap oluşturulur (ajan eşleme ve web oturumu için login yine var). Rol/izin/ban altyapısı kodda genişletilebilir şekilde durur ama arayüz önceliği düşük. **Lisans tartışması yok:** kişisel kullanıma izin veren her model eklenir. |
| Sunucu | Pi 5 (Debian 13 arm64): .NET 10 LTS runtime (framework-dependent), 2 systemd servisi, PostgreSQL 17 (Pi'de kurulu), modül başına şema. Portlar: **Web 7110, API 7111** (boş; kesinleşince doğrulanacak). |
| API key | Key'leri ben girerim; AES-256-GCM ile şifreli saklanır. **Tüm online çağrılar (LLM, online TTS, online görsel) Pi'deki API'den yapılır**; key bilgisayara inmez. |
| Masaüstü ajanı | Avalonia tray (sağ tık: Siteye git, Çıkış) + kurulum/güncelleme ekranı. Velopack installer, packId `TahaKonsept.Agent`, bağımlılıklar `%LocalAppData%\TahaKonsept\` (isim çakışması yok). Node/ffmpeg/Python **portable**. Web'den çıkış → ajan yetkisini kaybeder. |
| Web ↔ ajan | Ajan API'deki **SignalR hub**'a dışarıya doğru bağlanır (komut, ilerleme, log). Cihaz eşleme: RFC 8628 cihaz kodu. |
| Tarayıcı | Sadece **jQuery Ajax** (fetch yasak). Canlı ilerleme: tarayıcı → MVC → API **polling** (SignalR JS istemcisi içeride fetch kullandığı için kullanılmaz). |
| Video depolama | Tam kalite video **kullanıcının PC'sinde** kalır; web oynatıcı ajanın yerel, token'lı HTTP sunucusundan oynatır (Pi diski dolmaz). PC kapalıyken izlenemez. |
| Video motoru | **HyperFrames** (varsayılan, sürüm sabitli, site üzerinden güncellenir), **Remotion** (≤3 kişi ücretsiz, UI'da lisans notu), **Revideo** (deneysel). |
| Altyazı / ilerleme çubuğu | Motor sessiz + altyazısız sahne render eder; altyazı (ASS karaoke) ve üst ilerleme çubuğu **ayrı ffmpeg aşamasında** basılır → aşama izolasyonu korunur, seçenekler gerçekten çalışır. |
| Ses (hepsi) | **Yerel:** Supertonic 3 (varsayılan; ONNX, C#, Python'suz), Chatterbox Multilingual (klonlama), EMA Lightning *(Sadece Türkçe)*, Ahenk / Ahenk Cep *(Sadece Türkçe)*, Trendyol-TTS *(Sadece Türkçe, deneysel)*, XTTS-v2 (klonlama), F5-TTS Türkçe, Piper tr_TR *(Sadece Türkçe)*, MMS-TTS-tr *(Sadece Türkçe)*, OmniVoice, VoxCPM2 *(≥8 GB VRAM)*, Fish S2 Pro *(≥12 GB VRAM)*, Kokoro *(Türkçe yok; İngilizce vb.)*. **Online:** OpenAI gpt-4o-mini-tts, ElevenLabs, Gemini Flash TTS, Azure Neural, Cartesia. Kapalı = sessiz video. Ollama Phonon teknik nedenle yok (Windows/Türkçe desteği yok). |
| Görsel (hepsi) | Kapalı (koddan çiz). **Yerel** (stable-diffusion.cpp; ComfyUI isteğe bağlı): Z-Image Turbo (varsayılan), FLUX.2 klein 4B, FLUX.2 klein 9B, FLUX.3 Klein *(≥8 GB)*, SD3.5 Medium, SDXL-Lightning *(≤4 GB yedek)*, Qwen-Image *(≥12 GB)*. **Online:** Nano Banana 2.1, Gemini 3.1 Flash Lite Image, Gemini 3 Pro Image, gpt-image-2, gpt-image-1-mini, fal.ai (Z-Image Turbo, FLUX.2 Flash…), BFL FLUX.3 Klein, GLM-Image / CogView-4, Replicate. Görsele yazı yazdırılmaz; metin video katmanında. |
| Donanım etiketi | Her yerel modelin VRAM/disk ihtiyacı katalogda tutulur; ajan GPU'yu algılar, yetmeyen model listede "VRAM yetersiz" etiketiyle gösterilir (gizlenmez). |
| Türkçe okuma | Seslendirmeden önce **Okunuş (Normalize)** aşaması: ekranda/altyazıda yazılış korunur ("Google", "1453", "II. Mehmed"), TTS'e okunuş gider ("Gugıl", "bin dört yüz elli üç", "İkinci Mehmed"). Ayrıntı §3.1. |
| LLM | Anthropic (resmi C# SDK) + OpenAI-uyumlu ortak base → OpenAI, OpenRouter, GLM, Kimi. Varsayılanlar: konsept Sonnet 5.5, video kodu Opus 5.5 (sahne başına, streaming, max_tokens 64k), konu önerisi Haiku 5.5. Efor seçilebilir. |
| Maliyet | Her çağrı `CallLog` + sürümlü `ModelPrice` → aylık harcama (dolar + sent) LLM + TTS + görsel dahil. |
| Mediator | MediatR yok (ticari lisans). Controller → servis arayüzü; FluentValidation generic filtre. |
| Parola / auth | ASP.NET Core Identity (PBKDF2), MVC cookie + API JWT (15 dk) + rotating refresh token, permission policy'leri. |

## 2. Çözüm yapısı (taslak)
```
TAHA/
  src/
    BuildingBlocks/            # SharedKernel: Result, Entity base, generic repository, Ajax/Response sözleşmeleri, logging
    Modules/
      Identity/   {Domain, Application, Infrastructure, Api, Contracts}   # kullanıcı, rol/izin, ban, cihaz eşleme
      Profiles/   {...}        # kanal profilleri + hazır şablonlar
      Providers/  {...}        # ILlmModel / ITextModel / IImageModel / ISpeechModel …, API key kasası, ModelPrice, CallLog
      Concepts/   {...}        # konu önerisi, fikirden üretim, düzenleme döngüsü, kelime denetimi, silinenler, konu hafızası
      Production/ {...}        # üretim sırası, pipeline aşama grafiği, ajan komutları, sürümler (yeni sürüm)
      Agents/     {...}        # SignalR hub, cihaz kaydı, ön gereksinim manifesti
      Diagnostics/{...}        # loglar, hata bildirimleri, aktiviteler, admin istatistikleri
    Hosts/
      TahaKonsept.Api/         # tüm modüllerin Api katmanlarını barındırır
      TahaKonsept.Web/         # MVC (+ Areas/Admin, Chart.js), jQuery Ajax, tema
    Desktop/
      TahaKonsept.Agent/       # Avalonia tray + kurulum ekranı
      TahaKonsept.Agent.Core/  # pipeline yürütücüleri: IVideoEngine, ISpeechEngine, IImageEngine (Abstract/Base/Concrete)
  tests/                       # birim + entegrasyon + ArchitectureTests (dosya başına tek nesne, summary zorunlu, katman bağımlılıkları)
  deploy/pi/                   # kurulum betiği, systemd unit'leri
  VERSION                      # tek versiyon kaynağı (0.1.0)
```
Her bölümde `Abstract/` (interface) · `Base/` (abstract) · `Concrete/` klasörleri. Kurallar mimari testlerle otomatik denetlenir (dosya başına tek tür, her türde `<summary>`, controller'da tek satır iş).

## 3. Pipeline (aşama grafiği)
`Script → FactSheet → ScenePlan → SceneCode → Images → Normalize(okunuş) → TTS → Align → SceneRender → Compose(altyazı + ilerleme çubuğu, ffmpeg) → Mux → Final`
- Her aşamanın çıktısı, girdilerinin ve ayarlarının hash'iyle saklanır. Ayar değişince sadece hash'i değişen aşama ve ondan sonrakiler çalışır.
- Sadece ses değişti → TTS, Align, Compose, Mux. Sahne süresi değiştiyse yalnızca süresi değişen sahneler yeniden render edilir (küçük farklar pad ile karşılanır).
- Sadece motor değişti → SceneCode, SceneRender, Compose, Mux (ses yeniden kullanılır).
- Metin değişti → hepsi.
- Durdur / devam: aşama ve sahne bazında kontrol noktası; devam edince biten aşamalar atlanır.
- Güvenlik: LLM'in yazdığı kod ağ erişimi kapalı, `file://` yasak, izinli CDN listesi, render öncesi `lint/check` kapısı.

### 3.1 Türkçe okunuş düzeltme (Normalize aşaması)
Her sahnede iki metin tutulur: **yazılış** (ekran + altyazı) ve **okunuş** (TTS'e giden). Altyazı hizalaması kelime eşlemesiyle yazılışa geri çevrilir; yani ekranda "Google" yazar, ses "Gugıl" der.
Zincir (`ITextNormalizer` → `TextNormalizerBase` → somut sınıflar, sırayla):
1. **Sözlük (Okunuş sözlüğü):** kullanıcının düzenlediği global + profil bazlı liste (Google → Gugıl, YouTube → Yutüb, NVIDIA → Enviıdiya). En yüksek öncelik. Ayarlar'da ekle/düzenle + "Dinle" önizleme butonu.
2. **Sayılar:** 1453 → bin dört yüz elli üç; 3,5 → üç virgül beş; %40 → yüzde kırk; ekli sayılar (1453'te → bin dört yüz elli üçte, ünlü uyumuyla).
3. **Tarihler:** 29 Mayıs 1453, 29.05.1453, MÖ 330 / M.Ö. → milattan önce, MS → milattan sonra.
4. **Romen rakamları ve sıra sayıları:** II. Mehmed → İkinci Mehmed, XV. yüzyıl → on beşinci yüzyıl, 3. → üçüncü.
5. **Kısaltmalar:** yy. → yüzyıl, vb. → ve benzeri, Dr. → doktor, km → kilometre, TBMM → te be me me (harf harf okunacaklar listesi).
6. **LLM okunuş yardımcısı (isteğe bağlı):** sözlükte olmayan yabancı özel isimleri konsept üretimi sırasında LLM tek seferde işaretleyip okunuşunu önerir (ek çağrı yok, konsept JSON'una `pronunciations` alanı). Öneriler sözlüğe "öneri" olarak düşer, onaylanınca kalıcı olur.
- TTS motoru başına açılıp kapanabilir (bazı motorlar sayıları kendi okuyor). Okunuş metni sürümlenir; sözlük değişince sadece Normalize → TTS → sonrası yeniden çalışır.
- Testler: tarih/sayı/romen/ek uyumu için tablo tabanlı birim testleri (Türkçe `tr-TR` kültürü, i/İ dönüşümü).

## 4. Fazlar (yapım sırası) ve versiyon
| Faz | Kapsam | Versiyon |
|---|---|---|
| F0 İskelet | Solution, BuildingBlocks, mimari testler, Serilog (7 gün, günlük dosya), VERSION, Pi hazırlığı (.NET 10, DB, systemd, port), deploy betiği | 0.1.0 |
| F1 Temel web | Identity (kurulumda tek hesap + giriş; rol/izin altyapısı), layout (üst bar, aylık harcama, profil dropdown + ✏️ ➕, hata bildir, ayarlar, ☀/🌙), alt Günlük şeridi, Ayarlar (API key'ler, önerileri sıfırla), Profiller + şablonlar, Yayınlama "Çok yakında" | 0.1.x |
| F2 Konseptler | Sağlayıcı katmanı (Anthropic + OpenAI-uyumlu 4 sağlayıcı), maliyet sayacı, model/ince ayar modalları, konudan üret (başlık → seçtiklerini üret), fikirden üret, gerçek bilgi için fact-sheet (web search), kelime tekrar denetimi, düzenleme döngüsü modalı, Silinenler + negatif konu hafızası | 0.2.0 |
| F3 Masaüstü ajanı | Avalonia tray, Velopack installer, ön gereksinim kurulum ekranı (Node, ffmpeg, chrome-headless-shell, HyperFrames, Supertonic modeli, isteğe bağlı Python+CUDA+Chatterbox, sd.cpp + modeller), cihaz eşleme, SignalR, yerel log, hata bildirimi, güncelleme | 0.3.0 |
| F4 Konseptten Video | Üretim sırası (↑ ↓ ✕, sırayla üret, durdur/devam, aşama aşama ilerleme), HyperFrames + Normalize (Türkçe okunuş, sözlük ekranı) + Supertonic + ffmpeg compose, altyazı ve ilerleme çubuğu seçenekleri, ajan üzerinden oynatma | 0.4.0 |
| F5 Üretilen Videolar | Liste, oynatıcı (ses, tam ekran), Klasörde aç, Sistem oynatıcısında aç, Yeniden üret (düzenleyerek), Sil, sahneler/metin/ücret, **Yeni sürüm** (sadece değişen aşama) | 0.5.0 |
| F6 Sağlayıcı genişletme | Remotion, Revideo, §1'deki **tüm** ses ve görsel modelleri (yerel + online, VRAM etiketli katalog), ComfyUI seçeneği, forced alignment | 0.6.0 |
| F7 Admin + sertleştirme | Admin panel (istatistikler, toplam klip, aktiviteler, hata bildirimleri, Chart.js; kullanıcı/ban ekranı tek kullanıcı olduğu için basit), güvenlik gözden geçirmesi, uçtan uca testler | 0.7.0 → **1.0.0** |

Not: Kullanıcı MINOR'un tutumlu artırılmasını istedi; fazlar kapsamlı sayfa değişikliği olduğu için MINOR artırıyor. Ara düzeltmeler PATCH.

## 5. Varsayılanlar (bloklamayan; değiştirmek istersen söyle)
1. Online TTS varsayılanı: Türkçede OpenAI zayıf çıktı → **Gemini Flash TTS** varsayılan; diğerleri seçilebilir.
2. Bütçe limiti: aylık limit girilir; aşılınca **uyarı + yeni üretimi durdurma**.
3. Arka plan müziği: F6'da isteğe bağlı seçenek.
4. Manim "eğitim" stili: 1.0 sonrası.
