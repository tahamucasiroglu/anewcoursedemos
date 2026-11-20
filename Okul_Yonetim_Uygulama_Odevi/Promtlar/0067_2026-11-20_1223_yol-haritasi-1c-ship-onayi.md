# 0067 — Yol haritasına platform paneli; spec 0004 ship onayı

## İstek

yol haritasına ekle ship onayıda veriyorum

## Cevap

- Yol haritasına 1c "Platform yönetim paneli" (okul ekle / listele / pasifleştir; bütün okulları gören tek rol, kendi
  spec'i ve 2 adımlı doğrulama ister) 0005'ten sonra eklendi.
- Ship onayı alındı, ama tamamlanması tek bir gözleme bağlı: AC-1'in "şifre gizli girişle sorulur" kısmı hassas alanda
  (kimlik/şifre) ve ANEW kuralına göre hassas alandaki bir kriter kanıtsız ship edilemez, sonraya da taşınamaz. Şifrenin
  terminalde görünmediği bir kez gerçek terminalde gözlenmeli. Önerilen: kullanıcı kendi kurucu hesabını açarken
  `bash tools/pi/create-school.sh ...` komutunda bunu görür ve "şifre görünmedi" der; ardından ship tamamlanır (kriterler
  işaretlenir, spec `specs/done/`'a taşınır, yol haritası ✅, ekran görüntüleri commit, özet + sonraki öneriler).
- Alternatif: bağımsız bir doğrulama ajanı Pi'de geçici bir veritabanıyla terminal benzetimi (pty) üzerinden otomatik
  gözlem yapar (daha uzun sürer).
