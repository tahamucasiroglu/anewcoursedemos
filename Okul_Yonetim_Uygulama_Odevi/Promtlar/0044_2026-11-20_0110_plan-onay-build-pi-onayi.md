# 0044 — Plan 0003 onayı · BUILD (yerel kısım) · Pi'ye dokunmadan önce onay

**Tarih:** 2026-11-20 01:10

## İstek

> onaylıyorum

## Cevap

### Son mesajdan beri yapılanlar (Pi'ye henüz hiçbir şey yazılmadı)
1. **Plan onaylandı** (`1a3e87e`); spec durumu "In progress".
2. **Adım 1 — web aynı adres** (`c5e8eac`): API adresi `/` verilince web, sayfanın kendi adresini kullanıyor. Pi'nin IP'si değişse bile web yeniden derlenmez.
3. **Adım 2–5 — Pi betikleri** (`29793b6`, `tools/pi/`):
   - `setup.sh`: kurulum. Adım adım çalışabilir: nginx, sistem, veritabanı, servis.
   - `deploy.sh`: dağıtım. Kilit, disk eşiği, sağlık kontrolü, başarısızsa otomatik geri dönüş, son 2 sürüm.
   - `rollback.sh`: elle geri alma, migration uyarısıyla.
   - `status.sh`: salt okuma durum raporu.
   - `uninstall.sh`: `KALDIR` yazılmadan çalışmaz.
   - Servis: root olmayan `okul` kullanıcısı, yalnızca `127.0.0.1:5080`, sertleştirme ayarları, çökünce yeniden başlar.
   - nginx: yalnızca 8080; `/api` içeride API'ye iletilir.
   - Veritabanı: `okul_app` yönetici değil; erişim ayarında (`pg_hba`) yalnızca `okul` veritabanına izinli. Şifre Pi'de üretilir, yalnızca root'un okuyabildiği dosyaya yazılır, hiçbir yerde ekrana basılmaz.
   - **Yeni kontrol adımı `pi-config`:** Pi'ye bağlanmadan betikleri ve ayarları denetler. İlk çalıştırmada benim kodumda şifrenin yazıldığı bir satırı da yakaladı; incelendi, boru üzerinden veritabanına gittiği görüldü ve kural netleştirildi.
4. **Belgeler** (`d296df5`):
   - Türkçe Pi kılavuzu `docs/deploy-pi.md`;
   - güvenlik ve mimari güncellendi;
   - tuzak T12 eklendi: "Pi'de PostgreSQL 17";
   - ADR-0004'ün açık soruları cevaplandı;
   - backlog: B-9 (log yolu) kapandı; B-13 (HTTPS), B-14 (yedek), B-15 (iki veritabanı rolü) eklendi.
5. **Yerel doğrulama:**
   - Pi için API derlemesi bu makinede çalıştı: 125 MB, Türkçe metin dosyaları dahil.
   - `CHECK GREEN (fast, 9 steps, 68 s)`.
6. **Pi'nin bugünkü hâli kaydedildi** (salt okuma). Kurulumdan sonra karşılaştırmak için:
   - açık portlar;
   - çalışan servisler (tmautoclip, muskitakip-web…);
   - veritabanları;
   - erişim ayar dosyasının parmak izi.

### Şimdi: Pi'ye dokunacak ilk adımlar
Plan gereği her değiştiren adımdan önce onayını istiyorum. Sırayla şunlar yapılacak:

| # | Adım | Pi'de ne değişir |
|---|---|---|
| 1 | `setup.sh nginx` | `apt install nginx`. nginx'in 80 portundaki varsayılan sitesi kapatılır. Bizim site 8080'de açılır. |
| 2 | `setup.sh system` | `okul` sistem kullanıcısı (oturum açamaz); `/opt/okul`, `/var/log/okul`, `/etc/okul` klasörleri |
| 3 | `setup.sh database` | PostgreSQL 17'de `okul_app` kullanıcısı ve `okul` veritabanı. `pg_hba.conf`'un en üstüne işaretli blok eklenir (önce yedek alınır, diğer projelerin satırlarına dokunulmaz). Şifre dosyası oluşturulur. |
| 4 | `setup.sh service` | `okul-api` systemd servisi (açılışta başlar) |
| 5 | `deploy.sh` | İlk sürüm: derle, kopyala, etkinleştir, "çalışıyor" kontrolü |

### Karar
Bu 5 adımı sırayla çalıştırayım mı?
- **Önerim:** Hepsine tek onay ver. Her adımın çıktısını göstererek ilerlerim; bir adım hata verirse orada durur, sana sorarım.
- İstersen adım adım da onaylayabilirsin.
