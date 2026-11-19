# 0028 — Triage 1 kararları · Düzeltme turu 1 (fix-1) · Yeniden inceleme (Review round 2) · TRIAGE 2

**Tarih:** 2026-11-19 18:45

## İstek

> anladım önemde bilgi olanlara emin değilim ekleyim dersen ekleriz kalanları düzeltelim

## Cevap

### Son mesajdan beri yapılanlar
1. **Triage 1 kaydedildi:** Bilgi seviyesindeki bulgularda benim önerilerim uygulandı:
   - Düzeltilenler: 13 ("healthy" metni tek yerde), 14 (`window.fetch` lint'te yasak), 15 (eski test paketleri), 16 (satır sonları)
   - Ertelenenler: 12 (iPhone ekran okuyucu) → backlog B-8; 17 (Pi log yolu) → backlog B-9
   - 11 tuzak listesine eklendi; 18 doğrulamaya bırakıldı
   - Kalan bulguların hepsi düzeltildi. Sapma kayıtları A-1…A-10 onaylı olarak plana yazıldı.
2. **Düzeltme turu 1:** 14 commit, her bulgu için ayrı (`7c88c87…5ae5f6b`):
   - **Mimari testler artık gerçekten çalışıyor:** "Bu kural gerçekten bir şey görüyor mu" kontrol testleri eklendi. Düzelen test hemen 2. bulguyu, yani API katmanının veritabanı bağlamını doğrudan kullanmasını yakaladı. Şema güncellemesi veri katmanına taşındı.
   - npm taraması tümünü kapsıyor.
   - Kullanıcının iptal ettiği istek artık "hata" diye loglanmıyor.
   - "Uyarı = hata" kuralı tüm proje ve ayar dosyalarında denetleniyor.
   - Paralel kontroller birbirinin test veritabanını silmiyor.
   - "Server: Kestrel" başlığı kapalı.
   - "Tekrar dene" testleri eksiksiz.
   - Küçükler: durum metni tek yerde; `globalThis/window.fetch` yasak; kullanılmayan paketler kaldırıldı; API tanımı her makinede aynı satır sonuyla üretiliyor.
   - Tuzak listesine 3 yeni satır:
     - T8: test aracının yüklemediği paketlere bakan kurallar sessizce geçer
     - T9: loglanan işlem kişisel veriyi olduğu gibi loga yazar
     - T10: yeni veritabanında EF Core bir kez zararsız bir hata satırı yazar
3. **Kontrol:** `check` tam profil yeşil (.NET 29 + 8 test, web 9, mobil 9). "Arkada süreç kalmıyor" testi iki profilde temiz. doctor: 0 hata, 0 uyarı.
4. **Yeniden bağımsız inceleme (Review round 2):** 15 düzeltmenin **hepsi doğru yapılmış**. Kritik, yüksek veya orta bulgu yok. Yeni olarak 2 düşük ve 3 bilgi notu var; hiçbiri bir kabul kriterini, güvenliği ya da veriyi etkilemiyor.

### TRIAGE 2 — yeni bulgular ve önerim
| # | Bulgu (sade) | Önem | Önerim |
|---|---|---|---|
| R2-1 | "Bu kural gerçekten bir şey görüyor mu" kontrol testleri, sınadıkları sınıfın adını metin olarak tutuyor. Sınıf ileride yeniden adlandırılırsa kontrol hiçbir şey sınamadan yeşil kalır. | Düşük | **Düzelt:** adı derleyicinin denetlediği biçimde (`typeof(...)`) ver; yeniden adlandırmada derleme kırılsın |
| R2-2 | Sistem durumu yanıtı aynı bilgiyi iki alanda tutuyor (metin + enum). Biri yanlışlıkla elle oluşturursa enum varsayılanı "çalışıyor" olur ve API 200 döner. | Düşük | **Düzelt:** tek kaynak; metin enum'dan hesaplansın (API çıktısı aynı kalır) |
| R2-3 | Tuzak listesindeki T10 satırı (EF Core'un ilk kurulumdaki zararsız hata satırı) senin onayın olmadan eklendi. | Bilgi | **Onayla** (sapma kaydı A-11) |
| R2-4 | Satır sonu düzeltmesi yalnızca veri şemalarının açıklamalarını kapsıyor; ileride uç noktalara çok satırlı açıklama eklenirse aynı sorun çıkabilir. | Bilgi | **Ertele** (backlog B-10) |
| R2-5 | "Uyarı = hata" testinin yakalamadığı nadir yollar var (ör. `[SuppressMessage]`, `.globalconfig`). | Bilgi | **Gerek yok:** kuralın gerçekten çalıştığı doğrulamada kasıtlı bozmayla zaten gösterilecek |

**Sonraki adım:** İki düşük bulgu için küçük bir düzeltme turu 2 açılır. Sadece düşük ve bilgi seviyesinde bulgu olduğu için **yeniden inceleme gerekmez** (ANEW kuralı); doğrudan **VERIFY**'a geçilir: her kriterin kanıtı, kuralların kasıtlı bozma denemeleri ve ekran görüntüleri (`Promtlar/Sources/0001/`).

### Karar (tek)
Yukarıdaki 5 öneriyi kabul ediyor musun?
