# Spec 0021 — gunluk-seridi-gecmis-aktivite

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0021-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: specs/active/0008-gunluk-hata-bildirimi-loglama.md — Req 1–6 + C-2, C-3 + self-critique 1, 2, 5, 6, 13 (bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun')
- Sensitive area: none — yeni bir kimlik/yetki kuralı ya da gizli alan eklemez; sahiplik (BR-1) 0002'nin oturumunu, maskeleme 0008'in mekanizmasını kullanır
- Depends on: 0001, 0008, 0002, 0012, 0003

## Intent
İçerik üreticisi, sitede ne olup bittiğini sayfadan ayrılmadan görmek istiyor: ekranın en altındaki ince
"Günlük" şeridinde kendi son işlemleri akar (ör. "Video hazırlandı · 57 sn · 10:30–10:31"); hata kırmızı,
uyarı sarı, normal işlemler sade görünür ve geçmiş günlere bakılabilir (ilk_promt.txt: "Programın en altında
ince bir kısım olacak. Günlük kısmı … son yapılan işlem gözükecek … günlük bazda ayrılacak … hata kırmızı,
uyarı sarı, normal durumlar akacak"). 0001 olay kaydını ve şeritte son sistem olayını kurdu; bu spec onu
kişiye özel, seviyeli, süreli ve geçmişi gezilebilir hale getirir. Başarı: kullanıcı yalnızca kendi
kayıtlarını anında görür, günler arasında gezinir ve hataları filtreyle hızla bulur. Bilinçli olarak
yapılmayanlar: Günlük'ün ajan penceresinde gösterilmesi (Günlük yalnızca sitededir), admin'in aktivite
görünümü (0010), hata bildirimi (0022).

## Requirements
1. **Günlük şeridi:** Sitenin her sayfasının en altındaki ince "Günlük" şeridi, giriş yapmış kullanıcının
   en son kaydını gösterir (saat + metin). Yeni kayıtlar sayfa yenilenmeden şeride düşer. 0001'deki "son
   sistem olayı" gösterimini bu davranış devralır.
2. **Yoklama sıklığı:** Şerit sekme önplandayken 3 sn'de bir yoklar; sekme arka plandayken yoklamayı 15 sn'ye
   yavaşlatır, sekme yeniden görünür olunca beklemeden hemen yeniler.
3. **Seviye görünümü:** Her Günlük kaydının bir seviyesi vardır: Bilgi, Uyarı, Hata, Kritik. Hata ve Kritik
   kırmızı, Uyarı sarı, Bilgi temanın olağan renginde görünür; renk tek işaret değildir, seviye ayrıca
   ikon + metin etiketiyle ("Uyarı", "Hata", "Kritik") belirtilir. Günlük'e teknik log değil, kullanıcıya
   anlamlı işlemler yazılır (teknik seviye eşlemesi 0008 Req 3).
4. **Süreli işlemler:** Başlangıcı ve bitişi olan işlemler (ör. video hazırlanması) Günlük'te süre ile
   birlikte başlangıç ve bitiş saatini gösterir (ilk_promt.txt: "video hazırlandı, süresi 57 saniye,
   başlangıç 10.30, bitiş …"). Saat Europe/Istanbul, 24 saat `HH:mm`. Süre biçimi: 60 sn altı "57 sn";
   1 saate kadar "30 dk 12 sn"; 1 saat ve üstü "1 sa 5 dk". Süre her zaman başlangıç–bitiş farkından
   hesaplanır (ilk_promt.txt örneği kendi içinde tutarsız: 57 sn ama 10.30–11).
5. **Geçmişe bakış:** Kullanıcı şeride tıklayarak (ya da odaklanıp Enter) Günlük geçmişini sayfanın üstünde
   ayrı bir modalda açar (ilk_promt.txt'teki "ekranın üstünde ayrı bir modal" tercihi). Modal gün başlıklıdır;
   o günün kayıtları en yenisi üstte listelenir; günde ilk 50 kayıt gösterilir, fazlası "Daha fazla" ile
   50'şer yüklenir; önceki/sonraki gün düğmeleri vardır; seviye filtresi Tümü / Uyarı / Hata ("Hata" filtresi
   Hata ve Kritik kayıtları gösterir).
6. **Gün sınırları:** Bugünden sonraki güne geçiş pasiftir; geçersiz ya da ileri bir tarih isteği bugüne
   döner. Gezilebilecek en eski gün hesabın açıldığı gündür (o günde önceki gün düğmesi pasif).
7. **Sahiplik:** Kullanıcı yalnızca kendi Günlük kayıtlarını görür (BR-1). Giriş yapılmamış ziyaretçiye
   Günlük şeridi gösterilmez, Günlük istekleri reddedilir (default-deny).
8. **Saklama ve gün ayrımı:** Günlük kayıtları sunucuda süresiz tutulur ve Europe/Istanbul saatine göre gece
   yarısında günlere ayrılır (ilk_promt.txt: "bizim programda da sürekli tutulacak … günlük bazda
   ayrılacak"). Zamanlar UTC saklanır, Europe/Istanbul ile gösterilir.
9. **Profil etiketi:** Şerit ve geçmiş seçili profile göre filtrelenmez; bir profile ait kayıtta profil adı
   etiket olarak görünür (üretim sırası farklı profillerin işlerini karıştırabilir; filtre hata kaçırtır).
10. **Günlük kaynakları:** Diğer özellikler kullanıcıya gösterilecek işlemleri 0001'in olay kaydına yazar;
    hangi işlemlerin yazılacağı her özelliğin kendi spec'inde belirlenir. Bu spec'in kurulduğu anda mevcut
    kaynaklar: ajan bağlandı/koptu (0001), giriş/çıkış/başarısız giriş (0002), ajan eşleştirme (0012). Sonra
    gelenler: hata bildirimi gönderildi (0022), konsept/üretim olayları (0014, 0017; iş bitti/hata olayları
    süre ve başlangıç–bitiş saatiyle — E-1).
11. **Aktivite kaydı:** Admin "aktivite" görünümü aynı kayıtların sunucu tarafıdır (Ç-7); başlık/metin
    içeriği admin'e gösterilmez. Admin ekranı 0010'dadır; bu spec yalnızca kayıtların kullanıcıya ve tarihe
    göre ayrılmış, süresiz saklanmasını sağlar.
12. **Güvenli gösterim:** Kullanıcı ya da LLM içeriği Günlük'te düz metin gösterilir (`Html.Raw` yasak);
    kayıt metinleri 0008'in maskeleme mekanizmasından geçmiştir.

## User experience
- Flow: Kullanıcı herhangi bir sayfadayken alttaki şeritte son kaydını görür → bir işlem (ör. ajan
  bağlandı) olunca birkaç saniye içinde şerit güncellenir → hata olunca kayıt kırmızı + "Hata", uyarı olunca
  sarı + "Uyarı" görünür → şeride tıklar (ya da odaklanıp Enter) → Günlük geçmişi modalı açılır, bugünün
  kayıtları en yenisi üstte → filtreden "Hata"yı seçer → önceki güne geçer → "Daha fazla" ile devamını
  yükler → kapatır (Kapat, Esc ya da dışarı tıklama), odak şeride döner.
- States:
  - Günlük şeridi: yükleniyor · boş ("Henüz bir işlem yok") · Bilgi kaydı · Uyarı kaydı (sarı) · Hata/Kritik
    kaydı (kırmızı) · güncellenemiyor (son bilinen kayıt kalır + "Günlük güncellenemiyor" işareti, kendiliğinden
    düzelir).
  - Günlük geçmişi: yükleniyor · kayıtlı gün listesi · boş gün ("Bu günde işlem yok") · filtre sonucu boş
    ("Bu günde işlem yok") · "Daha fazla" yükleniyor · hata ("Günlük yüklenemedi" + tekrar dene) · bugün
    (sonraki gün pasif) · hesabın ilk günü (önceki gün pasif).
  - Yetkisiz: giriş yapılmamışsa Günlük görünmez/çalışmaz (default-deny).
- Design reference: none — the flow above is the design. `source/*.png` (commit `61cc824`) Günlük şeridini
  göstermez (ilk_promt.txt: "en altında ince bir kısım"). Bir eskiz verilirse `source/` altına tarihli görsel
  eklenip commit'i bu spec'e yazılır. Renk: tema değişkenleri, turuncu/kahve, açık mod esas (K-7).
- Accessibility: Şerit klavyeyle erişilebilir (Tab, Enter/Space); geçmiş modalı açılınca odak içine alınır,
  Esc kapatır, odak şeride döner. Seviye yalnızca renkle değil ikon + metin etiketiyle belirtilir. Yalnızca
  Uyarı, Hata ve Kritik yeni kayıtlar ekran okuyucuya `polite` duyurulur; Bilgi kayıtları duyurulmaz.
  Kırmızı/sarı/normal metin ve arka plan her iki temada WCAG AA kontrastında.

## Constraints & out of scope
- Sensitive area: none (bkz. başlık).
- Canlı güncelleme Ajax polling ile (önplanda 3 sn, arka planda 15 sn; architecture.md Kural 2); tarayıcıda
  SignalR yok, `fetch` yok (K-6).
- Arayüz metinleri Türkçe, resx'te. Zamanlar UTC saklanır, Europe/Istanbul ile gösterilir.
- Out of scope: Günlük'ün ajan penceresinde gösterilmesi; admin aktivite/istatistik ekranı (0010); hata
  bildirimi ve "Hata bildirimi gönderildi" kaydı (0022); üretim olaylarının yazılması (0014, 0017); seçili
  profile göre filtreleme; Günlük kayıtlarının silinmesi ya da dışa aktarılması; teknik loglar (0008).
- Split: done — 0021 Günlük şeridi, geçmiş ve aktivite kaydı / kardeşler: 0008, 0022

## Acceptance criteria
- [ ] AC-1 — Giriş yapmış kullanıcının herhangi bir sayfasında Günlük şeridi en son kaydı (saat + metin) gösterir; sekme önplandayken yeni bir kayıt en geç 5 sn içinde sayfa yenilenmeden şeritte görünür; sekme arka plandayken yoklama 15 sn'de birdir ve sekme yeniden görünür olunca beklemeden yeni istek gider.
- [ ] AC-2 — Hata ve Kritik kayıtlar kırmızı + "Hata"/"Kritik" etiketi ve ikonu, Uyarı kaydı sarı + "Uyarı" etiketi ve ikonu, Bilgi kaydı olağan renkte görünür; renkler yalnızca tema değişkenlerinden gelir ve her iki temada WCAG AA kontrastını sağlar.
- [ ] AC-3 — Başlangıç ve bitişi olan bir kayıt süreyi ve Europe/Istanbul `HH:mm` başlangıç–bitiş saatini gösterir (ör. "Video hazırlandı · 57 sn · 10:30–10:31"); süre başlangıç–bitiş farkından hesaplanır ve biçimi 59 sn → "59 sn", 60 sn → "1 dk 0 sn", 1812 sn → "30 dk 12 sn", 3600 sn → "1 sa 0 dk", 3900 sn → "1 sa 5 dk" olur.
- [ ] AC-4 — Şerit ilk veri gelene kadar yükleniyor durumunu gösterir; hiç kaydı olmayan kullanıcı "Henüz bir işlem yok" görür; güncelleme isteği başarısız olunca son bilinen kayıt kalır ve "Günlük güncellenemiyor" işareti görünür, bağlantı dönünce işaret kendiliğinden kalkar.
- [ ] AC-5 — Şeride tıklama ya da odaklanıp Enter Günlük geçmişi modalını açar ve odak modalın içine alınır; veri gelene kadar yükleniyor durumu görünür; kayıtlar gün başlıklı, en yenisi üstte; kaydı olmayan gün "Bu günde işlem yok", yükleme hatası "Günlük yüklenemedi" + tekrar dene gösterir; Kapat, Esc ve dışarı tıklama modalı kapatır ve odak şeride döner.
- [ ] AC-6 — Geçmişte bir gün için ilk 50 kayıt gösterilir; 51. kayıt ve sonrası "Daha fazla" ile 50'şer yüklenir; tam 50 kayıtlı günde "Daha fazla" görünmez; seviye filtresi Tümü / Uyarı / Hata çalışır ve "Hata" filtresi Hata ile Kritik kayıtları gösterir.
- [ ] AC-7 — Bugün görüntülenirken sonraki gün düğmesi pasiftir; ileri ya da geçersiz bir tarih isteği bugünü döndürür; hesabın açıldığı günde önceki gün düğmesi pasiftir.
- [ ] AC-8 — Kullanıcı A, kullanıcı B'nin Günlük kayıtlarını hiçbir yoldan (şerit, geçmiş, doğrudan istek) göremez; giriş yapılmamış Günlük isteği reddedilir; giriş yapılmamış ziyaretçiye (ör. giriş sayfasında) Günlük şeridi gösterilmez.
- [ ] AC-9 — 8 gün ve 400 gün önceki Günlük kayıtları geçmişte hâlâ görünür (sunucuda süresiz saklama); kayıtlar Europe/Istanbul saatine göre gece yarısında günlere ayrılır (23:59:59'daki kayıt o güne, 00:00:00'daki ertesi güne düşer).
- [ ] AC-10 — Şerit ve geçmiş seçili profil değişince filtrelenmez; bir profile ait kayıt profil adını etiket olarak gösterir, profile ait olmayan kayıt (ör. giriş) etiketsiz görünür.
- [ ] AC-11 — Yeni gelen Uyarı, Hata ve Kritik kayıtlar ekran okuyucuya `polite` duyurulur, Bilgi kayıtları duyurulmaz.
- [ ] AC-12 — `<script>` ya da HTML içeren bir kayıt metni Günlük'te çalıştırılmadan düz metin görünür; 0008 maskeleme listesindeki bir değeri (ör. maskesiz API key) içeren bir kayıt şeritte ve geçmişte maskeli görünür.

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
