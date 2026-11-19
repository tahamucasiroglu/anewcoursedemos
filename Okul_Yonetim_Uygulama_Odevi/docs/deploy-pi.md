# Raspberry Pi deneme ortamı — kullanım kılavuzu

> Spec 0003 · ADR-0004. Pi: Raspberry Pi 5 (8 GB), Debian 13 64-bit, `192.168.1.102`. Yalnızca yerel ağ, düz HTTP.
> Komutlar geliştirme makinesinde **Git Bash** ile, repo kökünden çalıştırılır. Pi'ye SSH anahtarıyla bağlanılır
> (`~/.ssh/config` içindeki `tmpi` tanımı). Tüm ayarlar tek dosyada: `tools/pi/pi.conf`.

## Düzen (Pi'de)
| Ne | Nerede |
|---|---|
| Web paneli | nginx, port **8080** → `/opt/okul/web/current` (ayrı birim) |
| API | systemd servisi `okul-api`, kullanıcı `okul` (root değil, oturum açamaz), yalnızca `127.0.0.1:5080` |
| Ağ geçidi | nginx `/api/…` isteklerini içeride API'ye iletir; dışarıya açık tek port 8080 |
| Sürümler | `/opt/okul/{api,web}/releases/<UTC zaman>-<git sha>`; etkin olan `current`; birim başına son **2** sürüm |
| Veritabanı | Pi'deki **PostgreSQL 17**; veritabanı `okul`, kullanıcı `okul_app` (yönetici değil, yalnızca `okul`'a bağlanır) |
| Gizli bilgi | `/etc/okul/okul-api.env` — bağlantı bilgisi ve şifre; `/etc/okul/okul-jwt.env` — oturum imza anahtarı (spec 0004). İkisini de **yalnızca root okur (600)**; repoda/sohbette/logda yok |
| Loglar | `/var/log/okul/okul-api-YYYYMMDD.log`, 30 gün |

## Komutlar
| Ne için | Komut |
|---|---|
| İlk kurulum (bir kez; tekrar çalıştırılabilir) | `bash tools/pi/setup.sh` (veya adım adım: `nginx`, `system`, `database`, `service`) |
| Yeni sürüm gönder (web + API) | `bash tools/pi/deploy.sh` |
| Yalnızca web / yalnızca API | `bash tools/pi/deploy.sh web` · `bash tools/pi/deploy.sh api` |
| Önceki sürüme dön | `bash tools/pi/rollback.sh` (veya `web` / `api`) |
| Durum (salt okuma) | `bash tools/pi/status.sh` |
| Okul + kurucu hesabı aç (spec 0004) | `bash tools/pi/create-school.sh --okul "Çınar Koleji" --sube "Merkez" --ad "Ayşe Yılmaz" --eposta ayse@ornek.com` — şifre ekranda görünmeden iki kez sorulur (argüman değil); e-posta zaten varsa sorulmaz ve şifre değişmez; aynı okul tekrar verilirse "Zaten var" der |
| Kaldır (**yalnızca istendiğinde**) | `bash tools/pi/uninstall.sh` → `KALDIR` yazılır (etkileşimsiz: `--onay KALDIR`) |

**Dağıtım ne yapar:** API'yi Pi için (.NET gömülü, `linux-arm64`) ve web'i derler → Pi'ye kopyalar → yeni sürüme geçer →
`http://127.0.0.1:8080/api/system-health` "healthy" mi bakar. Değilse **kendiliğinden önceki sürüme döner** ve hata
verir. Aynı anda ikinci bir dağıtım başlatılamaz (kilit). Pi'de 1 GB'tan az boş alan varsa başlamaz.

**Geri alma ve veritabanı:** Geri alma yalnızca uygulama sürümünü değiştirir; veritabanı şeması geri alınmaz. Geri
dönülen sürümden sonra eklenmiş bir migration varsa komut uyarı verir.

## Erişim
- Tarayıcı: `http://192.168.1.102:8080`
- Mobil (Expo Go, telefon aynı ağda): `mobile/.env.local` içine `EXPO_PUBLIC_API_BASE_URL=http://192.168.1.102:8080`
  yazıp `npm --workspace mobile run start` → telefonda Expo Go ile QR kodu okutun. (Expo'nun **tarayıcı** sürümü Pi'ye
  farklı adresten bağlandığı için CORS'a takılır; Pi'de geliştirme adresine CORS izni açılmaz — telefon kullanılır.)
- **Pi'nin IP'si değişirse:** `tools/pi/pi.conf` içinde `PI_ADDRESS`'i ve mobildeki adresi güncelleyin; web yeniden
  derlenmez (sayfayla aynı adresi kullanır). Öneri: modemde Pi'ye sabit IP verin.

## Kaldırma neyi siler, neyi korur
Silinir: `okul-api` servisi, `/opt/okul`, `/var/log/okul`, `/etc/okul`, nginx site ayarı `okul.conf`, `okul`
veritabanı, `okul_app` kullanıcısı, `pg_hba.conf`'taki `# okul-begin … # okul-end` bloğu, `okul` sistem kullanıcısı.
Korunur: nginx, PostgreSQL ve Pi'deki diğer uygulamalar, veritabanları, portlar.

## Sorun giderme
- `bash tools/pi/status.sh` — servis, portlar, sürümler, veritabanı yetkileri, son loglar.
- Pi'de: `sudo journalctl -u okul-api -n 50`, `sudo tail -n 50 /var/log/okul/okul-api-$(date +%Y%m%d).log`.
- "Pi'ye ulaşılamıyor": Pi açık ve aynı ağda mı, `ssh tmpi` çalışıyor mu?
