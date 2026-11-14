# Taha Konsept — Hafıza (memory)

Bu klasör projenin kalıcı hafızasıdır. Her oturumun başında **bu dosya ve `kurallar/` altındaki tüm dosyalar okunur ve uygulanır.**
Kullanıcı bu dosyalara doğrudan müdahale edebilir; kullanıcının yazdığı her şey Claude'un notlarından önce gelir.

## Dizin
| Dosya | İçerik |
|---|---|
| [kurallar/genel.md](kurallar/genel.md) | Süreç kuralları: prompt kaydı, commit, versiyon, raporlar, klasörler |
| [kurallar/kod.md](kurallar/kod.md) | Kodlama kuralları: mimari, dosya başına tek nesne, summary, controller, Ajax |
| [kurallar/tasarim.md](kurallar/tasarim.md) | Tasarım kuralları: tema, renkler, açık/koyu mod, yerleşim |
| [kararlar.md](kararlar.md) | Alınan kararlar günlüğü (tarihli) |
| [urun-tanimi.md](urun-tanimi.md) | Ürünün özeti: sayfalar, akışlar, pipeline |
| [../PLAN.md](../PLAN.md) | Ana plan: kararlar, çözüm yapısı, pipeline, fazlar |
| [../raporlar/](../raporlar/) | Araştırma raporları (0001: teknoloji araştırması) |
| [notlar.md](notlar.md) | Claude'un kendine notları, ipuçları, dikkat edilecek tuzaklar, açık sorular |

## Hafıza nasıl güncellenir
- Kullanıcı "bunu yapma", "yasak", "bir daha böyle olmasın" dediğinde ya da bir şeyi düzelttiğinde/uyardığında → ilgili `kurallar/*.md` dosyasına **kısa ve öz** bir kural eklenir (tarih + kısa neden).
- Bir karar alındığında → `kararlar.md` sonuna tarihli satır.
- Kural kategorisi yoksa yeni dosya açılabilir (ör. `kurallar/sayfa-konseptler.md`) ve bu dizine eklenir.
- Yanlış çıkan not silinir/düzeltilir; aynı kural iki yerde tutulmaz.
