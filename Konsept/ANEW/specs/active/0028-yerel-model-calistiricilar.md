# Spec 0028 — Yerel model çalıştırıcıları

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0028-plan.md`
- Self-critique: done (Approved under the user's explicit request (2026-11-17))
- Source: backlog B-116 (High) + B-73, B-74/B-117 (GPU tespiti kalanı), B-75 (isteğe bağlı yerel model bileşenleri); kullanıcı
  2026-11-17: profilde Z-Image-Turbo ve Supertonic 3 seçili, video işleri "kuramıyor" mesajıyla duruyor — "modele özel çözüm değil
  genel çözüm lazım"
- Approval: Approved under the user's explicit request (2026-11-17)
- Sensitive area: indirme bütünlüğü (her dosya SHA-256 + boyut, ajana gömülü liste; sunucudan adres/hash gelmez), ajanın dış süreç
  çalıştırması (yalnızca gömülü listedeki bileşenlerin dosyaları, sabit argüman şablonu; kullanıcı metni yalnızca argüman değeri)
- Depends on: 0009 (kurulum motoru), 0017 (üretim hattı), 0018 (render, kodlayıcı yoklaması), 0027 (eksik bileşen indirme onayı)

## Intent
Katalogdaki her yerel görsel ve ses modeli (Z-Image-Turbo, FLUX.2 klein 4B, Supertonic 3, Chatterbox Multilingual V3) ajan
bilgisayarında gerçekten çalışmalı: üretim modeli ilk kez istediğinde 0027 indirme onayı gerçek boyutlarla açılır, onaydan sonra ajan
modeli ve çalışma ortamını kendi klasörüne indirir, doğrular ve sahne görsellerini/seslendirmeyi üretir; ses videoya karışır,
altyazı sesin süresine göre zamanlanır. Çözüm genel: modeller tek bir **bileşen listesi + çalıştırma şablonu** mekanizmasıyla tanımlanır,
model başına el yazımı çalıştırıcı yoktur — görsel türü için tek motor (stable-diffusion.cpp), ses türü için tek betik (taşınabilir
Python + kilitli paketler). Ekran kartı gerektiren model, kartı olmayan bilgisayarda açık bir mesajla durur ve seçim ekranında bu
gereksinim önceden görünür. Remotion (lisans uyarılı video motoru) bu işin konusu değildir.

## Requirements
1. Ajanın gömülü bileşen listesi yerel modeller ve onların ortak çalışma ortamları için isteğe bağlı (`required: false`, ilk kurulumda
   Atlandı) girdiler içerir: taşınabilir Python, paket kurucu (uv), ses ortamları (ONNX; PyTorch), görsel motoru (stable-diffusion.cpp
   Vulkan), ortak metin kodlayıcı (Qwen3 4B) ve dört model. Her girdinin sabit sürümü, resmi adresi, SHA-256'sı ve gerçekçi indirme /
   kurulu boyutu vardır; lisansı yazılıdır.
2. Bir bileşen başka bileşenlere bağlı olabilir. Araç denetimi istenen modelin kurulu olmayan bağımlılıklarını da eksik sayar ve
   kurulabilir bilgisine ekler; böylece 0027 penceresi indirilecek her şeyi (ad, boyut, lisans) tek listede gösterir. Kurulum liste
   sırasıyla yapılır (önce bağımlılıklar).
3. Çok dosyalı bileşen (model ağırlıkları): her dosya sabit bir Hugging Face sürümünden (commit) indirilir, boyutu ve SHA-256'sı
   denetlenir; biri uyuşmazsa bileşen kurulmaz. Aynı kurulum denemesinde doğrulanmış dosya yeniden indirilmez. Açılışta bütünlük
   denetimi büyük dosyaları baştan hash'lemez: listede yazan her dosyanın varlığı ve boyutu, kurulumda yazılan dosya listesinin hash'i denetlenir.
4. Python paket ortamı: ajana gömülü, her paketi hash'li kilit dosyası (`--require-hashes`) bileşen klasörüne kurulur; kilit dosyasının
   hash'i listedekiyle aynı olmalıdır. Kurulum ve çalışma kullanıcının global Python'una, pip/uv/Hugging Face önbelleklerine ya da
   kayıt defterine dokunmaz; tüm önbellekler `%LOCALAPPDATA%\anewconcept` altındadır ve kurulumdan sonra silinir.
5. Donanım tespiti: ajan açılışta ekran kartını (ad, üretici, ayrılmış bellek) tespit eder, loga ve Kurulum ve bileşenler ekranına
   yazar. Bileşen varyantı karta göre seçilir (ör. PyTorch ortamı: yeterli belleğe sahip NVIDIA'da CUDA, yoksa CPU paketleri).
6. Ekran kartı gerektiren model (görsel modelleri; en az 4 GB ayrılmış bellek) uygun kartı olmayan ajanda araç denetiminde
   "desteklenmiyor" döner; iş hiçbir çağrı yapılmadan Hata olur, mesaj: modelin ekran kartı (en az N GB) gerektirdiği ve başka görsel
   modeli seçmesi ya da görseli kapatması. Seçim penceresinde bu modeller "Ekran kartı gerekir (≥ N GB)" etiketiyle görünür.
7. Görsel aşaması (yerel model): her sahne için sahne görsel tarifiyle (yoksa seslendirme metni) biçime uygun boyutta (yatay
   1024×576, dikey 576×1024) `image.png` üretilir; aynı tarif ve modelle üretilmiş görsel devamda yeniden üretilmez. Çalıştırma
   modelin listedeki şablonuyla yapılır (motor + argümanlar); kullanıcı metni yalnızca argüman değeridir.
8. Ses aşaması (yerel model): tüm sahnelerin seslendirmesi tek süreçte (model bir kez yüklenir) video dilinde üretilir. Her sahnenin
   sesi sahne süresine oturtulur: uzunsa en çok 1,6 kat hızlandırılır, kısaysa sessizlikle tamamlanır, sahne süresinden uzun kalan kısım
   kesilir; konuşmanın gerçek süresi kaydedilir.
9. Birleştirmede ses videoya karışır (sahne sesleri sırayla); altyazı her sahnede konuşmanın gerçek süresine göre zamanlanır (sahne
   başında başlar, konuşma bitince sahnenin geri kalanında son satır görünmez).
10. Çalıştırma hatası: süreç ekran kartı/bellek hatasıyla düşerse "Ekran kartı kullanılamadı: <model>" (CPU'ya düşülmez); diğer hatada
    aşama Hata, ayrıntı ajan logunda (iç yol cevaba girmez). Süreç Durdur'da ağacıyla öldürülür; her model çağrısının zaman sınırı vardır.
11. Çalışma ortamı: model süreçleri yalnızca ajan klasöründeki dosyalarla, çevrimdışı (Hugging Face çevrimdışı kipi), kullanıcı
    site-packages'ı kapalı çalışır; geçici dosyalar iş klasöründedir.
12. 0027 penceresi her indirilecek bileşenin lisansını da gösterir (Supertonic 3: OpenRAIL-M). Supertonic AI ses notu (0017) aynen sürer.
13. Kurulum ekranının ön kontrolü (gereken disk alanı) yalnızca kurulacak bileşenleri sayar; Atlandı (isteğe bağlı) bileşenler ilk
    kurulumda alan hesabına girmez.

## User experience
- Flow: Kullanıcı profilde Z-Image-Turbo + Supertonic 3 seçili iken "Sırayla üret"e basar → 0027 İndirme onayı penceresi: Python 3.12,
  uv, Ses ortamı (ONNX), Supertonic 3, stable-diffusion.cpp, Qwen3 4B metin kodlayıcı, Z-Image-Turbo — her biri ad, lisans, yaklaşık
  indirme ve disk boyutu; toplam. İndir ve devam et → satır "İndiriliyor ve kuruluyor: …" → aşamalar ilerler → video sesli ve
  altyazılı tamamlanır. Ekran kartı olmayan bilgisayarda Z-Image-Turbo seçiliyse iş başlar başlamaz açık mesajla Hata olur.
- States: model seçim penceresinde görsel modelleri "Yerel · Ekran kartı gerekir (≥ 4 GB)" etiketi; onay penceresi (0027) lisans
  sütunuyla; ajanın Kurulum ve bileşenler ekranında ekran kartı satırı ("Ekran kartı: <ad> · <N> GB" ya da "Ekran kartı bulunamadı —
  görsel modelleri kullanılamaz") ve isteğe bağlı satırlar "İsteğe bağlı — üretim isterse onayla indirilir".
- Design reference: none — the flow above is the design (0027 penceresi ve mevcut seçim etiketleri).
- Accessibility: yeni etiketler metin; onay penceresindeki lisans metin olarak okunur (0027 kuralları değişmez).

## Constraints & out of scope
- Remotion video motoru (lisans uyarılı) — kapsam dışı, backlog'da kalır.
- Görsel modelleri CPU'da çalıştırılmaz (dakikalar süren sahne başı üretim kullanılamaz); kartsız bilgisayarda açık mesaj.
- Ses klonlama (Chatterbox'a referans ses) yok; Chatterbox yerleşik sesle konuşur (Perth filigranı paketin kendisinde).
- Hugging Face/PyPI/GitHub için Pi aynası yok (bu bileşenler `mirrored: false`).
- Kesintiden sonra yarım dosyayı kaldığı yerden indirme (HTTP Range) yok: yarım dosya baştan iner (doğrulanmış dosyalar korunur).

## Acceptance criteria
- [ ] AC-1 — Bileşen listesi: dört yerel model ve ortamları isteğe bağlı, sabit sürüm/HTTPS/SHA-256 (dosya başına), gerçekçi boyut,
  bağımlılıklar listede ve kendilerinden önce; her modelin çalıştırma şablonu çözülebilir (motor bağımlılıkta, yer tutucular bilinen).
- [ ] AC-2 — Araç denetimi: eksik modelin kurulu olmayan bağımlılıkları eksik + kurulabilir döner (liste sırasında, lisanslı); kurulu
  olanlar dönmez.
- [ ] AC-3 — Çok dosyalı kurulum: dosyalar indirilir, boyut/SHA-256 denetlenir, uyuşmazlıkta bileşen Hata ve dosyalar kurulmaz;
  açılış bütünlük denetimi eksik/boyutu değişmiş dosyada Onarılmalı der.
- [ ] AC-4 — Python ortamı: gömülü kilit hash'i uyuşmazsa kurulmaz; kurulum uv'ye `--require-hashes`, hedef klasör, ajan içi önbellek
  ve kayıt defterine/kullanıcı yapılandırmasına dokunmayan ortamla çağrılır; önbellek sonra silinir.
- [ ] AC-5 — Donanım: varyant karta göre seçilir (NVIDIA + yeterli bellek → CUDA, aksi CPU); ekran kartı gerektiren model uygun kart
  yokken "desteklenmiyor" döner ve iş çağrısız, gereksinimi söyleyen mesajla Hata olur.
- [ ] AC-6 — Görsel aşaması: sahne başına şablondan süreç çağrılır (tarif, boyut, çıktı), `image.png` yazılır, aynı tarifle devamda
  yeniden üretilmez; GPU hatası → `gpu_unavailable`, diğer hata → `failed`, Durdur → süreç ağacı öldürülür.
- [ ] AC-7 — Ses aşaması: tek süreç tüm sahneleri üretir; sesler sahne süresine oturtulur (hızlandırma ≤ 1,6, doldurma, kesme) ve
  konuşma süresi kaydedilir.
- [ ] AC-8 — Birleştirme: ses karışır; altyazı konuşma süresine göre zamanlanır.
- [ ] AC-9 — Web: seçim penceresinde ekran kartı etiketi; onay penceresinde lisans.
- [ ] AC-10 — Ön kontrol yalnızca kurulacak (Atlandı olmayan) bileşenleri sayar.
- [ ] AC-11 — Gerçek duman testi (bu PC, geçici klasör): ortam kurulur, Supertonic 3 Türkçe ses üretir; uygun kart varsa Z-Image-Turbo
  bir görsel üretir; süreler raporlanır, geçici klasör ve önbellekler silinir.

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
