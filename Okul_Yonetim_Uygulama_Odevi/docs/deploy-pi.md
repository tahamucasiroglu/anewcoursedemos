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
| Gizli bilgi | `/etc/okul/okul-api.env` — bağlantı bilgisi ve şifre; `/etc/okul/okul-jwt.env` — oturum imza anahtarı (spec 0004); `/etc/okul/okul-2fa.env` — iki adımlı doğrulama şifreleme anahtarı (spec 0007); `/etc/okul/okul-data.env` — sağlık bilgisi şifreleme anahtarı (spec 0009, ayrı anahtar). Hepsini **yalnızca root okur (600)**; repoda/sohbette/logda yok |
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
| Platform yöneticisi yetkisi ver / al; iki adımlı doğrulamayı sıfırla (spec 0007) | `bash tools/pi/platform-admin.sh grant-platform-admin --eposta kisi@ornek.com` · `revoke-platform-admin` · `reset-two-factor` — hesap önceden var olmalı; kişinin açık oturumları kapanır |
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

## E-posta (SMTP) — spec 0006

SMTP bilgileri girilmedikçe uygulama e-posta göndermez ("Şifremi unuttum" yine aynı mesajı verir; log'a "SMTP ayarlı
değil" düşer). Bilgiler hazır olunca Pi'de `/etc/okul/okul-api.env` dosyasına (root, 600) şu satırlar eklenir ve
servis yeniden başlatılır (`sudo systemctl restart okul-api`; Pi'nin kendisi yeniden başlatılmaz):

```
Email__Host=smtp.<saglayici>
Email__Port=587
Email__UseStartTls=true
Email__UserName=<kullanici>
Email__Password=<yalnızca bu dosyada>
Email__FromAddress=okul@<alan-adi>
Email__FromName=Okul Yönetim
Email__WebBaseUrl=http://192.168.1.102:8080
```

Şifre repoya, sohbete ya da loglara yazılmaz. Yalnızca 587/STARTTLS desteklenir (465 gerekirse MailKit'e geçiş — plan
0006 Risks 1).

## Yerel yapay zekâ (sesli not) — spec 0012

Pi'de model çalışmaz. Varsayılan: `Ai__*` değişkenleri yok → sesli not "Yapay zekâ şu an kullanılamıyor. Mesajı elle
yazabilirsiniz." der, mesajlaşma normal çalışır. **İsteğe bağlı demo (LAN) modu** — aynı yerel ağdaki Windows bilgisayar
açıkken (ayrıntı ve güvenlik duvarı kuralı: `tools/ai/README.md`), `/etc/okul/okul-api.env` dosyasına (root, 600) şu
satırlar eklenir ve servis yeniden başlatılır (`sudo systemctl restart okul-api`):

```
Ai__TextGenerator__BaseUrl=http://<bilgisayar-ip>:11434
Ai__TextGenerator__Model=gemma3:4b
Ai__SpeechToText__BaseUrl=http://<bilgisayar-ip>:8765
```

Yalnızca yerel adresler kabul edilir (`192.168.x`, `10.x`, `172.16–31.x`, `localhost`); başka adres yazılırsa özellik kapalı
kalır. Demo bitince satırlar silinip servis yeniden başlatılır. Bu plan Pi'ye dokunmaz; adımlar ship'te kullanıcıdadır.

## Platform paneli ve iki adımlı doğrulama — spec 0007

Bu spec'te Pi'ye dokunulmadı; yayınlarken (ship) kullanıcı şu sırayla çalıştırır:

1. `bash tools/pi/setup.sh database` — `/etc/okul/okul-2fa.env` yoksa üretir (root, 600; ekrana basılmaz; varsa dokunmaz —
   değişirse kurulu doğrulamalar çözülemez). Servis dosyası için `bash tools/pi/setup.sh service` (servis yeniden
   başlar; Pi'nin kendisi yeniden başlatılmaz).
2. `bash tools/pi/deploy.sh` — yeni sürüm (veritabanı geçişi açılışta uygulanır; mevcut okullar etkin kalır).
3. `bash tools/pi/platform-admin.sh grant-platform-admin --eposta <hesabınız>` — ardından web'den giriş; ilk girişte
   kare kodu doğrulayıcı uygulamayla okutup kodu girin.

Anahtar dosyası yoksa servis yine açılır; yalnızca platform yöneticisi girişi "iki adımlı doğrulama yapılandırılmamış"
ile reddedilir. Telefon kaybında: `reset-two-factor` (sonraki girişte yeniden kurulum).

## Öğrenci ve veli kayıtları — spec 0009

Bu spec'te Pi'ye dokunulmadı; yayınlarken (ship) kullanıcı şu sırayla çalıştırır:

1. `bash tools/pi/setup.sh database` — `/etc/okul/okul-data.env` (sağlık notu ve alerji şifreleme anahtarı,
   `PersonalData__EncryptionKey`) yoksa üretir (root, 600; ekrana basılmaz; varsa dokunmaz). Servis dosyası için
   `bash tools/pi/setup.sh service` (servis yeniden başlar).
2. `bash tools/pi/deploy.sh` — yeni sürüm; `StudentsAndGuardians` geçişi açılışta uygulanır (yalnızca yeni tablolar).

Anahtar dosyası yoksa servis yine açılır; öğrenci kartı çalışır, yalnızca sağlık alanları "Sağlık bilgisi şu anda
gösterilemiyor." gösterir ve dolu sağlık bilgisi kaydedilemez. **Anahtar kaybolursa kayıtlı sağlık bilgisi okunamaz:**
`/etc/okul/okul-data.env` dosyasını veritabanı yedeğinden ayrı, güvenli bir yerde saklayın (B-14 ile birlikte). Anahtar
değiştirilmemeli; değişirse eski kayıtlar "gösterilemiyor" olur.
