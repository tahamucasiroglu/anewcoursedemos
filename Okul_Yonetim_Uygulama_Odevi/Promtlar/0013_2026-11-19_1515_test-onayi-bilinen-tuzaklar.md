# 0013 — Konu 7 onayı · Konu 8/12: Bilinen tuzaklar

**Tarih:** 2026-11-19 15:15

## İstek

> onaylıyorum

## Cevap

**Konu 7 — Test: ONAYLANDI.** Katmanlar: Unit + Architecture (fast), Integration gerçek PostgreSQL ile (full), web/mobil component testleri; e2e yok, coverage yüzde hedefi yok. Her koşu kendi `okul_test_<guid>` veritabanını açar ve siler. `docs/testing.md` dosyasına yazılacak.

**Konu 8/12 — Bilinen tuzaklar** (`docs/conventions.md` "Known traps")

Bu bölüm, bu stack'te önceden bilinen ve kodu yazan ajanın ilk plan adımından önce okuyacağı tuzakları listeler. Boş da bırakılabilir; ANEW, review'da bulunan gerçek tuzakları zamanla buraya ekler. Ancak bazıları şimdiden biliniyor (biri bugün kurulumda yaşandı). Önerdiğim başlangıç listesi:

| # | Tuzak | Ne yapılmalı | Kaynak |
|---|---|---|---|
| T1 | **Türkçe "I/ı" sorunu:** `ToUpper()`/`ToLower()`/`string.Compare` makine kültürüyle çalışırsa, `tr-TR`'de `"mail".ToUpper()` → `"MAİL"` olur. E-posta, kod ve anahtar karşılaştırmaları bozulur. | Teknik değerler (e-posta, kod, anahtar) için `ToLowerInvariant()` / `StringComparison.OrdinalIgnoreCase`. Kullanıcı metni aramalarında açıkça `tr-TR` kullanılır. Analizör `CA1304/CA1309/CA1311` hata seviyesinde (K2). | stack bilgisi |
| T2 | **Windows Türkçe locale + PostgreSQL:** `Turkish_Türkiye.1254` ASCII dışı karakter içerdiği için `initdb` çöker. | Küme `--locale C` + UTF8 ile kurulur. Türkçe sıralama, kolon bazında ICU `tr-TR` collation ile yapılır. | bootstrap 2026-11-19 (yaşandı) |
| T3 | **Npgsql + `DateTime.Kind`:** `timestamptz` kolonuna `Kind=Local/Unspecified` bir `DateTime` yazmak exception fırlatır. | Zaman damgaları için `DateTimeOffset` (UTC) veya `DateTime.UtcNow`. Saat `TimeProvider`'dan alınır. | stack bilgisi |
| T4 | **EF Core `decimal` hassasiyeti:** Yapılandırılmazsa para kolonu beklenmeyen hassasiyetle oluşur. | Tüm para property'lerinde `HasPrecision(18, 2)`; bir convention ile topluca. | stack bilgisi |
| T5 | **Global query filter delinmesi:** `IgnoreQueryFilters()`, ham SQL (`FromSql`) veya `DbContext`'e doğrudan erişim kiracı filtresini atlar (S1). | Bunlar yalnızca açıkça gerekçelendirilmiş yerlerde ve review onayıyla kullanılır. Architecture testi `IgnoreQueryFilters` kullanımını izinli listede tutar. | stack bilgisi |
| T6 | **`dotnet` build sunucusu artığı:** `dotnet build/test`, MSBuild node'larını ve Roslyn sunucusunu (VBCSCompiler) arkada açık bırakır; resource hygiene testi kırılır. | `check.conf`'ta `--disable-build-servers` + `-nodeReuse:false` + `DOTNET_CLI_USE_MSBUILD_SERVER=0`. | stack bilgisi |
| T7 | **N+1 sorgu / takip (tracking) maliyeti:** Liste sorgularında `Include` unutulursa veya tracking açık kalırsa performans düşer ("10 saniyede yoklama" hedefi). | Okuma sorgularında `AsNoTracking()` ve projeksiyon (`Select` → DTO). | stack bilgisi |

Önerim: T1–T7 ile başlamak. Özellikle **T1 ve T2 bu proje için gerçek tehlike**: ürün Türkçe ve makine Türkçe locale'de.

**Dokümanda istenen:** Doküman teknik tuzaklardan bahsetmiyor. Dolaylı bağlantılar:
- "Öğretmen mobilden 10 saniyede yoklama alır" → T7 (performans)
- "tenant bazlı veri izolasyonu" → T5
- Tahsilat/makbuz → T4
- "Excel ve e-Okul çıktısından içe aktarma" → T1 (Türkçe karakterli isimler)

Cevap bekleniyor: T1–T7 başlangıç listesi olarak uygun mu?
