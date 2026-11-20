# Spec 0012 — AI: sesli nottan veli mesajı

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0012-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Öğretmen gün içinde veliye yazmak istediği şeyi düzgün bir mesaja dökmeye vakit bulamıyor; kapsam dokümanı bu özelliği
"satış demosunun yıldızı" sayıyor. Bu spec yol haritasının 6. satırını getirir: öğretmen mobilde bir veliyle konuşmasındayken
~20 saniyelik **sesli not** kaydeder; not **bu bilgisayardaki yerel modellerle** yazıya dökülür (Whisper) ve kibar, kısa bir
**veli mesajı taslağına** çevrilir (Ollama, `gemma3:4b`). Taslak mesaj kutusuna gelir; öğretmen düzeltir ve kendisi
"Gönder"e basar — **öğretmen göndermeden hiçbir şey veliye gitmez** (BR-5). Başarı: öğretmen 20 sn konuşur, en geç ~30 sn
içinde taslağı görür, bir kelime düzeltip gönderir; veli mesajı spec 0011'in konuşmasında görür. Bilinçli olarak
yapılmayanlar: dış (bulut) yapay zekâ servisi, ses kaydının saklanması, velinin sesli notu, taslağın otomatik gönderimi,
kusursuz doğruluk ("yüksek oranda doğru yeter"), Pi üzerinde model çalıştırma.

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Hangi yerel yapay zekâ, nasıl çağrılır?** Metin: Ollama (`http://127.0.0.1:11434`, model `gemma3:4b`, ayardan
  değiştirilebilir — ör. daha küçük bir qwen). Ses→metin: faster-whisper `medium`, `tools/ai/.venv` içinde çalışan **küçük
  yerel HTTP servisi** (yalnızca Python standart kütüphanesi; model açılışta bir kez yüklenir, yalnızca `127.0.0.1`'i
  dinler). API ikisini de HTTP ile çağırır; adresler ayardan. Neden: her not için ayrı süreç açmak modeli her seferinde
  yeniden yükler (~10 sn+); servis bir kez yükler, API tarafı Ollama ile aynı kalıpta kalır. Backlog B-4 bu kararla kapanır
  (yerel model; "eğitimde kullanılmaz" şartı yerel çalıştığı için kendiliğinden sağlanır).
  - Dokümanda istenen: "6 ve 8 de şimdilik bizim bilgisayara ollama kuralım. gemme3:4b yada qwen gibi bir tık daha düşük
    model ile analizleri yapsın sesten yazı içinde whisper yeter." (Promtlar 0082); mimari: "Dış servisler (… AI, ses→metin)
    port arayüzlerinin arkasındadır".
- **C-2 — Raspberry Pi'de ne olur?** Pi'de ekran kartı yok, model çalıştırılmaz. Varsayılan: AI adresleri boş → özellik
  "Yapay zekâ şu an kullanılamıyor. Mesajı elle yazabilirsiniz." der, mesajlaşma normal çalışır. İsteğe bağlı demo ayarı:
  Pi'deki API, aynı yerel ağdaki bu bilgisayarın Ollama ve ses servisi adresine yönlendirilebilir (bilgisayarda servisler
  ağa açılır, Windows güvenlik duvarı yalnızca Pi'nin adresine izin verir). Veri okulun yerel ağından çıkmaz; dış servis
  yok. Neden: Pi'de çökme yok, demo günü bilgisayar açıksa tam özellik.
  - Dokümanda istenen: "şimdilik bizim bilgisayara ollama kuralım"; güvenlik: "Türkiye'de barındırılabilir tasarım; yurt
    dışı servis bağımlılıkları port arkasında"; "Çocuklara dair veriler model eğitiminde kullanılmaz".
- **C-3 — Akış nerede başlar?** Spec 0011'in mobil **konuşma ekranında**, yalnızca öğretmende bir "Sesli not" düğmesi:
  kayıt (en fazla 60 sn, 60'ta kendiliğinden durur) → "Dönüştürülüyor…" → taslak mesaj kutusuna gelir. Yeni bir veliye
  yazmak için önce 0011'in "Yeni mesaj" akışıyla konuşma açılır. Neden: alıcı ve öğrenci bağlamı konuşmadan gelir; yeni
  ekran ve yeni alıcı kuralı gerekmez.
  - Dokümanda istenen: "Sesli nottan veli mesajı | Öğretmen | 20 saniyelik sesli notu düzgün, kibar bir veli mesajına
    çevirir; öğretmen onaylayıp gönderir".
- **C-4 — Onay ve gönderim nasıl?** AI yalnızca taslak (`AiDraft`) üretir; veliye hiçbir şey göndermez. Öğretmenin
  "Gönder"i onaydır: mesaj 0011'in mesaj gönderme yolundan, öğretmenin son hâliyle gider ve taslak "onaylandı" olarak
  son metinle işaretlenir. Bir taslak bir kez kullanılır. "Vazgeç" taslağı siler. Neden: BR-5'in en sade, yapısal karşılığı
  — gönderme yolu tek ve insan elinde; mimari kural 6 (Ai, Communication'a bağımlı olmaz) korunur.
  - Dokümanda istenen: "AI hiçbir mesajı onaysız veliye göndermez"; domain: "AI taslağı (`AiDraft`) — AI'ın ürettiği,
    insan onayı bekleyen çıktı; kaynak veriyle birlikte saklanır".
- **C-5 — Ses kaydı saklanır mı?** Hayır. Ses yalnızca bellekte işlenir, veritabanına ve diske yazılmaz. Kaynak olarak
  yazıya dökümü taslakla birlikte saklanır. Neden: veri minimizasyonu (S2); kaynak gösterme için döküm yeter.
  - Dokümanda istenen: "Her AI çıktısında kaynak veri görünür"; güvenlik: "AI'a yalnızca görev için gereken alanlar
    gönderilir (veri minimizasyonu)".
- **C-6 — Veli ne görür, kaynak nerede görünür?** Veli, öğretmenin onayladığı sıradan bir mesaj görür (AI etiketi yok:
  metnin sahibi ve göndereni öğretmen). Kaynak (dökümü) taslakta öğretmene "Kaynak: sesli notun dökümü" olarak görünür.
  Neden: AI çıktısını onaylayan kişi kaynağı görür (BR-6); onaylanmış mesaj öğretmenin mesajıdır.
  - Dokümanda istenen: "Her AI çıktısında kaynak veri görünür"; "öğretmen onaylayıp gönderir".
- **C-7 — Doğruluk ve hata beklentisi?** Kalite kapısı yok: boş olmayan döküm ve taslak yeter; öğretmen düzeltir. Hatalar:
  servis kapalı/yanıt vermiyor → "Yapay zekâ şu an kullanılamıyor. Mesajı elle yazabilirsiniz."; sessiz/anlaşılmayan
  kayıt → "Ses anlaşılamadı, lütfen tekrar deneyin."; uzun kayıt → "Sesli not en fazla 60 saniye olabilir." Hedef süre:
  bu bilgisayarda kayıt bitişinden taslağa ≤ 30 sn. Neden: kullanıcı "tam doğruya gerek yok" dedi.
  - Dokümanda istenen: "tam doğruya gerek yok çalışsın ve yüksek oranda doğru yapsa çeviri yeter." (Promtlar 0082).
- **C-8 — Nasıl test edilir?** `scripts/check` Ollama/Whisper istemez: testler sahte portlarla. Gerçek modelleri çalıştıran
  ayrı, isteğe bağlı bir **duman testi betiği** (`tools/ai`) eklenir; check'e girmez, VERIFY'da bu bilgisayarda çalıştırılır.
  Neden: CI'da ve Pi'de model yok; gerçek zincir yine kanıtlanmalı.
  - Dokümanda istenen: test kuralı "dış servisler sahte port uygulamalarıyla".

## Requirements
1. **Sesli not → taslak:** Öğretmen, ilişkisi süren (0011) bir konuşmada 1–60 saniyelik sesli not gönderir. Not yazıya
   dökülür ve kısa, kibar, Türkçe bir veli mesajı taslağına çevrilir. Yanıtta döküm ve taslak metni döner; taslak,
   öğretmen, konuşma ve öğrenciyle birlikte saklanır (kaynak = döküm). Bu adımda veliye mesaj ya da bildirim **gitmez**.
2. **Onay = gönderim:** Öğretmen taslağı düzenleyip 0011'in mesaj gönderme yoluyla gönderir; giden metin öğretmenin son
   metnidir (0011 mesaj kuralları: boş değil, en fazla 2000 karakter). Taslak son metinle "onaylandı" olur ve gönderilen
   mesaja bağlanır. Aynı taslakla ikinci gönderim "Bu taslak zaten kullanıldı." ile reddedilir. "Vazgeç" taslağı (döküm
   dahil) siler.
3. **Yetki:** Sesli not yalnızca konuşmanın öğretmen tarafınca, ilişki sürerken kullanılır. Veli, başka öğretmen, kurucu,
   muhasebe kullanamaz; başka okulun ya da üçüncü kişinin konuşması "bulunamadı"; ilişkisi biten konuşmada "Bu konuşmaya
   artık mesaj gönderilemez."; bir taslak yalnızca onu oluşturan öğretmen tarafından ve oluşturulduğu konuşmada kullanılır.
4. **Ses sınırları:** Ses dosyası en fazla 2 MB; desteklenen türler telefon kaydı biçimleri (m4a/aac, webm, wav, ogg).
   Boş dosya, desteklenmeyen tür, 2 MB üstü ya da 60 saniyeden uzun not reddedilir ve taslak oluşmaz. Döküm boşsa
   "Ses anlaşılamadı, lütfen tekrar deneyin."
5. **AI kullanılamıyor:** Ses ya da metin servisi ayarlanmamış, kapalı, zaman aşımına uğramış ya da boş yanıt vermişse
   "Yapay zekâ şu an kullanılamıyor. Mesajı elle yazabilirsiniz." döner, taslak oluşmaz, aksaklık Warning olarak loglanır;
   elle mesaj yazma etkilenmez.
6. **Veri minimizasyonu ve yerellik:** Modele yalnızca döküm ve sabit talimat gider (öğrenci/veli adı, sağlık bilgisi,
   başka kayıt gitmez). Model çağrıları yalnızca ayarlı yerel adreslere yapılır; varsayılan ayarlarda dış adres yoktur.
   Taslak en fazla 1000 karakterdir.
7. **Gizlilik:** Ses kaydı veritabanına ve diske yazılmaz. Döküm, taslak ve mesaj metni log satırlarına yazılmaz; loglarda
   yalnızca kimlikler, ses süresi, karakter sayısı ve süre ölçümleri geçer.
8. **Duman testi (isteğe bağlı):** Bu bilgisayarda Ollama ve ses servisi çalışırken tek komutla gerçek modeller denenir:
   sabit bir döküm uygulamanın kullandığı talimatla taslağa çevrilir; verilirse bir ses dosyası yazıya dökülür. Betik
   `scripts/check`'e girmez.

## User experience
- **Flow — mobil (öğretmen):** Mesajlar → konuşma → "Sesli not" (mikrofon) → kayıt (geçen saniye, "Durdur"; 60 sn'de
  kendiliğinden durur) → "Dönüştürülüyor…" (gönder kilitli) → taslak mesaj kutusunda, üstünde "AI taslağı — göndermeden
  önce kontrol edin" ve açılır "Kaynak: sesli notun dökümü" → düzenle → "Gönder" (0011 akışı) ya da "Vazgeç".
- **Flow — mobil (veli):** değişiklik yok; mesaj 0011 konuşmasında sıradan öğretmen mesajı olarak görünür.
- **States:** kayıt yok (düğme) / kaydediliyor (sayaç) / dönüştürülüyor / taslak hazır / hata ("Yapay zekâ şu an
  kullanılamıyor. Mesajı elle yazabilirsiniz.", "Ses anlaşılamadı, lütfen tekrar deneyin.", "Sesli not en fazla 60 saniye
  olabilir.", "Mikrofon izni verilmedi.") / gönderildi (0011) / ilişkisi biten konuşmada düğme kapalı; velide düğme yok.
- **Design reference:** none — the flow above is the design (0011 konuşma ekranı kalıbı, ortak tema).
- **Accessibility:** "Sesli not", "Durdur", "Vazgeç" düğmelerinde erişilebilirlik etiketi; kayıt sayacı ve hata metni
  ekran okuyucuya duyurulur; "AI taslağı" etiketi renk dışında metinle; iki temada kontrast spec 0002 eşiklerinde.

## Constraints & out of scope
- Sensitive area: S5 (veliye giden iletişim, AI taslağı — BR-5, BR-6), S2 (öğrenciye dair ses/döküm), S1 (okul ve ilişki
  sınırı).
- **Bağımlılık:** spec 0011 (konuşma, mesaj gönderme yolu, ilişki denetimi) ve 0010 (öğretmen–sınıf ataması) önce biter.
  Bu spec ortak AI katmanını kurar (metin ve ses portları, yerel adaptörler, `AiDraft`, duman testi); spec 0014 onu kullanır.
- **Ortam:** AI yalnızca bu Windows bilgisayarda (RTX 3060 6 GB) çalışır; ses servisi elle (betikle) başlatılır. Pi'de
  varsayılan kapalı (C-2).
- **Kapsam dışı / sonra:** dış AI sağlayıcısı; velinin sesli notu; öğretmenin web'den sesli notu; duyuru için sesli not;
  ses kaydını saklama/dinletme; taslak geçmişi listesi; kullanılmamış taslakların süreli silinmesi (Compliance saklama
  spec'i); metin kalitesi ölçümü; ses servisini Windows hizmeti olarak otomatik başlatma; Pi'de model çalıştırma.
- Split: gerek yok — tek kullanıcı yeteneği, 12 kriter.

## Acceptance criteria
- [ ] AC-1 — Taslak üretimi: ilişkisi süren konuşmanın öğretmeni 1–60 sn'lik sesli not gönderince yanıtta döküm ve taslak döner; taslak kaydı öğretmen, konuşma, öğrenci, döküm (kaynak) ve taslak metniyle "bekliyor" durumunda oluşur; model taslağı 1000 karakteri aşarsa 1000'e kısaltılır.
- [ ] AC-2 — Onaysız gönderim yok (BR-5): taslak oluştuktan sonra konuşmada yeni mesaj yoktur ve bildirim kapısı hiç çağrılmamıştır; mesaj yalnızca öğretmen gönderince oluşur ve metni öğretmenin gönderdiği (düzenlenmiş) metindir; taslak son metin ve mesaj kimliğiyle "onaylandı" olur; aynı taslakla ikinci gönderim "Bu taslak zaten kullanıldı." ile reddedilir ve ikinci mesaj oluşmaz.
- [ ] AC-3 — Vazgeç: öğretmen taslaktan vazgeçince taslak kaydı (döküm dahil) silinir; silinmiş taslakla gönderim "bulunamadı" alır.
- [ ] AC-4 — Yetki ve izolasyon: veli, konuşmada taraf olmayan öğretmen, kurucu ve muhasebe sesli not gönderemez (403/404); başka okulun konuşması 404; girişsiz istek 401; ilişkisi biten konuşmada "Bu konuşmaya artık mesaj gönderilemez."; taslak başka öğretmen tarafından ya da başka konuşmada kullanılamaz (404); hiçbir reddedilen istekte taslak oluşmaz.
- [ ] AC-5 — Ses sınırları: boş dosya, desteklenmeyen tür ve 2 MB üstü dosya alan hatası verir (tam 2 MB kabul); 60 saniyeden uzun not "Sesli not en fazla 60 saniye olabilir." ile reddedilir (60,0 sn kabul); döküm boşsa "Ses anlaşılamadı, lütfen tekrar deneyin."; hiçbirinde taslak oluşmaz ve metin servisi çağrılmaz.
- [ ] AC-6 — AI kullanılamıyor: ses ya da metin servisinin adresi boş, servis ulaşılamaz, zaman aşımı ya da boş model yanıtı durumlarının her birinde "Yapay zekâ şu an kullanılamıyor. Mesajı elle yazabilirsiniz." döner, taslak oluşmaz, Warning log yazılır; aynı konuşmada elle mesaj gönderimi çalışır.
- [ ] AC-7 — Veri minimizasyonu: metin servisine giden istek yalnızca sabit talimatı ve dökümü içerir (öğrenci adı, veli adı, sağlık notu yok); varsayılan (Pi/üretim) ayarlarında AI adresleri boştur, geliştirme ayarında yalnızca `127.0.0.1` adresleri vardır.
- [ ] AC-8 — Gizlilik: ses içeriği veritabanında hiçbir kolona yazılmaz ve uygulama diske ses dosyası bırakmaz; log satırlarında döküm, taslak ve mesaj metni geçmez (yalnızca kimlikler, süre, karakter sayısı).
- [ ] AC-9 — Yerel adaptörler: Ollama adaptörü ayardaki modeli akışsız istekle çağırır ve yanıt metnini döndürür; ses servisi adaptörü sesi gönderip döküm ve süreyi okur; her ikisi zaman aşımı ve hata yanıtını "kullanılamıyor"a çevirir (sahte HTTP ile, servis gerekmeden).
- [ ] AC-10 — Ses servisi: `tools/ai` içindeki servis başlatma betiğiyle açılır, yalnızca `127.0.0.1`'i dinler (ayarla ağa açılabilir), Türkçe dökümü ve süreyi döner, sağlık ucu modelin yüklü olduğunu söyler, sesi diske yazmaz.
- [ ] AC-11 — Duman testi: Ollama ve ses servisi açıkken `tools/ai` duman betiği uygulamanın talimat dosyasıyla sabit bir dökümden boş olmayan Türkçe taslak üretir, verilen ses dosyasını yazıya döker ve süreleri yazar; servisler kapalıysa hangisinin kapalı olduğunu söyleyip sıfır olmayan kodla çıkar; betik `scripts/check`'te yoktur. Kanıt: VERIFY'da bu bilgisayardaki gerçek çıktı + telefondan 20 sn'lik gerçek notla taslağın ≤ 30 sn'de geldiği gözlemi.
- [ ] AC-12 — Mobil ekran (öğretmen, iki tema): "Sesli not" düğmesi yalnızca öğretmende ve ilişkisi süren konuşmada; kaydediliyor (sayaç, Durdur, 60 sn'de otomatik durma), dönüştürülüyor (gönder kilitli), taslak hazır ("AI taslağı — göndermeden önce kontrol edin", açılır kaynak döküm, düzenlenebilir kutu, Vazgeç), hata durumları (kullanılamıyor, anlaşılamadı, 60 sn, mikrofon izni yok) ve erişilebilirlik etiketleri; velinin konuşma ekranında düğme yok.

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Ses süresini kim ölçer?" → istemci 60 sn'de durdurur; sunucu ses servisinin bildirdiği süreye bakar (AC-5).
- "Aynı anda iki öğretmen not gönderirse?" → sunucu AI işlerini sıraya alır (tek ekran kartı); sırada uzun beklerse
  "kullanılamıyor" mesajı (plan Risks).
- "Taslak 2000 karakteri aşabilir mi?" → taslak 1000'e kısaltılır; gönderim sınırı 0011'deki gibi 2000.
- "Öğretmen taslağı hiç düzenlemeden gönderebilir mi?" → evet; "Gönder"e basmak onaydır.
- "Kullanılmayan taslaklar birikir mi?" → evet, MVP'de silinmez (Vazgeç hariç); saklama süresi Compliance spec'inde.
- "Önce durum sorgusu yapıp düğmeyi gizlemek?" → yok; hata kayıttan sonra gösterilir (sade; Pi'de varsayılan kapalıyken
  öğretmen bir kez görür).
- "Veli AI ile yazıldığını bilmeli mi?" → hayır (C-6); kaynak öğretmene görünür.

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
