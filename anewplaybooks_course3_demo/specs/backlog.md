# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- A criterion moved out at the ship gate (R-08, `→ moved to B-<n>`) lands here too, by the
  human's ship decision; the entry names the spec and criterion.
- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
  A decision deferred at bootstrap (its REPORT) uses Source: bootstrap <YYYY-MM-DD>.
-->

- **B-1** — Stored XSS: `PAGE` içindeki `{{ body|safe }}` ve string birleştirme yüzünden personel adı ve izin nedeni HTML'e kaçışsız yazılıyor (`app.py`, `docs/security.md` R-S1) — Source: bootstrap 2026-11-21 — Severity: High — Recommendation: kullanıcı girdisini `markupsafe.escape` ile yazan ayrı bir CHANGE talebi (karakterizasyon testleri korunarak) — Deferred by: tahamucasiroglu, 2026-11-21, ders demosu kapsamı dışında; ayrı iş olarak ele alınacak
- **B-2** — `app.run(debug=True)`: Werkzeug debugger hata anında uzaktan kod çalıştırmaya açık (`app.py` son satır, `docs/security.md` R-S2) — Source: bootstrap 2026-11-21 — Severity: High — Recommendation: debug'ı ortam değişkenine bağlayan küçük bir CHANGE; varsayılan kapalı — Deferred by: tahamucasiroglu, 2026-11-21, yalnız iç ağ/yerel kullanım; ayrı iş
- **B-3** — Personel numarası doğrulaması yok: `/new`'deki genel `except Exception` olmayan veya sayı olmayan personel numarasını hâlâ sessizce yutup yönlendiriyor (`app.py`, `/new`); tarih ve personel no birlikte geçersizse artık tarih mesajı dönüyor — Source: spec 0004, plan Risk 3 ve review round 1 R1-2 (`app.py:199-213`) — Severity: Low — Recommendation: CHANGE şeridi, BR-10 kalıbıyla görünür hata (400 + mesaj) — Deferred by: tahamucasiroglu, 2026-11-21, spec 0004 kapsamı dışında bırakıldı
