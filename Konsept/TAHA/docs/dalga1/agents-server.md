The Agents module server side is built, registered in the API, and committed. My 30 unit tests pass, the 17 architecture tests pass, and every project I touched builds with 0 warnings. The authenticated end-to-end path (approve a code, get a token, connect to the hub) has not been run against a live host, because the dev database has no user yet. It is covered only by unit tests using EF InMemory and fakes.

**Live smoke test** (API host on port 7391, dev PG at :5433, stopped afterwards):
- The `agents` schema migration applied. The one ERR line in the log is EF's normal "history table not found" probe on first run.
- `device/start` returned codes and URLs; polling gave `authorization_pending`, then `slow_down`.
- Without login, `GET api/agents`, `approve` and the hub negotiate all return 401. A start request with no machine name returns a 400 validation error.
- `api/identity/setup/status` returns `setupRequired:true`. I did not create the admin account in your dev database.

**Two departures from the brief:**
- Identity's `ITokenIssuer`, `ITokenRevoker` and `ISessionRevocationNotifier` already existed, so there is no placeholder token interface to wire later. `IAgentTokenIssuer` is implemented by `IdentityAgentTokenIssuer`, which issues with `TokenClientKind.Agent` and revokes by token family. `AgentSessionRevocationNotifier` is registered with `TryAddEnumerable`. As a result, `Agents.Infrastructure` references `Identity.Application`; the architecture rules allow that.
- Identity's tokens do not carry an `agent_device_id` claim. The hub therefore matches the agent by user plus the machine name sent in `Hello`; the claim is used when present. If Identity adds `AgentClaimTypes.AgentDeviceId` to agent tokens, the claim path is used.

**API routes** (exactly the strings the Web controller already calls, plus two extras):

| Route | Access | Returns |
|---|---|---|
| `POST api/agents/device/start` | anonymous | `DeviceStartResponse`, now with an added `VerificationUrlComplete` (`…/Ayarlar?kod=XXXX-XXXX#masaustu`) |
| `POST api/agents/device/token` | anonymous | `TokenResponse` once; otherwise 400 with `code` = pending / slow_down / expired_token / access_denied |
| `POST api/agents/device/approve` | logged in | `{userCode}` in, `DeviceApproveResponse` out |
| `POST api/agents/device/deny` | logged in | extra route |
| `GET api/agents` | logged in | `AgentDeviceInfo[]` with live `isOnline` |
| `DELETE api/agents/{id}` | logged in | revokes the device, its token family, and disconnects it |
| `GET api/agents/artifacts/{id}` | agent JWT | the file, range requests supported |
| `DELETE api/agents/artifacts/{id}` | agent JWT | extra route: confirms download, file is deleted |
| SignalR `/hubs/agent` | `[Authorize]` | `AgentHub : Hub<IAgentClient>, IAgentHub`, all 7 methods |

**Public interfaces for other modules** (namespace `TahaKonsept.Modules.Agents.Application.Abstract`):
- **`IAgentGateway`** (scoped). Every call targets the user's newest connection that has sent `Hello`; if none, `agents.offline`.
  - `bool IsAgentOnline(Guid userId)`
  - `Task<AgentDeviceInfo?> GetOnlineAgentAsync(Guid userId, ct)`
  - `Task<Result> SendStageAsync(Guid userId, StageCommand cmd, ct)`
  - `CancelJobAsync`, `PauseJobAsync`, `OpenFolderAsync`, `OpenInSystemPlayerAsync`, `DeleteVideoAsync`, `RefreshCapabilitiesAsync`
  - `Task<Result<SpeechPreviewResult>> PreviewSpeechAsync(Guid userId, SpeechPreviewCommand cmd, TimeSpan? timeout = null, ct)`: waits for `SpeechPreviewCompleted`; on timeout returns `agents.preview_timeout`.
  - `Result<string> GetVideoUrl(userId, videoId)` gives `http://{lan}:{port}/videos/{id}?k={key}`; `GetThumbnailUrl` and `GetAgentFileUrl(userId, relativePath)` work the same way.
- **`IArtifactStore`**:
  - `SaveAsync(userId, jobId, name, contentType, Stream, sceneIndex?, ct)` stores the file under `App_Data/artifacts/{jobId:N}/` and returns an `ArtifactDownload` to put into `StageCommand.Artifacts`.
  - Also `OpenAsync`, `DeleteAsync`, and `DeleteJobArtifactsAsync`.
- **`IAgentStageEventHandler`**: `OnStageProgressAsync`, `OnStageCompletedAsync`, `OnStageFailedAsync`, each taking `(AgentCallContext, report, ct)`. The default is a no-op.
- **`IAgentLogSink`**: `WriteAsync(AgentCallContext, AgentLogEntry, ct)`. The default writes to Serilog.
- **`IAgentSessionRevoker`**: `RevokeUserAsync`, `RevokeTokenFamilyAsync`, `DisconnectDevice`.

**New shared contracts** (all in `Shared.Contracts/Agent`):
- `AgentDeviceInfo`
- `AgentClaimTypes`
- `Pairing/DeviceApproveRequest`
- `Pairing/DeviceApproveResponse`

**Web:**
- New `wwwroot/js/pages/settings-agent.js` (`TK.settingsAgent`), using `TK.api` only. It shows the device list with an online dot, version, GPU and last seen, polling every 10 seconds. It also has the "Kodu gir" approve form (pre-filled from `?kod=`) and a remove button with confirmation.
- I removed the agent code from `ayarlar.js`.
- In `Views/Ayarlar/Index.cshtml` I added the three-step instructions, a download link placeholder at `~/downloads/TahaKonsept.Agent-win-Setup.exe`, and the script tag.
- The existing `AyarlarController` actions already matched, so the controller is unchanged.

**Tests** (`tests/TahaKonsept.Modules.Agents.Tests`, 30 passing): the pairing state machine (pending → approved → token once, expiry, slow_down, deny); the pairing service, including re-pairing the same machine (the device record is reused and the old token family revoked); the URL builder (IPv4, IPv6, key escaping); and the gateway and hub (offline errors, Hello storing an encrypted stream key, commands, send failure, preview result and timeout, revoked device rejected, disconnect marks offline, remove device).

**Commits:** `be44d66` (server side plus the shared `slnx`, `ApiModules.cs` and `TahaKonsept.Api.csproj` registration) and `a1f6bfc` (Web).

**Left for the integrator:**
1. Production should register its real `IAgentStageEventHandler`, and Diagnostics its `IAgentLogSink`. Agents uses TryAdd defaults, so their modules must stay before Agents in `ApiModules`, or use `Replace`.
2. The download link points to `wwwroot/downloads/TahaKonsept.Agent-win-Setup.exe`, which doesn't exist yet. The Velopack publish step needs to put the installer there.
3. When API and Web run on different hosts, set `Agents:VerificationUrl` (or `Agents:WebPort`/`VerificationPath`). Otherwise the verification URL is built from the request's host name plus port 7110.
4. If an agent sends `Hello` with a different machine name than it paired with, and the token has no device claim, the connection is cut. Adding the device claim in Identity removes this risk.

Key paths:
- `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/src/Modules/Agents/`
- `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/tests/TahaKonsept.Modules.Agents.Tests/`
- `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/src/Hosts/TahaKonsept.Web/wwwroot/js/pages/settings-agent.js`