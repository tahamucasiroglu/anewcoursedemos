# Dalga 2 — production-core (Production modülü, API tarafı)

The Production module (API side) is built and registered in the API host. Everything builds with 0 warnings; 36 tests pass (39 after the integrator's adapter tests), and so do the 17 architecture tests. The API host started on port 7951 against the dev database: the `production` schema migration applied, the orchestrator loop starts, and every production route returns 401 without a login. No logged-in request, real agent or real LLM call has been tried.

**Projects** (under `Konsept/TAHA/`)
- `src/Modules/Production/TahaKonsept.Modules.Production.{Domain,Application,Infrastructure,Api}`
- `tests/TahaKonsept.Modules.Production.Tests`
- `src/BuildingBlocks/TahaKonsept.Shared.Contracts/Production/` (22 new types; existing enums unchanged)
- Shared files edited: `TahaKonsept.slnx`, `ApiModules.cs` (`new ProductionModule()`), `TahaKonsept.Api.csproj`.

**Contracts (`TahaKonsept.Shared.Contracts.Production`)**
- Enums: `MediaMode` (Off|Local|Online), `QueueItemStatus` (Waiting|Running|Paused|Done|Failed|Cancelled), `QueueRunState` (Idle|Running|Paused), `PronunciationStatus` (Suggested|Approved).
- Settings: `ProductionSettingsDto` — codeModelKey, codeEffort, videoEngine, imageMode, imageModelKey, speechMode, speechModelKey, voiceId, speechSpeed, referenceAudioArtifactId, normalizeBySpeechModel, burnSubtitles, showProgressBar, fps.
- Queue: `AddToQueueRequest {conceptIds, settings?}`, `MoveQueueItemRequest {direction}`, `QueueStateDto {status, message, agentOnline, budgetBlocked, items}`, `QueueItemDto`, `StageSummaryDto`.
- Jobs: `JobDetailDto`, `StageRunDto`.
- Videos: `VideoSummaryDto`, `VideoDetailDto` (scenes with display and spoken text, concept document, cost breakdown, playbackUrl, thumbnailUrl, playbackMessage), `VideoSceneDto`, `StageCostDto`, `NewVersionRequest {settings?, changed, note}`, `RegenerateRequest {document?, settings?}`.
- Pronunciation: `PronunciationDto`, `SavePronunciationRequest {id?, written, spoken, isSuggestion}`, `PronunciationPreviewRequest {text, normalize}`, `PronunciationPreviewResponse {displayText, spokenText, audioUrl, audioBase64, mimeType, source, message}`.

**API routes**

| Area | Routes |
|---|---|
| Settings | `GET` / `PUT api/production/settings` |
| Queue | `GET` / `POST api/production/queue`, `POST queue/{id}/move`, `DELETE queue/{id}` (cancels the job if running), `POST queue/start\|pause\|resume`, `GET queue/{id}/status`, `POST queue/clear-finished` |
| Videos | `GET api/production/videos?take=`, `GET` / `DELETE videos/{id}`, `POST videos/{id}/open-folder`, `POST videos/{id}/open-in-player` (alias `open-player`), `POST videos/{id}/new-version`, `POST videos/{id}/regenerate` |
| Pronunciation | `GET` / `POST api/production/pronunciations`, `DELETE pronunciations/{id}`, `POST pronunciations/preview` |
| Jobs | `GET api/production/jobs/{id}` |

**Public interfaces** (namespace `TahaKonsept.Modules.Production.Application.Abstract`)
- `IConceptSource`: `Task<ConceptSnapshot?> GetAsync(Guid userId, Guid conceptId, CancellationToken)`. `ConceptSnapshot` = ConceptId, Title, ProfileId, Document. Fallback `UnavailableConceptSource` (TryAdd) returns null. *(Entegratör: `Production.Infrastructure/Integration/Concrete/ConceptsConceptSource` ile `Replace` edildi.)*
- `IPronunciationSuggestions.AddSuggestionsAsync(userId, conceptId?, source?, entries, ct)` — called by `JobFactory` at job start with the concept document's `Pronunciations`.
- Services: `IProductionSettingsService` (incl. `GetForUserAsync(userId)`), `IProductionQueueService`, `IVideoService`, `IPronunciationService`, `IJobQueryService`.
- Pipeline: `IStageHandler` (11), `IStageInputHasher` (SHA-256), `IReusePlanner.PlanAsync`, `IScenePlanner`, `IJobFactory`, `IJobAdvancer`, `IJobFinalizer`, `IProductionOrchestrator`, `IProductionSignal`, `IUserJobLock`.
- `ProductionStageEventHandler` replaces the Agents no-op `IAgentStageEventHandler` (`Replace`; order-independent). `ProductionStatsContributor`: `videos.total`, `videos.perDay` (30 days, Turkey time).
- DI: `AddProductionModule(config)` = DbContext + `AddProductionCore()` (+ concept adapter) + `ProductionOrchestratorService`. `AddProductionCore` calls `AddSpeechTextNormalization()`.

**How the pipeline behaves**
- **Stage graph:** PLAN order; SceneCode and SceneRender per scene, the rest per video. Each stage has input hash, output hash, location (server/agent).
- **Hashes:** ScenePlan durations from narration length only (voice change does not rerun scene code); SceneRender hash has no speech input; Mux hash includes the video id.
- **Scene code:** one LLM call per scene with `AuthoringGuides.ForEngine` ({W}/{H} filled; cacheable system prompt). Output limit 64000 tokens for scenes > 40 s, else 32000. Format-check failure → one retry with the error; agent lint rejection (`agent.scene_invalid`) → one rewrite.
- **Online speech** (one WAV per scene) joined into `speech.wav` on the server and sent as an artifact; **online images** become per-scene artifacts; **local** image/speech use agent payloads.
- **Agent offline:** "Ajan bekleniyor"; continues when the agent returns. An agent stage with no progress for 10 minutes is resent, up to 3 attempts.
- **Pause** sends `PauseJob`; `agent.paused` puts the stage back to pending; **Resume** skips finished stages.
- **Budget:** `EnsureCanStartProductionAsync` before each new job; on failure the queue goes idle with `budgetBlocked` (Warning logged); the running job finishes.
- **New version:** linked to the base job; unchanged server stages copied; the agent reuses the base workspace and skips unchanged files. Online speech/image regeneration → fresh workspace.
- **Activity log:** stage start/finish (per-scene stages once); finished video: "Video hazırlandı — süre 9 sn, başlangıç 10:30, bitiş 10:31, $0.15 · title".

**Tests** (36/36 at hand-off): EF InMemory with fake LLM, media, agent, artifact store, budget guard and concept source — reuse decisions, orchestrator, queue, event handler, new version, helpers, pronunciation service.

**Commits:** `531cad5`, `fdd449a`, `9c3272b`, `5db2287`, `510dee7`, `ed303b5`, `78de193`. Not pushed.

**For the integrator / Web agent**
1. Concept adapter — *done by the integrator.* `IConceptPronunciationSource` is not used (the document's `Pronunciations` already carries the same data).
2. Web JS field names — *production-web uses the DTO names* (`codeModelKey`, `burnSubtitles`, `Done`, …).
3. Online speech must return WAV (`Format="wav"`); MP3 cannot be joined on the server yet.
4. A server stage holds the user lock while it runs, so deleting a running queue item waits until the current LLM call ends.
5. Online artifacts are deleted when a job finishes; a later new version in a fresh workspace regenerates them (new cost).
6. Routes beyond `docs/WEB-KABUK.md`: `queue/{id}/status`, `queue/clear-finished`, `jobs/{id}`, `open-player` alias.
