# Teknoloji Araştırması — Taha Konsept
Tarih: 2026-11-14 (14 Kasım 2026). Rapor no: 0001. Kaynak: 5 araştırmacı raporu (video motoru, TTS, görsel, LLM, mimari) ve eleştirmenin çapraz denetimi. İkisi çeliştiğinde **eleştirmenin düzeltmesi geçerli**.
Okuyucu notu (AI için): Önce "Kararlar" bölümünü oku. Fiyatlar ve sürümler 2026-11-14 tarihli; tekrar kullanmadan önce doğrula. "Doğrulanmadı" listesindeki her şey varsayım.

---

## 1. Kararlar ve öneriler (özet)

| # | Alan | Öneri | Gerekçe / not |
|---|---|---|---|
| 1 | Video motoru (varsayılan) | **HyperFrames** `hyperframes@0.9.43` sabitlenmiş | Apache-2.0, HTML+CSS+GSAP (LLM'e en uygun), Windows/PowerShell resmi, `--variables`, `--json`, `lint/check/doctor`. 0.x olduğu için sürüm site tarafından kontrollü güncellenir. Telemetri kurulumda kapatılır |
| 2 | Video motoru (destekli) | **Remotion** v4.0.543 | Eleştirmen düzeltmesi: SSS, kodu yapay zekânın yazıp servisin render ettiği modeli **açıkça izinli** sayıyor (gri alan değil). Yasak olan: kullanıcının kendi Remotion projesini yükletmesi. ≤3 kişilik ekip ücretsiz; aksi halde **Automators: $0.01/render, aylık min $100**. `npx remotion render` da otomasyon sayılır, yani son kullanıcı PC'sindeki render da faturalanabilir |
| 3 | Video motoru (deneysel) | **Revideo** (MIT, `midrender/revideo`) | v0.11.0 (2026-07-10), Temmuz'dan beri commit yok |
| 4 | Ortak son aşama | **ffmpeg + libass (ASS `\k` karaoke) + `drawbox` progress bar + remux** | Tüm motorlarda aynı. Motor sessiz ve altyazısız video üretir |
| 5 | Aşama sırası (çelişki çözümü) | `Script → Code → Image → TTS → Align → SceneRender → Captions(re-encode) → Mux → Preview` | Sahne süresi TTS'ten gelir. "Yalnızca TTS değişince yalnızca ses" hedefi için **sabit sahne slotu** (pad/atempo) seçeneği. TTS değişince karaoke de değiştiği için Captions aşaması yeniden çalışır; `-c:v copy` yalnızca altyazısız senaryoda geçerli |
| 6 | Yerel TTS (varsayılan) | **Supertonic 3** (ONNX Runtime, doğrudan C#, Python yok) | 31 dil, Türkçe WER lideri (1.72), CPU'da çalışır. Kod MIT, model OpenRAIL-M. **Repo 2026-09-09'da arşivlendi**: ağırlıkları kendi aynanızda sabitleyin, lisans metni ve kısıtları dağıtıma ekleyin |
| 7 | Yerel TTS (Türkçe, hafif) | **EMA Lightning** (Apache-2.0, 34 MB, CPU) | Sadece Türkçe, tek ses. WER %0.92 (kendi ölçümü), bağımsız %1.10. Model kartı sentetik ses bildirimi istiyor |
| 8 | Yerel TTS (klonlama) | **Chatterbox Multilingual V3** (MIT, ~3.5–4 GB VRAM, gömülü Python 3.11) | Ağustos 2026'dan beri bakım yok. Perth watermark. Dil sayısı 23 mü 25 mi çelişkili; Türkçe ikisinde de var |
| 9 | Online TTS | **ElevenLabs v4 Turbo** (timestamp'li) veya **Gemini 3.8 Flash TTS**; yedek **Azure Neural** (.NET SDK, WordBoundary) | OpenAI gpt-4o-mini-tts Türkçede zayıf (Dolphy 55.1/100). Proje tanımı OpenAI diyor: **ürün sahibine sorulacak**. OpenAI kalırsa sabit id `gpt-4o-mini-tts-2025-12-15` |
| 10 | Ortak kelime-zaman formatı | **Remotion Caption JSON** `{text,startMs,endMs,timestampMs,confidence}` | Tüm TTS/align çıktıları buna normalize edilir |
| 11 | Hizalama (align) | MVP: cümle bazlı sentez + orantılı dağıtım. Sonra: CTC forced alignment (WhisperX/torchaudio MMS_FA veya Whisper.net) | Türkçe align kalitesi test edilmedi |
| 12 | Yerel görsel | **Z-Image-Turbo GGUF Q4_0** (Apache-2.0) + **stable-diffusion.cpp** `sd-server.exe` (CUDA/Vulkan) | Çelişki çözümü: görselde **Python yok**, sd.cpp seçildi. Edit/referans için **FLUX.2 klein 4B** (Apache-2.0). 8 GB+ için FLUX.3 Klein yalnızca lisans metni okunduktan sonra |
| 13 | Online görsel | **`gemini-nano-banana-2.1`** (1K $0.0336); ucuz toplu **fal `fal-ai/z-image/turbo`** (~$0.01/1080×1920); yazılı görsel **`gpt-image-2`** | gpt-image-2 resmi sayfada token bazlı ($8 görüntü girdi / $30 görüntü çıktı / $5 metin, 1M token); görsel başı rakamlar yeniden kontrol edilecek. Görsele metin yazdırılmaz, Türkçe metin video katmanında |
| 14 | LLM | Konsept `claude-sonnet-5-5` (effort medium, structured output, web_search); video kodu `claude-opus-5-5` (effort high, streaming, max_tokens 64000, sahne başına); fikir/fact-sheet `claude-haiku-5-5` | Haiku 5.5'te prompt >100K token 5x pahalı: fact-sheet prompt'u 100K altında tutulur. **Claude Fable 5.2** ($8/$40) değerlendirilmedi, karmaşık kod için aday |
| 15 | LLM sağlayıcı katmanı | `AnthropicProvider` (resmi C# SDK) + `OpenAiCompatibleProviderBase` → OpenAI, OpenRouter, GLM, Kimi | OpenRouter `usage.cost` veriyor; diğerleri için sürümlü `ModelPrice` tablosu. Kimi K2.x kullanılmaz (emekliye ayrılıyor) |
| 16 | Maliyet sayacı | `ModelPrice` + `CallLog`, **LLM + TTS + görsel + Remotion render** hepsini kapsar | Eleştirmen eksik: rapor yalnızca LLM'i kapsıyordu |
| 17 | Web ↔ agent | **SignalR Hub** (API'de), agent dışarı WSS ile bağlanır | Chrome Local Network Access nedeniyle tarayıcı localhost'a gitmez |
| 18 | Video oynatma | Pi'ye **480p önizleme + poster**; tam kalite agent diskinde, istenirse kotalı chunk'lı upload | |
| 19 | Online TTS/görsel çağrı yeri (çelişki) | **Pi üzerinden (sunucu/proxy)** önerilir, anahtarlar Windows'a inmez | Pi bant genişliği bedeli; ürün sahibi onaylamalı |
| 20 | Auth | MVC cookie + BFF; API kendi JWT (15 dk) + rotating refresh (DB'de hash); agent için RFC 8628 cihaz kodu; permission policy; API anahtarları AES-256-GCM envelope; DP key'leri dosyaya persist; PBKDF2-SHA512 210k | |
| 21 | Installer | **Velopack** 1.2.161, packId `TahaKonsept.Agent`; ağır bağımlılıklar `%LocalAppData%\TahaKonsept\` | winget yok; portable Node 22 zip, ffmpeg, `uv` + Python **3.11**, chrome-headless-shell. **Ollama kurulum listesinden çıkarıldı** |
| 22 | Pi deploy | .NET 10 LTS (destek 2028-11-14), `aspnetcore-runtime-10.0` arm64 apt, framework-dependent, 2 systemd servis, Caddy (backports 2.11.x), Npgsql EF Core 10, modül başına şema, Serilog | .NET 8/9 desteği 2026-11-10'da bitti |
| 23 | Kod yapısı | Modular monolith: `Modules/{X}/{Domain,Application,Infrastructure,Api,Contracts}`, Outbox, ArchitectureTests, Admin = Web `Areas/Admin` + Chart.js | MediatR 13+ ticari; lisanssız alternatif önerilir |
| 24 | Güvenlik (yeni) | LLM kodu için sandbox: ağ kapalı, `file://` yok, CSP, izinli CDN listesi, `lint` kapısı | Eleştirmen eksiği; prompt injection riski |

---

## 2. Video motorları

| Seçenek | Lisans | Türkçe | Bağımlılıklar | Fiyat | Karar |
|---|---|---|---|---|---|
| HyperFrames v0.9.43 (2026-11-13) | Apache-2.0 | Metin: font ile tam. Yerleşik Kokoro TTS ve Parakeet transcribe Türkçe yok; whisper.cpp gerekli | Node 22+, FFmpeg/FFprobe, Chrome (`browser ensure`), ≥2 GB disk, worker başı ~256 MB RAM | Ücretsiz | **Varsayılan** |
| Remotion v4.0.543 (2026-11-11) | Remotion License | Font ile tam | Node, FFmpeg gömülü, Chrome Headless Shell, ~950 MB node_modules | ≤3 kişi ücretsiz; Automators $0.01/render min $100/ay; Creators $25/koltuk/ay; Enterprise min $500/ay | **Destekli** (lisans uyarısı UI'da) |
| Revideo v0.11.0 | MIT | Font ile | Node, FFmpeg, Chromium | Ücretsiz | Deneysel |
| Motion Canvas v3.17.2 | MIT | – | Headless render resmi değil | – | Hayır |
| Manim CE v0.22.1 | MIT | – | Python, FFmpeg | – | İleride "eğitim" stili |
| MoviePy v2.2.1 | MIT | – | Python, FFmpeg | – | Yalnızca slideshow fallback |
| editly | MIT | – | Node, headless-gl | – | Hayır (durgun) |
| Rendervid | FlowHunt Attribution (doğrulanmadı) | – | Node, Chrome | – | Lisans doğrulanınca |
| ffmpeg-only | LGPL/GPL | libass + font | ffmpeg.exe | – | **Altyazı/progress/mix/remux aşaması** |

Notlar:
- HyperFrames telemetrisi: `npx hyperframes telemetry disable`. Remotion: v5'te şirket lisansında telemetri zorunlu; tarayıcı render'ında (`renderMediaOnWeb`) şimdiden kapatılamıyor.
- HyperFrames local render için makinece okunabilir progress akışı belgelenmemiş.
- Hız katili: `filter: blur`, `backdrop-filter`, ağır gölge. Prompt'ta sınırla.
- Token tahmini (doğrulanmadı): HyperFrames 6k–18k, Remotion 10k–25k output token/video.
- Tek HTML + `--variables` mı, sahne başına ayrı kompozisyon mu: HyperFrames alt kompozisyon modeli araştırılmadı (açık).
- Progress bar: `drawbox=x=0:y=ih-12:w='iw*t/DUR':h=12:color=white@0.9:t=fill`.
- Remux: `ffmpeg -i captioned.mp4 -i mix.m4a -map 0:v -map 1:a -c:v copy -c:a aac -shortest final.mp4`.

## 3. TTS

### Yerel
| Model | Lisans (ticari?) | Türkçe | VRAM / bağımlılık | Timestamp | Karar |
|---|---|---|---|---|---|
| Supertonic 3 | Kod MIT, model OpenRAIL-M (evet, kısıtlı) | Var, WER 1.72 (1.) | ~99M, CPU; ONNX Runtime C# | Yok (toplam süre) | **Varsayılan**. Repo 2026-09-09 arşiv |
| EMA Lightning | Apache-2.0 (evet) | Sadece TR, en iyi açık model (Dolphy 70.8) | 8.6M, 34 MB, CPU ~6x RT; PyTorch | Yok | **TR alternatifi** |
| Chatterbox Multilingual V3 | MIT (evet), Perth watermark | Var | ~3.5–4 GB VRAM; Python 3.11 + torch CUDA | Yok | Klonlama için; bakım yok |
| Ahenk / Ahenk Cep | Ağırlık CC BY 4.0 (evet, atıf) | Sadece TR | Cep 8.6M CPU; Ahenk 2B GPU | Yok | İzle |
| VoxCPM2 | Apache-2.0 | Var (WER 2.50) | ~8 GB, 6 GB'a sığmaz | Yok | Hayır |
| OmniVoice | Belirsiz (CC-BY-NC olabilir) | Var | Hafif | Yok | Lisans netleşene kadar hayır |
| Trendyol-TTS | Belirsiz | Sadece TR, zayıf | ~2B GPU | Yok | Hayır |
| Fish S2 Pro | Research (hayır) | Var | ~12–24 GB | Yok | Hayır |
| XTTS-v2 | CPML (hayır) | Zayıf | 2–4 GB | Yok | Hayır |
| F5-TTS TR | CC-BY-NC (hayır) | Zayıf | ~3 GB | Yok | Hayır |
| Piper tr_TR | Ses CC BY-NC-SA (hayır) | Robotik | CPU | Yok | Hayır |
| MMS-TTS-tr | CC-BY-NC (hayır) | Orta | CPU | Yok | Hayır |
| Kokoro-82M | Apache-2.0 | Resmi yok | CPU | – | Hayır |
| Ollama Phonon | 31M Apache, 40M CC BY-NC | **Yok** | Yalnızca Apple Silicon (Win/Linux "yakında") | Yok | **Kullanılamaz** |

### Online
| Servis | Türkçe (Dolphy, Eki 2026) | Fiyat | Timestamp | Karar |
|---|---|---|---|---|
| Cartesia Sonic 3.6 | 92.9 (1.) | Doğrulanmadı | Doğrulanmadı | İzle |
| ElevenLabs v4 / v4 Turbo | 90.0 | $0.08 / $0.04 per 1K karakter (resmi sayfa okunamadı) | `/with-timestamps` (v4 uyumu doğrulanmadı) | **Online varsayılan adayı** |
| Gemini 3.8 Flash TTS (preview) | 89.8 | $0.50 girdi / $9 ses çıktı (1M token); **2027-01-01'den itibaren $1.00 / $18** | Yok | Ucuz aday; eski 2.5/3.1 TTS 2026-11-17'de kapanıyor |
| Azure Neural | Test edilmedi | ~$15–16 / 1M karakter, 0.5M/ay ücretsiz | WordBoundary | Yedek |
| OpenAI gpt-4o-mini-tts(-2025-12-15) | 55.1 | $0.60 metin / $12 ses (1M token), ~$0.015/dk (tahmin) | Yok | Varsayılan olmasın |

## 4. Görsel

### Yerel
| Model | Lisans | Türkçe (görselde yazı) | 6 GB durumu | Karar |
|---|---|---|---|---|
| Z-Image-Turbo (6B) | Apache-2.0 | Garanti değil (yazı yazdırma) | GGUF Q4_0 3.68 GB / Q3_K; `--offload-to-cpu --diffusion-fa --vae-tiling` | **Varsayılan** |
| FLUX.2 klein 4B | Apache-2.0 | – | GGUF Q4_K_M ~2.6 GB, 4 adım | Edit/referans |
| FLUX.2 klein 9B | Non-Commercial | – | Zor | Hayır |
| FLUX.3 Image Klein (2026-10-15) | FLUX Kommunity (<$5M gelir, **affiliate dahil**; bir kaynak gömme için her durumda lisans diyor) | – | Resmi min INT8 8 GB | Lisans okunmadan sunulmasın |
| Qwen-Image-2.1 | Research (hayır) | – | 12–16 GB | Hayır |
| SD3.5 Medium | Stability Community (<$1M) | – | Belki | Hayır |
| SDXL-Lightning | OpenRAIL++ (doğrulanmadı) | – | Rahat | ≤4 GB yedek |

Runtime: **stable-diffusion.cpp** (varsayılan, `sd-server` OpenAI uyumlu `/v1/images/generations`); StableDiffusion.NET (alternatif, in-process risk); ComfyUI (opsiyonel gelişmiş); diffusers/ONNX (hayır).
Hız (doğrulanmadı): Klein 4B Q4 RTX 3060 Ti'de ~15–30 sn; Z-Image Q3/Q4 offload ~20–60 sn. Üret 576×1024 / 768×1344, sonra upscale veya Ken Burns.

### Online
| Model | Fiyat | Karar |
|---|---|---|
| `gemini-nano-banana-2.1` | 1K $0.0336, 2K $0.0504, 4K $0.113, batch yarı | **Online varsayılan** |
| `gemini-3.1-flash-lite-image` | 1K $0.0336, batch $0.0168 | `gemini-2.5-flash-image`'in **resmi geçiş hedefi** (o model 2027-03-15'te kapanıyor) |
| `gemini-3-pro-image` | 1K/2K $0.134, 4K $0.24 | Kalite |
| `gpt-image-2` | Resmi: token bazlı ($8 görüntü girdi, $30 görüntü çıktı, $5 metin /1M). Görsel başı $0.006/$0.053/$0.211 tekrar kontrol | Görselde yazı |
| `gpt-image-1-mini` | ~$0.005–0.036 | Ucuz |
| fal `fal-ai/z-image/turbo` | $0.005/MP (~$0.01/1080×1920) | **En ucuz toplu** |
| fal `fal-ai/flux-2/flash` | $0.005/MP | Yedek |
| BFL `flux-3-image-klein` | "$0.009'dan başlayan" | İzle |
| Z.ai `glm-image` / `cogview-4` | $0.015 / $0.01 | GLM ekosistemi |
| Replicate | flux-schnell $0.003 vb. | Düşük öncelik |

Örnek maliyet (60 sn, 8 sahne): Nano Banana 2.1 ≈ $0.27; fal Z-Image ≈ $0.08; yerel $0.
Cache: `SHA256(provider, model, version, prompt, negative, w, h, seed, steps, guidance, refs)` → `image_assets`.

## 5. LLM

| Model | Fiyat (in/out, $/MTok) | Max output / context | Not |
|---|---|---|---|
| `claude-opus-5-5` | 4 / 20 (cache okuma 0.20, batch 2/10) | 128K / 1M | Thinking kapatılamaz; effort varsayılan medium; temperature/prefill/forced tool_choice 400 |
| `claude-sonnet-5-5` | 2 / 10 | 128K / 1M | Effort varsayılan high |
| `claude-haiku-5-5` | 0.10 / 0.50 (≤100K); 0.50 / 2.50 (>100K) | 128K / 1M | Prompt'u 100K altında tut |
| Claude Fable 5.2 | 8 / 40 | – | Değerlendirilmedi |
| `gpt-6.1-sol` | 2 / 10 | 128K / 1.05M | 272K üstü input 2x |
| `gpt-6-luna` | 0.10 / 0.50 | – | Ucuz |
| GLM-5.3 | 1.40 / 4.40 | 128K / 1M | reasoning_effort varsayılan max; açıkça gönder |
| Kimi K3 | 3 / 15 (3. taraf) | ~128K / 1M | K2.x 2026 sonunda emekli |
| OpenRouter | passthrough | modele bağlı | `usage.cost` yanıtta |

Taktikler: prompt caching (framework başına sabit önek, cache okuma 0.05x), TopicFingerprint ile tekrar önleme + `pg_trgm`, JSON schema structured output, iki aşama (plan → sahne başına kod), web_search ile fact-sheet (konsept başı ~$0.03–0.05), stage başına model/effort, Batch %50, render hatasında yalnızca log son ~50 satır.
Streaming: API BackgroundService stream tüketir, MVC jQuery ile `/jobs/{id}/progress` poll eder.

## 6. Mimari

| Konu | Seçenekler | Karar |
|---|---|---|
| Web↔agent | SignalR (sunucu aktarımı) / localhost doğrudan | SignalR |
| Tarayıcı canlı ilerleme | SignalR JS (WebSocket) / jQuery polling | Ürün sahibine sor |
| Video | Pi upload / agent stream / tünel / WebRTC | Hibrit: 480p önizleme Pi'de |
| API anahtarı şifreleme | Data Protection / AES-256-GCM envelope | AES-GCM (anahtarlar), DP (cookie) |
| Parola | PBKDF2 210k / Argon2id | PBKDF2 210k, Argon2id sonra |
| Installer | Velopack / MSIX / Inno | Velopack |
| Pi publish | framework-dependent / self-contained | framework-dependent |
| Reverse proxy | Caddy / nginx / Cloudflare Tunnel | Caddy (backports); CGNAT varsa Tunnel |
| Mediator | MediatR 13+ (ticari) / kendi / Mediator SG / Wolverine | Lisanssız |

Agent ön gereksinimleri: Node 22 zip (SHASUMS256), ffmpeg (GPL build lisans yükümlülükleri değerlendirilmeli), `uv` + Python 3.11 (python-build-standalone), motor başına ayrı venv (yalnızca TTS/align için), chrome-headless-shell, nvidia-smi ile GPU tespiti, VC++ Redist (tek seferlik UAC). Manifest `GET /api/agent/prereq-manifest`.

## 7. Eleştirmenin tespit ettiği eksikler (yapılacaklar)
1. LLM kodu sandbox politikası (ağ, file://, CSP, CDN listesi, lint kapısı, prompt injection).
2. FFmpeg GPL/LGPL dağıtım yükümlülükleri.
3. AI içerik şeffaflığı: EU AI Act md. 50 (2026-08-02'den beri), Perth/SynthID filigranları, "AI üretimi" etiketi.
4. Maliyet sayacına TTS, görsel, Remotion render eklenmesi.
5. Türkçe font (Noto/OFL) ve `text-transform: uppercase` i/İ hatası (`lang="tr"` ayarla).
6. Türkçe forced alignment testi.
7. AMD/Intel GPU için TTS yedeği (Supertonic CPU/DirectML).
8. Müzik ve ducking aşaması, telifsiz müzik kaynağı.
9. Bakımı biten modellerin ağırlık aynası (Pi mi CDN mi; OpenRAIL-M koşullu, MIT serbest).
10. Pi depolama: NVMe/SSD, Velopack paketleri, Cloudflare 100 MB limiti.
11. Claude Fable 5.2 değerlendirmesi.
12. TikTok/Reels/Shorts çıktı profilleri (kodek, bitrate, süre, güvenli alan).

## 8. Kaynaklar (seçilmiş)
- HyperFrames: https://github.com/heygen-com/hyperframes , https://hyperframes.heygen.com/faq.md , https://hyperframes.heygen.com/packages/cli.md , https://hyperframes.heygen.com/guides/rendering.md , https://hyperframes.heygen.com/guides/hyperframes-vs-remotion.md , https://api.github.com/repos/heygen-com/hyperframes/releases/latest
- Remotion: https://www.remotion.dev/docs/license/faq , https://www.remotion.dev/docs/license/pricing , https://remotion.dev/docs/5-0-migration , https://www.remotion.dev/docs/captions/create-tiktok-style-captions
- Revideo: https://github.com/redotvideo/revideo
- Ollama TTS: https://ollama.com/blog/speech-models , https://docs.ollama.com/capabilities/speech.md
- Supertonic: https://github.com/supertone-inc/supertonic , https://huggingface.co/Supertone/supertonic-3 , https://csukuangfj.github.io/sherpa/onnx/tts/supertonic.html
- Chatterbox: https://github.com/resemble-ai/chatterbox , https://www.resemble.ai/resources/chatterbox-multilingual-v3-tts-with-embedded-watermarking-for-25-languages , https://docs.nvidia.com/ace-for-games/chatterbox-tts/nvigi-chatterbox-model-card.html
- Türkçe TTS: https://paperswithcode.co/benchmark/open-tts-leaderboard-turkish , https://dolphydesk.com/blog/turkce-text-to-speech-karsilastirmasi , https://huggingface.co/canberkkkkkk/ema-lightning , https://huggingface.co/canberkkkkkk/ema-tts , https://github.com/ozguradmin/ahenk-turkish-tts
- Online TTS: https://elevenlabs.io/pricing/api , https://elevenlabs.io/docs/api-reference/text-to-speech/convert-with-timestamps , https://aistudio.google.com/docs/pricing , https://developers.openai.com/blog/updates-audio-models , https://azure.microsoft.com/en-us/pricing/details/speech/ , https://github.com/m-bain/whisperx
- Görsel: https://huggingface.co/Tongyi-MAI/Z-Image-Turbo , https://huggingface.co/leejet/Z-Image-Turbo-GGUF , https://bfl.ai/blog/flux2-klein-towards-interactive-visual-intelligence , https://huggingface.co/black-forest-labs/FLUX.2-klein-4B , https://bfl.ai/blog/flux-3-image-klein , https://github.com/leejet/stable-diffusion.cpp , https://github.com/leejet/stable-diffusion.cpp/pull/1037 , https://docs.comfy.org/development/comfyui-server/comms_routes , https://fal.ai/pricing , https://fal.ai/docs/model-apis/model-endpoints/queue , https://docs.z.ai/guides/overview/pricing , https://developers.openai.com/api/docs/guides/image-generation
- LLM: https://platform.claude.com/docs/en/about-claude/pricing.md , https://platform.claude.com/docs/en/about-claude/models/overview.md , https://platform.claude.com/docs/en/build-with-claude/prompt-caching.md , https://developers.openai.com/api/docs/pricing.md , https://developers.openai.com/api/docs/models/gpt-6.1-sol.md , https://openrouter.ai/docs/use-cases/usage-accounting , https://docs.z.ai/guides/llm/glm-5.3 , https://platform.kimi.ai/docs/pricing/chat
- Mimari: https://developer.chrome.com/blog/local-network-access , https://learn.microsoft.com/aspnet/core/signalr/dotnet-client , https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys , https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview , https://datatracker.ietf.org/doc/html/rfc8628 , https://docs.velopack.io/packaging/installer , https://avaloniaui.net/blog/avalonia-12 , https://docs.avaloniaui.net/controls/navigation/trayicon , https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian , https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core , https://tracker.debian.org/pkg/caddy , https://www.npgsql.org/efcore/release-notes/10.0.html

## 9. Doğrulanmayanlar
- HyperFrames: minimum Node (22+ README'den), local render progress akışı, gerçek render süreleri, token tahminleri, whisper Türkçe kelime zamanı kalitesi.
- Remotion: "$100'a 10.000 render dahil" (yalnızca 3. taraf); v5 çıkış tarihi ve Node 18 minimumu.
- Revideo: Midrender geliştirmelerinin açık repoya aktarımı. Rendervid lisansı.
- gyan.dev full build'in libass içerdiği.
- Supertone şirket durumu (tasfiye iddiası tek mirror'da). Supertonic RTF değerleri.
- Chatterbox VRAM (3.5 / 4 / 6–16 GB) ve dil sayısı (23/25).
- OmniVoice ağırlık lisansı. WhisperX Türkçe align modeli. EMA ONNX export.
- Cartesia fiyat/timestamp. ElevenLabs resmi fiyat ve `/with-timestamps` v4 uyumu. Azure fiyatı ve tr-TR ses isimleri. OpenAI TTS $/dk tahmini.
- Görsel 6 GB hızları; FLUX.3 Klein 6 GB ve lisans ayrıntısı; BFL fiyat yorumu; Gemini `/v1beta/interactions` vs `generateContent`; fal "per unit" tanımı; GLM-Image lisansı; Replicate fiyatları; StableDiffusion.NET Z-Image/FLUX desteği; SDXL-Lightning lisansı; gpt-image-2 görsel başı fiyatlar.
- Kimi K3 fiyat/özellikleri; GLM-5.3 tarih ve structured output formatı; OpenRouter ücretleri; SDK'ların linux-arm64'te çalışması (beklenir).
- Chrome 147+ WebSocket LNA; SignalR AccessTokenProvider reconnect davranışı; Avalonia 12 sürüm ayrıntısı; Velopack + Avalonia 12 + .NET 10; .NET 10 son patch; Npgsql 10.0.x son patch; NSec/Geralt arm64; MediatR lisansı ve FluentValidation 12; Cloudflare 100 MB; Pi'de hash süreleri.

## 10. Açık sorular (ürün sahibine)
1. Ekip/şirket kaç kişi? (≥4 ise Remotion Automators min $100/ay.) Remotion destekli listede kalsın mı?
2. Altyazı: ffmpeg/ASS ayrı aşama (varsayılan önerisi) mı, motorun animasyonlu caption'ı mı?
3. TTS süresi değişince sahne süresi de değişsin mi (yeniden render), yoksa sabit slot + pad/atempo mı?
4. Video kodu tek HTML (+`--variables`) mı, sahne başına ayrı çağrı/kompozisyon mu?
5. HyperFrames sürümü sabitlenip site üzerinden mi güncellensin?
6. Yerel TTS varsayılanı Supertonic 3 mü, EMA Lightning mi?
7. Ses klonlama ilk sürümde var mı? (Chatterbox için gömülü Python ~3–5 GB; Perth watermark kabul mü?)
8. Online TTS varsayılanı: proje tanımındaki OpenAI mı, ElevenLabs v4 Turbo mu, Gemini 3.8 Flash TTS mi, Azure mı?
9. Karaoke hizalaması ilk sürümde gerçek forced alignment mı, yoksa cümle bazlı yaklaşık zamanlama mı?
10. Ürün ticari mi? OpenRAIL-M kısıtları EULA'ya aktarılabilir mi?
11. Yerel görsel varsayılanı Z-Image-Turbo mu, FLUX.2 klein 4B mi? FLUX.3 Klein (Kommunity) 8 GB+ için sunulsun mu?
12. Online görsel varsayılanı Nano Banana 2.1 mi, fal Z-Image Turbo mu? Görsele metin tamamen yasak mı?
13. ComfyUI opsiyonel backend olsun mu? GPU'suz/≤4 GB kullanıcı online'a mı düşsün?
14. Görsel çözünürlüğü: doğrudan 1080×1920 mi, düşük üretip upscale mi?
15. Online TTS/görsel çağrıları Pi üzerinden mi (önerilen), agent'tan mı? API anahtarları BYOK mu, tek sistem anahtarı mı?
16. LLM model dağılımı (Sonnet/Opus/Haiku) onaylanıyor mu? Fable 5.2 denensin mi?
17. Bütçe limiti aşılınca engelle mi, uyar mı? Sağlayıcı bazında mı toplam mı?
18. İlk sürümde doğrudan entegre sağlayıcılar: Anthropic + OpenRouter yeterli mi?
19. Fact-grounding hangi kategorilerde zorunlu? allowed_domains kullanıcıca düzenlenebilir mi? Batch gece üretimi istenir mi?
20. Web logout agent'ı da düşürsün mü? ("Tüm cihazlardan çık" ayrı buton önerisi.)
21. Tarayıcıda SignalR JS client'a izin var mı, yoksa yalnızca jQuery polling mi?
22. Tam kalite video Pi'ye yüklensin mi? Kota/silme süresi? Pi'de NVMe/SSD var mı?
23. Site erişimi: port yönlendirme + alan adı + Caddy mi, Cloudflare Tunnel/Tailscale mi? CGNAT var mı?
24. Kod imzalama sertifikası alınacak mı? Velopack güncellemeleri Pi'den mi GitHub'dan mı?
25. Argon2id ve passkey ilk sürümde gerekli mi? Mediator olarak MediatR mi, lisanssız alternatif mi?
26. Tek seferlik UAC (VC++ Redist) kabul mü? Node/FFmpeg portable mı?
27. Manim ileride ayrı stil olarak eklensin mi?
28. Videolara "AI üretimi" etiketi politikası ne olsun? Arka plan müziği istenir mi?
