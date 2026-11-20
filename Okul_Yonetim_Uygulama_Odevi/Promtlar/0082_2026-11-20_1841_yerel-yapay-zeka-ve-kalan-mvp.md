# 0082 — Yerel yapay zekâ (Ollama + Whisper) ve kalan MVP'ye başlama

## İstek

6 ve 8 de şimdilik bizim bilgisayara ollama kuralım. gemme3:4b yada qwen gibi bir tık daha düşük model ile analizleri yapsın sesten yazı içinde whisper yeter. tam doğruya gerek yok çalışsın ve yüksek oranda doğru yapsa çeviri yeter. haftalık gelişimde bir kaç söylese bile yeter mvp için. kalanlarda zaten plan yapıldı döküman var bu sebep ile senin yapmaya bşalamanı hatta speclere paralel bile başlayabilirsin. en son ben programı kullanarak sana detaylı tekrar düzenlemeler veririm bu sayede vakitten kazanırız. olur mu?

(Soru cevapları: onaylar "Uçtan uca, ship sende"; paralellik "Analiz paralel, kod sırayla"; yapay zekâ: "sen ollamayı kur
modeli kur ve whisper kur ben pc de olamayacağım bir süre sen önce model kurulumuna bak benden yardım gerekmemeli ama kontrol et".)

## Cevap

- Ollama 0.40.16 kullanıcı kurulumuyla (winget) kuruldu, `gemma3:4b` indirildi; Türkçe deneme yanıtı 6,5 sn (yükleme dahil), %100 ekran kartı.
- Whisper: `uv` ile projeye özel Python 3.12 ortamı (`tools/ai/.venv`, git dışı), faster-whisper 1.2.1; `av<16` uyumu ve CUDA
  kütüphaneleri ortamın içine. `medium` model Türkçe deneme sesini hatasız yazdı (1,1 sn); `small` iki küçük hata yaptı.
  İki model birlikte ~4,4 GB / 6 GB ekran kartı belleği. Ayrıntı: `tools/ai/README.md`.
- Onay kapsamı (yeni, kalan bütün MVP spec'leri için): clarify = önerilerim, spec/plan onaylı, triage bende (bilgi dışındakiler
  düzeltilir), ship ve Pi yayını kullanıcıda. Spec ve planlar paralel, kodlama bağımlılık sırasıyla.
