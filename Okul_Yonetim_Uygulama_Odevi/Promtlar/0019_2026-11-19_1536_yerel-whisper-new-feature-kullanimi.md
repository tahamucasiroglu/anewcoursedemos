# 0019 — Ses→metin: yerel Whisper · `/new-feature` nasıl kullanılır?

**Tarih:** 2026-11-19 15:36

## İstek

> sesi localde yazı yapan whisper gibi modeller var onlarla çözeriz localde şimdilik. kurulu var ama onları ellemeyelim senle ayrı kendimize kurarız repoya. new feature önerin güzel ama new feature nasıl kullanılıyor kendim özel yazmak istersem nasıl kullanacağım kısa anlatsana bi öyle feature başlayalım.

## Cevap

**Ses→metin kararı kaydedildi:** `ISpeechToText` için **yerelde çalışan Whisper benzeri bir model** kullanılacak. Makinede kurulu olanlara **dokunulmayacak**; bu repo için ayrı kurulacak. Model seçimi ve kurulum, "sesli nottan veli mesajı" spec'inde yapılacak. `specs/backlog.md` B-4 güncellendi. AI metin sağlayıcısı (taslak üretimi) hâlâ açık karar.

### `/new-feature` kısaca

**Yazım:** `/new-feature "<ne istediğin, serbest Türkçe metin>"`

Tırnak içine istediğini kendi cümlelerinle yazarsın. Teknik dil şart değil. Ajan bunu spec'e çevirir, belirsiz yerleri sana sorar.

**Ne olur (lite mod) — 4 durak, her birinde senin onayın:**

```
/new-feature "..."
  1. ANALYZE  → soruları sorar, specs/active/NNNN-...md yazar   ⛔ DURAK 1: spec onayı
  2. PLAN     → adım adım plan + hangi dosyalar + hangi testler ⛔ DURAK 2: plan onayı
  3. BUILD    → kodu yazar, scripts/check yeşil olana kadar
  4. REVIEW   → bağımsız reviewer alt-ajanı inceler              ⛔ DURAK 3: bulguları sen ayıklarsın (düzelt / ertele / reddet)
  5. VERIFY   → her kabul kriteri ↔ kanıt tablosu
  6. SHIP     → spec specs/done/'a taşınır                       ⛔ DURAK 4: teslim onayı
```

- Durakta "onaylıyorum" dersen devam eder; bir şey söylersen o aşamada düzeltir.
- Yarıda bırakırsan, aynı komutu tekrar yazdığında kaldığı duraktan devam eder (yeni spec açmaz).
- Hassas alana (S1–S6) dokunursa duraklar sıkılaşır: clarify her zaman sorulur, her kriter kasıtlı bozmayla ispatlanır.

**İyi bir açıklama nasıl yazılır:** *Ne*, *kim için* ve *sınır* yeterli. *Nasıl* kısmını ajan önerir.

Örnekler:
```
/new-feature "Öğretmen mobilden kendi sınıfının yoklamasını alsın; varsayılan herkes 'geldi', sadece gelmeyenlere dokunsun. Devamsız öğrencinin velisine bildirim taslağı oluşsun."

/new-feature "Muhasebe bir öğrenciye yıllık ücret ve taksit planı tanımlasın; indirim ve burs yüzde veya tutar olabilsin. Kuruş farkı son taksite."

/new-feature "Walking skeleton: ..."   ← önerdiğim ilk iş
```

**Sıra önerisi:** İlk iş **mutlaka walking skeleton** olmalı. Henüz hiç kod, proje dosyası ve `check.conf` adımı yok. Yoklama gibi gerçek bir özellik, iskelet olmadan başlarsa aynı altyapıyı yarım yamalak kurmak zorunda kalır. İskeletten sonra kendi yazdığın özellik açıklamalarıyla devam edersin.

İlk komut (istersen metnini değiştirebilirsin):
```
/new-feature "Walking skeleton: nArchitecture backend (5 katman, K1–K3 kuralları, architecture testleri), PostgreSQL sağlık ucu + test veritabanı yaşam döngüsü, web (React) ve mobil (Expo) iskeleti, ortak api-client; check.conf adımlarını aktifleştir"
```

Not: Bu mesaj aynı zamanda bootstrap RAPOR kapısının onayı sayılabilir mi? "Önerin güzel" dedin. Çalışma alanını onaylıyorsan, komutu yazman yeterli.
