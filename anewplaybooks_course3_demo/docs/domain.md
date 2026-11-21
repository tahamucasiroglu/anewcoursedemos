# Domain

> Bootstrap'ta dolduruldu (2026-11-21, adoption). Burada **var olan** davranış yazılıdır, olması
> gereken değil. Kaynak `app.py`. Koddaki yorumlar ile kod çelişirse **kod geçerlidir**.

## Product
**LeaveManager**: personel izin takip sistemi. Sistemde şunlar yapılır: izin talebi oluşturma
(`/new`), talepleri listeleme (`/requests`), bekleyen talebi iptal etme (`POST /requests/<no>/cancel`,
BR-9), dashboard'da talep durum sayıları ve personel
bakiyeleri (hak / kullanılan / kalan) (`/`). 2015'ten beri iç geliştirme, testsiz bir legacy kod.
**Amaç:** mevcut davranışı bozmadan yeni özellik eklemek. Mevcut davranış sözleşmedir.

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Personel (employee) | İzin hakkı olan çalışan: `id`, `name`, `department`, `hire_date` | Kodda `Db.employees` |
| Birim (department) | Personelin bağlı olduğu bölüm (IT, Muhasebe, IK, Satis) | Bakiye hesabını etkilemez |
| İzin talebi (request) | Bir personelin `start`–`end` aralığı için izin isteği, `reason` ve `status` ile | Kodda `Db.requests` |
| Durum (status) | `Beklemede` / `Onaylandi` / `Reddedildi` / `Iptal Edildi`: Türkçe ASCII metin, enum yok | BR-5, BR-9 |
| İptal (cancel) | Bekleyen bir talebin geri çekilmesi; talep silinmez, durumu `Iptal Edildi` olur | BR-9; ret (`Reddedildi`) ile karıştırılmaz |
| Kıdem (seniority) | İşe girişten bu yana geçen yıl, BR-2'deki biçimde hesaplanır | Gerçek tam yıl değildir |
| Hak (entitlement) | Kıdeme göre yıllık izin günü (BR-1) | Devir (carry) yürürlükte değil (BR-8) |
| Kullanılan (used) | Onaylanmış taleplerin gün toplamı (BR-4) | Bekleyenler dahil değil |
| Kalan / bakiye (remaining) | Hak − kullanılan (BR-4) | |
| Gün (days) | Bir talebin gün sayısı, takvim günü (BR-3) | "İş günü" **değildir** |

## Business rules
- **BR-1 Kıdem → yıllık hak:** kıdem < 1 → **0** · 1–5 → **14** · 6–15 → **20** · 16+ → **26**
  gün (`get_entitlement`, IK el kitabı 2014). Koddaki yorum "1–5 yıl 15 gün" diyor ama kod 14
  uyguluyor. **Geçerli olan 14.**
- **BR-2 Kıdem hesabı:** `bugünün yılı − işe giriş yılı`. Ay ve gün dikkate alınmaz (IK, 2015).
  Sonuç sistem saatine (`datetime.now()`, yerel saat) bağlıdır.
- **BR-3 Gün hesabı:** `(bitiş − başlangıç) + 1` **takvim günü, hafta sonları dahil**. Yorumdaki
  "iş günü" ifadesi yanlış. **Deliberately preserved:** mevcut davranış sözleşmedir. İş gününe
  geçmek ayrı bir talep olur.
- **BR-4 Bakiye:** kullanılan = yalnız `Onaylandi` taleplerin günleri. Kalan = hak − kullanılan.
  `Beklemede` talepler düşülmez ("2018'de konuşuldu, karar yok", bkz. açık soru Q-2).
- **BR-5 Durumlar:** yeni talep `Beklemede` olarak doğar. Metinler `Beklemede`, `Onaylandi`,
  `Reddedildi` ve spec 0002 ile eklenen `Iptal Edildi` (BR-9). **Preserved.** Enum'a geçiş teknik borç olarak duruyor (`docs/architecture.md`).
  Arayüzde talepleri onaylayan veya reddeden bir akış yok, durumlar yalnız seed verisinde değişik.
- **BR-6 Geçmiş tarih:** başlangıcı geçmişte olan talep kabul edilir ("IK bazen geriye dönük
  giriyor").
- **BR-7 Bakiye eşiği (ÇELİŞKİLİ):** form (`/new`) `kalan < gün` ise "ERR-412 … bakiye yetersiz"
  hatası gösterir. Servis (`create_request`) `kalan <= gün` ise kaydı sessizce reddeder, route
  yine `/requests`'e yönlendirir. Sonuç: `kalan == gün` olan talep hata vermeden kaybolur.
  **Açık soru Q-1.** Bootstrap karar vermez, davranış olduğu gibi korunur.
- **BR-8 Yıl sonu devri:** `recalculate_year_end` (devir, en fazla 5 gün) yorum satırında ve
  çalışmıyor. **Devir kuralı yürürlükte değil.** Silinip silinmeyeceği ayrı bir karar konusu.
- **BR-9 İptal (spec 0002):** yalnız `Beklemede` talep iptal edilir (talepler listesindeki "Iptal"
  düğmesi, form gönderimiyle). Talep silinmez, durumu `Iptal Edildi` olur. İptal edilen talep
  dashboard'daki üç kutunun hiçbirinde sayılmaz ve bakiyeyi değiştirmez (BR-4: bekleyenler zaten
  düşülmüyordu). Bekleyen olmayan, var olmayan veya geçersiz numaralı bir talebin iptali hiçbir
  şeyi değiştirmez ve `Yalniz bekleyen talepler iptal edilebilir.` mesajını gösterir. Eşik
  çelişkisi (Q-1) için spec 0002'nin açık kararı: hangisinin doğru olduğu bilinmiyor, iki davranış
  da korunuyor.
- **BR-10 Tarih doğrulaması (spec 0004):** yeni talepte başlangıç ve bitiş `YYYY-AA-GG` biçiminde
  geçerli, takvimde var olan tarihler olmalı ve bitiş başlangıçtan önce olamaz (başlangıç = bitiş
  geçerli, 1 gün). Doğrulama bakiye kontrolünden önce yapılır. Geçersizse talep kaydedilmez ve
  sayfa düzeni içinde `HATA: Tarihler gecersiz. …` mesajı (HTTP 400) gösterilir. Eski sessiz
  yönlendirme (spec 0001 AC-8) spec 0004 ile değişti.

## Open questions
- **Q-1:** Bakiye eşiği `<` mı `<=` mi (BR-7)? Hangisinin doğru olduğunu bilen yok. Bu konuya
  dokunan spec, kararı açıkça yazar. O zamana kadar iki davranış da korunur.
- **Q-2:** Bekleyen talepler kalan bakiyeden düşülmeli mi (BR-4)?

## Key domain invariants
- Bir spec "Changed" demedikçe bakiye hesabı (BR-1…BR-4) hiçbir değişiklikle sonuç değiştirmez.
- Referans tarih 2026-11-21'deki sözleşme değerleri: dashboard **Bekleyen 4 · Onaylanan 1 ·
  Reddedilen 3**. Ayşe Yılmaz **Hak 26 · Kullanılan 12 · Kalan 14** (12 gün, hafta sonları dahil).

## Sensitive areas
- **Bakiye hesabı:** `get_entitlement`, `get_seniority_years`, `calc_days`, `get_used_days`,
  `get_remaining`. Personelin izin hakkını belirler, hata IK ve hukuk sorunu doğurur.
- **Talep oluşturma ve bakiye eşiği:** `create_request` ve `/new`. Çifte ve çelişkili kontrol
  var (BR-7).
- **Kişisel veri:** personel adı, birimi, işe giriş tarihi, izin nedeni. "Saglik" gibi nedenler
  hassas veridir, loglara ve hata mesajlarına yazılmaz.
