# Uçtan uca (e2e) test raporu — 14 Kas 2026

Bu rapor, uygulamanın uçtan uca nasıl denendiğini, bulunan hataları ve düzeltmeleri anlatır. Tüm senaryo yerel makinede, ayrı **`tahakonsept_e2e`** veritabanıyla koşuldu; gerçek `tahakonsept` veritabanına dokunulmadı. LLM olarak sahte bir OpenAI uyumlu sunucu kullanıldı, bu yüzden hiçbir ücretli çağrı yapılmadı. Masaüstü ajanı ise gerçekti: eşlendi, Supertonic 3 ile ses üretti, HyperFrames ile render etti ve ffmpeg ile birleştirdi.

**Özet:**
- **Senaryo:** tüm adımlar ✅ (son tam koşu: `a b c d e f g h pages i` + `b` + `mobile`).
- **Derleme:** 0 uyarı, 0 hata.
- **Testler:** 761/761 yeşil.

## Nasıl koşulur
```
scripts/dev-tunnel.ps1 -Background          # 5433 → Pi PostgreSQL
dotnet build TahaKonsept.slnx
node tests/e2e/stack.cjs start everything   # mock LLM 7199, API 7191, Web 7190, ajan --headless (video 7120)
node tests/e2e/e2e.cjs a b c d e f g h pages i uyelik
node tests/e2e/stack.cjs stop everything
```

**Bileşenler:**
- **`tests/e2e/e2e.cjs`:** senaryo koşucusu. puppeteer-core ile sistem Chrome'u headless çalıştırır. Adımlar `tests/e2e/steps/*.cjs` dosyalarındadır.
- **Çıktılar** `tests/e2e/out/` altına yazılır (git dışı):
  - adım sonuçları: `results.json`
  - konsol/HTTP sorunları: `issues.txt`
  - ekran görüntüleri: `<adım>-<ad>.png`
- **`tests/e2e/stack.cjs`:** yığını başlatır, durdurur ve durumunu gösterir. Bağlantı metninde yalnız `Database=tahakonsept_e2e` kullanılır, betik başka bir veritabanına izin vermez. Veri klasörü `out/appdata-*`'dır. `Agents__WebPort=7190` ile eşleme adresi doğru porta gider.
- **`tests/e2e/mock-llm/server.cjs`:** bağımlılıksız Node sunucusudur, 7199 portunda çalışır.
  - Uçlar: `GET /v1/models`, `POST /v1/chat/completions`. `stream:true` gelirse SSE ile yanıt verir.
  - İsteği `response_format.json_schema` şekline ve promptlara bakarak tanır, şu yanıtlardan birini geçerli biçimde döner:
    - konu önerisi
    - tam konsept: 3 sahne, ~21 sn; "Google" ve "29 Mayıs 1453" ile normalizasyonu dener, okunuş listesi verir
    - kelime denetimi
    - revize
    - HyperFrames sahne kodu: ajanın lint'inden geçer ve render edilir
  - `GET /__log` son çağrıları listeler.
- **Ajan kökü:** `.tools-dev/e2e-agent`. Node, ffmpeg, chrome-headless-shell, motor şablonları ve Supertonic modeli var olan kopyalara bağlantı (junction) ile verildi. ffmpeg bağlantı yerine kopyalandı, çünkü ajan `bin/ffmpeg.exe` düzenini bekliyor.

## Senaryo sonuçları

| Adım | Ne denendi | Sonuç | Ekran görüntüleri |
|---|---|---|---|
| a | ➕ ile şablondan profil (Tarih – Osmanlı, 20 sn) → aktif; ✏️ ile ton düzenlendi; üst bar profil seçici kesilmiyor | ✅ (hata düzeltildi, bkz. #2) | `a-profil-ekle-modal`, `a-profil-duzenle-modal`, `a-topbar-*`, `probe-topbar` |
| b | Ayarlar → Yerel LLM satırı (`http://localhost:7199/v1`, anahtarsız) → **Test et: geçerli**; Konseptler model penceresinde 3 model = `local-openai:mock`; üretim kod modeli = `local-openai:mock` | ✅ | `b-yerel-llm-satir`, `b-konsept-modeller-modal`, `b-kod-modeli-modal` |
| c | 5 konu öner → 2 seç → Seçtiklerini üret → iş biter → 2 konsept Üretilenler'de (kelime denetimi açık: konu + konsept + **kelime denetimi** LLM çağrıları görüldü); Fikirden üret 1; Düzenleme alanında kanca değiştir + Kaydet; revize talimatı → aday penceresi → Bunu da düzenle → Tekrar üret → Seç; Sil → Silinenler → Geri al → Sil → Kalıcı sil; Onayla | ✅ | `c-konular`, `c-is-ilerleme`, `c-uretilenler-2`, `c-duzenleme`, `c-revize-aday`, `c-revize-aday-2`, `c-silinenler`, `c-son` |
| d | Okunuş sözlüğüne kayıt (Bizantion → Bizantiyon); deneme kutusu **ses üretmeden** normalleştirilmiş okunuşu ve 4 değişikliği gösterdi (Google → Gugıl, tarih yazıyla, III. → Üçüncü) | ✅ (hata düzeltildi, bkz. #3) | `d-deneme-kutusu` |
| e | Gerçek masaüstü ajanı `--headless` modda; kod `out/pair-code.txt`'ye yazıldı, Web'den Ayarlar → Masaüstü programı → **Kodu gir** ile onaylandı; ajan çevrimiçi (RTX 3060 Laptop, 6 GB) | ✅ (yeni geliştirici modu, bkz. #8) | `e-masaustu` |
| f | Ayarlar: HyperFrames, görsel kapalı, yerel Supertonic 3, altyazı + ilerleme çubuğu → onaylı konsept sıraya → Sırayla üret → aşamalar Hazır (~65–85 sn); koşu sırasında **Durdur** → Paused, **Kaldığı yerden devam** → Running; mp4 diskte; Web oynatıcısı ajan adresinden yükledi (**HTTP 206**, readyState 4, 1080×1920, 29,07 sn) | ✅ | `f-ayarlar`, `f-ses-modal`, `f-sira-basladi`, `f-durduruldu`, `f-son-durum`, `f2-oynatici`, `f-frame-3/13/24` |
| f (dosya) | ffprobe: h264 1080×1920 + aac 48 kHz, ikisi de 29,23 sn; seslendirme toplamı (3 sahne wav) 29,23 sn = video süresi. Kareler: karaoke altyazı alt bölgede, üst ilerleme çubuğu görünüyor, sahne boş değil (başlık, alt başlık, halka) | ✅ | `f-frame-3.png`, `f-frame-24.png` |
| g | Üretilenler: 3 sekme dolu (seslendirme/sahneler, konsept, ayrıntı + aşama aşama ücret); **Klasörde aç** ve **Sistem oynatıcısı** komutları ajana ulaştı (ajan logu: `Sunucu komutu işlendi: OpenFolder / OpenInSystemPlayer`; açılan pencereler test sonunda kapatıldı); **Yeni sürüm** yalnız altyazı kapalı → tahmin "Altyazı ve birleştirme + Son dosya", gerçekte yalnız **Compose + Mux** yeniden çalıştı (diğer 9 aşama `reused`), v2 oluştu (altyazısız kare doğrulandı), v2 silindi (dosya da diskten silindi) | ✅ (hata düzeltildi, bkz. #5, #6) | `g-sayfa`, `g-ayrinti-ucret`, `g-yeni-surum-modal`, `g-v2`, `g-v2-frame` |
| h | Yönetim paneli: konsept/video sayıları, etkinlik günlüğü sekmesi; alt Günlük şeridi + açılır panel; Hata bildir penceresi gönderildi (200) ve rapor panelde görünüyor | ✅ | `h-admin`, `h-admin-gunluk`, `h-gunluk`, `h-hata-bildir`, `h-admin-hatalar` |
| pages / mobile | Tüm sayfalar açık/koyu tema + 390 px; dar ekranda üst bar araçları taşmıyor | ✅ (hata düzeltildi, bkz. #9) | `pages-*.png`, `mobile-topbar` |
| i | Web'den Çıkış yap (logout-all) → giriş sayfası; ajan logunda "Sunucu hub bağlantısını hemen kapattı … token yenileniyor" → "Token yenileme reddedildi" → "yeniden eşleme gerekiyor" → yeni eşleme kodu | ✅ (hata düzeltildi, bkz. #7) | `i-cikis`, `out/i-agent-log.txt` |
| uyelik (15 Kas, 0.4.0) | Oturumsuz `/` → Giriş, `/Kurulum` → Kayıt ol, giriş ↔ kayıt bağlantıları; A kayıt → Başlangıç → profil → çıkış → giriş; B kayıt → A profilini listede görmez, kimlikle açamaz/silemez (404), Yönetim paneline ve kayıt ayarına giremez (403); SAHTE e2e yöneticisi (`tests/e2e/lib/e2e-admin.cjs`, rastgele parola + Identity v3 özeti) normal giriş sayfasından e-postayla girer; kullanıcı listesi; "Yeni kayıtlara izin ver" kapat → Kayıt ol mesajı + Ajax kayıt 403 → aç; B devre dışı → giriş reddi → etkinleştir; yönetici kendini devre dışı bırakamaz; açık/koyu + 390 px (taşma yok) | ✅ 34/34 | `uyelik-giris-*`, `uyelik-kayit-*`, `uyelik-a-baslangic`, `uyelik-admin-kullanicilar-*`, `uyelik-kayit-kapali` |

## Bulunan hatalar ve düzeltmeler

1. **Yeni özellik: Yerel LLM (OpenAI uyumlu)** — commit `f36db6f`
   - Platform `local-openai`; Ollama, LM Studio ve llama.cpp server ile kullanılır.
   - Ayarlar satırında kök adres alanı zorunlu, anahtar isteğe bağlı. Boş anahtarla yeniden kaydedince kayıtlı anahtar korunur.
   - Kök adres kasada şifreli saklanır: yeni kolon `protected_base_url`, migration `LocalOpenAiBaseUrl`.
   - Test çağrısı `GET {kök}/models`'tir.
   - Katalogda geçiş kaydı `local-openai:<model>` olarak durur, fiyatı 0'dır.
   - `LocalOpenAiTextModel` sınıfı `OpenAiCompatibleTextModelBase` üzerinde kuruldu; anahtar yoksa `Authorization` başlığı gönderilmez.
   - Konseptler ve üretim model seçicilerine "Yerel model adı yaz…" kutusu eklendi.
   - 6 birim testi eklendi.
2. **Üst bar profil seçici metni kesiliyordu** ("Profil yok — ➕ ile ekleyi…") — commit `0331d83`
   - Metin kısa "Profil yok" oldu, ayrıntı ipucunda (`title`).
   - Seçici `text-overflow: ellipsis` ile biter.
   - Aktif profilin tam adı da ipucunda görünür.
3. **Okunuş deneme kutusu her denemede ses üretiyordu** (online seste ücretli) — commit `38911d4`
   - `PronunciationPreviewRequest.Synthesize` eklendi. `false` olunca yalnız metin döner: normalize sonucu ve `Changes` listesi.
   - Deneme kutusu artık "Dönüştür" (ücretsiz) yapıyor. Sonuçtaki "Dinle" düğmesi seslendirmeyi isteğe bağlı yapar.
   - 1 birim testi eklendi.
4. **Yerel LLM üretim seçicisinde "Online" rozetli ve fiyatsız görünüyordu** — commit `38911d4`
   - Artık "Yerel sunucu" rozeti ve "Ücretsiz" fiyatı gösteriliyor.
5. **Yeni sürüm penceresi varsayılan sesi (Supertonic M1) açıkça kaydediyordu** — commit `86efe3c`
   - Belirti: ayarda ses boştu (model varsayılanı). Yalnız altyazı değiştirilince bile Ses aşaması yeniden çalışıyordu. Online seste bu boşa ücret demektir.
   - Neden: düzenleyicinin `read()` metodu ekranda işaretli görünen varsayılan sesi ayara yazıyordu.
   - Düzeltme: yalnız kullanıcının açıkça seçtiği ses yazılır.
   - Sonuç: v2'de yalnız Compose + Mux yeniden çalışıyor.
6. **Ajan "Klasörde aç / Sistem oynatıcısı / Sil" komutlarını başarı durumunda hiç loglamıyordu** — commit `86efe3c`
   - Komutun ajana ulaşıp ulaşmadığı doğrulanamıyordu.
   - `AgentCommandHandlerBase.OnVideoCommand` kancası eklendi; somut sınıf başarıyı `Information`, başarısızlığı `Warning` olarak yazar.
7. **Web'den çıkış (logout-all) sonrası ajan sonsuz yeniden bağlanma döngüsüne giriyordu** — commit `3e5fde2`
   - Belirti: sunucu ajanı iptal edip hub bağlantısını kesiyordu, ama ajanın erişim token'ı (15 dk) hâlâ geçerliydi. 401 gelmediği için ajan "bağlandı → kesildi" döngüsünde kalıyor, yeniden eşleme istemiyordu.
   - Düzeltme: sunucu bağlantıyı tanıtım (Hello) bitmeden kapatırsa ajan token'ı yeniler. Yenileme reddedilirse oturumu kapatır ve yeniden eşleme ister (GUI'de kurulum penceresi, headless modda yeni kod).
8. **Geliştirici/e2e için ajanın penceresiz modu** — commit `901354b`
   - Komut: `TahaKonsept.Agent.exe --headless --server <url> --pair-code-out <dosya> [--stop-file <dosya>] [--skip-prerequisites]`
   - Yapı: `IHeadlessAgentRunner` → `HeadlessAgentRunnerBase` → `HeadlessAgentRunner`; servisler `AddHeadlessAgent` ile kaydedilir.
   - Avalonia açılmaz, eksik bileşenleri kurar. Kod dosyaya yazılır, onaydan sonra dosyaya `PAIRED` yazılır.
   - Belgesi: `docs/dalga2/desktop-app.md`.
   - 2 birim testi eklendi.
9. **390 px ekranda üst bar araçları taşıyordu**
   - ➕, tema, hesap ve Ayarlar ekran dışında kalıyordu.
   - ≤640 px'de araç etiketleri gizlenir ve profil seçici daralır; ≤420 px'de "Bu ay" harcama rozeti gizlenir.

Test betiğinin kendi hataları (seçiciler, aynı adımın tekrar koşulmasında eski sıra kaydını bulma, kutuya iki kez "mock" yazma) ürün hatası değildir. Bunlar betikte düzeltildi.

## Gözlemler (düzeltilmedi, karar gerekebilir)
- **Süre tahmini:** konsept üretimi 2,4 kelime/sn varsayıyor (~21 sn). Supertonic 3 (1,0× hız) aynı metni 29 sn'de okudu. Hedef süre 20 sn olan profilde video ~%40 uzun çıkıyor. Ses modeline göre kelime/sn katsayısı ya da hızın otomatik ayarı düşünülebilir. **Çözüldü (14 Kas 2026):** katalogda model başına `wordsPerSecond` (Supertonic 3 = 1,7, bu ölçümden: 50 kelime / 29,23 sn); konsept üretimi kullanıcının üretimdeki ses modelinin hızını kullanır, her videodan sonra gerçek hız kaydedilip ortalaması tercih edilir; ScenePlan tahmini hedefi %15 aşarsa işlem günlüğünde uyarı.
- **Gerçek bilgi:** "Tarih" profilinde otomatik açık. Yerel LLM web araması yapamadığı halde prompt kaynak istiyor. Modelin `supportsWebSearch=false` olduğu durumda bu seçeneği kapatmak ya da uyarmak gerekebilir; aksi halde uydurma kaynak riski var.
- **Eşleme doğrulama adresi:** sunucu yapılandırmasından gelir (`Agents:WebPort`, varsayılan 7110). e2e'de `Agents__WebPort=7190` ile düzeltildi. Pi'de 7110 doğru.

## Doğrulanmayanlar
- Avalonia GUI ajanının kendisiyle eşleme ve kurulum penceresi. UI otomasyonu yerine `--headless` kullanıldı; çekirdek, motorlar ve eşleme aynı kod.
- Gerçek bir Ollama / LM Studio sunucusu. Yalnız sahte sunucu denendi; gerçek modellerin `json_schema strict` desteği modele göre değişebilir.
- Online LLM, ses ve görsel sağlayıcıları (anahtar yok).
- (Bu bölüm ilk turu anlatır; yerel görsel modelleri ve Remotion/Revideo sonradan kurgu30 matrisiyle doğrulandı — aşağıya bakın.)
- "Yeniden üret" (konsept düzenleyip baştan üretme), bütçe limiti engeli ve ajan çevrimdışıyken kuyruğun beklemesi.
- Velopack kurulum paketi ve güncelleme.
- Okunuş "Dinle" düğmesiyle yerel sentez (deneme kutusu artık sentez yapmıyor; Dinle akışı otomasyonda tıklanmadı).

## 30 saniyelik yöntem matrisi (kurgu30) — 14 Kas 2026

Her yöntem (motor × yerel ses × yerel görsel) aynı sabit kurguyla, gerçek boru hattından geçirilip doğrulanır.

- **Kurgu:** `tests/e2e/mock-llm/kurgu30.cjs` — İstanbul'un fethi, 4 sahne, 51 yazılı kelime (Supertonic 3 ile ~31 sn). Anlatımda "Google", "29 Mayıs 1453", "II. Mehmed", "%40", "XV. yüzyıl" geçer; her sahnenin İngilizce `visualPrompt`'u var. Mock, konsept isteğinde "kurgu30" görünce bunu döner; konsept `kurgu30` etiketini taşır.
- **Sahne kodu:** `tests/e2e/mock-llm/scene-code.cjs` — sistem promptundan motoru tanır (HyperFrames HTML+GSAP, Remotion TSX, Revideo TSX). Başlık + ekran metni kartı, görsel verilirse tam ekran + Ken Burns + okunurluk katmanı (yoksa degrade + şekiller). Görsel yolu promptta `assets/<ad>` olarak gelir: HyperFrames `assets/<ad>`, Remotion `staticFile("assets/<ad>")`, Revideo `'/assets/<ad>'`.
- **Koşucu:** `tests/e2e/matrix.cjs` (API + bearer). Şunları yapar:
  1. Üretim ayarlarını yazar.
  2. Onaylı kurgu30 konseptini bulur ya da üretip onaylar.
  3. Konsepti sıraya ekler, başlatır ve aşamaları yazdırır.
  4. Doğrular: ffprobe (h264+aac, 1080×1920, 25–40 sn), 4 kare, volumedetect (video + `speech.wav`), API `spokenText` okunuşları (Gugıl, yirmi dokuz Mayıs bin dört yüz elli üç, İkinci Mehmed, yüzde kırk, On beşinci yüzyıl). İsteğe bağlı olarak faster-whisper ile anlaşılırlığı ölçer.
  5. Çıktıları `tests/e2e/out/kurgu30/<etiket>/` altına yazar: `result.json`, `video.mp4`, `speech.wav`, `frames/`, `detail.json`, `spoken.txt`, `whisper.json`.
- **Motor ve model kurulumu:** ajan aşama yürütücüleri motoru gerektiğinde `EnsureEngineReadyAsync` ile kurar (SceneRender, Speech, Images). İlerleme aşama yüzdesi olarak görünür. Elle kurulum gerekmez.

```
node tests/e2e/stack.cjs start everything
node tests/e2e/matrix.cjs --label hyperframes_supertonic3_img-off --new-concept           # taban çizgisi (temiz)
node tests/e2e/matrix.cjs --label remotion_supertonic3_img-off --engine remotion
node tests/e2e/matrix.cjs --label hf_supertonic3_zimage --image-mode Local --image-model <anahtar>
node tests/e2e/matrix.cjs --label hf_piper --speech-model piper-tr [--voice <id>]
node tests/e2e/matrix.cjs ... --whisper <uv.exe yolu>                                     # faster-whisper (CPU int8, model önbelleği .tools-dev/e2e-agent/Models/hf-cache)
node tests/e2e/stack.cjs stop everything
```

Notlar:
- Aynı konsept + aynı ayarla yeniden koşmak önceki işin aşamalarını yeniden kullanabilir. Temiz süre ölçümü için `--new-concept` kullanılır.
- Çıkış kodları: 0 = geçti, 1 = denetim başarısız, 2 = üretim başarısız ya da zaman aşımı.

| Yöntem | Sonuç | Süre (video / seslendirme) | Render (iş başı → son) | Not |
|---|---|---|---|---|
| HyperFrames + Supertonic 3 + görsel kapalı + altyazı + çubuk | ✅ | 31,27 / 31,24 sn | 80,2 sn (SceneRender 62,6 · Speech 7,4 · Compose 9,6) | whisper small kelime geri çağırma 0,865 |
| Remotion + Supertonic 3 + görsel kapalı + altyazı + çubuk | ✅ (düzeltmeyle) | 31,27 / 31,24 sn | 91,4 sn (SceneRender 71,5 · Speech 8,3 · Compose 10,6) | whisper 0,865; çıktı artık `yuv420p` tv/bt709 |
| Revideo + Supertonic 3 + görsel kapalı + altyazı + çubuk | ✅ (düzeltmeyle) | 31,27 / 31,24 sn | 53,3 sn (SceneRender 24,5 · Speech 10,2 · Compose 17,4) | whisper 0,865; ölü proxy ağ engeli doğrulandı |

Bu turda düzeltilen hata: HyperFrames lint, görsel kullanan her sahneyi `missing_local_asset` ile reddediyordu, çünkü doğrulama klasörüne yalnız `index.html` yazılıyordu. Artık görseller doğrulama klasörünün `assets/` klasörüne de kopyalanıyor. Entegrasyon testi: `HyperFrames_ValidateAcceptsSceneWithImageAsset`.

### Remotion ve Revideo turu (14 Kas 2026)

İkisi de `--new-concept` ile, gerçek boru hattından geçti; tüm denetimler geçti (ffprobe, 4 kare, volumedetect, 5 okunuş, whisper). Bulunan ve düzeltilen hatalar:

- **Renk aralığı:** Remotion/Revideo sahneleri JPEG karelerinden gelen tam aralıkta (`yuvj420p`, `pc`) çıkıyor, birleştirici bunu son videoya taşıyordu. Bazı oynatıcı ve platformlar bu dosyaları soluk ya da patlak gösterebilir. Normalize adımı artık sınırlı aralığa (`out_range=tv`) ve BT.709'a çeviriyor, çıktılar tv/bt709 etiketleniyor. Her motorda son dosya `yuv420p` oluyor. Testler: `Compose_FullRangeScene_ProducesLimitedRangeYuv420p` (yeniden kodlama ve kopyalama yolu), `BuildNormalizeFilter_ConvertsToLimitedRangeBt709`.
- **Revideo yazı tipi yarışı:** düzen (layout), yazı tipi yüklenmeden yedek yazı tipinin ölçüleriyle hesaplanıp önbelleğe alınıyordu. Montserrat 900 ile iki satıra kayan kart metninin ikinci satırı kartın dışına taşıyordu (sahne 3 "yaşında", sahne 4 "bile"). `project.ts` artık paketli tüm yazı tipi ağırlıklarını sahneden önce yüklüyor (`document.fonts.load`; Remotion'daki `delayRender` önyüklemesinin karşılığı). Test: `Revideo_LayoutCardGrowsForWrappedSecondLine`. Kartın iç boşluğundan piksel okuyor; düzeltme olmadan rgb 16,16,16 ile başarısız oluyor.
- **Penceresiz ajan kapanışı:** `stack.cjs stop` sırasında ajan `AgentHubConnection type only implements IAsyncDisposable` ile çöküyordu. Servis kapsayıcısı artık `DisposeAsync` ile kapatılıyor; kapanış temiz.

**Revideo ölü proxy doğrulaması:** render betiği kaynaktaki `RenderScript` ile birebir çalıştırıldı. Sahne, makinenin LAN adresindeki (192.168.1.100:7399) yerel bir HTTP sunucusuna `fetch` ve `Image` isteği, example.com'a da `fetch` gönderdi. `--proxy-server=http://127.0.0.1:9` ile sunucuya hiç istek düşmedi, dış istek `EXTERNAL-BLOCKED` oldu ve render yine mp4 üretti. Bayraklar kaldırılınca (kontrol) istekler sunucuya ulaştı ve dış istek geçti. Kayıt: `tests/e2e/out/kurgu30/revideo_supertonic3_img-off/proxy-probe-hits.txt`. Gerçek boru hattındaki Revideo render'ı da bu betikle başarılı.

### Hafif yerel Türkçe ses turu (14 Kas 2026)

HyperFrames + görsel kapalı + altyazı + çubuk; her yöntem `--new-concept` ile gerçek boru hattından geçti. Python ortamı ajanın kendi yoluyla kuruldu (ses aşaması: uv → `PythonEnvs/<anahtar>/.venv` → paketler → `tts_worker.py`; model ilk yüklemede HF önbelleğine iner). Whisper için ajanın kurduğu uv kullanıldı: `--whisper .tools-dev/e2e-agent/Tools/uv/uv.exe`.

| Yöntem | Sonuç | Süre (video / seslendirme) | Render | Speech aşaması | whisper (yazılı) | Not |
|---|---|---|---|---|---|---|
| Piper DFKI (`piper-tr`, `tr_TR-dfki-medium`) | ✅ (düzeltmeyle) | 26,41 / 26,41 sn | 628 sn soğuk (uv + Python 3.11 + paket + ses indirme) | 340 sn soğuk; sentez ~1,6 sn | 0,808 | uv'yi ajan kendisi kurdu; tepe 0 dBFS (6 örnek) |
| Piper Fahrettin (`tr_TR-fahrettin-medium`) | ✅ (düzeltmeyle) | 23,27 / 23,24 sn | 87,1 sn | 4,6 sn | 0,769 | hızlı konuşuyor: 25 sn alt sınırının altında, içerik aynı |
| Piper Fettah (`tr_TR-fettah-medium`) | ✅ | 28,50 / 28,49 sn | 105,3 sn | 5,7 sn | 0,731 | ayna deposundan indi |
| MMS-TTS Türkçe (`mms-tts-tr`) | ✅ (düzeltmeyle), anlaşılırlık zayıf | 34,37 / 34,37 sn | 93,4 sn (ilk, soğuk: 194 sn) | 13,1 sn | 0,673 | model kendi telaffuzu bulanık ("Gubil aritasında") |
| EMA Lightning (`ema-lightning`) | ✅ | 23,00 / 23,00 sn | 240,2 sn soğuk | 127,5 sn soğuk (torch kurulumu 86 sn) | 0,827 | hızlı konuşuyor (23 sn) |
| Ahenk Cep (`ahenk-cep`) | ✅ | 25,32 / 25,32 sn | 193,6 sn soğuk | 102,2 sn soğuk; sentez ~8 sn | 0,846 | en anlaşılır hafif motor |
| Trendyol-TTS | ⏭ atlandı | — | — | — | — | katalogda en az 8 GB VRAM (VoxCPM tabanlı); makinede 6 GB, OBS ile ~4 GB boş; hafif/CPU motoru değil |

Bu turda düzeltilen hatalar:
- **uv yoksa Python motorları düşüyordu:** web'den Piper/MMS/EMA seçilince ses aşaması "uv bulunamadı. Kurulum ekranından 'uv' bileşenini kurun." ile başarısız oluyordu; uv isteğe bağlı bir bileşen olduğu için yalnız masaüstü kurulum ekranından kurulabiliyordu. Yeni `IToolProvisioner` (Abstractions) → `ToolProvisionerBase` → `PrerequisiteToolProvisioner` (Agent.Core) eksik aracı ilgili ön gereksinim bileşeniyle kurar; `PythonSpeechEngineBase` uv yoksa onu kullanır. Testler: `PrerequisiteToolProvisionerTests` (4), `EnsureReady_provisions_missing_uv`.
- **uv Python indirmesinde Windows dosya kilidi:** ilk kurulumda `failed to rename … Erişim engellendi (os error 5)` (yeni açılan klasörü tarayıcı kısa süre kilitliyor). Bu sefer sunucunun aşama yeniden denemesi kurtardı. Artık uv komutları geçici kilitte 3 ve 10 sn arayla iki kez yeniden denenir (`IsTransientFileLock`). Test: `EnsureReady_retries_uv_on_transient_file_lock`.
- **Piper Fahrettin/Fettah 404:** `rhasspy/piper-voices` main dalında artık yalnız `tr_TR-dfki-medium` var. Adapter resmi depoda bulamazsa `speaches-ai/piper-<ses>` aynasından (`model.onnx` + `config.json`) indirir.
- **MMS-TTS noktalama ve İ/I:** sözlükte noktalama yok (sahne tek nefeste okunuyordu) ve tokenizer `str.lower()` ile İ'yi "i + birleşik nokta", I'yı "i" yapıyordu. Adapter artık Türkçe küçük harfe çevirip cümle/virgül parçalarıyla sentezler, araya kısa duraklar koyar. Whisper dökümünde cümleler ve "Henüz", "1453" düzeldi; skor 0,673'te kaldı (kalan hata modelin telaffuzu).

### GPU yerel Türkçe ses turu (14 Kas 2026)

HyperFrames + görsel kapalı + altyazı + çubuk; her yöntem `--new-concept` ile gerçek boru hattından geçti (API kuyruğu → orkestratör → penceresiz ajan). Yöntemler sırayla koşuldu. Her koşudan sonra işçinin kapandığı ve VRAM'in boşaldığı `nvidia-smi` ile izlendi (`tests/e2e/out/kurgu30/logs/vram-*.csv`). Makine RTX 3060 Laptop 6 GB; OBS vb. açıkken boş VRAM ~4,5 GB, boş RAM ~2,7 GB (toplam 15,7 GB).

| Yöntem | Sonuç | Süre (video / ses) | Render (iş başı → son) | Speech aşaması | VRAM tepe (toplam) | whisper (yazılı) | Not |
|---|---|---|---|---|---|---|---|
| Chatterbox Multilingual (`chatterbox-multilingual`) | ✅ (düzeltmeyle) | 23,04 / 23,04 sn | 206,6 sn | 102,6 sn (setuptools ekleme kurulumu + ~60 sn torch içe aktarma + yükleme) | 5,7 GB | 0,712 | hızlı konuşuyor: 25 sn alt sınırının altında, içerik tam ("surları"→"soruları" telaffuzu) |
| XTTS-v2 (`xtts-v2`, Ana Florence) | ✅ (düzeltmeyle) | 31,59 / 31,59 sn | 157,0 sn | 73,8 sn (model önbellekte; yükleme ~60 sn) | 4,5 GB | 0,827 | en doğal tempo |
| F5-TTS Türkçe (`f5-tts-tr`) | ✅ (düzeltmeyle) | 25,37 / 25,35 sn | 126,7 sn | 48,8 sn | 3,4 GB | 0,712 | düzeltmeden önce 44,4 sn ve aksanlıydı |
| OmniVoice (`omnivoice`) | ✅ | 25,23 / 25,22 sn | 337,7 sn soğuk | 253,7 sn soğuk (torch + model indirme) | 3,6 GB | 0,827 | rakamlar ve "%40" doğru okunuyor |
| VoxCPM2 (`voxcpm2`) | ⏭ atlandı | — | — | — | 5,7 GB (taşma) | — | GPU: model yüklendi ama ~4,5 GB boş VRAM'e sığmadı, paylaşılan belleğe taştı; sahne 0 (6,6 sn ses) 7 dk sürdü → 30 sn ≈ 30 dk (sınır 10 dk). CPU: `CUDA_VISIBLE_DEVICES=-1` ile model ağırlığı yüklenirken segfault (boş RAM ~2,7 GB). İş durduruldu, işçi kapandı. |
| Ahenk (`ahenk`, VoxCPM2 2B + Türkçe LoRA) | ⏭ atlandı | — | — | — | — | — | VoxCPM2 tabanı aynı (katalogda ≥ 8 GB); VoxCPM2'nin GPU ve CPU sonucu bunu da kapsar. Hafif sürümü Ahenk Cep önceki turda geçti. |
| Fish S2 Pro (`fish-s2-pro`) | ⏭ atlandı | — | — | — | — | — | ağırlıklar 9,1 GB + codec 1,9 GB (≥ 12 GB VRAM); ne 6 GB VRAM'e ne ~2,7 GB boş RAM'e sığar, CPU yolu da yüklenemez. Koşulmadı. |
| Kokoro (`kokoro`) | ⏭ render atlandı; uyarı doğrulandı | — | — | 4 sn'de açık hata | — | — | Türkçe yok. Etiket: katalog "Türkçe yok" (arayüzde kırmızı rozet), ajan "Türkçe yok (İngilizce vb.)". Türkçe içerikle seçilince ses aşaması kurulum yapmadan durur: "Kokoro Türkçe seslendiremez; dil desteği: …". Kokoro ortamı kurulmadı. Arayüzde model seçilince kırmızı uyarı kutusu çıkar. |

Bu turda düzeltilen hatalar:
- **Chatterbox yüklenmiyordu:** `TypeError: 'NoneType' object is not callable`. Filigran paketi `resemble-perth` `pkg_resources` içe aktarıyor. uv venv'inde setuptools yok, 81+ sürümünde de `pkg_resources` kaldırıldı; bu yüzden `PerthImplicitWatermarker` sessizce `None` kalıyordu. Gereksinimlere `setuptools<81` eklendi. Test: `Chatterbox_requirements_include_pkg_resources_provider`.
- **XTTS-v2 yüklenmiyordu:** `No module named 'six'`. coqui-tts 0.28.0 `punctuation.py` içinde `import six` yapıyor ama bağımlılık olarak bildirmiyor. Gereksinimlere `six` eklendi. Test: `Xtts_requirements_include_six`.
- **F5-TTS Türkçe yavaş ve aksanlıydı:** referans ses verilmeyince kütüphanenin İngilizce örnek klibiyle klonluyordu (anlatım 31 yerine 44,4 sn). F5 referansın temposunu ve aksanını kopyalar. Ajan artık 6,7 sn'lik Türkçe referans klibini (`PythonWorkers/assets/f5_ref_tr.wav`, Supertonic 3 M1 sesi) ve metnini getiriyor ve varsayılan olarak kullanıyor. Sonuç 25,4 sn. Test: `F5_default_turkish_reference_is_shipped_with_worker`.
- **GPU ses modeli VRAM'de kalıyordu:** Python işçisi ses aşamasından sonra açık kalıyordu (Chatterbox ~4 GB). Images aşaması Speech'ten önce çalıştığı için sonraki işin yerel görsel aşaması belleksiz kalırdı. `ISpeechEngine.ReleaseAsync` eklendi. Ses aşaması bitince (başarı ya da hata) GPU motorunun işçisi kapanıyor; CPU motorları sıcak kalıyor. Canlı doğrulama: Chatterbox'ta VRAM, aşama bitişinden 2 sn sonra 4,2 GB'tan 2,0 GB'a indi. Testler: `Release_stops_worker_only_for_gpu_engines` (GPU/CPU), `Speech_ReleasesEngine_EvenWhenSynthesisFails`.
- **Türkçe desteklemeyen motor önce kuruluyordu:** Kokoro seçilince ajan önce ~2 GB torch ortamını kuruyor, ancak sentezde "dili desteklemiyor" diyordu. Dil kontrolü artık kurulumdan önce yapılıyor ve Türkçe önerili açık bir mesaj veriyor. Arayüzde de "Türkçe yok" model seçilince uyarı çıkıyor. Test: `Speech_UnsupportedLanguage_FailsBeforeInstall`.
- **Koşucu (matrix.cjs):** erişim belirteci ~15 dk'da doluyor ve uzun (soğuk GPU) koşularda `401` ile kopuyordu. 401 alınca bir kez yeniden giriş yapılıyor.

### Yerel görsel turu (14 Kas 2026)

HyperFrames + Supertonic 3 + altyazı + çubuk; her yöntem `--new-concept` ile gerçek boru hattından geçti (API kuyruğu → orkestratör → penceresiz ajan → sd.cpp). Ajan sd.cpp'yi (master-1098, CUDA 12) ve her modelin dosyalarını **kendisi indirdi** (`.tools-dev/e2e-agent/Tools/sd-cpp`, `.tools-dev/e2e-agent/Models/image`; Z-Image dosyaları eski `.tools-dev/models/image`'dan sabit bağlantıyla alındı). Makine: RTX 3060 Laptop 6 GB, bu turda boş VRAM ~5,3 GB, boş RAM ~5,5 GB. VRAM tepe (toplam) 5,57 GB (`tests/e2e/out/kurgu30/logs/vram-images.csv`).

`matrix.cjs` artık işin `images/` klasörünü yöntem klasörüne kopyalar ve denetler: görsel sayısı = sahne sayısı, her görsel **tam 1080x1920**; görsel kapalıyken hiç görsel yok.

| Yöntem | Sonuç | Üretim boyutu | Görsel başı | Images aşaması | Render (iş başı → son) | whisper | Not |
|---|---|---|---|---|---|---|---|
| Z-Image Turbo (`z-image-turbo`) | ✅ | 1080x1920 | ~80 sn (Standard) | 365 sn (sd.cpp soğuk kurulum ~37 sn dahil) | 461 sn | 0,846 | yüksek ayrıntı |
| FLUX.2 klein 4B (`flux2-klein-4b`) | ✅ (düzeltmeyle) | 1080x1920 (sd.cpp 1088 verir → kırpılır) | ~29 sn (Standard) | 119 sn | 214 sn | 0,827 | en hızlı |
| SDXL-Lightning (`sdxl-lightning`) | ✅ (düzeltmeyle) | 768x1344 → 1080x1920 | ~12 sn | 49 sn | 169 sn | 0,769 | istem uyumu zayıf (top → sütun) |
| SD3.5 Medium (`sd35-medium`) | ✅ | 768x1344 → 1080x1920 | ~65 sn (Standard) | 374 sn (5,7 GB indirme dahil) | 498 sn | 0,865 | |
| FLUX.3 Image Klein (`flux3-klein`) | ✅ | 1080x1920 | ~92 sn (Reduced) | 466 sn (6 GB indirme dahil) | 571 sn | 0,846 | istem uyumu en iyi (genç sultan + top) |
| FLUX.2 klein 9B (`flux2-klein-9b`) | ✅ | 1080x1920 | ~88 sn (Reduced) | 483 sn (10,6 GB indirme dahil) | 595 sn | — | metin kodlayıcı CPU'da; boş RAM ~5 GB yetti |
| Qwen-Image (`qwen-image`) | ⏭ atlandı | — | — | — | — | — | en küçük GGUF Q2_K 7,06 GB + Qwen2.5-VL-7B 4,7 GB ≈ 11,8 GB; 6 GB VRAM + ~5,5 GB boş RAM'e sığmaz (Q4_0 11,85 GB) |
| Görsel kapalı (boyut denetimi) | ✅ | — | — | sunucuda geçiş ("Kapalı (koddan çizim)") | 73 sn | — | 0 görsel, degrade + şekiller |

Her yöntemde 4 kare incelendi: görsel sahnenin tamamını kaplıyor (Ken Burns + okunurluk katmanı), başlık ve kart üstte, sarı karaoke altyazı alt bölgede, turuncu ilerleme çubuğu üstte, Türkçe karakterler doğru. Tüm okunuşlar (Gugıl, yirmi dokuz Mayıs bin dört yüz elli üç, İkinci Mehmed, yüzde kırk, On beşinci yüzyıl) doğru; ses ve görüntü 31,27 / 31,24 sn.

Bu turda düzeltilen hatalar:
- **Görsel istenen boyutta değildi:** sd.cpp FLUX.2'de genişliği 16'nın katına yuvarlıyor; 1080x1920 isteğine 1088x1920 verdi ve ajan bunu olduğu gibi geçiriyordu. Yeni `IImageFitter` → `ImageFitterBase` → `FfmpegImageFitter` (Agent.Core/Media): Images aşaması her görseli kontrol noktasından önce isteğin tam boyutuna getirir (kapla + ortadan kırp, esnetme yok; boyut zaten doğruysa dokunmaz). Tüm görsel motorlarını kapsar. Testler: `FfmpegImageFitterTests` (gerçek ffmpeg ile 1088→1080), `Images_FitsEachGeneratedImageToRequestedSize`, `Images_FitFailure_FailsStage`.
- **SDXL 2 MP'de nesneleri ikiliyordu:** 1080x1920 (2 MP) üretimde 3. sahnede iki sultan çıktı (SDXL ~1 MP'de eğitildi). Katalogda `maxPixels` (SDXL ve SD3.5 için 1 MP); sd.cpp motoru bu modellerde aynı oranda, 64'ün katı boyutta (768x1344) üretir, Images aşaması tam boyuta büyütür. SDXL görseli 18 sn'den 12 sn'ye indi, tek figür. Testler: `ImageEngineBaseSizeTests`, `Generates_within_model_pixel_budget`, `Generates_requested_size_without_budget`.
- **Katalog VRAM değerleri:** FLUX.3 klein ve FLUX.2 klein 9B "en az 8 GB" yazıyordu; 6 GB kartta (5,3 GB boş) azaltılmış kipte ~90 sn/görselle çalıştılar → 6144 MB. Sunucu kataloğundaki "doğrulanmadı" notları ölçülen sürelerle değiştirildi.

