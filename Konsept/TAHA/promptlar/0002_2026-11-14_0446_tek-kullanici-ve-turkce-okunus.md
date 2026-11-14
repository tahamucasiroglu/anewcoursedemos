# 0002 — Tek kullanıcı, tüm modeller, Türkçe okunuş

- **Zaman:** 2026-11-14 04:46 (+03:00)
- **Konu:** Lisans sorularının kaldırılması, tüm ses/görsel modellerinin eklenmesi, Türkçe okunuş düzeltme

## İstek

bak açık sorularda hala lisans diyon tekim tek ben kullanacak. seslendirmelerin ve görsellerin hepsini al. programa eklemeye izin verilenlerden şahsi kullanım programı olarak ticari değil. pi dışarı açık olmayacak localde sadece ben erişeceğim şuan sadece ben gibi geliştir. türkçe tarih gibi okuma hatalarını düzeltelim. google yi gugıl gibi okumalı mesela. kalan plan iyi gibi.

## Cevap

Haklısın, lisans sorularını kaldırdım. Lisansı bir daha soru olarak açmamak için memory'ye kural da ekledim.

**PLAN.md'de değişenler:**
- **Kullanım:** Tek kullanıcı, şahsi, ticari değil, sadece yerel ağ. Kayıt kapalı; kurulumda tek hesap açılır. Login kalıyor, çünkü masaüstü programını web'e eşlemek ve web oturumu için gerekli. Çok kullanıcılı ekranlar öne çıkmıyor.
- **Ses modelleri (hepsi):**
  - Yerel: Supertonic 3, Chatterbox, EMA Lightning, Ahenk, Trendyol-TTS, XTTS-v2, F5-TTS TR, Piper tr, MMS-TTS-tr, OmniVoice, VoxCPM2, Fish S2 Pro, Kokoro.
  - Online: OpenAI, ElevenLabs, Gemini, Azure, Cartesia.
  - "Sadece Türkçe" olanlar etiketli.
- **Görsel modeller (hepsi):**
  - Yerel: Z-Image Turbo, FLUX.2 klein 4B ve 9B, FLUX.3 Klein, SD3.5 Medium, SDXL-Lightning, Qwen-Image. İsteğe bağlı ComfyUI.
  - Online: Nano Banana 2.1, Gemini Flash Lite Image ve Pro Image, gpt-image-2 ve 1-mini, fal.ai, BFL, GLM-Image, Replicate.
- **VRAM etiketi:** 6 GB'a sığmayan modeller (VoxCPM2, Fish, Qwen-Image, FLUX.3) listeden gizlenmez, "VRAM yetersiz" etiketiyle gösterilir. Ajan bilgisayardaki ekran kartını kendisi algılar.
- **Ollama Phonon** yok. Sebep lisans değil, teknik: Windows'ta çalışmıyor ve Türkçe desteklemiyor.

**Türkçe okunuş:** Pipeline'a seslendirmeden önce çalışan yeni bir **Normalize** aşaması eklendi (PLAN.md §3.1). Her sahnenin iki metni olacak:
- **Yazılış:** Ekranda ve altyazıda görünür. Örnek: "Google", "29 Mayıs 1453", "II. Mehmed".
- **Okunuş:** Seslendirmeye gider. Örnek: "Gugıl", "yirmi dokuz mayıs bin dört yüz elli üç", "İkinci Mehmed".

Düzeltmeler şu sırayla uygulanır:
1. Okunuş sözlüğü: Ayarlar'da düzenlenir, global ya da profile özel olabilir; yanında "Dinle" butonu olur.
2. Sayılar, eklerle birlikte (ör. 1453'te → bin dört yüz elli üçte).
3. Tarihler ve MÖ/MS.
4. Romen rakamları ve sıra sayıları.
5. Kısaltmalar (yy. → yüzyıl, TBMM → harf harf).
6. LLM'in konsept üretirken yabancı isimler için önerdiği okunuşlar. Ek bir çağrı yapılmaz; öneriler sözlüğe düşer, onaylarsan kalıcı olur.

Sözlüğü değiştirirsen sadece seslendirme ve sonrası yeniden çalışır. Kalan plan aynı. Kod kuralları ve memory güncellendi, commit alındı.
