The Identity module is built, registered in the API host and committed as `369cffe`. All 22 of its tests pass, the 17 architecture tests pass, and every project I built has 0 warnings and 0 errors.

**Paths** (all under `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/`):
- Module: `src/Modules/Identity/` (Domain, Application, Infrastructure, Api)
- Tests: `tests/TahaKonsept.Modules.Identity.Tests/`

**API routes** (match what the Web already calls; no Web route strings changed):

| Route | Access | Returns |
|---|---|---|
| `GET api/identity/setup/status` | anonymous | `SetupStatusResponse` |
| `POST api/identity/setup` | anonymous | `TokenResponse`; 409 once any user exists |
| `POST api/identity/login` | anonymous | `TokenResponse` |
| `POST api/identity/refresh` | anonymous | `TokenResponse` (rotating) |
| `POST api/identity/logout-all` | logged in | 204 |
| `GET api/identity/me` | logged in | `CurrentUserResponse` |
| `GET` / `PUT api/identity/me/preferences` | logged in | `UserPreferencesDto` |

- **Errors:** Turkish ProblemDetails with codes like `identity.invalid_credentials`, `identity.user_banned`, `identity.refresh_token_reuse`.
- **Access token:** a 15-minute JWT with claims `sub`, `name`, `jti`, `role` (`Admin`), `permission` (array), `tfam` (token family) and `client` (`web` or `agent`).
- **Refresh tokens:** stored only as a SHA-256 hash, with a `FamilyId` and a `Kind` (Web or Agent).

**Public interfaces for other modules** (namespace `TahaKonsept.Modules.Identity.Application.Abstract`):
- `ITokenIssuer`: `Task<Result<IssuedTokens>> IssueAsync(Guid userId, TokenClientKind kind, CancellationToken ct = default)`. `IssuedTokens(TokenResponse Tokens, Guid FamilyId)` is in `...Application.Models`. Agents should keep `FamilyId` so it can revoke that agent later.
- `ITokenRevoker`: `RevokeFamilyAsync(Guid userId, Guid familyId, ct)` and `RevokeAllAsync(Guid userId, ct)`, both returning `Result<int>`.
- `ISessionRevocationNotifier`: `OnAllSessionsRevokedAsync(Guid userId, ct)` and `OnTokenFamilyRevokedAsync(Guid userId, Guid familyId, ct)`.
  - Identity calls every registered notifier; one that throws is logged and skipped.
  - The default is a no-op.
  - Agents can derive from `...Application.Base.SessionRevocationNotifierBase` and register it with `services.AddScoped<ISessionRevocationNotifier, X>()`. Module order doesn't matter.
- `IUserDirectory`: `ListAsync(ct)` returns `IReadOnlyList<UserSummaryDto>`, `CountAsync(ct)` returns `int`, `SetBannedAsync(Guid userId, bool banned, ct)` returns `Result`. Banning revokes all of that user's tokens, and the last active admin cannot be banned.

**Shared.Contracts** (`TahaKonsept.Shared.Contracts.Identity`): I added `LoginRequest`, `RefreshRequest`, `SetupRequest`, `SetupStatusResponse`, `UserPreferencesDto`, `ThemePreference` (System/Light/Dark), `CurrentUserResponse`, `UserSummaryDto` and `TokenClientKind` (Web/Agent). There are also two constant classes: `PermissionNames` (`models.use`, `publish.use`, `admin.access`, `All`) and `RoleNames.Admin`.

**Authorization policies:** each permission is registered as a policy with the same name, so other modules can write `[Authorize(Policy = PermissionNames.AdminAccess)]`. The policy passes on the matching `permission` claim or the Admin role.

**Database:**
- Context: `IdentityModuleDbContext`, schema `identity`, snake_case.
- Tables: `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `role_claims`, `refresh_tokens`.
- The `Initial` migration was created and I applied it to the dev database on port 5433.
- The dev database still reports `setupRequired: true`. I deliberately did not call setup there, because it can only be done once.

**Web:** I deleted the four temporary DTOs in `Models/Api/` and switched `AccountService.cs`, `AccountServiceBase.cs` and `ApiTokenRefresher.cs` to the Shared.Contracts types. The first two needed short name aliases because the Web's own `Models.LoginRequest` and `Models.SetupRequest` now share names with the new contracts. These three files were not on my owned list; editing them was the only way to remove the old DTOs. I did not touch `AccountController`, `KurulumController`, `_Layout.cshtml` or any JS. Theme saving (`PUT /Ayarlar/Tema`) works as before, since `{theme: "light|dark|system"}` maps onto `UserPreferencesDto`.

**Other shared files edited:** `TahaKonsept.slnx`, `Directory.Packages.props` (added `Microsoft.Extensions.Identity.Core` and `.Stores` 10.0.15), `ApiModules.cs` (`new IdentityModule()` is first) and `TahaKonsept.Api.csproj`.

**Testing:**
- **Tests (22):** cover setup only once, login, refresh rotation and reuse detection, logout-all revoking agent tokens with the notifier called, banned user blocked, and preferences/validators.
- **Smoke test:** I ran the API host on port 7191; status, bad login (401), empty login (400), bad refresh (401), and anonymous `me` / `logout-all` (401) all behaved correctly. I then stopped the host.

**Things the integrator should know:**
- **Others' entries in my commit:** the shared files I committed already contained other agents' lines (Diagnostics projects in the solution, `ApiModules.cs` and the API csproj), so my commit includes those lines.
- **Logout and ban lag:** access tokens stay valid for up to 15 minutes, because there is no per-request check against the database. Cutting live agent connections immediately depends on Agents implementing the notifier.
- **Not wired yet:**
  - Agents must call `ITokenIssuer` with `TokenClientKind.Agent`, implement `ISessionRevocationNotifier`, and call `RevokeFamilyAsync` when an agent is removed.
  - Diagnostics/admin needs to call `IUserDirectory`.
- **Reuse detection is strict:** there is no grace window, so the Web's existing single-flight refresh cache is what prevents accidental family revocation.