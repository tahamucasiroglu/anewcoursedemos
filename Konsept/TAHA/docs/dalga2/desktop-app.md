# Dalga 2 — desktop-app (Avalonia tepsi ajanı + kurulum)

The desktop agent is built and committed: the Avalonia tray app, its first-run setup and status windows, and the installer. The app builds with 0 warnings, and all 42 tests in `TahaKonsept.Agent.Tests` pass. The pack script produced `TahaKonsept.Agent-win-Setup.exe` at 83.9 MB. The real exe ran for 12 s and stayed alive; its log had no warnings or errors, the setup window opened in dark mode, and the tray icon showed. A second launch exited immediately with code 0.

Not tested: pairing against the real API, installing prerequisites or engines for real, updates, and running the Setup.exe installer itself.

**What it does**
- **Entry point:** `Program.Main` calls `VelopackApp.Build().Run()` first, then takes a named mutex; a second launch brings the running copy to the front and exits. The app keeps running after its windows close.
- **Tray icon:** "Siteye git", "Kurulum / Durum", "Çıkış". Clicking opens setup or status; tooltip shows connection state.
- **Startup with Windows:** first launch adds HKCU `Run` once, with `--tray`. A `--tray` start opens no window unless setup is needed. Status window has a checkbox to turn it off.
- **Setup window** (warm coffee theme, follows dark mode), three steps:
  - Components: server URL, GPU/VRAM line, one row per prerequisite with a progress bar, optional Python + uv group, log panel, cancel and retry.
  - Pairing: the user code in large type, "Tarayıcıda onayla" (opens the pre-filled approval page), new-code and cancel.
  - Done.
- **Status window:** connection state, paired account name (from the stored token), GPU, current job progress, engine list with installed / not installed, VRAM labels, "Yetersiz VRAM" warning and "Kur" with progress; opens logs and videos folders, the website; unpair / pair again.
- **Wiring:** `AddDesktopAgent(options)` calls `AddAgentCore`, `AddVideoEngines`, `AddSpeechEngines`, `AddImageEngines`. Launch starts `IAgentHost`; exit stops it then `DisposeAsync`. Logging via the core's Serilog (7 days).
- **Engines:** listed directly from the three engine interfaces (no `IAgentEngine` registration needed).
- **Updates:** checks `<web>/downloads/` in the background and installs silently on exit; skipped when not installed through Velopack.

**Main types** (namespace `TahaKonsept.Agent.*`, interface → abstract base → concrete):
- `Runtime.IDesktopAgentRuntime`: `StartAsync(DesktopLaunchOptions, ct)`, `StopAsync(ct)`
- `Navigation.IWindowNavigator`: `ShowSetup(bool pairingOnly)`, `ShowStatus()`, `ShowDefault()`, `IsSetupRequired()`, `CloseAll()`
- `Tray.ITrayIconController`: `Show(Application)`, `ExitRequested`
- `SingleInstance.ISingleInstanceGuard`, `Startup.IStartupRegistration`, `Shell.IDesktopLauncher`, `Settings.IDesktopAppSettingsStore` (`Config/desktop-settings.json`), `WebSite.IWebSiteLocator` (API port 7111 → web port 7110), `Account.IPairedAccountReader`, `EngineList.IEngineCatalog`, `Updates.IAgentUpdater`
- View models: `SetupViewModel`, `PairingViewModel`, `SetupStepViewModel`, `StatusViewModel`, `EngineItemViewModel`

**Packaging**
- `deploy/agent/pack-agent.ps1`: publish Release win-x64 self-contained, then `vpk pack` (packId `TahaKonsept.Agent`, version from `VERSION`, icon, `--runtime win-x64`).
- Copies Setup.exe, `releases.win.json`, `assets.win.json`, `RELEASES` and the full `.nupkg` to `src/Hosts/TahaKonsept.Web/wwwroot/downloads/` (gitignored).
- vpk 1.2.161 is a local tool in `.config/dotnet-tools.json`. Not code-signed.
- `deploy/agent/make-icon.ps1` draws a temporary icon from the web logo; if `source/icon.ico` is added, the script copies that instead.

**Tests:** setup steps, setup state machine, pairing, engine install and VRAM warning, status window, startup/shutdown order, helper services.

**Shared files touched:** `TahaKonsept.slnx`, `Directory.Packages.props` (`CommunityToolkit.Mvvm` 8.4.2), `.config/dotnet-tools.json` (vpk), `.gitignore` (downloads, `artifacts/agent/`).

**Commits:** `4ff63f2`, `1a8229e`, `c2dc8e3`. Not pushed.

**For the integrator**
- `.nupkg` static serving — *done by the integrator* (`FileExtensionContentTypeProvider` mapping in `WebApplicationExtensions`; `.json` is served by default).
- The smoke test briefly wrote the Debug exe into HKCU `Run`; removed afterwards.
- Dev-only env vars: `TAHAKONSEPT_AGENT_ROOT`, `TAHAKONSEPT_AGENT_VIDEO_PORT`, `TAHAKONSEPT_AGENT_SERVER`.
- Window code-behind files are named `App.cs`, `SetupWindow.cs`, `StatusWindow.cs` (not `*.axaml.cs`) because of the file-name architecture rule.

Key paths: `src/Desktop/TahaKonsept.Agent/`, `tests/TahaKonsept.Agent.Tests/`, `deploy/agent/pack-agent.ps1`, `deploy/agent/make-icon.ps1`, `src/Hosts/TahaKonsept.Web/wwwroot/downloads/TahaKonsept.Agent-win-Setup.exe`.
