# Ürün Tanımı — Taha Konsept (özet, kaynak: promptlar/0001)

**Amaç:** Sosyal medya kanalları (YouTube ve diğer dikey/yatay platformlar) için video **konsepti** üretmek ve konsepti videoya çevirmek. Konsept = sahne başlıkları + ekranda gösterilecekler + seslendirme metni (videonun tüm kurgusu/fikri).

## Bileşenler
- **Web (Pi üzerinde):** her şeyin yönetildiği yer (API key girişi, üretim, izleme). MVC → API → PostgreSQL.
- **Masaüstü ajanı (Avalonia, Windows):** görev çubuğunda (sağ alt) ikon; sağ tık: "Siteye git", "Çıkış". Render, yerel TTS, yerel görsel üretimi bu bilgisayarda. Kurulum/güncelleme ekranı profesyonel görünür; format atılmış PC'ye bile gerekli araçları (ör. Ollama, ffmpeg…) kurar. Web'deki login'i takip eder (web'den çıkış → ajan da yetkisiz).
- **Admin panel:** kullanıcı listesi, toplam üretilen klip, aktiviteler, ban, bildirilen hatalar, grafikler. Öncelik düşük.
- Şu an **tek kullanıcı** (sadece Taha, şahsi, yerel ağ). Rol/izin altyapısı ileride genişlemek için kodda durur.
- Türkçe okunuş: Normalize aşaması + Okunuş sözlüğü (Ayarlar), ör. Google → Gugıl.

## Model sınıfları
- Konsept, konu önerisi, video kodu: **online LLM** (Anthropic varsayılan; OpenAI, OpenRouter, GLM, Kimi…). Performans/efor seçimi (low→max).
- Video motoru: HyperFrames (varsayılan), Remotion, Revideo (+ araştırmadan çıkan alternatifler).
- Görsel: kapalı (sadece koddan çiz) / yerel Z-Image Turbo, FLUX.2 Klein 4B (6 GB VRAM) / online Nano Banana, fal.ai, GPT…
- Ses: kapalı (sessiz) / yerel Supertonic, Chatterbox Multilingual / online (GPT-4o TTS vb. pahalı). Dil kısıtı varsa "Sadece Türkçe/İngilizce" etiketi.

## Sayfalar
1. **Konseptler:** model bilgisi şeridi (tık → modal: konsept üretimi, kelime tekrar denetimi, konu önerisi modelleri) · ince ayar (tık → modal: ton, izleyici, anlatım biçimi) · kelime denetimi checkbox.
   - Sol üst **Konudan üret:** 1–100 adet sadece başlık önerilir (ucuz model), seçilenler "Seçtiklerini üret" ile detaylandırılır. Daha önce üretilen/silinen/beğenilmeyen konular tekrar önerilmez (token-optimize hafıza; Ayarlar'da "Önerileri sıfırla").
   - Sağ üst **Fikirden üret:** serbest metin + 1–100 adet → doğrudan Üretilenler'e.
   - Sol alt **Üretilenler** (✏️ düzenle, 🗑 sil). Sağ alt **Düzenleme alanı:** metinleri doğrudan düzenle ya da "şunları düzelterek tekrar üret" talimatı → LLM → sonuç **modal**da: Seç / Sil / Bunu da düzenle (döngü).
   - Gerçek bilgi: tarih/spor vb. konularda uydurma yok, gerçek verilerle.
2. **Konseptten Video:** model seçimi (video kodu modeli + efor, video motoru, görsel, ses) · sağ üst: altyazı var/yok, üst ilerleme çubuğu var/yok. Sol: onaylanan konsept listesi (tık → altta ayrıntı, düzenlenebilir; tik → üretim sırasına). Orta: **üretim sırası** (↑ ↓ ✕, "Sırayla üret", durdur / kaldığı yerden devam). Aşamalar adım adım gösterilir (konsept inceleniyor → kod → görseller → ses → render…). Sağ: üretilenler; tıklayınca kesik çizginin sağında oynatıcı. Örnek ölçek: 200 video tek seferde (~$1/video).
3. **Üretilen Videolar:** sol liste · orta oynatıcı (ses, tam ekran) + Klasörde aç, Sistem oynatıcısında aç, Yeniden üret (önce düzenle), Sil · sağ: seslendirme+sahneler, konsept metni, ayrıntılar ve ücret · **Yeni sürüm:** sadece değişen aşamayı yeniden çalıştır (ör. TTS motorunu değiştir → sadece ses + birleştirme; motor değiş → sadece video; metin değiş → hepsi).
4. **Silinenler:** sonsuza kadar saklanır; buradan silinirse tamamen silinir. Negatif örnek olarak konu önerisine etki eder.
5. **Yayınlama:** "Çok yakında sizlerleyiz" (ileride: yayın zamanlama/destek).

## Profil (kanal)
- Kanal adı, video dili, biçim (yatay/dikey), hedef süre, kanal içeriği (odak, tarih aralığı… → sistem promptuna optimize şekilde girer). Örnek görsel YOK.
- Hazır şablonlar: Tarih-Bizans, Tarih-Osmanlı, Teknoloji-Antik, Teknoloji-Günümüz vb.
- Üst barda dropdown ile seçilir, ✏️ düzenler, ➕ ekler.

## Günlük / Log
- Alt şeritte son işlemler (süre, başlangıç/bitiş). Seviyeler: Info/Warning/Error/Critical. Ajan logları 7 gün yerel; kullanıcı işlemleri/hatalar sunucuda kalıcı (günlük bazlı). Hata bildirimi → API → admin panel.
