# 0056 — Yol haritası repoya yazıldı

**Tarih:** 2026-11-19 23:30

## İstek

> evet yaz neredeyizi diye sorunca oradan gösterirsin.

## Cevap

**`docs/roadmap.md` yazıldı.** "Neredeyiz?" diye sorduğunda cevabı bu dosyadan göstereceğim. Kural: her ship'te güncellenir. Biten satır ✅ ve spec numarasıyla işaretlenir; sıra yalnızca senin kararınla değişir.

### İçeriği
- **Biten altyapı:**
  - 0001 iskelet ✅
  - 0002 tema ✅
  - 0003 Pi deneme ortamı ✅
- **Faz 1 (MVP) sıralı listesi, hepsi ⬜ başlanmadı.** Her satırda kapsamdan ne içerdiği ve neden o sırada olduğu yazılı.
  1. Giriş ve roller + kurum/şube
  2. Kurum ve akademik yapı
  3. Öğrenci ve veli kayıtları
  4. Yoklama ve devamsızlık
  5. Veli iletişimi
  6. AI: sesli nottan veli mesajı
  7. Ödev ve not takibi
  8. AI: haftalık gelişim özeti
  9. Tahsilat ve ön muhasebe
  10. Randevu
  11. Yönetici paneli
- **Faz 1 dışı:** Faz 2 ve Faz 3. Kapsamın kuralına göre önceki faz kanıtlanmadan başlamaz.

### Kontrol
- `scripts/doctor` temiz.
- `CHECK GREEN (fast, 9 steps)`.

### Sonraki adım
Sıradaki ilk iş için: `/new-feature Giriş ve roller + kurum/şube`
