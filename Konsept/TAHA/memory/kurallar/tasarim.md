# Tasarım Kuralları

## Tema (14 Kas 2026)
- Ana tema **açık mod**: turuncu, beyazımsı turuncu / krem, açık kahverengi, beyaz. "Kahve dükkanı" sıcaklığı, göz yormayan, insanın içini açan (ama içerik kahveyle ilgili değil).
- **Koyu mod** da olacak: aynı turuncu/kahve paletinin koyu karşılığı.
- Tema değiştirici: **sağ üstte güneş / ay ikonu**.
- Renkler CSS değişkenleriyle (`:root` + `[data-theme="dark"]`), tek yerden yönetilir.

## Yerleşim (taslak çizimler: `source/tasarim/` — orijinaller `Konsept/Sources/`)
- Üst bar (her sayfada ortak): sol üst logo + "Taha Konsept" → aylık harcama (dolar + sent, ör. "Bu ay $2.37") → sayfalar: Konseptler · Konseptten Video · Üretilenler · Silinenler · Yayınlama → sağda: profil seçimi dropdown · ✏️ (profil düzenle) · ➕ (profil ekle) · hata bildir · ayarlar · ☀/🌙.
- **Profil sayfası üst menüde YOK**; profil ✏️/➕ ile modalda yönetilir.
- **Yayınlama** sayfası şimdilik sadece "Çok yakında sizlerleyiz".
- En altta ince **Günlük** şeridi: son işlemler akar; hata kırmızı, uyarı sarı, normal nötr.
- Seçim/düzenleme pencereleri sayfanın içine sıkıştırılmaz → **ekranın üstünde ayrı modal**; dışına tıklayınca veya kapat ile kapanır ve ayarları kaydeder.
- Seçenek kutuları (altyazı, ilerleme çubuğu, kelime denetimi) **gerçekten çalışmalı**; süs olmaz.
