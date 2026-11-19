# 0004 — Konu 1 onayı (dış servis notu) · Konu 2/12: Alan terimleri ve iş kuralları

**Tarih:** 2026-11-19 14:30

## İstek

> ücret ve sms hizmetlerim şuan yok hatta whatsapp filanda yok onlar hazıro lsun ama sonra ekleriz. onaylıyorum

## Cevap

**Konu 1 — Ürün ve hedef: ONAYLANDI.** Odak Faz 1.
Ek karar: **Ödeme sağlayıcı, SMS ve WhatsApp hesabı şu an yok.** Sistem bunlara hazır tasarlanacak ama gerçek bağlantılar sonra eklenecek:
- Her biri bir arayüzün (port) arkasında durur: `PaymentProvider`, `SmsSender`, `MessagingChannel`.
- MVP'de bu arayüzlerin yerinde sahte/kayıt tutan bir uygulama çalışır. Gönderilen SMS loglanır, ödeme "manuel tahsilat" olarak elle kaydedilir.
- Gerçek sağlayıcı (iyzico/Paycell, Netgsm, WhatsApp Business) sonradan ayrı bir spec ile, iş kodunu değiştirmeden takılır.
- Bu bekleyen iş, RAPOR adımında `specs/backlog.md` dosyasına `Source: bootstrap 2026-11-19` ile yazılacak.

**Konu 2/12 — Alan terimleri ve iş kuralları** (`docs/domain.md`)

⚠ **Dokümanda belirsizlik var:** "Şube" kelimesi iki anlamda kullanılmış.
- "2–5 şubeli okullar", "çok şubeli yönetim", "şube bazlı görünürlük" → **kampüs / fiziksel lokasyon** anlamında.
- "Şube, sınıf, şube-sınıf eşleşmesi" → Türk okul dilinde **7-A gibi sınıf şubesi** de olabilir.

Önerim (tek anlam, kod ve dokümanda sabit):

| Terim | Anlamı | Kodda |
|---|---|---|
| Kurum | Sistemi kullanan okul/kurs (kiracı) | `Tenant` |
| Şube | Kurumun fiziksel lokasyonu (kampüs) | `Branch` |
| Sınıf seviyesi | 1…12, hazırlık, anaokulu yaş grubu | `GradeLevel` |
| Sınıf | Belirli bir şubedeki öğrenci grubu, ör. "7-A" | `Classroom` |
| Dönem | Eğitim yılı içindeki yarıyıl/dönem | `Term` |
| Kişi / Kullanıcı | Bir hesap; birden çok role sahip olabilir | `User` + `RoleAssignment` |
| Veli | Öğrenciyle velilik bağı olan kişi | `Guardian` |
| Öğrenci kartı | Öğrencinin ana kaydı (sağlık, alerji, teslim alabilecekler) | `Student` |
| Yoklama | Ders/gün bazlı devam kaydı | `Attendance` |
| Duyuru / Mesaj | Okul/sınıf/öğrenci kapsamlı yayın; birebir yazışma | `Announcement` / `Message` |
| Taksit planı, Tahsilat, Cari | Ücret planı, alınan ödeme, veli hesabı | `InstallmentPlan`, `Payment`, `Ledger` |
| AI taslağı | AI'nın ürettiği, onay bekleyen çıktı | `AiDraft` |

Önerilen iş kuralları:
1. Bir kişi birden fazla role sahip olabilir. Rol atamaları kurum ve şube bazındadır.
2. Bir veli tek hesapla tüm çocuklarını görür. Kardeşler aynı veli kaydı üzerinden bağlanır.
3. Bir öğrencinin birden fazla velisi olabilir.
4. Her veri bir kuruma aittir. Hiçbir sorgu, kurum sınırını geçemez.
5. AI hiçbir mesajı onaysız veliye göndermez. AI çıktısı her zaman önce `AiDraft` olarak oluşur, bir insan onaylar.
6. Her AI çıktısında, dayandığı kaynak veri görünür.
7. Para tutarları kuruş cinsinden tam sayı (veya decimal) ve TRY ile tutulur, asla float kullanılmaz. Ayrıntısı Konu 6'da.
8. Mesai dışı sessiz mod: Öğretmenin tanımladığı saatler dışında velinin mesajı iletilir, ama öğretmene bildirim gitmez.

Önerim: "Şube = kampüs", "Sınıf = 7-A" olsun. Gerekçe: Dokümanın sonraki bölümleri (çok şubeli yönetim, şube bazlı görünürlük, şubeler arası rapor) "şube"yi açıkça kampüs anlamında kullanıyor. Sınıf şubesine "Sınıf" demek çakışmayı ortadan kaldırır.

**Dokümanda istenen:**
- "Şube, sınıf, şube-sınıf eşleşmesi ve dönem ve akademik takvim", ders programı (manuel + Excel).
- Öğrenci kartı, birden fazla veli, kardeş bağlantısı; sağlık notu, alerji, teslim alabilecek kişiler.
- "Bir kişi birden fazla role sahip olabilir (örneğin hem öğretmen hem veli). Kardeşi olan veli tek hesapla tüm çocuklarını görür."
- AI ilkeleri: "AI hiçbir mesajı onaysız veliye göndermez. Çocuklara dair veriler model eğitiminde kullanılmaz. Her AI çıktısında kaynak veri görünür."
- Mesai saati dışı sessiz mod; okundu bilgisi; onay formları.

Cevap bekleniyor: Terim tablosu ve iş kuralları uygun mu? Özellikle "Şube = kampüs" kararı.
