# kurgu30 Yöntem Testleri — Taha Konsept
Tarih: 2026-11-14. Rapor no: 0002. Sürüm: 0.2.2 → 0.2.3.
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
