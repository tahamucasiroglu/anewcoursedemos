# Architecture

> Bootstrap'ta dolduruldu (2026-11-21, adoption). Burada var olan yapı yazılıdır. Yeni kodun
> hangi yöne akacağı ise "Communication rules" ve "Forbidden dependencies" bölümlerinde.

## System overview
LeaveManager tek dosyalık bir Python/Flask monolitidir (`app.py`). Kalıcılık yok: veri bellekte
duruyor (`Db`) ve her açılışta `seed()` ile sabit veriye dönüyor ("sonra SQL'e geçilecek",
2016). HTML, route'larda Python string birleştirme ve `render_template_string` ile sunucu
tarafında üretiliyor. Bu yapı bilinçli bir seçim değil, legacy mirası. Dosya bölme veya
yeniden yapılandırma ancak `/refactor` iş akışıyla, davranışın korunduğu kanıtlanarak yapılır.

**Stack:** Python 3 (doğrulanan 3.15.2) · Flask 3.1.x (3.1.3). Çalıştırma `python app.py`,
port 5000, `debug=True`.

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `Db` + `seed()` | Bellekteki veri deposu, açılışta sabit seed | Personel listesi, talep listesi, `next_id` |
| `LeaveService` (`svc`) | İş kuralları (`docs/domain.md` BR-1…BR-8) | Hak, kıdem, gün ve bakiye hesabı, talep oluşturma |
| Flask route'ları + `PAGE` | HTTP uçları ve HTML üretimi: `/`, `/requests`, `/new` | Sayfa düzeni, form işleme |

## Communication rules
- Yön: **route → `LeaveService` → `Db`**. Route'lar okuma için `Db`'yi doğrudan da kullanıyor
  (mevcut durum).
- **Yeni iş kuralı servise yazılır, route'a yazılmaz.** Route yalnız girdiyi çözer, servisi
  çağırır ve sonucu sayfaya döker.

## Forbidden dependencies (make them testable)
- `LeaveService`, Flask'a (`request`, `redirect`, `render_template_string`) bağımlı olmaz.
- `Db`, `LeaveService`'e veya route'lara bağımlı olmaz.
- Yeni kod, iş kuralını (gün hesabı, eşik, hak) route içinde tekrar etmez.

**Mevcut ihlaller (borç, bilinçli olarak korunuyor):**
- `/new` route'u gün hesabını (`(end - start).days + 1`) ve bakiye eşiğini kendi içinde
  tekrarlıyor (`docs/domain.md` BR-7).
- `LeaveService.status_badge` HTML üretiyor.

## Technical debt (known, not scheduled)
- Bağımlılık dosyası yok (`requirements.txt`). İlk spec'in (characterization) planında
  eklenecek, pytest ile birlikte.
- Durumlar enum değil, metin (BR-5).
- Yutulan hatalar: `create_request` ve `/new` içindeki `except Exception` blokları.
- Ölü kod: `recalculate_year_end` (BR-8).
- Kalıcılık yok, HTML ve iş mantığı aynı dosyada, güvenlik borçları (`docs/security.md`).

## Deliberately out of scope
- Veritabanına geçiş, kimlik doğrulama, dosya veya modül bölme, şablon motoruna geçiş. Bunların
  her biri ayrı bir talep olur.
