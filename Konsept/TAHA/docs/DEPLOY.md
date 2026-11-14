# Taha Konsept — Raspberry Pi Yayını (README-DEPLOY)

> Hedef: Pi 5, Debian 13 arm64 (`ssh tmpi`, LAN `192.168.1.102`). .NET runtime **yok** → her iki host **self-contained `linux-arm64`** yayınlanır.
> Dosyalar: `deploy/publish-pi.ps1`, `deploy/pi/{install-release.sh, rollback.sh, tahakonsept-api.service, tahakonsept-web.service}`, `src/Hosts/*/appsettings.Pi.json`, `scripts/{dev-tunnel.ps1, dev-run.ps1}`.

## 1. Pi düzeni

| Ne | Yer |
|---|---|
| Sürümler | `/opt/tahakonsept/releases/<VERSION>-<yyyyMMdd-HHmmss>/{api,web}` (root sahipli, salt okunur) |
| Etkin sürüm | `/opt/tahakonsept/current` → sembolik bağ (atomik çevrilir) |
| Önceki sürüm | `/opt/tahakonsept/previous` → sembolik bağ (geri dönüş hedefi) |
| Yardımcı betikler | `/opt/tahakonsept/bin/{rollback.sh, install-release.sh}` |
| Veri (yazılabilir tek yer) | `/var/lib/tahakonsept` (kullanıcı `tahakonsept`, 0750) |
| App_Data | `/var/lib/tahakonsept/App_Data` (`keys/` 0700: `jwt.key`, `master.key`, `web-dataprotection/`; `artifacts/`) |
| Loglar (Serilog, günlük, 7 gün) | `/var/lib/tahakonsept/logs/api-YYYYMMDD.log`, `web-YYYYMMDD.log` + journald |
| Gizli ortam | `/var/lib/tahakonsept/tahakonsept.env` (0600, sahibi `tahakonsept`): `TAHAKONSEPT_DB_PASSWORD=...` |
| Servisler | `tahakonsept-api.service` (0.0.0.0:7111, LAN — masaüstü ajanı bağlanır), `tahakonsept-web.service` (0.0.0.0:7110) |
| Veritabanı | PostgreSQL 17, `localhost:5432`, DB/rol `tahakonsept` |

Unit'ler `ASPNETCORE_ENVIRONMENT=Pi` ile çalışır → `appsettings.Pi.json`:
- **Api:** `ConnectionStrings:Main = Host=localhost;Port=5432;Database=tahakonsept;Username=tahakonsept` (parola env dosyasından, `DatabaseConnectionString` ekler), `App:DataDirectory=/var/lib/tahakonsept/App_Data`, `Logging:Directory=/var/lib/tahakonsept/logs`.
- **Web:** `Api:BaseUrl=http://127.0.0.1:7111`, `DataProtection:KeysDirectory=/var/lib/tahakonsept/App_Data/keys/web-dataprotection`, aynı log klasörü.
- App_Data çözümü BuildingBlocks'ta zaten yapılandırılabilir: `App:DataDirectory` → `TAHAKONSEPT_DATA` env → `<contentRoot>/App_Data` (`AppDataLocation.Resolve`). Unit'ler ayrıca `TAHAKONSEPT_DATA` ve `TAHAKONSEPT_LOG_DIR` verir (yedek). Ayrı bir `Paths:AppData` anahtarına gerek yok.
- `HOME=/var/lib/tahakonsept` → ASP.NET varsayılan Data Protection anahtarları (`~/.aspnet`) da kalıcı veri klasörüne düşer.

Sertleştirme (`tmautoclip-canli-api.service` deseni): `NoNewPrivileges`, boş `CapabilityBoundingSet`, `ProtectSystem=strict` + `ReadWritePaths=/var/lib/tahakonsept`, `ProtectHome`, `PrivateTmp/Devices`, çekirdek/cgroup/saat korumaları, `RestrictAddressFamilies=AF_INET AF_INET6 AF_UNIX AF_NETLINK`, `UMask=0077`. `MemoryDenyWriteExecute` ve `SystemCallFilter` **yok** (.NET JIT). `systemd-analyze security tahakonsept-api` → 2.9 OK.

## 2. Yayınlama (bu bilgisayardan)

```powershell
# Çalışma klasöründen (yarım kalmış değişiklikler de girer):
powershell -ExecutionPolicy Bypass -File deploy\publish-pi.ps1
# Son commit'ten temiz kopya (paralel çalışma sürerken önerilen):
powershell -ExecutionPolicy Bypass -File deploy\publish-pi.ps1 -Source Head
# Son yayın çıktısını yeniden paketle/kur (derlemeden):
powershell -ExecutionPolicy Bypass -File deploy\publish-pi.ps1 -SkipPublish
```
Ajan indirmeleri (`wwwroot/downloads/*`: Setup.exe, .nupkg, releases/assets json — gitignore'da) her zaman **çalışma klasöründen** web çıktısına kopyalanır (`-Source Head` dahil); doğrulamada Setup.exe de kontrol edilir.

Parametreler: `-SshHost tmpi`, `-PiAddress 192.168.1.102`, `-SkipVerify`.

Adımlar: `VERSION` okunur → `dotnet publish -c Release -r linux-arm64 --self-contained` (Api + Web; ayrı `--artifacts-path deploy/.out/build`, geliştirme `bin/obj`'una dokunmaz) → `deploy/.out/last/{api,web,pi}` → `tahakonsept-<sürüm>.tar.gz` (~93 MB) → `scp tmpi:/tmp` → `sudo bash install-release.sh <sürüm> <klasör>`:
1. `releases/<sürüm>` oluşturur, unit dosyalarını değiştiyse kurar + `daemon-reload`.
2. Veri klasörlerini garanti eder, env dosyasının izinlerini düzeltir (yoksa uyarır).
3. `current`'ı yeni sürüme çevirir, eskisini `previous` yapar; servisleri `enable` + `restart` eder.
4. `http://127.0.0.1:7111/api/health` ve `http://127.0.0.1:7110/` için 90 sn (`HEALTH_TIMEOUT`) sağlık kontrolü. API'de yalnız HTTP 200 yetmez: `status=ok`, `database=ready` ve `version=<yeni sürüm>` beklenir (`degraded` = veritabanı hazır değil → başarı sayılmaz, süre içinde beklenir). **Başarısızsa otomatik önceki sürüme döner** ve hata verir.
5. Son 5 sürümü tutar (`KEEP_RELEASES`), `current`/`previous` asla silinmez.

Sonra betik bu bilgisayardan `http://192.168.1.102:7111/api/health` (aynı koşullar: ok + ready + sürüm, değilse hata) ve `http://192.168.1.102:7110/` doğrular.

## 3. Geri dönüş (rollback)

```bash
ssh tmpi sudo /opt/tahakonsept/bin/rollback.sh --list                   # sürümler (current/previous işaretli)
ssh tmpi sudo /opt/tahakonsept/bin/rollback.sh                          # previous'a dön
ssh tmpi sudo /opt/tahakonsept/bin/rollback.sh 0.1.0-20261114-054614    # belirli sürüme dön
```
Not: EF migration'ları geri alınmaz. Şema değiştiren bir sürümden dönülecekse önce yeni şemanın eski kodla uyumlu olduğundan emin olun (ileri uyumlu migration yazın; gerekiyorsa `dotnet ef database update <önceki-migration>` tünel üzerinden).

## 4. Loglar / durum

```bash
ssh tmpi journalctl -u tahakonsept-api -f            # canlı API logu
ssh tmpi journalctl -u tahakonsept-web -n 100 --no-pager
ssh tmpi systemctl status tahakonsept-api tahakonsept-web
ssh tmpi sudo tail -f /var/lib/tahakonsept/logs/api-$(date +%Y%m%d).log
curl http://192.168.1.102:7111/api/health
```

## 5. Gizli ortam dosyası

Bir kez yazıldı (14 Kas 2026). Yeniden yazmak için (parola `TAHA/.env` içinde):
```bash
pw=$(grep '^TAHAKONSEPT_DB_PASSWORD=' .env | cut -d= -f2- | tr -d '\r')
printf 'TAHAKONSEPT_DB_PASSWORD=%s\n' "$pw" | ssh tmpi 'sudo install -m 0600 -o tahakonsept -g tahakonsept /dev/stdin /var/lib/tahakonsept/tahakonsept.env'
ssh tmpi sudo systemctl restart tahakonsept-api
```
İleride: `TAHAKONSEPT_MASTER_KEY` (base64, AES-GCM) da buraya eklenebilir; yoksa `App_Data/keys/master.key` ilk kullanımda üretilir — **bu dosya kaybolursa şifreli API key'ler okunamaz, yedekleyin.**

## 6. Geliştirme betikleri

- `scripts/dev-tunnel.ps1` — `ssh -N -L 5433:localhost:5432 tmpi` (ön planda; `-Background` ile arka planda). Port açıksa yeni tünel açmaz.
- `scripts/dev-run.ps1` — tüneli açar (`-NoTunnel` ile atlanır), Api (7111) ve Web (7110) host'larını ayrı pencerelerde `dotnet run` (Development) ile başlatır; `-Wait` ile bu pencerede bekler, Ctrl+C ikisini de kapatır.

## 7. Yayın kaydı

| Tarih | Sürüm | Kaynak | Sonuç |
|---|---|---|---|
| 2026-11-14 05:46 | `0.1.0-20261114-054614` | HEAD `e392960` (F0 host'ları; modüller henüz yok) | Kuruldu. `api/health` → `{"status":"ok","version":"0.1.0","environment":"Pi","database":"ready"}`; Web `/` → 302 `/Account/Login` (200 sonrası). |
| 2026-11-14 05:47 | `0.1.0-20261114-054659` | aynı çıktı (`-SkipPublish`) | Kuruldu (sürüm dönüşü testi). `rollback.sh` → 054614 ve geri 054659 denendi, ikisi de sağlıklı. **Etkin: 054659.** |
| 2026-11-14 07:58 | `0.2.0-20261114-075813` | HEAD `f6b047c` (dalga 2: Concepts + Production modülleri, yeni sayfalar; ajan indirmeleri çalışma klasöründen eklendi) | Kuruldu. `api/health` → `ok`, `0.2.0`, `database: ready`; Web `/` → 302 `/Account/Login`; `/downloads/TahaKonsept.Agent-win-Setup.exe` → 200. **Etkin: 075813.** |
| 2026-11-14 10:00 | `0.2.1-20261114-095859` | HEAD `43e4d3d` (E2E + 24 inceleme düzeltmesi; 2 yeni migration: Identity `RefreshTokenConcurrency`, Agents `DevicePairingConcurrency`; ajan kurulumu 0.2.1 olarak yeniden paketlendi; `install-release.sh` artık `degraded` sağlığı başarı saymıyor) | Kuruldu. `api/health` → `ok`, `0.2.1`, `database: ready`; Web `/` → 302 `/Account/Login`; `/downloads/TahaKonsept.Agent-win-Setup.exe` → 200 (88 003 397 bayt, yereldekiyle aynı). Gerçek DB'deki `diagnostics.error_reports` duman testi satırı (`Smoke test: kurulum hatasi…`) silindi. **Etkin: 095859.** |
| 2026-11-14 11:04 | `0.2.2-20261114-110248` | HEAD `ff33601` (cila: ses modeline göre anlatım hızı + `production.speech_rate_stats` migration'ı, web araması olmayan modelde kaynak uydurmama / `reviewNotes`, düşük VRAM görsel kipi, Avalonia pencere düzeltmeleri; ajan 0.2.2 olarak yeniden paketlendi, delta 0.2.1 → 0.2.2; eski 0.1.0 paketi `releases.win.json`/`RELEASES`'tan ve `wwwroot/downloads`'tan çıkarıldı — beslemede yalnız 0.2.2 + 0.2.1) | Kuruldu. `api/health` → `ok`, `0.2.2`, `database: ready` (migration açılışta uygulandı); Web `/` → 302 `/Account/Login`; `/downloads/TahaKonsept.Agent-win-Setup.exe` → 200 (88 011 219 bayt, yereldekiyle aynı); `/downloads/releases.win.json` → 0.2.2 Full/Delta + 0.2.1 Full/Delta. **Etkin: 110248** (previous: 0.2.1-20261114-095859). |
| 2026-11-14 15:46 | `0.2.3-20261114-154527` | HEAD `2bcbc10` (kurgu30 yöntem testlerinden 16 düzeltme: HyperFrames görselli lint, Compose limited range BT.709, Revideo yazı tipi ön yükleme, ajan DisposeAsync, uv/araç kurulumu (IToolProvisioner) + dosya kilidi yeniden denemesi, Piper ayna, MMS Türkçe, Chatterbox/XTTS bağımlılıkları, F5 Türkçe referans, GPU ses VRAM serbest bırakma, dil denetimi kurulumdan önce, IImageFitter, SDXL piksel bütçesi; katalog VRAM değerleri; ajan 0.2.3 olarak yeniden paketlendi, delta 0.2.2 → 0.2.3; 0.2.1 paketleri beslemeden çıkarıldı — beslemede yalnız 0.2.3 + 0.2.2) | Kuruldu. `api/health` → `ok`, `0.2.3`, `database: ready`; Web `/` → 302 `/Account/Login`; `/downloads/TahaKonsept.Agent-win-Setup.exe` → 200 (88 267 523 bayt, yereldekiyle aynı); `/downloads/releases.win.json` → 0.2.3 Full/Delta + 0.2.2 Full/Delta. En eski `0.1.0-20261114-054614` KEEP_RELEASES ile silindi. **Etkin: 154527** (previous: 0.2.2-20261114-110248). |
| 2026-11-14 16:57 | `0.2.4-20261114-165659` | HEAD `2eb5ddf` (önce GPU gerçek RTX 3060'ta doğrulandı — kurgu30 GPU turu, 5 düzeltme: Revideo Chrome `--enable-gpu` + `--disable-accelerated-video-encode`, CUDA torch yerel sürüm sabitleme, Supertonic kart adı logu, EMA düz CUDA, Supertonic DirectML oturumu serbest bırakma; ajan 0.2.4 olarak yeniden paketlendi — `DirectML.dll` pakette, delta 0.2.3 → 0.2.4; 0.2.2 paketleri beslemeden ve `artifacts/agent/releases`'tan çıkarıldı — beslemede yalnız 0.2.4 + 0.2.3; paket 349,6 MB, indirmeler dahil) | Kuruldu. `api/health` → `ok`, `0.2.4`, `database: ready`; Web `/` → 302 `/Account/Login`; `/downloads/TahaKonsept.Agent-win-Setup.exe` → 200 (91 104 066 bayt, yereldekiyle aynı); `/downloads/releases.win.json` → 0.2.4 Full/Delta + 0.2.3 Full/Delta. En eski `0.1.0-20261114-054659` KEEP_RELEASES ile silindi. **Etkin: 165659** (previous: 0.2.3-20261114-154527). |

Bilinen: çalışma klasöründen yayın denemesi o an başka bir ajanın yarım `Shared.Contracts` değişikliği (`ImagePromptItem` yok) yüzünden derlenmedi → `-Source Head` eklendi. Web'in çağırdığı `api/identity/setup/status` Identity modülü gelene kadar 404 döner (beklenen).

Not (14 Kas 2026): `publish-pi.ps1` PowerShell'den çalıştırılmalı. Git Bash içinden `powershell -File …` ile çağrılınca PATH'te önce `/usr/bin/tar` (GNU tar) bulunur, `C:\…` yolunu uzak makine sanır ("Cannot connect to C: resolve failed"); Windows `tar` (bsdtar) ile sorun yok.
