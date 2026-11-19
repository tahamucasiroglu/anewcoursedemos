# 0041 — Pi cevapları · Pi keşfi (salt okuma) · iki açık soru

**Tarih:** 2026-11-19 23:40

## İstek

> 1- raspi 5 8gb
> 2- ssh ile erişebilirsin.
> 3- veritabanıda raspide zaten postgre var
> 4- api ve web ayrı daha iyi değil mi api sunsun nasıl oalcak react nasıl sunar api
> 5- localdeyiz erişebilirsin gerekli yerlere sorun yok
> 6- olur
> 7- kaldır dediğimde yaparsın

## Cevap

### Yaptığım (salt okuma)
Bu bilgisayarın SSH ayarında `tmpi` tanımı vardı: 192.168.1.102, kullanıcı `tahapi`. Pi'ye yalnızca bilgi okuyan komutlar gönderdim; hiçbir şey değiştirmedim. Bulduklarım:

| Konu | Durum |
|---|---|
| Cihaz | Raspberry Pi 5, 8 GB RAM, 113 GB disk (%8 dolu) |
| İşletim sistemi | Debian 13 (trixie), 64-bit (aarch64) — uygun |
| PostgreSQL | **17.11** çalışıyor, yalnızca Pi'nin içinden (`127.0.0.1:5432`) erişilebilir. Geliştirmede 18 kullanıyoruz. |
| .NET / nginx | Yok |
| Dolu portlar | 22 (SSH), 5090, 7100–7105 (başka uygulamalar), 5432 (yalnızca içeriden) |
| Yetki | Parolasız `sudo` var |

Kararların kaydedildi:
- 1: Pi 5, 8 GB.
- 2: SSH (`tmpi`); her değiştiren komuttan önce onay alacağım.
- 5: yerel ağ.
- 6: tek komutla dağıtım + son 2 sürüm + geri alma.
- 7: kaldırma yalnızca sen "kaldır" dediğinde.

### Soru 4'ün cevabı: "React'i API nasıl sunar, ayrı daha iyi değil mi?"
React derlenince (`npm run build`) ortaya **düz dosyalar** çıkar: bir `index.html`, birkaç JavaScript ve CSS dosyası. Bunları çalıştırmak için Node gerekmez. Tarayıcı dosyaları alır, uygulama tarayıcıda çalışır. Yani "web'i sunmak", bu dosyaları istenince göndermekten ibarettir. Bu işi herhangi bir web sunucusu yapabilir; API de bir web sunucusu olduğu için o da yapabilirdi.

**Haklısın, ayrı tutmak daha temiz.**
- API yalnızca API olarak kalır.
- Web ileride başka bir yere (CDN, başka sunucu) taşınabilir.
- Biri güncellenirken öteki etkilenmez.

**Önerim — nginx ile ayrı:**
- **nginx** (Pi'de çok hafif, standart bir web sunucusu) web dosyalarını sunar.
- `/api` ile başlayan istekleri Pi'nin içindeki API'ye iletir.
- Adres tek kalır: `http://192.168.1.102:8080`. Web ve mobil buraya bağlanır.
- API dışarıya açılmaz, yalnızca Pi'nin içinden dinler. Bu güvenlik açısından da iyi.
- Port 8080 öneriyorum: 80 boş, ama Pi'deki diğer uygulamalarına ileride yer kalsın diye ana portu işgal etmiyorum.

### Açık soru A — PostgreSQL 17 mi, 18 mi?
Pi'de 17 kurulu; geliştirmede 18 kullanıyoruz.
- **Önerim: Pi'deki 17'yi kullanalım.** Mevcut kuruluma dokunmayız: Pi'deki diğer veritabanları ve uygulamalar etkilenmez.
- Bizim kullandığımız özellikler (tablo, migration, sağlık kontrolü) 17'de aynı çalışır.
- **Risk:** İleride yalnızca 18'de olan bir özelliği kullanırsak Pi'de hata alırız. Bunu yakalamak için dağıtımdan sonraki otomatik kontrol migration'ları ve sistem durumunu doğrular. Ayrıca belgelere "Pi'de PostgreSQL 17; 18'e özgü özellik kullanma" tuzağı eklenir.
- **Alternatif:** Pi'ye 18'i ayrı bir küme olarak (farklı portta) kurmak. Daha fazla kurulum ve bakım demek; şu an gerekmiyor.

### Açık soru B — nginx kurulsun mu?
Pi'de yok; yukarıdaki öneri için `apt install nginx` gerekir.
- nginx yalnızca bizim sitemizi (port 8080) sunar; diğer uygulamalarına dokunmaz.
- Kaldırma betiği bizim site ayarını siler; nginx'in kendisini kaldırmaz.

### Karar
Şu iki öneriyi kabul ediyor musun?
- **A:** Pi'deki PostgreSQL 17 kullanılsın.
- **B:** Web için nginx kurulsun (port 8080, `/api` içeride API'ye iletilir).

Onaylarsan spec'i yazıp öz eleştiriyle birlikte onayına getiririm.
