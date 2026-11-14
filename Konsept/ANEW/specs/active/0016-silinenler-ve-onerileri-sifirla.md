# Spec 0016 — Silinenler sayfası ve önerileri sıfırla

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0016-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0005-konseptler.md` (Req 17'nin çöp kısmı, Req 21–23; AC-8'in Silinenler kısmı, AC-30…AC-33, AC-34–35'in silme/Silinenler kısmı; S-5'in silme kısmı, G-9, G-10, G-14, G-15; Ç-2) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: yok (kalıcı silme geri alınamaz ama BR-7 ve normal testler yeterli — `docs/domain.md`; veri sahipliği BR-1 yalnızca tüketilir)
- Depends on: 0001, 0002, 0003, 0004, 0013, 0005, 0014

## Intent
İçerik üreticisi beğenmediği konseptleri Üretilenler'den kaldırmak, ama bu konuların bir daha önerilmemesi için
sistemin onları hatırlamasını istiyor; Silinenler'de süresiz duran kayıtları gerektiğinde kalıcı olarak silebiliyor
(`ilk_promt.txt`: "Burada saklanacak sonsuza kadar … negatif prompt gibi düşün"). Ayrıca daha önce önerilip seçmediği
başlıkların yeniden önerilebilmesi için öneri hafızasını Ayarlar'dan sıfırlayabiliyor. Bu spec Silinenler sayfasının
kendisini iki sekmeyle (Konseptler / Videolar) kurar ve konsept sekmesini doldurur; video sekmesi 0019 ile dolar (Ç-2).
Başarı: silinen konsept kaybolmaz, öneri üretiminde negatif sinyal olur; kalıcı silme ve sıfırlama ne yapacağını açıkça
söyleyen bir onaydan sonra çalışır. Bilinçli olarak yapılmayanlar: geri yükleme (v1'de yok), video silme (0019).

## Requirements
Kaynak: `ilk_promt.txt` (Konseptler ve Silinenler bölümleri, Ayarlar'da "öneriyi sıfırla"), `docs/domain.md`
(BR-1, BR-2, BR-4, BR-7, BR-8); sabah karar özeti Ç-2.

**A. Konsept silme**
1. Üretilenler satırında kırmızı çöp kutusu (sil) ikonu vardır. Silme yalnızca **Üretildi** ve **Onaylandı** durumundaki
   konseptlerde mümkündür; Sırada, Üretiliyor ve Video oldu konseptte ikon pasiftir ve açıklama gösterir ("Önce üretim
   sırasından çıkarın" / "Videosu olan konsept burada silinemez") (S-5).
2. Çöpe basınca kısa bir onay diyaloğu çıkar ("Konsept Silinenler'e taşınacak; geri yükleme yok"; G-9). Onaylanınca
   konsept Üretilenler'den (ve onaylıysa Konseptten Video'nun onaylı listesinden) kalkar ve Silinenler'e gider;
   süresiz saklanır (BR-7). Vazgeçilirse hiçbir şey değişmez.

**B. Silinenler sayfası**
3. Üst bardaki "Silinenler" bağlantısı (0001'de pasif) Silinenler sayfasını açar. Sayfada iki sekme vardır:
   **Konseptler** ve **Videolar**; her sekmenin kendi boş durumu vardır. Bu spec'te Videolar sekmesi yalnızca boş
   durumunu ("Henüz silinmiş video yok") gösterir; içeriği 0019 ile gelir (Ç-2).
4. Konseptler sekmesi seçili profilin silinmiş konseptlerini başlık ve silinme zamanıyla, en yeni silinen üstte
   listeler; 50'şerli sayfalanır; arama v1'de yok (G-14). Profil değişince liste o profilinkilere geçer (BR-2).
5. Silinmiş bir konsepte tıklanınca sahneleri (ve varsa kaynak listesi) salt okunur görüntülenir; düzenleme ve onay
   yoktur (G-15).
6. Silinenler'den kalıcı silme bir onay diyaloğu ister ("Konsept kalıcı olarak silinecek, geri alınamaz"); onaylanınca
   konsept tamamen gider ve geri alınamaz (BR-7). Geri yükleme v1'de yoktur (Ç-2, G-9).

**C. Negatif sinyal**
7. Silinenler'deki konseptlerin başlıkları konu önerisinde negatif sinyaldir (BR-8): öneri talimatına 0014'ün "elenen
   konular" kümesine (ve 200 başlık kuralına) girer, anlamca tekrar denetiminin karşılaştırma kümesindedir ve model
   bu başlıklardan birini döndürse bile listelenmez (BR-4). Fikirden üretimdeki mükerrer kontrolü de bunları kapsar.
8. Kalıcı silinen konseptin başlığı negatif sinyal olmaktan çıkar; yalnızca Silinenler'de duran kayıtlar sinyal
   sayılır (G-9).

**D. Önerileri sıfırla (Ayarlar)**
9. Ayarlar'da (temeli 0004) "Öneriler" bölümünde "Önerileri sıfırla" eylemi vardır. Ne silineceğini anlatan bir onay
   ("Seçili profilde önerilip seçmediğin başlıklar yeniden önerilebilir olacak; üretilmiş ve silinmiş konseptler
   etkilenmez") sonrası **yalnızca seçili profilin önerilip seçilmemiş** başlıklarının hafızasını siler. Üretilmiş ve
   Silinenler'deki konseptler tekrar önerilmemeye devam eder (BR-4; G-10). Seçili profil yoksa eylem pasiftir.

**E. Yetki ve sahiplik**
10. Kullanıcı yalnızca kendi profillerinin konseptlerini siler, Silinenler'ini görür, kalıcı siler ve önerilerini
    sıfırlar (BR-1).

**F. Profil silme kuralının Silinenler kısmı (0014 Req 26, 0003 Req 14)**
11. Silinenler'deki (silinmiş işaretli) konseptler de profilin konsepti sayılır: profilin tek konsepti Silinenler'de
    olsa bile profil kalem modalındaki Sil pasiftir ve "Bu profilin konseptleri var; silinemez" der; arayüzü atlayan
    silme isteği sunucuda reddedilir. Profilin son konsepti Silinenler'den kalıcı silinince (Req 6) profil 0003'teki
    gibi silinebilir hâle gelir.

## User experience
- Flow (silme): Üretilenler'de çöp → onay diyaloğu → "Sil" → konsept Silinenler'e taşınır.
- Flow (Silinenler): Üst bardan "Silinenler" → Konseptler sekmesi → konsepte tıklayıp salt okunur sahneleri okur →
  kalıcı silmek isterse "Kalıcı sil" → onay → konsept gider.
- Flow (sıfırlama): Üst bardan "Ayarlar" → "Öneriler" bölümü → "Önerileri sıfırla" → onay → "Öneriler sıfırlandı".
- States:
  - Çöp ikonu: etkin (Üretildi, Onaylandı) · pasif + açıklama (Sırada, Üretiliyor, Video oldu).
  - Silme onayı: açık · siliniyor · silindi · hata (sade mesaj, konsept yerinde kalır).
  - Silinenler: profil yok ("Önce bir profil oluştur") · yükleniyor · alınamadı (sade Türkçe mesaj) · Konseptler
    sekmesi boş ("Henüz silinmiş konsept yok") · Konseptler listesi · seçili konsept (salt okunur) · Videolar sekmesi
    boş ("Henüz silinmiş video yok") · kalıcı silme onayı · kalıcı silindi · hata.
  - Önerileri sıfırla: profil yok (pasif) · onay diyaloğu · sıfırlanıyor · tamamlandı · hata.
  - Yetki yok: başka kullanıcının kaydına erişim "bulunamadı".
- Design reference: none — the flow above is the design (Silinenler sayfası ve Ayarlar'daki Öneriler bölümü için görsel
  yok; üst bar yerleşimi 0001'deki gibi). Renk: tema değişkenleri (K-7); çöp ikonu kırmızı (ilk_promt.txt).
- Accessibility: sekmeler klavyeyle (ok tuşları / Tab) değiştirilebilir ve sekme rolüne sahiptir; çöp ve kalıcı sil
  düğmelerinin erişilebilir adı "Sil: <başlık>", "Kalıcı sil: <başlık>"; onay diyaloglarında odak diyaloğa gider, Esc
  vazgeçer, kapanınca odak açan kontrole döner; WCAG AA kontrast her iki temada.

## Constraints & out of scope
- Sensitive area: yok. Kalıcı silme geri alınamaz; BR-7 ve normal testlerle kanıtlanır (`docs/domain.md`). BR-1 yalnızca
  tüketilir; AC-11 normal testle kanıtlanır (G-21). Bu spec online model çağrısı yapmaz.
- Silme soft delete'tir (`DeletedAt`); kalıcı silme kaydı tamamen kaldırır.
- Out of scope: geri yükleme (v1'de iki tarafta da yok) · Videolar sekmesinin içeriği, video silme ve silinen videonun
  "beğenilmedi" sinyali (0019) · Ayarlar sayfasının temeli (0004) · konsept üretimi (0014) · düzenleme (0015) · arama.
- Split: done — 0016 Silinenler sayfası ve önerileri sıfırla / kardeşler: 0005, 0014, 0015

## Acceptance criteria
- [ ] AC-1 — Üretildi ya da Onaylandı konseptte çöpe basınca "geri yükleme yok" bilgisini içeren onay diyaloğu açılır; onaylanınca konsept Üretilenler'den kalkar (onaylıysa onaylı listeden de çıkar), Silinenler'in Konseptler sekmesinde (seçili profil için) görünür ve yeniden yüklemeden sonra da orada durur (BR-7); vazgeçilirse hiçbir şey değişmez.
- [ ] AC-2 — Sırada, Üretiliyor ve Video oldu konseptte çöp ikonu pasiftir ve ilgili açıklamayı gösterir; bu konseptler için doğrudan silme isteği reddedilir ve konsept değişmez (S-5).
- [ ] AC-3 — Üst bardaki "Silinenler" bağlantısı sayfayı açar; Konseptler ve Videolar sekmeleri görünür; Videolar sekmesi "Henüz silinmiş video yok", konsept yokken Konseptler sekmesi "Henüz silinmiş konsept yok" boş durumunu gösterir; yüklenirken yükleniyor, veri alınamazsa sade Türkçe hata mesajı görünür; profil yoksa "Önce bir profil oluştur" görünür (Ç-2).
- [ ] AC-4 — Konseptler sekmesi yalnızca seçili profilin silinmiş konseptlerini en yeni silinen üstte listeler; 51. kayıt ikinci sayfadadır; profil değiştirilince liste yeni profile göre yenilenir; başka profilin ve silinmemiş konseptler görünmez (G-14, BR-2).
- [ ] AC-5 — Silinmiş konsepte tıklanınca sahneleri ve varsa kaynakları salt okunur düz metin olarak görünür; düzenleme, onay ve yeniden üretim kontrolü yoktur ve bu işlemlere yönelik doğrudan istek reddedilir (G-15).
- [ ] AC-6 — Silinenler'den kalıcı silme bir onay diyaloğu ister; onaylanınca konsept tamamen gider, Silinenler'de ve Üretilenler'de görünmez ve yeniden yüklemede de dönmez; vazgeçilirse hiçbir şey değişmez. Sayfada geri yükleme kontrolü yoktur (BR-7, G-9).
- [ ] AC-7 — Silinenler'deki konseptin başlığı sonraki konu önerisi talimatında "elenen konular" arasında yer alır (stub'a giden istekte görülür) ve anlamca denetim kümesindedir; stub bu başlığı birebir döndürse bile yeni önerilerde listelenmez; fikirden üretimde aynı başlıklı konsept kaydedilmez (BR-8, BR-4).
- [ ] AC-8 — Kalıcı silinen konseptin başlığı sonraki öneri talimatında yer almaz ve stub onu döndürünce (başka bir kurala takılmıyorsa) öneri listelenir (G-9).
- [ ] AC-9 — Ayarlar'daki "Önerileri sıfırla" ne silineceğini anlatan bir onay diyaloğu açar; onaylanınca yalnızca seçili profilin önerilip seçilmemiş başlıkları tekrar önerilebilir olur (stub o başlığı döndürünce listelenir); aynı profilin üretilmiş ve Silinenler'deki konseptlerinin başlıkları ile başka profillerin önerilip seçilmemiş başlıkları engel olmaya devam eder (BR-4, G-10); vazgeçilirse hiçbir şey değişmez; seçili profil yokken eylem pasiftir.
- [ ] AC-10 — Silme, kalıcı silme ya da sıfırlama sunucu hatasıyla biterse sade Türkçe mesaj görünür ve veri değişmez.
- [ ] AC-11 — Kullanıcı A, kullanıcı B'nin konseptini silme, B'nin Silinenler kaydını görüntüleme ya da kalıcı silme, B'nin profilinin önerilerini sıfırlama girişiminde "bulunamadı" alır ve B'nin verisi değişmez (BR-1). Girişsiz istekler reddedilir.
- [ ] AC-12 — Tüm tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile gider (K-6); sekmeler, çöp, kalıcı sil ve onay diyalogları yalnızca klavyeyle kullanılabilir, diyalog açılınca odak diyaloğa geçer, Esc vazgeçer, kapanınca odak açan kontrole döner; ikon düğmelerinin konsept başlığını içeren erişilebilir adı vardır.
- [ ] AC-13 — Tek konsepti olan bir profilde konsept silinip Silinenler'e taşınınca profil kalem modalındaki Sil pasif kalır ve "Bu profilin konseptleri var; silinemez" gösterir; arayüzü atlayan doğrudan profil silme isteği sunucuda reddedilir, profil ve Silinenler'deki konsept yerinde kalır; konsept Silinenler'den kalıcı silinince profil 0003 AC-14'teki gibi silinebilir (0014 Req 26, Req 11).

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
