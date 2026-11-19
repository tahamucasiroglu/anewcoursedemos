# Spec 0004 — Giriş ve oturum (kurum + ilk şube + kurucu)

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0004-plan.md`
- Self-critique: done

## Intent
Okulun kurucusu ve ileride eklenecek öğretmen, veli ve muhasebe kullanıcıları kendi hesaplarıyla web'e ve mobil
uygulamaya giriş yapmak; girişten sonra yalnızca kendi okulunun (kurum) ve yetkili oldukları şubelerin bilgisini görmek
istiyor. Okul satışı birebir yapıldığı için ilk okul, ilk şubesi ve kurucu hesabı herkese açık bir sayfadan değil,
proje sahibinin çalıştırdığı bir komutla açılır. Bir kişi (öğretmen, veli) tek bir kimliktir: aynı e-posta birden çok
okulda yer alabilir; girişten sonra hangi okulla çalışacağını seçer ve yalnızca o okulun verisini görür. Başarı:
komutla iki ayrı okul açılır; her kurucu kendi okuluna girer, kendi okulunu ve şubelerini görür, öbür okulun hiçbir
bilgisini — denese bile — göremez; yanlış şifre denemeleri kilitlenir ve her giriş olayı değiştirilemez kayda geçer.
Bu spec kullanıcı/şube yönetim ekranlarını (spec 0005), iki adımlı doğrulamayı (MVP sonrası), şifre sıfırlamayı,
e-posta gönderimini ve öğrenci/veli-çocuk bağını yapmaz.

## Requirements
1. **Okul açma komutu:** Proje sahibi yerelde ve Pi'de aynı komutla okul açar (Pi için `tools/pi` altında sarmalayıcı — Ö-6): okul adı, ilk şubenin adı, kurucunun
   ad-soyadı ve e-postası verilir; şifre komut tarafından **gizli girişle** sorulur (komut satırında, ekranda, logda
   görünmez). Komut okulu, ilk şubesini ve kurucuyu kurucu rolüyle oluşturur ve sonucu (okul, şube, kurucu e-postası)
   raporlar.
2. **Tek kimlik, birden çok okul:** Bir e-posta sistemde tek bir kişidir. Komut, sistemde zaten olan bir e-postayla
   başka bir okul açarsa yeni kişi açılmaz; mevcut kişiye o okulda kurucu rolü verilir ve şifresi **değiştirilmez**
   (komut şifre sormaz). Aynı okulda aynı kişiye ikinci kez kurucu rolü verilmez (komut "zaten var" der, hiçbir şey
   değiştirmez).
3. **Giriş:** E-posta + şifre ile web'de ve mobilde giriş. E-posta büyük/küçük harf ve baştaki/sondaki boşluk
   farkı gözetmeden eşleşir.
4. **Okul seçimi:** Kişinin tek okulu varsa doğrudan o okulla devam edilir; birden çok okulu varsa girişten sonra
   okul listesi gösterilir ve seçilen okulla devam edilir. Giriş sonrası ekranda "okul değiştir" ile başka okula
   geçilebilir (yeniden şifre sorulmaz). Kişi o anda yalnızca seçili okulun verisini görür.
5. **Hoş geldin ekranı (web + mobil):** Kişinin adı, seçili okul, o okuldaki rolleri ve görebildiği şubeler; "çıkış"
   ve (birden çok okulu varsa) "okul değiştir". Kurucu okulun tüm şubelerini görür; diğer roller yalnızca atandıkları
   şubeleri görür (bu spec'te diğer rollere atama yalnızca test verisiyle yapılabilir; ekranı spec 0005'te).
6. **Hatalı giriş:** Yanlış şifre ve sistemde olmayan e-posta **aynı** mesajı verir ("E-posta veya şifre hatalı");
   hangisinin yanlış olduğu belli edilmez.
7. **Kilit:** Aynı e-posta için art arda **3** hatalı denemede o e-postayla giriş **15 dakika** kilitlenir; kilit
   süresince doğru şifre de reddedilir; başarılı giriş sayacı sıfırlar. Sistemde olmayan e-posta için de aynı sayaç ve
   aynı kilit mesajı işler (kimin kayıtlı olduğu anlaşılmaz — Ö-3). Aynı IP'den dakikada 20'den fazla giriş denemesi
   "Çok fazla istek, biraz sonra deneyin" alır (Ö-4).
8. **Şifre kuralı:** Şifre en az **8** karakter; başka kural yok (çok bilinen şifre listesi pilot öncesine — Ö-5). Bu
   spec'te şifre yalnızca okul açma komutunda belirlenir ve kişinin şifresi olarak kalır: ilk girişte değiştirme zorunlu
   değildir (kullanıcı kararı, Ö-1; spec 0005'te eklenen kullanıcılar için de geçerli).
9. **Oturum:** Oturum kullanıcı fark etmeden yenilenir; **7 gün** hiç kullanılmazsa yeniden giriş istenir, kullanıldıkça
   uzar (Ö-2). "Çıkış" oturumu hemen geçersiz kılar (çıkıştan sonra eski oturum bilgisiyle hiçbir istek kabul edilmez).
10. **Varsayılan: reddet.** Giriş, oturum yenileme ve sistem durumu dışındaki her API isteği giriş ister; giriş
    yoksa reddedilir.
11. **Okul duvarı:** Hiçbir istek seçili okulun dışındaki bir okulun ya da şubenin bilgisini döndürmez; kişi kendi
    okulu içinde yetkili olmadığı şubenin bilgisini de göremez. Böyle bir kayıt istenirse "bulunamadı" denir.
12. **Şifre saklama:** Şifre yalnızca tek yönlü özet (hash) olarak saklanır; veritabanında, logda, hata mesajında ve
    hiçbir yanıtta düz hâli yoktur.
13. **Audit kaydı:** Başarılı giriş, hatalı giriş, kilitlenme, çıkış, okul değiştirme ve okul açma komutunun her
    işlemi (okul, şube, kurucu, rol) kayda geçer: kim (yoksa denenen e-posta), ne, hangi kayıt, ne zaman, hangi IP.
    Kayıtlar uygulama tarafından değiştirilemez ve silinemez. Kayıtta şifre yoktur. (Görüntüleme ekranı sonra.)
14. **Mobil:** Mobil uygulama da aynı girişi, okul seçimini, hoş geldin ekranını ve çıkışı sunar; uygulama
    kapatılıp açılınca oturum (süresi dolmadıysa) devam eder.

## User experience
- **Flow (web ve mobil):** Uygulama açılır → oturum yoksa **Giriş** ekranı (e-posta, şifre, "Giriş yap") → başarılı:
  tek okul → **Hoş geldin**; birden çok okul → **Okul seç** → **Hoş geldin**. Hoş geldin'de "Okul değiştir" →
  Okul seç; "Çıkış" → Giriş. Oturum varken uygulama açılırsa doğrudan Hoş geldin. Mevcut sistem durumu bilgisi ve
  tema seçici korunur.
- **States:**
  - Giriş: boş form; alan hataları (e-posta boş / geçersiz biçim, şifre boş); gönderiliyor (düğme kilitli, yükleniyor);
    "E-posta veya şifre hatalı"; kilitli ("Çok fazla hatalı deneme. 15 dakika sonra tekrar deneyin."); sunucuya
    ulaşılamıyor ("Sunucuya ulaşılamıyor…" + tekrar dene).
  - Okul seç: liste (okul adları); seçim yükleniyor; hata.
  - Hoş geldin: yükleniyor; dolu (ad, okul, roller, şubeler); kişinin seçili okulda görebildiği şube yoksa "Bu okulda
    size atanmış şube yok."; oturum süresi doldu → Giriş ekranına dönülür ve "Oturumunuz sona erdi, tekrar giriş yapın."
- **Design reference:** none — the flow above is the design; spec 0002 teması (açık/koyu) ve mevcut düzen kullanılır.
- **Accessibility:** web'de tüm akış yalnızca klavyeyle (Tab, Enter); alanların görünür etiketleri; hata mesajları
  ekran okuyucuya duyurulur ve ilgili alanla ilişkilidir; şifre alanı gizli; iki temada da kontrast spec 0002
  eşiklerinde.

## Constraints & out of scope
- Sensitive area: S1 (kurum ve şube izolasyonu), S3 (kimlik, şifre, oturum, rol ataması), S2 (ad-soyad, e-posta), S6
  (audit kaydı).
- **İki adımlı doğrulama (2FA) bu spec'te ve MVP demosunda yok** — kullanıcı kararı 2026-11-19: "mvp sonrası 2fa
  ekleriz, önce mvp olarak okullara gösterip fikir alalım". `docs/security.md` kuralı ("kurucu ve muhasebe için zorunlu")
  bu kararla güncellenir: gerçek okul verisiyle pilota açılmadan önce zorunlu; backlog'a ve yol haritasına yazılır.
- `docs/security.md` 2FA kuralına "MVP demosuna kadar ertelendi; gerçek okul verisiyle pilot öncesi zorunlu" notu; backlog'a
  2FA + bilinen şifre listesi maddesi; yol haritasına "pilot öncesi" satırı (Ö-7).
- Yalnızca e-posta + şifre; telefonla/SMS ile giriş, Google/Microsoft ile giriş kapsam dışı.
- Şifre sıfırlama / "şifremi unuttum", şifre değiştirme ekranı, e-posta gönderimi (SMTP altyapısı) → spec 0005.
- Kullanıcı ekleme, rol/şube verme, şube ekleme/düzenleme ekranları → spec 0005.
- Öğrenci, veli-çocuk bağı, KVKK aydınlatma/rıza ekranı → sonraki spec'ler. Not (kullanıcı): "çocuklar ve
  öğretmenler eşsiz olmalı" — öğrenci de okullar arası tek kimlik olacak; öğrenci spec'inde ele alınır.
- Audit kayıtlarını görüntüleme ekranı kapsam dışı (yalnızca yazılır).
- Kurucu şifresi repoya, spec'e, plana, Promtlar'a yazılmaz; komutta gizli girişle verilir.
- **Split:** proposed 0004 "Giriş ve oturum" (bu spec) → 0005 "Şube ve kullanıcı yönetimi" (kurucu şube ekler,
  kullanıcı ekler, rol/şube verir, geçici şifre, şifre sıfırlama, SMTP'ye hazır e-posta). Kullanıcının istediği
  "Giriş ve roller + kurum/şube" iki yetenek (giriş; yönetim) ve ~25 kriter olurdu.

## Acceptance criteria
- [ ] AC-1 — Okul açma komutu okul adı, ilk şube adı, kurucu ad-soyadı ve e-postasıyla çalışır; şifreyi gizli girişle sorar; okul, şube ve kurucu (kurucu rolüyle) oluşur ve sonuç raporlanır; şifre komut satırında, komut çıktısında ve loglarda geçmez. 8 karakterden kısa şifre veya geçersiz e-posta reddedilir ve hiçbir şey oluşturulmaz.
- [ ] AC-2 — Sistemde olan bir e-postayla ikinci bir okul açılınca yeni kişi oluşmaz, kişiye o okulda kurucu rolü eklenir ve şifresi değişmez; aynı okul + aynı kişiyle tekrar çalıştırılınca "zaten var" der ve hiçbir şey değişmez.
- [ ] AC-3 — Web'de ve mobilde doğru e-posta + şifreyle giriş: tek okullu kişi doğrudan Hoş geldin ekranını görür (ad, okul, roller, şubeler); e-posta büyük/küçük harf ve baştaki/sondaki boşluktan bağımsız eşleşir.
- [ ] AC-4 — İki okullu kişi girişten sonra Okul seç ekranını görür; seçtiği okulun Hoş geldin bilgisini görür; "Okul değiştir" ile şifre sorulmadan öbür okula geçer; ekran hep yalnızca seçili okulun bilgisini gösterir.
- [ ] AC-5 — Yanlış şifre ve sistemde olmayan e-posta aynı "E-posta veya şifre hatalı" mesajını ve aynı yanıt biçimini verir; boş/geçersiz e-posta ve boş şifre için alan hatası gösterilir, istek gönderilmez.
- [ ] AC-6 — Aynı e-postayla art arda 3 hatalı denemeden sonra 15 dakika boyunca doğru şifre de reddedilir ve kilit mesajı gösterilir; 15 dakika sonra doğru şifre kabul edilir; 2 hatalı + 1 başarılı giriş sayacı sıfırlar (sonraki 2 hata kilitlemez); sistemde olmayan e-posta 3 denemede aynı kilit mesajını alır; aynı IP'den dakikada 20'yi aşan deneme "Çok fazla istek" alır.
- [ ] AC-7 — Giriş, oturum yenileme ve sistem durumu dışındaki her API isteği girişsiz reddedilir; süresi dolmuş, bozuk veya başka bir sistemin imzasıyla üretilmiş oturum bilgisi reddedilir.
- [ ] AC-8 — Oturum kullanıcı fark etmeden yenilenir (kısa ömürlü erişim süresi dolduktan sonra ekran çalışmaya devam eder); 7 gün kullanılmayan oturum reddedilir ve Giriş istenir; "Çıkış"tan sonra eski oturum bilgileriyle (erişim ve yenileme) yapılan her istek reddedilir ve Giriş ekranı gelir.
- [ ] AC-9 — Okul duvarı: A okulunun kurucusu B okulunun ya da B'nin şubesinin bilgisini isteyince (kimliği bilerek bile) "bulunamadı" alır; listeleri yalnızca A'nın kayıtlarını içerir; iki okullu kişi A seçiliyken B'nin bilgisini göremez.
- [ ] AC-10 — Şube görünürlüğü: kurucu okulunun tüm şubelerini görür; kurucu olmayan ve yalnızca bir şubeye atanmış kişi (test verisiyle) yalnızca o şubeyi görür, öbür şube istenirse "bulunamadı" alır; şubesi olmayan kişi "Bu okulda size atanmış şube yok." görür.
- [ ] AC-11 — Şifre veritabanında yalnızca tek yönlü özet olarak durur; giriş ve komut sırasında üretilen loglarda, hata yanıtlarında ve audit kayıtlarında şifre ya da oturum anahtarı geçmez.
- [ ] AC-12 — Başarılı giriş, hatalı giriş, kilitlenme, çıkış, okul değiştirme ve okul açma komutu işlemleri audit kaydı oluşturur (kim / denenen e-posta, ne, hangi kayıt, ne zaman, IP); uygulama audit kaydını değiştiremez ve silemez (deneme hata verir).
- [ ] AC-13 — Ekran durumları web ve mobilde: gönderiliyor (düğme kilitli), sunucuya ulaşılamıyor + tekrar dene, oturum süresi doldu mesajı; uygulama yeniden açılınca geçerli oturum Hoş geldin'e, oturum yoksa Giriş'e gider; web'de akış yalnızca klavyeyle tamamlanır, hata mesajları ekran okuyucuya duyurulur; açık ve koyu temada çalışır.

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
