# 0011 — Konu 5 onayı · Konu 6/12: Konvansiyonlar (isimlendirme, veri, hata yönetimi)

**Tarih:** 2026-11-19 15:10

## İstek

> onaylıyorum

## Cevap

**Konu 5 — Mimari: ONAYLANDI.** nArchitecture monolit; 7 bağımlılık kuralı + K1/K2/K3. Kurallar `docs/architecture.md` ve `docs/conventions.md` dosyalarına yazılacak; derleme kuralları `Directory.Build.props` + `.editorconfig`, mimari kurallar `tests/Architecture` ile zorlanacak.

**Konu 6/12 — Konvansiyonlar** (`docs/conventions.md`)

**A. Dil kuralları**

| Ne | Dil | Gerekçe |
|---|---|---|
| Kod tanımlayıcıları (class, metot, tablo, kolon, API yolu) | **İngilizce** (`Student`, `Guardian`, `InstallmentPlan`) | nArchitecture ve .NET ekosistemi İngilizce; ı/ş/ğ sorunları olmaz |
| `/// <summary>` açıklamaları (K3) | **Türkçe** | Senin kararın |
| Kullanıcıya görünen metinler (UI, hata mesajı, bildirim) | **Türkçe**, kaynak dosyalardan (resx / i18n JSON); koda gömülmez | Ürün Türkiye pazarı için; ileride ek dil eklenebilir |
| Branch adları, commit mesajları | **İngilizce, Conventional Commits** (`feat(attendance): ...`) | Araçlar ve ANEW gate'leri bu formatı okur |
| Terim eşleşmesi | `docs/domain.md` terim tablosu (Kurum=`Tenant`, Şube=`Branch`, Sınıf=`Classroom`...) | Her yerde aynı kelime |

**B. İsimlendirme (.NET)**
- nArchitecture düzeni: `Features/<Modül>/Commands/<Eylem>/<Eylem><Varlık>Command.cs` + `...CommandHandler` + `...CommandValidator`; Query'ler aynı düzende. K1 gereği her biri ayrı dosyada.
- Arayüzler `I` önekiyle (`IPaymentProvider`). Async metotlar `Async` sonekiyle. Sınıflar varsayılan olarak `sealed`.
- Test adı: `<Metot>_<Durum>_<Beklenen>` (ör. `Handle_StudentFromOtherTenant_ThrowsNotFound`).
- Veritabanı: tablo ve kolonlar **snake_case** (PostgreSQL alışkanlığı; `EFCore.NamingConventions` paketi ile otomatik).

**C. Veri kuralları**
1. **Para:** C#'ta `decimal`, PostgreSQL'de `numeric(18,2)`, para birimi TRY. `float`/`double` para için **yasak**. Taksit bölmede kuruş farkı **son taksite** eklenir (toplam her zaman ana tutara eşittir).
2. **Zaman:** Veritabanında her zaman **UTC** (`timestamptz`). Kullanıcıya **Europe/Istanbul** saatiyle gösterilir. Sadece-tarih alanları (doğum tarihi, ders günü) `DateOnly` / `date` olur. Saat kaynağı `TimeProvider` üzerinden alınır; testlerde sabit saat kullanılır.
3. **Kimlikler (ID):** **UUID v7** (`Guid.CreateVersion7()`, .NET 9+). Sıralanabilir, tahmin edilemez, dağıtık üretime uygun. Ardışık int ID dışarıya açılmaz, çünkü başka okulun kayıt sayısını sızdırır.
4. **Kiracı alanları:** Kiracıya ait her tabloda `tenant_id` (zorunlu). Şubeye bağlı olanlarda `branch_id`. İkisi de istemciden alınmaz, **oturumdan** gelir.
5. **Silme:** İş kayıtları **soft delete** (`deleted_at`) ile silinir; nArchitecture `Entity` tabanı bunu destekliyor. KVKK saklama süresi dolan kişisel veri ise **anonimleştirilir** (S6 kuralı).
6. **Denetim alanları:** Her varlıkta `created_at/by`, `updated_at/by` bulunur. Hassas okuma ve değişiklikler ayrıca audit log'a yazılır.
7. **TC kimlik no:** 11 hane + algoritma doğrulaması. Saklanırken şifrelenir, gösterilirken maskelenir (`*******1234`). Ayrıntısı Konu 9'da.
8. **Telefon:** E.164 formatında saklanır (`+905xxxxxxxxx`).
9. **Metin karşılaştırma:** Kullanıcı aramalarında Türkçe büyük-küçük harf duyarsızlığı (`tr-TR` ICU collation) kullanılır. E-posta ve kod alanları ise küçük harfe `ToLowerInvariant` ile çevrilir.

**D. Hata yönetimi** (nArchitecture deseni)
- İş kuralı ihlali → `BusinessException` → HTTP **400**, Türkçe mesajla.
- Doğrulama hatası → `ValidationException` (FluentValidation) → HTTP **400**, alan bazlı liste.
- Yetki yok → `AuthorizationException` → **403**. Kayıt yok **veya başka kiracıya ait** → **404**. Bu durumda "var ama sizin değil" bilgisi verilmez.
- Beklenmeyen hata → **500**, kullanıcıya yalnızca genel mesaj ve izleme kimliği (`traceId`) gösterilir. Stack trace, SQL ve kişisel veri **asla** yanıta konmaz.
- Tüm hatalar **RFC 9457 ProblemDetails** formatında döner. nArchitecture'ın global exception middleware'i bunu yapar.
- Loglarda kişisel veri (S2/S3) maskelenir veya hiç yazılmaz. Ayrıntısı Konu 9'da.
- Dış servis (port) hataları uygulamayı çökertmez: zaman aşımı + yeniden deneme + "beklemede" durumu.

Önerim: A–D'nin tamamı. En kritikleri C1 (para decimal), C2 (UTC), C3 (UUID v7), C4 (tenant_id oturumdan) ve D'deki "başka kiracı = 404" kuralı.

**Dokümanda istenen:**
- Tahsilat: "Yıllık ücret, taksit planı, indirim ve burs tanımları… Makbuz/fatura çıktısı, cari takibi" → para kuralları (C1).
- "Audit log: kim, hangi öğrenci kaydını, ne zaman görüntüledi/değiştirdi" → C6.
- "Mezun/ayrılan öğrenci verisi için saklama süresi ve anonimleştirme" → C5.
- "Şifreli veri saklama" → C7 (Konu 9'da ayrıntı).
- "Mesai saati dışı sessiz mod" → C2 (Europe/Istanbul saat dilimi).
- "Excel ve e-Okul çıktısından tek tıkla içe aktarma" → içe aktarılan veriye de aynı doğrulamalar (TC, telefon) uygulanır.
- Doküman isimlendirme ve hata formatı konusunda bir şey söylemiyor; bunlar teknik öneri.

Cevap bekleniyor: A–D konvansiyonları uygun mu? Değiştirmek istediğin madde var mı?
