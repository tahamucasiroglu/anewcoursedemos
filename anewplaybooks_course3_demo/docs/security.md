# Security

> Bootstrap'ta dolduruldu (2026-11-21). Bağlam: yalnız iç ağda kullanılan bir araç. Aşağıdaki
> riskler mevcut durumdur. Kod bootstrap'ta değişmedi.

## Secrets
- Gizli bilgiler repoya, spec'lere, prompt'lara ve sohbete asla girmez. `.env` gitignore'dadır,
  yerine `.env.example` verilir.
- Agent'lar gizli değerleri hata ayıklarken bile asla yazdırmaz.
- Uygulamada şu an gizli bilgi yok. Eklenirse ortam değişkeninden okunur, koda girmez.

## Known risks (existing, not fixed)
- **R-S1 Stored XSS:** `PAGE` içindeki `{{ body|safe }}` ve string birleştirme yüzünden personel
  adı ve izin nedeni HTML'e kaçışsız yazılıyor.
- **R-S2 Debug modu:** `app.run(debug=True)`. Werkzeug debugger hata anında uzaktan kod
  çalıştırmaya açık. Uygulama iç ağ dışına asla açılmaz.
- **R-S3 CSRF:** form korumasız.
- **R-S4 Denetim izi yok:** hatalar yutuluyor ve log tutulmuyor.

## Input & output
- **Yeni kodda** kullanıcı girdisi HTML'e her zaman kaçışlı yazılır (`markupsafe.escape`).
  Yeni `|safe` eklenmez.
- Form girdileri sınırda doğrulanır (tarih biçimi, personel varlığı). Geçersiz girdi kullanıcıya
  anlamlı bir mesajla döner, sessizce yutulmaz.
- Hata mesajlarına ve loglara kişisel veri (ad, işe giriş tarihi) ve izin nedeni yazılmaz.
  Stack trace kullanıcıya gösterilmez (mevcut `debug=True` bir borçtur, R-S2).

## AuthN / AuthZ
- **Yok.** Siteye erişen herkes her personel adına talep açabilir. Bu mevcut durum olarak
  kaydedildi. Kimlik doğrulama eklemek ayrı bir talep olur.

## Dependencies
- Yalnız gerekli ve yaygın paketler kullanılır: Flask, test için pytest.
- Yeni bir bağımlılık spec'te gerekçelendirilir, `requirements.txt`'e sürümü sabitlenerek yazılır
  ve plan onayında insan tarafından onaylanır. Lisans ve bakım durumu plan aşamasında kontrol
  edilir.

## Review lens
Güvenlik, her bağımsız review'un zorunlu bir boyutudur (`prompts/review.md`). Sonradan eklenen
ayrı bir aşama değildir.
