# Taha Konsept — Ana Plan

> Kaynak: `promptlar/0001`, `raporlar/0001-teknoloji-arastirmasi/`, `memory/`. Tarih: 2026-11-14. Hedef: **tüm özellikler eksiksiz → 1.0.0** (kullanıcı kararı: "hepsi tam olacak"). Aşağıdaki fazlar sadece yapım sırasıdır, kapsamı daraltmaz.

## 1. Kesinleşen kararlar
| Konu | Karar |
|---|---|
| Kullanım | Açık kaynak, ticari değil. Herkes kendi Raspberry Pi'sine kurar; Pi dışarı açılmaz (sadece yerel ağ). Yine de çok kullanıcılı (kayıt, rol/izin, ban, admin). İlk kayıt olan kullanıcı = admin. |
| Sunucu | Pi 5 (Debian 13 arm64): .NET 10 LTS runtime (framework-dependent), 2 systemd servisi, PostgreSQL 17 (Pi'de kurulu), modül başına şema. Portlar: **Web 7110, API 7111** (boş; kesinleşince doğrulanacak). |
| API key | Her kullanıcı kendi key'ini girer (BYOK); AES-256-GCM ile şifreli saklanır. **Tüm online çağrılar (LLM, online TTS, online görsel) Pi'deki API'den yapılır**; key bilgisayara inmez. |
| Masaüstü ajanı | Avalonia tray (sağ tık: Siteye git, Çıkış) + kurulum/güncelleme ekranı. Velopack installer, packId `TahaKonsept.Agent`, bağımlılıklar `%LocalAppData%\TahaKonsept\` (isim çakışması yok). Node/ffmpeg/Python **portable**. Web'den çıkış → ajan yetkisini kaybeder. |
| Web ↔ ajan | Ajan API'deki **SignalR hub**'a dışarıya doğru bağlanır (komut, ilerleme, log). Cihaz eşleme: RFC 8628 cihaz kodu. |
| Tarayıcı | Sadece **jQuery Ajax** (fetch yasak). Canlı ilerleme: tarayıcı → MVC → API **polling** (SignalR JS istemcisi içeride fetch kullandığı için kullanılmaz). |
| Video depolama | Tam kalite video **kullanıcının PC'sinde** kalır; web oynatıcı ajanın yerel, token'lı HTTP sunucusundan oynatır (Pi diski dolmaz). PC kapalıyken izlenemez. |
| Video motoru | **HyperFrames** (varsayılan, sürüm sabitli, site üzerinden güncellenir), **Remotion** (≤3 kişi ücretsiz, UI'da lisans notu), **Revideo** (deneysel). |
| Altyazı / ilerleme çubuğu | Motor sessiz + altyazısız sahne render eder; altyazı (ASS karaoke) ve üst ilerleme çubuğu **ayrı ffmpeg aşamasında** basılır → aşama izolasyonu korunur, seçenekler gerçekten çalışır. |
| Ses | Yerel: **Supertonic 3** (ONNX, C#, Python'suz, varsayılan), **Chatterbox Multilingual** (ilk sürümde VAR; gömülü Python + CUDA, kurulumda isteğe bağlı bileşen), EMA Lightning (Sadece Türkçe). Online: OpenAI, ElevenLabs, Gemini, Azure. Kapalı = sessiz video. |
| Görsel | Kapalı (koddan çiz) / yerel **Z-Image Turbo** ve **FLUX.2 klein 4B** (stable-diffusion.cpp, Python'suz, 6 GB VRAM) / online Nano Banana, fal.ai, gpt-image, GLM. Görsele yazı yazdırılmaz; metin video katmanında. |
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
`Script → FactSheet → ScenePlan → SceneCode → Images → TTS → Align → SceneRender → Compose(altyazı + ilerleme çubuğu, ffmpeg) → Mux → Final`
- Her aşamanın çıktısı, girdilerinin ve ayarlarının hash'iyle saklanır. Ayar değişince sadece hash'i değişen aşama ve ondan sonrakiler çalışır.
- Sadece ses değişti → TTS, Align, Compose, Mux. Sahne süresi değiştiyse yalnızca süresi değişen sahneler yeniden render edilir (küçük farklar pad ile karşılanır).
- Sadece motor değişti → SceneCode, SceneRender, Compose, Mux (ses yeniden kullanılır).
- Metin değişti → hepsi.
- Durdur / devam: aşama ve sahne bazında kontrol noktası; devam edince biten aşamalar atlanır.
- Güvenlik: LLM'in yazdığı kod ağ erişimi kapalı, `file://` yasak, izinli CDN listesi, render öncesi `lint/check` kapısı.

## 4. Fazlar (yapım sırası) ve versiyon
| Faz | Kapsam | Versiyon |
|---|---|---|
| F0 İskelet | Solution, BuildingBlocks, mimari testler, Serilog (7 gün, günlük dosya), VERSION, Pi hazırlığı (.NET 10, DB, systemd, port), deploy betiği | 0.1.0 |
| F1 Temel web | Identity (kayıt/giriş, rol/izin, ban), layout (üst bar, aylık harcama, profil dropdown + ✏️ ➕, hata bildir, ayarlar, ☀/🌙), alt Günlük şeridi, Ayarlar (API key'ler, önerileri sıfırla), Profiller + şablonlar, Yayınlama "Çok yakında" | 0.1.x |
| F2 Konseptler | Sağlayıcı katmanı (Anthropic + OpenAI-uyumlu 4 sağlayıcı), maliyet sayacı, model/ince ayar modalları, konudan üret (başlık → seçtiklerini üret), fikirden üret, gerçek bilgi için fact-sheet (web search), kelime tekrar denetimi, düzenleme döngüsü modalı, Silinenler + negatif konu hafızası | 0.2.0 |
| F3 Masaüstü ajanı | Avalonia tray, Velopack installer, ön gereksinim kurulum ekranı (Node, ffmpeg, chrome-headless-shell, HyperFrames, Supertonic modeli, isteğe bağlı Python+CUDA+Chatterbox, sd.cpp + modeller), cihaz eşleme, SignalR, yerel log, hata bildirimi, güncelleme | 0.3.0 |
| F4 Konseptten Video | Üretim sırası (↑ ↓ ✕, sırayla üret, durdur/devam, aşama aşama ilerleme), HyperFrames + Supertonic + ffmpeg compose, altyazı ve ilerleme çubuğu seçenekleri, ajan üzerinden oynatma | 0.4.0 |
| F5 Üretilen Videolar | Liste, oynatıcı (ses, tam ekran), Klasörde aç, Sistem oynatıcısında aç, Yeniden üret (düzenleyerek), Sil, sahneler/metin/ücret, **Yeni sürüm** (sadece değişen aşama) | 0.5.0 |
| F6 Sağlayıcı genişletme | Remotion, Revideo, Chatterbox, EMA Lightning, online TTS'ler, yerel görsel (Z-Image, FLUX.2 klein), online görseller, forced alignment | 0.6.0 |
| F7 Admin + sertleştirme | Admin panel (kullanıcılar, toplam klip, aktiviteler, ban, hata bildirimleri, Chart.js), güvenlik gözden geçirmesi, uçtan uca testler | 0.7.0 → **1.0.0** |

Not: Kullanıcı MINOR'un tutumlu artırılmasını istedi; fazlar kapsamlı sayfa değişikliği olduğu için MINOR artırıyor. Ara düzeltmeler PATCH.

## 5. Açık (bloklamayan) sorular — varsayılanla ilerlenir
1. Online TTS varsayılanı: Türkçede OpenAI zayıf çıktı → **Gemini Flash TTS** varsayılan, OpenAI/ElevenLabs/Azure seçilebilir.
2. Repo lisansı: **MIT** (kullandığımız model lisansları ayrıca NOTICE dosyasında).
3. Bütçe limiti: kullanıcı aylık limit girer; aşılınca **uyarı + yeni üretimi durdurma**.
4. Videolara "AI üretimi" etiketi ve arka plan müziği: F6'da isteğe bağlı seçenek.
5. Manim "eğitim" stili: 1.0 sonrası.
