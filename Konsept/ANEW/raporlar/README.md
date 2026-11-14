# raporlar — araştırma raporları

Kural kaynağı: `ilk_promt.txt`. Uzun bir araştırma istendiğinde sonuç buraya yazılır; kısa olanlar terminalde verilir.

- Her rapor kendi klasöründe: `raporlar/<konu-kebab-case>/`
  - `index.html` — kullanıcının tarayıcıda okuyacağı sürüm
  - `<konu>.md` — agent'ın sonraki oturumlarda hızlıca okuyacağı özet (bulgular, kaynaklar, öneri)
- Rapor bir karara dönüşürse karar ADR ya da spec olarak kaydedilir; rapor yalnızca dayanaktır.
