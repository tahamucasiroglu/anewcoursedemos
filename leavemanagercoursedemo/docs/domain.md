# Domain

> Ortak dil: iş tarafı, insanlar ve ajanlar aynı kelimeyi aynı anlamda kullanır.
> Bir terim burada yoksa, yapay zekânın kendi anlamını uydurmasını bekleyin.

**Ürün:** Leave Manager — çalışanların izin talebi oluşturduğu, yöneticilerin onaylayıp
reddettiği, izin bakiyelerinin otomatik takip edildiği küçük bir web uygulaması. Bir **kurs
demosudur**: amaç ANEW iş akışını (spec → plan → build → review → verify) gerçekçi ama küçük bir
alanda göstermektir.

**v1 kapsamı:** çalışan izin talebi oluşturur / iptal eder; yönetici onaylar / reddeder; yıllık
izin bakiyesi düşülür; herkes kendi talep geçmişini görür. Kapsam dışı: `docs/architecture.md`.

## Ubiquitous language

Kod tanımlayıcıları İngilizce, doküman ve arayüz Türkçe; tablo ikisini eşler.

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Çalışan (`Employee`) | Sisteme giriş yapan ve izin talep eden kişi | Her yönetici de bir çalışandır |
| Yönetici (`Manager`) | Doğrudan bağlı çalışanlarının taleplerini onaylayan / reddeden çalışan | Sistem yöneticisi (admin) değildir; v1'de admin rolü yok |
| İzin türü (`LeaveType`) | Yıllık izin (`Annual`), mazeret izni (`Excuse`), ücretsiz izin (`Unpaid`) | v1'de sabit liste; bakiye yalnızca yıllık izinde tutulur |
| İzin talebi (`LeaveRequest`) | Bir çalışanın belirli tarih aralığı ve izin türü için yaptığı başvuru | Tarihler `DateOnly`, başlangıç ve bitiş dahil |
| Talep durumu (`LeaveStatus`) | `Pending` (Beklemede), `Approved` (Onaylandı), `Rejected` (Reddedildi), `Cancelled` (İptal) | Başka durum yoktur |
| İzin bakiyesi (`LeaveBalance`) | Bir çalışanın bir yıl ve izin türü için kalan gün sayısı | "Kullanılabilir bakiye" = bakiye − beklemedeki taleplerin günleri (BR-3) |
| İş günü (`WorkingDay`) | Pazartesi–Cuma arası gün | v1'de resmî tatiller dikkate alınmaz |
| Talep edilen gün (`RequestedDays`) | Talep aralığındaki iş günü sayısı (BR-2) | Takvim günü değildir |
| Ret gerekçesi (`RejectionReason`) | Yöneticinin reddederken yazdığı zorunlu açıklama | Boş veya yalnız boşluk geçersiz |

## Business rules

- **BR-1:** Bir talebin bitiş tarihi başlangıç tarihinden önce olamaz; başlangıç tarihi bugünden
  (sistem saatine göre) önce olamaz — geçmiş tarihli talep oluşturulamaz.
- **BR-2:** Talep edilen gün sayısı, aralıktaki iş günleri (Pzt–Cum) sayılarak hesaplanır;
  hafta sonu sayılmaz. Hiç iş günü içermeyen bir talep geçersizdir.
- **BR-3:** Yıllık izin talebinin gün sayısı, kullanılabilir bakiyeyi (kalan bakiye − aynı yılın
  `Pending` yıllık izin taleplerinin günleri) aşamaz.
- **BR-4:** Bir çalışanın `Pending` veya `Approved` durumdaki talepleri tarih olarak çakışamaz
  (tek bir ortak gün bile çakışmadır).
- **BR-5:** Yalnızca `Pending` talep onaylanabilir veya reddedilebilir. Reddetmede gerekçe zorunludur.
- **BR-6:** Bakiye yalnızca onayda düşülür; onaylı bir yıllık izin talebi iptal edilirse günleri
  bakiyeye geri eklenir.
- **BR-7:** Çalışan yalnızca kendi talebini iptal edebilir: `Pending` talebi her zaman,
  `Approved` talebi yalnızca izin başlangıç tarihinden önce.
- **BR-8:** Yönetici kendi talebini onaylayamaz / reddedemez; yalnızca doğrudan bağlı
  çalışanlarının taleplerini görür ve karara bağlar.

## Key domain invariants

- İzin bakiyesi hiçbir zaman negatif olamaz.
- `Rejected` ve `Cancelled` talepler değişmezdir (immutable); hiçbir işlem onları değiştiremez.
- Bir talebin durumu yalnızca şu geçişlerle değişir: `Pending → Approved | Rejected | Cancelled`,
  `Approved → Cancelled` (BR-7). Başka geçiş yoktur.

## Sensitive areas

- **Yetkilendirme (rol / ekip erişimi):** kim hangi talebi görebilir, onaylayabilir, iptal
  edebilir (BR-7, BR-8, `docs/security.md` AuthZ). Hata = başkasının izinlerini görmek veya karara
  bağlamak.
- **İzin bakiyesi hesaplaması:** gün sayımı, bakiye düşme / iade (BR-2, BR-3, BR-6, değişmezler).
  Hata = çalışan aleyhine hak kaybı.

Bu alanlara dokunan bir spec, Constraints bölümünde `Sensitive area: <ad>` yazar; lite modda da
clarify sorulur, her fix round yeniden review edilir ve her kriter kasıtlı bir kırma ile kırmızıya
düşürülerek kanıtlanır (`workflows/README.md`, Modes).
