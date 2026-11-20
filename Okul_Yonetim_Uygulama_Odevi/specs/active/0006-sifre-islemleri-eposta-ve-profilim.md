# Spec 0006 — Şifre işlemleri, e-posta ve Profilim

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0006-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu, 2026-11-20 — önceden verilen onay (Promtlar 0077: clarify cevapları = analistin
  önerileri, spec ve plan onayı önceden verildi; triage analist/geliştiricide, bilgi dışındaki bulgular düzeltilir; ship
  kullanıcıda)

## Intent
Okulda herkes (kurucu, öğretmen, veli, muhasebe) şifresini kendisi yönetebilmeli ve kendi ad-soyad ile telefonunu
düzeltebilmeli: bugün şifre yalnızca kurucunun sıfırlamasıyla değişiyor, iki okulda kayıtlı kişinin adını ise hiç kimse
düzeltemiyor (spec 0005, review tur 2). Başarı: kişi "Hesabım" ekranından şifresini ve profilini değiştirir; şifresini
unutan kişi e-postasına gelen tek kullanımlık bağlantıyla yeni şifre belirler; e-posta gönderimi SMTP sunucu bilgileri
girildiği an çalışacak şekilde hazırdır, bilgiler girilmeden hiçbir şey gönderilmez ve uygulama bozulmaz. Bilinçli olarak
yapılmayanlar: e-posta adresini değiştirme, iki adımlı doğrulama (B-17), SMS ile kurtarma, Pi'de SMTP bilgilerinin
girilmesi (kullanıcı ileride kendisi girer), mobil uygulamada derin bağlantı (yenileme bağlantısı web'de açılır).

Clarify kararları (önceden verilen onayla analistin önerileri — Promtlar 0078): C-1 şifre değişince diğer oturumlar
kapanır, bu oturum kalır · C-2 yanlış mevcut şifre giriş kilidi sayacına eklenir · C-3 "Şifremi unuttum" her durumda aynı
mesaj, e-posta başına 15 dk'da en fazla 3 bağlantı · C-4 bağlantı 30 dk, tek kullanımlık, yeni bağlantı eskileri
geçersiz kılar · C-5 bağlantı web'de açılır (mobilden de) · C-6 SMTP bilgisi yoksa gönderilmez, yalnızca "gönderilemedi"
logu; geliştirme ortamında e-posta yerel klasöre dosya olarak yazılır · C-7 Profilim herkese, web + mobil, e-posta
değişmez · C-8 giriş noktası Hoş geldin'de "Hesabım".

## Requirements
1. **Hesabım (web + mobil):** Giriş yapmış her kişi Hoş geldin ekranındaki "Hesabım" ile kendi hesap ekranına gider:
   profil bilgileri (ad-soyad, telefon düzenlenebilir; e-posta yalnızca gösterilir) ve "Şifremi değiştir" bölümü. Okul
   seçimi gerekmez; hesap kişiye aittir, bütün okullarında geçerlidir.
2. **Profil düzenleme:** Ad-soyad zorunlu, en fazla 100 karakter; telefon isteğe bağlı, spec 0005'teki Türkiye numarası
   kuralıyla (farklı yazımlar kabul, tek biçimde saklanır). Kaydedilince Hoş geldin ekranında ve kişinin bütün
   okullarındaki kurucu ekranlarında yeni bilgi görünür. Kişi yalnızca kendi profilini değiştirir; başkasının profilini
   değiştirmenin bir yolu yoktur. E-posta değiştirilemez (kimlik; ileride ayrı iş).
3. **Şifremi değiştir:** Mevcut şifre, yeni şifre ve yeni şifre (tekrar) istenir. Yeni şifre en az 8 karakter (spec
   0004 kuralı), tekrarıyla aynı ve mevcut şifreden farklı olmalı. Başarıda: yeni şifre geçerli olur, eski şifre
   reddedilir, kişinin **diğer** bütün açık oturumları hemen kapanır, bu cihazdaki oturum devam eder; "Şifreniz
   değiştirildi" gösterilir.
4. **Yanlış mevcut şifre:** "Mevcut şifre hatalı" gösterilir, hiçbir şey değişmez. Yanlış mevcut şifre denemeleri giriş
   kilidiyle aynı sayaca eklenir (spec 0004: art arda 3 hata → o e-postayla 15 dk kilit); kilitliyken şifre değiştirme de
   "Çok fazla hatalı deneme, 15 dakika sonra tekrar deneyin" ile reddedilir. Doğru mevcut şifreyle başarılı değişiklik
   sayacı sıfırlar.
5. **Şifremi unuttum — istek:** Giriş ekranında (web + mobil) "Şifremi unuttum" bağlantısı; kişi e-postasını yazar.
   Sistemde kayıtlı olsun olmasın **her durumda aynı** mesaj: "Bu e-posta sistemde kayıtlıysa şifre yenileme
   bağlantısı gönderildi. Gelen kutunuzu (ve istenmeyen klasörünü) kontrol edin." Kayıtlı e-postaya yenileme bağlantısı
   içeren bir e-posta gönderilir; kayıtsız e-postaya hiçbir şey gönderilmez. Yanıtın biçimi ve süresi iki durumda
   ayırt edilemez. Aynı e-posta için 15 dakikada en fazla 3 bağlantı gönderilir; fazlasında yine aynı mesaj gösterilir
   ama e-posta gönderilmez. Genel istek sınırı (IP başına dakikada 20) giriş ile aynıdır.
6. **Yenileme bağlantısı:** 30 dakika geçerli, **tek kullanımlık**; aynı kişi için yeni bağlantı istenince eskileri
   geçersiz olur; şifre (herhangi bir yolla — değiştirme, kurucu sıfırlaması, bağlantı) değişince açıkta kalan
   bağlantılar geçersiz olur. Bağlantı tahmin edilemez; sistemde yalnızca tek yönlü özeti saklanır. Geçersiz, süresi
   dolmuş ya da kullanılmış bağlantıda "Bu bağlantı geçersiz ya da süresi dolmuş" ve "Yeni bağlantı iste" gösterilir.
7. **Şifremi unuttum — yeni şifre:** Bağlantı web'de "Yeni şifre belirle" sayfasını açar (mobilde de tarayıcıda açılır):
   yeni şifre + tekrar, kural madde 3'teki gibi. Başarıda: şifre değişir, kişinin **bütün** açık oturumları kapanır,
   giriş kilidi ve sayacı sıfırlanır, bağlantı kullanılmış olur; "Şifreniz yenilendi, yeni şifrenizle giriş
   yapabilirsiniz" ve Giriş'e git. Kişi pasif ya da hiçbir okulda etkin değilse de şifre yenilenir (hesap kişiye ait),
   girişte okul listesi spec 0005 kurallarıyla gelir.
8. **E-posta gönderimi (SMTP'ye hazır):** Sistem e-postayı ayarlardaki SMTP sunucusu (adres, port, şifreli bağlantı,
   kullanıcı adı, şifre, gönderen adı ve adresi) ve web adresi (bağlantının başı) ile gönderir. Bilgiler girilmemişse
   e-posta gönderilmez, kullanıcıya yine madde 5'teki mesaj gösterilir ve log'a yalnızca "e-posta gönderilemedi: SMTP
   ayarlı değil" (kişi kimliğiyle, e-posta adresi ve bağlantı olmadan) yazılır. Sunucuya ulaşılamazsa ya da sunucu
   reddederse de kullanıcıya aynı mesaj, log'a uyarı; istek çökmez, uzun süre beklemez. E-posta Türkçedir: konu
   "Okul Yönetim — şifre yenileme", gövdede kişinin adı, bağlantı, "30 dakika geçerlidir", "Bu isteği siz yapmadıysanız
   bu e-postayı yok sayın; şifreniz değişmez." Geliştirme ortamında (yalnızca yerel geliştirme) e-postalar SMTP yerine
   yerel bir klasöre dosya olarak yazılır; Pi'de ve diğer ortamlarda bu yapılmaz. SMTP şifresi repoda, logda ve
   yanıtlarda bulunmaz.
9. **Gizlilik:** Şifreler, yenileme bağlantısı ve SMTP şifresi hiçbir logda, hata yanıtında ve audit kaydında geçmez;
   "Şifremi unuttum" isteği hesabın var olup olmadığını ele vermez.
10. **Audit kaydı:** Profil değiştirme, şifre değiştirme (başarılı ve yanlış mevcut şifre), yenileme bağlantısı isteği
    (kayıtlı kişi için; kayıtsız e-posta için denenen e-postayla), bağlantıyla şifre yenileme ve geçersiz bağlantı
    denemesi audit'e yazılır (kim, ne, ne zaman, IP).

## User experience
- **Flow (web + mobil):**
  - Hoş geldin → "Hesabım" → Profil (ad-soyad, telefon, e-posta salt okunur → Kaydet) ve Şifremi değiştir (mevcut,
    yeni, tekrar → Şifreyi değiştir) → "Hoş geldin'e dön".
  - Giriş → "Şifremi unuttum" → e-posta → Gönder → aynı bilgi mesajı → "Girişe dön".
  - E-postadaki bağlantı → (web) Yeni şifre belirle (yeni, tekrar) → başarı → Giriş.
- **States:**
  - Hesabım: yükleniyor; profil kaydediliyor (düğme kilitli) / kaydedildi / alan hataları / sunucu hatası;
    şifre değiştiriliyor (düğme kilitli) / değiştirildi / mevcut şifre hatalı / kilitli / alan hataları.
  - Şifremi unuttum: alan hatası (boş/geçersiz e-posta, istek gönderilmez) / gönderiliyor / bilgi mesajı (her durumda
    aynı) / sunucuya ulaşılamıyor + tekrar dene.
  - Yeni şifre belirle: bağlantı denetleniyor / geçerli → form / geçersiz-süresi dolmuş + "Yeni bağlantı iste" /
    alan hataları / kaydediliyor / başarı + "Girişe git".
  - Girişsiz kişi "Hesabım" adresine gelirse Giriş'e yönlenir.
- **Design reference:** none — the flow above is the design (spec 0004/0005 ekran kalıbı, ortak tema).
- **Accessibility:** akışlar yalnızca klavyeyle tamamlanır; alanların görünür etiketleri; şifre alanları
  `autocomplete` ile (mevcut / yeni şifre) parola yöneticisine uygun; hatalar alanla ilişkili ve ekran okuyucuya
  duyurulur; başarı mesajları duyurulur; açık ve koyu temada kontrast spec 0002 eşiklerinde; 320 px'te yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S2 (ad-soyad, telefon, e-posta), S3 (şifre, yenileme bağlantısı, oturumlar, SMTP şifresi), S6
  (audit kaydı).
- E-posta adresi değiştirme yok (ileride ayrı iş). İki adımlı doğrulama yok (B-17). SMS ile kurtarma yok.
- Pi'de SMTP bilgileri girilmez; Pi dağıtım ayarı yalnızca boş/eksik ayarla güvenle çalışacak şekilde kalır (kullanıcı
  ileride girer). Pi'ye bu spec'te dağıtım yapılmaz (ship kullanıcıda).
- Mobil uygulamada derin bağlantı yok; bağlantı web'de açılır.
- Kurucunun geçici şifresinden sonra ilk girişte şifre değiştirme zorunluluğu yok (spec 0005 kararı; değişmez).
- Çok bilinen şifre listesi yok (B-17 ile pilot öncesi).
- Split: proposed 0006a "Şifremi değiştir + Profilim" ve 0006b "Şifremi unuttum + e-posta"; kept as one —
  tahamucasiroglu, 2026-11-20 (Promtlar 0076/0077: yol haritası 1b-2 + 1b-3 tek spec, kullanıcı uzakta).

## Acceptance criteria
- [ ] AC-1 — Profil: giriş yapmış kişi Hesabım'da ad-soyad ve telefonunu değiştirir; telefon farklı yazımlarla kabul edilip tek biçimde saklanır; Hoş geldin ve kişinin her okulundaki kurucu kartı yeni bilgiyi gösterir; e-posta değiştirilemez (istekte gönderilse bile değişmez); boş/101 karakter ad ya da geçersiz telefon alan hatası verir, istek gönderilmez ve hiçbir şey değişmez.
- [ ] AC-2 — Profil yetkisi: kişi yalnızca kendi profilini değiştirir (başka kişinin kimliğiyle değiştirmenin yolu yok); girişsiz istek reddedilir; okul seçimi beklenen (iki okullu, okul seçmemiş) kişi de kendi profilini ve şifresini değiştirebilir.
- [ ] AC-3 — Şifremi değiştir: doğru mevcut şifre + kurala uyan yeni şifreyle şifre değişir; eski şifreyle giriş reddedilir, yenisiyle kabul edilir; kişinin diğer açık oturumları hemen reddedilir, bu oturum çalışmaya devam eder; "Şifreniz değiştirildi" gösterilir.
- [ ] AC-4 — Şifremi değiştir hataları: yanlış mevcut şifre "Mevcut şifre hatalı" verir ve hiçbir şey değişmez; art arda 3 yanlış mevcut şifre (girişteki hatalarla aynı sayaç) o e-postayı 15 dk kilitler, kilitliyken doğru mevcut şifre de reddedilir; yeni şifre 8 karakterden kısa, tekrarıyla farklı ya da mevcut şifreyle aynıysa alan hatası verir ve hiçbir şey değişmez.
- [ ] AC-5 — Şifremi unuttum isteği: kayıtlı ve kayıtsız e-posta için yanıt metni, durum kodu ve biçimi aynıdır; kayıtlı e-postaya tek bir yenileme e-postası gider (adı, bağlantı, "30 dakika geçerlidir", "siz yapmadıysanız yok sayın"), kayıtsıza hiçbir şey gitmez; e-posta büyük/küçük harf ve boşluktan bağımsız eşleşir; boş/geçersiz e-postada alan hatası, istek gönderilmez.
- [ ] AC-6 — İstek sınırı: aynı e-posta için 15 dakika içinde 4. istek aynı mesajı verir ama e-posta göndermez; 15 dakika sonra yeniden gönderilir; IP başına genel sınır aşılınca "Çok fazla istek" döner.
- [ ] AC-7 — Bağlantı geçerliliği: bağlantı 30 dakika geçerli ve tek kullanımlıktır; süresi dolmuş, bir kez kullanılmış, sonradan istenen yeni bağlantıyla geçersizleşmiş, şifre başka yolla (değiştirme ya da kurucu sıfırlaması) değiştikten sonra kalan ve uydurma bağlantı "Bu bağlantı geçersiz ya da süresi dolmuş" verir ve şifre değişmez; veritabanında bağlantının kendisi değil yalnızca özeti bulunur.
- [ ] AC-8 — Bağlantıyla yenileme: geçerli bağlantı + kurala uyan yeni şifre ile şifre değişir; kişinin bütün açık oturumları hemen reddedilir; kilitli hesabın kilidi ve hata sayacı sıfırlanır; eski şifre reddedilir, yenisi kabul edilir; aynı bağlantı ikinci kez kullanılamaz; kurala uymayan yeni şifrede alan hatası verir ve bağlantı kullanılmış sayılmaz.
- [ ] AC-9 — SMTP ayarlı değilken: "Şifremi unuttum" aynı mesajı verir, e-posta gönderilmez, istek hata vermez; log'a "e-posta gönderilemedi: SMTP ayarlı değil" kişi kimliğiyle yazılır, e-posta adresi ve bağlantı yazılmaz. SMTP ayarlıyken e-posta ayarlardaki sunucuya, gönderen bilgisiyle, Türkçe konu ve gövdeyle ve ayarlardaki web adresiyle başlayan bağlantıyla gider; sunucuya ulaşılamaz ya da sunucu reddederse kullanıcı aynı mesajı alır, log'a uyarı düşer, istek makul sürede (en fazla ~10 sn) biter.
- [ ] AC-10 — Geliştirme e-postası: yalnızca geliştirme ortamında e-posta yerel klasöre dosya olarak yazılır; geliştirme dışı ortamda bu klasöre hiçbir şey yazılmaz.
- [ ] AC-11 — Gizlilik: şifreler, yenileme bağlantısı (ve içindeki anahtar) ve SMTP şifresi hiçbir logda, hata yanıtında ve audit kaydında geçmez; SMTP şifresi repoda yoktur.
- [ ] AC-12 — Audit: profil değiştirme, şifre değiştirme (başarılı ve yanlış mevcut şifre), yenileme bağlantısı isteği (kayıtlı kişi; kayıtsız e-posta denenen e-postayla), bağlantıyla yenileme ve geçersiz bağlantı denemesi audit kaydı oluşturur (kim / denenen e-posta, ne, ne zaman, IP).
- [ ] AC-13 — Ekran durumları (web ve mobil, iki tema): Hesabım (yükleniyor, kaydediliyor/düğme kilitli, kaydedildi, alan hataları, sunucu hatası, şifre değiştirildi, mevcut şifre hatalı, kilitli), Şifremi unuttum (alan hatası, gönderiliyor, bilgi mesajı, ulaşılamıyor + tekrar dene), web Yeni şifre belirle (geçersiz bağlantı + yeni bağlantı iste, alan hataları, kaydediliyor, başarı + girişe git); girişsiz kişi Hesabım'da Giriş'e yönlenir; web'de akış yalnızca klavyeyle tamamlanır, hatalar ve başarı ekran okuyucuya duyurulur, şifre alanlarında doğru `autocomplete`; 320 px'te yatay kayma yok.

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
