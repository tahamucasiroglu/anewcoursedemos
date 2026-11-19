# Spec 0002 — Tema ve açık/koyu mod

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: done

## Intent
Okul yöneticisi, öğretmen ve veli uygulamayı açtığında "sade/ham" değil, bakımlı ve tutarlı bir görünüm görmeli;
gece kullanımda göz yormayan koyu bir seçenek de olmalı. Başarı: web paneli ve mobil uygulama aynı renk temasını
kullanır; kullanıcı "Sistem / Açık / Koyu" arasından tek dokunuşla seçim yapar, seçim aynı cihazda bir sonraki
açılışta hatırlanır; hiç seçim yapmamış kullanıcı cihazının ayarını görür; iki modda da metinler ölçülerek okunaklıdır.
Tema, ileride kuruma özel marka rengine geçilebilecek biçimde tek bir ana renk üzerine kurulur. Bilinçli olarak
yapılmayanlar: kuruma özel logo/renk (markalı uygulama, ileriki faz), seçimin kullanıcı hesabına kaydı (henüz giriş
yok), yazı boyutu ayarı, yeni ekran.

## Requirements
1. **Ortak renk teması:** Web ve mobil aynı renk sözlüğünü kullanır: sakin nötr zemin, tek ana renk (lacivert),
   durum renkleri (başarılı = yeşil, hata = kırmızı). Her renk için bir açık ve bir koyu karşılık vardır. Ana renk
   tek yerden değiştirilebilir (ileride kurum markası).
2. **Üç mod seçeneği:** "Sistem" (cihaz ayarına uyar), "Açık", "Koyu". Varsayılan "Sistem".
3. **Mod seçici:** Başlığın sağ üstünde üç parçalı seçici; her parçada yazı (ve yardımcı simge); seçili olan
   görünür biçimde ve ekran okuyucuya "seçili" olarak belirtilir. Dar ekranda (320 piksel) taşmaz; gerekirse alt satıra iner.
4. **Anında geçiş:** Bir seçenek seçilince tüm ekran yeniden açmaya gerek kalmadan o moda geçer; mobil durum çubuğu ve
   tarayıcının yerleşik parçaları (kaydırma çubuğu, form kutuları) da dahil.
5. **Hatırlama:** Seçim cihaz başına saklanır (web'de tarayıcıda, mobilde telefonda); sunucuya gönderilmez. Uygulama
   yeniden açıldığında son seçim geçerlidir.
6. **Cihazı izleme:** "Sistem" seçiliyken cihazın modu değişirse uygulama anında uyar. "Açık" veya "Koyu" seçiliyken
   cihaz değişikliği görünümü değiştirmez.
7. **Bozuk kayıtta güvenli davranış:** Kayıtlı seçim okunamazsa, bilinmeyen bir değerse veya saklama kapalıysa
   uygulama hata göstermeden "Sistem" ile açılır; seçici yine çalışır (seçim o oturum boyunca geçerli olur).
8. **Açılışta parlama yok (web):** Kayıtlı mod sayfa ilk kez çizilmeden önce uygulanır; koyu kayıtlı kullanıcı
   açılışta açık renkli bir an görmez.
9. **Okunabilirlik:** İki modda da normal metin/zemin çiftleri en az 4.5:1, büyük metin ile düğme ve çerçeve
   kenarları en az 3:1 kontrasta sahiptir (WCAG AA); otomatik ölçülür.
10. **Mevcut davranış korunur:** Sistem durumu ekranının 4 durumu ve "Tekrar dene" iki modda da aynı metinle çalışır;
    durum yalnızca renkle değil metin/işaretle de anlatılır.
11. Kullanıcıya görünen yeni metinler ("Tema", "Sistem", "Açık", "Koyu") kodda değil, Türkçe metin dosyalarındadır.

## User experience
- **Flow:** Kullanıcı web panelini veya mobil uygulamayı açar → cihaz ayarına (ya da kayıtlı seçimine) göre açık veya
  koyu görünüm gelir → sağ üstteki "Sistem · Açık · Koyu" seçicisinden birine dokunur → ekran anında o moda geçer →
  uygulamayı kapatıp açtığında seçimi hatırlanmış olarak görür.
- **States:**
  - **Açık mod** — mevcut sistem durumu ekranı (yükleniyor, çalışıyor, hizmet dışı, ulaşılamıyor) açık renklerle.
  - **Koyu mod** — aynı 4 durum koyu renklerle.
  - **Seçici durumları** — "Sistem" seçili, "Açık" seçili, "Koyu" seçili (seçili parça belirgin).
  - **Kayıt okunamıyor** — kullanıcıya hata gösterilmez; "Sistem" seçili görünür.
  - Boş durum / yetkisiz durum: yok (veri listesi yok, giriş yok).
- **Design reference:** none — yukarıdaki akış ve renk sözlüğü tasarımın kendisidir; renkleri agent önerir, görsel
  onay SHIP'te ekran görüntülerine bakılarak verilir (iki mod × 4 durum, web ve mobil).
- **Accessibility:** Seçici klavyeyle kullanılabilir (web: Tab ile odaklanır, Enter/Boşluk ile seçer, odak çerçevesi
  görünür); ekran okuyucu grubun adını ("Tema"), seçenek adlarını ve seçili olanı okur (web ve mobil). Kontrast
  Requirements 9'daki eşiklerle ölçülür. Durum yalnızca renkle anlatılmaz.

## Constraints & out of scope
- Kapsam dışı: kuruma özel logo/renk (markalı uygulama — kapsam dokümanı Faz 3 / premium); seçimin hesaba kaydı
  (kullanıcı girişi gelince — backlog); yazı boyutu ayarı; birden çok açık sekme arasında anlık eşitleme (diğer sekme
  bir sonraki açılışta görür); yeni ekran; geçiş animasyonu.
- Seçim yalnızca cihazda tutulur, kişisel veri değildir. Sensitive area: none.
- Yeni dış servis veya sunucu değişikliği yok; API davranışı değişmez.
- Mobil doğrulama (öz eleştiri Ö-4): gerçek iOS/Android cihazda görsel doğrulama yapılamıyor; mantık testlerle,
  görünüm Expo web çıktısının ekran görüntüleriyle kanıtlanır. Kullanıcı isterse kendi telefonunda Expo Go ile bakar.
- Size: tek kullanıcı yeteneği (görünüm modu), 11 kabul kriteri — bölünmeden tek spec olarak öneriliyor
  (kriterlerin çoğu aynı seçicinin sınır durumları).

## Acceptance criteria
- [ ] AC-1 — Web ve mobil aynı renk sözlüğünü kullanır: aynı mod ve aynı rol (zemin, metin, ana renk, başarılı, hata) için iki tarafta aynı renk değeri vardır; ana renk tek bir tanımdan gelir.
- [ ] AC-2 — Hiç seçim yapılmamışsa seçici "Sistem"i seçili gösterir; cihaz açık moddaysa açık, koyu moddaysa koyu görünüm gelir; cihaz bir tercih bildirmiyorsa açık görünüm gelir.
- [ ] AC-3 — Seçici başlığın sağ üstünde "Sistem", "Açık", "Koyu" seçeneklerini yazıyla gösterir; seçili olan görsel olarak ve ekran okuyucu için "seçili" olarak belirtilir; metinler Türkçe metin dosyasındandır; 320 piksel genişlikte başlık ve seçici taşmadan sığar (gerekirse seçici alt satıra iner).
- [ ] AC-4 — "Açık" veya "Koyu" seçilince ekran yeniden açılmadan o moda geçer ve cihaz ayarından bağımsız kalır; mobilde üstteki durum çubuğu (saat, pil) ve web'de tarayıcının yerleşik parçaları (kaydırma çubuğu, form kutuları) da seçilen moda uyar.
- [ ] AC-5 — Seçim hatırlanır: web'de sayfa yenilenince veya yeniden açılınca, mobilde uygulama yeniden başlatılınca son seçim (Sistem, Açık veya Koyu) geçerlidir; seçim sunucuya gönderilmez.
- [ ] AC-6 — "Sistem" seçiliyken cihazın modu değişirse görünüm yeniden açmadan değişir; "Açık" veya "Koyu" seçiliyken cihaz değişikliği görünümü değiştirmez.
- [ ] AC-7 — Kayıtlı seçim bozuk, bilinmeyen bir değer veya okunamıyorsa (saklama kapalı/hata) uygulama hata göstermeden "Sistem" ile açılır ve seçici çalışmaya devam eder.
- [ ] AC-8 — Web: "Koyu" kayıtlıyken sayfa açılışında ilk çizilen görünüm koyudur (açık renkli ara görüntü yok).
- [ ] AC-9 — Kontrast: iki modda da tüm metin/zemin çiftleri en az 4.5:1, büyük metin ile düğme ve çerçeve kenarları en az 3:1; bir renk bu eşiğin altına düşerse otomatik test kırmızıya düşer.
- [ ] AC-10 — Sistem durumu ekranının 4 durumu (yükleniyor, çalışıyor, hizmet dışı, ulaşılamıyor) ve "Tekrar dene" iki modda da web ve mobilde aynı metinle görünür ve çalışır.
- [ ] AC-11 — Seçici web'de yalnızca klavyeyle kullanılabilir (Tab ile odak, Enter/Boşluk ile seçim, görünür odak çerçevesi); web ve mobilde ekran okuyucu grubun adını ("Tema") ve seçenekleri okur.

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
