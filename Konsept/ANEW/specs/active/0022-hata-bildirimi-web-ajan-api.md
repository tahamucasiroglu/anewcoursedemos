# Spec 0022 — hata-bildirimi-web-ajan-api

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0022-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: specs/active/0008-gunluk-hata-bildirimi-loglama.md — Req 9–12, 14 + C-1, C-4, C-5, C-7 + self-critique 4, 8, 9, 11, 12, 14 + Ç-6 (bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun')
- Sensitive area: H-2 Gizli bilgiler (bildirim açıklaması, otomatik eklenen bilgiler ve ajan log satırlarında maskeleme); H-1 Kimlik ve yetki (kimliksiz uç açılmaz; bildirim yalnızca oturumlu kullanıcıdan ya da eşleşmiş ajandan kabul edilir; bekleyen ajan bildirimi oluştuğu hesaba bağlıdır; okuma yalnızca `admin.panel`)
- Depends on: 0001, 0008, 0002, 0011, 0012, 0021

## Intent
Kullanıcı bir sorunla karşılaştığında bunu birkaç adımda, gizli bilgi sızdırmadan bildirebilmeli; geliştirici
de bildirimleri API'de saklı bulmalı (ilk_promt.txt: sağ üstte "hata bildirimi"; "kurulumlarda falan hata
olduğu zaman bu hataları takip etmeli ve hata oldu bildir demeli. Bildirilen hatalar da API'ye gitmeli.
API'ye giden hatalar depolanmalı"). Üç kaynak vardır: web (sağ üstteki "Hata bildir"), ajan ("Bir hata oldu,
bildir") ve API'nin kendi beklenmeyen hataları (otomatik kayıt, Ç-6). Başarı: bildirim korelasyon numarası ve
bağlamla birlikte "Yeni" durumunda saklanır, admin panelinin (0025) okuyabileceği biçimdedir; ajan bağlantısız
ya da eşleşmemişken bile bildirim kaybolmaz ve yanlış hesaba gitmez; bir hata döngüsü bildirim yağmuru
oluşturmaz. Bilinçli olarak yapılmayanlar: bildirimlerin listelenmesi/durum yönetimi (0025), kullanıcının kendi
bildirimlerini takip etmesi, ekran görüntüsü, giriş ekranından bildirim, ajan kurulum ekranının kendisi (0009).

## Requirements
1. **Web'den hata bildirimi:** Sağ üstteki "Hata bildir" düğmesi (0001'de pasifti) etkinleşir ve modal açar.
   Kullanıcı bir açıklama (10–4000 karakter) yazar ve gönderir. Bildirime otomatik eklenir: gönderen kullanıcı,
   kaynak "web", zaman, ürün versiyonu, bulunulan sayfa, kullanıcının son 20 Günlük kaydı (0021) ve varsa son
   hatanın korelasyon numarası. Kullanıcı otomatik eklenecek bilgiyi göndermeden önce modalda görür.
   Başarılı gönderimde bir bildirim numarası gösterilir.
2. **Hata mesajından bildir:** Web'in kullanıcıya gösterdiği ProblemDetails hata mesajında bir "Bildir"
   bağlantısı vardır; modalı o hatanın korelasyon numarası dolu olarak açar.
3. **İptal:** Modal İptal/Esc ile kapanınca yazılan metin atılır ve kayıt oluşmaz; metin yalnızca "Gönder"de
   sunucu hatası olursa korunur.
4. **Ajandan hata bildirimi:** Ajanda kurulum ya da çalışma sırasında bir hata olduğunda ajan kullanıcıya
   "Bir hata oldu" der (ilk_promt.txt: "hata oldu bildir demeli"): küçük bir ajan penceresi öne gelir ve bir
   Windows bildirimi gösterilir; tray ikonunda bekleyen bildirim işareti görünür. Pencerede maskelenmiş hata
   özeti ile "Bildir", "Kapat" ve "Hata raporunu dosyaya kaydet" seçenekleri ve gönderilecek metnin tamamını
   gösteren "Ayrıntıları göster" vardır. "Bildir" seçilirse bildirim kaynak "ajan", ajan versiyonu, işletim
   sistemi, hata özeti ve ajanın yerel logundan son 15 dakikanın Warning+ satırlarıyla (en fazla 200 satır,
   en yeniler) API'ye gönderilir; "Kapat" seçilirse hiçbir şey gönderilmez. Ajanın kurulum ekranı 0009'dadır;
   0009 kurulum hatalarında, 0023 güncelleme hatalarında bu mekanizmayı kullanır. Kurulum/güncelleme ekranından
   açılan bildirim ayrıca hatalı bileşenin adını ve hatanın adımını (indirme / doğrulama / kurulum) taşır; hiçbir
   ajan bildiriminde makine adı yer almaz.
5. **Ajan içeriğinde kişisel veri:** Gönderilen ve dosyaya kaydedilen metin 0008 maskelemesinden geçer;
   Windows kullanıcı adı içeren yollar `%USERPROFILE%` olarak kısaltılır.
6. **Aynı hata bir kez:** Ajan aynı hata özetini aynı ajan oturumunda (ajan yeniden başlayana kadar) yalnızca
   bir kez sorar; tekrarlar yalnızca teknik loga yazılır.
7. **Gönderilemeyen ajan bildirimi:** Kimliksiz bir bildirim ucu açılmaz (default-deny). Ajan API'ye bağlı
   değilken ya da henüz eşleştirilmemişken seçilen bildirim ajanda (maskeli) bekletilir, "Bağlantı kurulunca
   gönderilecek" gösterilir ve bağlantı + eşleşme kurulunca gönderilir; ajan yeniden başlasa da kaybolmaz.
   Kurulum hiç bitmezse kullanıcı "Hata raporunu dosyaya kaydet" ile raporu elle iletebilir.
8. **Bekleyen bildirimin hesabı:** Bekleyen bildirim oluştuğu andaki hesaba bağlanır; ajan farklı bir hesapla
   eşleşirse gönderilmez ve kullanıcıya "Bu bildirim başka bir hesapta oluşturuldu, gönderilmedi" bilgisi
   verilir. Eşleşme öncesi oluşan bildirim ilk eşleşen hesapla gider (BR-1).
9. **Bekleme kuyruğu sınırı:** Ajanda en fazla 20 bekleyen bildirim tutulur; 21. eklenince en eskisi düşer ve
   teknik loga Warning yazılır.
10. **API'nin otomatik hata kaydı (Ç-6):** API'de beklenmeyen bir hata (Error/Critical) oluştuğunda otomatik
    olarak kaynak "API" ile bir hata kaydı oluşur: zaman, korelasyon numarası, ürün versiyonu, maskelenmiş
    hata özeti ve istek oturumluysa kullanıcı. Beklenen hatalar (doğrulama, yetkisiz, bulunamadı gibi 4xx
    yanıtlar) kayıt oluşturmaz.
11. **Saklama:** Hata bildirimleri (web, ajan, API) API'de süresiz saklanır ve "Yeni" durumunda başlar;
    kaynak alanı web / ajan / API değerlerinden biridir (0025 filtresi bu üç kaynağı kullanır). Durum listesi
    ve durum yönetimi, listeleme ve okuma 0025'tedir.
12. **Okuma yetkisi:** Normal kullanıcı hiçbir hata bildirimini (başkalarınınkini de kendininkini de)
    okuyamaz; okuma yalnızca `admin.panel` yetkisiyle mümkündür (yetki iskeleti 0011).
13. **Oturum:** Giriş yapılmamış ziyaretçiye (ör. giriş sayfasında) "Hata bildir" gösterilmez; giriş
    yapılmamış ya da çıkış yapmış kullanıcının web'den ve ajandan gönderdiği bildirim reddedilir. Giriş
    sorunları için bildirim yolu ileride ayrı karar konusudur.
14. **Günlük kaydı:** Web ya da ajan bildirimi API'ye ulaştığında kullanıcının Günlük'üne Bilgi seviyesinde
    "Hata bildirimi gönderildi" düşer.
15. **Kötüye kullanım sınırı:** Bir kullanıcı saatte en fazla 10 hata bildirimi gönderebilir (web + ajan
    toplamı); sınır aşılınca "Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin" mesajı gösterilir ve
    kayıt oluşmaz. Ajan bu mesajı gösterir, bildirimi bekletmeye almaz (dosyaya kaydetme seçeneği kalır).
    API'nin otomatik kayıtları bu sınıra sayılmaz.

## User experience
- Flow (web): Sağ üstte "Hata bildir" → modal açılır: açıklama alanı + otomatik eklenecek bilgiler (sayfa,
  versiyon, zaman, son 20 Günlük kaydı, varsa korelasyon numarası) → "Gönder" → gönderiliyor → "Bildiriminiz
  alındı (No: …)" → modal kapanır, Günlük'e "Hata bildirimi gönderildi" düşer.
- Flow (hata mesajından): Bir işlem ProblemDetails hatasıyla döner → hata mesajında "Bildir" → modal
  korelasyon numarası dolu açılır → yukarıdaki akış.
- Flow (ajan): Ajanda hata → ajan penceresi öne gelir + Windows bildirimi + tray işareti: maskelenmiş hata
  özeti + "Ayrıntıları göster" + "Bildir" / "Kapat" / "Hata raporunu dosyaya kaydet" → "Bildir" → gönderildi
  (ya da "Bağlantı kurulunca gönderilecek") → bağlantı + eşleşme gelince gönderilir.
- States:
  - Web hata bildir modalı: açık (boş) · korelasyon numarası dolu açık · doğrulama hatası (açıklama boş/kısa/
    uzun) · gönderiliyor (düğme pasif) · başarılı (numara) · sunucu hatası (yazılan metin korunur, tekrar
    dene) · sınır aşıldı.
  - Ajan hata uyarısı: hata gösteriliyor · ayrıntılar açık · gönderiliyor · gönderildi · bekletiliyor
    (bağlantı/eşleşme yok) · gönderilemedi (bekletmeye alınır) · başka hesapta oluşturuldu (gönderilmedi) ·
    sınır aşıldı · dosyaya kaydedildi.
  - Yetkisiz: giriş yapılmamışsa "Hata bildir" görünmez; istek reddedilir (default-deny).
- Design reference: none — the flow above is the design. `source/*.png` (commit `61cc824`) yalnızca sağ
  üstteki "hata bildir" yerini gösterir; modal ve ajan penceresi için görsel yok. Bir eskiz verilirse `source/`
  altına tarihli görsel eklenip commit'i bu spec'e yazılır. Renk: tema değişkenleri, turuncu/kahve, açık mod
  esas (K-7).
- Accessibility: "Hata bildir" klavyeyle erişilebilir (Tab, Enter/Space); modal açılınca odak içine alınır,
  Esc kapatır, odak düğmeye döner. Doğrulama mesajları alana bağlıdır ve ekran okuyucuya duyurulur. Ajan
  penceresinin düğmeleri klavyeyle erişilebilir ve erişilebilir ada sahiptir.

## Constraints & out of scope
- Sensitive area: H-2 ve H-1 (bkz. başlık).
- Tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile (`fetch` yok, K-6). Arayüz metinleri Türkçe, resx'te.
  Zamanlar UTC saklanır, Europe/Istanbul ile gösterilir.
- Kullanıcı metni ve hata özetleri düz metin gösterilir (`Html.Raw` yasak).
- Out of scope: admin'in hata bildirimlerini listelemesi, okuması, durum değiştirmesi (0025); ban'lı
  kullanıcının bildiriminin reddi (0024; ban orada gelir — Ç-12); kullanıcının kendi bildirimlerinin durumunu
  takip etmesi; ekran görüntüsü ekleme; giriş ekranından (oturumsuz) bildirim; ajan kurulum/güncelleme
  ekranı (0009 — bu spec yalnızca ajan hata uyarısı ve bildirim mekanizmasını sağlar); API otomatik
  kayıtlarının gruplanması/tekilleştirilmesi; harici log/izleme servisleri.
- Split: done — 0022 Hata bildirimi web + ajan + API otomatik / kardeşler: 0008, 0021

## Acceptance criteria
- [ ] AC-1 — "Hata bildir" modalı açılır (klavyeyle de) ve otomatik eklenecek bilgileri (sayfa, versiyon, zaman, son 20 Günlük kaydı, varsa son hatanın korelasyon numarası) gösterir; geçerli açıklamayla "Gönder" bildirim numarası gösterir; kayıt gönderen kullanıcı, kaynak "web", "Yeni" durumu ve bu bilgilerle saklanır; Günlük'e Bilgi seviyesinde "Hata bildirimi gönderildi" düşer.
- [ ] AC-2 — İptal ya da Esc hiçbir kayıt oluşturmadan modalı kapatır, odak "Hata bildir" düğmesine döner ve modal yeniden açıldığında açıklama alanı boştur.
- [ ] AC-3 — Boş, 9 karakterlik ve 4001 karakterlik açıklama Türkçe doğrulama mesajıyla reddedilir ve kayıt oluşmaz; 10 ve 4000 karakterlik açıklama kabul edilir; sunucu hatasında yazılan metin korunur; gönderiliyorken düğme pasiftir ve çift tıklama tek kayıt oluşturur.
- [ ] AC-4 — ProblemDetails hata mesajındaki "Bildir" bağlantısı modalı o hatanın korelasyon numarası dolu olarak açar ve gönderilen kayıtta bu numara bulunur; modalda ekran görüntüsü ekleme seçeneği yoktur.
- [ ] AC-5 — Ajanda bir hata olduğunda ajan penceresi öne gelir, Windows bildirimi gösterilir ve tray ikonunda bekleyen bildirim işareti görünür; pencerede maskelenmiş hata özeti, "Bildir", "Kapat", "Hata raporunu dosyaya kaydet" ve "Ayrıntıları göster" bulunur; "Ayrıntıları göster" gönderilecek metnin tamamını gösterir; "Bildir" kaynak "ajan", ajan versiyonu, işletim sistemi ve hata özetiyle bir kayıt oluşturur; "Kapat" hiçbir kayıt oluşturmaz.
- [ ] AC-6 — Ajan bildirimine yalnızca son 15 dakikanın Warning, Error ve Critical log satırları eklenir: 16 dakika önceki satır ve Information satırı eklenmez; 200'den fazla uygun satır varsa en yeni 200 satır gider; `C:\Users\<ad>\…` yolu `%USERPROFILE%\…` olarak görünür.
- [ ] AC-7 — Aynı hata özeti aynı ajan oturumunda ikinci kez oluştuğunda uyarı yeniden gösterilmez ve teknik loga yazılır; ajan yeniden başlatıldıktan sonra aynı hata yeniden sorulur.
- [ ] AC-8 — Ajan API'ye bağlı değilken ya da eşleşmemişken seçilen bildirim "Bağlantı kurulunca gönderilecek" ile bekletilir; ajan yeniden başlatılsa da kaybolmaz; bağlantı ve eşleşme kurulunca tam bir kez gönderilir; ajan kimliği olmadan bildirim gönderen istek reddedilir (kimliksiz uç yoktur).
- [ ] AC-9 — A hesabıyla eşleşmişken bekletilen bildirim, ajan B hesabıyla eşleşince gönderilmez ve kullanıcı "Bu bildirim başka bir hesapta oluşturuldu, gönderilmedi" bilgisini görür; eşleşme öncesi oluşan bildirim ilk eşleşen hesap adına kaydedilir.
- [ ] AC-10 — Ajanda 20 bekleyen bildirim varken 21. eklenince en eski bildirim düşer, kuyrukta 20 kalır ve teknik loga Warning yazılır.
- [ ] AC-11 — "Hata raporunu dosyaya kaydet" maskelenmiş raporu (gönderilecek metnin aynısı) kullanıcının seçtiği dosyaya yazar; API'de kayıt oluşmaz.
- [ ] AC-12 — Bir kullanıcının bir saat içindeki 10. bildirimi (web + ajan toplamı) kabul edilir, 11. bildirimi "Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin" mesajıyla reddedilir ve kayıt oluşmaz; ajan reddedilen bildirimi bekletmeye almaz; API'nin otomatik kayıtları bu sayıya katılmaz.
- [ ] AC-13 — API'de beklenmeyen bir hata (Error/Critical) oluştuğunda kaynak "API", "Yeni" durumu, korelasyon numarası, ürün versiyonu, maskelenmiş hata özeti ve istek oturumluysa kullanıcıyla tek bir kayıt oluşur; doğrulama hatası, yetkisiz ve bulunamadı gibi 4xx yanıtlar kayıt oluşturmaz; tüm kayıtların kaynak alanı yalnızca web / ajan / API değerlerinden biridir.
- [ ] AC-14 — Giriş yapılmamış ziyaretçiye "Hata bildir" gösterilmez; giriş yapılmamış ya da çıkış yapmış kullanıcının web'den ve ajandan gönderdiği bildirim reddedilir; normal kullanıcı hiçbir hata bildirimini (kendininkini ve başkasınınkini) hiçbir yoldan (doğrudan istek dahil) okuyamaz; okuma isteği `admin.panel` yetkisi olmadan reddedilir.
- [ ] AC-15 — Açıklamaya, otomatik eklenen bilgilere ya da ajan log satırlarına yerleştirilmiş 0008 maskeleme listesindeki değerler (ör. maskesiz API key, JWT) kayıtta ve dosyaya kaydedilen raporda maskeli bulunur (H-2).
- [ ] AC-16 — Bileşen adı ve adım (indirme / doğrulama / kurulum) bilgisiyle açılan bir ajan bildirimi bu iki alanla birlikte saklanır; bu bildirimde ve diğer ajan bildirimlerinde (kayıtta ve dosyaya kaydedilen raporda) makine adı geçmez.

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
