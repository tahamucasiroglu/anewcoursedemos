# Spec 0012 — Ajan cihaz eşleştirme ve ajan sahipliği

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0012-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0002-kimlik.md` (Req 14–15'in ajan kısmı, Req 19–24, AC-13'ün ajan kısmı, AC-16–21, AC-22–24'ün eşleştirme kısmı) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-1 Kimlik ve yetki (H-2'ye eşleştirme onay sırrı ve ajan oturum belirtecinin hiç loglanmaması yönüyle değer: `docs/security.md`)
- Depends on: 0001, 0002, 0008

## Intent
Masaüstü ajanı şifre sormadan, ekranda gösterdiği bir kodun web'den onaylanmasıyla kullanıcının hesabına
bağlanır (ilk_promt.txt: "masaüstü uygulaması da web'deki bir login'i takip edecek"). Her kullanıcı
yalnızca kendi ajanını görür ve yalnızca kendi ajanına komut gönderebilir (BR-1); web'den çıkış ajanı da
düşürür (BR-14). Başarı: eşleştirilmemiş ajan sunucuya bağlanamıyor; iki kullanıcı birbirinin ajanını
göremiyor; çıkış ajanın bağlantısını en geç 10 sn içinde kapatıyor. Bilinçli olarak yapılmayanlar: bir
kullanıcıya birden çok ajan, ajan kurulum ekranı ve "Ajanı indir" (0009), ban'ın ajana etkisi (0024).

## Requirements
**Eşleştirme kodu**
1. Eşleştirilmemiş ajan açıldığında bir eşleştirme kodu gösterir ve kodun ne zamana kadar geçerli olduğunu
   geri sayımla belirtir. Kod **8 karakter, `ABCD-1234` biçiminde**, karışan karakterler (O/0, I/1) hariç,
   **10 dakika geçerli ve tek kullanımlık**tır. Ajana şifre yazdırılmaz (`docs/security.md`).
2. Ajan penceresindeki "Siteye git" düğmesi web'deki "Ajanı eşleştir" sayfasını açar ama kodu adreste
   taşımaz; kullanıcı kodu elle yazar (kodun tarayıcı geçmişine düşmemesi için).

**Web eşleştirme**
3. "Ajanı eşleştir" ayrı bir sayfadır; giriş ister ve ajan bağlı değilken sistem durumundaki "Ajanı
   eşleştir" bağlantısından açılır. Kullanıcı kodu girer; sayfa kodun ait olduğu cihazı **makine adı +
   ajan sürümü** olarak gösterir; kullanıcı "Onayla" ya da "Reddet" der (yabancı bir cihaz görürse reddeder).
4. Onaylanınca ajan, kodu onaylayan kullanıcının hesabına bağlanır (kodu kim onaylarsa ajan onundur),
   kendi oturumunu alır, sunucuya bağlanır ve eşleştiğini gösterir; sitede "Ajan bağlı" görünür.
5. Reddedilen, süresi dolmuş, daha önce kullanılmış ya da var olmayan kod onaylanamaz ve web'de Türkçe hata
   verir; ajan reddedildiğini ya da kodun süresinin dolduğunu gösterir ve yeni kod alabilir.
6. Kullanıcı başına **tek eşleşmiş ajan** vardır: yeni bir eşleştirme önceki ajanın oturumunu iptal eder;
   önceki ajan "Oturum kapandı" gösterir ve eski oturumuyla bağlanamaz. Eski ajanın oturumu düşerken o ajanda
   çalışan yerel bir üretim aşaması varsa iş, ajan kopmasıyla aynı kurala göre *Ajan bekleniyor* olur ve yeni
   ajan bağlanınca kaldığı aşamadan sürer; bu davranış ve kanıtı 0017'dedir (Req 15, ajan kopması). Bu spec'te
   üretim işi yoktur.
7. Eşleştirme formu gönderilirken "gönderiliyor" durumu görünür ve düğmeler pasiftir; çift tıklama iki
   kez onaylamaz.
8. Aynı IP adresinden dakikada 10'dan fazla kod onayı (kaba kuvvetle kod tahmini) ya da kod alma isteği
   gelirse sonraki istekler 1 dakika boyunca reddedilir ve "Çok fazla deneme, biraz sonra tekrar deneyin"
   gösterilir (0002 Req 9 ile aynı sınır).

**Ajan oturumu ve bağlantı**
9. Ajan oturumu son kullanımdan itibaren **90 gün** geçerlidir ve ajan çalıştıkça yenilenir; yalnızca
   çıkış, yeni eşleştirme (Req 6), ban (0024) ya da 90 gün kullanılmama ile düşer. Eşleşmiş ajan yeniden
   başlatılınca yeniden eşleştirme istemez.
10. 0002 Req 11'deki geçici "kimliksiz ajan bağlantısı" istisnası kaldırılır. Giriş istemeyen ajan
    işlemleri yalnızca: eşleştirilmemiş ajanın eşleştirme kodu alması ve kodunun onay durumunu öğrenmesi.
    Eşleştirilmemiş ya da oturumu iptal edilmiş bir ajan sunucuya bağlanamaz ve komut alamaz
    (`docs/security.md`: ajan yalnızca kimliği doğrulanmış bağlantıdan gelen komutları çalıştırır).
11. Ajan sunucuya ulaşamazken açılırsa "Sunucuya ulaşılamıyor, yeniden deneniyor" gösterir ve 0001'deki
    yeniden bağlanma kuralıyla kendiliğinden dener; sunucu erişilebilir olunca kod gösterir (eşleşmemişse)
    ya da bağlanır (eşleşmişse).

**Çıkış (BR-14)**
12. Web'den "Çıkış yap" (0002 Req 15'teki "tüm oturumları iptal et" yeteneği) eşleştirilmiş ajanın
    oturumunu da anında geçersiz kılar; ajanın canlı bağlantısı sunucu tarafından kapatılır. Ajan oturumunun
    kapandığını gösterir ve yeniden eşleştirme ister; eski oturumuyla yeniden bağlanamaz. Bedeli bilerek
    kabul edilir: her web çıkışı ajanın yeniden eşleştirilmesini gerektirir.

**Sahiplik ve olaylar (BR-1)**
13. Ajan ve ajan olayları sahibini taşır ve yalnızca sahibine görünür: kullanıcı sistem durumunda yalnızca
    kendi ajanının bağlı olup olmadığını görür, "Ajanı test et" yalnızca kendi ajanına ulaşır, Günlük
    şeridinde yalnızca kendi ajan olaylarını görür.
14. Eşleştirme olayı (onaylandı; cihaz adıyla) kullanıcıya bağlı olarak 0001'in olay altyapısına yazılır;
    kod, onay sırrı ve ajan oturum belirteci içermez. Bu olay Günlük şeridinde görünür ve admin "aktivite"
    verisinin kaynağıdır (gösterimi 0010'da).

**Tray'de oturum durumu**
15. Ajanın tray menüsünün en başında tıklanamayan bir oturum durumu satırı bulunur; ajan penceresindeki
    oturum durumunu gösterir: "Eşleşmedi" · "Eşleşti" · "Oturum kapandı" · "Sunucuya ulaşılamıyor". Satır
    durum değişince güncellenir; tray'in 0001'deki öğeleri ("Siteye git", "Çıkış") değişmez. (Sonradan
    eklenen "Kurulum ve bileşenler" öğesiyle birlikte çalışmanın kanıtı 0009'dadır.)

## User experience
- Flow: Kullanıcı PC'de ajanı başlatır → ajan penceresi bir eşleştirme kodu ve geri sayımı gösterir →
  kullanıcı "Siteye git"e basar ya da sistem durumundaki "Ajanı eşleştir" bağlantısını açar → kodu elle
  girer, cihaz adını (makine adı + ajan sürümü) görür, "Onayla" der → ajan "Eşleşti" gösterir, sitede
  "Ajan bağlı" görünür. Web'den çıkınca ajan "Oturum kapandı" gösterir ve yeni kod ister.
- States:
  - Web "Ajanı eşleştir" sayfası: boş kod alanı · gönderiliyor · cihaz bilgisi ve Onayla/Reddet ·
    onaylandı · reddedildi · kod geçersiz/süresi dolmuş/kullanılmış · çok fazla deneme.
  - Sistem durumu: "Ajan bağlı" · "Ajan bağlı değil" + "Ajanı eşleştir" bağlantısı.
  - Ajan penceresi: kod gösteriliyor (geri sayımla) · kod süresi doldu (yeni kod al) · reddedildi ·
    eşleşti · oturum kapandı (çıkış ya da yeni eşleştirme; ban kaynaklısı 0024'te; yeniden eşleştir) ·
    sunucuya ulaşılamıyor (yeniden deneniyor).
  - Tray menüsü: en başta oturum durumu satırı (Eşleşmedi · Eşleşti · Oturum kapandı · Sunucuya ulaşılamıyor).
- Design reference: none — the flow above is the design. Eşleştirme sayfası ve ajan penceresi için tasarım
  yok. Renk ve tema 0001'deki tema değişkenlerinden gelir (turuncu/kahve, açık/koyu).
- Accessibility: kod alanının görünür etiketi var; Tab sırası alan sırasını izler, Enter formu gönderir;
  hata mesajları alanla ilişkilendirilir ve ekran okuyucuya duyurulur; hata sonrası odak kod alanına
  gider; ajan penceresindeki kod büyük ve okunaklı yazılır, seçilip kopyalanabilir; kontrast her iki
  temada WCAG AA.

## Constraints & out of scope
- Erişim: yalnızca yerel ağ, HTTP (B-3). Arayüz metinleri yalnızca Türkçe.
- Eşleştirme onay sırrı ve ajan oturum belirteci hiçbir logda, hata mesajında ya da Günlük şeridinde
  görünmez (maskeleme mekanizması 0008'de; bu spec yalnızca kendi alanlarını kanıtlar, AC-13).
- "En geç 10 sn" sınırları 0001'deki ajan durum sınırıyla aynıdır.
- Out of scope: bir kullanıcıya birden çok ajan (somut istek gelince), eşleştirmenin Ayarlar sayfasına
  taşınması, ajan kurulum ekranı ve "Ajanı indir" (0009), ajan güncelleme ve kaldırma (0023), ban'ın
  ajana etkisi (0024), çıkışta üretim işlerinin durdurulması (0006), yeniden eşleştirmede çalışan yerel işin
  *Ajan bekleniyor* olması ve devamı (0017).
- Split: done — 0012 Ajan cihaz eşleştirme ve ajan sahipliği / kardeşler: 0002, 0011

## Acceptance criteria
- [ ] AC-1 — Eşleştirilmemiş ajan açılınca `ABCD-1234` biçiminde, O/0/I/1 içermeyen 8 karakterlik bir kod ve 10 dakikalık geri sayım gösterir; kullanıcı web'de "Ajanı eşleştir" sayfasına kodu girince makine adı + ajan sürümü görünür; "Onayla" sonrası sayfa "Onaylandı" gösterir, ajan "Eşleşti" gösterir ve sitede en geç 10 sn içinde "Ajan bağlı" görünür.
- [ ] AC-2 — Ajan bağlı değilken sistem durumunda "Ajanı eşleştir" bağlantısı görünür ve eşleştirme sayfasını açar; ajan penceresindeki "Siteye git" aynı sayfayı açar ve açılan adres kodu içermez.
- [ ] AC-3 — Eşleşmiş ajan kapatılıp yeniden açılınca eşleştirme istemeden aynı hesaba bağlanır; 90 günden uzun süre kullanılmamış ajan oturumu reddedilir ve ajan yeniden eşleştirme ister; 90 gün dolmadan kullanılan ajan oturumu yenilenir (saat ilerletilerek test edilir).
- [ ] AC-4 — "Reddet" sonrası sayfa "Reddedildi" gösterir, ajan reddedildiğini gösterir ve bağlanmaz; süresi dolmuş (10 dk sonra), daha önce kullanılmış ya da var olmayan kod web'de onaylanamaz ve Türkçe hata gösterir; ajan süresi dolan kod için yeni kod alabilir.
- [ ] AC-5 — Eşleştirilmemiş ya da oturumu iptal edilmiş ajanın sunucuya bağlanma denemesi reddedilir ve bu ajana hiçbir komut ulaşmaz; giriş istemeyen ajan işlemleri yalnızca kod alma ve kod durumunu öğrenmedir.
- [ ] AC-6 — A ve B kullanıcıları için: A'nın ajanı bağlıyken B'nin sistem durumunda "Ajan bağlı değil" görünür; B'nin "Ajanı test et" eylemi A'nın ajanına ulaşmaz ("Ajan bağlı değil" döner); B'nin Günlük şeridinde A'nın ajan olayları görünmez; B, A'nın onaylanmamış kodunu kendi hesabıyla onaylarsa ajan B'ye bağlanır, A'ya değil.
- [ ] AC-7 — Eşleşmiş bir ajanı olan kullanıcı ikinci bir ajanı eşleştirince önceki ajan en geç 10 sn içinde "Oturum kapandı" gösterir ve eski oturumuyla yeniden bağlanamaz; sitede yalnızca yeni ajan bağlı görünür.
- [ ] AC-8 — Eşleştirilmiş ajanı bağlı kullanıcı web'den "Çıkış yap" dediğinde ajan bağlantısı en geç 10 sn içinde kapanır, ajan "Oturum kapandı" gösterir ve yeni kod ister; ajan eski oturumuyla yeniden bağlanamaz.
- [ ] AC-9 — Ajan sunucuya ulaşamazken açılırsa "Sunucuya ulaşılamıyor, yeniden deneniyor" gösterir ve kendiliğinden yeniden dener; sunucu erişilebilir olunca kod gösterir (eşleşmemişse) ya da bağlanır (eşleşmişse).
- [ ] AC-10 — Eşleştirme formu gönderilirken "gönderiliyor" durumu görünür ve düğmeler pasiftir; çift tıklama kodu iki kez onaylamaz ve tek eşleştirme olayı yazılır.
- [ ] AC-11 — Aynı IP'den bir dakika içinde 11. kod onayı ya da kod alma isteği reddedilir ve "Çok fazla deneme" gösterilir; 1 dakika sonra yeniden mümkün olur.
- [ ] AC-12 — Onaylanan eşleştirme, cihaz adıyla birlikte kullanıcıya bağlı bir olay olarak olay kaydına yazılır ve yalnızca o kullanıcının Günlük şeridinde görünür.
- [ ] AC-13 — Eşleştirme, ajan bağlantısı ve çıkışta ajan düşürme sırasında üretilen hiçbir log, hata mesajı ya da olay kaydı eşleştirme onay sırrını ya da ajan oturum belirtecini içermez (kasıtlı bozma ile kanıtlanır; mekanizma 0008'de).
- [ ] AC-14 — Eşleştirme formu yalnızca klavyeyle doldurulup gönderilebilir; kod alanının görünür etiketi var; hata mesajları ekran okuyucuya duyurulur ve odak kod alanına gider; ajan penceresindeki kod seçilip kopyalanabilir.
- [ ] AC-15 — Tray menüsünün en başında tıklanamayan oturum durumu satırı vardır: eşleşmemiş ajanda "Eşleşmedi", onaydan sonra "Eşleşti", web'den çıkış ya da yeni eşleştirmeden sonra en geç 10 sn içinde "Oturum kapandı", sunucuya ulaşılamazken "Sunucuya ulaşılamıyor" gösterir; 0001'in "Siteye git" ve "Çıkış" öğeleri değişmeden çalışır.

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
