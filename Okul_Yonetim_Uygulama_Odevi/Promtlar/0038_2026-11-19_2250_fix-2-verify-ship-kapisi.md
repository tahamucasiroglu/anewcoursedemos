# 0038 — Düzeltme turu 2 (fix-2) · VERIFY · SHIP kapısı (spec 0002)

**Tarih:** 2026-11-19 22:50

## İstek

> düzelt

## Cevap

### Son mesajdan beri yapılanlar
1. **Düzeltme turu 2** (`64941e1`): Odak kırpma testi, kuralı CSS dosyasında bulamazsa artık kırmızıya düşüyor.
   - Bu turda yalnızca düşük önemde bir bulgu vardı; bu yüzden yeniden inceleme gerekmedi.
   - `CHECK GREEN (full, 11 steps, 118 s) @64941e1`.
2. **VERIFY (doğrulama):** Repo dışındaki bir kopyada **9 kasıtlı bozma** yapıldı; hepsi kırmızıya düştü. Kopya sonra silindi.
3. **28 ekran görüntüsü** alındı ve her biri tek tek kontrol edildi.
   - Konum: `Promtlar/Sources/0002/`; görüntüleri üreten betikler `kaynak/` altında.

### Kasıtlı bozmalar
| # | Bozma | Sonuç |
|---|---|---|
| B1 | Koyu moddaki soluk metin rengi düşük kontrastlı yapıldı | Kontrast testi kırmızı (2 çift 4.5:1'in altında) |
| B2 | Web'in koyu ana rengi ortak renk dosyasından farklı yapıldı | `cssVariables.test.ts` kırmızı |
| B3 | Web ön betiği cihaz modunu yok sayacak şekilde bozuldu | `prePaint.test.ts` kırmızı (ortak mantıkla ayrıştı) |
| B4 | Web bileşenine renk değeri (`#ffffff`) yazıldı | ESLint hatası: "Renk yalnızca ortak temadan" |
| B5 | Web, cihaz değişimini dinlemeyecek şekilde bozuldu | "Sistem seçiliyken cihaz değişirse…" testi kırmızı |
| B6 | Web seçimi saklamayacak şekilde bozuldu | 3 hatırlama testi kırmızı |
| B7 | Mobil zaman aşımı 500 ms'den 2000 ms'ye çıkarıldı | Zaman aşımı testi kırmızı |
| B8 | Mobil, cihaz modunu yok sayacak şekilde bozuldu | 5 test kırmızı |
| B9 | Web'de "seçili" bilgisi ekran okuyucudan kaldırıldı (`aria-pressed`) | 4 test kırmızı |

### Kriter ↔ kanıt tablosu
| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-1 Aynı renk sözlüğü, tek ana renk | `cssVariables.test.ts` (B2 ile kırmızı), `ThemeProvider.test.tsx` (mobil renkler = tokens.json), ESLint yasağı (B4) | PASS |
| AC-2 İlk açılışta Sistem; cihaza uyar; cihaz bilgi vermiyorsa açık | `preference.test.ts`, web ve mobil "ilk açılış" testleri; S-5 görüntüleri (cihaz koyu → koyu) | PASS |
| AC-3 Sağ üstte üç seçenek, seçili belli, metinler tr.json'dan; 320 px'de taşma yok | web ve mobil `ThemeSwitcher` testleri (B9 ile kırmızı); S-6 görüntüleri (320 px'de seçici alt satıra iniyor) | PASS |
| AC-4 Anında geçiş; durum çubuğu ve tarayıcı parçaları da uyar | web testi (data-theme + color-scheme), mobil testi (zemin + durum çubuğu stili) | PASS |
| AC-5 Hatırlama; sunucuya gitmez | web ve mobil yeniden açılış testleri (B6 ile kırmızı); fetch'e tema gitmiyor testi | PASS |
| AC-6 Sistem'de cihaz değişimini izler; Açık/Koyu'da izlemez | web (B5) ve mobil (B8) testleri | PASS |
| AC-7 Bozuk/okunamayan kayıt → Sistem; 500 ms zaman aşımı | `preference.test.ts`, `prePaint.test.ts` (hata fırlatan depolama), mobil hata ve zaman aşımı testleri (B7 ile kırmızı) | PASS |
| AC-8 Web: koyu kayıtlıyken açılışta parlama yok | `prePaint.test.ts` (B3 ile kırmızı); derleme çıktısında ön betik `<head>`'de, ana betikten önce; CSS de `<head>`'de | PASS |
| AC-9 Kontrast iki modda AA; eşik altı → kırmızı | `contrast.test.ts` (B1 ile kırmızı) | PASS |
| AC-10 4 durum ve Tekrar dene iki modda | web ve mobil sistem durumu testleri iki modda; S-1…S-4 görüntüleri (16 adet) | PASS |
| AC-11 Klavye ve ekran okuyucu | web klavye testi (Tab/Enter/Boşluk), mobil radiogroup/radio testi; S-7 görüntüleri (Tab ile her seçenekte odak çerçevesi tam görünüyor) | PASS |

Not: Mobil durum çubuğu Expo'nun web çıktısında görünmüyor. Bu yüzden kanıtı testtir (durum çubuğu stili moda göre değişiyor). İstersen kendi telefonunda Expo Go ile de bakabilirsin.

### Görüntü galerisi (`Promtlar/Sources/0002/`)
- **S-1…S-4:** 4 durum (yükleniyor, çalışıyor, hizmet dışı, ulaşılamıyor) × açık/koyu × web/mobil.
- **S-5:** "Sistem" seçili, cihaz koyu → koyu görünüm (web ve mobil).
- **S-6:** 320 px genişlik (web ve mobil, açık ve koyu); seçici başlığın altına iniyor, taşma yok.
- **S-7:** Web'de Tab ile Sistem / Açık / Koyu üzerinde odak çerçevesi (açık ve koyu).

### SHIP — onay verirsen
- 11 kriter işaretlenir, spec "Shipped" olur ve `specs/done/` klasörüne taşınır. Kanıt tablosu ve karne spec'e yazılır.
- Çalıştırılacak kontroller: `doctor --strict` ve tam kontrol.
- Ardından özelliğin özeti ve yeni özellik önerileri gelir.

### Karar (tek)
Spec 0002'yi (tema ve açık/koyu mod) ship edelim mi? Onayın, görüntülere baktığın anlamına da gelir.
