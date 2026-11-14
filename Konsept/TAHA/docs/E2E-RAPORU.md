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
node tests/e2e/e2e.cjs a b c d e f g h pages i
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
- Yerel görsel (Z-Image Turbo) ve Remotion/Revideo motorları. Görsel kapalıydı, motor HyperFrames'ti.
- "Yeniden üret" (konsept düzenleyip baştan üretme), bütçe limiti engeli ve ajan çevrimdışıyken kuyruğun beklemesi.
- Velopack kurulum paketi ve güncelleme.
- Okunuş "Dinle" düğmesiyle yerel sentez (deneme kutusu artık sentez yapmıyor; Dinle akışı otomasyonda tıklanmadı).
