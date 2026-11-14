# Spec 0002 — kimlik

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
ANEW Konsept'e kimlik geliyor: içerik üreticisi ad, e-posta ve şifreyle kayıt olur, e-posta ve şifreyle
giriş yapar, çıkış yapınca ajan dahil her yerden çıkmış olur; masaüstü ajanı şifre sormadan, ekranda
gösterdiği bir kodun web'den onaylanmasıyla kullanıcının hesabına bağlanır (ilk_promt.txt: "masaüstü
uygulaması da web'deki bir login'i takip edecek"). Şimdilik yalnızca geliştirici yerel ağda kullanıyor,
ama yapı çok kullanıcılı kuruluyor: her kullanıcı yalnızca kendi verisini ve kendi ajanını görür (BR-1'in
başlangıcı), yetkiler rollerle belirlenir (BR-13 temeli; varsayılan kullanıcı rolü ve admin rolü). Başarı:
giriş yapmadan siteye ve ajan bağlantısına erişilemiyor; iki ayrı kullanıcı birbirinin verisini ve ajanını
göremiyor; çıkış, ajanın bağlantısını anında düşürüyor. Bilinçli olarak yapılmayanlar: e-posta doğrulaması
(B-2), şifremi unuttum, HTTPS (B-3), "kalan hak" mesajının e-posta sızdırmasının giderilmesi (B-4), ban ve
etkisi ile admin panelinin kendisi (0010), model bazlı yetkilerin uygulanması (0004).

## Requirements
**Kayıt**
1. Giriş yapmamış biri kayıt sayfasında ad, e-posta ve şifre girerek doğrudan hesap açabilir; e-posta
   doğrulaması yapılmaz (B-2). Şifrenin iki kez sorulup sorulmayacağı → Open questions (a4).
2. Aynı e-postayla ikinci hesap açılamaz; e-posta karşılaştırmasında büyük/küçük harf farkı gözetilmez ve
   karşılaştırma Türkçe kültür ayarından etkilenmez (ör. `IREM@x.com` ile `irem@x.com` aynı hesaptır;
   `docs/conventions.md`, Türkçe I/İ tuzağı).
3. Ad, e-posta ve şifre geçerlilik kurallarına uymayan kayıt reddedilir ve hangi alanın neden geçersiz
   olduğu Türkçe gösterilir; girilen ad ve e-posta formda korunur, şifre korunmaz (kurallar → Open
   questions a4).
4. Yeni hesap varsayılan **kullanıcı** rolüyle açılır. Kayıt başarılı olunca kullanıcı giriş yapmış olarak
   Konseptler sayfasına gelir (öneri; brief'te yok → Open questions b1).

**Giriş**
5. Kayıtlı kullanıcı e-posta ve şifreyle giriş yapar; giriş başarılıysa Konseptler sayfasına gelir.
6. Yanlış şifrede ekranda kalan hak gösterilir: 1. hatada "Yanlış girdiniz, 2 hakkınız kaldı", 2. hatada
   "Yanlış girdiniz, 1 hakkınız kaldı". 3. hatada hesap geçici olarak kilitlenir ve kilidin ne zaman
   açılacağı gösterilir (kilit süresi → Open questions a1). Başarılı giriş hata sayacını sıfırlar.
7. Kilitli hesaba, şifre doğru olsa bile kilit süresi dolana kadar giriş yapılamaz; süre dolunca kullanıcı
   yeniden 3 hakla başlar.
8. Kayıtlı olmayan bir e-postayla giriş denemesinde kalan hak sayılmaz; gösterilecek mesaj → Open questions
   (a3). (Kalan hak mesajının e-postanın kayıtlı olduğunu belli etmesi bilinçli olarak kabul edildi, B-4.)
9. Giriş denemeleri aşırı sıklaşırsa (hesaptan bağımsız) geçici olarak reddedilir ve "Çok fazla deneme,
   biraz sonra tekrar deneyin" türünde bir mesaj gösterilir (`docs/security.md`, rate limiting; eşik →
   Open questions b2).

**Erişim ve oturum**
10. Giriş ve kayıt sayfaları dışındaki her sayfa ve her işlem giriş ister (default-deny). Giriş istemeyen
    istisnalar yalnızca: giriş, kayıt, eşleştirilmemiş ajanın eşleştirme kodu alması ve kodunun onay
    durumunu öğrenmesi (Req 20–22) ve — kararlaştırılırsa — kullanıcı verisi içermeyen sağlık kontrolü
    (Open questions b8). Giriş yapmamış biri başka bir sayfayı açarsa giriş sayfasına gönderilir; giriş
    yapınca istediği sayfaya döner, yalnızca site içi adres (öneri → Open questions b11).
11. Giriş yapmış biri giriş ya da kayıt sayfasını açarsa Konseptler sayfasına gönderilir.
12. Oturum süresi dolduğunda (Open questions a5) sayfada yapılan bir işlem sessizce başarısız olmaz:
    kullanıcı "Oturumunuz sona erdi" bilgisiyle giriş sayfasına gönderilir.
13. Üst barda giriş yapmış kullanıcının adı ve "Çıkış yap" eylemi bulunur (yerleşim → Open questions a7;
    tasarımda bu öğe yok).

**Çıkış (BR-14)**
14. "Çıkış yap", kullanıcının tüm oturumlarını — diğer tarayıcılar ve eşleştirilmiş ajan dahil — anında
    geçersiz kılar; ajanın canlı bağlantısı sunucu tarafından kapatılır. Kullanıcı giriş sayfasına gelir.
15. Çıkıştan sonra ajan, kendisinin oturumunun kapandığını gösterir ve yeniden eşleştirme ister; eski
    oturumuyla yeniden bağlanamaz.

**Roller ve yetkiler (BR-13 temeli)**
16. Kullanıcının neleri kullanabileceği rolüne bağlı yetkilerle belirlenir (ilk_promt.txt: "login olması
    haricinde yetkileri olacak"). Bu spec'te iki rol vardır: **kullanıcı** (varsayılan) ve **admin**. Admin
    rolü, admin alanına giriş yetkisini (`admin.panel`) taşır; kullanıcı rolü taşımaz. Diğer yetkiler
    (`publish.use`, `model.use:<model>`) kendi spec'lerinde uygulanır (Open questions b14).
17. Yetkisi olmayan bir sayfayı açan kullanıcı "Bu sayfaya erişim yetkiniz yok", yetkisi olmayan bir işlemi
    isteyen kullanıcı "Bu işlem için yetkiniz yok" durumuyla karşılaşır (0010 ile aynı metin); arayüz
    yetkisiz bağlantıları gizler ama karar her zaman sunucudadır.
18. Admin alanı bu spec'te yalnızca bir iskelettir: admin yetkisi olan kullanıcıya görünen bir giriş
    noktası ve boş durum metni gösteren bir sayfa (giriş noktasının yeri 0010 Open question 1 ile aynı
    karar; içerik 0010 → Open questions a7, b15). İlk admin hesabının nasıl belirleneceği → Open questions a2.

**Ajan cihaz eşleştirme**
19. Eşleştirilmemiş ajan açıldığında bir eşleştirme kodu gösterir ve kodun ne zamana kadar geçerli
    olduğunu belirtir. Ajana şifre yazdırılmaz (`docs/security.md`).
20. Giriş yapmış kullanıcı web'deki eşleştirme ekranına kodu girer (ekranın yeri → Open questions a6);
    ekran kodun ait olduğu cihazın adını gösterir (Open questions b10); kullanıcı "Onayla" ya da "Reddet" der.
21. Onaylanınca ajan kullanıcının hesabına bağlanır, kendi oturumunu alır, sunucuya bağlanır ve
    eşleştiğini gösterir; ajan yeniden başlatılınca yeniden eşleştirme istemez.
22. Reddedilen, süresi dolmuş ya da daha önce kullanılmış kod onaylanamaz; ajan reddedildiğini ya da
    kodun süresinin dolduğunu gösterir ve yeni kod alabilir. Hatalı kod girişi web'de Türkçe hata verir.
23. Eşleştirilmemiş ya da oturumu iptal edilmiş bir ajan sunucuya bağlanamaz ve komut alamaz
    (`docs/security.md`: ajan yalnızca kimliği doğrulanmış bağlantıdan gelen komutları çalıştırır).

**Sahiplik (BR-1 başlangıcı)**
24. Kullanıcıya ait her kayıt sahibini taşır ve yalnızca sahibine görünür. Bu spec'te bu, ajan ve ajan
    olayları için geçerlidir: kullanıcı sistem durumunda yalnızca kendi ajanının bağlı olup olmadığını
    görür, "Ajanı test et" yalnızca kendi ajanına ulaşır, Günlük şeridinde yalnızca kendi olaylarını görür.

## User experience
- Flow (web): Siteyi açan giriş yapmamış kullanıcı giriş sayfasını görür → "Kayıt ol" bağlantısıyla
  kayıt sayfasına geçer → ad, e-posta, şifre girer → Konseptler sayfasına gelir, üst barda adı görünür →
  "Çıkış yap" der → giriş sayfasına döner → e-posta + şifreyle tekrar girer.
- Flow (ajan): Kullanıcı PC'de ajanı başlatır → ajan penceresi bir eşleştirme kodu ve geçerlilik süresini
  gösterir → kullanıcı web'de eşleştirme ekranını açar (yeri → Open questions a6), kodu girer, cihaz adını
  görür, "Onayla" der → ajan "Eşleşti" gösterir, sitede "Ajan bağlı" görünür. Web'den çıkınca ajan
  "Oturum kapandı" gösterir ve yeni kod ister.
- States:
  - Giriş: boş form · gönderiliyor · başarılı (yönlendirme) · eksik/geçersiz alan · yanlış şifre (kalan hak
    2 / 1) · hesap kilitli (açılma zamanı) · kayıtlı olmayan e-posta · çok fazla deneme · sunucuya
    ulaşılamıyor (genel hata, korelasyon numarasıyla). (Banlı hesabın "askıya alındı" durumu 0010'da.)
  - Kayıt: boş form · gönderiliyor · başarılı · alan hatası (ad / e-posta / şifre) · e-posta zaten kayıtlı ·
    sunucuya ulaşılamıyor.
  - Oturum: giriş yapmamış (giriş sayfasına yönlendirme) · oturum süresi doldu ("Oturumunuz sona erdi") ·
    yetkisiz sayfa/işlem ("Bu sayfaya erişim yetkiniz yok" / "Bu işlem için yetkiniz yok").
  - Üst bar: kullanıcı adı + "Çıkış yap" · admin yetkisi varsa admin giriş noktası · yoksa giriş noktası yok.
  - Web eşleştirme ekranı: boş kod alanı · cihaz bilgisi ve Onayla/Reddet · onaylandı · reddedildi · kod
    geçersiz/süresi dolmuş/kullanılmış.
  - Ajan penceresi: kod gösteriliyor (geri sayımla) · kod süresi doldu (yeni kod al) · reddedildi ·
    eşleşti · oturum kapandı (çıkış; ban kaynaklısı 0010'da; yeniden eşleştir) · sunucuya ulaşılamıyor.
  - Admin iskeleti: boş durum metni (yalnızca admin).
- Design reference: none — the flow above is the design. Giriş, kayıt, eşleştirme ekranları ve ajan
  penceresi için tasarım yok; `source/*.png` (commit `61cc824`) üst barında kullanıcı adı / çıkış öğesi
  bulunmuyor (yerleşim → Open questions a7). Renk ve tema 0001'deki tema değişkenlerinden gelir
  (turuncu/kahve, açık/koyu).
- Accessibility: tüm form alanlarının görünür etiketi var; Tab sırası alan sırasını izler, Enter formu
  gönderir; hata mesajları ilgili alanla ilişkilendirilir ve ekran okuyucuya duyurulur (canlı bölge);
  hata sonrası odak ilk hatalı alana gider; şifre alanı maskelidir; ajan penceresindeki kod seçilip
  kopyalanabilir ve büyük, okunaklı yazılır; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki. (H-2'ye yalnızca şifrelerin hash'lenip hiç loglanmaması yönüyle
  değer: `docs/security.md`.)
- Erişim: yalnızca yerel ağ, HTTP (B-3); yapı ayarla HTTPS'e geçebilir. Web 8000 / Api 8001 (0001, D2).
  Arayüz metinleri yalnızca Türkçe. Kalan hak mesajının e-posta sızdırması bilinçli kabul (B-4).
- Kimlik bilgileri (şifre, oturum belirteçleri, eşleştirme onay sırrı) hiçbir logda, hata mesajında ya da
  Günlük şeridinde görünmez; şifre düz metin saklanmaz. Tarayıcı oturum belirtecini görmez (`docs/security.md`).
- Bağımlılık: 0001 (üst bar, sistem durumu, "Ajanı test et", Günlük şeridi, ajan bağlantısı) bu spec'le
  giriş ister hale gelir.
- Out of scope: e-posta doğrulaması (B-2), şifremi unuttum / şifre değiştirme, profil (0003), model
  yetkilerinin uygulanması ve Ayarlar sayfasının temeli (0004), **ban, ban kaldırma ve etkileri** (oturum
  iptali, ajanın düşürülmesi, "askıya alındı" giriş mesajı — 0010 Req 5–8, AC-8–12), admin paneli içeriği,
  kullanıcı listesi (0010), ajan kurulum ekranı (0009), dış sağlayıcıyla giriş (Google vb.), iki adımlı
  doğrulama, HTTPS.
- Split: proposed — bu spec birden çok kullanıcı yeteneği taşıyor ve 25 kriter içeriyor. Önerilen
  sıra: **0002a Kayıt, giriş, çıkış ve erişim** (Req 1–15, AC-1–13, 23–25) → **0002b Roller, yetkiler ve
  admin iskeleti** (Req 16–18, AC-14–15) → **0002c Ajan cihaz eşleştirme ve ajan sahipliği** (Req 19–24,
  AC-16–22). Her biri ayrı ayrı H-1 kanıtı ister. Karar insanda (Open questions a8).

## Acceptance criteria
- [ ] AC-1 — Geçerli ad, e-posta ve şifreyle kayıt olan kişi giriş yapmış olarak Konseptler sayfasına gelir, üst barda adı görünür ve hesabı kullanıcı rolündedir (admin giriş noktası görünmez).
- [ ] AC-2 — Kayıtlı bir e-postayla, büyük/küçük harf farkıyla yazılmış olsa bile (ör. `irem@x.com` kayıtlıyken `IREM@X.COM`; testler `tr-TR` kültüründe), ikinci kayıt reddedilir, "Bu e-posta zaten kayıtlı" türünde mesaj gösterilir ve ikinci hesap oluşmaz.
- [ ] AC-3 — Boş ad, geçersiz e-posta biçimi, sınır dışı uzunluk ya da şifre kuralına uymayan şifre ile kayıt reddedilir; her hatalı alan kendi Türkçe mesajını gösterir; ad ve e-posta formda kalır, şifre boşaltılır; kayıt sınır değerlerde (izin verilen en kısa/en uzun) kabul edilir.
- [ ] AC-4 — Doğru e-posta ve şifreyle giriş Konseptler sayfasına götürür; giriş öncesi başka bir site içi sayfa istenmişse o sayfaya döner; site dışı bir dönüş adresi verilmişse yok sayılır ve Konseptler açılır.
- [ ] AC-5 — Yanlış şifreyle 1. denemede "Yanlış girdiniz, 2 hakkınız kaldı", 2. denemede "Yanlış girdiniz, 1 hakkınız kaldı" gösterilir; 3. denemede hesap kilitlenir ve kilidin açılma zamanı gösterilir.
- [ ] AC-6 — Kilitliyken doğru şifreyle giriş reddedilir ve kilit mesajı gösterilir; kilit süresi dolduktan sonra doğru şifreyle giriş başarılı olur; kilit sonrası yanlış şifrede sayaç yeniden "2 hakkınız kaldı"dan başlar.
- [ ] AC-7 — 2 yanlış denemenin ardından başarılı giriş sayacı sıfırlar: sonraki yanlış denemede "2 hakkınız kaldı" gösterilir.
- [ ] AC-8 — Kayıtlı olmayan bir e-postayla giriş denemesi reddedilir, kalan hak sayısı içermeyen kararlaştırılmış mesajı gösterir ve hiçbir hesabın sayacını değiştirmez.
- [ ] AC-9 — Kararlaştırılan eşiği aşan sıklıkta giriş denemesi, kimlik bilgileri doğru olsa bile geçici olarak reddedilir ve "Çok fazla deneme" mesajı gösterilir; bekleme sonrası giriş yeniden mümkün olur.
- [ ] AC-10 — Giriş yapmamış biri giriş ve kayıt dışındaki her sayfayı açınca giriş sayfasına gönderilir; giriş istemeyen işlemler yalnızca Req 10'daki istisna listesinden oluşur (default-deny); giriş yapmamış biri sitenin üzerinden ya da sunucuya doğrudan gönderilen isteklerle hiçbir kullanıcı verisi alamaz ve bu istisnalar dışında hiçbir işlem yapamaz.
- [ ] AC-11 — Giriş yapmış kullanıcı giriş ya da kayıt sayfasını açınca Konseptler sayfasına gönderilir.
- [ ] AC-12 — Oturumu süresi dolmuş kullanıcının sayfada yaptığı işlem (Ajax dahil) "Oturumunuz sona erdi" bilgisiyle giriş sayfasına yönlendirir; işlem sessizce başarısız olmaz.
- [ ] AC-13 — İki ayrı tarayıcıda ve eşleştirilmiş bir ajanla oturumu açık kullanıcı bir tarayıcıdan "Çıkış yap" dediğinde: o tarayıcı giriş sayfasına gelir, diğer tarayıcıdaki bir sonraki işlem giriş sayfasına yönlendirir, ajan bağlantısı en geç 10 sn içinde kapanır ve ajan "Oturum kapandı" gösterir; ajan eski oturumuyla yeniden bağlanamaz.
- [ ] AC-14 — Kullanıcı rolündeki biri admin alanını adresini yazarak açmaya çalıştığında "Bu sayfaya erişim yetkiniz yok", admin verisini sunucuya doğrudan isteyerek almaya çalıştığında yetkisiz yanıt alır ve hiçbir admin verisi dönmez; admin giriş noktasını görmez.
- [ ] AC-15 — Admin rolündeki kullanıcı admin giriş noktasını görür ve admin iskelet sayfasını boş durum metniyle açar; ilk admin hesabı kararlaştırılan yolla oluşur, başka hiçbir yolla (ör. kayıt formuna ek alan göndermek) kullanıcı kendine admin rolü veremez.
- [ ] AC-16 — Eşleştirilmemiş ajan açılınca bir kod ve geçerlilik süresini gösterir; kullanıcı web'de kodu girince cihaz adı görünür; "Onayla" sonrası web ekranı "Onaylandı" gösterir, ajan "Eşleşti" gösterir ve sitede en geç 10 sn içinde "Ajan bağlı" görünür.
- [ ] AC-17 — Eşleşmiş ajan kapatılıp yeniden açılınca eşleştirme istemeden aynı hesaba bağlanır.
- [ ] AC-18 — "Reddet" sonrası web ekranı "Reddedildi" gösterir, ajan reddedildiğini gösterir ve bağlanmaz; süresi dolmuş, daha önce kullanılmış ya da var olmayan kod web'de onaylanamaz ve Türkçe hata gösterir; ajan süresi dolan kod için yeni kod alabilir.
- [ ] AC-19 — Eşleştirilmemiş ya da oturumu iptal edilmiş ajanın sunucuya bağlanma denemesi reddedilir ve bu ajana hiçbir komut ulaşmaz.
- [ ] AC-20 — A ve B kullanıcıları için: A'nın ajanı bağlıyken B'nin sistem durumunda "Ajan bağlı değil" görünür; B'nin "Ajanı test et" eylemi A'nın ajanına ulaşmaz ("Ajan bağlı değil" döner); B'nin Günlük şeridinde A'nın ajan olayları görünmez; B, A'nın onaylanmamış kodunu kendi hesabıyla onaylarsa ajan B'ye bağlanır, A'ya değil (kodu kim onaylarsa ajan onundur).
- [ ] AC-21 — Ajan sunucuya ulaşamazken açılırsa "Sunucuya ulaşılamıyor" gösterir ve kendiliğinden yeniden dener; sunucu erişilebilir olunca kod gösterir (eşleşmemişse) ya da bağlanır (eşleşmişse) (Open questions b9).
- [ ] AC-22 — Giriş, kayıt ve eşleştirme formları gönderilirken "gönderiliyor" durumu görünür ve gönder düğmesi pasiftir; çift tıklama iki hesap açmaz ve iki yanlış deneme saymaz; boş alanla gönderilen giriş formu alan hatası gösterir ve hak sayacını değiştirmez (Open questions b12).
- [ ] AC-23 — Kayıt, giriş (başarılı ve başarısız), çıkış ve eşleştirme sırasında üretilen hiçbir log, hata mesajı ya da Günlük kaydı şifreyi ya da oturum belirtecini içermez; veritabanında şifre düz metin bulunmaz (kasıtlı bozma ile kanıtlanır).
- [ ] AC-24 — Giriş, kayıt ve eşleştirme formları yalnızca klavyeyle doldurulup gönderilebilir; her alanın etiketi var; hata mesajları ekran okuyucuya duyurulur ve odak ilk hatalı alana gider.
- [ ] AC-25 — Giriş ya da kayıt sırasında sunucuya ulaşılamazsa form "Şu an işleminizi yapamıyoruz" türünde, korelasyon numaralı genel bir hata gösterir; iç ayrıntı (stack trace, SQL) görünmez ve girilen ad/e-posta formda kalır.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**(a) Netleştirme soruları (etkiye göre sıralı)**
1. **Kilit süresi (Req 6–7, AC-5/6).** Öneri: **15 dakika**, sabit (her kilitte artmayan). Gerekçe: 3 hak
   zaten sıkı; LAN'da tek kullanıcı için 15 dk kaba kuvveti durdurur ama kendini kilitleyen geliştiriciyi
   uzun süre bekletmez. Kilidin açılma zamanı "hh:mm'de açılacak" biçiminde, kullanıcının yerel saatiyle gösterilir.
2. **İlk admin hesabı (Req 18, AC-15).** Öneri: Pi'deki ayarlarda (gizli bilgi değil, ortam ayarı) verilen
   **bir e-posta adresi**, o e-postayla kayıt olunduğunda ya da API açılışında hesap varsa admin rolü alır;
   başka yol yok (0010 Open question 2 ile aynı öneri). Gerekçe: "ilk kayıt olan admin olur" kuralı
   internete açılış günü yarış durumu ve risk doğurur; ayar yolu deterministik ve test edilebilir.
3. **Kayıtlı olmayan e-postada mesaj (Req 8, AC-8).** Öneri: "E-posta ya da şifre hatalı" (kalan hak
   yok). Gerekçe: hak sayacı bir hesaba aittir; "Bu e-posta kayıtlı değil" demek sızıntıyı daha da açar.
   B-4 zaten kayıtlı e-postaları belli ettiği için tam gizlilik sağlamaz; bu internete açılırken birlikte
   ele alınır.
4. **Şifre ve alan kuralları (Req 1, 3, AC-3).** Öneri: şifre **en az 8, en fazla 128 karakter, en az bir harf
   ve bir rakam**; ad 1–100 karakter (baş/son boşluk kırpılır); e-posta en fazla 254 karakter. Gerekçe:
   Identity'nin varsayılanı (büyük harf + sembol zorunlu) LAN tek kullanıcı için gereksiz sürtünme;
   8 karakter + harf/rakam makul taban. Şifre tekrar alanı: öneri **yok** (göster/gizle düğmesi de yok;
   ileride istenirse eklenir).
5. **Oturum süreleri (Req 12, 21, AC-12/17).** Öneri: web oturumu **son kullanımdan itibaren 14 gün** (hareket
   oldukça uzar), "beni hatırla" seçeneği yok; ajan oturumu **son kullanımdan itibaren 90 gün** — ajan
   çalıştıkça yenilenir, yalnızca çıkış, ban (0010) ya da 90 gün kullanılmama ile düşer. Gerekçe: ajanın
   her gün yeniden eşleştirme istemesi kurulum deneyimini bozar; 15 dk'lık kısa ömürlü erişim + dönen
   yenileme zaten `docs/security.md`'de karar.
6. **Ajan eşleştirme ayrıntıları ve web eşleştirme ekranının yeri (Req 19–22, AC-16/18).** Öneri: kod
   **8 karakter, `ABCD-1234` biçiminde, karışan harfler (O/0, I/1) hariç, 10 dakika geçerli, tek
   kullanımlık**; ajan penceresinde "Siteye git" düğmesi web eşleştirme ekranını açar ama kodu adreste
   taşımaz, kullanıcı kodu elle yazar (kodun tarayıcı geçmişine düşmemesi için). Kullanıcı başına **tek
   eşleşmiş ajan**: yeni eşleştirme eskisini iptal eder. Ekranın yeri: öneri **ayrı bir "Ajanı eşleştir"
   sayfası**, ajan bağlı değilken sistem durumundaki "Ajanı eşleştir" bağlantısından açılır. Gerekçe:
   BR-11 ve tasarımlar tek ajan varsayıyor; çok cihaz desteği somut bir istek gelince eklenir. Ayarlar
   sayfası 0004'te çalışır hale geliyor (0004 Req 20) ve 0002 ondan önce geliyor; eşleştirmeyi Ayarlar'a
   koymak 0004'ün temelini 0002'ye çekerdi. 0004/0009 sonrasında Ayarlar'a bir "Ajan" bölümü olarak
   taşınabilir.
7. **Üst barda kullanıcı ve çıkış öğesi + admin giriş noktası (Req 13, 18).** Tasarımda yok. Öneri: üst
   barın en sağında, tema düğmesinin yanında **kullanıcı adı olan küçük bir menü**, içinde "Çıkış yap".
   Admin giriş noktası 0010 Open question 1 ile **tek kararla**: "Ayarlar"ın yanında yalnızca
   `admin.panel` yetkisi olanlara görünen "Admin" ikonu. Admin iskeleti bu spec'te boş sayfa olarak gelir.
   Gerekçe: üst bar sırası tasarımdaki öğeleri bozmadan yalnızca sona ekleme yapar; iki spec'in farklı
   yerleşim önermesi çelişki doğururdu; admin iskeleti olmadan AC-14/15 (yetki kontrolü) gözle kanıtlanamaz.
8. **Split (Constraints).** Öneri: **üçe böl** (0002a → 0002b → 0002c). Gerekçe: 25 kriter tek bir H-1
   review turunu şişirir; her parçada kasıtlı bozma kanıtı daha yönetilebilir. Tek tutulacaksa
   Constraints'e "kept as one — <kim>, <neden>" yazılır.

**(b) Self-critique boşlukları (düşmanca okuyan geliştirici gözüyle; önerilerimle, onayda katlanır)**
1. **Kayıt sonrası otomatik giriş (Req 4)** brief'te açıkça yok. Öneri: otomatik giriş — tek kullanıcı için
   ikinci form adımı gereksiz.
2. **Rate limiting eşiği (Req 9, AC-9)** sayısız. Öneri: aynı IP'den **dakikada 10** giriş/kayıt denemesi;
   aşılırsa 1 dk bekleme. Hesap kilidinden (3 hak) bağımsızdır; eşleştirme kodu onayına (kaba kuvvetle kod
   tahmini) ve giriş istemeyen kod alma isteğine de aynı sınır.
3. **Eşzamanlı yanlış denemeler:** iki sekmeden aynı anda gelen yanlış denemeler sayacı atlatmamalı.
   Öneri: AC-5'e "eşzamanlı 3 yanlış deneme de kilide yol açar" testi eklensin.
4. **Kilit sırasında yanlış şifre** sayacı ya da süreyi uzatır mı? Öneri: hayır, kilit süresi sabit kalır,
   aynı kilit mesajı döner.
5. **Çıkışın üretim işlerine etkisi:** çıkış ajanı düşürür; o sırada render eden bir iş ne olur? Öneri:
   bu spec yalnızca bağlantıyı keser; sunucu tarafı aşamalar sürer, yerel aşamalar BR-11 gereği
   "ajan bekleniyor"a düşer — ayrıntı 0006'da. Ayrıca BR-14'ün bedeli: her web çıkışı ajanı yeniden
   eşleştirmeyi gerektirir; bu karar bilerek kabul edilir.
6. **"En geç 10 sn" (AC-13/16)** 0001'deki ajan durum sınırıyla aynı tutuldu. Öneri: kalsın.
7. **Ban bu spec'ten çıkarıldı (bağımsız review).** Önceki taslakta Req 19–20 / AC-16–17 ban etkisini
   tanımlıyordu; 0010 aynı davranışı (Req 5–8, AC-8–12) arayüzüyle birlikte zaten tanımlıyor ve yol
   haritasında ban 0010'da. Öneri: ban ve etkisi yalnızca 0010'da kalsın; 0002 yalnızca "tüm oturumları
   iptal et" yeteneğini çıkış için kurar, 0010 bunu yeniden kullanır. Alternatif: ban etkisi 0002'de,
   arayüzü 0010'da — seçilirse 0010'daki AC-8–11 0002'ye taşınır, iki yerde tutulmaz.
8. **0001'deki kimliksiz durum uçları:** skeleton'ın sistem durumu ve "Ajanı test et" artık giriş ister;
   Pi sağlık kontrolü (deploy betikleri) için kimliksiz, yalnızca "ayakta mı" bilgisini veren, kullanıcı
   verisi içermeyen tek bir uç açık kalsın mı? Öneri: evet, yalnızca API/DB ayakta bilgisi ve versiyon;
   ajan durumu dahil kullanıcıya ait hiçbir bilgi yok. Req 10'daki istisna listesine bu eklenir.
9. **Ajan, sunucuya ulaşamazken açılırsa** eşleştirme kodu alamaz. Öneri: ajan "Sunucuya ulaşılamıyor,
   yeniden deneniyor" gösterir ve otomatik dener (0001'deki yeniden bağlanma kuralı) — AC-21.
10. **Ajan cihaz adı** neyi gösterir? Öneri: makine adı + ajan sürümü (0001'de zaten okunuyor); kullanıcı
    yabancı bir cihaz görürse "Reddet" der.
11. **Girişten sonra istenen sayfaya dönüş (Req 10, AC-4)** brief'te yok. Öneri: kalsın, yalnızca site
    içi adreslere (açık yönlendirme önlemi); istenmezse AC-4'ün dönüş kısmı çıkarılır ve her giriş
    Konseptler'e gider.
12. **Boş alanla giriş ve çift gönderim (AC-22)** brief'te yok. Öneri: boş alan sunucuya deneme olarak
    sayılmaz (hak sayacı değişmez); gönderim sırasında düğme pasif.
13. **0010'un ihtiyaç duyduğu kimlik verisi:** 0010 kullanıcı listesinde "kayıt tarihi, son giriş zamanı"
    ve Aktiviteler'de "giriş, çıkış" gösteriyor (ilk_promt.txt: "kullanıcıların aktiviteleri"). Öneri:
    bu spec'e "kayıt zamanı ve son başarılı giriş zamanı kaydedilir; giriş, çıkış, kayıt ve ajan
    eşleştirme olayları kullanıcıya bağlı olarak kaydedilir (şifre/belirteç olmadan)" gereksinimi ve
    buna bir kriter eklensin; gösterimi 0010'da.
14. **Varsayılan kullanıcı rolünün diğer yetkileri:** "herkes her modeli / publish'i kullanamayacak"
    (ilk_promt.txt). Bu spec yalnızca `admin.panel`'i uygular. Öneri: kullanıcı rolünün hangi
    `model.use:<model>` yetkilerini taşıyacağı 0004'te kararlaştırılsın; o güne kadar model kısıtı yok.
    `publish.use` yayınlama gelince.
15. **0010 ile örtüşme:** admin iskeleti (Req 18) ve yetkisiz sayfa durumu 0010 AC-1 ile örtüşüyor.
    Öneri: 0002 yalnızca boş iskeleti ve yetki kontrolünü kurar; 0010 iskeletin içini doldurur ve
    0010 AC-1'i 0002 AC-14/15'e dayandırır (aynı testin iki kez yazılmaması).

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
