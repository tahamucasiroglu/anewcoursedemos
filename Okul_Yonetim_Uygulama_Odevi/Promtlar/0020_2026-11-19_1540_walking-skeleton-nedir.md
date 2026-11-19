# 0020 — Walking skeleton nedir?

**Tarih:** 2026-11-19 15:40

## İstek

> walking skeleton ne demek

## Cevap

**Walking skeleton (yürüyen iskelet):** Sistemin **tüm katmanlarını uçtan uca birbirine bağlayan, en küçük çalışan parçası**. İşlevi neredeyse yoktur ama "kemikleri" yerindedir ve **yürür**: derlenir, çalışır, test edilir, kontrol edilir.

Benzetme: Bir binaya önce iskeleyi, temeli, su ve elektrik tesisatını çekersin; odaların içi boştur ama musluk açınca su gelir. Sonraki her oda (özellik) bu tesisata bağlanır.

**Bizim projede somut olarak:**
```
Mobil (Expo) ve Web (React) ekranı  →  "Sistem çalışıyor ✔" yazısı
        │  (ortak api-client ile HTTP)
        ▼
.NET 10 Web API  →  GET /health
        │  (nArchitecture: Controller → Query → Handler)
        ▼
PostgreSQL 18  →  "SELECT 1" — veritabanı ayakta mı?
```
Bununla birlikte kurulanlar:
- Çözüm ve 5 katman projesi (Domain, Application, Persistence, Infrastructure, WebAPI) + test projeleri
- `Directory.Build.props` / `.editorconfig`: senin K1 (dosya başına tek tip), K2 (uyarı = hata), K3 (tip başına summary) kuralların **burada devreye girer**
- Architecture testleri (yasak bağımlılıklar)
- Test veritabanı yaşam döngüsü (`okul_test_<guid>` aç → kullan → sil)
- `scripts/check.conf` adımlarının yorumdan çıkarılması → `./scripts/check` artık gerçekten derler ve test eder; residue testi ilk kez ölçülür

**Neden önce bu:**
1. **Riskleri en başta yakalar:** .NET 10 + nArchitecture 2.0 + Npgsql + PostgreSQL 18 + Expo birlikte gerçekten çalışıyor mu? Bunu yoklama özelliğinin ortasında değil, boş bir iskelette öğrenmek ucuzdur.
2. **Kurallar ilk günden işler:** K1–K3 ve mimari testler ilk koddan itibaren aktif olur; sonradan eklemek yüzlerce dosyayı düzeltmek demektir.
3. **ANEW gereği:** "Kanıt = check yeşil". Check'in kontrol edecek adımı olmadan hiçbir özellik "bitti" sayılamaz.
4. **Sonraki her özellik sadece kendi işine odaklanır:** Yoklama spec'i yalnızca yoklamayı anlatır, altyapı kurmaz.

**Ne değildir:** Kullanıcıya değer veren bir özellik değildir. Giriş ekranı, kurum/öğrenci kaydı, yoklama yok. Onlar sonraki `/new-feature`'lar.
