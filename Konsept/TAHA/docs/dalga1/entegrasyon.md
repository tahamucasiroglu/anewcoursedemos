Wave 1 is integrated: the full solution builds with 0 warnings and 0 errors, all 352 tests pass, and the API applied all five module migrations against the dev DB and reports the database ready. Everything is committed as `627cf99`, nothing pushed.

**Registration check (task 1).** Nothing was missing:
- All 22 projects from the 10 parts are in `TahaKonsept.slnx`: 20 module projects, 5 desktop projects and 11 test projects.
- `ApiModules.cs` registers Identity, Profiles, Providers, Agents and Diagnostics, and `TahaKonsept.Api.csproj` references each module's Api project.
- Concepts and Production are still commented out in `ApiModules.cs`, because those modules don't exist yet.

**Cross-module adapters (task 2)**
- Three of the links in the brief were already done by the part agents, and I left them as they were:
  - Agents to Identity token issuing: `IdentityAgentTokenIssuer` calls `ITokenIssuer`.
  - Identity logout and ban to Agents disconnect: `AgentSessionRevocationNotifier` is registered.
  - Diagnostics to Identity's user list: `IdentityAdminUserDirectory` calls `IUserDirectory`.
- **New: agent logs into the activity log.** `ActivityAgentLogSink` in `src/Modules/Agents/TahaKonsept.Modules.Agents.Infrastructure/Logging/Concrete/` is now the default `IAgentLogSink`.
  - Every entry still goes to Serilog.
  - Entries other than Debug also go to Diagnostics `IActivityLogger.LogAsync`, with source `Agent`, the agent's user as owner, and details (category, device, job, timestamp, exception).
  - `Agents.Infrastructure` now references `Diagnostics.Application`.
- **New: spending in the admin panel.** `ProvidersStatsContributor` in `src/Modules/Providers/TahaKonsept.Modules.Providers.Infrastructure/Stats/Concrete/` feeds the stats page.
  - It gives `spend.monthUsd` (this month, all users, month boundary in Turkey time) and `spend.perDayUsd` (last 30 days by Turkey-time day).
  - It is registered in `AddProvidersModule`, and `Providers.Infrastructure` now references `Diagnostics.Application`.
- Both new references follow the rule that a module may reference another module's Application project only. The 17 architecture tests pass.
- **New tests:**
  - `tests/TahaKonsept.Modules.Agents.Tests/Logging/ActivityAgentLogSinkTests.cs` (2 tests), with a fake logger in `Support/RecordingActivityLogger.cs` and `Support/RecordedActivity.cs`.
  - `tests/TahaKonsept.Modules.Providers.Tests/Stats/ProvidersStatsContributorTests.cs` (2 tests).

**Build and tests (task 3).** Full solution build with `--no-incremental`: 0 warnings, 0 errors. The full test run gave 348 passed, 0 failed, 0 skipped. After adding the 4 new tests I ran Agents (32/32), Providers (42/42) and the architecture tests (17/17) again, for 352 in total.

**Live run (task 4).** I ran the API on port 7191 and the Web on port 7190, against the dev DB through the tunnel on 5433, then stopped both processes.
- **Migrations:** applied in order: Identity, Profiles, Providers, Agents, Diagnostics.
- **Health:** `/api/health` returned `database:"ready"`.
- **Setup status:** `api/identity/setup/status` returned `{"setupRequired":true}`.
- **Login and auth:** a bad login returned 401 with `identity.invalid_credentials`. The profiles, providers, agents and diagnostics routes all returned 401 without a login.
- **Pairing:** `device/start` returned codes and verification URLs.
- **Web:** `/` redirected to `/Account/Login`, which redirected to `/Kurulum`, which loaded with 200.

**Remaining gaps**
1. **No logged-in test:** the dev DB has no admin account yet. I didn't run the one-time setup, so no logged-in request or approve-code/agent-hub flow has been tested.
2. **Test row in the dev DB:** my `device/start` check left one pending pairing row. It expires on its own.
3. **No modules yet:** Concepts and Production don't exist. They will need to:
   - register their own stats contributors (concepts and video counts show 0 until then);
   - provide the real handler for agent stage events (`IAgentStageEventHandler`);
   - call `IBudgetGuard.EnsureCanStartProductionAsync` before starting work;
   - use `IProfileReader` and `IProfilePromptBuilder`;
   - send `AuthoringGuides.ForEngine(key)` with the placeholders filled in.
4. **No desktop app yet:** there is no desktop host (Avalonia app). It will need to call `AddAgentCore`, `AddVideoEngines`, `AddSpeechEngines` and `AddImageEngines`. The speech and image engines are not registered as `IAgentEngine`, so the setup screen will need that forwarding or must list engines another way.
5. **No device id in agent tokens:** Identity's agent tokens don't carry an `agent_device_id` claim, so the hub matches an agent by user plus machine name. Adding that claim in `ITokenIssuer` for agent tokens would remove the risk that a renamed machine gets disconnected.
6. **Missing installer:** `wwwroot/downloads/TahaKonsept.Agent-win-Setup.exe` doesn't exist yet; it needs a Velopack publish step.
7. **Pi still on old version:** the Pi runs only the first host build. It needs `deploy\publish-pi.ps1 -Source Head` to get the modules.
8. **Leftover test row:** one smoke-test row is still in `diagnostics.error_reports` ("Smoke test: kurulum hatasi (silinebilir)") and can be deleted.
9. **Uncommitted file:** `promptlar/0003_...md` was already modified before I started. I didn't touch it and it is not in my commit.