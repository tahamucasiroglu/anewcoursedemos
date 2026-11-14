# Genel / Süreç Kuralları

## Prompt kaydı (14 Kas 2026)
- Kullanıcının **her** isteği ve Claude'un kullanıcıya verdiği **her** cevap `promptlar/` altına kaydedilir. İlk kayıt: proje başlangıç promptu.
- Dosya adı: `NNNN_YYYY-MM-DD_HHMM_kisa-baslik.md` (sıra numarası 4 hane, zaman yerel saat). İçerik: `## İstek` (kullanıcının metni aynen) + `## Cevap` (kullanıcıya verilen cevap; sorulan sorular ve şıkları, verilen cevaplar dahil).
- Claude'un kendi iç düşünceleri kaydedilmez; sadece kullanıcıya doğrudan söylenen/sorulan.
- Soru-cevap (AskUserQuestion) yapıldıysa sorular, şıklar ve kullanıcının seçtiği cevaplar da kayda girer.
- `promptlar/README.md` içindeki zaman çizelgesi tablosuna her kayıt için bir satır eklenir.

## Ses → yazı
- Kullanıcı sesle yazıyor; kelime hataları olabilir (ör. "Cloud" = Claude, "Concept" = Konsept, "eleleme" = LLM'e, "RAM metodu" = Run metodu, "OpenRoad" = OpenRouter, "Supersonic" = Supertonic). Anlamdan düzelt; emin değilsen sor.

## Klasörler
- Tüm çalışma `Konsept/TAHA/` içinde. `source/` = kaynaklar (ikon, resim, video); proje kaynakları buradan kullanılır. `memory/` = hafıza. `raporlar/` = araştırma raporları. `promptlar/` = konuşma kaydı.

## Commit (14 Kas 2026)
- Commit **`anewcoursedemos` kökünden** alınır (TAHA'nın iki üstü). **Push YOK.**
- Sadece kendi klasörü: `git add -A -- Konsept/TAHA` ardından `git commit -m "..." -- Konsept/TAHA`. Pathspec sayesinde başka klasörlerin stage'leri commit'e girmez.
- **Her adım/faz/anlamlı iş biriminin sonunda commit al — ihmal etme** (14 Kas 2026, kullanıcı uyarısı). Uzun işlerde ara commit'ler at; build kırık olsa bile ilerleme kaybolmasın (mesajda belirt).
- Bu **kaynak kodu (src/, tests/ …) da kapsar**, sadece doküman/memory değil. Arka planda iş akışı/ajan çalışırken de beklemeden ara commit al (kullanıcı uyarısı 14 Kas 2026: "proje kodları duruyor hala").
- Aynı anda başka oturumlar commit alabilir: `index.lock` hatası olursa birkaç saniye bekleyip tekrar dene; lock dosyasını asla elle silme.

## Raporlar (14 Kas 2026)
- Rapor istendiğinde **iki format**: `raporlar/NNNN-konu/rapor.md` (Claude'un ileride hızlı hatırlaması için kısa) + `raporlar/NNNN-konu/index.html` (kullanıcı için CSS'li, proje temalı, açık/koyu modlu web sayfası).

## Versiyonlama (14 Kas 2026)
- Biçim `MAJOR.MINOR.PATCH`. Başlangıç **0.1.0**. **1.0.0 = programın testleriyle tam çalışır hali.**
- Her küçük düzeltme / deploy öncesi → PATCH +1 (0.1.20 → 0.1.21).
- Sayfa ekleme, sayfanın kapsamlı değişikliği → MINOR +1 (0.1.x → 0.2.0). MINOR'u tutumlu artır; hedef ~0.1.10 civarında bitirmek.
- Her değişiklikte versiyon güncellenir (tek kaynak: kökteki versiyon dosyası, kod başlayınca oluşturulacak).

## Sunucu
- Web sunucusu: yerel ağdaki Raspberry Pi 5 8 GB. Bağlantı bilgisi `anewcoursedemos/.env` (gizli; asla commit'leme, asla dışarı yazma). Boş port seçilir; kullanılanlar: 22, 111, 5090, 5432, 7100-7105.

## Kullanım kapsamı (14 Kas 2026 — kullanıcı uyarısı)
- Program **tek kullanıcılı, şahsi, ticari değil.** Lisans/ticari kullanım konusunu **tekrar soru olarak açma**; kişisel kullanıma izin veren her model eklenir. Sadece teknik engel (VRAM, platform) belirtilir.
- "Şu an sadece ben gibi" geliştir: çok kullanıcılı özellikleri öne çıkarma, Pi dışarı açılmaz.
