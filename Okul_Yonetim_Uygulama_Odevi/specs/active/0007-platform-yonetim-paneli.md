# Spec 0007 — Platform yönetim paneli

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0007-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu, 2026-11-20 — önceden verilen onay (Promtlar 0077: clarify cevapları = analistin
  önerileri, spec ve plan onayı önceden verildi; triage analist/geliştiricide, bilgi dışındaki bulgular düzeltilir; ship
  kullanıcıda)

## Intent
Proje sahibi (platformu satan kişi) yeni bir okulu bugün yalnızca komut satırından açabiliyor; okulları göremiyor ve
ödemeyen ya da ayrılan bir okulu kapatamıyor. Bu spec ona web'de bir **platform yönetim paneli** verir: okulları
listeler, yeni okul açar (okul + ilk şube + kurucu), okulu pasifleştirir ve yeniden etkinleştirir. Bütün okulları gören
tek kişi olduğu için hesabı **iki adımlı doğrulamayla** (telefondaki doğrulayıcı uygulamanın 6 haneli kodu) korunur ve
okulların içindeki veriye (kişiler, öğrenciler, şube ayrıntıları) erişemez. Başarı: proje sahibi Pi'deki web
adresinden, şifre + kodla girip okul açar ve yeni kurucuya geçici şifreyi iletir. Bilinçli olarak yapılmayanlar:
okulun içine girip veri görme ("okul adına giriş"), okul silme, faturalama/abonelik, platform yöneticisini web'den
atama, diğer roller için iki adımlı doğrulama (B-17).

Clarify kararları (önceden verilen onayla analistin önerileri — Promtlar 0079): C-1 platform yöneticisi yetkisi kişiye
bağlıdır, yalnızca komutla verilir/alınır · C-2 iki adımlı doğrulama zorunlu, ilk girişte kurulur, platform
yöneticisinin her girişinde sorulur · C-3 kod kaybında kurtarma komutla · C-4 okul ekleme komutla aynı kurallar,
yeni kurucuya geçici şifre bir kez · C-5 listede kurucu ad-soyad ve e-postası görünür, başka kişisel veri yok ·
C-6 pasif okulda kimse giremez, veriler silinmez · C-7 yalnızca web · C-8 okul komutu (create-school) kalır.

## Requirements
1. **Platform yöneticisi yetkisi:** Okul rollerinden ayrı, kişiye bağlı bir yetkidir. Proje sahibi komutla (yerelde ve
   Pi'de aynı komut; Pi için `tools/pi` sarmalayıcısı) var olan bir hesaba verir ya da alır; web'den verilemez. Yetki
   verilince ya da alınınca kişinin açık oturumları kapanır. Bir kişi hem platform yöneticisi hem okul kurucusu olabilir.
2. **İki adımlı doğrulama — kurulum:** Platform yöneticisi şifresiyle girdikten sonra doğrulaması kurulu değilse "İki
   adımlı doğrulamayı kur" ekranı açılır: doğrulayıcı uygulamaya (Google Authenticator, Microsoft Authenticator vb.)
   eklenecek kare kod ve elle yazılabilir anahtar; kişi uygulamadaki 6 haneli kodu girerek kurulumu tamamlar. Kurulum
   bitmeden platform paneli açılmaz. Anahtar yalnızca kurulum ekranında bir kez gösterilir; sistemde şifrelenmiş
   saklanır.
3. **İki adımlı doğrulama — giriş:** Platform yöneticisi her girişte şifreden sonra 6 haneli kodu girer; doğru kodla
   devam eder. Yanlış kod "Kod hatalı" verir; art arda 3 yanlış kod (şifre hatalarıyla aynı sayaç) o hesabı 15 dakika
   kilitler. Aynı kod (aynı 30 saniyelik dilim) ikinci kez kabul edilmez. Saat kayması için bir önceki ve bir sonraki
   dilim kabul edilir. Kod girilmeden alınan oturum platform paneline erişemez. Kişinin okul ekranları (kurucu vb.)
   kod adımı tamamlanınca açılır (aynı giriş).
4. **Kurtarma:** Telefonunu kaybeden platform yöneticisinin doğrulaması komutla sıfırlanır; sonraki girişte yeniden
   kurulum ekranı açılır; açık oturumları kapanır.
5. **Okul listesi:** Okullar ad sırasıyla listelenir: okul adı, açılış tarihi, durum (etkin/pasif), etkin şube sayısı,
   etkin kişi sayısı, kurucu(lar)ın ad-soyad ve e-postası. Ada göre arama, duruma göre süzme, sayfa başına 25 okul.
   Boşken "Henüz okul yok".
6. **Okul ekle:** Okul adı (en fazla 100), ilk şube adı (en fazla 100), kurucu ad-soyad (en fazla 100) ve e-posta
   zorunlu. Kurallar okul açma komutuyla aynı (spec 0004): sistemde olmayan e-postada yeni kişi açılır ve **geçici şifre**
   (spec 0005 kuralı: 12 karakter, harf+rakam, karışan karakterler yok) sonuç ekranında bir kez gösterilir; var olan
   e-postada yeni kişi açılmaz, kişiye bu okulda kurucu rolü verilir, şifre değişmez ve "Bu kişinin zaten bir hesabı var;
   mevcut şifresiyle girer." gösterilir. Aynı adlı ve aynı kurucunun okulu zaten varsa "Bu okul zaten var" ile reddedilir.
7. **Okulu pasifleştir / etkinleştir:** Onaydan sonra okul pasif olur: o okulda hiç kimse (kurucu dahil) girişte o okulu
   göremez, seçemez; açık oturumlar o okul için bir sonraki istekte reddedilir; okulun verisi silinmez. Etkinleştirince
   her şey eski hâline döner. Pasif okul listede "pasif" etiketiyle durur.
8. **Okulların içi kapalı:** Platform yöneticisi okulların kişi, şube ayrıntısı ve diğer verilerine erişemez; panel
   yalnızca madde 5'teki özet bilgiyi gösterir. Okul duvarı (spec 0004/0005) değişmez.
9. **Gizlilik:** Doğrulama anahtarı, 6 haneli kodlar, geçici şifre ve şifreler hiçbir logda, hata yanıtında ve audit
   kaydında geçmez; anahtar veritabanında şifrelenmiş durur; şifreleme anahtarı repoda değil, ortam ayarındadır
   (Pi'de oturum imza anahtarı gibi ayrı, yalnızca root'un okuyabildiği dosyada — yalnızca betik hazırlanır, Pi'ye
   bu spec'te dokunulmaz).
10. **Audit kaydı:** Platform yöneticisi verme/alma, doğrulama kurulumu, doğru/yanlış kod, doğrulama sıfırlama, okul
    açma, okul pasifleştirme/etkinleştirme audit'e yazılır (kim, ne, hangi okul, ne zaman, IP / komutta makine).

## User experience
- **Flow (yalnızca web):**
  - Giriş (e-posta + şifre) → [doğrulama kurulu değilse] Kurulum (kare kod + anahtar → kod) → [kuruluysa] Kod →
    Hoş geldin. Platform yöneticisinin Hoş geldin'inde "Platform paneli" bağlantısı → `#/platform`.
  - Platform paneli: okul listesi (arama, durum süzgeci, sayfalama) → "Okul ekle" formu → sonuç (geçici şifre bir kez
    ya da "zaten hesabı var") → listeye dön; satırda "Pasifleştir" / "Etkinleştir" (onaylı).
- **States:** liste yükleniyor / boş / dolu / hata + tekrar dene; form alan hataları (istek gönderilmez) / gönderiliyor
  (düğme kilitli) / sunucu hatası ("Bu okul zaten var" vb.); geçici şifre sonucu (spec 0005 kalıbı: uyarı, kopyala,
  kopyalandı); onay iletişimi; kurulum ekranı (kare kod, anahtar, kod alanı, kod hatalı); kod ekranı (kod hatalı,
  kilitli); yetkisiz kişi `#/platform`'da "Bu sayfaya erişiminiz yok.".
- **Design reference:** none — the flow above is the design (spec 0005 yönetim sayfası kalıbı, ortak tema).
- **Accessibility:** akış yalnızca klavyeyle; kod alanı `autocomplete="one-time-code"`, sayısal klavye; kare kodun yanında
  elle yazılabilir anahtar (görme engelli/kamera yok); hatalar alanla ilişkili ve duyurulur; iki temada kontrast spec
  0002 eşiklerinde; 320 px'te yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S1 (okullar arası izolasyon — bütün okulları gören tek rol), S2 (kurucu ad-soyad, e-posta), S3
  (yetki, iki adımlı doğrulama anahtarı, şifre, oturum), S6 (audit kaydı).
- Okul adına giriş / okul verisini görme yok. Okul silme yok (pasifleştirme var). Faturalama/abonelik yok.
- Platform yöneticisi web'den atanmaz; diğer roller için iki adımlı doğrulama yok (B-17, pilot öncesi).
- Mobilde platform paneli yok.
- Okul açma komutu (`create-school`) ilk kurulum ve acil durum için kalır.
- Pi'ye bu spec'te dağıtım yapılmaz; Pi için gerekli şifreleme anahtarı dosyasını hazırlayan betik değişikliği repoda
  hazırlanır, çalıştırma kullanıcıda (ship).
- Split: proposed 0007a "Platform yöneticisi + iki adımlı doğrulama" ve 0007b "Okul listesi, ekleme, pasifleştirme";
  kept as one — tahamucasiroglu, 2026-11-20 (Promtlar 0076/0077: yol haritası 1c tek spec, kullanıcı uzakta).

## Acceptance criteria
- [ ] AC-1 — Yetki: platform paneli işlemleri yalnızca platform yöneticisi yetkisi olan ve bu girişte iki adımlı kodu doğrulamış oturuma açıktır; okul kurucusu, öğretmen, veli, muhasebe ve kodu henüz doğrulanmamış oturum 403 alır ve hiçbir şey değişmez; girişsiz istek 401 alır; yetkisi komutla alınan kişinin sonraki isteği reddedilir.
- [ ] AC-2 — Yetki komutu: var olan hesaba yetki verir ve alır (yerelde; Pi sarmalayıcısı aynı komutu çağırır); olmayan e-postada "Bu e-postayla bir hesap yok" der ve hiçbir şey değişmez; verme/alma kişinin açık oturumlarını kapatır; işlem audit'e komut satırı kaynağıyla yazılır.
- [ ] AC-3 — Kurulum: doğrulaması kurulu olmayan platform yöneticisi şifreyle girince kurulum adımına yönlenir (kare kod + elle anahtar); doğrulayıcı uygulamanın ürettiği doğru kodla kurulum tamamlanır ve panel açılır; yanlış kodda kurulum tamamlanmaz; kurulum tamamlanmadan panel 403'tür; anahtar kurulumdan sonra hiçbir yanıtta tekrar alınamaz.
- [ ] AC-4 — Kodla giriş: kurulu platform yöneticisi şifreden sonra kod ister; doğru kod (önceki/sonraki 30 sn dilimi dahil) kabul edilir; yanlış kod "Kod hatalı" verir; art arda 3 yanlış (şifre hatalarıyla aynı sayaç) hesabı 15 dk kilitler, kilitliyken doğru kod da reddedilir; aynı kod ikinci kez kabul edilmez; platform yöneticisi olmayan kişinin girişi değişmez (kod sorulmaz).
- [ ] AC-5 — Kurtarma komutu: platform yöneticisinin doğrulamasını sıfırlar, açık oturumlarını kapatır; sonraki girişte kurulum adımı açılır; işlem audit'e yazılır.
- [ ] AC-6 — Okul listesi: okullar ad sırasıyla ad, açılış tarihi, durum, etkin şube sayısı, etkin kişi sayısı ve kurucu(lar)ın ad-soyad + e-postasıyla listelenir; ada göre arama (Türkçe harf kuralıyla) ve durum süzgeci çalışır; 25'ten fazla okulda sayfalama; boşken "Henüz okul yok"; yanıtta başka kişisel veri (telefon, diğer kişiler) yoktur.
- [ ] AC-7 — Okul ekle (yeni kurucu): geçerli bilgilerle okul + ilk şube + kurucu oluşur; geçici şifre (12 karakter, harf+rakam, 0/O/1/l/I yok) bir kez gösterilir ve sonra hiçbir yanıtta alınamaz; kurucu bu şifreyle girip okulunu kurucu olarak görür.
- [ ] AC-8 — Okul ekle (var olan kişi ve hatalar): var olan e-postada yeni kişi açılmaz, kurucu rolü eklenir, şifre değişmez, "Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer." gösterilir; aynı ad + aynı kurucu "Bu okul zaten var" ile reddedilir; boş/uzun alan ve geçersiz e-posta alan hatası verir, istek gönderilmez ve hiçbir şey oluşmaz.
- [ ] AC-9 — Pasifleştir/etkinleştir: onaydan sonra okul pasif olur; o okulun kişileri (kurucu dahil) girişte okulu görmez, seçemez, açık oturumları o okul için bir sonraki istekte reddedilir (yenileme dahil); başka okulları etkilenmez; veri silinmez; etkinleştirince kişiler eski rolleriyle girer.
- [ ] AC-10 — Okulların içi kapalı: platform yöneticisi (kurucu rolü olmayan okulda) o okulun şube, kişi ve yönetim uçlarına erişemez (403/404); panel yanıtları madde 5'teki özetten fazlasını içermez.
- [ ] AC-11 — Gizlilik: doğrulama anahtarı veritabanında şifreli durur (düz anahtar veritabanında bulunmaz); anahtar, kodlar, geçici şifre ve şifreler log, hata yanıtı ve audit kayıtlarında geçmez; şifreleme anahtarı repoda yoktur; ayarlanmamışsa uygulama platform yöneticisi girişini güvenli biçimde reddeder ("iki adımlı doğrulama yapılandırılmamış" logu), diğer girişler çalışır.
- [ ] AC-12 — Audit: yetki verme/alma, kurulum, doğru ve yanlış kod, kilit, sıfırlama, okul açma, pasifleştirme ve etkinleştirme audit kaydı oluşturur (kim, ne, hangi okul, ne zaman, IP / komut kaynağı).
- [ ] AC-13 — Ekran durumları (web, iki tema): giriş kod ve kurulum adımları (kare kod + anahtar, kod hatalı, kilitli), panel listesi (yükleniyor / boş / dolu / hata + tekrar dene), okul ekle formu (alan hataları, gönderiliyor, sunucu hatası), geçici şifre sonucu, onay iletişimi, yetkisiz kişide "Bu sayfaya erişiminiz yok."; "Platform paneli" bağlantısı yalnızca platform yöneticisinde; akış yalnızca klavyeyle; hatalar duyurulur; 320 px'te yatay kayma yok.

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
