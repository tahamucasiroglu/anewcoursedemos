# 0006 — Konu 2 onayı · Konu 3/12: Hassas alanlar

**Tarih:** 2026-11-19 14:40

## İstek

> onaylıyorum

## Cevap

**Konu 2 — Alan terimleri ve iş kuralları: ONAYLANDI.** Şube = aynı kurumun farklı lokasyonu; terim tablosu ve 9 iş kuralı `docs/domain.md` dosyasına yazılacak.

**Konu 3/12 — Hassas alanlar** (`docs/domain.md` "Sensitive areas")

ANEW'de bir alanı "hassas" işaretlemenin sonucu: Bu alana dokunan her spec, Constraints bölümüne `Sensitive area: <ad>` yazar. Bu durumda lite modda da şu ek güvenceler devreye girer:
- netleştirme (clarify) kapısı her zaman sorulur;
- her düzeltme turu yeniden incelenir (yalnızca doküman değişikliği olan turlar hariç);
- her kabul kriteri, kasıtlı tek bir bozma ile "kırmızıya düşüyor mu" diye ispatlanır.

Yani hassas alan = **daha yavaş ama daha güvenli** süreç.

Önerilen hassas alanlar:

| # | Hassas alan | Neden | Örnek dokunuş |
|---|---|---|---|
| S1 | **Kiracı ve şube izolasyonu** | Bir okulun verisi başka okula sızarsa ürün biter | Sorgu filtreleri, yetki kontrolü |
| S2 | **Çocuk kişisel verisi ve özel nitelikli veri** | KVKK: sağlık notu ve alerji özel nitelikli veridir; çocuk verisi | Öğrenci kartı, sağlık, teslim alabilecekler, fotoğraf |
| S3 | **Kimlik, rol ve yetki** | Yanlış rol = yanlış kişinin çocuğu görmesi | Giriş, 2FA, rol ataması, veli-öğrenci bağı |
| S4 | **Para ve tahsilat** | Yanlış tutar, çift tahsilat, cari hatası | Taksit planı, indirim/burs, tahsilat, makbuz |
| S5 | **Veliye giden iletişim ve AI çıktıları** | Onaysız veya yanlış kişiye mesaj, "AI onaysız gönderemez" kuralı | Duyuru, mesaj, AI taslağı, otomatik devamsızlık bildirimi |
| S6 | **KVKK rıza, audit log ve saklama süresi** | Hukuki yükümlülük | Rıza kayıtları, erişim logu, anonimleştirme |

Önerim: Bu 6 alanın tamamını hassas olarak işaretlemek. Gerekçe: Doküman "KVKK ve güvenlik satış görüşmesinde ilk sorulacak konu; MVP'den itibaren tam olmalı" diyor. Ayrıca bu alanların hatası para, erişim veya hukuki sorumluluk demek. Hassas olmayan modüller (ders programı, randevu, ödev listesi, akademik takvim) normal hızda ilerler.

**Dokümanda istenen:**
- "Çocuk verisi işlendiği için KVKK ve güvenlik, satış görüşmesinde ilk sorulacak konu; MVP'den itibaren tam olmalı."
- Altyapı: multi-tenant, tenant bazlı veri izolasyonu; rol ve yetki, şube bazlı görünürlük; audit log ("kim, hangi öğrenci kaydını, ne zaman görüntüledi/değiştirdi").
- KVKK: veli açık rıza ve aydınlatma metni, rıza kayıtları; fotoğraf paylaşımında öğrenci bazlı izin; mezun/ayrılan öğrenci için saklama süresi ve anonimleştirme; 2FA (yönetici zorunlu); şifreli veri saklama.
- AI: "AI hiçbir mesajı onaysız veliye göndermez. Çocuklara dair veriler model eğitiminde kullanılmaz."
- Tahsilat: yıllık ücret, taksit, indirim/burs, online ödeme, gecikme hatırlatması, makbuz, cari.

Cevap bekleniyor: S1–S6 hassas alan olarak uygun mu? Çıkarmak ya da eklemek istediğin var mı?
