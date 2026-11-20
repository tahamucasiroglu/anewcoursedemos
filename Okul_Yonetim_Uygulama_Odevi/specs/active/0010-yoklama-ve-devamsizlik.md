# Spec 0010 — Yoklama ve devamsızlık

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0010-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu, 2026-11-20 — önceden verilen onay (Promtlar 0082: clarify cevapları = analistin
  önerileri, spec ve plan onayı önceden verildi; triage analist/geliştiricide, bilgi dışındaki bulgular düzeltilir; ship
  ve Pi yayını kullanıcıda)

## Intent
Öğretmen bugün yoklamayı kâğıtla ya da e-Okul'da alıyor; veli çocuğunun okula gelmediğini çoğu zaman akşam öğreniyor.
Bu spec öğretmene telefondan (ve web'den) **10 saniyede yoklama** verir: kendi sınıfını açar, herkes "Var" gelir,
yalnızca gelmeyenlere dokunur ve kaydeder. "Yok" işaretlenen öğrencinin her velisine **otomatik bildirim** gider:
uygulamada (mobil + web) bildirim ve okulun e-posta kuyruğundan bir e-posta (SMTP bilgisi girilene kadar yalnızca
log). Başarı: 40 kişilik sınıfta 3 devamsızlı yoklama 5 dokunuşla alınır, veli uygulamayı açınca "Ali bugün okula
gelmedi" bildirimini görür. Bilinçli olarak yapılmayanlar: ders saati bazlı yoklama (ders programı gelince), mazeretli
/ izinli devamsızlık, push bildirim (B-6), SMS (B-2), devamsızlık raporları ve yönetici özeti (yol haritası satır 11),
veliye çocuğun devamsızlık geçmişi ekranı (haftalık gelişim özeti, satır 8).

**Bağımlılık:** spec 0008 (sınıf şubeye bağlı, sınıf seviyesi, dönem, akademik takvim) ve spec 0009 (öğrenci kartı,
sınıfa kayıt, birden çok veli; veli = Veli rolündeki kişi, öğretmen = şubede Öğretmen rolündeki kişi) bu spec'ten
önce yapılır. Buradaki sınıf, öğrenci, veli, dönem ve tatil kavramları `docs/domain.md`'deki anlamlarıyladır; adlar
0008/0009 bitince plan değişikliğiyle (amendment) eşlenir.

### Clarify kararları
Hepsi analistin önerisi, kullanıcının önceden verdiği onayla alındı.

**C-1 — Yoklama birimi: gün mü, ders saati mi?** → **Günlük yoklama**: bir sınıf için günde bir yoklama.
Neden: ders programı henüz yok (yol haritası satır 2'de "sonra"); günlük yoklama tek ekranda 10 saniye hedefini
tutturur. Ders saati bazlı yoklama ders programı gelince ayrı spec.
Dokümanda istenen: "Öğretmen mobilden 10 saniyede yoklama alır" (kapsam, MVP modülü 3); `docs/domain.md`: "Yoklama
(`Attendance`) — Ders/gün bazlı devam kaydı".
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-2 — Hangi durumlar?** → **Var / Yok / Geç**; liste açılınca herkes "Var"; yalnızca "Yok" veliye bildirim
üretir. Mazeretli/izinli sonraki adım.
Neden: varsayılan "Var" ile öğretmen yalnızca gelmeyenlere dokunur (10 saniye); "Geç" okulların günlük ihtiyacı,
bildirim gerektirmez.
Dokümanda istenen: "Devamsız öğrencinin velisine otomatik bildirim" (kapsam, MVP modülü 3).
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-3 — Öğretmen hangi sınıfın yoklamasını alır?** → **Sınıf öğretmeni ataması**: kurucu web'deki yönetim sayfasında
bir sınıfa, o sınıfın şubesinde Öğretmen rolü olan bir ya da birkaç kişiyi atar; öğretmen yalnızca atandığı sınıfları
görür. 0008 aynı bağı (ör. "sınıf öğretmeni") zaten tanımladıysa yenisi açılmaz, o kullanılır (plan değişikliği).
Neden: güvenlik kuralı "öğretmen yalnızca kendi sınıflarını görür" (`docs/security.md`, üç katmanlı yetki); ders
programı olmadan en küçük bağ budur.
Dokümanda istenen: "Rol ve yetki yönetimi, şube bazlı görünürlük" (kapsam, platform altyapısı); rol tablosu
"Öğretmen | Yoklama, not, ödev, veli mesajı | Mobil + Web".
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-4 — Bildirim hangi kanaldan?** → **Uygulama içi bildirim** (veli mobilde ve web'de görür, okunmamış sayısıyla)
**+ e-posta** (spec 0006'nın e-posta kuyruğu; SMTP ayarlı değilse yalnızca "SMTP ayarlı değil" logu). Push bildirim
sağlayıcı gelince (B-6), SMS hesap açılınca (B-2).
Neden: push sağlayıcı yok; uygulama içi bildirim yeni altyapı istemez, e-posta kuyruğu zaten var (tek ek çağrı) ve
SMTP girildiği gün veliye gerçekten ulaşır.
Dokümanda istenen: "Devamsız öğrencinin velisine otomatik bildirim"; "Push bildirim, kurum logosuyla markalı açılış"
(kapsam, MVP modülü 9 — push burada değil, B-6).
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-5 — Bildirim ne zaman gider, düzeltmede ne olur?** → Yoklama **kaydedilince hemen**; aynı öğrenci için aynı gün
ikinci "yok" bildirimi gitmez. Öğretmen aynı gün "Yok"u "Var"/"Geç"e çevirirse veliye **düzeltme bildirimi** (uygulama
içi + e-posta) gider ve ilk bildirim "düzeltildi" görünür; yeniden "Yok" yapılırsa yeni "yok" bildirimi gider.
Neden: bekleme süresi (ör. 15 dk) karmaşıklık katar; yanlış işaretin zararı düzeltme bildirimiyle kapanır.
Dokümanda istenen: "Devamsız öğrencinin velisine otomatik bildirim".
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-6 — Hangi gün için yoklama alınır?** → **Yalnızca bugün** (Türkiye saati). Aynı gün düzeltilebilir; ertesi gün
kilitlenir. Dönem dışında ya da akademik takvimde tatil olan günde "Bugün okul günü değil" (hafta sonu engellenmez:
kurslar hafta sonu çalışır).
Neden: geriye dönük yoklama ve bildirim veliyi yanıltır; en sade kural.
Dokümanda istenen: "Şube, sınıf, şube-sınıf eşleşmesi, dönem ve akademik takvim" (kapsam, MVP modülü 1); "Öğretmen
mobilden 10 saniyede yoklama alır".
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-7 — Veli ne görür?** → **Bildirimler listesi**: yalnızca kendi çocuklarına ait bildirimler, yeniden eskiye, son 50;
hoş geldin ekranında okunmamış sayısı; liste açılınca okundu olur. Çocuğun devamsızlık geçmişi ekranı yok.
Neden: bildirimi okuma yeri gerekli ve yeterli; geçmiş ve özet haftalık gelişim özeti ile yönetici panelinin işi.
Dokümanda istenen: rol tablosu "Veli | Takip, bildirim, ödeme, randevu | Mobil"; "Kardeşi olan veli tek hesapla tüm
çocuklarını görür".
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

**C-8 — Kurucunun yoklamadaki yeri?** → Kurucu yalnızca öğretmen atar; yoklama almaz ve rapor görmez (kendini de
öğretmen olarak atarsa alır — BR-1). Raporlar yönetici panelinde (yol haritası satır 11).
Neden: kapsam bu spec'te en dar hâlinde kalır.
Dokümanda istenen: "Günlük özet: devamsızlık, geciken ödemeler, okunmayan duyurular" (kapsam, MVP modülü 8 — ayrı spec).
— tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20

## Requirements
1. **Sınıf öğretmeni ataması:** Kurucu, yönetim sayfasında bir sınıfa o sınıfın şubesinde etkin Öğretmen rolü olan
   kişileri atar ve kaldırır. Başka şubenin öğretmeni, öğretmen rolü olmayan ya da okulda pasif kişi atanamaz.
   Öğretmen rolü alınan, pasifleştirilen ya da atamadan çıkarılan kişi o sınıfın yoklamasına bir sonraki istekte
   erişemez.
2. **Sınıflarım:** Öğretmen yoklama ekranında yalnızca atandığı etkin sınıfları görür (ad, şube, bugünkü yoklama
   alındı mı). Tek sınıfı varsa doğrudan o sınıfın listesi açılır.
3. **Yoklama listesi:** Sınıfın etkin dönemde kayıtlı öğrencileri ad-soyad sırasıyla (Türkçe harf kuralı) gelir.
   Bugün yoklama alınmadıysa herkes "Var"; alındıysa kayıtlı durumlar ve "Kaydedildi, saat SS:DD" görünür. Her
   öğrencide üç seçim: Var / Yok / Geç; bir dokunuş durumu değiştirir. Düğmede sayılar: "Kaydet (3 yok, 1 geç)".
4. **Kaydetme:** Yoklama tek seferde kaydedilir; kayıtta kim, ne zaman, hangi sınıf, hangi gün tutulur. Sonuç:
   "Kaydedildi — 36 var, 3 yok, 1 geç. 5 veliye bildirim gitti." Velisi kayıtlı olmayan devamsız öğrenci varsa
   "2 öğrencinin velisi kayıtlı değil" eklenir. Aynı gün yeniden kaydetmek yoklamayı günceller (düzeltme).
5. **Gün kuralı:** Yalnızca bugün (Türkiye saati) için yoklama alınır ve düzeltilir. Geçmiş günün yoklaması
   değiştirilemez ("Geçmiş günün yoklaması değiştirilemez"). Etkin dönem dışında ya da akademik takvimde tatil olan
   günde liste yerine "Bugün okul günü değil" görünür ve kaydedilemez.
6. **Devamsızlık bildirimi:** "Yok" kaydedilen her öğrencinin okulda etkin her velisine bir uygulama içi bildirim
   oluşur ve e-posta kuyruğuna bir e-posta bırakılır. Metin: "{Öğrenci adı} bugün ({gün.ay.yıl}) {okul adı}'nda
   yoklamada gelmedi olarak işaretlendi. Bilginiz yoksa okulla iletişime geçin." "Var" ve "Geç" bildirim üretmez.
7. **Tekrar ve düzeltme:** Aynı öğrenci için aynı gün ikinci "yok" bildirimi oluşmaz. "Yok" → "Var"/"Geç" düzeltmesinde
   velilere "{Öğrenci adı} için bugünkü yoklama düzeltildi: okulda." bildirimi ve e-postası gider; ilk bildirim
   "düzeltildi" etiketi alır. Yeniden "Yok" olursa yeni bir "yok" bildirimi gider.
8. **Veli bildirimleri:** Veli mobilde ve web'de "Bildirimler" listesinde yalnızca kendi çocuklarına (bütün şubeler —
   BR-2) ait bildirimleri görür: yeniden eskiye, en fazla 50. Hoş geldin ekranında okunmamış sayısı; listeyi açınca
   hepsi okundu olur. Bildirim yoksa "Henüz bildirim yok".
9. **Eşzamanlılık:** Aynı sınıfın aynı gün yoklaması aynı anda iki kez (iki öğretmen, çift dokunuş) kaydedilirse tek
   yoklama kalır ve bir veliye aynı öğrenci için en fazla bir "yok" bildirimi gider.
10. **Gizlilik:** Bildirim ve e-posta yalnızca öğrenci adı, gün, okul adı ve durumu içerir (sağlık notu, sınıf
    arkadaşları, öğretmen notu yok). E-posta adresi, e-posta gövdesi ve öğrenci adı loglara yazılmaz; loglarda yalnızca
    kimlikler (kişi, sınıf, yoklama) geçer.
11. **Audit:** Öğretmen atama/kaldırma, yoklama kaydı ve düzeltmesi audit'e yazılır (kim, hangi sınıf, hangi gün,
    kaç öğrencinin durumu değişti, ne zaman, IP).

## User experience
- **Flow (öğretmen; mobil birincil, web aynı):** Hoş geldin → "Yoklama al" (yalnızca öğretmen rolünde görünür) →
  [birden çok sınıf] Sınıflarım (bugün alındı/alınmadı) → Yoklama listesi (herkes Var) → gelmeyenlere "Yok", geç
  kalana "Geç" → "Kaydet (3 yok)" → sonuç mesajı, liste kayıtlı hâliyle kalır → geri. Web adresi `#/yoklama`.
- **Flow (veli):** Hoş geldin → "Bildirimler (2)" (yalnızca veli rolünde) → liste (en yenisi üstte; "düzeltildi"
  etiketi) → geri. Web adresi `#/bildirimler`.
- **Flow (kurucu, yalnızca web):** Yönetim → Sınıflar (0008'in sınıf ekranı) → sınıf satırında "Öğretmenler" → şubenin
  öğretmenlerinden seç / kaldır → kaydet.
- **States:** Sınıflarım: yükleniyor / boş ("Size atanmış sınıf yok") / dolu / hata + tekrar dene. Yoklama listesi:
  yükleniyor / öğrenci yok ("Bu sınıfta kayıtlı öğrenci yok", kaydet kapalı) / okul günü değil / alınmadı / kaydedildi
  (saat) / kaydediliyor (düğme kilitli) / hata (işaretler kaybolmaz, tekrar dene) / erişim yok ("Bu sayfaya erişiminiz
  yok."). Bildirimler: yükleniyor / boş / dolu (okunmamışlar belirgin) / hata + tekrar dene. Öğretmen atama: yükleniyor /
  şubede öğretmen yok ("Bu şubede öğretmen yok") / kaydediliyor / hata.
- **Design reference:** none — the flow above is the design (spec 0005 yönetim ve spec 0004 mobil ekran kalıbı, ortak tema).
- **Accessibility:** her durum düğmesinin etiketi öğrenci adını içerir ("Ayşe Yılmaz: Yok"), seçili durum ekran
  okuyucuya bildirilir (seçili/işaretli); dokunma alanı en az 44×44; durum yalnızca renkle değil yazıyla da görünür;
  web'de akış yalnızca klavyeyle; sonuç ve hatalar duyurulur; iki temada kontrast spec 0002 eşiklerinde; 320 px'te
  yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S1 (okul/şube sınırı, öğretmen yalnızca kendi sınıfı), S2 (öğrenci ad-soyad + devam bilgisi, veli
  e-postası), S3 (rol ve veli-öğrenci bağına göre yetki), S5 (veliye giden otomatik bildirim), S6 (audit).
- Bağımlılık: 0008 ve 0009 tamamlanmadan build başlamaz.
- Performans: 40 öğrencilik sınıfta liste açılması ve kaydetme her biri en fazla 1 saniye (yerel geliştirme makinesi,
  test verisi); ağ gecikmesi hariç. Pi ölçümü ship'te kullanıcıda.
- Kapsam dışı: ders saati bazlı yoklama; mazeretli/izinli; geçmiş güne yoklama; push (B-6), SMS (B-2), WhatsApp (B-3);
  devamsızlık raporu, yönetici özeti, Excel'e aktarma (satır 11); velinin devamsızlık geçmişi ekranı; e-Okul'a
  aktarma; bildirimi silme; bildirim tercihleri (kapatma); veliden öğretmene cevap (satır 5, mesajlaşma).
- E-posta kuyruğu bellektedir (spec 0006): uygulama yeniden başlarsa gönderilmemiş e-posta kaybolur; uygulama içi
  bildirim veritabanında kalır.
- Split: önerilmedi — tek yetenek (yoklama al → veliye haber ver); öğretmen ataması onsuz yoklamanın yetkisi
  tanımlanamadığı için içeride. 13 kriter, tahamucasiroglu (önceden verilen onay, Promtlar 0082) sınırı.

## Acceptance criteria
- [ ] AC-1 — Öğretmen ataması: kurucu web'de bir sınıfa o şubenin etkin öğretmenlerini atar ve kaldırır; başka şubenin öğretmeni, öğretmen rolü olmayan ya da pasif kişi atanamaz (hata mesajı, hiçbir şey değişmez); kurucu olmayan 403 alır; atama/kaldırma audit'e yazılır.
- [ ] AC-2 — Sınıflarım: öğretmen yalnızca atandığı etkin sınıfları bugünkü durumuyla (alındı/alınmadı) görür; tek sınıfta liste doğrudan açılır; atanmış sınıf yoksa "Size atanmış sınıf yok".
- [ ] AC-3 — Yoklama listesi: sınıfın etkin dönemde kayıtlı öğrencileri Türkçe ad sırasıyla gelir, bugün alınmadıysa hepsi "Var"; başka sınıfın ya da kaydı biten öğrencinin adı listede yoktur; öğrencisi olmayan sınıfta "Bu sınıfta kayıtlı öğrenci yok" ve kaydet kapalı.
- [ ] AC-4 — Kaydetme: Var/Yok/Geç durumları tek seferde kaydedilir; sonuç sayıları (var, yok, geç, bildirim giden veli, velisi kayıtlı olmayan öğrenci) doğru; aynı gün yeniden açınca kayıtlı durumlar ve kayıt saati gelir.
- [ ] AC-5 — 10 saniye: 40 öğrencilik sınıfta liste açılması ve kaydetme her biri ≤ 1 sn (yerel ölçüm); 3 devamsızlı yoklama tek sınıflı öğretmende "Yoklama al" + 3 dokunuş + "Kaydet" = 5 dokunuşla tamamlanır (onay iletişimi yok).
- [ ] AC-6 — Gün kuralı: yalnızca bugün kaydedilir; aynı gün düzeltilebilir; dünkü yoklama değiştirilemez ("Geçmiş günün yoklaması değiştirilemez"), başka gün için yoklama gönderilemez; dönem dışında ve akademik takvimde tatil olan günde "Bugün okul günü değil", kayıt reddedilir; gün sınırı Türkiye saatine göredir (23:59 / 00:00).
- [ ] AC-7 — Devamsızlık bildirimi: "Yok" kaydedilen öğrencinin okulda etkin her velisine bir uygulama içi bildirim ve bir e-posta (kuyruğa) gider, metin gereksinim 6'daki gibidir; "Var" ve "Geç" bildirim üretmez; pasif veliye ve başka öğrencinin velisine gitmez; SMTP ayarlı değilken kayıt yine başarılıdır.
- [ ] AC-8 — Tekrar ve düzeltme: aynı gün yeniden kaydetmek aynı öğrenci için ikinci "yok" bildirimi/e-postası üretmez; "Yok" → "Var"/"Geç" velilere düzeltme bildirimi ve e-postası gönderir ve ilk bildirim "düzeltildi" görünür; yeniden "Yok" yeni bir "yok" bildirimi gönderir.
- [ ] AC-9 — Veli bildirimleri: veli mobil ve web'de yalnızca kendi çocuklarının bildirimlerini (farklı şubedeki kardeş dahil) yeniden eskiye, en fazla 50 görür; hoş geldin'de okunmamış sayısı doğru; listeyi açınca sayı sıfırlanır; bildirim yoksa "Henüz bildirim yok".
- [ ] AC-10 — Yetki ve izolasyon: öğretmen atanmadığı sınıfın ve başka şube/okulun yoklamasını okuyamaz ve yazamaz (404); öğretmen rolü olmayan (veli, muhasebe, atanmamış kurucu) yoklama işlemlerinde 403; veli başka velinin bildirimini göremez; atamadan çıkarılan, rolü alınan ya da pasifleştirilen öğretmenin bir sonraki isteği reddedilir; girişsiz istek 401.
- [ ] AC-11 — Eşzamanlılık: aynı sınıf ve gün için aynı anda gönderilen iki kayıttan sonra tek yoklama kalır ve her veli aynı öğrenci için en fazla bir "yok" bildirimi ve bir e-posta alır.
- [ ] AC-12 — Gizlilik ve audit: bildirim/e-posta gövdesinde öğrenci adı, gün, okul adı ve durumdan başka kişisel veri yok; loglarda e-posta adresi, gövde ve öğrenci adı geçmez; atama, yoklama kaydı ve düzeltme audit'e (kim, sınıf, gün, değişen öğrenci sayısı, IP) yazılır.
- [ ] AC-13 — Ekran durumları (mobil + web, iki tema): "Yoklama al" yalnızca öğretmende, "Bildirimler" yalnızca velide; Sınıflarım, yoklama listesi, bildirimler ve öğretmen atama ekranlarının bütün durumları (yükleniyor, boş, okul günü değil, kaydediliyor-düğme kilitli, kaydedildi, hata + tekrar dene ve işaretlerin korunması, erişim yok); durum düğmesi etiketleri öğrenci adını içerir ve seçili durum bildirilir; web akışı yalnızca klavyeyle; 320 px'te yatay kayma yok.

## Self-critique (yapıldı — kapanan boşluklar)
- "Bugün" hangi saat dilimi? → Türkiye saati (Avrupa/İstanbul), AC-6'da sınır değerleriyle.
- Veli e-postası yoksa? → Her kişinin giriş e-postası var (spec 0004); pasif veliye gitmez (AC-7).
- Öğrenci gün içinde başka sınıfa nakledilirse? → Liste açıldığı andaki kayıtlı öğrenciler; kaydı biten öğrenci için
  gelen durum yok sayılır (AC-3, plan Risks).
- İki veli aynı kişiyse (kardeşler)? → Her öğrenci için ayrı bildirim; aynı veliye iki çocuk için iki bildirim.
- Hafta sonu? → Engellenmez (C-6); yalnızca dönem dışı ve takvimdeki tatil.
- 0008'de tatil kavramı yoksa? → Yalnızca dönem kontrolü kalır; plan değişikliğiyle kayda geçer (plan Risks 2).

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
