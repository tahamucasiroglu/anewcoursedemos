# 0018 — Konu 12 onayı (lite) · Adım 3 ÜRETİM · Adım 4 DOĞRULAMA · Adım 5 RAPOR

**Tarih:** 2026-11-19 15:35

## İstek

> lite mod olsun

## Cevap

**Konu 12 — Mod: ONAYLANDI → lite.** Görüşme tamamlandı (12/12).

### Adım 3 — ÜRETİM (oluşturulan/doldurulan dosyalar)
| Dosya | İçerik |
|---|---|
| `AGENTS.md` | Proje özeti, `Mode: lite`, `Language: chat=tr · docs=tr`, değiştirilmemiş invariant kurallar, proje işaretçileri — **40 satır** |
| `docs/domain.md` | Ürün, 24 terim (Şube = aynı kurumun lokasyonu), BR-1…BR-9, değişmezler, hassas alanlar S1–S6 |
| `docs/architecture.md` | Stack tablosu, klasör düzeni, 11 modül, iletişim kuralları, 11 yasak bağımlılık (K1–K3 dahil), kapsam dışı |
| `docs/conventions.md` | Dil kuralları (Türkçe commit/summary/resx), isimlendirme, hata yönetimi, 10 veri kuralı, tuzaklar T1–T7, araçla zorlananlar |
| `docs/testing.md` | Katmanlar/araçlar, fast/full seçicisi, test veritabanı yaşam döngüsü, determinizm, zorunlu testler, e2e yok |
| `docs/security.md` | Secrets, hassas veri koruması, girdi/çıktı, AuthN/AuthZ, KVKK, paket politikası |
| `docs/git.md` | Üst repo, `main`, Türkçe commit, `okul/` kapsamı, Promtlar commit'leri, trivial politikası (1-uyarlanmış) |
| `docs/decisions/0001-...md` | ADR: nArchitecture modüler monolit + .NET 10 + PostgreSQL 18 |
| `docs/decisions/0002-...md` | ADR: Üst repoda `main` üzerinde çalışma (ANEW varsayılanından bilinçli sapma) |
| `scripts/check.conf` | Greenfield: planlanan adımlar + `requires` satırları **yorum olarak** (walking skeleton açacak) |
| `specs/backlog.md` | B-1 ödeme, B-2 SMS, B-3 WhatsApp, B-4 AI/ses sağlayıcısı, B-5 PostgreSQL RLS, B-6 push — `Source: bootstrap 2026-11-19` |
| `README.md` | Kısa proje tanımı |

Ayrıca: Promtlar 0010–0017'deki saatler gerçek saatlere göre düzeltildi (önceden tahmini yazılmıştı, sıra bozuktu).

### Adım 4 — DOĞRULAMA
- `./scripts/doctor` → **0 fail, 1 warn**. Uyarı: "check.conf has no steps". Greenfield'da beklenen bir durum; profil kararı walking skeleton'ın PLAN kapısında verilir.
- `./scripts/check` → `check.conf has no steps — greenfield no-op`, **exit 0**.
- Residue: **RESIDUE NOT MEASURED (fast + full)** — henüz adım yok; walking skeleton'ın BUILD'i iki profilde ölçecek.
- Repoda sızan gizli bilgi taraması: Postgres şifresi hiçbir dosyada yok ✔.
- **Bağlam testi (önerilir, kullanıcı):** Yeni bir oturum aç ve "para alanları hangi tipte tutulur?" veya "Şube ne demek?" diye sor. Ajan dosyalardan cevaplamalı (`decimal`/`numeric(18,2)`; aynı kurumun farklı lokasyonu).

### Adım 5 — RAPOR
**Varsayımlar** (hepsi görüşmede onaylandı; yalnızca şunlar ayrıntı düzeyinde benim seçimim):
- Test araçları (xUnit v3, Shouldly, NSubstitute, NetArchTest, Vitest, Jest) ve Integration için `Category=Integration` trait seçicisi.
- Kesin paket sürümleri walking skeleton planında sabitlenecek.
- Backlog kayıtlarında "Deferred by" adı git kullanıcı adın.

**Remote/CI:** Yok (senin kararın). Tam yeşil kanıt, yerelde `./scripts/check` (full) ile alınır. Dal birleştirme yok (`main`, ADR-0002). `.github/workflows/check.yml` ANEW'den geldi ama remote olmadığı için çalışmaz; dosya zararsız, ileride remote olursa kullanılabilir.

**Açık kararlar (önerilerimle):**
1. **AI/ses→metin sağlayıcısı (B-4):** "Sesli nottan veli mesajı" spec'inin ANALYZE aşamasında karar verilmesini öneriyorum; o zamana kadar sahte port.
2. **Profil kararı (R-13):** Walking skeleton'ın PLAN kapısında; fast ve full profillerinin ikisi de bu makinede çalışabilir görünüyor (.NET 10, Node 26, PostgreSQL 18 kurulu).
3. **Commit:** Üst repoda `main`e 3 commit atıldı: `4cd4ed7` bootstrap çıktısı, `15d67be` Promtlar 0001–0018, `b6a8df1` betiklere çalıştırma izni (+x). Push sende.

**İlk spec — walking skeleton:** Uçtan uca en küçük dilim. Backend çözümü (5 katman + `Directory.Build.props`/`.editorconfig` ile K1–K3), architecture testleri, PostgreSQL'e bağlanan bir sağlık ucu + test veritabanı yaşam döngüsünü kanıtlayan bir integration testi, web ve mobil iskeletleri, ortak api-client. Amaç `check.conf` adımlarını açmak. PLAN kapısında doctor'ın profil kararı, BUILD'de residue testi var.

**[GATE: insan]** Çalışma alanını onaylıyor musun? Onaydan sonra:

`Next: /new-feature "Walking skeleton: nArchitecture backend (5 katman, K1–K3 kuralları, architecture testleri), PostgreSQL sağlık ucu + test veritabanı yaşam döngüsü, web (React) ve mobil (Expo) iskeleti, ortak api-client; check.conf adımlarını aktifleştir"`
