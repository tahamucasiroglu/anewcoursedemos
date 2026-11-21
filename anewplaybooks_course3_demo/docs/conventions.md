# Conventions

> Bootstrap'ta dolduruldu (2026-11-21, adoption). Burada yalnız gerçek kurallar var: mevcut
> kodun stili ve yeni kod için review'da denetlenen kurallar.

## Language
- Sohbet dili (röportaj, oturumlar): Türkçe (`tr`)
- Doküman dili (docs/, spec'ler, planlar, ADR'ler, review ve verify raporları): Türkçe (`tr`)
- Her zaman İngilizce (protokol, düzyazı değil): ANEW çekirdek dosyaları, şablon başlıkları ve
  alan etiketleri, `Status:` değerleri (Draft / Approved / In progress / Shipped),
  `Approved by / on:`, `Source:`.
- Kod tanımlayıcıları, branch adları, commit mesajları: kod tanımlayıcıları mevcut düzende kalır
  (İngilizce `snake_case`, arayüz metinleri Türkçe ASCII). Commit mesajları Türkçe yazılır,
  Conventional Commits tipleri ve scope İngilizce kalır (`docs/git.md`).
- Agent mesajları (devir özetleri, retler, `Next:` satırları): sohbet dili (Türkçe). Komut
  adları, dosya yolları ve protokol alanları olduğu gibi kalır. `/new-feature` gibi komutlara
  verilen talep metni de Türkçe yazılır.

## Language & framework versions
- Python 3, doğrulanan sürüm 3.15.2. Flask 3.1.3, pytest 9.2.0: `requirements.txt`'te sabitli.

## Naming
- Python `snake_case` fonksiyon ve değişkenler. Kayıtlar sözlük olarak tutulur
  (`emp["hire_date"]`, `r["status"]`). Yeni kod da aynı stilde yazılır, karma stil (dataclass ile
  sözlük yan yana) getirilmez. Değiştirmek `/refactor` işidir.
- Arayüz ve durum metinleri Türkçe ASCII (`Onaylandi`, `Baslangic`). Mevcut metinler birebir
  korunur (BR-5).
- Testler: `tests/test_<konu>.py`, test adları davranışı söyler
  (`test_dashboard_counts_pending_approved_rejected`).

## Error handling
- **Mevcut** yutulan `except Exception` blokları (`create_request`, `/new`) olduğu gibi kalır.
  Bilinen borçtur ve ayrı bir talep (`/fix-bug`) olmadan değişmez.
- **Yeni kodda** hata yutulmaz: `except: pass`, `except Exception: return True` veya sessiz
  redirect yazılmaz. Kullanıcı hatası (geçersiz girdi, yetersiz bakiye) anlamlı bir mesajla
  gösterilir. Beklenmeyen hata yükselir.
- Kullanıcıya stack trace, kişisel veri veya izin nedeni sızmaz (`docs/security.md`).

## Data rules
- Tarihler `datetime.date`. Formdan gelen tarih `%Y-%m-%d` biçiminde çözülür.
- Gün sayıları `int`, takvim günü, hafta sonları dahil (BR-3).
- Durumlar yalnız `Beklemede` / `Onaylandi` / `Reddedildi` / `Iptal Edildi` (BR-5, BR-9), birebir aynı yazılır.
- "Bugün" sistem saatinden (`datetime.now()`, yerel saat) okunuyor. Yeni kod da aynı kaynağı
  kullanır ki testlerde tek noktadan sabitlenebilsin.

## Known traps
- `datetime.now()` yerel saat ve bugüne bağlı. Kıdem ve bakiye sonucu tarihe göre değişir. Testte saat referans tarihe (2026-11-21) sabitlenir: `monkeypatch` ile `app.datetime` değiştirilir — from bootstrap 2026-11-21
- `Db` modül seviyesinde global, testler arasında state sızar. Her testten önce `seed()` çağrılır (fixture) — from bootstrap 2026-11-21
- `render_template_string(... body|safe)` HTML'i kaçışsız basar. Yeni girdi `markupsafe.escape` ile yazılır, yeni `|safe` eklenmez — from bootstrap 2026-11-21
- 29 Şubat işe giriş tarihi (Ahmet Celik, 2020-02-29). Tarih aritmetiğinde yıl değiştirmek (`replace(year=...)`) ValueError verir. Kıdem yalnız yıl farkıyla hesaplanır (BR-2) — from bootstrap 2026-11-21
- Koddaki yorumlar koda göre yanlış olabilir (BR-1 "15 gün", BR-3 "iş günü"). Gerçeğin kaynağı kod ve `docs/domain.md` — from bootstrap 2026-11-21
- `/new` içinde gün hesabı ve eşik route'ta tekrar ediliyor. Gün veya eşik değişikliği iki yere birden dokunur (BR-7) — from bootstrap 2026-11-21

## Enforced by tooling
- `scripts/check`: `syntax [fast]` (Python sözdizimi), `import [fast]` (modül Flask ile
  yüklenebiliyor mu, seed çalışıyor mu) ve `test [fast]` (pytest, `tests/` altındaki tüm testler).
- Linter veya formatter yok, bootstrap'ta da dayatılmadı. Eklemek ayrı bir talep olur.
