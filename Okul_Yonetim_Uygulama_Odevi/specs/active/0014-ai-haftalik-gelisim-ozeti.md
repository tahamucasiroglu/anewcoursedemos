# Spec 0014 — AI: haftalık gelişim özeti

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0014-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Veli çocuğunun haftasını parça parça bildirimlerden öğreniyor; öğretmenin her veliye haftalık birkaç cümle yazmaya vakti
yok. Bu spec yol haritasının 8. satırını getirir: sınıf öğretmeni web'de sınıfını açar, bir öğrenci için "Özet oluştur"a
basar; yerel model (spec 0012'nin AI katmanı, Ollama `gemma3:4b`) o haftanın **yoklama** (0010), **ödev ve not** (0013,
varsa) verisinden ve öğretmenin isteğe bağlı kısa **notundan** 3–5 cümlelik bir **özet taslağı** yazar. Öğretmen dayanak
verinin yanında taslağı okur, düzeltir ve onaylar; özet o çocuğun velilerine uygulama içinde "Haftalık gelişim özeti" olarak
gider. Başarı: öğretmen bir öğrenci için ~1 dakikada özet gönderir; veli "Ali bu hafta 4 gün okuldaydı, 1 gün gelmedi; 2
ödevini teslim etti…" gibi birkaç cümle ve dayanağını görür. Bilinçli olarak yapılmayanlar: otomatik (zamanlanmış) haftalık
üretim, sınıfa toplu üretim, öğretmen onayı olmadan gönderim, ayrıntılı analiz/grafik ("bir kaç söylese bile yeter"),
dış AI servisi, push/e-posta ile gönderim.

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Veliye gitmeden önce kim onaylar?** **Sınıf öğretmeni** (0010'daki sınıf öğretmeni ataması) her özeti okuyup
  onaylar; "AI taslağı" etiketiyle otomatik gönderim yok. Sınıfa atanmış her öğretmen aynı taslağı görür ve onaylayabilir.
  Neden: BR-5 "AI hiçbir mesajı onaysız veliye göndermez" — özet de veliye giden AI çıktısı; öğrenciyi tanıyan kişi
  sınıf öğretmeni.
  - Dokümanda istenen: "AI hiçbir mesajı onaysız veliye göndermez"; domain: "AI taslağı (`AiDraft`) — insan onayı bekleyen
    çıktı"; "haftalık gelişimde bir kaç söylese bile yeter mvp için." (Promtlar 0082).
- **C-2 — Hangi veriler, hangi hafta?** Hafta Pazartesi 00:00 – Pazar 23:59 (Türkiye saati); öğretmen "Bu hafta" ya da
  "Geçen hafta"yı seçer. Dayanak: o haftanın yoklaması (yoklama alınan gün sayısı, var/yok/geç sayıları ve yok/geç
  günleri), o haftaya ait ödev ve notlar (0013 verisi varsa; yoksa bu satırlar yer almaz) ve öğretmenin o özet için yazdığı
  isteğe bağlı not (en fazla 500 karakter). Neden: kapsamın saydığı kaynaklar; ayrı bir "öğretmen notu" modülü olmadan
  öğretmen notu en sade hâliyle özetin girdisi olur.
  - Dokümanda istenen: "Haftalık gelişim özeti | Veli | Yoklama, ödev, not ve öğretmen notlarından çocuğa özel haftalık
    özet üretir".
- **C-3 — Nasıl tetiklenir?** İsteğe bağlı, öğrenci başına: öğretmen web'deki sınıf listesinde öğrenciyi seçip "Özet
  oluştur"a basar (~5–10 sn). Zamanlanmış üretim ve "sınıfın hepsi için oluştur" yok. Neden: zamanlayıcı ve toplu iş kuyruğu
  gerektirmez; tek ekran kartında öğrenci başına beklemek kabul edilebilir.
  - Dokümanda istenen: "çocuğa özel haftalık özet".
- **C-4 — Ne kadar ayrıntı?** 3–5 cümle, olumlu ama dürüst, yalnızca verilen veriye dayanan; model en fazla 800 karakter;
  öğretmen düzenlerken en fazla 1500 karakter. Neden: "bir kaç söylese bile yeter".
  - Dokümanda istenen: "haftalık gelişimde bir kaç söylese bile yeter mvp için."
- **C-5 — Veliye nasıl ulaşır, ne görünür?** Onaylanan özet spec 0011'in **bildirim kapısından** öğrencinin okuldaki etkin
  her velisine gider ve velinin duyuru akışında "Haftalık gelişim özeti — 16–22 Kasım" başlıklı okul bildirimi olarak
  görünür. Metnin altında: "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." ve dayanak satırı (ör. "Dayanak: 4 gün
  geldi, 1 gün gelmedi; 2 ödev; 1 not; öğretmen notu"). Öğrenci-hafta başına bir özet gönderilir. Neden: veliye giden her
  şeyin tek çıkışı Communication; BR-6 kaynak verinin görünmesini ister.
  - Dokümanda istenen: "Her AI çıktısında kaynak veri görünür"; mimari: "Communication … veliye giden her şeyin tek çıkışı".
- **C-6 — Veri yoksa ya da AI yoksa?** Haftada yoklama kaydı, ödev/not ve öğretmen notu hiç yoksa "Bu hafta için
  özetlenecek veri yok." AI kapalıysa (Pi varsayılanı, spec 0012 C-2) "Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar
  deneyin."; var olan taslak yine onaylanıp gönderilebilir. Neden: uydurma özetin önüne geçer; Pi'de çökme yok.
  - Dokümanda istenen: "6 ve 8 de şimdilik bizim bilgisayara ollama kuralım."
- **C-7 — Hangi ekran hangi kanalda?** Öğretmen tarafı **web**'de (`#/haftalik-ozet`: sınıf → öğrenci listesi → özet paneli);
  veli tarafı **mobil** duyuru akışında (0011). Neden: dayanak + taslak + düzenleme geniş ekranda rahat; 0011'de duyuru
  yazımı da web'de; veli yalnızca mobil.
  - Dokümanda istenen: rol tablosu "Öğretmen … Mobil + Web"; "Veli: Takip, bildirim … Mobil".

## Requirements
1. **Sınıf ve öğrenci listesi:** Öğretmen yalnızca atandığı sınıfları görür; sınıfı ve haftayı ("Bu hafta"/"Geçen hafta")
   seçince öğrenciler Türkçe ad sırasıyla, her birinde durumla gelir: "Özet yok", "Taslak hazır", "Gönderildi".
2. **Dayanak verisi:** Seçilen öğrenci-hafta için dayanak toplanır ve öğretmene gösterilir: yoklama (alınan gün, var/yok/geç
   sayıları, yok ve geç günleri), o haftaya ait ödev ve notlar (0013 verisi varsa), öğretmen notu. Hafta sınırları Türkiye
   saatiyle Pazartesi 00:00 dahil – sonraki Pazartesi 00:00 hariç.
3. **Taslak üretimi:** "Özet oluştur" dayanaktan 3–5 cümlelik Türkçe taslak üretir (en fazla 800 karakter) ve dayanakla
   birlikte saklar; veliye hiçbir şey gitmez. Yeniden oluşturmak bekleyen taslağın yerine geçer. Dayanak hiç yoksa (öğretmen
   notu da yoksa) "Bu hafta için özetlenecek veri yok." ile reddedilir.
4. **Veri minimizasyonu:** Modele yalnızca öğrencinin adı (soyadı değil), dayanak verisi ve sabit talimat gider; soyad,
   sağlık notu, alerji, veli bilgisi, başka öğrenci verisi gitmez.
5. **Onay ve gönderim:** Öğretmen taslağı düzenler (boş olamaz, en fazla 1500 karakter) ve "Onayla ve gönder"e basar. Özet,
   öğrencinin okulda etkin her velisine C-5'teki başlık, etiket ve dayanak satırıyla bir okul bildirimi olarak gider; taslak
   son metin, onaylayan ve zamanla "onaylandı" olur. Aynı öğrenci-hafta için ikinci gönderim "Bu hafta için özet zaten
   gönderildi." ile reddedilir; iki öğretmen aynı anda gönderirse tek özet gider. "Vazgeç" bekleyen taslağı siler. Öğrencinin
   etkin velisi yoksa "Bu öğrencinin kayıtlı velisi yok." ile reddedilir.
6. **Yetki:** Yalnızca öğrencinin sınıfına atanmış öğretmen liste, dayanak, üretim ve gönderimi kullanır; veli, muhasebe ve
   sınıfa atanmamış kurucu kullanamaz; başka sınıfın öğrencisi ve başka okul "bulunamadı".
7. **Veli görünümü:** Veli özeti yalnızca kendi çocukları için görür (farklı şubedeki kardeş dahil, BR-2), başka velinin
   çocuğununkini göremez.
8. **AI kullanılamıyor:** Metin servisi kapalı/ayarsız/zaman aşımı/boş yanıtta "Yapay zekâ şu an kullanılamıyor. Daha sonra
   tekrar deneyin." döner, taslak oluşmaz (var olan bekleyen taslak korunur), Warning log yazılır.
9. **Gizlilik ve audit:** Özet metni, öğretmen notu, dayanak ve öğrenci adı log satırlarına yazılmaz (yalnızca kimlikler).
   Özet gönderimi audit'e yazılır (kim, hangi öğrenci, hangi hafta, ne zaman, IP).
10. **Duman testi:** Spec 0012'nin isteğe bağlı duman betiği, sabit örnek dayanaktan uygulamanın özet talimatıyla boş olmayan
    Türkçe özet üretir.

## User experience
- **Flow — web (öğretmen):** Hoş geldin → "Haftalık özet" (yalnızca sınıf ataması olan öğretmende) → `#/haftalik-ozet` →
  sınıf seç (tek sınıfsa doğrudan) → hafta seç ("Bu hafta" varsayılan) → öğrenci listesi (durumlarla) → öğrenci → panel:
  dayanak (yoklama, ödev/not, öğretmen notu alanı) → "Özet oluştur" → "Oluşturuluyor…" → taslak ("AI taslağı — göndermeden
  önce kontrol edin", düzenlenebilir) → "Onayla ve gönder" / "Yeniden oluştur" / "Vazgeç" → "Gönderildi" (salt okunur metin).
- **Flow — mobil (veli):** Duyurular → "Haftalık gelişim özeti — 16–22 Kasım" (çocuk adıyla) → ayrıntı: özet, etiket,
  dayanak satırı.
- **States:** liste yükleniyor / boş ("Bu sınıfta öğrenci yok") / dolu / hata + "Tekrar dene"; atanmış sınıf yoksa "Size
  atanmış sınıf yok."; panel: dayanak boş ("Bu hafta için özetlenecek veri yok." — oluştur kapalı, not yazınca açılır) /
  oluşturuluyor (düğmeler kilitli) / taslak hazır / alan hataları (not 500, metin boş ya da 1500 üstü — istek gitmez) /
  sunucu hatası ("Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar deneyin.", "Bu hafta için özet zaten gönderildi.",
  "Bu öğrencinin kayıtlı velisi yok.") / gönderildi; yetkisiz kişi `#/haftalik-ozet`'te "Bu sayfaya erişiminiz yok.".
- **Design reference:** none — the flow above is the design (0011 İletişim sayfası ve mobil akış kalıbı, ortak tema).
- **Accessibility:** web akışı yalnızca klavyeyle; durumlar metinle (renk tek başına anlam taşımaz); alan hataları alanla
  ilişkili ve duyurulur; "Oluşturuluyor…" duyurulur; iki temada kontrast spec 0002 eşiklerinde; 320 px'te yatay kayma yok.

## Constraints & out of scope
- Sensitive area: S5 (veliye giden AI çıktısı — BR-5, BR-6), S2 (öğrenci verisi modele), S1 (okul/şube/ilişki sınırı), S6 (audit).
- **Bağımlılık:** spec 0012 (AI katmanı, `AiDraft`, duman betiği), 0011 (bildirim kapısı, okul bildirimi, veli akışı), 0010
  (sınıf öğretmeni ataması, yoklama) önce biter. Spec 0013 (ödev ve not, yol haritası 7) paralel planlanıyor ve sırada
  önce gelir; build anında 0013 verisi yoksa ödev/not satırları dayanakta yer almaz (amendment ile kayda geçer).
- **Ortam:** üretim yalnızca AI açık bilgisayarda; Pi'de varsayılan "kullanılamıyor" (0012 C-2, LAN demo ayarı aynen).
- **Kapsam dışı / sonra:** zamanlanmış haftalık üretim; sınıfa toplu üretim; ayrı "öğretmen notları" (gözlem) modülü;
  grafik/eğilim; öğretmenin mobilden özet onayı; push/e-posta ile özet (B-6); gönderilmiş özeti düzeltme/geri çekme;
  geçmiş haftalar (yalnızca bu ve geçen hafta); velinin özet geçmişi ayrı ekranı (akışta zaten durur).
- Split: gerek yok — tek kullanıcı yeteneği, 12 kriter.

## Acceptance criteria
- [ ] AC-1 — Liste: öğretmen yalnızca atandığı sınıfları görür; seçilen sınıf ve haftada öğrenciler Türkçe ad sırasıyla "Özet yok" / "Taslak hazır" / "Gönderildi" durumlarıyla gelir; "Geçen hafta" seçimi bir önceki Pazartesi–Pazar'ı kullanır.
- [ ] AC-2 — Dayanak ve hafta sınırı: dayanak o haftanın yoklama sayılarını (alınan gün, var/yok/geç, yok/geç günleri), 0013 verisi varsa o haftanın ödev ve notlarını ve öğretmen notunu içerir; Pazartesi 00:00 (Türkiye saati) kaydı dahil, önceki Pazar 23:59 ve sonraki Pazartesi 00:00 kayıtları hariçtir; başka öğrencinin verisi yoktur.
- [ ] AC-3 — Taslak üretimi (BR-5): "Özet oluştur" dayanak ve taslakla "bekliyor" kaydı oluşturur, veliye bildirim gitmez ve bildirim kapısı çağrılmaz; model metni 800 karakteri aşarsa 800'e kısaltılır; yeniden oluşturma aynı öğrenci-hafta için tek bekleyen taslak bırakır.
- [ ] AC-4 — Veri yok: yoklama, ödev/not ve öğretmen notu olmayan öğrenci-haftada "Bu hafta için özetlenecek veri yok." döner, model çağrılmaz, taslak oluşmaz; yalnızca öğretmen notu varsa taslak üretilir.
- [ ] AC-5 — Veri minimizasyonu: metin servisine giden istek öğrencinin adını, dayanağı ve sabit talimatı içerir; soyadı, sağlık notu, alerji, veli adı/iletişim bilgisi ve başka öğrencinin adı içermez.
- [ ] AC-6 — Onay ve gönderim: onaylanan (düzenlenmiş) metin, öğrencinin okulda etkin her velisine başlık "Haftalık gelişim özeti — <hafta aralığı>", etiket "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." ve dayanak satırıyla birer okul bildirimi olarak gider (kapı veli başına bir kez çağrılır, pasif veliye gitmez); taslak son metin ve onaylayanla "onaylandı" olur; etkin velisi olmayan öğrencide "Bu öğrencinin kayıtlı velisi yok." ve hiçbir şey değişmez.
- [ ] AC-7 — Tekrar ve eşzamanlılık: aynı öğrenci-hafta için ikinci gönderim "Bu hafta için özet zaten gönderildi." ile reddedilir; aynı anda gelen iki gönderimden sonra her veliye tek özet gitmiş olur; gönderilmiş haftada "Özet oluştur" da aynı mesajla reddedilir; "Vazgeç" bekleyen taslağı siler.
- [ ] AC-8 — Metin sınırları: öğretmen notu 500 karakterde kabul, 501'de alan hatası; onay metni boş ya da 1500 karakteri aşarsa alan hatası (1500 kabul); hatada hiçbir şey değişmez.
- [ ] AC-9 — Yetki ve izolasyon: sınıfa atanmamış öğretmen ve atanmamış kurucu 403/404, veli ve muhasebe 403, başka sınıfın öğrencisi ve başka okul 404, girişsiz 401; başka sınıfın taslağı onaylanamaz (404); hiçbir reddedilen istekte taslak ya da bildirim oluşmaz.
- [ ] AC-10 — Veli görünümü: veli akışında yalnızca kendi çocuklarının (farklı şubedeki kardeş dahil) özetlerini görür; başka velinin çocuğunun özeti listede yok ve doğrudan istendiğinde 404; mobil ayrıntıda özet, etiket ve dayanak satırı görünür (iki tema).
- [ ] AC-11 — AI kullanılamıyor, gizlilik ve audit: metin servisi ayarsız/kapalı/zaman aşımı/boş yanıtta "Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar deneyin.", taslak oluşmaz, var olan bekleyen taslak korunur ve gönderilebilir, Warning log yazılır; log satırlarında özet, not, dayanak ve öğrenci adı geçmez; gönderim audit kaydı oluşturur (kim, öğrenci, hafta, ne zaman, IP).
- [ ] AC-12 — Web ekranı (öğretmen, iki tema) ve duman testi: "Haftalık özet" bağlantısı yalnızca sınıf ataması olan öğretmende; sınıf/hafta/öğrenci listesi (yükleniyor / boş / dolu / hata + tekrar dene, "Size atanmış sınıf yok."), panel durumları (dayanak boş, oluşturuluyor — düğmeler kilitli, taslak hazır, alan hataları — istek gitmez, sunucu hatası duyurulur, gönderildi); yetkisizde "Bu sayfaya erişiminiz yok."; akış yalnızca klavyeyle; 320 px'te yatay kayma yok. Duman betiği sabit örnek dayanaktan boş olmayan Türkçe özet üretir (VERIFY'da bu bilgisayarda gerçek çıktı).

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Hafta içinde öğrenci sınıf değiştirdiyse?" → öğrencinin bugünkü sınıfının öğretmeni üretir; dayanak öğrenciye göredir
  (o haftaki bütün yoklama kayıtları).
- "Aynı sınıfa iki öğretmen atanmışsa taslak kimin?" → öğrenci-hafta başına tek taslak; atanmış her öğretmen görür, düzenler,
  gönderir; onaylayan kaydedilir.
- "Öğretmen notu nerede saklanır?" → taslağın dayanağında; yeniden oluşturmada alan dolu gelir.
- "Hafta bitmeden 'Bu hafta' özeti?" → izinli; dayanak o ana kadarki veridir (öğretmen Cuma gönderir).
- "Veli hangi saatte görür, sessiz mod?" → sessiz mod yalnızca öğretmeni korur (0011); veliye gönderim anında.
- "Gönderilmiş özette hata varsa?" → düzeltme yok; gerekirse öğretmen 0011 mesajıyla açıklar (kapsam dışı satırında).
- "Gönderilmiş özetin dayanağı saklanır mı?" → evet, taslak kaydıyla (BR-6, `AiDraft` kaynak veriyle saklanır).

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
