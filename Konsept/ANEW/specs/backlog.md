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

- **B-1** — Docker Desktop (WSL2) kurulu değil; Testcontainers entegrasyon testleri için gerekli — Source: bootstrap 2026-11-14 — Severity: Medium — Recommendation: walking skeleton PLAN gate'inden önce agent kurmayı dener, olmazsa geliştiriciye hatırlatır (`requires integration: docker info`) — Deferred by: tahamucasiroglu, 2026-11-14, bootstrap'te kod/test yok
- **B-2** — Kayıtta e-posta doğrulaması yok — Source: bootstrap 2026-11-14 — Severity: Low — Recommendation: internete açılmadan önce ekle — Deferred by: tahamucasiroglu, 2026-11-14, şimdilik yerel ağ ve tek kullanıcı
- **B-3** — HTTPS + TLS sonlandıran reverse proxy (ör. Caddy) yok; site LAN'da HTTP — Source: bootstrap 2026-11-14 — Severity: High (internete açılırsa) — Recommendation: internete açılış spec'inde zorunlu; yapı ayarla HTTPS'e geçebilir kurulur — Deferred by: tahamucasiroglu, 2026-11-14, yalnızca yerel ağ
- **B-4** — Login'de "kalan hak" mesajı e-postanın kayıtlı olduğunu belli ediyor (user enumeration) — Source: bootstrap 2026-11-14 — Severity: Low — Recommendation: internete açılırken yeniden değerlendir — Deferred by: tahamucasiroglu, 2026-11-14, LAN tek kullanıcı için kabul
- **B-5** — Araştırma raporu: video motoru alternatifleri (Hyperframes/Remotion/Revideo/…), yerel ses ve görsel modellerinin çalıştırma yolu (Python/ONNX/ComfyUI), H-4 render sandbox yöntemi — Source: bootstrap 2026-11-14 — Severity: Medium — Recommendation: bootstrap sonrası `raporlar/` altında; sonucu ADR/spec olur — Deferred by: tahamucasiroglu, 2026-11-14, bootstrap kapsamı dışında
- **B-6** — Uygulama ikonu için Claude Web'e verilecek ikon promptu (ikon `source/` içine konacak) — Source: bootstrap 2026-11-14 — Severity: Low — Recommendation: bootstrap sonrası hazırla — Deferred by: tahamucasiroglu, 2026-11-14, ilk_promt.txt isteği, bootstrap kapsamı dışında
