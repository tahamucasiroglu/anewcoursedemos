# 0012 — Konu 6 onayı (summary yalnız tip başında, metinler resx) · Konu 7/12: Test

**Tarih:** 2026-11-19 15:13

## İstek

> class başı summary yeter içindeki method yada değişken için summaryler yine class başında anlatılsın. dil desteği şuan mvp için gereksiz türkçe yeter ama türkçeyide resx mi neydi tablo olarak tutulan metinler oradan al ileride dilleri kolay ekleriz. bunlarla devam et onaylıyorum

## Cevap

**Konu 6 — Konvansiyonlar: DEĞİŞİKLİKLERLE ONAYLANDI.**

- **K3 güncellendi:** `/// <summary>` **yalnızca tip başında** zorunlu (class, interface, enum, record, struct). Metotlar, property'ler ve alanlar için ayrı summary yazılmaz. Önemli olanlar tipin summary'sinde kısaca anlatılır.
  - Nasıl zorlanır: StyleCop `SA1600` metotları da kapsadığı için kapatılır. Bunun yerine `GenerateDocumentationFile` açık kalır ve `tests/Architecture` içinde bir test, derlenen tüm assembly'lerdeki **her tipin** XML belgesinde boş olmayan bir `<summary>` olduğunu doğrular. Eksik summary testi kırmızıya düşürür, `scripts/check` de kırmızı olur. `CS1591` (üye belgesi eksik) uyarısı kapatılır; aksi halde K2 yüzünden her metot için hata verirdi.
- **Dil desteği:** MVP'de **yalnızca Türkçe**. Ama kullanıcıya görünen hiçbir metin koda gömülmez:
  - .NET: **`.resx` kaynak dosyaları** (`Messages.resx` → anahtar/değer tablosu: `Student.NotFound` = "Öğrenci bulunamadı."). İleride `Messages.en.resx` eklemek yeni dil demek; kod değişmez.
  - Web/mobil (TypeScript `.resx` okumaz): Aynı mantıkta `tr.json` anahtar/değer dosyası.
- Geri kalan A–D aynen onaylandı (İngilizce kod adları, para `decimal`, UTC, UUID v7, kiracı kimliği oturumdan, soft delete, "başka kiracı = 404", ProblemDetails).

**Konu 7/12 — Test beklentileri** (`docs/testing.md`)

ANEW kuralı: Her kabul kriteri en az bir testle ispatlanır. Test, kriteri ispatlayabilen **en ucuz katmanda** yazılır. Tüm testler `scripts/check` içinde koşar.

| Katman | Araç | Ne test edilir | Profil |
|---|---|---|---|
| **Unit** | xUnit v3 + Shouldly + NSubstitute | İş kuralları (Rules), handler mantığı, para/taksit hesabı, TC doğrulama, domain metotları | **fast** |
| **Architecture** | NetArchTest + XML-doc testi | 7 bağımlılık kuralı, K3 (her tipte summary) | **fast** |
| **Integration** | xUnit v3 + `WebApplicationFactory` + **gerçek PostgreSQL 18** (yerel servis) | API uçtan uca (HTTP → handler → DB): **kiracı/şube izolasyonu**, yetki, 404/403, migration'lar | **full** |
| **Web/mobil component** | Vitest + React Testing Library (web), Jest + RNTL (mobil) | Ekran durumları (boş, yükleniyor, hata, başarılı, yetkisiz) | fast |
| **e2e (tarayıcı)** | **Yok** (önerim) | — | — |

**Zorunlu test edilecekler:**
1. Her iş kuralı (`docs/domain.md` BR-1…BR-9).
2. **Her hassas alan (S1–S6) kriteri**, özellikle "A kurumunun kullanıcısı B kurumunun öğrencisini göremez / değiştiremez" ve "şube yetkisi olmayan kullanıcı o şubenin verisini göremez". Bunlar her yeni uç noktada tekrar yazılır.
3. Para hesapları: taksit bölme (kuruş farkı son taksitte), indirim/burs, gecikme.
4. "AI onaysız gönderemez" kuralı.
5. Yasak bağımlılıklar ve K3 (architecture testleri).

**Test verisi ve izolasyon (Resource hygiene, kural 5):**
- Integration testleri her koşuda **yeni, rastgele adlı bir test veritabanı** (`okul_test_<guid>`) oluşturur, migration'ları uygular ve koşu sonunda **siler**. Koşu yarıda kesilse bile bir sonraki koşu temiz başlar; eski `okul_test_*` veritabanları başta temizlenir.
- Geliştirme veritabanına (`okul_dev`) testler **asla** dokunmaz.
- Saat `FakeTimeProvider` ile sabit, kültür `tr-TR`, rastgelelik sabit seed ile çalışır (determinizm).
- Testler dış servise gerçekten çıkmaz. Ödeme/SMS/AI sahte port uygulamalarıyla çalışır.
- `dotnet test` build sunucusunu ayakta bırakmaz: `--disable-build-servers` / `-nodeReuse:false` (resource hygiene). Ayrıntı ÜRETİM adımındaki `check.conf`'ta.

**e2e neden yok:** Bir tarayıcı testinin ispatlayabildiği her şeyi integration (API) + component (ekran durumu) testleri daha ucuz ve daha kararlı ispatlıyor. UI kriterleri için ANEW zaten VERIFY'da ajanın çektiği ekran görüntüsünü istiyor; onları SHIP'te sen onaylıyorsun. İleride kritik bir yolculuk için (ör. "veli öder → makbuz") e2e gerekirse, bütçesiyle birlikte ayrı bir kararla eklenir.

**Kapsam (coverage) yüzdesi hedefi:** Önerim **koymamak**. ANEW'in sözleşmesi "her kriter ↔ test". Yüzde hedefi, anlamsız test yazmaya iter.

**Dokümanda istenen:**
- Doküman test konusunda doğrudan bir şey söylemiyor.
- Dolaylı olarak en çok test isteyen yerler: "tenant bazlı veri izolasyonu", "şube bazlı görünürlük", "AI hiçbir mesajı onaysız veliye göndermez", tahsilat/taksit/makbuz, "Öğretmen mobilden 10 saniyede yoklama alır" (bu bir performans/UX hedefi; component testi + VERIFY'da ölçüm).

Cevap bekleniyor: Test katmanları, e2e olmaması ve test veritabanı yaklaşımı uygun mu?
