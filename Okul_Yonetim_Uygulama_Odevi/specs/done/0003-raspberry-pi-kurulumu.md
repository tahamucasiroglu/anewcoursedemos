# Spec 0003 — Raspberry Pi kurulumu (MVP deneme ortamı)

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0003-plan.md`
- Self-critique: done

## Intent
Proje sahibi uygulamayı kendi bilgisayarı dışında, yerel ağ üzerinden erişilen gerçeğe yakın bir ortamda denemek
istiyor: evdeki Raspberry Pi 5 (8 GB, Debian 13 64-bit, ağ adresi 192.168.1.102). Başarı: Pi açıldığında API ve web
paneli kendiliğinden çalışır; aynı ağdaki tarayıcıdan `http://192.168.1.102:8080` açılınca web paneli, telefondaki
mobil uygulama da aynı adres üzerinden API'yi görür; yeni sürüm tek komutla gönderilir, sorun çıkarsa önceki sürüme
dönülür. Web ve API ayrı birimlerdir (web nginx ile, API ayrı servis olarak). Uygulama Pi'de zaten çalışan
PostgreSQL 17'ye yalnızca kendi veritabanına yetkili, yönetici yetkisi olmayan ayrı bir kullanıcıyla bağlanır; şifre
repoya, sohbete ve loglara girmez. Pi'deki diğer uygulamalar etkilenmez. Bilinçli olarak yapılmayanlar: internete
açma / HTTPS / alan adı, otomatik yedekleme, birden çok Pi, gerçek okul verisi, mobil uygulamanın mağaza derlemesi.

## Requirements
1. **Kurulum (ilk kez):** Geliştirme makinesinden tek komutla Pi hazırlanır: nginx kurulur, uygulamanın sistem
   kullanıcısı, klasörleri, servisi, web sitesi ayarı, veritabanı ve veritabanı kullanıcısı oluşturulur.
2. **Web ayrı birim:** React web paneli derlenmiş dosyalar olarak nginx ile `8080` portundan sunulur; sayfa
   yenilemede ve doğrudan adreste uygulama açılır. `/api` ile başlayan istekleri nginx Pi içindeki API'ye iletir.
3. **API ayrı birim:** API Pi'de servis olarak çalışır; yalnızca Pi'nin içinden dinler (dışarıdan doğrudan erişilmez);
   root olmayan, oturum açamayan ayrı bir sistem kullanıcısıyla çalışır; Pi açılışında ve çökme sonrasında
   kendiliğinden başlar. Pi'ye ayrıca .NET kurulmaz (uygulama kendi çalışma ortamıyla gelir).
4. **Kısıtlı veritabanı kullanıcısı:** Uygulama yalnızca kendi veritabanına bağlanabilen, süper kullanıcı olmayan,
   veritabanı/rol oluşturamayan bir kullanıcıyla bağlanır; kendi veritabanında şema geçişlerini (migration) uygulayabilir.
5. **Gizli bilgi:** Veritabanı şifresi Pi'de rastgele üretilir; yalnızca root'un okuyabildiği bir dosyada durur ve
   servis oradan okur. Şifre repoya, sohbete, komut çıktılarına ve loglara yazılmaz.
6. **Dağıtım:** Tek komut: web ve API derlenir, Pi'ye kopyalanır, yeni sürüme geçilir, sistem durumu kontrol edilir;
   sonuç ve sürüm bilgisi raporlanır. Pi'de son 2 sürüm tutulur. Web ve API ayrı ayrı da dağıtılabilir; yalnızca
   biri güncellenince diğeri yeniden başlatılmaz/değişmez.
7. **Güvenli geçiş ve geri alma:** Dağıtımdan sonra sistem durumu "çalışıyor" değilse komut hata verir ve otomatik
   olarak önceki sürüme dönülür. Ayrıca elle "önceki sürüme dön" komutu vardır.
8. **Loglar:** API logları Pi'de sabit (mutlak) bir klasöre (`/var/log/okul`) günlük dosyalar olarak yazılır, 30 günden
   eskileri silinir (ADR-0003; backlog B-9 kapanır).
9. **Mobil:** Mobil uygulamanın API adresi ortam ayarıyla Pi'ye (`http://192.168.1.102:8080`) çevrilebilir; bunun
   nasıl yapılacağı belgelenir.
10. **Kaldırma:** Kullanıcı açıkça istediğinde ve komut açık onayla çalıştırıldığında bizim servis, uygulama
    dosyaları, loglar, nginx site ayarı, veritabanı ve veritabanı kullanıcısı silinir; nginx, PostgreSQL ve Pi'deki
    diğer uygulamalar, veritabanları ve portlar etkilenmez.
11. **Tekrar çalıştırılabilirlik:** Kurulum ve dağıtım ikinci kez çalıştırılınca hata vermez; mevcut veriyi ve
    şifreyi bozmaz.
12. **Belge:** Kurulum, dağıtım, geri alma, kaldırma ve mobil bağlantı adımları Türkçe tek bir belgede.
13. Pi'deki mevcut uygulamalar ve portları (5090, 7100–7105) kurulumdan ve dağıtımdan sonra aynı çalışır.
14. **Tek adres ayarı (Ö-1):** Pi'nin ağ adresi, SSH tanımı ve portlar tek bir ayar dosyasından gelir; web paneli
    sayfayla aynı adresteki API'yi kullandığı için Pi'nin IP'si değişince yeniden derlenmesi gerekmez. Belge, modemde
    Pi'ye sabit IP verilmesini önerir.
15. **Kilit (Ö-3):** Bir dağıtım sürerken ikinci bir dağıtım/geri alma başlamayı reddeder.
16. **Disk (Ö-5):** Kurulum ve dağıtım, Pi'de 1 GB'tan az boş alan varsa durur ve uyarır.

## User experience
Kullanıcı arayüzü değişmez (web ve mobil ekranları spec 0001/0002'deki gibi). İşletim akışı (komutlar):
- **Flow:** Geliştirme makinesinde kurulum komutu (bir kez) → dağıtım komutu → tarayıcıda
  `http://192.168.1.102:8080` → "Sistem çalışıyor". Sorun olursa: dağıtım kendisi geri döner veya "önceki sürüme dön"
  komutu. İş bitince (yalnızca kullanıcı isterse): kaldırma komutu, onayla.
- **States (komut sonuçları):** başarılı (sürüm + "çalışıyor"); dağıtım başarısız → otomatik geri dönüldü (hangi
  adımda neden); Pi'ye ulaşılamıyor (SSH) → açık hata; önkoşul eksik (ör. PostgreSQL çalışmıyor) → hangi önkoşul.
- **Design reference:** none — arayüz yok; komut çıktıları Türkçe ve kısa.
- **Accessibility:** none — no user interface (komut çıktıları düz metin).

## Constraints & out of scope
- Sensitive area: S3 (kimlik/gizli bilgi: veritabanı şifresi ve kısıtlı veritabanı kullanıcısı).
- Pi'deki PostgreSQL **17** kullanılır (geliştirme 18); 18'e özgü özellik kullanılmaz (kullanıcı kararı, 2026-11-19).
- Yalnızca yerel ağ, düz HTTP; internete açma, HTTPS, alan adı kapsam dışı (backlog'a yazılır).
- Otomatik yedekleme kapsam dışı (Pi'deki veri deneme verisidir); birden çok Pi, mağaza derlemesi kapsam dışı.
- Geri alma yalnızca uygulama sürümünü değiştirir; veritabanı şeması ileri yönlüdür (geri alınmaz). Geri dönülen sürümden
  sonra eklenmiş migration varsa komut uyarı verir (Ö-2).
- Dağıtım sırasında API yeniden başlarken birkaç saniyelik kesinti kabul edilir (deneme ortamı, Ö-4).
- Kaldırma yalnızca kullanıcı "kaldır" dediğinde çalıştırılır (kullanıcı kararı).
- Pi'ye değiştiren her uzak komuttan önce kullanıcı onayı alınır (agent davranışı).
- Size: tek yetenek (deneme ortamına kurulum/dağıtım), 12 kabul kriteri — kriterlerin çoğu aynı kurulumun güvenlik
  ve geri dönüş sınırları; bölünmeden tek spec olarak öneriliyor.

## Acceptance criteria
- [x] AC-1 — Kurulum + dağıtımdan sonra aynı ağdaki bir tarayıcıda `http://192.168.1.102:8080` web panelini açar ve gerçek API + Pi'deki PostgreSQL 17 üzerinden "Sistem çalışıyor" görünür; sayfa yenilenince de açılır.
- [x] AC-2 — API yalnızca Pi'nin içinden dinler: ağdan API portuna doğrudan bağlantı kurulamaz; `/api` istekleri yalnızca `8080` üzerinden (nginx) çalışır.
- [x] AC-3 — API root olmayan, oturum açamayan ayrı bir sistem kullanıcısıyla çalışır; API süreci öldürüldüğünde kendiliğinden yeniden çalışır; servis açılışta başlayacak şekilde etkindir (systemd `enabled`). Pi yeniden başlatma testi yapılmaz — kullanıcı kararı 2026-11-19: Pi'de başka işler çalışıyor, yeniden başlatma onları aksatır.
- [x] AC-4 — Uygulamanın veritabanı kullanıcısı kendi veritabanında migration'ları uygular; başka bir veritabanına bağlanamaz; süper kullanıcı değildir; veritabanı veya rol oluşturamaz.
- [x] AC-5 — Veritabanı şifresi yalnızca root'un okuyabildiği bir dosyadadır; repoda, kurulum/dağıtım çıktılarında ve API loglarında geçmez.
- [x] AC-6 — Dağıtım tek komuttur; sonunda sistem durumu "çalışıyor" doğrulanır ve sürüm bildirilir; Pi'de en fazla son 2 sürüm kalır; başka bir dağıtım sürerken ikincisi reddedilir; 1 GB'tan az boş alanda durur.
- [x] AC-7 — Dağıtım sonrası kontrol başarısızsa komut hata verir ve önceki sürüm otomatik olarak geri gelir (web açılır, "çalışıyor"); elle "önceki sürüme dön" komutu da aynı sonucu verir; geri dönülen sürümden sonra migration eklenmişse uyarı verilir.
- [x] AC-8 — Yalnızca web dağıtılınca API yeniden başlatılmaz; yalnızca API dağıtılınca web dosyaları değişmez.
- [x] AC-9 — API logları `/var/log/okul` altında günlük dosyalara yazılır; 30 günden eskiler silinir.
- [x] AC-10 — Mobil uygulama API adresi `http://192.168.1.102:8080` yapılınca Pi'deki API'den "Sistem çalışıyor" gösterir.
- [x] AC-11 — Kaldırma komutu açık onay olmadan hiçbir şey silmez; onayla bizim servisi, dosyaları, logları, nginx site ayarını, veritabanını ve kullanıcısını siler; nginx, PostgreSQL ve diğer uygulamalar/veritabanları çalışmaya devam eder.
- [x] AC-12 — Kurulum ve dağıtım ikinci kez çalıştırılınca hata vermez, veriyi ve şifreyi değiştirmez; Pi'deki mevcut portlar (5090, 7100–7105) ve diğer uygulamalar önce ve sonra aynıdır.

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 1 (VERIFY sırasında AC-3'ten Pi yeniden başlatma gözlemi çıkarıldı — kullanıcı kararı, plan A-5) |
| Fix rounds | 4 (fix-1..fix-3: review round 1–3; fix-4: R-06 sonrası kullanıcı onaylı, VERIFY V-1) + bir belge düzeltmesi (review round 5) |
| Review findings: real / noise / deferred | 22 / 7 / 1 (round 1: 11/2/1, round 2: 6/0/0, round 3: 3/0/0, round 4: 0/4/0, round 5: 2/1/0); VERIFY: 1 (V-1 web API adresi) |
| Regressions introduced | 3 (fix-1'in pi-config bölme kuralı yeni açık → round 2 M-1; fix-2'nin `_` etkisizleştirmesi → round 3 L-1; fix-4 ilk denemesi MSYS_NO_PATHCONV ile derlemeyi yanlış klasöre yazdırdı → dağıtım Pi'ye göndermeden durdu) |
| Bugs escaped to production | 0 (üretim yok; V-1 deneme ortamına çıkmıştı, VERIFY'da tarayıcı kanıtıyla yakalandı) |

## Verification evidence
VERIFY 2026-11-19, QA (lite, ana oturum), gerçek Raspberry Pi 5 (192.168.1.102). Tam yeşil: `CHECK GREEN (full, 12 steps, 72 s) @2c19e68` (sonrası yalnızca belge). Çıktılar ve betikler: `Promtlar/Sources/0003/cikti`, `Promtlar/Sources/0003/kaynak`. Kasıtlı bozmalar repo dışında yapıldı, hiçbiri commit edilmedi. Şifre hiçbir çıktıya basılmadı (aramalar yalnızca eşleşme sayısı verdi).

| Acceptance criterion | Evidence | Status |
|---|---|---|
| AC-1 | Gerçek zamanlı başsız Chrome: `/` ve `/okul/derin/baglanti` → sayfanın isteği `http://192.168.1.102:8080/api/system-health` 200, ekranda "✔ Sistem çalışıyor" (V-1-pi-ana-sayfa.png, V-1-pi-derin-baglanti.png); kullanıcı da `/herhangi/bir/adres` ile denedi. İlk gözlem başarısızdı (V-1, `…-HATA-file-adresi.png`) → fix-4 | PASS |
| AC-2 | Ağdan `:8080` → 200, `:5080` → bağlantı reddedildi (curl rc=7); `ss`: 5080 yalnızca 127.0.0.1; pi-config | PASS |
| AC-3 | `kill -9` → 5 sn'de yeni PID, NRestarts=1, journal "Scheduled restart"; User=okul (nologin); UnitFileState=enabled, WantedBy=multi-user.target; pi-config. Pi yeniden başlatma: kriterden çıkarıldı (A-5) | PASS |
| AC-4 | okul_app → postgres/tmautoclip_canli/template1: "pg_hba.conf rejects connection"; SET ROLE okul_app: CREATE DATABASE/ROLE "permission denied"; rol özellikleri hepsi f; sahip okul_app; migration 20261119170000_Initial. Kasıtlı bozma (paylaşılan pg_hba'ya dokunmadan): blok olmasa okul_app 134. kurala (host all all 127.0.0.1 scram) düşer ve postgres'e CONNECT yetkisi var (t) → engel bizim reject satırı | PASS |
| AC-5 | /etc/okul/okul-api.env root:root 600; şifre araması 7 kaynakta 0 (yerel çıktılar, /var/log/okul, /var/log/postgresql, /var/log/nginx, journal okul-api/postgresql/tümü), olumlu kontrol 1; PG log ayarları none/-1; pi-config öz-sınama (kasıtlı bozmalar kırmızı) | PASS |
| AC-6 | deploy çıktıları (sürüm + "çalışıyor"); web dağıtımı sürerken ikinci dağıtım "Başka bir dağıtım/geri alma sürüyor" rc=1; eşik 999999999 MB → "yeterli boş alan yok", kilit alınmadı; birim başına 2 sürüm | PASS |
| AC-7 | Repo dışı bozuk API (açılışta istisna + sahte migration adı) → "UYARI: … migration", "önceki sürüm geri getirildi ve çalışıyor", rc=1, kilit/hazırlık klasörü kalmadı; rollback.sh → "geri dönüldü; çalışıyor"; ardından güncel sürüm yeniden dağıtıldı | PASS |
| AC-8 | deploy web: API PID 415702 → 415702; deploy api: web current aynı | PASS |
| AC-9 | /var/log/okul/okul-api-20261119.log (okul:okul); 30 gün: `LoggingSetupTests.cs::FileSink_Retains30Days_DailyRolling` | PASS |
| AC-10 | Mobilin kod yolu (mobile/src/config.ts + @okul/api-client fetchSystemHealth) Node'dan → `{"kind":"healthy","message":"Sistem çalışıyor"}` (A-2); telefonda Expo Go isteğe bağlı | PASS |
| AC-11 | Onaysız: stdin boş, `--onay hayir`, Pi tarafı argümansız ve küçük harf → 4'ünde "hiçbir şey silinmedi", rc=1, site çalışmaya devam etti; pi-config. **Onaylı kaldırma Pi'de çalıştırılmadı** (kullanıcı kuralı: yalnızca "kaldır" denince) — o yol review'da (pg_hba blok doğrulaması, mktemp yedek) ve statik denetimde doğrulandı | PASS (onaylı yol gözlenmedi) |
| AC-12 | setup.sh (tümü) ikinci kez rc=0 ("zaten var, dokunulmadı"); önce/sonra: env özeti f00ee4076f97a59c aynı, portlar/servisler/veritabanları aynı, pg_hba okul dışı özeti aynı; deploy birçok kez tekrarlandı | PASS |

