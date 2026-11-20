# Spec 0005 — Şube ve kullanıcı yönetimi

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0005-plan.md`
- Self-critique: done

## Intent
Okulun kurucusu, okulunun şubelerini ve kişilerini web panelinden kendisi yönetmek istiyor: şube ekler, öğretmen,
veli, muhasebe ve gerekirse ikinci bir yönetici hesabı açar; kime hangi rolü ve hangi şubeyi vereceğine karar verir,
gerekince rolü geri alır ya da kişiyi okulunda pasifleştirir; şifresini unutan kişiye yeni geçici şifre verir. Böylece
okul açma komutu yalnızca okulun ilk açılışında kullanılır; diğer herkes kurucunun eklemesiyle sisteme girer. Başarı:
kurucu bir öğretmen ekler, ekranda bir kez gösterilen geçici şifreyi ona iletir, öğretmen o şifreyle girer ve yalnızca
atandığı şubeyi görür; kurucu şifresini sıfırladığında eski şifre ve açık oturumları geçersiz olur. Bu spec "şifremi
değiştir", "şifremi unuttum" ve e-posta gönderimini (spec 0006), veli-çocuk bağını, sınıf ve dönemi, iki adımlı
doğrulamayı ve platform panelini yapmaz.

## Requirements
1. **Yönetim yetkisi:** Şube ve kullanıcı yönetim ekranları yalnızca **web'de** ve yalnızca seçili okulda **kurucu/
   yönetici** rolü olan kişiye açıktır. Diğer roller (öğretmen, veli, muhasebe) bu ekranları görmez; bu işlemleri
   doğrudan denerlerse reddedilir. Her işlem yalnızca seçili okulun kayıtlarına uygulanır; başka okulun kaydı
   "bulunamadı" olur (okul duvarı, spec 0004).
2. **Şubeler:** Kurucu şube ekler (ad zorunlu, en fazla 100 karakter; adres ve telefon isteğe bağlı), şube bilgisini
   düzenler, şubeyi pasifleştirir ve yeniden etkinleştirir. Şube silinmez. Aynı okulda aynı adla (harf farkı gözetmeden)
   iki etkin şube olamaz. Okulun son etkin şubesi pasifleştirilemez. Pasif şube yeni atamada seçilemez; o şubeye atanmış
   kişiler pasif şubeyi görmez (şube listesinden düşer).
3. **Kullanıcı ekleme:** Kurucu kişi ekler: ad-soyad (en fazla 100 karakter) ve e-posta zorunlu, telefon isteğe bağlı
   (Türkiye numarası: "0 5xx xxx xx xx", "5xx…" veya "+90 5xx…" kabul edilir ve tek biçimde saklanır; geçersizse alan
   hatası — Ö-3); en az bir rol seçilir.
   Öğretmen, veli ve muhasebe rolü bir ya da birden çok etkin **şubeye** verilir; kurucu/yönetici rolü **okul
   düzeyindedir** (şube seçilmez). E-posta büyük/küçük harf ve boşluk farkı gözetmeden eşleşir.
4. **Geçici şifre:** Sistemde olmayan bir e-posta eklenince sistem rastgele, okunabilir bir **geçici şifre** üretir
   (12 karakter, harf ve rakam karışık; birbirine benzeyen 0/O ve 1/l/I kullanılmaz — Ö-1) ve kurucuya **yalnızca bir kez**, ekleme sonucunda gösterir; sonradan
   tekrar görüntülenemez. Kurucu bunu kişiye kendisi iletir. İlk girişte şifre değiştirme zorunlu değildir (kullanıcı
   kararı, spec 0004 Ö-1).
5. **Var olan kişi:** E-posta sistemde zaten kayıtlıysa (başka bir okulda) yeni kişi açılmaz, şifre üretilmez ve
   değiştirilmez; kişiye bu okulda seçilen roller eklenir ve kurucuya "Bu kişinin zaten bir hesabı var; mevcut
   şifresiyle girer." denir. Kurucu kişinin başka hangi okullarda olduğunu ya da başka okuldaki bilgilerini göremez.
   Kişi bu okulda zaten varsa ekleme reddedilir ("Bu kişi bu okulda zaten kayıtlı"). Kişinin "başka okulda kayıtlı"
   olduğunun bu yolla kurucuya görünmesi bilinçli olarak kabul edildi; hangi okul olduğu görünmez (Ö-4).
6. **Kullanıcı listesi ve kart:** Kurucu okulundaki kişileri listeler: ad-soyad, e-posta, roller ve şubeler, durum
   (etkin/pasif). Ad veya e-postayla arama; rol, şube ve duruma göre süzme; sayfa başına 25 kişi. Kişi kartında kurucu
   ad-soyad ve telefonu düzenleyebilir; e-posta değiştirilemez (kişinin kimliği; başka okullarda da kullanılıyor).
   Başka okullardaki rol ve bilgiler hiçbir ekranda görünmez.
7. **Rol ve şube verme/alma:** Kurucu kişiye rol ekler, rolün şubelerini değiştirir, rolü geri alır. Kişinin bu okulda
   hiç rolü kalmazsa pasif sayılır. Bir rol değişikliği kişinin açık oturumunda bir sonraki istekte geçerli olur.
8. **Pasifleştirme:** Kurucu bir kişiyi **bu okulda** pasifleştirir ve yeniden etkinleştirir. Pasif kişi bu okulu
   seçemez; bu okuldaki açık oturumları hemen geçersiz olur; kişinin başka okullardaki hesabı etkilenmez. Kalıcı silme
   yoktur. Hiçbir okulda etkin rolü kalmayan kişi giriş yapınca "Bu hesaba bağlı etkin okul yok." görür (Ö-2). Rolü
   alınan ya da pasifleştirilen kişi o anda ekrandaysa bir sonraki işleminde Okul seç ya da Giriş ekranına döner (Ö-7).
9. **Yönetici korumaları:** Kurucu kendi kurucu rolünü geri alamaz ve kendini pasifleştiremez; okulda en az bir etkin
   kurucu/yönetici kalmak zorundadır (son yöneticinin rolü alınamaz, pasifleştirilemez). İki yönetici aynı anda
   birbirinin rolünü alsa ya da birbirini pasifleştirse bile bu kural bozulamaz (Ö-5).
10. **Kurucu şifre sıfırlama:** Kurucu bir kişinin şifresini sıfırlar: yeni geçici şifre üretilir ve bir kez gösterilir;
    kişinin eski şifresi ve **tüm** açık oturumları geçersiz olur. Kişinin tek bir kimliği ve tek şifresi olduğu için
    sıfırlama onun bütün okullardaki girişini etkiler; bir okulun kurucusu başka okuldaki girişi bozamasın diye sıfırlama
    yalnızca kişinin **tek okulu bu okul ise** yapılabilir. Başka okulda da kayıtlı kişi için düğme yerine "Bu kişi başka
    bir okulda da kayıtlı; şifresini kendisi 'Şifremi unuttum' ile yenilemeli." yazar (spec 0006).
11. **Audit:** Şube ekleme/düzenleme/pasifleştirme, kullanıcı ekleme, bilgi düzenleme, rol verme/alma, pasifleştirme/
    etkinleştirme ve şifre sıfırlama audit kaydına geçer (kim, ne, hangi kayıt, ne zaman, IP). Geçici şifre audit'e,
    loga ve yanıt dışında hiçbir yere yazılmaz.

## User experience
- **Flow (web, kurucu):** Hoş geldin ekranında kurucuya "Yönetim" bağlantısı görünür (diğer rollere görünmez) →
  **Şubeler** ve **Kullanıcılar** sekmeleri.
  - Şubeler: liste → "Şube ekle" (form) → listede görünür; satırda "Düzenle", "Pasifleştir"/"Etkinleştir".
  - Kullanıcılar: liste (arama, süzgeç, sayfalama) → "Kullanıcı ekle" (ad-soyad, e-posta, telefon, roller ve
    şubeler) → **Sonuç**: yeni hesapsa geçici şifre bir kez gösterilir ("Kopyala" düğmesi, "Bu şifre bir daha
    gösterilmeyecek" uyarısı); var olan hesapsa "Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer." →
    listeye dönüş. Kişi kartı: bilgiler, roller ve şubeler (düzenle), "Pasifleştir"/"Etkinleştir", "Şifreyi sıfırla"
    (onay sorulur → yeni geçici şifre bir kez gösterilir).
- **States:**
  - Listeler: yükleniyor; boş ("Henüz şube yok" / "Aramaya uyan kişi yok"); dolu; hata + tekrar dene.
  - Formlar: alan hataları (zorunlu alan, e-posta biçimi, en az bir rol, şube gerektiren rolde şube); gönderiliyor
    (düğme kilitli); sunucu hatası (ör. "Bu adla bir şube zaten var", "Bu kişi bu okulda zaten kayıtlı").
  - Geçici şifre sonucu: gösterildi; kopyalandı bildirimi. Sayfa kapanır/yenilenirse şifre bir daha gösterilmez;
    kurucu gerekirse yeniden sıfırlar (Ö-6).
  - Korumalar: son yönetici / kendi rolü / son etkin şube için işlem düğmesi pasif ve nedeni yazılı.
  - Yetkisiz: kurucu olmayan kişi yönetim adresine giderse "Bu sayfaya erişiminiz yok."
  - Onay iletişimleri: pasifleştirme ve şifre sıfırlama için "Emin misiniz?" (Vazgeç / Onayla).
- **Design reference:** none — the flow above is the design; spec 0002 teması ve spec 0004 ekran düzeni kullanılır.
- **Accessibility:** tüm yönetim akışı yalnızca klavyeyle (Tab, Enter, Esc onay iletişimini kapatır); alanların görünür
  etiketleri; hatalar alanla ilişkili ve ekran okuyucuya duyurulur; geçici şifre seçilebilir metin olarak da görünür;
  açık ve koyu temada kontrast spec 0002 eşiklerinde; 320 px genişlikte yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S1 (kurum ve şube izolasyonu), S2 (ad-soyad, e-posta, telefon), S3 (rol ataması, şifre, oturum),
  S6 (audit kaydı).
- Yönetim ekranları yalnızca web; mobilde yönetim yok (kapsam belgesi: kurucu kanalı "Web").
- **Spec 0006'ya (bölme):** "Şifremi değiştir" (web + mobil, herkese), "Şifremi unuttum" (tek kullanımlık, 30 dk
  geçerli bağlantı; her durumda aynı mesaj), SMTP'ye hazır e-posta gönderimi (sunucu bilgileri boşken gönderilmez,
  yalnızca "gönderilemedi" logu; bağlantı loga yazılmaz).
- Veli-çocuk bağı, öğrenci kaydı, sınıf, dönem → sonraki spec'ler (yol haritası 2, 3).
- Kalıcı kullanıcı silme ve KVKK anonimleştirme → ileride (pilot öncesi).
- Audit görüntüleme ekranı yok (yalnızca yazılır). İki adımlı doğrulama yok (B-17). Platform paneli → yol haritası 1c.
- Excel'den toplu kullanıcı alma yok (sonra).
- Geçici şifre repoya, loga, audit'e, Promtlar'a yazılmaz.
- **Split:** proposed 0005 "Şube ve kullanıcı yönetimi" (bu spec: şube, kullanıcı, rol, pasifleştirme, geçici şifre,
  kurucu sıfırlaması) → 0006 "Şifre işlemleri ve e-posta" (şifremi değiştir, şifremi unuttum, SMTP'ye hazır e-posta).
  Kullanıcının istediği tek iş iki yetenek (kurucunun yönetimi; kişinin kendi şifresi) ve ~20 kriter olurdu.

## Acceptance criteria
- [ ] AC-1 — Yetki: kurucu olmayan kişi (öğretmen, veli, muhasebe) Hoş geldin'de "Yönetim"i görmez; yönetim adresine giderse "Bu sayfaya erişiminiz yok." görür; şube/kullanıcı işlemlerini doğrudan denerse reddedilir ve hiçbir şey değişmez. Girişsiz istek reddedilir.
- [ ] AC-2 — Okul duvarı: A okulunun kurucusu B okulunun şubesini veya kişisini (kimliğini bilerek bile) görüntüleyemez, düzenleyemez, pasifleştiremez, rol veremez, şifresini sıfırlayamaz — "bulunamadı" alır; A'nın listeleri yalnızca A'nın kayıtlarını içerir; iki okulda olan kişinin A'daki kartında B'deki rolleri ve bilgileri görünmez.
- [ ] AC-3 — Şube ekleme/düzenleme: ad zorunlu (boş → alan hatası, 100 karakterden uzun → hata), adres ve telefon isteğe bağlı; eklenen şube listede görünür ve atamada seçilebilir; aynı okulda aynı adla (harf farkı gözetmeden) ikinci etkin şube "Bu adla bir şube zaten var" ile reddedilir; başka okulda aynı ad serbesttir.
- [ ] AC-4 — Şube pasifleştirme: pasif şube atamada seçilemez, o şubeye atanmış kişinin şube listesinden düşer, kurucunun listesinde "pasif" olarak kalır ve yeniden etkinleştirilebilir; okulun son etkin şubesi pasifleştirilemez (düğme pasif, neden yazılı; doğrudan denenirse reddedilir).
- [ ] AC-5 — Yeni kişi ekleme: ad-soyad + e-posta + en az bir rol (şube gerektiren rolde en az bir etkin şube) ile kişi oluşur; geçici şifre (12 karakter, harf+rakam, 0/O/1/l/I içermez) sonuç ekranında bir kez gösterilir ve sonra hiçbir ekranda/yanıtta tekrar alınamaz; kişi bu şifreyle girer ve yalnızca atandığı şubeleri görür; ilk girişte şifre değiştirmesi istenmez. Eksik/geçersiz alanlarda (geçersiz telefon dahil) alan hatası gösterilir, istek gönderilmez ve hiçbir şey oluşmaz; geçerli telefon tek biçimde saklanır.
- [ ] AC-6 — Var olan kişi: başka okulda kayıtlı e-posta eklenince yeni kişi oluşmaz, şifre üretilmez ve değişmez, kişiye bu okulda roller eklenir ve "Bu kişinin zaten bir hesabı var; mevcut şifresiyle girer." gösterilir; kişi mevcut şifresiyle girip iki okulu görür. Aynı okulda zaten olan e-posta (harf/boşluk farkıyla bile) "Bu kişi bu okulda zaten kayıtlı" ile reddedilir.
- [ ] AC-7 — Liste: kişiler ad-soyad, e-posta, roller/şubeler ve durumla listelenir; ad veya e-posta araması, rol/şube/durum süzgeci çalışır; 25'ten fazla kişide sayfalama vardır; boş sonuçta "Aramaya uyan kişi yok"; kartta ad-soyad ve telefon düzenlenir, e-posta düzenlenemez.
- [ ] AC-8 — Rol verme/alma: kurucu rol ekler, şubelerini değiştirir, rolü geri alır; değişiklik kişinin açık oturumunda bir sonraki istekte geçerli olur (alınan şubenin verisi artık "bulunamadı"); rolü kalmayan kişi pasif görünür ve okulu seçemez; kurucu/yönetici rolü okul düzeyinde verilir ve verilen kişi tüm şubeleri görür.
- [ ] AC-9 — Pasifleştirme: pasif kişi bu okula giremez/okulu seçemez ve bu okuldaki açık oturumları hemen reddedilir (ekrandaysa bir sonraki işlemde Okul seç / Giriş'e döner); başka okuldaki girişi etkilenmez; hiçbir okulda etkin rolü kalmayan kişi girişte "Bu hesaba bağlı etkin okul yok." görür; yeniden etkinleştirilince eski rolleriyle girer.
- [ ] AC-10 — Yönetici korumaları: kurucu kendi kurucu rolünü alamaz, kendini pasifleştiremez; okulun son etkin yöneticisinin rolü alınamaz ve pasifleştirilemez (düğme pasif, neden yazılı; doğrudan denenirse reddedilir ve hiçbir şey değişmez); ikinci yönetici varken biri pasifleştirilebilir; iki yönetici aynı anda birbirinin rolünü alır ya da birbirini pasifleştirirse en az biri etkin yönetici olarak kalır.
- [ ] AC-11 — Kurucu şifre sıfırlama: yalnızca tek okulu bu okul olan kişi için, onaydan sonra yeni geçici şifre bir kez gösterilir; eski şifreyle giriş reddedilir; kişinin tüm açık oturumları hemen reddedilir; başka okulda da kayıtlı kişide sıfırlama yapılamaz ve açıklama mesajı gösterilir.
- [ ] AC-12 — Audit ve gizlilik: şube ekleme/düzenleme/pasifleştirme/etkinleştirme, kişi ekleme, bilgi düzenleme, rol verme/alma, pasifleştirme/etkinleştirme ve şifre sıfırlama audit kaydı oluşturur (kim, ne, hangi kayıt, ne zaman, IP); geçici şifre loglarda, audit kayıtlarında ve sonuç yanıtı dışındaki hiçbir yanıtta geçmez; veritabanında yalnızca tek yönlü özet durur.
- [ ] AC-13 — Ekran durumları (web, iki tema): listelerde yükleniyor / boş / dolu / hata + tekrar dene; formlarda alan hataları, gönderiliyor (düğme kilitli), sunucu hatası; geçici şifre sonucu ("bir daha gösterilmeyecek" uyarısı, kopyala + kopyalandı bildirimi); onay iletişimi (Vazgeç/Onayla, Esc kapatır); korumalarda pasif düğme + neden; akış yalnızca klavyeyle tamamlanır, hatalar ekran okuyucuya duyurulur; 320 px'te yatay kayma yok.

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
