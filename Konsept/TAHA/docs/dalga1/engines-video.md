Video engines, composer and aligner are built and committed. The HyperFrames, Remotion and Revideo engines each rendered a real mp4 with Turkish text on this machine. All 45 tests in `TahaKonsept.Agent.Engines.Video.Tests` pass, including real ffmpeg and render runs (about 3 minutes on the first run, which installs the npm templates; about 45 seconds after that).

**Commit:** `509d74f` "Taha Konsept Video motorları: …; 45 test geçiyor". It covers my project, its tests, `docs/engines`, `.gitignore` (added `.tools-dev/`) and `Directory.Packages.props` (added `Microsoft.Extensions.DependencyInjection(.Abstractions)` 10.0.15). My two project lines in `TahaKonsept.slnx` had already gone in through another agent's commit. Nothing pushed.

**Two departures from the brief:**
- **Progress bar:** I couldn't build it with `drawbox` alone. In `drawbox`, `t` means line thickness, not time, and its sizes are worked out once rather than per frame, so the bar sat at full width the whole video. It now uses a semi-transparent `drawbox` strip plus a coloured bar that slides in with `overlay=x=-w+w*t/DURATION`. I measured the bar width in the output at several timestamps and it grows correctly.
- **Subtitle lines:** besides the 5-word cap, lines also break at about 22 characters on vertical video and 42 on horizontal. Without this, Turkish words are long enough that a 4–5-word line wrapped onto two rows.

**Public API** (namespace root `TahaKonsept.Agent.Engines.Video`)
- `DependencyInjection.VideoEnginesServiceCollectionExtensions.AddVideoEngines(this IServiceCollection)` registers everything as singletons:
  - `IVideoEngine` and `IAgentEngine` ×3: `HyperFramesVideoEngine` ("hyperframes"), `RemotionVideoEngine` ("remotion"), `RevideoVideoEngine` ("revideo").
  - `IComposer` → `FfmpegComposer`, and `IAligner` → `ProportionalAligner` ("proportional").
  - Helpers: `IMediaProbe` (ffprobe), `ISceneCodeGuard`, `ICaptionSubtitleWriter` (ASS).
  - It needs `IAgentPaths`, `IToolLocator`, `IProcessRunner` and `ILogger<>` registered elsewhere. Agent.Core's `ToolLocator` already matches this: npm resolves to `npm-cli.js`, and if it finds a Chrome headless shell, all three engines use it.
- `Authoring.AuthoringGuides` (static): `HyperFrames`, `Remotion`, `Revideo` (Markdown strings) and `ForEngine(string engineKey)`, which returns `null` for an unknown key.
- Public pieces other code may reuse:
  - `Engines.Base.NodeVideoEngineBase` (and `TsxVideoEngineBase`): public `TemplateDir`, `BuildPackageJson()` and `ParseProgress(string)`.
  - `FfmpegComposer.BuildFinalVideoFilter(...)` and `BuildNormalizeFilter(...)`.
  - `FfmpegFilterEscaper`, which does ffmpeg's two-level escaping for Windows paths. Tested on folder names containing spaces, an apostrophe and a comma.
  - `AssKaraokeSubtitleWriter`, `ProportionalAligner.ResolveRanges(...)`, `HyperFramesVideoEngine.ForceRootSize(...)`.

**How each part works:**
- **Shared engine base:** keeps one template per engine at `ToolsDir/engines/<key>` with pinned `package.json` versions and an `npm install`. Telemetry is switched off by environment variables. Fonts (Inter, Montserrat, Playfair Display, including the glyphs for ğ, ş and İ) come from the template, not the network. Scene code is checked before render for network calls, CDN or external URLs, `file://`, `Math.random`/`Date.now` and `eval`.
- **HyperFrames 0.9.43:**
  - Scene folder holds `index.html`, a local `gsap.min.js` (3.15.1), `fonts/` and `assets/`.
  - Render: `hyperframes render <dir> -o out.mp4 --fps N --quality looks --format mp4 --strict`.
  - Progress is read from its `@hf-progress {json}` output lines.
  - `ValidateSceneAsync` runs `hyperframes lint --json` and returns only the error-level findings, ready to send back to the LLM.
  - The root element's width and height are forced to the requested size.
  - It downloads Chrome via `browser ensure` into `.hf-cache` unless a Chrome headless shell is already available.
- **Remotion 4.0.543:** the LLM writes only `Scene.tsx`. The engine generates the entry, the root (size, fps and frame count from the request) and a font loader. Render uses the CLI; images are referenced as `staticFile("assets/x")`.
- **Revideo 0.11.0:** works, but needed two fixes found by testing.
  - Revideo always launches Chrome with `--single-process`, which crashes the page on current Chrome. The generated render script blocks that flag.
  - ffmpeg 8 breaks Revideo's own ffmpeg detection, so Revideo uses its bundled ffmpeg (`FFMPEG_PATH` is not passed to it).
- **`FfmpegComposer`:**
  - Each scene is scaled and padded to the target size and fps. A short scene is held on its last frame (`tpad` clone); a long one is cut.
  - Scenes are joined with the concat demuxer.
  - Optional layers: ASS karaoke subtitles (`\k` per word, bold, outlined, lower third, different font sizes for vertical and horizontal) and the progress bar.
  - Audio is AAC, padded with silence if short. With subtitles and bar both off, the video stream is copied without re-encoding.
  - A thumbnail jpg is written next to the output.
- **`ProportionalAligner`:** spreads each segment's [start, end] over its displayed words by letter count, with extra weight after punctuation. Segments without times share the gap between known neighbours (or the whole audio length) by the length of their spoken text. Output is always the displayed spelling, so a word shown as "Google" keeps that spelling even though the voice says "Gugıl".

**Guides:** embedded as resources and copied to `docs/engines/hyperframes-authoring.md`, `remotion-authoring.md` and `revideo-authoring.md`. A test checks that the copies match the embedded versions. The guides contain `{W}`, `{H}` and `{D}` placeholders that the server should replace with width, height and scene duration before sending.

**Tests:** cover subtitles, filter escaping, the aligner, the safety check, engine output parsing, DI registration and the guides. They also include real ffmpeg composition, checked with ffprobe for duration, size and audio, and pixel checks on the progress bar. A test confirms the HyperFrames lint rejection works.
- Tests that need ffmpeg skip if it isn't found in `TAHAKONSEPT_FFMPEG_DIR` or `.tools-dev/ffmpeg`.
- Render tests also need Node and the `.tools-dev` folder. Set `TAHAKONSEPT_SKIP_ENGINE_TESTS=1` to skip them.

**Dev tools:** a portable ffmpeg 8.1 (GPL build) is in `.tools-dev/ffmpeg`, and engine templates are cached in `.tools-dev/agent-tools`. Both are gitignored.

**For the integrator:**
- Call `services.AddVideoEngines()` in the agent host. Agent.Core's stage executors already consume `IEnumerable<IVideoEngine/IComposer/IAligner>`.
- The server-side prompt builder should send `AuthoringGuides.ForEngine(key)`, with the placeholders filled in, and feed `ValidateSceneAsync` errors back to the LLM.
- My part has no API routes and no Web pages or JS.
- Not verified: Remotion running against Agent.Core's own Chrome download instead of the one it fetches itself, and Revideo on the portable Node 22 runtime (only tested on Node 26 here).

Files are in `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/`:
- src/Desktop/TahaKonsept.Agent.Engines.Video/
- tests/TahaKonsept.Agent.Engines.Video.Tests/
- docs/engines/