The desktop agent core runtime is done. `TahaKonsept.Agent.Core` builds with 0 warnings and 0 errors, and all 62 tests in `TahaKonsept.Agent.Core.Tests` pass. Two commits: `7bffca5` (intermediate) and `31700c6` (final).

Not tested against a real server: the SignalR hub flow, the Kestrel firewall prompt and the real prerequisite downloads. The host smoke test only covers the unpaired path (video server answers, hub waits for pairing, clean stop). Nothing on the API or Web side was added: no DbContext, migration, route or page.

**Entry point for the Avalonia app**
- `TahaKonsept.Agent.Core.DependencyInjection.AgentCoreServiceCollectionExtensions.AddAgentCore(this IServiceCollection, AgentOptions)`. It registers everything and adds Serilog logs at `%LocalAppData%/TahaKonsept/Logs/agent-*.log`, kept 7 days, via `AddTahaSerilog`.
- `TahaKonsept.Agent.Core.Hosting.Abstract.IAgentHost`: `Task<Result> StartAsync(ct)`, `Task StopAsync(ct)`, `IsRunning`, plus `Hub`, `Pairing` and `Prerequisites` for the UI.
- `AgentOptions` (`TahaKonsept.Agent.Core.Configuration`): `DefaultServerBaseUrl` (default `http://192.168.1.102:7111`), `VideoServerPort` (7120), `RootDirectory`, `AgentVersion`, `MaxConcurrentStages` (1), `EnableFileLogging`, `StartVideoServer`.
- **Shutdown:** dispose the service provider with `DisposeAsync()`. The hub connection is async-disposable only, so a plain `Dispose()` throws.

**Interfaces the UI or other modules will use** (all under `TahaKonsept.Agent.Core.*`)
- `Pairing.Abstract.IDevicePairingService`: `StartAsync`, `WaitForApprovalAsync(DeviceStartResponse, ct)`, `PairAsync`, `Unpair`, `Status`/`StatusChanged`. `PairingStatus` carries the user code, verification URL and state.
- `Hub.Abstract.IAgentHubConnection` (also serves as `IAgentReporter`): `Status`/`StatusChanged`, `SendHelloAsync`. It reconnects on its own, sends Hello on every connect, sends a heartbeat every 30 s, and queues completed/failed reports while offline.
- `Prerequisites.Abstract.IPrerequisiteInstaller`: `GetStatus()`, `AreRequiredInstalled()`, `InstallAsync(optionalIds, IProgress<PrerequisiteProgress>, ct)`, `InstallOneAsync`. Progress gives step name, step percent, overall percent and a log line.
  - Required: Node 22 LTS, FFmpeg (gyan.dev), Chrome Headless Shell (Chrome for Testing).
  - Optional: VC++ Redist (detection only; install asks for admin rights), uv, and Python 3.11 (installed through uv, in the "Python + CUDA" group).
- `Auth.Abstract.IAgentAuthService` and `ITokenStore` (DPAPI, CurrentUser). If the server rejects a token refresh (for example after logout from the web), the agent signs out and waits to be paired again.
- Smaller pieces: `IAgentSettingsStore` (`Config/settings.json`, random stream key per install), `IAgentApiClient.DownloadFileAsync`, `Execution.Abstract.IJobRunner`, `Videos.Abstract.IVideoLibrary`/`IVideoIndex`, `VideoServer.Abstract.IVideoServer`, `Capabilities.Abstract.ICapabilityDetector`. `AgentPaths` and `ToolLocator` implement the existing `IAgentPaths` and `IToolLocator`; `ProcessRunner` kills the whole process tree on cancel or timeout.

**API routes the agent calls**
- `POST api/agents/device/start` and `POST api/agents/device/token` (pending / slow-down / expired / denied codes from `DeviceTokenErrorCodes`).
- `POST api/identity/refresh` with body `{refreshToken}`.
- `GET api/agents/artifacts/{id}`, used when an artifact arrives without a URL.
- SignalR at `/hubs/agent`, token passed as `access_token`. All handlers in `IAgentClient` are wired.

**Local video server** (Kestrel, `0.0.0.0:<port>`, every route except `/health` needs `?k=<streamKey>`, wrong key gives 403)
- `GET|HEAD /videos/{id}` serves the mp4 with Range support (200 / 206 / 416).
- `GET|HEAD /thumbs/{id}` and `/previews/{id}.wav`, plus `/health`.

**Stage payloads, added to Shared.Contracts** (`src/BuildingBlocks/TahaKonsept.Shared.Contracts/Agent/Payloads/`, namespace `TahaKonsept.Shared.Contracts.Agent.Payloads`, one type per file)
- Inputs: `ImagesStagePayload`/`ImagePromptItem`, `SpeechStagePayload`/`SpeechSegmentItem`, `AlignStagePayload`/`AlignSegmentItem`, `SceneRenderStagePayload`, `ComposeStagePayload`, `MuxStagePayload` (just `Title`).
- Outputs: `SpeechStageResult`/`SpeechSceneTiming` and `AlignStageResult`/`CaptionWordItem`, returned in `OutputJson`.
- `AgentArtifactNames` holds the standard file names (`speech.wav`, `captions.json`, `scene-{i}.mp4`, `composed.mp4`). If the server sends online-generated files under these names, the agent uses them as if they were local.

**How the stages behave**
- Each stage writes into its own folder in the job workspace (`images/`, `speech/`, `align/`, `scenes/{i}/`, `compose/`, `final/`).
- If `InputHash` changes, that stage's folder is wiped. Otherwise existing outputs are skipped, which is how resume-after-pause works.
- Pause cancels the current step, keeps files, and reports `agent.paused` (retryable). Cancel reports `agent.cancelled` and deletes the workspace. Shutdown reports `agent.shutdown`.
- SceneRender lints the scene first and fails with `agent.scene_invalid` if the engine rejects it.
- Speech also writes per-scene WAVs, the combined `speech.wav` and scene timings.
- Mux copies the video to `Videos/<title> (<id8>).mp4`, makes a thumbnail, and registers it in the video index.

**Shared files I touched**
- `TahaKonsept.slnx`: added both projects.
- `Directory.Packages.props`: added `System.Security.Cryptography.ProtectedData` 10.0.15. Another agent's commit had already included this line.
- `ApiModules.cs`, `TahaKonsept.Api.csproj` and `_Layout.cshtml` were not touched.

**What the integrator must wire**
- **Server Production module:** build `StageCommand.PayloadJson` from the payload records above, and send one SceneRender command per scene with `SceneIndex` set.
- **Server Agents hub and API:** must implement `IAgentHub` and the device, refresh and artifact routes above with these exact paths, and reject revoked tokens on reconnect.
- **Engine projects:** must register their `IVideoEngine`, `IComposer`, `IAligner` (key `proportional` is the default), `ISpeechEngine` and `IImageEngine` in DI. With no engine registered, the stage fails with `agent.engine_not_found`.
- **Packaging:** Agent.Core references the ASP.NET Core framework (for Kestrel), so the Velopack package must include the ASP.NET runtime.
- **Workspace cleanup:** job folders are kept after Mux so "Yeni sürüm" can reuse stages. They are only deleted on cancel or `DeleteVideo`, so nothing limits their disk use yet.
- **Not built:** the API's prerequisite manifest endpoint; download URLs are hard-coded in the prerequisite classes. Not reading it was my choice, not something the task listed.

**Tests** (`tests/TahaKonsept.Agent.Core.Tests/`): the process runner, agent paths, settings store, Range parsing, the Kestrel video server end to end, stage dispatch with fake engines, the job runner, pairing states with a fake HTTP handler, token refresh, the parsers, DI resolution and a host smoke test.

Paths:
- C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/src/Desktop/TahaKonsept.Agent.Core/
- C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/tests/TahaKonsept.Agent.Core.Tests/
- C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/src/BuildingBlocks/TahaKonsept.Shared.Contracts/Agent/Payloads/