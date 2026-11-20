# Spec 0017 — Yönetici paneli: günlük özet, temel raporlar ve Excel'e aktarma

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0017-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Kurucu sabah okula geldiğinde "bugün kim gelmedi, hangi sınıfın yoklaması alınmadı, kimin ödemesi gecikti, hangi
duyuruyu veliler okumadı" sorularının cevabını bugün öğretmenlere ve muhasebeye tek tek sorarak buluyor; ay sonunda
devamsızlık ve tahsilat listelerini elle Excel'e döküyor. Bu spec yol haritasının 11. satırını getirir: kurucuya web'de
tek sayfalık **günlük özet** (devamsızlık, geciken ödemeler, okunmayan duyurular), üç **temel rapor** (devamsızlık,
tahsilat, borç/gecikme) ve her raporu **Excel'de açılan dosya** olarak indirme. Panel yalnızca okur; hiçbir kaydı
değiştirmez. Başarı: kurucu sabah paneli açar, 2 saniyede "3 sınıfta yoklama alınmadı, 12 öğrenci yok, 18 öğrencinin
41.500 TL ödemesi gecikti, 2 duyuruyu 9 veli okumadı" görür; ay sonunda tahsilat raporunu indirip Excel'de Türkçe
karakterleri ve tutarları düzgün görür. Bilinçli olarak yapılmayanlar: grafikler, doğal dilde rapor (AI, Faz 2), şubeler
arası konsolide rapor (Faz 3), ödev/not ve randevu özetleri (o modüller henüz yok), zamanlanmış e-posta raporu, gerçek
.xlsx dosyası.

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Panel kimin için?** Yalnızca **kurucu/yönetici** (okulun bütün şubeleri, şube süzgeciyle). Muhasebe kendi
  tahsilat ekranlarını kullanır (0015), öğretmen ve veli paneli görmez. Neden: kapsam "Kurucu / Yönetici — raporlar";
  devamsızlık + borç + iletişimi bir arada görmek kurucunun ihtiyacı; en dar yetki.
  - Dokümanda istenen: "Kurucu / Yönetici — Kayıt, tahsilat, raporlar, ayarlar — Web"; "Yönetici paneli — Günlük özet".
- **C-2 — Günlük özette ne var?** Yalnızca bugün var olan modüllerden üç kart: **devamsızlık** (yoklama, 0010),
  **geciken ödemeler** (0015), **okunmayan duyurular** (0011). Ödev/not, randevu, AI özeti o modüller gelince eklenir.
  "Bugün" Türkiye saatine göredir; ekran açılınca ve "Yenile" ile güncellenir (canlı güncelleme yok). Neden: kapsamın
  saydığı üç madde tam bu; olmayan veriyi göstermek boş kart demek.
  - Dokümanda istenen: "Günlük özet: devamsızlık, geciken ödemeler, okunmayan duyurular".
- **C-3 — Kartların içeriği?** Devamsızlık: yoklaması alınan/alınmayan sınıf sayısı ve alınmayanların listesi, yok ve
  geç sayısı, yok öğrencilerin listesi (ad, sınıf). Geciken ödemeler: öğrenci sayısı, toplam geciken tutar, en eski
  vadeden başlayan ilk 10 öğrenci. Okunmayan duyurular: son 7 günde yayınlanmış, en az bir velinin okumadığı duyurular
  (başlık, tarih, "X / Y okudu"), en çok okunmayandan başlayarak ilk 10. Neden: kurucunun sabah aksiyon alacağı bilgi;
  liste uzunsa ayrıntı raporda.
  - Dokümanda istenen: aynı satır; 0011 C-3: "ileride yönetici panelinin 'okunmayan duyurular' özeti için tutarlı sayı".
- **C-4 — Hangi raporlar?** Üç temel rapor, tarih aralığıyla: **Devamsızlık raporu** (öğrenci başına yok/geç gün
  sayısı, şube/sınıf süzgeci, "en az N gün yok" eşiği), **Tahsilat raporu** (aralıktaki tahsilatlar, iptaller ayrı
  işaretli, yönteme göre toplamlar), **Borç/gecikme raporu** (bugün itibarıyla öğrenci başına net, ödenen, kalan,
  geciken; "yalnızca gecikenler"). Aralık en fazla 366 gün; varsayılan bu ay. Neden: okulun ay sonu ve dönem sonu
  ihtiyacı; kapsamın AI örneği ("3 günden fazla devamsızlık yapan") eşikle bugünden karşılanır.
  - Dokümanda istenen: "Temel raporlar ve Excel dışa aktarma"; AI tablosu: "Bu ay 3 günden fazla devamsızlık yapan 7.
    sınıflar".
- **C-5 — Excel'e aktarma nasıl?** **CSV dosyası**: UTF-8 BOM'lu, ayırıcı noktalı virgül (;), satır sonu CRLF, başlık
  satırı Türkçe, tutar "1234,50" (ondalık virgül, binlik ayırıcı yok), tarih "GG.AA.YYYY"; Türkçe Excel çift tıkla doğru
  sütunlarla açar. Dosyayı sunucu üretir, ekrandaki süzgeçlerle aynı satırların tamamını (sayfalamasız) verir; en fazla
  10.000 satır. Yeni paket yok. Gerçek .xlsx (ör. MIT lisanslı ClosedXML) okullar biçimli dosya isterse sonra. Neden:
  bağımlılık onayı, lisans/CVE denetimi ve `npm audit`/NuGet riski olmadan; birkaç düzine satır kod, kolay test; BOM +
  noktalı virgül Türkçe Excel'in ayırıcı ve kodlama beklentisini karşılar.
  - Dokümanda istenen: "Temel raporlar ve Excel dışa aktarma"; güvenlik: "Dışa aktarma: `=`, `+`, `-`, `@` ile başlayan
    hücreler kaçışlanır (CSV/formül enjeksiyonu)"; bağımlılık: "Yeni NuGet/npm paketi plan onayında açıkça listelenir".
- **C-6 — Dışa aktarılan kişisel veri nasıl korunur?** Raporlarda yalnızca ad-soyad, okul numarası, şube/sınıf,
  sayılar ve tutarlar; TC kimlik no, telefon, e-posta, sağlık bilgisi yok. Her dışa aktarma audit'e yazılır (kim, hangi
  rapor, süzgeçler, satır sayısı, ne zaman, IP). Neden: dosya sistemden çıkan kişisel veridir; KVKK'da "kim neyi aldı"
  sorusunun cevabı gerekir.
  - Dokümanda istenen: "Audit log: kim, hangi öğrenci kaydını, ne zaman, görüntüledi/değiştirdi"; S2.
- **C-7 — Kanal?** Yalnızca web. Mobilde yönetici paneli sonra. Neden: kapsam "Kurucu — Web"; tablolar geniş ekranda.
  - Dokümanda istenen: rol tablosu "Kurucu / Yönetici — Web".

## Requirements
1. **Erişim:** Panel, raporlar ve dışa aktarma yalnızca seçili okulun kurucusuna açıktır (her istekte veritabanından);
   öğretmen, veli, muhasebe 403; girişsiz 401. Hoş geldin'de "Yönetici paneli" bağlantısı yalnızca kurucuda.
2. **Şube süzgeci:** Özet ve raporlar "Bütün şubeler" ya da tek etkin şube için gösterilir. Başka okulun şubesi 404.
   Okul geneli duyuru her süzgeçte görünür; şube/sınıf/öğrenci kapsamlı duyuru yalnızca kendi şubesinin süzgecinde.
3. **Devamsızlık kartı (bugün):** Güncel dönemde etkin ve en az bir öğrencisi olan sınıflardan yoklaması alınan ve
   alınmayan sayısı, alınmayanların listesi (şube, sınıf, atanmış öğretmen adı); yok ve geç toplamı; yok öğrencilerin
   listesi (ad-soyad, şube, sınıf). Bugün okul günü değilse (dönem dışı ya da tatil) kart "Bugün okul günü değil" der.
   Sınıf yoksa "Henüz sınıf yok".
4. **Geciken ödemeler kartı:** Gecikmesi olan öğrenci sayısı ve toplam geciken tutar (0015'in gecikme tanımı: iptal
   plan ve iptal tahsilat sayılmaz); en eski geciken vadeden başlayarak ilk 10 öğrenci (ad-soyad, sınıf, geciken tutar,
   en eski vade, gecikme gün sayısı) ve "Tümünü gör" → borç raporu "yalnızca gecikenler". Yoksa "Geciken ödeme yok".
5. **Okunmayan duyurular kartı:** Son 7 günde (bugün dahil) yayınlanmış ve en az bir alıcı velinin okumadığı duyuru
   sayısı ve toplam okumayan veli; en çok okunmayandan ilk 10 (başlık, yayın tarihi, yayınlayan, "X / Y okudu") ve
   duyuru ayrıntısına (0011) bağlantı. Yoksa "Okunmayan duyuru yok".
6. **Rapor süzgeçleri:** Başlangıç ve bitiş tarihi zorunlu, başlangıç ≤ bitiş, aralık en fazla 366 gün; varsayılan bu
   ayın ilk günü – bugün. Ekranda 50'lik sayfa; toplamlar bütün sonuç üzerinden.
7. **Devamsızlık raporu:** Aralıkta alınmış yoklamalardan öğrenci başına yok ve geç gün sayısı; şube, sınıf ve "en az N
   gün yok" (0–366) süzgeci; yok sayısına göre azalan, eşitlikte Türkçe ad sırası; sütunlar: ad-soyad, okul no, şube,
   sınıf, yok, geç. Hiç yok/geç kaydı olmayan öğrenci listede yok.
8. **Tahsilat raporu:** Tahsilat tarihi aralıkta olan bütün tahsilatlar: tarih, makbuz no, öğrenci, şube, tutar, yöntem,
   durum (Geçerli / İptal), kaydeden; tarih ve makbuz no sırasıyla. Toplamlar: geçerli tahsilat toplamı ve yönteme göre
   toplamlar; iptaller toplama girmez.
9. **Borç/gecikme raporu:** Bugün itibarıyla geçerli planı olan öğrenciler: ad-soyad, şube, sınıf, plan adı, net,
   ödenen, kalan, geciken, en eski geciken vade; "yalnızca gecikenler" süzgeci; toplamlar. Tarih aralığı bu raporda yok.
10. **Excel'e aktarma:** Her raporda "Excel'e aktar" o anki süzgeçlerle C-5'teki biçimde dosya indirir; dosya adı
    `<rapor>-<GG.AA.YYYY>.csv` (ör. `tahsilat-raporu-20.11.2026.csv`); son satırda toplamlar (raporda varsa). 10.000
    satırı aşan sonuç "Sonuç çok büyük (en fazla 10.000 satır); tarih aralığını ya da süzgeci daraltın." ile reddedilir.
    `=`, `+`, `-`, `@`, sekme ya da satır başı karakteriyle başlayan metin hücresi başına `'` eklenerek kaçışlanır.
11. **Audit ve gizlilik:** Her dışa aktarma audit'e yazılır (kim, rapor türü, süzgeçler, satır sayısı, ne zaman, IP).
    Panel ve raporlarda TC, telefon, e-posta, sağlık bilgisi yok; loglarda ad-soyad yok (yalnızca kimlikler).
12. **Hız:** 1.000 öğrenci, 40 sınıf, 2.000 taksit ve 200 duyurulu okulda günlük özet ≤ 2 sn, 10.000 satırlık dışa
    aktarma ≤ 5 sn (yerel ölçüm).

## User experience
- **Flow — web (kurucu):** Hoş geldin → "Yönetici paneli" → `#/panel`: üstte şube süzgeci + "Yenile" + bugünün tarihi;
  üç kart (devamsızlık, geciken ödemeler, okunmayan duyurular), her kartta liste ve "Tümünü gör" → "Raporlar" sekmesi
  (`#/panel/raporlar`): rapor seçimi (Devamsızlık / Tahsilat / Borç-gecikme) → süzgeçler → "Göster" → tablo + toplamlar
  + sayfalama → "Excel'e aktar" → dosya iner.
- **States:** her kart ve rapor: yükleniyor / boş (gereksinim 3–5'teki iletiler; raporlarda "Bu süzgeçlere uyan kayıt
  yok") / dolu / hata + "Tekrar dene" (bir kartın hatası diğer kartları bozmaz); "Bugün okul günü değil"; süzgeç alan
  hataları (istek gönderilmez); dışa aktarma: hazırlanıyor (düğme kilitli) / indirildi / "Sonuç çok büyük…" hatası;
  yetkisiz kişi `#/panel`'de "Bu sayfaya erişiminiz yok.".
- **Design reference:** none — the flow above is the design (spec 0005 yönetim sayfası sekme/tablo kalıbı, ortak tema).
- **Accessibility:** yalnızca klavyeyle kullanılır; kart başlıkları başlık öğesi; tablolarda başlık hücreleri; sayılar
  metinle ("12 öğrenci yok"), renk tek başına anlam taşımaz; iki temada kontrast spec 0002 eşiklerinde; 320 px'te sayfa
  yatay kaymaz (tablolar kendi içinde kayar).

## Constraints & out of scope
- Sensitive area: S1 (okul/şube izolasyonu — modüller arası okuma), S2 (öğrenci ad-soyad, devamsızlık; dışa aktarılan
  dosya), S4 (tahsilat ve borç tutarları), S6 (dışa aktarma audit'i).
- **Bağımlılık:** spec 0010 (yoklama, öğretmen–sınıf ataması, okul günü), 0011 (duyuru alıcıları ve okundu bilgisi) ve
  0015 (plan, taksit, tahsilat, gecikme tanımı) — ve onların dayandığı 0008/0009 — önce biter. Panel yalnızca bunların
  sorgularını okur; ödev/not (0013), randevu (0016) ve AI (0012/0014) bu spec'e girmez.
- **Kapsam dışı / sonra:**
  - Gerçek .xlsx (biçim, birden çok sayfa); PDF rapor; zamanlanmış/e-postayla rapor; grafikler ve eğilimler.
  - Ödev/not, randevu, AI özetleri kartları (o modüller gelince ayrı küçük spec).
  - Muhasebe ve öğretmen için panel; mobil yönetici paneli; canlı güncelleme.
  - Duyuru okuma raporu (kart + 0011 ayrıntısı yeterli), sınıf bazlı yoklama yüzdeleri, devamsızlık eşiğinde otomatik
    uyarı, doğal dilde rapor ve tahsilat riski (AI, Faz 2), şubeler arası konsolide rapor (Faz 3).
  - Panelden işlem (hatırlatma gönderme, yoklama alma) — panel yalnızca okur.
- Split: proposed 0017a "Günlük özet" ve 0017b "Temel raporlar ve Excel'e aktarma"; kept as one — tahamucasiroglu
  (önceden verilen onay, Promtlar 0082), 2026-11-20, aynı sayfa, aynı süzgeç ve aynı okuma sorguları; 13 kriterde kalıyor.

## Acceptance criteria
- [ ] AC-1 — Yetki: öğretmen, veli ve muhasebe panel, rapor ve dışa aktarma isteklerinde 403 alır; girişsiz istek 401; "Yönetici paneli" bağlantısı yalnızca kurucuda, başkası `#/panel`'de "Bu sayfaya erişiminiz yok." görür; kurucu rolü alınan kişinin bir sonraki isteği reddedilir.
- [ ] AC-2 — İzolasyon: A okulunun kurucusunun özetinde, raporlarında ve dosyalarında B okulunun hiçbir sınıfı, öğrencisi, tahsilatı ya da duyurusu yoktur (B'de veri dolu iken); B'nin şube kimliğiyle süzgeç 404; tek şube süzgecinde başka şubenin yoklama, borç ve şube/sınıf/öğrenci kapsamlı duyuru satırları yoktur, okul geneli duyurular vardır.
- [ ] AC-3 — Devamsızlık kartı: bilinen test verisinde alınan/alınmayan sınıf sayısı, alınmayanların listesi, yok ve geç toplamı ve yok öğrenci listesi doğrudur; "bugün" Türkiye saatine göredir (UTC'de önceki gün olan gece saatlerinde de); tatil ya da dönem dışı günde "Bugün okul günü değil"; sınıf yokken "Henüz sınıf yok"; öğrencisiz ve pasif sınıf "alınmadı" sayılmaz.
- [ ] AC-4 — Geciken ödemeler kartı: gecikmeli öğrenci sayısı ve toplam geciken tutar 0015'in öğrenci carilerindeki geciken tutarların toplamına eşittir; iptal plan ve iptal tahsilat hesabı bozmaz; vadesi bugün olan taksit sayılmaz; liste en eski vadeden ilk 10 ve gün sayısı doğru; gecikme yokken "Geciken ödeme yok".
- [ ] AC-5 — Okunmayan duyurular kartı: son 7 günde (bugün dahil; 8 gün önceki dahil değil) yayınlanan ve okumayanı olan duyurular "X / Y okudu" ile en çok okunmayandan ilk 10 listelenir; herkesin okuduğu duyuru listede yok; sayılar 0011'in duyuru sonuç ekranıyla aynıdır; yoksa "Okunmayan duyuru yok".
- [ ] AC-6 — Rapor süzgeçleri: başlangıç > bitiş, 367 günlük aralık, eksik tarih alan hatası verir ve istek gönderilmez (sunucu da reddeder); 366 gün kabul; varsayılan aralık bu ayın ilk günü – bugün; 50'lik sayfalama ve toplamlar bütün sonuç üzerinden doğrudur; sonuç yokken "Bu süzgeçlere uyan kayıt yok".
- [ ] AC-7 — Devamsızlık raporu: aralıktaki yoklamalardan öğrenci başına yok/geç gün sayıları doğrudur (aralık dışındaki gün sayılmaz, sınırdaki gün sayılır); şube/sınıf süzgeci ve "en az 3 gün yok" eşiği doğru öğrencileri bırakır; sıralama yok sayısına göre azalan, eşitlikte Türkçe ad sırasıyla (Ç, Ş, İ, Ö, Ü doğru yerde).
- [ ] AC-8 — Tahsilat raporu: aralıktaki tahsilatlar (sınır günleri dahil) makbuz no ile listelenir; iptal tahsilat "İptal" olarak görünür ve toplamlara girmez; geçerli toplam ve yönteme göre toplamlar doğrudur.
- [ ] AC-9 — Borç/gecikme raporu: öğrenci başına net, ödenen, kalan, geciken ve en eski geciken vade 0015 carisiyle aynıdır; iptal plan listede yok; "yalnızca gecikenler" süzgeci ve toplamlar doğrudur.
- [ ] AC-10 — Excel'e aktarma: dosya UTF-8 BOM ile başlar, ayırıcı `;`, satır sonu CRLF, ilk satır Türkçe başlıklar, tutar `1234,50`, tarih `GG.AA.YYYY`, `;` ya da `"` içeren metin tırnaklanır; satırlar ekrandaki süzgeçle aynı ve sayfalamasız tamamıdır, toplam satırı sonda; dosya adı kuralı uyar; Türkçe Excel'de çift tıkla açıldığında Türkçe karakterler ve sütunlar doğru görünür (VERIFY gözlemi, ekran görüntüsü).
- [ ] AC-11 — Dışa aktarma güvenliği: `=1+1`, `+90…`, `-5`, `@SUM(…)`, sekme ve satır başıyla başlayan ad hücreleri `'` ile kaçışlanır (tutar sütunları sayı olarak kalır); 10.001 satırlık sonuç "Sonuç çok büyük (en fazla 10.000 satır); …" ile reddedilir, 10.000 kabul; her başarılı dışa aktarma audit kaydı oluşturur (kim, rapor, süzgeçler, satır sayısı, ne zaman, IP), reddedilen oluşturmaz; dosyada ve panelde TC, telefon, e-posta, sağlık bilgisi yok; loglarda ad-soyad yok.
- [ ] AC-12 — Hız: gereksinim 12'deki test verisinde günlük özet ≤ 2 sn, 10.000 satırlık dışa aktarma ≤ 5 sn (yerel ölçüm, sürelerin yazıldığı test çıktısı).
- [ ] AC-13 — Ekran durumları (iki tema): panel ve raporlar sekmesinde her kart/rapor için yükleniyor / boş / dolu / hata + tekrar dene (bir kartın hatası diğerlerini bozmaz), "Bugün okul günü değil", "Yenile", şube süzgeci, rapor süzgeç alan hataları, dışa aktarma hazırlanıyor (düğme kilitli) / "Sonuç çok büyük…" hatası; akış yalnızca klavyeyle; 320 px'te sayfa yatay kaymaz.

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Özet hangi döneme bakar?" → devamsızlık kartı güncel döneme (0008 tanımı); dönem yoksa "Bugün okul günü değil".
- "Ayrılmış öğrenci raporda?" → devamsızlık raporunda aralıktaki yoklama kaydı varsa evet (geçmiş doğru kalır); borç
  raporunda geçerli planı varsa evet (borç sürer).
- "Devamsızlık raporu gün mü, kayıt mı sayar?" → gün (0010 günlük yoklama).
- "Okundu oranında payda?" → 0011'deki alıcı veli sayısı (yayın anında sabit).
- "Excel'de sayılar metin mi görünür?" → tutar sütunu ondalık virgüllü sayı, Türkçe Excel sayı olarak okur; kaçış yalnız
  metin sütunlarında (negatif tutar yok).
- "İngilizce Excel'de?" → ayırıcı farklı olduğu için "Veri → Metinden" ile açılır; okul kullanıcıları Türkçe (bilinen sınır).
- "Dosyayı kim indirebilir, link paylaşılır mı?" → dosya istek yanıtında iner, sunucuda saklanmaz, paylaşım linki yok.

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
