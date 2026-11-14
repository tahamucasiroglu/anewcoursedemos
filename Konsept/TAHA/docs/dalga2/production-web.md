# Dalga 2 — production-web (Konseptten Video, Üretilenler, Ayarlar → Okunuş sözlüğü)

The Konseptten Video, Üretilenler and Ayarlar → Okunuş sözlüğü pages are built and committed. They have never been opened in a browser (Chrome extension not connected), so layout, dark mode, modals and console errors are unchecked.

**What was checked**
- **Build:** `TahaKonsept.Web` 0 warnings, 0 errors.
- **Tests:** the 17 architecture tests pass (no `fetch`, thin controllers).
- **Syntax:** `node --check` passes on all 5 JS files touched.
- **Smoke test:** Web on 7972 against a fake API on 7971 returning Production-shaped data. Without login the three pages redirect to login; logged in, the pages and all 12 Ajax GET endpoints return 200; write calls (settings PUT, add to queue, move, concept PUT, pronunciation preview, clear finished) went through with the CSRF token and returned 200.
- **Stage-rerun predictions** (checked in node):

| Change | Stages that rerun |
|---|---|
| Only speech | Speech, Align, Compose, Mux |
| Only engine | SceneCode, SceneRender, Compose, Mux |
| Images turned on | ScenePlan, SceneCode, Images, SceneRender, Compose, Mux |
| Subtitles | Compose, Mux |
| Nothing | Mux |

- **Real API:** not run by this part (the integrator smoke checked the real API + Web redirects).

**Konseptten Video**
- **Model strip:** each item opens a modal; closing saves production settings. Code model + effort; engine; image (Kapalı / Yerel / Online + model); speech (Kapalı / Yerel / Online + model, voice, speed). Voices from `api/providers/voices` for online models, F1–F5 / M1–M5 for Supertonic, a text box otherwise. Badges: Yerel/Online, language label ("Sadece Türkçe", "Türkçe yok" in red), "≥ N GB VRAM", price; red "VRAM yetersiz" when the agent reports less VRAM.
- **Toggles:** Altyazı and Üst ilerleme çubuğu.
- **Left:** approved concepts with search and select-all → "Sıraya ekle"; clicking a concept opens an editor (title, hook, summary, scenes) saved through the Concepts PUT, with an unsaved-changes warning.
- **Middle — queue:** ↑ ↓ ✕ (confirm for a running item), Sırayla üret, Durdur, Kaldığı yerden devam, clear finished. Running item shows the full stage timeline (status, progress, duration, cost, "sahne x/y", reused/skipped/waiting notes); others show stage dots; finished items get a play button. Polling every 2 s with `TK.poll`. Banners for "Ajan çevrimdışı" and budget exhausted.
- **Right:** produced videos; plays in an HTML5 `<video>` from `playbackUrl` or shows the API message.

**Üretilenler**
- **List:** thumbnail (from the detail call, one at a time), title, duration, date, cost, version, search.
- **Player:** sound, fullscreen, Klasörde aç, Sistem oynatıcısında aç, Sil (confirm modal).
- **Yeniden üret:** concept edit modal first, then `regenerate` with the edited document.
- **Yeni sürüm:** tabs for engine/code model, image, speech, subtitles; live rerun/reuse prediction; sends `{settings, changed, note}`.
- **Right tabs:** Seslendirme + sahneler (display vs spoken text with changed words highlighted, click time to seek), Konsept metni (sources, tags), Ayrıntılar ve ücret (per-stage cost/duration/tokens, models, timings, version history).

**Ayarlar → Okunuş sözlüğü**
- Table with Tümü / Öneri / Onaylı filter, search, add, edit (modal with approved switch), delete (confirm), approve one, approve all.
- Dinle plays `audioUrl` or a `data:` URL from `audioBase64`.
- Test box: spoken text, a word-by-word change list computed in the page (uses `changes` if the API ever returns it), audio; "Sözlüğe ekle" copies a change into the add form.

**Web files** (under `src/Hosts/TahaKonsept.Web/`)
- Controllers: `KonsepttenVideoController` gained `Sesler`, `Ajanlar`, `Konsept` (GET and PUT), `BitenleriTemizle`, `Video`. `AyarlarController` summary comments only.
- Views: `Views/KonsepttenVideo/Index.cshtml`, `Views/Uretilenler/Index.cshtml`, Okunuş section of `Views/Ayarlar/Index.cshtml` (+ `Head` section and two script tags).
- JS (`wwwroot/js/pages/`): `production-shared.js` (new), `konseptten-video.js`, `uretilenler.js`, `settings-pronunciation.js` (new); dictionary code removed from `ayarlar.js`.
- CSS (`wwwroot/css/pages/`): `konseptten-video.css`, `uretilenler.css`, `settings-pronunciation.css`.

**Commits:** `0100aad`, `1b34fef`. Not pushed.

**For the integrator**
1. Ajax bodies are passed through as raw JSON (the Web host has no string-enum converter); the JS uses DTO field names, status `Done`, progress 0–100.
2. The test box costs money with online voices (preview always synthesizes). A `POST api/production/pronunciations/normalize` route returning `{display, spoken, changes}` would avoid that.
3. Real data needs the concept adapter — *done by the integrator.*
4. VRAM warnings use `vramMb` from `api/agents`; without it no "yetersiz" warning.
5. `docs/WEB-KABUK.md` not updated with the new MVC actions — *integrator added a wave-2 section.*
