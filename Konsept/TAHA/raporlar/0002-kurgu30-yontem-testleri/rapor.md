# kurgu30 Yöntem Testleri — Taha Konsept
Tarih: 2026-11-14. Rapor no: 0002. Sürüm: 0.2.2 → 0.2.3; GPU turu 0.2.4 (sonda).
Okuyucu notu (AI için): Her satır gerçek hattan geçti (Web/API kuyruğu → orkestratör → `agent --headless`), sahte LLM (`tests/e2e/mock-llm/kurgu30.cjs`, 4 sahne, 51 kelime, İstanbul'un fethi), DB yalnız `tahakonsept_e2e`. Ayrıntılı ölçüm ve komutlar: `docs/E2E-RAPORU.md`. Çıktılar (gitignore): `tests/e2e/out/kurgu30/<etiket>/`.

## Her başarılı koşuda denetlenen
ffprobe h264 + aac, 1080x1920, 30 fps, yuv420p; video ≈ anlatım süresi (±0.05 s); volumedetect (sessiz değil); 2–4 kare gözle (sahne, altyazı, ilerleme çubuğu, Türkçe harf); API spokenText 5 normalizasyon (Gugıl, yirmi dokuz Mayıs bin dört yüz elli üç, İkinci Mehmed, yüzde kırk, on beşinci yüzyıl); faster-whisper small kelime yakalama (yazılı metne göre); görsel açıkken sahne başına tam 1080x1920 tek görsel.
Koşucu: `node tests/e2e/matrix.cjs --label <x> [--engine|--speech-model|--image-mode Local --image-model] --new-concept [--whisper <uv.exe>]`.

## Sonuçlar
Süre = video saniyesi; Render = kuyruktan bitişe toplam (soğuk = ilk kurulum/indirme dahil); WR = Whisper kelime yakalama.

### Video motorları (Supertonic 3, görsel kapalı)
| Yöntem | Durum | Süre | Render | WR | Not |
|---|---|---|---|---|---|
| HyperFrames | düzeltildi+geçti | 31.27 | 80.2 | 0.865 | SceneRender 62.6 s |
| Remotion | düzeltildi+geçti | 31.27 | 91.4 | 0.865 | SceneRender 71.5 s |
| Revideo | düzeltildi+geçti | 31.27 | 53.3 | 0.865 | SceneRender 24.5 s (en hızlı); dead-proxy ağ engeli doğrulandı |

### Hafif yerel ses (CPU, HyperFrames, görsel kapalı)
| Yöntem | Durum | Süre | Render | WR | Not |
|---|---|---|---|---|---|
| Supertonic 3 (taban) | geçti | 31.27 | 80.2 | 0.865 | ONNX, Python yok, ses 7.4 s |
| Piper DFKI | düzeltildi+geçti | 26.41 | 628.2 soğuk | 0.808 | uv+Python ilk kurulum; 0 dBFS tepe |
| Piper Fahrettin | düzeltildi+geçti | 23.27 | 87.1 | 0.769 | koşucunun 25 s alt sınırı dışı (hızlı ses) |
| Piper Fettah | geçti | 28.50 | 105.3 | 0.731 | ayna yedeği |
| MMS-TTS tr | düzeltildi+geçti | 34.37 | 93.4 | 0.673 | en az anlaşılır; isteğe bağlı 0.70 eşiği altı |
| EMA Lightning | geçti | 23.00 | 240.2 soğuk | 0.827 | 25 s alt sınırı dışı (hızlı) |
| Ahenk Cep | geçti | 25.32 | 193.6 soğuk | 0.846 | hız ayarını yok sayar |
| Trendyol-TTS | atlandı | – | – | – | ≥8 GB VRAM |

### GPU yerel ses (HyperFrames, görsel kapalı)
| Yöntem | Durum | Süre | Render | WR | Toplam VRAM tepe |
|---|---|---|---|---|---|
| Chatterbox ML | düzeltildi+geçti | 23.04 | 206.6 | 0.712 | 5.7 GB (sınırda) |
| XTTS-v2 | düzeltildi+geçti | 31.59 | 157.0 | 0.827 | 4.5 GB |
| F5-TTS Türkçe | düzeltildi+geçti | 25.37 | 126.7 | 0.712 | 3.4 GB |
| OmniVoice | geçti | 25.23 | 337.7 soğuk | 0.827 | 3.6 GB |
| VoxCPM2 | atlandı | – | – | – | VRAM taşması: 6.6 s ses ≈ 7 dk; CPU'da RAM ile çöktü |
| Ahenk (tam) | atlandı | – | – | – | VoxCPM2 tabanı, ≥8 GB |
| Fish S2 Pro | atlandı | – | – | – | 11 GB ağırlık, ≥12 GB |
| Kokoro (dil koruması) | düzeltildi+geçti | – | – | – | kurulumdan önce 4 s'de net Türkçe hata |

### Yerel görsel (HyperFrames + Supertonic 3, sd.cpp CUDA)
| Model | Durum | Görsel başı | Render | WR | Kip |
|---|---|---|---|---|---|
| FLUX.2 klein 4B | düzeltildi+geçti | ~29 s | 214.4 | 0.827 | Standard |
| SDXL-Lightning | düzeltildi+geçti | ~12 s | 168.5 | 0.769 | Standard + offload, 768x1344 → büyütme |
| Z-Image Turbo | geçti | ~80 s | 461.0 | 0.846 | Standard |
| SD3.5 Medium | geçti | ~65 s | 498.0 soğuk | 0.865 | Standard, 768x1344 |
| FLUX.3 Klein Q4_K | geçti | ~92 s | 571.1 soğuk | 0.846 | Reduced; katalog 8→6 GB |
| FLUX.2 klein 9B Q4_0 | geçti | ~88 s | 595.3 soğuk | – | Reduced; ~5 GB boş RAM ister; katalog 8→6 GB |
| Qwen-Image | atlandı | – | – | – | en küçük Q2_K 7 GB + 4.7 GB kodlayıcı |
| Görsel kapalı (boyut denetimi) | geçti | – | 72.8 | – | 0 görsel |

## Düzeltilen hatalar (16)
1. HyperFrames lint, görselli sahneyi `missing_local_asset` ile reddediyordu — doğrulama klasörüne assets kopyalanır (c9e84cb).
2. Remotion/Revideo son videosu yuvj420p/pc idi — Compose limited range BT.709'a çevirir, tv/bt709 etiketler (90b9870).
3. Revideo yazı tipleri yüklenmeden düzen hesaplıyordu, kartta ikinci satır taşıyordu — fonts.load ön yükleme (0aae158).
4. Headless ajan kapanışta IAsyncDisposable hatasıyla çöküyordu — DisposeAsync (38d95d3).
5. uv yoksa Python ses motorları düşüyordu — IToolProvisioner → ToolProvisionerBase → PrerequisiteToolProvisioner (d829f5a).
6. uv Python indirmesi Windows dosya kilidinde (os error 5) düşüyordu — 2 yeniden deneme (982fa39).
7. Piper Fahrettin/Fettah 404 — speaches-ai aynası yedeği (98275ae).
8. MMS-TTS noktalamayı yutuyor, İ/I'yı bozuyordu — Türkçe küçük harf + parçalı sentez (38bcb98).
9. Chatterbox yüklenmiyordu — `setuptools<81` (1c6a421).
10. GPU ses modeli Speech aşamasından sonra VRAM'de kalıyordu — ISpeechEngine.ReleaseAsync (1c6a421).
11. Kokoro + Türkçe önce ~2 GB kuruyordu — dil kurulumdan önce denetlenir + web uyarısı (1c6a421, 1e1f4b8).
12. XTTS-v2 `six` eksik (c3bbbaf).
13. F5-TTS İngilizce örnek klipten klonluyordu (44 s, aksanlı) — gömülü Türkçe referans (0788ff0).
14. Yerel görsel istenen boyutta değildi (1088x1920) — IImageFitter, kırp+ortala (b38f42c).
15. SDXL 2 MP'de nesneyi ikiliyordu — katalog maxPixels, bütçe içinde üret + büyüt (dafb1cb).
16. (koşucu) matrix.cjs 15 dk sonra 401 — bir kez yeniden giriş (1e1f4b8).

## Öneriler (6 GB RTX 3060, ~2 GB başka uygulamalarda)
- **Ses varsayılanı: Supertonic 3** — en yüksek WR (0.865), GPU/Python gerektirmez, sentez ~7 s, süre hedefi tutuyor. CPU yedeği: Ahenk Cep (0.846). GPU ile farklı ses: XTTS-v2 veya OmniVoice (0.827). MMS-TTS önerilmez.
- **Görsel varsayılanı: FLUX.2 klein 4B** (~29 s/görsel, iyi kalite). En iyi istem uyumu: FLUX.3 Klein (~92 s). Hızlı taslak: SDXL-Lightning (~12 s, istem uyumu zayıf).
- **Motor: HyperFrames varsayılan** (LLM'e en uygun, lint, doğrudan yuv420p). **Revideo** hız seçeneği (sahne render'ı ~2.5x hızlı). Remotion destekli.

## Doğrulanmayanlar
- Motorların soğuk kurulumu (üçü de kuruluydu). Revideo opaklık 0.32 sonrası, Revideo/Remotion görselli koşu yapılmadı (yalnız HyperFrames görselli).
- Kokoro mesajının son metni canlı koşulmadı; web uyarısı tarayıcıda görülmedi.
- Chatterbox/XTTS modelleri teşhis sırasında indirildi; ses süreleri indirme içermez. FLUX.2 9B için Whisper koşulmadı.
- Altyazısız / ilerleme çubuksuz kombinasyonlar ve online sağlayıcılar bu turda koşulmadı.

## GPU turu (0.2.4) — 14 Kas 2026
Önce GPU (4166d09, 52d3c1f) gerçek RTX 3060 Laptop 6 GB'ta kurgu30 ile doğrulandı. Aynı koşucu ve denetimler (ffprobe, 4 kare, volumedetect, 5 okunuş, Whisper small). Cihaz kanıtı: ajan logu + Windows GPU sayaçları (süreç başına adaptör LUID'i; RTX = `0x10357`, Intel UHD ayrı) + `nvidia-smi` (0,5 sn; `tests/e2e/out/kurgu30/logs/vram-gpu-*.csv`, `gpuproc-*.csv`). VRAM tepe = masaüstü dahil toplam (boşta ~0,6–1,0 GB).

| Yöntem | Kullanılan cihaz | Render önce → sonra (s) | Aşama önce → sonra | VRAM tepe | Sonuç |
|---|---|---|---|---|---|
| HyperFrames + Supertonic 3 | Supertonic DirectML (RTX, log adı + LUID); Chrome ANGLE/D3D11 RTX (`browserGpuMode probe → hardware`); Compose `h264_nvenc` | 80.2 → 69.8 | SceneRender 62.6 → 54.4, Compose 9.6 → 7.4, Speech 7.4 → 7.2 | 1.68 GB | geçti, WR 0.846 |
| Remotion + Supertonic 3 | Chrome `--gl=angle` (3D motoru RTX'te); Compose NVENC | 91.4 → 70.9 | SceneRender 71.5 → 60.0, Compose 10.6 → 7.2 | 1.75 GB | geçti, WR 0.865 |
| Revideo + Supertonic 3 | Chrome ANGLE/D3D11 RTX (düzeltmeyle; önce SwiftShader'daydı); Compose NVENC | 53.3 → 28.6 | SceneRender 24.5 → 14.8, Compose 17.4 → 7.6 | 1.74 GB | düzeltildi+geçti, WR 0.865; ağ yasağı iki kipte doğrulandı |
| MMS-TTS tr | cuda (düzeltmeyle; ilk koşu CPU tekerleğinde kaldı) | 93.4 → 153.8 soğuk / 85.5 sıcak | Speech 13.1 → 73.7 soğuk (CUDA torch 57 s) / 15.3 sıcak | 1.92 GB | düzeltildi+geçti, WR 0.731–0.75 (CPU 0.673) |
| EMA Lightning | cuda, düz yol (düzeltmeyle) | 240.2 soğuk → 301.3 soğuk (lightning) → 70.8 | Speech 127.5 soğuk → 232 (lightning soğuk) / 36 (lightning sıcak) → 4.5 | 5.76 → 1.38 GB | düzeltildi; tek başarısız denetim bilinen 25 s alt sınırı (23.00 s, CPU'daki gibi); WR 0.846 |
| Ahenk Cep | cuda | 193.6 → 150.8 soğuk / 69.5 sıcak | Speech 102.2 → 84.3 soğuk (CUDA torch 73 s) / 6.2 sıcak | 1.37 GB | geçti, WR 0.808–0.827 |
| FLUX.2 klein 4B + Supertonic 3 | sd.cpp CUDA (sd-cli 4.3 GB, RTX), Supertonic DirectML, Compose NVENC | 214.4 → 222.5 | Images 119 → 138 (görsel ~33 s, Standard) | 5.54 GB | geçti, WR 0.846; NVENC + sd.cpp sığdı (sıralı aşamalar) |
| FLUX.2 4B, Supertonic açık kalmışken | aynı | 229.7 | boş VRAM 5.25 → 4.6–4.8 GB | 5.71 GB | geçti ama sınırda → düzeltildi (aşağıda) |
| FLUX.2 4B, düzeltmeden sonra | aynı | 219.0 | boş VRAM 5.48–5.50 GB (ilk görsel 4.83, geçici) | 5.47 GB | geçti, WR 0.865; logda "DirectML oturumu kapatıldı" |

Notlar: sıcak = ortam kurulu, model önbellekte; GPU işçisi her işten sonra kapandığı için her iş torch içe aktarma + CUDA başlatmayı öder (MMS sıcak Speech 15.3 s, CPU 13.1 s — kazanç yok; diğer hafif sesler 4–6 s). Görsel zaten CUDA'daydı, FLUX süresi değişmedi. SceneRender'da koşular arası ±10 s oynama var (son taban koşusu 70.3 s). Hiçbir yöntemde (MMS'in düzeltme öncesi koşusu hariç) CPU'ya düşüş uyarısı yok.

### GPU turunda düzeltilenler (5)
1. Revideo Chrome GPU'yu hiç kullanmıyordu: chrome-headless-shell `--enable-gpu` olmadan rasterleştirmeyi `disabled_software` (SwiftShader) bırakıyor; eklendi. GPU açılınca WebCodecs donanım kodlayıcısı mp4-wasm süresini bozuyordu (`t: N/A`) → `--disable-accelerated-video-encode` (çizim GPU'da). Gerçek Chrome testi (SystemInfo) (7d6f3d7).
2. Hafif Python sesleri CPU torch'ta kalıyordu: `torch==2.8.0` isteğini yüklü `2.8.0+cpu` karşıladığı için uv CUDA tekerleğini kurmuyordu; CUDA dizininde `==2.8.0+cu128` sabitlenir, parmak izi değişir (8d79f60).
3. Supertonic logu kartı göstermiyordu: DXGI yüksek performans sırasıyla "GPU: NVIDIA GeForce RTX 3060 Laptop GPU" yazılır (31b3c95).
4. EMA Lightning `lightning()` CUDA grafikleri her işte ~28 s (ilk ~170 s) ve 2.9–5.7 GB VRAM harcayıp 0.8 s kazandırıyordu → düz CUDA varsayılan (4a2c481).
5. Supertonic DirectML oturumu işler arasında ~0.5 GB VRAM tutuyordu → Speech sonunda ReleaseAsync kapatır (ac08df6).

### GPU turunda doğrulanmayanlar
- Remotion'ın sahne içi kodlaması (`--hardware-acceleration=if-possible`) Windows'ta NVENC'e geçiyor mu ölçülmedi; son kodlama zaten birleştiricide NVENC.
- CPU yedek yolları (GPU'suz makine, sürücü hatası, VRAM yetmemesi) bu turda canlı tetiklenmedi; birim testli. GPU modunda GPU'ya özgü olmayan bir render hatası CPU'ya düşmez (yalnız log GPU kaynaklı görünürse).
- Revideo/Remotion görselli koşu ve motorların soğuk kurulumu yine koşulmadı.
