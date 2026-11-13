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

- **B-1** — `UseStatusCodePagesWithReExecute("/Home/NotFound")` gövdesiz her 4xx/5xx yanıtını 404 "Sayfa bulunamadı"ya çeviriyor (`src/LeaveManager.Web/Program.cs:29`, `src/LeaveManager.Web/Controllers/HomeController.cs:16-21`) — Source: spec 0001, review round 1 — Severity: Low
  — Recommendation: `/Home/Status/{0}`; 404'te NotFound view'ı, diğer kodlarda özgün durum kodu + genel mesaj; spec 0002 planında ele alınsın — Deferred by: tahamucasiroglu, 2026-11-07, bugün etkilenen yol yok; antiforgery/yetki yanıtları spec 0002 ile gelir
