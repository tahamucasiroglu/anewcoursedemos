# Promtlar — konuşma kaydı

Bu klasör, ANEW Konsept projesi boyunca kullanıcı ile asistan arasındaki **her istek ve her cevabın**
zaman damgalı, sıralı kaydıdır (kural: `ilk_promt.txt`, madde 1).

## Format
- Bir alışveriş (istek + cevap) = bir dosya: `NNNN_YYYY-MM-DD_HHMM.md`
- `NNNN` sıra numarasıdır; hiç atlanmaz, hiç yeniden kullanılmaz.
- Saat, isteğin geldiği andır (yerel saat). İlk 3 kaydın saatleri yaklaşıktır (oturum 04:17 civarı başladı).
- Dosyada: `## İstek` (kullanıcının yazdığı, birebir) ve `## Cevap` (asistanın kullanıcıya gösterdiği metin, birebir).
- Asistanın iç düşünceleri ve araç çıktıları kaydedilmez; yalnızca kullanıcıya doğrudan söylenenler.
