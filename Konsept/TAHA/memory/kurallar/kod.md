# Kodlama Kuralları

## Mimari
- **Modüler monolit**; her modül kendi içinde **Clean Architecture** (ayrı projeler: Domain / Application / Infrastructure / Api vb.).
- 3 ana uygulama: **Desktop** (.NET Avalonia, tepsi/tray ajanı + kurulum ekranı), **Web** (ASP.NET Core MVC: HTML/CSS/JS), **API** (ASP.NET Core Web API).
- Web'den **tüm istekler jQuery Ajax** ile. **`fetch` YASAK.** İstekler önce MVC controller'a, oradan API'ye gider (token/auth kontrolü bu yolda).
- API şimdilik sadece web'e hizmet eder ama ileride mobil/masaüstü de bağlanabilecek şekilde tasarlanır.
- Pipeline (konsept → kod → görsel → ses → render → birleştirme) **aşama izole**: bir aşamanın ayarı değişince sadece o aşama ve bağımlıları yeniden çalışır.

## Interface → Abstract → Concrete (çıplak class YASAK)
- Her yapı: **interface** (sözleşme) → **abstract base** (ortak davranış, generic metotlar) → **concrete class**.
- Sistem interface'ler üzerinden çalışır; yeni sağlayıcı = yeni concrete class, sisteme sadece kayıt edilir.
- Örnek: `ILlmModel` (PlatformName, BaseUrl, ApiKey, ModelId…) → `LlmModelBase` (generic `RunAsync<TResponse>(string prompt, string systemPrompt)`) → `AnthropicModel`, `OpenAiModel`… Metin/görsel/ses gibi farklı yetenekler ayrı interface dallarıyla (`ITextModel`, `IImageModel`, `ISpeechModel`…).
- Klasör yapısı (her bölüm için): `Abstract/` (interface'ler), `Base/` (abstract class'lar), `Concrete/` (somut class'lar).

## Dosya başına TEK nesne (YASAK kuralı)
- **Bir dosyada birden fazla nesne (class, interface, abstract, enum, record, struct) olamaz.** Çok dosya sorun değil.
- "Sadece burada kullanıyoruz" diye controller'ın/başka class'ın altına class eklenmez; her nesnenin kendi yeri ve dosyası vardır.

## Controller
- Controller'da iş YOK: veriyi al → valid mi kontrol et (FluentValidation, tek satır generic) → servise ver → sonucu direkt döndür.

## Summary
- Her class / interface / abstract / enum'un başında `/// <summary>`: nesnenin ne iş yaptığı, önemli metotları ve önemli alanları. Hem insan hem Claude için; büyük dosyayı okumadan summary'den anlaşılmalı.

## Genel kalite
- **TreatWarningsAsErrors=true** tüm projelerde (14 Kas 2026, kullanıcı isteği). Uyarı `NoWarn` ile bastırılmaz, kod düzeltilir; zorunlu istisna olursa gerekçesiyle yorum yazılır.
- SOLID, clean code, DRY: aynı mantıkta çalışan kod tek yerde toplanır (farklı tiplerle aynı işi yapan → generic metot).
- Loglama her uygulamada (MVC, API, Desktop): Information / Warning / Error / Critical. Yerel loglar 7 gün tutulur, günlük dosyalara ayrılır.
- Güvenlik: şifreler hash'li (geri dönüşsüz), kullanıcı API key'leri şifreli saklanır.
- Optimizasyon: kullanıcı API key'i girdi diye savurgan token kullanımı yok; minimum token / maksimum kalite. Output token limitlerine dikkat (1-2 dk video kodu için 32k yetmeyebilir, 64k gerekebilir).
- Test yazılır (1.0.0 için şart).

## Türkçe seslendirme (14 Kas 2026)
- TTS'e giden metin her zaman Normalize aşamasından geçer: yazılış (ekran/altyazı) ile okunuş (ses) ayrı tutulur. Yabancı kelimeler Türkçe okunuşla (Google → Gugıl), sayılar/tarihler/romen rakamları/kısaltmalar açık yazıyla okunur. Ayrıntı: PLAN.md §3.1.

## Önce GPU (14 Kas 2026 — kullanıcı: "ekran kartı ile yapılabilenleri orada yap, boşa işlemci ile vakit kaybetme; bu program için de geçerli")
- Ekran kartında çalışabilen her iş GPU'da çalışır: yerel ses (torch → CUDA tekerlekleri, ONNX → GPU sağlayıcısı), yerel görsel (sd.cpp CUDA), video kodlama (NVENC), tarayıcı tabanlı render (GPU hızlandırma açık).
- CPU yalnız yedek: GPU yoksa / VRAM yetmezse / sürücü hatasında otomatik düşer ve bu loglanır. CPU'ya düşüş sessiz olmaz.
- Testlerde ve geliştirme sırasında da aynı: GPU ile yapılabilecek denemeyi CPU'da koşturma.
