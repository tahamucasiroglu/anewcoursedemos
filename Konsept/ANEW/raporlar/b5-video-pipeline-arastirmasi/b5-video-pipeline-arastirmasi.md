# B-5 Video Pipeline Araştırması (ajan için yoğun özet)

> **DURUM: ÖNERİ, insan kararı bekliyor.** Buradaki hiçbir madde karar değildir. Onaylananlar ADR ve spec'e dönüşür (ör. spec 0006: VideoEngine / TTS / Image / Sandbox). Önerilerin dayanağı aşağıdaki kaynaklardır. **(doğrulanmadı)** işareti birincil kaynakta teyit edilemeyen iddiaları gösterir.
>
> Tarih: 2026-11-14. Kapsam: video motoru, yerel TTS, yerel görsel üretimi, çalışma zamanı stratejisi, H-4 sandbox. Geliştirici GPU: 6 GB VRAM, Windows 11. İçerik dili önce Türkçe.

## 0. Önerilen varsayılanlar (tek bakışta)

| Alan | Önerilen varsayılan | Yedek / opsiyonel | Bırak |
|---|---|---|---|
| Video motoru | **HyperFrames** (Apache-2.0), tam sürüm sabitleme (bugün 0.9.43) | Remotion (varsayılan kapalı, lisans uyarısı), Manim CE (faz 2, matematik klipleri) | Revideo, Motion Canvas, Editly, Diffusion Studio, kendi HTML+Puppeteer |
| Altyazı + ilerleme çubuğu | Ajan üretir, sabit şablon: ASS karaoke (`\k`/`\kf`, libass) + `overlay` ya da `drawtext` çubuğu (drawbox DEĞİL) | Motorun caption rayı | LLM'in yazdığı altyazı kodu |
| TTS (yerel) | **Supertonic 3** ONNX, .NET içinde (resmi C# örneği veya sherpa-onnx .NET) | Chatterbox Multilingual V3 (Python sidecar, klonlama) | XTTS-v2, F5-TTS TR, OmniVoice, MMS, Piper dfki (lisans) |
| TTS (online) | ElevenLabs (karakter zaman damgaları, ~$1.2 / 15 dk) | Google Chirp 3 HD, Gemini 3.8 Flash TTS (Türkçe ses doğrulanmadı) | OpenAI `gpt-4o-mini-tts`, `tts-1*` (2027-01-06 kapanıyor) |
| Kelime zamanlaması | Cümle/sahne başına sentez + Whisper.net transkript + senaryoyla eşleme (kendi kodumuz) | Karakter sayısına oranlı dağıtım | — |
| Görsel modeli | **Z-Image-Turbo GGUF** (Apache-2.0) | FLUX.2 [klein] 4B distilled (Apache-2.0, düzenleme) | Klein 9B/9B-Base (NCL, sert engel), SD 3.5M, Qwen-Image-2.1 |
| Görsel runtime | **stable-diffusion.cpp `sd-server`** (MIT), GPU üreticisine göre ayrı zip | ComfyUI adaptörü (GPL-3.0, ayrı süreç), bulut: fal Z-Image-Turbo $0.005/MP | Ollama (Windows yok), diffusers (üretimde) |
| Runtime stratejisi | ONNX işlem içi (.NET); PyTorch modelleri uv ile izole Python sidecar (localhost, OpenAI uyumlu uç); sd-server isteğe bağlı başlatılır; GPU işleri sıralı (TTS ile eşzamanlı değil) | — | Tek dev Python ortamı |
| Sandbox (H-4) | Katmanlı, çalışma zamanında admin yok: L0 sözleşme → L1 AST kapısı → L2 iş klasörü → L3 **AppContainer (spike şartlı)** → L4 Job Object → L5 Chromium ayarları → L6 Node seat belt → L7 çıktı kapısı | L3 yedeği: restricted token + Chromium sandbox; kurulumda opsiyonel firewall kuralı | Windows Sandbox (admin, Home yok, tek örnek) varsayılan olarak |

## 1. Video motoru

### Bulgular
| | HyperFrames | Remotion | Revideo | Motion Canvas | Manim CE | FFmpeg filtergraph |
|---|---|---|---|---|---|---|
| LLM ne yazar | HTML/CSS/JS + data attr; GSAP varsayılan ("paused-and-seekable by design") | React/TS | TS generator | TS generator | Python | filtre dizesi, ASS |
| Lisans | Apache-2.0; ticari kullanım serbest, yerel render ücretsiz ve hesap gerektirmez | Açık kaynak değil; birey, ≤3 çalışanlı şirket, kâr amacı gütmeyenler ücretsiz; diğerleri Company Licence | MIT | MIT | MIT | build'e göre LGPL/GPL (doğrulanmadı) |
| Son sürüm (2026-11-14) | 0.9.43 (2026-11-13) | 4.0.543 (2026-11-11) | 0.11.0 (2026-07-10) | 3.17.2 (2024-12-14) | 0.22.1 (2026-10-27), Python ≥3.11 | — |
| Olgunluk | 1.0 öncesi, günde birkaç sürüm | v4, birkaç günde bir | düzensiz | durmuş | aktif | çok olgun |
| Runtime | Node ≥22, FFmpeg, Chrome (@puppeteer/browsers) | Node ≥16 / Bun ≥1.0.3; Chrome Headless Shell node_modules içinde; platform başına native compositor | Node+tarayıcı | Node+tarayıcı | Python, PyAV (0.19'dan beri harici FFmpeg yok), LaTeX yalnız formül için (Windows'ta MiKTeX) | libass, libfreetype |
| Windows | Destekli, PowerShell'den render dahil. "On macOS and Windows, multi-worker capture is screenshot-based" | Windows x64 (ARM64 yok) | belirtilmemiş | belirtilmemiş | evet | evet |
| Determinizm | `time = floor(frame)/fps`; Date.now/rAF/timer/tohumsuz Math.random/render sırasında fetch yasak; makineler arası piksel eşitliği yok, `--docker` sabitler | `random(seed)` | seek | seek | deterministik | deterministik |
| Hız | Performans rehberi: aynı kompozisyonun iki yazımı 25.0 s / 9.8 s (1080p, 300 kare, 3 ölçüm medyanı). Optimizasyon kazancını gösterir, tipik hız değildir. `--workers` 1-24, varsayılan auto, worker başına ~256 MB | `--concurrency` | — | — | yavaş | en hızlı |
| LLM uyumu | llms.txt, 9 skill (6 çekirdek + 3 isteğe bağlı), `lint --json`, `check --json` | En çok eğitim verisi (nitel, doğrulanmadı), skills, llms.txt | az | orta | büyük korpus, sürümler arası API kayması (ManiBench) | kısa filtreler |
| Altyazı | Caption rayı, aktif kelime vurgusu; `transcribe` (Whisper / Parakeet) | `@remotion/captions` createTikTokStyleCaptions | elle | elle | elle | ASS `\k`/`\kf` |
| İlerleme çubuğu | CSS/GSAP | useCurrentFrame | kod | kod | kod | `overlay` (x ifadesi t ile) veya `drawtext`; **drawbox'ta `t` kalınlıktır** |
| Uzun render | Dağıtık `plan`/`renderChunk`/`assemble`; HLS yalnız yerel; yerel uzun render rehberi yok | concurrency, OffthreadVideo cache, SIGKILL rehberi | — | — | sahne başına | stream |

### HyperFrames notları
- Tasarım makalesi (HeyGen, 2026-06-22, James Russo): "Outputs got safer and more repetitive the more guardrails we added"; düz HTML/CSS/JS'e dönünce "the creativity came back".
- Çıktı: mp4, webm, mov, gif, png-sequence, hls. `--quality` draft / looks (CRF 16, varsayılan) / delivery(=high) / standard / high. `--crf` 0-51, `--fps` 1-240 veya rasyonel, `--gpu` NVENC/VideoToolbox/AMF/VAAPI/QSV.
- `lint --json` → errorCount, warningCount, infoCount, findings. `check --json` → lint, runtime, layout, motion, contrast, audio...; uyarılar yalnız `--strict` ile başarısız.
- Changelog: v0.9.41 Chrome 157+ hızlı yakalama düzeltmesi (Windows drawElementImage hatası); v0.9.43 Windows ClearType renk saçağı düzeltmesi. Sürüm sabitlemek şart.
- Telemetri varsayılan açık: komut adları, render performansı, checkpoint ve hata adları; giriş yapılana kadar anonim. Kapat: `npx hyperframes telemetry disable` veya `HYPERFRAMES_NO_TELEMETRY=1` (DO_NOT_TRACK da anılıyor). Ajan kurulumda kapatmalı.
- Dahili konuşma Türkçeyi kapsamıyor: Kokoro `--lang` en-us, en-gb, es, fr-fr, hi, it, pt-br, ja, zh. Parakeet TDT 0.6B v3 model kartı 25 Avrupa dili, Türkçe yok; HyperFrames'in hangi Parakeet'i paketlediği doğrulanmadı. Çok dilli Whisper (small/medium/large-v3) `--language` alır. ANEW kendi TTS kelime zamanlamalarını transcript import / özel caption şablonuyla vermeli.
- Hesap yalnızca HeyGen sesleri/müziği, avatar, bulut render, Studio için gerekli; yerel render için değil.
- Repo (2026-11-14): 84.0k yıldız, 7.8k fork, 7,135 commit. Kesin açık kaynak çıkış günü doğrulanmadı (HeyGen Nisan 2026 özeti gün vermiyor; üçüncü taraflar Nisan-Haziran).

### Remotion notları
- Fiyat: Creators $25/koltuk/ay; Automators "$0.01 per render, charged in increments of 1000 renders ($10), with a minimum of $100 per month"; Enterprise $500/ay'dan.
- FAQ: prompt-to-video araçları Automators'a girer; kullanıcının kendi Remotion kodunu yüklemesi yasak; istemci tarafı render'da kapatılamayan telemetri; Remotion 5.0'da Automators için sunucu tarafı telemetri zorunlu; LICENSE: "In Remotion 5.0, the license will slightly change."
- 4.0.540'tan itibaren Chrome 158.0.8127.2 sabit. Platformlar: macOS x64/arm64, Windows x64, Linux x64/arm64.
- ANEW için: tek kişi kendi PC'sinde ücretsiz lisansa uyar. 4+ kişilik şirket dağıtımı ADR + hukuki inceleme ister; müşteri PC'sinde render'a lisansın nasıl uygulandığı doğrulanmadı.
- Varsayılan `--concurrency` değeri yeniden kontrol edilmedi.

### Diğerleri
- Revideo: MIT, `midrender/revideo`, "the engine behind Midrender"; PostHog sayımı, `DISABLE_TELEMETRY=true`. Ekip odağının kaydığı iddiası yalnız üçüncü taraf. Bırak.
- Motion Canvas: son kararlı 3.17.2 (2024-12-14), son kayıt 3.18.0-alpha.0 (2025-02-16). Durmuş. Bırak.
- Manim CE: ManiBench (arXiv 2603.13251) API kayması; PRISM (arXiv 2605.19382) değerlendirme "should go beyond executability", "runnable code does not necessarily yield spatially coherent visual output" (Manim'e özgü olduğu özette görünmüyor); TheoremExplainAgent (2502.19400) yerleşim sorunları. Altyazı yok; ayrı klip olarak render. Faz 2.
- Editly (0.14.2, 2022), Diffusion Studio core (MPL-2.0, 4.0.3, derin değerlendirilmedi), Twick (değerlendirilmedi): bırak.
- Düz HTML + Puppeteer: yapma; HyperFrames bunu Apache-2.0 ile zaten veriyor.

### FFmpeg son aşama
- ASS: `\k` dolguyu anında değiştirir; `\K`/`\kf` soldan sağa süpürür. `subtitles` veya `ass` filtresiyle yak (libass, `--enable-libass`).
- İlerleme çubuğu: `overlay` (t = saniye cinsinden zaman damgası; renk kaynağını x=-W'den kaydır) veya `drawtext`. drawbox ifadelerinde zaman değişkeni yok.
- Sahne başına klip + concat demuxer, aynı codec ayarlarıyla stream-copy.

## 2. TTS

### 2026 değişiklikleri
1. **Supertonic arşivlendi** (2026-09-09, "Development and support have ended"). Supertonic 3: 2026-04-29, 31 dil (Türkçe dahil), ~99M param, ONNX, resmi C# örneği (.NET 9). Voice Builder 2026-08-31'den sonra erişilemez. Ağırlıklar HF `supertone-oss-archive/supertonic-3`. "2026-07-23 duyurusu" tarihi ve şirketin dağıldığı iddiası doğrulanmadı (yalnızca yeniden yapılanma işareti; destek makalesi 403).
   - **sherpa-onnx** Supertonic 3'ü destekliyor (int8 arşiv 2026-05-11, PR #3605) ve .NET örneği var (`dotnet-examples/supertonic-tts`): bakımı süren C# yolu.
2. **Chatterbox emekli** (Ağustos 2026). MIT kalır. Multilingual V3: 0.5B, GitHub'a göre 23 dil (Türkçe dahil); üretici sayfası "20+" / 21.
3. **Ollama TTS** (2026-10-24, 0.40.11+): yalnız macOS Apple Silicon, Türkçe yok. Şimdilik kullanılamaz.

### Bulgular
| Model | Türkçe | Lisans | .NET (Python'suz) | VRAM / hız | Klonlama | Kelime zamanı |
|---|---|---|---|---|---|---|
| Supertonic 3 | Evet; üretici tablosunda WER 1.00 (VoxCPM2 0.88) | Örnek kod MIT; ağırlık BigScience Open RAIL-M (ticari serbest, Attachment A kısıtları downstream'e aktarılmalı §4a) | Evet (resmi C# + sherpa-onnx) | CPU öncelikli; tek yayınlanmış sayı e-okuyucuda RTF 0.3 | Hayır | Hayır |
| Chatterbox Multilingual V3 | Evet | MIT, PerTh filigranı | Yalnız topluluk ONNX (diakritik sorunu raporları, yeniden okunmadı) | ~4-4.5 GB (doğrulanmadı) | ≥10 s referansla | Hayır |
| Chatterbox Turbo/Nano | Hayır (EN) | MIT | Turbo ONNX (ResembleAI org) | Nano 110M, 8 çekirdekte "3x realtime" (üretici) | Evet | Hayır |
| VoxCPM2 | Evet; kendi benchmark'ında WER %1.65 | Apache-2.0 | Topluluk ONNX | **~8 GB** (6 GB'tan fazla); RTX 4090 RTF 0.30 (Nano-vLLM ile 0.13); Q8_0 GGUF M4 Pro'da 1.76 | Evet | stable-ts ile sonradan |
| Trendyol-TTS | Yalnız TR; TR-Zor UTMOS 3.82 (en iyi), held-out 3.73, WER 3.98 | MIT etiketi, ama VoxCPM2 ve veri seti lisansı da kontrol; "research and prototyping" | Hayır | VRAM belirtilmemiş; A100 RTF 1.37 | belirtilmemiş | — |
| Ahenk / Ahenk Cep | Yalnız TR; Ahenk TR-Zor WER 2.54 (en iyi) | Ağırlık CC BY 4.0 (atıf şart); kod, trnorm Apache-2.0 | Cep: belirtilmemiş | Ahenk A100 RTF 2.28; **Cep 8.6M, CPU RTF ~0.2** | — | Hayır |
| Kokoro-82M | **Hayır** | Apache-2.0 | sherpa-onnx paketleri yalnız EN+ZH | çok hafif | Hayır | Hayır |
| Lisans nedeniyle elenen | XTTS-v2 (CPML, Coqui kapandı), F5-TTS TR (CC BY-NC), OmniVoice ağırlıkları (CC BY-NC; kod Apache-2.0; Türkçe adı geçmiyor), MMS tur (CC BY-NC), Piper tr dfki (CC BY-NC-SA veri, kişisel/araştırma) | | | | | |
| Türkçe yok | Qwen3-TTS, MeloTTS, Orpheus (bu turda yeniden çekilmedi), Ollama Phonon | | | | | |

### Online
| Servis | Fiyat | Türkçe | Zaman damgası | ~15 dk (≈15k karakter varsayımı, doğrulanmadı) |
|---|---|---|---|---|
| OpenAI gpt-4o-mini-tts / tts-1 / tts-1-hd | $0.60+$12/1M token; $15/$30 per 1M kar. **2027-01-06 kapanış**; yerine gelen yalnız Realtime API | Evet | Belgelenmemiş | ~$0.2 / $0.23 / $0.45 |
| Google Chirp 3 HD | $30/1M kar.; aylık 1M ücretsiz (doğrulanmadı) | Doğrulanmadı | — | $0.45 |
| Gemini 3.8 Flash TTS (Preview) | $0.50+$9/1M (2026-12-31'e kadar), sonra $1+$18; 25 ses token/s | Doğrulanmadı | — | ~$0.20 → ~$0.41 |
| ElevenLabs | $0.08/1K kar.; Flash/Turbo $0.04/1K | Evet (multilingual_v2, v3, v4, flash_v2_5, v4_turbo) | Evet: `/with-timestamps` karakter hizalaması, kelimeye biz grupluyoruz | $1.20 ($0.60) |
| Azure Neural | F0: ayda 0.5M kar. ücretsiz (resmi); PAYG ~$15-16/1M (doğrulanmadı) | Evet (tr-TR-EmelNeural, üçüncü taraf) | WordBoundary tr-TR doğrulanmadı | ~$0.23 |

### Öneri
- Yerel varsayılan: Supertonic 3, ONNX, .NET Agent içinde. Arşiv riski: revizyonu sabitle, hash kaydet, ONNX dosyalarını aynala. int8 sherpa yapısının Türkçe kalitesi fp32 ile karşılaştırılmalı (doğrulanmadı).
- **OpenRAIL-M yükümlülüğü:** Attachment A makine üretimi içeriği açıkça belirtmeden yayımlamayı yasaklıyor → ürün ve dışa aktarma akışında varsayılan "Yapay zekâ ile üretilmiş ses" beyanı; kullanım kısıtları dağıtım koşullarımıza girmeli.
- İkinci yerel: Chatterbox Multilingual V3, izole Python sidecar (GPU, klonlama).
- Seçici etiketleri: "Yalnız Türkçe, ≥8 GB, deneysel" (Trendyol-TTS/Ahenk), "Yalnız Türkçe, CPU, atıf gerekli" (Ahenk Cep), "Yalnız İngilizce" (Kokoro, Chatterbox Turbo).
- Normalize: trnorm (Apache-2.0) veya JS portu `@ozgurguler/trnorm` (Python ile 742,716 kontrolde eşleşiyor).
- Tüm motorlar tek `ITtsEngine` arkasında: metin+ses → WAV+zamanlamalar.
- Kelime zamanı: cümle/sahne başına sentez → Whisper.net (MIT; CPU/CUDA 12/13/Vulkan/OpenVINO; token zaman damgası; **forced alignment yapmaz**) → senaryoyla eşleme (kendi kodumuz) → başarısızsa karakter oranlı dağıtım. VoxCPM için stable-ts.
- Uzun ses: normalize → cümlelere böl → sentez → hizala → FFmpeg ile birleştir → loudness normalize. Cümle bazlı önbellek: revizyonda yalnız değişen cümleler yeniden üretilir.
- Runtime: ONNX işlem içi (`Microsoft.ML.OnnxRuntime` veya sherpa-onnx). DirectML sustained engineering'de, yeni iş WinML'e taşındı → GPU planı WinML veya CUDA. PyTorch modelleri uv ile (python-build-standalone otomatik indirilir) localhost sidecar `/v1/audio/speech`.

## 3. Görsel üretimi

### Bulgular
| Model | Param / adım | Lisans | 6 GB uyumu | 6 GB süre | Metin |
|---|---|---|---|---|---|
| Z-Image-Turbo | 6B DiT, 8 NFE (diffusers `num_inference_steps=9`); guidance 0 (diffusers), sd.cpp'de `--cfg-scale 1.0` | Apache-2.0 | Kart: "fits comfortably within 16G VRAM"; bf16 transformer ~12 GB (hesap). Resmi sd.cpp **4 GB** tarifi: Q3_K/Q4_0 GGUF + Qwen3-4B-Instruct-2507 Q4_K_M (`--llm`) + `--offload-to-cpu --diffusion-fa` | 15-70 s / 1024², bir RTX 2060 raporu 34 s (doğrulanmadı) | EN/ZH; Türkçe test edilmemiş |
| FLUX.2 [klein] 4B | 4B, distilled 4 adım; 4B-Base distilled değil | Apache-2.0 (4B, 4B-Base); **9B/9B-Base FLUX NCL** | "fits in ~13GB VRAM"; FP8 %40, NVFP4 %55 daha az VRAM (BFL blog, RTX 5080/5090); GGUF Q4_K_M ~2.6 GB (doğrulanmadı) | ComfyUI RTX 5090: distilled ~1.2 s / 8.4 GB, Base ~17 s / 9.2 GB; 6 GB doğrulanmadı | "may be inaccurate or subject to distortion" |
| FLUX 3 Image Klein (sd.cpp: FLUX.3 Image Klein) | 5.6B; distilled 16 adım, CFG yok, negatif prompt reddedilir; Base 50 adım | FLUX Kommunity: yıllık gelir <$5M ücretsiz (blog); repo gated; tam metin okunmadı | BF16 ~11.2 GB, FP8 ~5.6 GB, INT8 "as little as 8 GB"; **6 GB resmi değil**; encoder Qwen3-VL-4B-Instruct | RTX 4070 8.9 s, RTX 5090 ~4 s (INT8, 1024², 16 adım) | belirtilmemiş |
| SDXL-Lightning | 1/2/4/8 adım (1 deneysel) | OpenRAIL++ | fp16 + VAE tiling ile sığar (doğrulanmadı) | 3-8 s (doğrulanmadı) | zayıf |
| SD 3.5 Medium | 2B MMDiT-X, CLIP-G/L + T5-xxl | Stability Community (<$1M), gated | NF4 + cpu offload | doğrulanmadı | "improved typography" iddiası |
| Qwen-Image-2.1 | 7B, RGBA | Research/non-commercial (yalnız ikincil kaynak) | büyük | — | güçlü |

- Hepsi ~1 MP yerel model: 1344×768 / 768×1344 üret, sonra büyüt (`sd-server --hires-upscalers-dir` ESRGAN/LTX, ya da FFmpeg). Doğrudan 1920×1088 piksel sayısını ~2 katına çıkarır (tahmin). Klein 1080×1080 yavaşlama iddiası resmi ComfyUI rehberinde yok (doğrulanmadı).

### Runtime
- **stable-diffusion.cpp** (MIT): CPU (AVX/AVX2/AVX512), CUDA, Vulkan, Metal, OpenCL, SYCL; Windows ROCm da var. Son sürüm `master-1094-3bad892` (2026-11-13). **Tek binary yok**: `win-cuda12-x64` (+`cudart-sd-bin-win-cu12-x64`), `win-vulkan-x64`, `win-rocm-10.1.0-x64`, `win-cpu-x64` ayrı zip. P/Invoke için DLL olup olmadığı doğrulanmadı.
  - sd-server uçları: OpenAI uyumlu `POST /v1/images/generations`, `/v1/images/edits`, `GET /v1/models`; A1111 `/sdapi/v1/txt2img|img2img|progress`; native `/sdcpp/v1/img_gen`, `vid_gen`, `upscale`, `jobs/{id}`, `jobs/{id}/cancel`, `models/load|unload|status`, `capabilities`. Bayraklar: `--max-pending-jobs` (64), `--completed-ttl-seconds` (600), `--listen-ip/--listen-port`, `--model-dir`, `--offload-to-cpu`.
- ComfyUI: GPL-3.0, ayrı süreç olarak uygun; `/prompt`, `/ws`, `/history`, `/queue`; ağır kurulum, Klein için nightly gerekebilir → "gelişmiş / kendi kurulumun" adaptörü.
- diffusers: GGUF yalnız model sınıflarında `from_single_file`, her forward'da dequantize; paketlemesi en ağır → yalnız deney.
- Ollama: görsel üretim 2026-01-20 deneysel, yalnız macOS, API belgesiz; Windows kanıtı yok.

### Online
| Servis | Fiyat |
|---|---|
| fal Z-Image-Turbo | $0.005/MP (~$0.0104 / 1920×1080); prompt expansion +$0.0025 |
| fal FLUX.2 klein 4B / 4B Base / 9B | $0.005 / $0.009 / $0.006 per MP (9B ağırlıkları NC; fal çıktı koşulları doğrulanmadı) |
| BFL FLUX 3 Image Klein | $0.009 / 1K görselden başlıyor; fal Flux 3 Image **edit** ucu 2026-10-22'ye kadar $0.024→$0.048/MP (ikincil) |
| Google gemini-3.1-flash-lite-image (Nano Banana 2 Lite, 2026-06-30) | $0.0336 / 1K, batch $0.0168, yalnız 1K |
| Google gemini-3.1-flash-image | 0.5K $0.045, 1K $0.067, 2K $0.101, 4K $0.151 |
| Google gemini-3-pro-image | 1K/2K $0.134, 4K $0.24 |
| Google gemini-2.5-flash-image | $0.039; **2027-03-15 kapanış** |
| OpenAI gpt-image-2 | 1024²: $0.006/$0.053/$0.211; 1536×1024: $0.005/$0.041/$0.165 |
| OpenAI gpt-image-1.5 / 1-mini | 1024²: $0.009/$0.034/$0.133 ; $0.005/$0.011/$0.036 |
| OpenAI gpt-image-2.5/2.6 | $30/1M görsel çıktı tokenı (batch $15); gpt-image-1 2026-10-23'te kapandı |

60 görsel (~1 MP): fal Z-Image ~$0.31 (1080p ~$0.62); Nano Banana 2 Lite ~$2.02 (batch $1.01); gpt-image-2 low ~$0.30-0.36.

### Öneri
1. Varsayılan runtime sd-server, GPU üreticisine göre doğru zip, localhost'ta isteğe bağlı başlat; tek iş; TTS ile eşzamanlı değil; boşta `models/unload`. `IImageGenerator` portu: SdCpp, ComfyUI, Cloud adaptörleri.
2. Varsayılan model Z-Image-Turbo GGUF (Q4_K_M; 4-6 GB'ta Q3_K/Q4_0), Qwen3-4B Q4_K_M encoder, 8 adım.
3. İkinci: FLUX.2 klein 4B distilled. 9B ve 9B-Base sert engelli.
4. Yalnız opt-in: FLUX 3 Image Klein (lisans onayı, ≥8 GB), SDXL-Lightning.
5. Görselde metin çizdirme yok; Türkçe metin video kodunda overlay.
6. Bulut yedeği fal Z-Image-Turbo; premium: Nano Banana 2 Lite batch veya gpt-image-2 low/medium.
7. İlk çalıştırmada VRAM + üretici tespiti ve benchmark.

## 4. H-4 Sandbox

### Neyi kapsıyoruz
LLM kodu her iki motorda da headless Chromium içinde çalışır; Node yalnız orkestratör. Saldırı yüzeyleri: (1) Chromium renderer (fetch, WebSocket, WebRTC, file://, yerel dosya sunucusu), (2) Node orkestratör (Remotion uzak medyayı indirir: "needs to download it in order to use it's audio in the output file"), (3) alt süreçler (Chrome, FFmpeg, compositor; Node izin modeli kapsamaz), (4) tedarik zinciri (`npm install` scriptleri).

- **Remotion Chromium sandbox'ını varsayılan kapatır:** `--no-sandbox`, `--disable-setuid-sandbox`, `--no-zygote`; `disable-features` içinde IsolateOrigins, site-per-process, LocalNetworkAccessChecks, BlockInsecurePrivateNetworkRequests, PrivateNetworkAccessSendPreflights, PrivateNetworkAccessRespectPreflightResults; `--no-proxy-server`, `--proxy-server=direct://`, `--proxy-bypass-list=*`; `--remote-debugging-port=0`; profil `os.tmpdir()` altında `puppeteer_dev_chrome_profile-`. Flag bazlı kaldırma (`false`) yalnız **v4.0.543+**; `user-data-dir` ve `remote-debugging-*` değiştirilemez (CDP loopback kaçınılmaz).
- **HyperFrames Windows'ta sandbox açık** (`HYPERFRAMES_CHROME_SANDBOX=0` değilse). Linux'ta root, Lambda/Cloud Run/Kubernetes, container işaretleri, userns kısıtı, başlatma hatasında yeniden deneme ile kapanır. Dosya sunucusu 127.0.0.1, sözcüksel kapsama kontrolü, symlink bilerek çözülmez ve `openSync` symlink izler.

### Seçenekler
| # | Seçenek | FS okuma | FS yazma | Ağ | Admin | Güç | Karar |
|---|---|---|---|---|---|---|---|
| A | Node `--permission` | yalnız Node JS | yalnız Node JS | `--allow-net` v25.0.0+ (1.1) | Hayır | Düşük ("seat belt") | Orkestratör kemeri |
| B | Chromium kendi sandbox'ı | Evet | Evet | Low Box token ağı keser | Hayır | Yüksek | **Çekirdek katman**; Remotion'da yeniden aç |
| C | Chromium ağ bayrakları | – | – | URL evet, IP literal proxy kuralıyla, WebRTC ayrı | Hayır | Orta | Kullan |
| D | CDP `Fetch.enable` | file:// reddedebilir | – | HTTP(S); worker/WebSocket boşlukları (doğrulanmadı) | Hayır | Orta-düşük | Opsiyonel denetim |
| E | **AppContainer** | Evet (varsayılan red, ACL) | Evet | Evet (capability yok) | Profil/başlatma için belirtilmemiş | Yüksek (kernel) | **En güçlü admin'siz; önce spike** |
| F | Low IL / restricted token | Hayır | Evet | Hayır | Hayır | Düşük-orta | Yalnız yedek |
| G | Job Object | – | – | – | Hayır | Kaynak kontrolü | **Her zaman** |
| H | Windows Sandbox | Evet | Evet | Kapatılabilir | Evet (özellik), Home yok, tek örnek; `wsb` CLI 24H2+ | Çok yüksek | "Paranoyak mod" adayı |
| I | Firewall program kuralı | – | – | Evet | **Evet** (kurulumda bir kez) | Yüksek (ağ) | Opsiyonel güçlü mod |
| J | Statik AST allowlist | dolaylı | dolaylı | dolaylı | Hayır | Sınır değil; erken red | **İlk kapı** |
| K | `Experimental_CreateProcessInSandbox` | Evet | Evet | Evet | Listelenmemiş | Yüksek ama deneysel | İzle, bağımlı olma |

Önemli notlar:
- Node: `--allow-fs-*` v23.5.0/v22.13.0'dan beri stabil; Node 24 LTS'te ağ kısıtı yok, Node 26 LTS'te `--allow-net` olmadan kısıtlı. `--allow-child-process` zorunlu olduğundan Chrome/FFmpeg kısıtlanmaz. Kısıtlar: mevcut fd'ler, `process._debugProcess`, Worker execArgv, `--env-file`/`--openssl-config`/`v8.setFlagsFromString`; symlinkler izin dışına çıkar. `--permission-audit` sürümü doğrulanmadı. CVE-2026-21636 yalnız 25.x (25.3.0'da düzeldi), 24/26'yı etkilemez. Node sürümleri: v26.13.1 Lithium LTS, v24.22.3 Krypton LTS, v22.23.5 Jod LTS, v25 EOL.
- Chromium: renderer kendi Job'ında, tek aktif süreç; Win10 TH2+ çocuk süreç yasağı; Low Box token capability'siz, RS2+ LPAC → bizim AppContainer içinde çalıştırmak iç içe geçme demek.
- Ağ bayrakları: dead proxy `--proxy-server=http://127.0.0.1:1` (localhost, 127/8, ::1, link-local örtük bypass); `--host-resolver-rules="MAP * ^NOTFOUND, EXCLUDE localhost"` (IP literal'i durdurmaz; 2023 davranış değişikliği raporu doğrulanmadı); `--force-webrtc-ip-handling-policy` belgesiz → **RTCPeerConnection statik red birincil kontrol**.
- AppContainer loopback: hh780593 (eski WinRT belgesi) muafiyetle bile loopback yalnız istemci hedefi olabilir, dinleyici gelen paket almaz → Node HTTP sunucusu konteyner içindeyse ciddi risk; Win32'ye uygulanıp uygulanmadığı doğrulanmadı. Muafiyetin admin gerektirdiğini yalnız üçüncü taraf Sandy söylüyor.
- OpenAI Codex emsali (yalnız PyTorchKR özeti; orijinal 403): AppContainer'ı açık uçlu iş akışı nedeniyle reddetti; yükseltilmiş kurulum, iki yerel kullanıcı, firewall, write-restricted token, DPAPI. Bizim iş yükümüz dar ve sabit → AppContainer'a uygun.
- Acornima 1.9.1 (2026-10-25, BSD-3-Clause), JSX `Acornima.Extras`. Meziantou.Framework.Win32.Jobs 4.0.9 (2026-11-13, MIT; limit API yüzeyi yeniden kontrol edilmedi).
- HyperFrames README örneği GSAP'ı CDN'den yüklüyor → ajan GSAP'ı yerelde vendor etmeli, CDN URL'lerini reddetmeli.

### Önerilen katmanlar (çalışma zamanında admin yok)
| Katman | Ne |
|---|---|
| L0 Sözleşme | LLM yalnız sabit, ajana ait şablona karşı kompozisyon kaynağı yazar. package.json/config değişikliği yok, iş başına `npm install` yok. Varlıklar (TTS, görsel, font, GSAP) ajan tarafından `job\public\` altına konur |
| L1 Statik kapı | AST allowlist: Pi'de göndermeden önce ve ajanda render öncesi. Yasak: dinamik import, require, eval, Function, Worker, fetch, XHR, WebSocket, EventSource, sendBeacon, RTCPeerConnection, importScripts, process, window/globalThis computed erişim, http(s)/file/data script; boyut/karmaşıklık sınırı. Hata LLM'e geri döner |
| L2 İş klasörü | `%LOCALAPPDATA%\Konsept\jobs\<id>\`; TEMP/TMP/HOME içeride; şablon node_modules salt okunur; ağaçta sır yok (refresh token DPAPI ile dışarıda); symlink reddi |
| L3 OS konteyner | Tercih: Node orkestratör sıfır capability AppContainer'da, iş klasörü RW, runtime RX ACE. Yedek (spike başarısızsa): restricted token + Chromium sandbox + L5 |
| L4 Job Object | kill-on-close, ~64 süreç, bellek sınırı, .NET duvar saati zaman aşımı, UI kısıtları |
| L5 Chromium | Remotion ≥4.0.543, sandbox açık, dead proxy, host-resolver-rules, PNA/LNA girdileri disable-features'tan çıkar, WebRTC bayrağı best effort, disableWebSecurity asla, sabit chrome-headless-shell |
| L6 Node kemeri | Node 26 LTS: `--permission --allow-fs-read=<job>,<runtime> --allow-fs-write=<job> --allow-child-process`, `--allow-net` yok |
| L7 Çıktı kapısı | ffprobe (codec, süre, boyut), beklenen dosya adları, çıktı asla çalıştırılmaz |
| Opsiyonel | Kurulumda admin ile firewall outbound blok (güçlü mod) |

### Spike'lar (karar öncesi)
1. Tek AppContainer içinde loopback: Node 127.0.0.1'de dinleyip aynı konteynerdeki Chromium'dan (CDP dahil) bağlantı kabul edebiliyor mu, muafiyet ve admin olmadan?
2. Chromium AppContainer içinde: kendi sandbox'ı iç içe başlıyor mu, yoksa yalnız `--no-sandbox` ile mi? `gl=angle` (D3D11) çalışıyor mu? Render süresi? Çocuklar konteyneri miras alıyor mu?
3. Remotion ≥4.0.543 `no-sandbox: false` ile Windows'ta aynı hızda render ediyor mu?
4. Node 26 `--permission` (`--allow-net` yok) fetch/undici ve Remotion indirme yolunu engelliyor mu?
5. Remotion OffthreadVideo `file:///`, mutlak yol veya yerel proxy ile `public/` dışına ulaşıyor mu?

### Kasıtlı bozma testleri (H-4 kabul)
T1 fetch/IP literal/Image src → başarısız, honeypot isabet yok · T2 WebSocket, RTCPeerConnection STUN → UDP çıkışı yok · T3 file:// fetch/img/iframe → engel, canary karede yok · T4 path traversal, `%2e%2e%2f`, public içinde symlink → 404/red (symlink HyperFrames'te bilinen boşluk, L2 reddetmeli) · T5 OffthreadVideo `file:///C:/Windows/win.ini` → L1 red, zorlanırsa L3 · T6 `window["fe"+"tch"]`, `import("https://…")`, eval → L1 red · T7 uzak https ses (Remotion) → render başarısız · T8 `%USERPROFILE%\x.txt` yazma → oluşmaz · T9 sonsuz döngü, 10 GB, 500 iframe → Job ağacı öldürür, yetim Chrome yok · T10 ajan token'ı ve %APPDATA% tarayıcı profili okuma → red · T11 zararsız şablon → başarılı, ffprobe geçer (aşırı sıkılaştırma koruması). Her test katmanı kaldırılınca kırmızıya dönmeli; CI'da admin olmayan Windows runner'da, her runtime yükseltmesinde yeniden.

## 5. Karar listesi (her biri öneriyle)

| # | Karar | Öneri | Gerekçe |
|---|---|---|---|
| K-1 | Varsayılan video motoru | HyperFrames, tam sürüm sabit (0.9.43), telemetri kurulumda kapalı | Apache-2.0, ücretsiz yerel render, ajan odaklı, Windows destekli, lint/check döngüsü, sandbox açık |
| K-2 | Remotion'un rolü | Opsiyonel, varsayılan kapalı, UI'da lisans uyarısı, ticari dağıtım öncesi ADR + hukuk | Lisans/telemetri/kullanıcı kodu yasağı; sandbox'ı kapatıyor |
| K-3 | Manim | Faz 2, yalnız matematik/diyagram klipleri | API kayması, altyazı yok |
| K-4 | Altyazı ve ilerleme çubuğu kimde | Ajan, sabit şablon: ASS karaoke + overlay/drawtext çubuk | LLM token'ı harcamaz, motor bağımsız, deterministik |
| K-5 | Render birimi | Sahne başına klip + concat demuxer stream-copy; 1080×1920 ve 1920×1080 @30 fps preset | 15 dk videolar, revizyonda kısmi yeniden render |
| K-6 | Yerel TTS varsayılanı | Supertonic 3 ONNX (.NET), ağırlıklar aynalanır | Türkçe + 30 dil, Python'suz, CPU |
| K-7 | AI ses beyanı | Varsayılan açık "Yapay zekâ ile üretilmiş ses" beyanı + dağıtım koşullarına OpenRAIL-M kısıtları | Lisans §4a ve Attachment A |
| K-8 | İkinci TTS / klonlama | Chatterbox Multilingual V3, uv sidecar | MIT, Türkçe, klonlama |
| K-9 | Online TTS varsayılanı | ElevenLabs; OpenAI TTS'e yeni iş yok | Türkçe + zaman damgası; OpenAI 2027-01-06 kapanış |
| K-10 | Kelime zamanlaması yöntemi | Whisper.net + metin eşleme + oransal yedek | Forced alignment hazır değil |
| K-11 | Görsel modeli ve runtime | Z-Image-Turbo GGUF + sd-server; ikinci FLUX.2 klein 4B; 9B sert engel | Apache-2.0, 4 GB tarifi var, MIT runtime |
| K-12 | Görselde metin | Yasak; metin video katmanında | Türkçe render test edilmemiş |
| K-13 | Bulut görsel yedeği | fal Z-Image-Turbo | En ucuz, aynı model |
| K-14 | GPU iş sırası | TTS / görsel / render GPU işleri sıralı; sd-server boşta unload | 6 GB VRAM |
| K-15 | PyTorch runtime | uv ile model başına izole Python, localhost OpenAI uyumlu uç | Bağımlılık izolasyonu |
| K-16 | H-4 sandbox mimarisi | L0-L7 katmanları; L3 AppContainer spike 1-2 geçerse, yoksa restricted token + Chromium sandbox | Admin'siz en güçlü primitive |
| K-17 | Node sürümü | Node 26 LTS (`--allow-net` olmadan ağ kısıtı) | Node 24'te izin modeli ağı kısıtlamıyor |
| K-18 | Güçlü mod | Kurulumda opsiyonel admin firewall kuralı | L3/L5 yedeği |
| K-19 | Windows Sandbox | Varsayılan değil; ileride "paranoyak mod" | Admin, Home yok, tek örnek |

## 6. Açık sorular / riskler (her biri öneriyle)
- HyperFrames 1.0 öncesi değişkenlik → sürüm sabitle, yükseltmeyi H-4 testleri + örnek render ile kapıla.
- 15 dk render hızı ve bellek bilinmiyor → geliştirici PC'sinde `hyperframes benchmark --json`, düşük worker sayısıyla başla.
- Supertonic arşivli → aynalama + sherpa-onnx yolu; int8/fp32 Türkçe karşılaştırma testi.
- 6 GB VRAM süreleri doğrulanmadı → ilk çalıştırmada kalibrasyon, sonuçları ayarlara yaz.
- AppContainer loopback/iç içe Chrome belirsiz → spike 1-2 L3 seçiminden önce.
- Remotion müşteri PC lisansı belirsiz → ticari dağıtıma kadar Remotion kapalı.
- FFmpeg build lisansı (LGPL/GPL) doğrulanmadı → LGPL build paketle, configure bayraklarını kaydet.
- `lang="tr"` ile uppercase i→İ davranışı birincil kaynakta yeniden kontrol edilmedi → Türkçe glif içeren fontları paketle, görsel regresyon testi ekle.
- Google TTS Türkçe ses listesi doğrulanmadı → kullanmadan önce kontrol et.

## 7. Doğrulanmayanlar (toplu)
HyperFrames kesin çıkış tarihi · Chromium 147'nin beginFrame'i kaldırması · HyperFrames uzun render belleği · gerçek 15 dk render hızı · HyperFrames Parakeet sürümü · Remotion varsayılan concurrency · Revideo ekip durumu · Remotion dağıtık masaüstü lisansı · FFmpeg build lisansı · `lang="tr"` uppercase · motor başına eğitim verisi iddiaları · Supertonic 2026-07-23 duyurusu ve şirket dağılması · Chirp 3 HD 1M ücretsiz · Google Türkçe sesleri · Azure PAYG ve tr-TR WordBoundary · Chatterbox V3 6 GB VRAM/RTF · VoxCPM2 GGUF 6 GB · Supertonic hız (0.3x dışı) · int8 sherpa Türkçe kalite · Qwen3-TTS/MeloTTS/Orpheus/MMS detayları · 15k karakter/15 dk varsayımı · Chatterbox ONNX diakritik raporları · tüm 6 GB görsel süreleri · klein GGUF boyutu · city96 GGUF sd-server hatası · Z-Image-Turbo V2 · Klein 1080 yavaşlama · sd.cpp DLL · tüm modellerde Türkçe metin · Kommunity tam metni · Qwen-Image-2.1 lisansı · Ollama Windows görsel · BFL fiyat kademeleri · gemini-nano-banana-2.1 pazarlama adı · fal Klein 9B çıktı koşulları · diffusers sınıf adları · AppContainer loopback/muafiyet admin/miras/iç içe Chrome/GPU · Remotion sandbox açık Windows render · OffthreadVideo file: erişimi · WebRTC policy bayrağı · ^NOTFOUND catch-all · Firewall loopback · `--permission-audit` sürümü · CDP worker Fetch · Meziantou API yüzeyi · Codex orijinal yazı · `wsb exec` stdout.

## 8. Kaynaklar

**HyperFrames:** https://github.com/heygen-com/hyperframes · https://registry.npmjs.org/hyperframes · https://hyperframes.heygen.com/faq.md · https://hyperframes.heygen.com/packages/cli.md · https://hyperframes.heygen.com/concepts/determinism.md · https://hyperframes.heygen.com/guides/performance.md · https://hyperframes.heygen.com/guides/rendering.md · https://hyperframes.heygen.com/packages/engine.md · https://hyperframes.heygen.com/packages/producer.md · https://hyperframes.heygen.com/guides/captions-and-recuts.md · https://hyperframes.heygen.com/changelog.md · https://hyperframes.heygen.com/llms.txt · https://www.heygen.com/research/html-to-video · https://www.heygen.com/blog/heygen-april-2026-release · https://raw.githubusercontent.com/heygen-com/hyperframes/main/packages/engine/src/services/chromeSandbox.ts · https://raw.githubusercontent.com/heygen-com/hyperframes/main/packages/engine/src/services/fileServer.ts · üçüncü taraf: https://autoae.online/blog/hyperframes-topic2video-windows-setup

**Remotion:** https://github.com/remotion-dev/remotion/blob/main/LICENSE.md · https://www.remotion.dev/docs/license/pricing · https://www.remotion.dev/docs/license/faq.md · https://www.remotion.dev/docs/index.md · https://www.remotion.dev/docs/miscellaneous/chrome-headless-shell.md · https://www.remotion.dev/docs/using-randomness · https://www.remotion.dev/docs/ai/skills · https://www.remotion.dev/docs/captions/create-tiktok-style-captions · https://www.remotion.dev/docs/troubleshooting/sigkill · https://www.remotion.dev/docs/options/offthreadvideo-cache-size-in-bytes · https://registry.npmjs.org/remotion · https://registry.npmjs.org/@remotion/renderer/latest · https://raw.githubusercontent.com/remotion-dev/remotion/main/packages/renderer/src/open-browser.ts · https://www.remotion.dev/docs/chromium-flags · https://www.remotion.dev/docs/options/chromium-flags · https://www.remotion.dev/docs/renderer/render-media · https://www.remotion.dev/docs/offthreadvideo

**Diğer motorlar:** https://github.com/redotvideo/revideo · https://registry.npmjs.org/@revideo/core · https://www.webfolio.to/tools/revideo (üçüncü taraf) · https://registry.npmjs.org/@motion-canvas/core · https://pypi.org/pypi/manim/json · https://docs.manim.community/en/stable/changelog/0.19.0-changelog.html · https://docs.manim.community/en/stable/installation/uv.html · https://arxiv.org/abs/2603.13251 · https://arxiv.org/abs/2605.19382 · https://arxiv.org/abs/2502.19400 · https://registry.npmjs.org/editly · https://registry.npmjs.org/@diffusionstudio/core

**FFmpeg / ASS:** https://ffmpeg.org/ffmpeg-filters.html · https://aegisub.org/docs/latest/ass_tags/

**TTS:** [1] https://github.com/supertone-inc/supertonic · [2] https://huggingface.co/supertone-oss-archive/supertonic-3 · https://huggingface.co/Supertone/supertonic-3 · [3] https://antinodeaudio.com/en/work/faster-and-more-accurate-across-31-languages----introducing-supertonic-3 · https://support.supertone.ai/hc/articles/16940735914639 (403) · [4] https://github.com/resemble-ai/chatterbox · [5] https://ollama.com/blog/speech-models · [6] https://huggingface.co/supertone-oss-archive/supertonic-3/raw/main/LICENSE · [7] https://www.resemble.ai/learn/models/chatterbox-multilingual · [8] https://huggingface.co/ResembleAI/chatterbox-turbo-ONNX · https://huggingface.co/Vocabook/chatterbox-multilingual-ONNX-q4 · https://huggingface.co/hugbos/chatterbox-multilingual-ONNX-v2 · https://huggingface.co/ResembleAI/chatterbox-turbo-ONNX/discussions/3 · [9] https://docs.clore.ai/guides/audio-and-voice/chatterbox-tts (üçüncü taraf) · [10] https://github.com/OpenBMB/VoxCPM · [11] https://github.com/ozguradmin/ahenk-turkish-tts · [12] https://huggingface.co/Trendyol/Trendyol-TTS · [13] https://huggingface.co/coqui/XTTS-v2 · [14] https://www.ablt.dev/blog/turkish-tts/ · [15] https://huggingface.co/marduk-ra/F5-TTS-Turkish · [16] https://huggingface.co/k2-fsa/OmniVoice · [17] https://huggingface.co/rhasspy/piper-voices/blob/main/tr/tr_TR/dfki/medium/MODEL_CARD · [18] https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/VOICES.md · [19] https://github.com/k2-fsa/sherpa-onnx · https://csukuangfj.github.io/sherpa/onnx/tts/supertonic.html · https://k2-fsa.github.io/sherpa/onnx/tts/pretrained_models/kokoro.html · [20] https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md · [21] https://arxiv.org/abs/2601.15621 · https://gigazine.net/gsc_news/en/20260123-qwen3-tts-family-opensource · [22] https://github.com/myshell-ai/MeloTTS · [23] https://github.com/canopyai/Orpheus-TTS · [24] https://developers.openai.com/api/docs/models/gpt-4o-mini-tts · https://developers.openai.com/api/docs/pricing · [25] https://developers.openai.com/api/docs/guides/text-to-speech · [26] https://cloud.google.com/text-to-speech/pricing · https://www.costbench.com/software/voice-apis/google-tts-api/ (üçüncü taraf) · [27] https://elevenlabs.io/pricing/api · [28] https://elevenlabs.io/docs/overview/models · [29] https://elevenlabs.io/docs/api-reference/text-to-speech/convert-with-timestamps · [30] https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-services/ · https://www.developersdigest.tech/models/speech/azure · https://json2video.com/ai-voices/azure/voices/tr-tr-emelneural/ · https://learn.microsoft.com/en-us/answers/questions/1613851/wordboundary-event-not-called-when-using-neural-te · [31] https://onnxruntime.ai/docs/execution-providers/DirectML-ExecutionProvider.html · [32] https://docs.astral.sh/uv/guides/install-python/ · [33] https://github.com/sandrohanea/whisper.net · https://huggingface.co/nvidia/parakeet-tdt-0.6b-v3

**Görsel:** https://huggingface.co/Tongyi-MAI/Z-Image-Turbo · https://arxiv.org/html/2511.22699v2 · https://github.com/leejet/stable-diffusion.cpp/wiki/How-to-Use-Z%E2%80%90Image-on-a-GPU-with-Only-4GB-VRAM · https://huggingface.co/leejet/Z-Image-Turbo-GGUF · https://huggingface.co/black-forest-labs/FLUX.2-klein-4B · https://bfl.ai/blog/flux2-klein-towards-interactive-visual-intelligence · https://docs.comfy.org/tutorials/flux/flux-2-klein · https://bfl.ai/blog/flux-3-image-klein · https://recipes.vllm.ai/black-forest-labs/flux-3-image-klein · https://build.nvidia.com/black-forest-labs/flux-3-image-klein/modelcard · https://docs.nvidia.com/nemo/automodel/nemo-automodel/nemo_automodel/components/models/flux3/text_encoding · https://huggingface.co/ByteDance/SDXL-Lightning · https://huggingface.co/stabilityai/stable-diffusion-3.5-medium · https://stability.ai/license · https://github.com/leejet/stable-diffusion.cpp · https://api.github.com/repos/leejet/stable-diffusion.cpp/releases/latest · https://raw.githubusercontent.com/leejet/stable-diffusion.cpp/master/examples/server/README.md · https://raw.githubusercontent.com/leejet/stable-diffusion.cpp/master/examples/server/api.md · https://github.com/comfyanonymous/ComfyUI · https://docs.comfy.org/development/comfyui-server/comms_routes.md · https://huggingface.co/docs/diffusers/main/en/quantization/gguf · https://ollama.com/blog/image-generation · https://fal.ai/models/fal-ai/z-image/turbo/llms.txt · https://fal.ai/models/fal-ai/flux-2/klein/4b/llms.txt · https://fal.ai/models/fal-ai/flux-2/klein/4b/base/llms.txt · https://fal.ai/models/fal-ai/flux-2/klein/9b/llms.txt · https://docs.bfl.ai/quick_start/pricing · https://aistudio.google.com/docs/pricing · https://docs.cloud.google.com/gemini-enterprise-agent-platform/models/gemini/3-1-flash-lite-image · https://developers.openai.com/api/docs/guides/image-generation · ikincil: https://admin.kinovi.ai/blog/z-image-low-vram-6gb-gpu-setup · https://civarchive.com/articles/25600/z-image-turbo-6b-on-6gb-gpu-settings-workflow-source-for-beginners · https://alternativeto.net/news/2026/9/alibaba-launches-qwen-image-2-1-a-7b-ai-model-with-native-transparency-and-a-license-change/ · https://runtimewire.com/article/alibaba-qwen-image-2-1-transparent-editing-research-license · https://aiweekly.co/alerts/alibaba-ships-qwen-image-21-7b-dit-with-native-2048x2048-and-rgba-drops-apache · https://alternativeto.net/news/2026/1/ollama-launches-experimental-local-image-generation-on-macos-with-z-image-turbo-and-flux-2 · https://www.usagepricing.com/blueprint/activity/fal-ai-2026-10-22-flux-3-image-price-doubles

**Sandbox:** https://nodejs.org/api/permissions.html · https://nodejs.org/api/cli.html · https://nodejs.org/en/about/previous-releases · https://sources.debian.org/src/nodejs/22.14.0%2Bdfsg-1/doc/contributing/security-model-strategy.md · https://nodejs.org/en/blog/vulnerability/december-2025-security-releases · https://www.tenable.com/cve/CVE-2026-21636 · https://security-tracker.debian.org/tracker/CVE-2026-21636 · https://chromium.googlesource.com/chromium/src/+/main/docs/design/sandbox.md · https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md · https://chromium.googlesource.com/chromium/src/+/main/net/dns/mapped_host_resolver.h · https://chromium.googlesource.com/chromium/src.git/+/lkgr-android-internal/third_party/blink/common/peerconnection/webrtc_ip_handling_policy.cc · https://bugzilla.mozilla.org/show_bug.cgi?id=1452713 · https://gist.github.com/josephbolus/e214b3720dc1f3d6ef274ad905e6ab7d · https://chromium.googlesource.com/chromium/src/sandbox/+/1900961933199837fc895087580930d4274a5259 · https://hackernoon.com/from-playwright-routing-to-browser-protocols-how-request-interception-works · https://kiwix.lema.org/stackoverflow.com_en_all_2023-11/questions/70645680/does-chromium-support-intercept-webworker-requests-via-cdp · https://learn.microsoft.com/en-us/windows/win32/api/userenv/nf-userenv-createappcontainerprofile · https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute · https://learn.microsoft.com/en-us/windows/win32/secauthz/createprocessinsandbox · https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh780593(v=win.10) · https://learn.microsoft.com/en-us/windows/win32/api/netfw/nf-netfw-networkisolationsetappcontainerconfig · https://blahcat.github.io/posts/2020/12/29/cheap-sandboxing-with-appcontainers.html · https://windows-hexerror.linestarve.com/q/so63627101-failing-when-using-pinvoke-calling-updateprocthreadattribute-for-appcontainer-security-capabilities · https://ahrvoje.github.io/sandy_cli/modes.html · https://learn.microsoft.com/en-us/windows/win32/secauthz/mandatory-integrity-control · https://zeltser.com/windows-integrity-levels-malware-protection-files · https://msdn.microsoft.com/library/ms684161(v=VS.85).aspx · https://learn.microsoft.com/pl-pl/windows/desktop/api/WinNT/ns-winnt-jobobject_basic_limit_information · https://www.nuget.org/packages/Meziantou.Framework.Win32.Jobs · https://meziantou.net/killing-all-child-processes-when-the-parent-exits-job-object.htm · https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/ · https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-install · https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-cli · https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-versions · https://openai.com/index/building-codex-windows-sandbox/ (403) · https://discuss.pytorch.kr/t/openai-windows-codex-appcontainer-write-restricted-token-windows-firewall/10252 · https://github.com/adams85/acornima · https://www.nuget.org/packages/Acornima · https://www.nuget.org/packages/Acornima.Extras
