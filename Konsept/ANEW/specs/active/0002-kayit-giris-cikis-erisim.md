# Spec 0002 — Kayıt, giriş, çıkış ve erişim

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0002-kimlik.md` (Req 1–15, AC-1–13, 22–25 ve Req 24'ün Günlük kısmı) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-1 Kimlik ve yetki (H-2'ye yalnızca şifrelerin hash'lenip hiç loglanmaması yönüyle değer: `docs/security.md`)
- Depends on: 0001, 0008

## Intent
ANEW Konsept'e kimlik geliyor: içerik üreticisi ad, e-posta ve şifreyle kayıt olur, e-posta ve şifreyle
giriş yapar, çıkış yapınca bütün oturumlarından çıkmış olur. Şimdilik yalnızca geliştirici yerel ağda
kullanıyor, ama yapı çok kullanıcılı kuruluyor: giriş yapmadan siteye ve sunucudaki hiçbir işleme
erişilemez, her kullanıcı yalnızca kendi Günlük olaylarını görür (BR-1'in başlangıcı). Başarı: giriş
yapmadan hiçbir kullanıcı verisine ulaşılamıyor; yanlış şifre 3 hakla sınırlı; çıkış diğer tarayıcıları
da anında düşürüyor. Bilinçli olarak yapılmayanlar: e-posta doğrulaması (B-2), şifremi unuttum, HTTPS
(B-3), "kalan hak" mesajının e-posta sızdırmasının giderilmesi (B-4); roller ve admin iskeleti 0011'de,
ajan eşleştirme ve ajanın çıkışta düşürülmesi 0012'de, ban ve etkisi 0024'te.

## Requirements
**Kayıt**
1. Giriş yapmamış biri kayıt sayfasında ad, e-posta ve şifre girerek doğrudan hesap açabilir; e-posta
   doğrulaması yapılmaz (B-2). Şifre bir kez sorulur (tekrar alanı yok; göster/gizle düğmesi yok).
2. Aynı e-postayla ikinci hesap açılamaz; e-posta karşılaştırmasında büyük/küçük harf farkı gözetilmez ve
   karşılaştırma Türkçe kültür ayarından etkilenmez (ör. `IREM@x.com` ile `irem@x.com` aynı hesaptır;
   `docs/conventions.md`, Türkçe I/İ tuzağı).
3. Geçerlilik kuralları: ad baş/son boşlukları kırpıldıktan sonra 1–100 karakter; e-posta geçerli biçimde
   ve en fazla 254 karakter; şifre 8–128 karakter, en az bir harf ve en az bir rakam içerir. Kurala uymayan
   kayıt reddedilir ve hangi alanın neden geçersiz olduğu Türkçe gösterilir; girilen ad ve e-posta formda
   korunur, şifre korunmaz.
4. Kayıt başarılı olunca kullanıcı otomatik olarak giriş yapmış olur ve Konseptler sayfasına gelir.
   Hesabın kayıt zamanı kaydedilir. (Yeni hesabın rolü 0011'de.)

**Giriş**
5. Kayıtlı kullanıcı e-posta ve şifreyle giriş yapar; giriş başarılıysa Konseptler sayfasına gelir ve son
   başarılı giriş zamanı kaydedilir.
6. Yanlış şifrede ekranda kalan hak gösterilir: 1. hatada "Yanlış girdiniz, 2 hakkınız kaldı", 2. hatada
   "Yanlış girdiniz, 1 hakkınız kaldı". 3. hatada hesap **15 dakika** kilitlenir ve kilidin açılma saati
   "HH:mm'de açılacak" biçiminde (Europe/Istanbul) gösterilir. Başarılı giriş hata sayacını sıfırlar.
   Aynı anda (ör. iki sekmeden) gelen yanlış denemeler sayacı atlatamaz: her yanlış deneme sayılır.
7. Kilitli hesaba, şifre doğru olsa bile kilit süresi dolana kadar giriş yapılamaz. Kilit süresi sabittir:
   kilitliyken yapılan (doğru ya da yanlış) denemeler süreyi uzatmaz, sayacı değiştirmez ve aynı kilit
   mesajını döndürür. Süre dolunca kullanıcı yeniden 3 hakla başlar.
8. Kayıtlı olmayan bir e-postayla giriş denemesinde "E-posta ya da şifre hatalı" gösterilir; kalan hak
   gösterilmez ve hiçbir hesabın sayacı değişmez. (Kalan hak mesajının e-postanın kayıtlı olduğunu belli
   etmesi bilinçli olarak kabul edildi, B-4; tam gizlilik internete açılırken ele alınır.)
9. Aynı IP adresinden dakikada 10'dan fazla giriş ya da kayıt denemesi gelirse (hesaptan bağımsız) sonraki
   denemeler 1 dakika boyunca reddedilir ve "Çok fazla deneme, biraz sonra tekrar deneyin" gösterilir
   (`docs/security.md`, rate limiting). Bu sınır hesap kilidinden (3 hak) bağımsızdır.
10. Boş alanla gönderilen giriş formu alan hatası gösterir ve deneme sayılmaz (hak sayacı değişmez).
    Gönderim sürerken gönder düğmesi pasiftir; çift tıklama iki hesap açmaz, iki deneme saymaz.

**Erişim ve oturum**
11. Giriş ve kayıt sayfaları dışındaki her sayfa ve her işlem giriş ister (default-deny). Giriş istemeyen
    istisnalar yalnızca: giriş, kayıt; kullanıcı verisi içermeyen sağlık kontrolü (yalnızca API ve
    veritabanının ayakta olup olmadığı ve sürüm; ajan durumu dahil kullanıcıya ait hiçbir bilgi yok);
    ve **geçici olarak** ajanın 0001'deki sunucu bağlantısı — 0012 bu istisnayı kaldırır ve yerine
    eşleştirme istisnalarını koyar. Giriş yapmamış biri başka bir sayfayı açarsa giriş sayfasına
    gönderilir; giriş yapınca istediği sayfaya döner, yalnızca site içi adreslere (site dışı dönüş adresi
    yok sayılır).
12. Giriş yapmış biri giriş ya da kayıt sayfasını açarsa Konseptler sayfasına gönderilir.
13. Web oturumu son kullanımdan itibaren **14 gün** geçerlidir ve kullanıldıkça uzar; "beni hatırla"
    seçeneği yoktur. Oturum süresi dolduğunda sayfada yapılan bir işlem (Ajax dahil) sessizce başarısız
    olmaz: kullanıcı "Oturumunuz sona erdi" bilgisiyle giriş sayfasına gönderilir.
14. Üst barın en sağında, tema düğmesinin yanında, giriş yapmış kullanıcının adını taşıyan küçük bir menü
    bulunur; menüde "Çıkış yap" eylemi vardır.

**Çıkış (BR-14)**
15. "Çıkış yap", kullanıcının tüm oturumlarını — diğer tarayıcılar dahil — anında geçersiz kılar ve
    kullanıcıyı giriş sayfasına götürür. Eşleştirilmiş ajanın oturumunun da düşürülmesi 0012'de (aynı
    "tüm oturumları iptal et" yeteneğini kullanır; 0024'teki ban da aynı yeteneği kullanır).
16. Kullanıcının üretiminin durması 0006'nın konusudur (0006 Req 16 ve Req 21, AC-19 ve AC-26: çalışan iş
    *Durduruldu* olur, *Sırada* işler *Sırada* kalır ve sıra durur). Üretim yalnızca kullanıcının **son geçerli web
    oturumu kapandığında** durur: çıkışta (Req 15 tüm oturumları düşürdüğü için her çıkış son oturumu da kapatır)
    ya da son geçerli oturumun süresi dolduğunda (Req 13). Bir tarayıcının oturum süresi dolarken başka bir
    tarayıcıda geçerli oturum varsa üretim durmaz. Bu spec'te üretim işi yoktur.

**Olaylar ve sahiplik (BR-1 başlangıcı)**
17. Kayıt, başarılı giriş, başarısız giriş (kayıtlı bir hesaba yapılan) ve çıkış olayları, kullanıcıya bağlı
    olarak 0001'in olay altyapısına yazılır; şifre ve oturum belirteci içermez. Bu olaylar Günlük
    şeridinde görünür ve admin "aktivite" verisinin kaynağıdır (gösterimi 0010'da).
18. Günlük şeridinde kullanıcı yalnızca kendi olaylarını görür.

## User experience
- Flow: Siteyi açan giriş yapmamış kullanıcı giriş sayfasını görür → "Kayıt ol" bağlantısıyla kayıt
  sayfasına geçer → ad, e-posta, şifre girer → Konseptler sayfasına gelir, üst barda adı görünür →
  kullanıcı menüsünden "Çıkış yap" der → giriş sayfasına döner → e-posta + şifreyle tekrar girer.
- States:
  - Giriş: boş form · gönderiliyor · başarılı (yönlendirme) · eksik/geçersiz alan · yanlış şifre (kalan hak
    2 / 1) · hesap kilitli ("HH:mm'de açılacak") · "E-posta ya da şifre hatalı" (kayıtlı olmayan e-posta) ·
    çok fazla deneme · sunucuya ulaşılamıyor (genel hata, korelasyon numarasıyla). (Banlı hesabın
    "askıya alındı" durumu 0024'te.)
  - Kayıt: boş form · gönderiliyor · başarılı · alan hatası (ad / e-posta / şifre) · e-posta zaten kayıtlı ·
    çok fazla deneme · sunucuya ulaşılamıyor.
  - Oturum: giriş yapmamış (giriş sayfasına yönlendirme) · oturum süresi doldu ("Oturumunuz sona erdi").
  - Üst bar: kullanıcı adı menüsü + "Çıkış yap". (Admin giriş noktası 0011'de.)
- Design reference: none — the flow above is the design. Giriş ve kayıt ekranları için tasarım yok;
  `source/*.png` (commit `61cc824`) üst barında kullanıcı menüsü bulunmuyor, Req 14'teki yerleşim
  tasarımdaki öğeleri bozmadan yalnızca sona ekleme yapar. Renk ve tema 0001'deki tema değişkenlerinden
  gelir (turuncu/kahve, açık/koyu).
- Accessibility: tüm form alanlarının görünür etiketi var; Tab sırası alan sırasını izler, Enter formu
  gönderir; hata mesajları ilgili alanla ilişkilendirilir ve ekran okuyucuya duyurulur (canlı bölge);
  hata sonrası odak ilk hatalı alana gider; şifre alanı maskelidir; kullanıcı menüsü klavyeyle açılır;
  kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Erişim: yalnızca yerel ağ, HTTP (B-3); yapı ayarla HTTPS'e geçebilir. Web 8000 / Api 8001 (0001, D2).
  Arayüz metinleri yalnızca Türkçe. Kalan hak mesajının e-posta sızdırması bilinçli kabul (B-4).
- Kimlik bilgileri (şifre, oturum belirteçleri) hiçbir logda, hata mesajında ya da Günlük şeridinde
  görünmez; şifre düz metin saklanmaz. Tarayıcı oturum belirtecini görmez (`docs/security.md`).
  Maskeleme mekanizması ve genel kanıtı 0008'de; bu spec yalnızca kendi alanlarının maskeli olduğunu
  kanıtlar (AC-17).
- Bağımlılık: 0001 (üst bar, sistem durumu, "Ajanı test et", Günlük şeridi, olay altyapısı) bu spec'le
  giriş ister hale gelir; 0008 (loglama, maskeleme, korelasyon numarası).
- Bilinen geçiş durumu: 0012'ye kadar ajanın sunucu bağlantısı kimliksizdir (Req 11) ve ajan tek, ortak
  kalır; sahiplik ve kimlikli ajan bağlantısı 0012'de. 0002 ile 0012 arasında Pi'ye sürüm çıkılmaz; bu
  aralıkta sistem yalnızca yerel ağda, tek kullanıcıyla (geliştirici) çalışır.
- Out of scope: e-posta doğrulaması (B-2), şifremi unuttum / şifre değiştirme, roller, yetkiler ve admin
  iskeleti (0011), ajan cihaz eşleştirme, ajan sahipliği ve çıkışta ajanın düşürülmesi (0012), profil
  (0003), model yetkilerinin uygulanması (`model.use`, 0013), Ayarlar sayfasının temeli (0004), çıkışta
  üretimin durması (0006), **ban, ban kaldırma ve etkileri** ("askıya alındı" giriş mesajı dahil — 0024),
  admin istatistik ve aktivite gösterimi (0010), kullanıcı listesi (0024), dış sağlayıcıyla giriş (Google
  vb.), iki adımlı doğrulama, HTTPS.
- Split: done — 0002 Kayıt, giriş, çıkış ve erişim / kardeşler: 0011, 0012

## Acceptance criteria
- [ ] AC-1 — Geçerli ad, e-posta ve şifreyle kayıt olan kişi otomatik olarak giriş yapmış şekilde Konseptler sayfasına gelir; üst barın en sağındaki kullanıcı menüsünde adı ve "Çıkış yap" görünür; hesabın kayıt zamanı kaydedilmiştir.
- [ ] AC-2 — Kayıtlı bir e-postayla, büyük/küçük harf farkıyla yazılmış olsa bile (ör. `irem@x.com` kayıtlıyken `IREM@X.COM`; testler `tr-TR` kültüründe), ikinci kayıt reddedilir, "Bu e-posta zaten kayıtlı" türünde mesaj gösterilir ve ikinci hesap oluşmaz.
- [ ] AC-3 — Kayıt şu durumlarda reddedilir ve her hatalı alan kendi Türkçe mesajını gösterir: yalnızca boşluktan oluşan ya da kırpıldıktan sonra 101 karakter olan ad; geçersiz biçimli ya da 255 karakterlik e-posta; 7 ya da 129 karakterlik şifre; rakamsız ya da harfsiz şifre. Ad ve e-posta formda kalır, şifre boşaltılır. Sınır değerlerde (1 ve 100 karakterlik ad, 254 karakterlik e-posta, harf+rakam içeren 8 ve 128 karakterlik şifre) kayıt kabul edilir; formda tek şifre alanı vardır.
- [ ] AC-4 — Doğru e-posta ve şifreyle giriş Konseptler sayfasına götürür ve son başarılı giriş zamanını günceller; giriş öncesi başka bir site içi sayfa istenmişse o sayfaya döner; site dışı bir dönüş adresi verilmişse yok sayılır ve Konseptler açılır.
- [ ] AC-5 — Yanlış şifreyle 1. denemede "Yanlış girdiniz, 2 hakkınız kaldı", 2. denemede "Yanlış girdiniz, 1 hakkınız kaldı" gösterilir; 3. denemede hesap 15 dakika kilitlenir ve "HH:mm'de açılacak" biçiminde (Europe/Istanbul) açılma saati gösterilir.
- [ ] AC-6 — Aynı hesaba eşzamanlı gönderilen 3 yanlış deneme de hesabı kilitler; eşzamanlılık sayacı atlatıp 3'ten fazla deneme hakkı vermez.
- [ ] AC-7 — Kilitliyken doğru şifreyle giriş reddedilir ve kilit mesajı gösterilir; kilitliyken yapılan yanlış deneme açılma saatini değiştirmez ve aynı mesajı döndürür; 15 dakika dolduktan sonra doğru şifreyle giriş başarılı olur; kilit sonrası ilk yanlış şifrede sayaç yeniden "2 hakkınız kaldı"dan başlar.
- [ ] AC-8 — 2 yanlış denemenin ardından başarılı giriş sayacı sıfırlar: sonraki yanlış denemede "2 hakkınız kaldı" gösterilir.
- [ ] AC-9 — Kayıtlı olmayan bir e-postayla giriş denemesi reddedilir, "E-posta ya da şifre hatalı" gösterir (kalan hak sayısı yok) ve hiçbir hesabın sayacını değiştirmez.
- [ ] AC-10 — Aynı IP'den bir dakika içinde 11. giriş ya da kayıt denemesi, kimlik bilgileri doğru olsa bile reddedilir ve "Çok fazla deneme" mesajı gösterilir; 1 dakika bekledikten sonra giriş yeniden mümkün olur; bu reddetme hiçbir hesabın hak sayacını değiştirmez.
- [ ] AC-11 — Giriş yapmamış biri giriş ve kayıt dışındaki her sayfayı açınca giriş sayfasına gönderilir; giriş istemeyen işlemler yalnızca Req 11'deki istisna listesinden oluşur (default-deny); giriş yapmamış biri sitenin üzerinden ya da sunucuya doğrudan gönderilen isteklerle hiçbir kullanıcı verisi alamaz ve bu istisnalar dışında hiçbir işlem yapamaz; sağlık kontrolü yalnızca API/veritabanı durumu ve sürümü döndürür, ajan durumu dahil kullanıcıya ait hiçbir bilgi içermez.
- [ ] AC-12 — Giriş yapmış kullanıcı giriş ya da kayıt sayfasını açınca Konseptler sayfasına gönderilir.
- [ ] AC-13 — Son kullanımından bu yana 14 günden uzun süre geçmiş oturumla sayfada yapılan işlem (Ajax dahil) "Oturumunuz sona erdi" bilgisiyle giriş sayfasına yönlendirir ve sessizce başarısız olmaz; 14 gün dolmadan kullanılan oturum geçerli kalır ve süresi son kullanımdan yeniden başlar (saat ilerletilerek test edilir).
- [ ] AC-14 — İki ayrı tarayıcıda oturumu açık kullanıcı birinden "Çıkış yap" dediğinde: o tarayıcı giriş sayfasına gelir, diğer tarayıcıdaki bir sonraki işlem (Ajax dahil) giriş sayfasına yönlendirir; çıkış yapılmış oturumun belirteciyle sunucuya gönderilen istek reddedilir.
- [ ] AC-15 — Kayıt, başarılı giriş, kayıtlı hesaba yapılan başarısız giriş ve çıkış olayları kullanıcıya bağlı olarak olay kaydına yazılır ve kullanıcının Günlük şeridinde görünür; A ve B kullanıcıları için B'nin Günlük şeridinde A'nın hiçbir olayı görünmez; kayıtlı olmayan e-postayla deneme hiçbir kullanıcıya olay yazmaz.
- [ ] AC-16 — Giriş ve kayıt formları gönderilirken "gönderiliyor" durumu görünür ve gönder düğmesi pasiftir; çift tıklama iki hesap açmaz ve iki yanlış deneme saymaz; boş alanla gönderilen giriş formu alan hatası gösterir ve hak sayacını değiştirmez.
- [ ] AC-17 — Kayıt, giriş (başarılı ve başarısız) ve çıkış sırasında üretilen hiçbir log, hata mesajı ya da olay kaydı şifreyi ya da oturum belirtecini içermez; veritabanında şifre düz metin bulunmaz (kasıtlı bozma ile kanıtlanır; mekanizma 0008'de).
- [ ] AC-18 — Giriş ve kayıt formları yalnızca klavyeyle doldurulup gönderilebilir; her alanın görünür etiketi var; hata mesajları ekran okuyucuya duyurulur ve odak ilk hatalı alana gider; kullanıcı menüsü ve "Çıkış yap" klavyeyle kullanılabilir.
- [ ] AC-19 — Giriş ya da kayıt sırasında sunucuya ulaşılamazsa form "Şu an işleminizi yapamıyoruz" türünde, korelasyon numaralı genel bir hata gösterir; iç ayrıntı (stack trace, SQL) görünmez ve girilen ad/e-posta formda kalır.

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
