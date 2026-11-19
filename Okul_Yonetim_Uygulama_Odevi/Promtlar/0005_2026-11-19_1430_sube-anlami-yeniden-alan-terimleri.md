# 0005 — Konu 2/12 yeniden: "Şube" = aynı kurumun farklı lokasyonu

**Tarih:** 2026-11-19 14:30

## İstek

> ben sanki a okullarının istanbul bahçelievler şubesi, a okullarının istanbul avcılar şubesi gibi anladım. aynı okul ama farklı yerlerde, aynı kurum daha doğrusu. bu anlamda olabilir mi düşün tekrar bu aşamayı hazırla ve sor

## Cevap

Evet, bu anlam hem dokümanla hem de önceki önerimle uyumlu ("Şube = kampüs"). "Kampüs" kelimesi kafa karıştırdığı için tanımı senin örneğinle yeniden yazdım. Böylece "şube-sınıf eşleşmesi" de netleşiyor: **hangi şubede hangi sınıflar açık.**

Örnek hiyerarşi:

```
Kurum (kiracı): A Okulları
├── Şube: İstanbul Bahçelievler
│   ├── Sınıf seviyesi 5 → Sınıf: 5-A, 5-B
│   └── Sınıf seviyesi 7 → Sınıf: 7-A
└── Şube: İstanbul Avcılar
    ├── Sınıf seviyesi 5 → Sınıf: 5-A
    └── Sınıf seviyesi 8 → Sınıf: 8-A, 8-B
```

- Bahçelievler'deki 5-A ile Avcılar'daki 5-A **farklı sınıflardır**. Sınıf her zaman bir şubeye bağlıdır.
- Şube-sınıf eşleşmesi: Her dönem, her şubede hangi seviyede kaç sınıf açıldığının tanımı.
- Kurum: Sözleşmeyi yapan, faturası kesilen ve verisi diğer kurumlardan tamamen ayrı olan müşteri (A Okulları). Tek şubeli bir okul = 1 kurum + 1 şube.
- Kurumun ortak ayarları (logo, ücret tarifesi şablonu) kurum düzeyindedir. Yoklama, sınıf ve öğretmen ataması şube düzeyindedir.
- Kurucu/genel müdür tüm şubeleri görür. Bir şube müdürü, öğretmen veya muhasebeci yalnızca atandığı şube(ler)i görür ("şube bazlı görünürlük").
- Bir öğretmen iki şubede ders verebilir; rol ataması her şube için ayrı yapılır.
- Kardeşler farklı şubelerde okuyabilir; veli yine tek hesapla ikisini de görür.

Güncel terim tablosu:

| Terim | Anlamı | Örnek | Kodda |
|---|---|---|---|
| Kurum | Müşteri okul/kurs; verisi diğer kurumlardan izole | A Okulları | `Tenant` |
| Şube | Aynı kurumun farklı lokasyonu | İstanbul Bahçelievler Şubesi | `Branch` |
| Sınıf seviyesi | Kademe | 5. sınıf, hazırlık, 4 yaş | `GradeLevel` |
| Sınıf | Bir şubedeki, bir seviyedeki öğrenci grubu | Bahçelievler 5-A | `Classroom` |
| Dönem | Eğitim yılı içindeki dönem | 2026-2027 Güz | `Term` |
| Kullanıcı | Bir hesap; birden çok role sahip olabilir | Öğretmen + veli | `User` + `RoleAssignment` |
| Veli / Öğrenci kartı | Velilik bağı / öğrencinin ana kaydı | — | `Guardian` / `Student` |
| Yoklama | Devam kaydı | — | `Attendance` |
| Duyuru / Mesaj | Kurum/şube/sınıf/öğrenci kapsamlı yayın / birebir yazışma | — | `Announcement` / `Message` |
| Taksit planı / Tahsilat / Cari | Ücret planı / alınan ödeme / veli hesabı | — | `InstallmentPlan` / `Payment` / `Ledger` |
| AI taslağı | Onay bekleyen AI çıktısı | — | `AiDraft` |

İş kuralları (değişen ve eklenenler **kalın**):
1. Bir kişi birden fazla role sahip olabilir. **Rol ataması kurum düzeyinde (kurucu) veya şube düzeyinde (diğer roller) yapılır.**
2. Veli tek hesapla tüm çocuklarını görür, **çocuklar farklı şubelerde okusa bile.**
3. Bir öğrencinin birden fazla velisi olabilir.
4. Her veri bir kuruma aittir; hiçbir sorgu kurum sınırını geçemez. **Kurum içinde de kullanıcı yalnızca yetkili olduğu şubelerin verisini görür.**
5. AI hiçbir mesajı onaysız veliye göndermez (`AiDraft` → insan onayı).
6. Her AI çıktısında kaynak veri görünür.
7. Para tutarları TRY ile tutulur, float asla kullanılmaz.
8. Mesai dışı sessiz mod.
9. **Bir öğrenci, bir dönemde tek bir şubede ve tek bir sınıfta kayıtlıdır. Şube değiştirmek "nakil" kaydıdır, geçmiş korunur.**

**Dokümanda istenen:**
- Hedef segment: "Tek şubeli veya 2–5 şubeli özel okullar, kolejler, kurs ve etüt merkezleri."
- MVP: "Şube, sınıf, şube-sınıf eşleşmesi, dönem ve akademik takvim."
- Altyapı: "Rol ve yetki yönetimi, şube bazlı görünürlük."
- Faz 3: "Çok şubeli yönetim: Şubeler arası konsolide rapor, merkezi ayarlar."
→ Tüm bu ifadeler "şube = aynı kurumun farklı lokasyonu" anlamıyla tutarlı.

Cevap bekleniyor: Bu tanım ve kurallar (özellikle 1, 4 ve 9) uygun mu?
