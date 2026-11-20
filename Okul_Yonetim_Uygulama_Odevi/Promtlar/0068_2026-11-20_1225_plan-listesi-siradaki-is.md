# 0068 — Plan listesi ve sıradaki iş

## İstek

yok bunu geçelim adimn panelde bakarız plan listesini ve önerdiğin sıradaki iş new-feature yazsını söyle bana

## Cevap

- AC-1 gözlemini (okul açma komutunda şifrenin terminalde görünmemesi) bağımsız bir ajanla Pi'de geçici veritabanında
  otomatik yaptırmayı denedim; izin denetimi reddetti, zorlamadım. ANEW kuralı (hassas alandaki kriter kanıtsız ship
  edilemez, sonraya da taşınamaz) esnetilemediği için spec 0004 **ship edilmedi**: kod bitti, Pi'de çalışıyor, 13
  kriterin 12'si kanıtlı; yol haritasında "ship tek gözlem bekliyor" diye işaretlendi. Kullanıcı kurucu hesabını
  açarken (`bash tools/pi/create-school.sh ...`) şifrenin görünmediğini söylediğinde ship tamamlanır.
- Yol haritası (docs/roadmap.md) özetlendi; sıradaki önerilen iş: 1b şube ve kullanıcı yönetimi (spec 0005), komut:
  `/new-feature Şube ve kullanıcı yönetimi: kurucu web'den şube ve kullanıcı ekler, rol ve şube verir; geçici şifre ve şifre sıfırlama; SMTP'ye hazır e-posta (bilgiler girilmeden)`
