# 0025 — Plan onayı · BUILD (kodlama) · bağımsız REVIEW · TRIAGE kapısı

**Tarih:** 2026-11-19 18:10

## İstek

> onaylıyorum

## Cevap

### Son mesajdan beri yapılanlar
1. **Plan onaylandı:** Plana "tahamucasiroglu, 2026-11-19" yazıldı, spec "In progress" durumuna alındı (`faa4c2b`).
2. **BUILD — 9 adımın hepsi bitti:**
   - Backend (.NET 10, nArchitecture): sistem durumu ucu, loglama, hata yakalama, CORS, PostgreSQL, ilk migration. **31 test** yeşil (`c9d6593`).
   - Web (React) ve mobil (Expo): 4 durum + "Tekrar dene"; ortak API istemcisi API tanımından üretiliyor. Web 7, mobil 7 test yeşil (`f8e3d7d`).
   - `scripts/check` açıldı: hızlı profil 7 adım (~27 sn), tam profil 10 adım (~55 sn), **ikisi de yeşil**. "Arkada süreç kaldı mı" testi iki profilde **temiz**.
   - doctor: **0 hata, 0 uyarı**.
3. **Build sırasında çıkan sorunlar ve çözümleri:**
   - "Uyarı = hata" kuralın ilk derlemede çalıştı ve bir log uyarısını yakaladı.
   - .NET 10'da eski test çalıştırıcısı desteklenmiyor → yeni çalıştırıcıya geçildi.
   - nArchitecture'ın metin yükleyicisi bölüm adını klasör adından alıyor → bölüm adı düzeltildi.
   - Testlerde loglar aynı dosyaya karışıyordu → her API örneği kendi logger'ını kullanıyor.
   - Windows test sonrası log dosyasını kısa süre kilitliyordu (flaky) → temizlik kilidi bekliyor; 5 ardışık koşu yeşil.
   - Expo lint paketi ESLint 10 ile uyumsuz çıktı → web ile aynı kural seti kullanıldı.
   - Bunların hepsi plana "sapma kaydı" (A-1…A-8) olarak yazıldı.
4. **Bağımsız inceleme:** Kodu yazmayan, salt okunur bir AI kodu inceledi (ANEW kuralı: işi yapan kendi işini doğrulamaz). Sonuç: 1 yüksek, 2 orta, 7 düşük, 8 bilgi notu.

### İnceleme bulguları — her biri için önerim
(Düzelt = gerçek, düzeltelim · Ertele = backlog'a · Gürültü = gerek yok)

| # | Bulgu (sade anlatım) | Önem | Önerim |
|---|---|---|---|
| 1 | **Bazı mimari testler hiç kırmızıya düşemiyor.** "Npgsql yalnızca veri katmanında", "Application Serilog kullanmaz" gibi kurallar test aracının yüklemediği paketlere baktığı için her zaman geçiyor. Kuralın kendisi doğru, test kör. | Yüksek | **Düzelt** + "bu kurallar gerçekten çalışıyor mu" kontrol testleri + tuzak listesine yeni satır |
| 2 | **API katmanı veritabanı bağlamını doğrudan kullanıyor** (başlangıçta şema geçişi için). Spec "API katmanı veritabanına doğrudan bağlanmaz" diyor; test yalnızca controller'lara bakıyordu. | Orta | **Düzelt:** şema geçişini veri katmanına taşı, kuralı tüm API katmanına genişlet |
| 3 | **npm güvenlik taraması belgede yazandan dar:** geliştirme araçlarını atlıyor. | Orta | **Düzelt:** belgedeki gibi tümünü tara (bugün yine geçiyor) |
| 4 | Kullanıcı sayfayı kapatınca (istek iptal) log'a "beklenmeyen hata" yazılıyor → hata logları gürültüyle dolar. | Düşük | **Düzelt** + test |
| 5 | "Uyarı = hata" testi yalnızca ana ayar dosyasına bakıyor; bir proje kuralı kendi içinde kapatsa test görmez. | Düşük | **Düzelt:** tüm proje/ayar dosyalarını tara |
| 6 | Aynı anda iki tam kontrol koşarsa birbirinin test veritabanını silebilir. | Düşük | **Düzelt:** yalnızca 1 saatten eski artıkları sil |
| 7 | Sunucu her yanıta "Server: Kestrel" başlığı ekliyor; spec "sunucu bilgisi sızmaz" diyor. | Düşük | **Düzelt** (tek satır) |
| 8 | Plan listesinde olmayan birkaç küçük şablon dosyası; güvenlik belgesine "testler postgres kullanıcısıyla çalışır" notu yazılmamış. | Düşük | **Düzelt:** sapma kaydını genişlet + notu yaz |
| 9 | Planda tek tek yazılmamış birkaç yardımcı paket (test araçları vb.) + kullanılmayan bir paket kaydı. | Düşük | **Düzelt:** yeni sapma kaydı A-9 ile listele, kullanılmayanı sil |
| 10 | "Tekrar dene" testleri, basınca "yükleniyor"a döndüğünü ve "hizmet dışı"dan tekrar denemeyi sınamıyor. | Düşük | **Düzelt:** 2 kontrol ekle |
| 11 | İleride kişisel veri taşıyan bir işlem "loglanabilir" işaretlenirse nArchitecture içeriğini olduğu gibi loga yazar. | Bilgi | **Tuzak listesine ekle** (bugün sorun yok) |
| 12 | Mobilde durum değişimi iPhone ekran okuyucusunda otomatik duyurulmuyor (Android'de duyuruluyor). | Bilgi | **Ertele** (backlog) |
| 13 | "healthy" yazısı iki yerde elle karşılaştırılıyor. | Bilgi | **Düzelt** (enum'dan türet) |
| 14 | Lint kuralı `globalThis.fetch` / `window.fetch` yazımını yakalamıyor. | Bilgi | **Düzelt** |
| 15 | Eski test çalıştırıcısı paketleri hâlâ projelerde. | Bilgi | **Düzelt:** kaldır |
| 16 | Otomatik üretilen API tanımında Windows satır sonu var; Pi'de derlenirse dosya gereksiz değişir. | Bilgi | **Düzelt** |
| 17 | Log klasörü göreli yol; Pi'de servis olarak çalışınca beklenmedik yere yazabilir. | Bilgi | **Ertele:** Pi spec'inde (0002) çözülür |
| 18 | Doğrulamada "bir dosyada iki record" vakası da denensin. | Bilgi | **Kabul:** doğrulamaya eklenir |

### Plandan sapmalar (A-1…A-8) — inceleme hepsine "onay" öneriyor
- **A-1** Yeni test çalıştırıcısı (.NET 10 zorunluluğu)
- **A-2** Birkaç gerekli küçük dosya
- **A-3** Bir performans uyarısı (CA1873) kapatıldı
- **A-4** Metin bölümü = klasör adı
- **A-5** Mobil lint = web lint
- **A-6** TypeScript 6 için paket ayarı (overrides)
- **A-7** Log/izleme numarası düzeni
- **A-8** Şablon artıkları commit edilmedi

**Senden ayrı bir iş:** Şu şablon dosyalarını silmeme otomatik mod izin vermedi; sen silmelisin. `mobile/AGENTS.md` ve `mobile/.claude/settings.json` ayrıca Expo'nun kendi AI talimatlarını içeriyor ve bizim kurallarımızla çelişiyor:
`mobile/AGENTS.md`, `mobile/.claude/`, `mobile/LICENSE`, `web/README.md`, `web/.oxlintrc.json`, `web/src/assets/`, `web/src/App.css`, `web/public/icons.svg`.

### Sonraki adım
Onaylarsan düzeltme turu 1 yapılır (bulgu başına bir commit). Orta seviye bulgu olduğu için ardından **yeniden bağımsız inceleme**, sonra **doğrulama** gelir: her kabul kriterinin kanıtı, kuralları kasıtlı bozma denemeleri, ekran görüntüleri.

### Karar (tek)
Yukarıdaki önerilerin hepsini kabul ediyor musun? Yani 18 bulgunun karar sütunu, A-1…A-8'in onayı ve A-9.
