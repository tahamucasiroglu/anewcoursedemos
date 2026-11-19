# Spec 0003 — Raspberry Pi kurulumu (MVP deneme ortamı)

- Status: In progress
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
- [ ] AC-1 — Kurulum + dağıtımdan sonra aynı ağdaki bir tarayıcıda `http://192.168.1.102:8080` web panelini açar ve gerçek API + Pi'deki PostgreSQL 17 üzerinden "Sistem çalışıyor" görünür; sayfa yenilenince de açılır.
- [ ] AC-2 — API yalnızca Pi'nin içinden dinler: ağdan API portuna doğrudan bağlantı kurulamaz; `/api` istekleri yalnızca `8080` üzerinden (nginx) çalışır.
- [ ] AC-3 — API root olmayan, oturum açamayan ayrı bir sistem kullanıcısıyla çalışır; API süreci öldürüldüğünde kendiliğinden yeniden çalışır; servis açılışta başlayacak şekilde etkindir (systemd `enabled`). Pi yeniden başlatma testi yapılmaz — kullanıcı kararı 2026-11-19: Pi'de başka işler çalışıyor, yeniden başlatma onları aksatır.
- [ ] AC-4 — Uygulamanın veritabanı kullanıcısı kendi veritabanında migration'ları uygular; başka bir veritabanına bağlanamaz; süper kullanıcı değildir; veritabanı veya rol oluşturamaz.
- [ ] AC-5 — Veritabanı şifresi yalnızca root'un okuyabildiği bir dosyadadır; repoda, kurulum/dağıtım çıktılarında ve API loglarında geçmez.
- [ ] AC-6 — Dağıtım tek komuttur; sonunda sistem durumu "çalışıyor" doğrulanır ve sürüm bildirilir; Pi'de en fazla son 2 sürüm kalır; başka bir dağıtım sürerken ikincisi reddedilir; 1 GB'tan az boş alanda durur.
- [ ] AC-7 — Dağıtım sonrası kontrol başarısızsa komut hata verir ve önceki sürüm otomatik olarak geri gelir (web açılır, "çalışıyor"); elle "önceki sürüme dön" komutu da aynı sonucu verir; geri dönülen sürümden sonra migration eklenmişse uyarı verilir.
- [ ] AC-8 — Yalnızca web dağıtılınca API yeniden başlatılmaz; yalnızca API dağıtılınca web dosyaları değişmez.
- [ ] AC-9 — API logları `/var/log/okul` altında günlük dosyalara yazılır; 30 günden eskiler silinir.
- [ ] AC-10 — Mobil uygulama API adresi `http://192.168.1.102:8080` yapılınca Pi'deki API'den "Sistem çalışıyor" gösterir.
- [ ] AC-11 — Kaldırma komutu açık onay olmadan hiçbir şey silmez; onayla bizim servisi, dosyaları, logları, nginx site ayarını, veritabanını ve kullanıcısını siler; nginx, PostgreSQL ve diğer uygulamalar/veritabanları çalışmaya devam eder.
- [ ] AC-12 — Kurulum ve dağıtım ikinci kez çalıştırılınca hata vermez, veriyi ve şifreyi değiştirmez; Pi'deki mevcut portlar (5090, 7100–7105) ve diğer uygulamalar önce ve sonra aynıdır.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
