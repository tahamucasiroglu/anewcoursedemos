# Spec 0024 — Admin: kullanıcılar ve ban

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0024-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0010-admin-paneli.md` (eski 0010b; bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"); `specs/active/0008-gunluk-hata-bildirimi-loglama.md` eski AC-15'in ban kısmı (Ç-12)
- Sensitive area: H-1 Kimlik ve yetki (ban, token iptali, ajan bağlantısının düşürülmesi); H-3 Maliyet (ban anında kullanıcı adına online çağrıların durması)
- Depends on: 0001, 0002, 0011, 0012, 0021, 0022, 0006, 0014, 0010

## Intent
Admin, panelde tüm kullanıcıları listelemek ve gerektiğinde bir kullanıcıyı sistemden banlamak istiyor
(ilk_promt.txt: "kullanıcı listesi… kullanıcıları sistemden banlayabiliriz"). Başarı: admin kullanıcıyı
bulup banlayabiliyor; banlanan kullanıcı web'de de ajanda da anında sistem dışı kalıyor, adına arka planda
hiçbir iş ve para harcayan çağrı sürmüyor (BR-13, Ç-4); ban geri alınabiliyor ve her ban/ban kaldırma iz
bırakıyor. Admin kullanıcıların içeriğini görmez (BR-1). Bilinçli olarak yapılmayanlar: süreli ban,
kullanıcı silme, şifre sıfırlama, kullanıcı adına işlem, rol/yetki düzenleme, admin hesaplarını banlama,
kullanıcı verisinin silinmesi.

## Requirements
1. Panelin **Kullanıcılar** sekmesi (sekme sırasında İstatistikler'den sonra; panel iskeleti 0010)
   tüm kullanıcıları sayfa başına 25 kayıtla listeler. Sütunlar: ad, e-posta, kayıt tarihi, son giriş,
   durum (Aktif / Banlı, metinle), rol, kullanıcının ürettiği klip sayısı ("klip" tanımı 0010 Req 3 ile
   aynı).
2. Liste ad ya da e-postaya göre aranabilir: büyük/küçük harf duyarsız "içerir" araması, en az 2 karakter.
   Durum filtresi: Tümü / Aktif / Banlı (varsayılan Tümü).
3. Admin bir kullanıcıyı **banlayabilir**: satırdaki "Banla" eylemi onay penceresi açar; pencerede isteğe
   bağlı bir sebep alanı vardır (en fazla 500 karakter; aşılırsa onay düğmesi pasif ve "En fazla 500
   karakter" uyarısı). Sebebi yalnızca admin görür. Ban süresizdir. Var olmayan bir kullanıcıya ban isteği
   reddedilir. Zaten banlı kullanıcıya tekrar ban isteği idempotenttir (ikinci kayıt oluşmaz).
4. Ban anında etkili olur (BR-13):
   - kullanıcının web oturumları ve ajan oturumu dahil tüm token'ları iptal edilir; her istekte ban ve
     oturum geçerliliği kontrol edildiğinden süresi henüz dolmamış erişim token'ı da reddedilir;
   - ajanın canlı bağlantısı sunucu tarafından kapatılır; ajan yeniden bağlanamaz, yeni cihaz
     eşleştirmesi reddedilir (eşleştirme 0012);
   - kullanıcının video üretim işleri 0006 Req 16'nın çıkışta durdurma mekanizmasıyla durdurulur (Ç-4,
     iş durumları 0006): çalışan iş "Durduruldu" olur, tamamlanan aşamaları korunur; "Sırada" işler Sırada
     kalır ve sıra durur (kendiliğinden başlamaz);
   - kullanıcının süren konu önerisi / konsept üretim partileri 0014 Req 20'nin çıkışta durdurma
     mekanizmasıyla durdurulur ve başlamamış konseptler için çağrı yapılmaz;
   - kullanıcının hiçbir verisi silinmez;
   - kullanıcının (web ya da ajandan, ajanda bekletilmiş olanlar dahil) hata bildirimi reddedilir ve kayıt
     oluşmaz (0022'nin bildirim akışı; 0008 eski AC-15'ten taşındı).
5. Banlı kullanıcı doğru şifreyle giriş denediğinde giriş reddedilir ve "Hesabınız askıya alınmıştır."
   mesajını görür (sebep gösterilmez). Bu deneme 0002'deki 3 hak sayacını tüketmez.
6. Admin banlı bir kullanıcının **banını kaldırabilir** ("Banı kaldır", onay penceresiyle). Kullanıcı
   yeniden giriş yapabilir; eski oturumları geri gelmez, yeniden giriş ve gerekirse ajan eşleştirmesi
   gerekir. Durdurulmuş işleri 0006'daki gibi kaldığı yerden devam ettirilebilir.
7. Admin rolündeki hesaplarda (kendisi dahil) "Banla" eylemi yoktur: kendi satırında pasif ve ipucu
   "Kendinizi banlayamazsınız", başka bir admin satırında pasif ve ipucu "Admin hesapları banlanamaz".
   Aynı istek sunucuya doğrudan gönderilse de reddedilir.
8. Her ban ve ban kaldırma; yapan admin, zaman, hedef kullanıcı ve (ban için) sebeple kaydedilir ve
   kullanıcının satırından/detayından görülebilir. Aynı olaylar Aktiviteler'de "banlandı" / "ban
   kaldırıldı" türüyle görünür (0001/0021 olay altyapısına yazılır; içerik/sebep aktivitede görünmez).
9. 0010'un İstatistikler bölümüne **banlı kullanıcı sayısı** kartı eklenir; ban ve ban kaldırma bu sayıyı
   değiştirir.
10. **Banlı hesapta yanlış şifre.** Banlı hesaba yanlış şifreyle yapılan giriş denemesi 0002'nin normal akışını
    izler: 3 hak sayacı çalışır, 0002 Req 6'daki mesajlar gösterilir ve 3. yanlışta hesap 15 dakika kilitlenir;
    ban bilgisi (askıya alındı mesajı) yanlış şifrede hiç gösterilmez. Doğru şifreyle denemede Req 5 geçerlidir:
    "Hesabınız askıya alınmıştır." gösterilir, giriş olmaz ve sayaç değişmez. Hesap kilitliyken 0002 Req 7
    önce gelir (doğru şifrede de kilit mesajı); kilit dolduktan sonra doğru şifre Req 5'teki mesajı verir.

## User experience
- Flow: Admin paneli açar → Kullanıcılar sekmesine geçer → e-postayla arar ya da "Banlı" filtresini seçer →
  bir kullanıcının satırında "Banla" der → onay penceresinde isteğe bağlı sebebi yazar, onaylar → satırın
  durumu "Banlı" olur, ban kaydı görünür → aynı satırda "Banı kaldır" ile geri alabilir.
- States:
  - Kullanıcılar: yükleniyor · liste · yükleme hatası (Türkçe sade mesaj + korelasyon numarası + "Tekrar
    dene") · arama/filtre sonucu yok ("Eşleşen kullanıcı yok") · arama 2 karakterden kısa (uygulanmaz).
  - Ban: onay penceresi · sebep sınırı aşıldı ("En fazla 500 karakter", onay pasif) · işleniyor (onay
    düğmesi pasif) · başarılı · başarısız (hata mesajı, durum değişmez) · admin satırında eylem pasif +
    ipucu.
  - Ban kaldırma: onay penceresi · işleniyor · başarılı · başarısız (durum "Banlı" kalır, hata mesajı).
  - Banlı kullanıcının gördüğü: giriş ekranında doğru şifreyle "Hesabınız askıya alınmıştır." (yanlış şifrede
    0002'nin normal kalan hak / kilit mesajları, Req 10); açık sayfası varsa bir
    sonraki istekte oturumu düşer ve giriş sayfasına döner; ajan "oturum kapandı" durumuna geçer; işleri
    çalışan işi "Durduruldu", sıradaki işleri "Sırada" (sıra durmuş) görünür.
- Design reference: none — the flow above is the design. (Admin paneli tasarım görsellerinde yok; panel
  iskeleti 0010'dan.)
- Accessibility: tablo, arama, filtre, sayfalama, ban/ban kaldırma eylemleri ve onay pencereleri klavyeyle
  kullanılabilir (Tab, Enter/Space, Esc pencereyi kapatır, odak pencereye taşınır ve kapanınca geri döner);
  durum yalnızca renkle değil metinle de gösterilir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki; H-3 Maliyet (ban anında işlerin durması). Lite modda da netleştirme
  sorulur, her düzeltme turu yeniden review edilir ve AC-1, AC-4…AC-10 kasıtlı bozma ile kırmızıya
  döndürülerek kanıtlanır.
- İş durdurma mekanizmasının sahibi 0006'dır (video işleri, Req 16) ve 0014'tür (konsept/öneri partileri,
  Req 20); bu spec onları yeniden tanımlamaz, ban anında çağırır.
  Token iptali 0002'nin çıkış için kurduğu "tüm oturumları iptal et" yeteneğini yeniden kullanır.
- Veri kaynağı sınırı: veri Identity ve Production'ın açık sözleşmelerinden okunur (`docs/architecture.md`);
  admin konsept, video içeriği, profil, API key ve harcama ayrıntısını görmez (BR-1).
- Ban uçlarına rate limit uygulanmaz (yalnızca admin, LAN); tekrar gönderim idempotenttir.
- Out of scope: süreli ban · kullanıcı silme, veri silme · şifre sıfırlama · kullanıcı adına giriş ·
  rol/yetki atama ve düzenleme · admin hesaplarını banlama · banlı kullanıcıya sebep gösterme · kullanıcı
  başına harcama (BR-12) · admin'e bildirim · dışa aktarma (CSV) · canlı yenileme.
- Split: done — 0024 Kullanıcılar ve ban / kardeşler: 0010, 0025

## Acceptance criteria
- [ ] AC-1 — Kullanıcı listesi, ban ve ban kaldırma istekleri `admin.panel` yetkisi olmayan bir kullanıcının token'ıyla sunucuya doğrudan gönderildiğinde reddedilir; hiçbir veri dönmez ve hiçbir değişiklik olmaz; yetki kontrolü kaldırıldığında bu test kırmızıya döner.
- [ ] AC-2 — Kullanıcılar listesi ad, e-posta, kayıt tarihi, son giriş, durum (metinle), rol ve klip sayısını bilinen veri setiyle doğru gösterir; sayfa başına 25 kayıt, sayfalama tüm kullanıcılara erişim sağlar; ad/e-posta araması büyük/küçük harf duyarsız "içerir" ile eşleşenleri getirir, 2 karakterden kısa aramada uygulanmaz; durum filtresi (Tümü / Aktif / Banlı) doğru sonuç verir; eşleşme yoksa "Eşleşen kullanıcı yok" görünür.
- [ ] AC-3 — Kullanıcılar bölümü yüklenirken yükleniyor durumunu, sunucu hatasında Türkçe sade mesaj + korelasyon numarası + "Tekrar dene"yi gösterir; stack trace, SQL ya da gizli bilgi görünmez.
- [ ] AC-4 — Banla eylemi onay penceresi açar; vazgeçilince (Esc dahil) hiçbir şey değişmez; sebep boş bırakılabilir, 500 karakteri aşınca onay düğmesi pasif ve "En fazla 500 karakter" görünür; onaylanınca istek sürerken onay düğmesi pasif ve "işleniyor" görünür (çift tıklama tek ban üretir); başarılıysa satır "Banlı" olur ve ban kaydı (yapan admin, zaman, sebep) görünür; zaten banlı kullanıcıya tekrar ban isteği ikinci kayıt oluşturmaz; sunucu hatasında durum değişmez ve hata mesajı görünür; var olmayan bir kullanıcıya ban isteği reddedilir.
- [ ] AC-5 — Banlanan kullanıcının o anda açık web oturumu bir sonraki isteğinde geçersizdir ve giriş sayfasına döner; ban öncesi alınmış erişim (süresi henüz dolmamış olanı dahil) ve yenileme token'larının hiçbiri artık kabul edilmez.
- [ ] AC-6 — Banlanan kullanıcının bağlı ajanının canlı bağlantısı ban anından en geç 10 sn içinde sunucu tarafından kapatılır ve ajan "oturum kapandı" durumunu gösterir; ajan yeniden bağlanamaz ve yeni cihaz eşleştirmesi reddedilir.
- [ ] AC-7 — Ban anında, 0006'nın çıkışta durdurma kriteriyle (0006 AC-19) aynı şekilde, banlanan kullanıcının çalışan işi "Durduruldu" olur ve tamamlanan aşamaları korunur, "Sırada" işleri Sırada kalır ve kendiliğinden başlamaz; süren konu önerisi / konsept üretim partisi 0014 Req 20'deki gibi durdurulur ve başlamamış konseptler için çağrı yapılmaz; ban sonrasında kullanıcı adına yeni bir online sağlayıcı çağrısı yapılmaz; kullanıcının hiçbir verisi silinmez.
- [ ] AC-8 — Banlı kullanıcı doğru şifreyle giriş denediğinde giriş reddedilir ve "Hesabınız askıya alınmıştır." görünür (sebep görünmez); bu deneme 3 hak sayacını azaltmaz.
- [ ] AC-9 — Banlanmış kullanıcının web'den ya da ajandan (ajanda bekletilmiş olanlar dahil) gönderdiği hata bildirimi reddedilir ve kayıt oluşmaz. (0008 eski AC-15'in ban kısmı, Ç-12.)
- [ ] AC-10 — "Banı kaldır" onay penceresi açar, vazgeçilince durum değişmez; onaylanınca banı kaldırılan kullanıcı yeniden giriş yapabilir; ban öncesi token'ları geçersiz kalmaya devam eder; ban kaldırma kaydı (yapan admin, zaman) görünür; sunucu hatasında durum "Banlı" kalır ve hata mesajı görünür.
- [ ] AC-11 — Admin kendi satırında "Banla"yı kullanamaz (pasif, ipucu "Kendinizi banlayamazsınız"), başka bir admin satırında da kullanamaz (pasif, ipucu "Admin hesapları banlanamaz"); aynı istekler sunucuya doğrudan gönderilse de reddedilir ve hesap banlanmaz.
- [ ] AC-12 — Ban ve ban kaldırma, Aktiviteler'de ilgili kullanıcı için "banlandı" / "ban kaldırıldı" türüyle (sebep olmadan) görünür; İstatistikler'deki banlı kullanıcı sayısı bilinen veri setiyle doğrudur ve ban/ban kaldırma sonrası güncellenir; hiç banlı yoksa 0'dır.
- [ ] AC-13 — Kullanıcılar ekranında ve onu besleyen yanıtlarda hiçbir kullanıcının konsept başlığı/metni, prompt'u, profil içeriği ya da API key'i yer almaz (bilinen içerikli veri setinde arama ile kanıtlanır).
- [ ] AC-14 — Bu spec'teki tüm eylemler klavyeyle yapılabilir (Tab, Enter/Space, Esc onay penceresini kapatır, odak geri döner); durumlar metinle de gösterilir; her iki temada kontrast WCAG AA; tarih/saatler yerel saatle (Europe/Istanbul) gösterilir.
- [ ] AC-15 — Banlı hesaba yanlış şifreyle 1. denemede "Yanlış girdiniz, 2 hakkınız kaldı", 2. denemede "Yanlış girdiniz, 1 hakkınız kaldı" gösterilir, 3. denemede hesap 15 dakika kilitlenir ve "HH:mm'de açılacak" görünür (0002 AC-5 ile aynı); bu yanıtların hiçbirinde "askıya alınmıştır" geçmez. Kilitliyken doğru şifre kilit mesajını döndürür; kilit dolunca doğru şifre "Hesabınız askıya alınmıştır." gösterir, giriş olmaz ve sayaç değişmez (Req 10).

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
