# Domain

> İzin yönetimi (kurs demosu) için ortak dil. Bir terim burada yoksa yapay zekâ ona kendi anlamını uydurur.
> Koddaki İngilizce karşılık parantez içinde verilmiştir; kod yalnız o adı kullanır.

## Ürün ve amaç
Bir şirkette çalışanların izin talebi oluşturduğu, yöneticilerin bu talepleri onayladığı ya da reddettiği,
kalan izin bakiyesinin izlendiği bir web uygulaması. Kurs demosu: tek şirket, tek onay kademesi
(doğrudan yönetici), temel raporlama.
**Başarı ölçütü:** çalışan talep açar → yöneticisi onaylar → bakiye doğru düşer.

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Çalışan (`Employee`) | İzin talep eden kişi; bir yöneticiye bağlıdır | Her kullanıcı bir çalışandır; temel rol `Employee` (`docs/security.md`) |
| Yönetici (`Manager`) | Kendisine bağlı çalışanların taleplerini onaylayan/reddeden çalışan | Yöneticisi olmayan kişinin (hiyerarşinin tepesi) talebini İK onaylar (BR-5) |
| İK (`HR`) | İzin türlerini ve yıllık hakları tanımlayan, tüm talepleri görebilen rol | Onay kademesi değildir |
| İzin türü (`LeaveType`) | Yıllık, mazeret, hastalık, ücretsiz; her türün bakiyeden düşüp düşmediği tanımlıdır (`DeductsFromBalance`) | |
| İzin talebi (`LeaveRequest`) | Bir çalışanın bir tarih aralığı ve izin türü için başvurusu | |
| Talep durumu (`LeaveRequestStatus`) | `Beklemede` (`Pending`) → `Onaylandı` (`Approved`) / `Reddedildi` (`Rejected`); çalışan yalnız `Beklemede` talebi iptal eder → `İptal` (`Cancelled`) | Bu değerler domain'e aittir, ANEW `Status:` alanlarıyla karıştırılmaz |
| İzin bakiyesi (`LeaveBalance`) | Çalışanın bir yıl ve bir izin türü için kalan gün sayısı | Yıllık hak (`Entitlement`) ≠ kalan bakiye |
| İş günü (`WorkingDay`) | Cumartesi ve pazar dışındaki gün | Resmî tatiller ilk sürümde yok (kapsam dışı) |
| Talep süresi (`Duration`) | Başlangıç ile bitiş arasındaki iş günü sayısı, iki uç dahil | Takvim günü değil |

## Business rules
- **BR-1:** Talep süresi = başlangıç ile bitiş arasındaki iş günü sayısı (iki uç dahil). Bitiş başlangıçtan önceyse ya da süre 0 ise talep geçersizdir.
- **BR-2:** Bakiyeden düşen bir türde, talep süresi kalan bakiye eksi aynı türdeki `Beklemede` taleplerin toplamını aşamaz. Talep oluşturulurken kontrol edilir.
- **BR-3:** Bakiye talep **onaylandığında** düşer. Ret ve iptal bakiyeye dokunmaz.
- **BR-4:** Bir çalışanın `Beklemede` ya da `Onaylandı` iki talebi tarih olarak çakışamaz.
- **BR-5:** Talebi yalnız çalışanın kendi yöneticisi onaylar ya da reddeder; yöneticisi olmayan çalışanın talebini İK onaylar ya da reddeder. Kimse kendi talebini onaylayamaz.
- **BR-6:** `Onaylandı`, `Reddedildi` ve `İptal` son durumlardır; değiştirilemez.
- **BR-7:** Başlangıç tarihi bugünden (`TimeProvider`'a göre) önce olan talep açılamaz.

## Key domain invariants
- Bakiye asla negatif olmaz.
- Onaylanan bir talep bakiyeden tam olarak bir kez düşer.

## Sensitive areas
- **Kişisel veri:** hastalık izninin nedeni/notu (KVKK özel nitelikli sağlık verisi sayılabilir) yalnız talep sahibine, onun yöneticisine ve İK'ya görünür.
- **Yetkilendirme:** kim hangi talebi görebilir, onaylayabilir, reddedebilir (BR-5 ve görünürlük kuralları).

## Out of scope (ilk sürüm)
Resmî tatiller, yarım gün izin, yıl sonu bakiye devri, çok kademeli onay, departman hiyerarşisi.
