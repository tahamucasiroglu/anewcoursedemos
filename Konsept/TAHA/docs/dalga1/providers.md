The Providers module is built and committed as `2b464e6`. All my projects, the Api host and the Web host build with 0 warnings. My test project passes 40/40 and the architecture tests pass 17/17. No real provider has been called yet, because there are no API keys. Request shapes are checked only against a fake HTTP handler, and several provider details are guesses (listed at the end).

Root for all paths below: `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/`.

**Projects**
- `src/Modules/Providers/TahaKonsept.Modules.Providers.{Domain,Application,Infrastructure,Api}`
- `tests/TahaKonsept.Modules.Providers.Tests`
- `src/BuildingBlocks/TahaKonsept.Shared.Contracts/Providers/`

**What other modules use** (namespace `TahaKonsept.Modules.Providers.Application.Abstract`)
- **`IProviderGateway`** (singleton, safe to call from background services):
  - `Task<Result<TextResult<T>>> RunStructuredAsync<T>(TextTask task, CancellationToken ct)`
  - `Task<Result<TextResult<string>>> RunTextAsync(TextTask task, CancellationToken ct)`
  - It builds the JSON schema from `T`, which must be an object type. If the reply isn't valid JSON it makes one repair call. Every call is written to CallLog with its cost.
  - `TextTask` (in `...Application.Models`) has: `UserId`, `ModelKey`, `Effort`, `SystemPrompt`, `UserPrompt`, `MaxOutputTokens` (0 means min(model limit, 16000); above 16000 it streams), `UseWebSearch`, `Purpose`, `CacheSystemPrompt` (default true).
  - `TextResult<T>` returns: `Value`, `RawText`, `Usage`, `CostUsd`, `DurationMs`, `StopReason`, `Repaired`.
  - For OpenRouter, the model key is `openrouter:<vendor/model>`.
- **`IBudgetGuard`** (scoped):
  - `GetStatusAsync(Guid userId, ct)` returns `BudgetStatus`.
  - `EnsureCanStartProductionAsync(Guid userId, ct)` returns 409 `providers.budget_exceeded` when the limit is passed and "stop on exceed" is on.
- **`IModelCatalog`**: `All`, `Find(key)`, `Query(capability, runsOn)`.
- **`IApiKeyVault`** (for the image/speech agent): `GetCredentialAsync(userId, platform, ct)` returns a `ProviderCredential`.
- Error codes are in `...Application.Common.ProviderErrorCodes`.
- DI registration: `AddProvidersModule` in the Infrastructure `DependencyInjection` folder. The Api project has `ProvidersModule`.

**For the image/speech agent**
- `IImageModel` / `ImageModelBase` and `ISpeechModel` / `SpeechModelBase` exist in Application, with no concrete classes.
- A concrete class only implements `GenerateCoreAsync` or `SynthesizeCoreAsync`. Retry and cost logging come from `AiModelBase`.
- Put concrete classes in `Infrastructure/ImageModels/Concrete` and `Infrastructure/SpeechModels/Concrete`.

**Catalog** (`Infrastructure/Catalog/catalog.json`, embedded)
- 50 entries: 9 LLMs, 6 online TTS, 11 online image, 14 local TTS, 7 local image, 3 video engines.
- Prices come from rapor.md; unknown prices are null.
- Local and video-engine entries use platform `local` / `engine`.

**Other storage and cost behaviour**
- API keys are encrypted with `ISecretProtector` and only the last 4 characters are stored in clear.
- `ModelPrice` (user price override) only overrides the catalog price; there is no API route to edit it yet.
- The month boundary for spending uses Istanbul time.

**Database**
- `ProvidersDbContext`, schema `providers`, with the `Initial` migration. I applied it to the dev database, and the host also applies it on startup.

**API routes** (`ProvidersController`)
- `GET api/providers/models?capability=&runsOn=`
- `GET|PUT api/providers/keys`, `DELETE api/providers/keys/{platform}`, `POST api/providers/keys/{platform}/test`
- `GET api/providers/usage/monthly` returns `{month, totalUsd, limitUsd, exceeded, warning, stopOnExceed, byPlatform[]}`
- `GET|PUT api/providers/budget` with `{monthlyLimitUsd, stopOnExceed, warnAtPercent}`
- Platform ids: `anthropic`, `openai`, `openrouter`, `zai`, `moonshot`, `google`, `elevenlabs`, `azure-speech` (needs a region), `cartesia`, `fal`, `bfl`, `replicate`.

**Smoke test**: the Api host on port 7191 started, the database was ready, and all provider routes answered 401 without a login, so they are mapped. I couldn't test logged-in calls because I have no account credentials. I stopped the host afterwards.

**Web**
- `AyarlarController`: added `ApiAnahtariTest/{id}` (POST).
- `Views/Ayarlar/Index.cshtml`: added a warning-threshold field, a "this month" spending box, and the new script tag.
- New `wwwroot/js/pages/settings-providers.js` (`TK.settingsProviders`): the key list comes from the server, with save, test, delete and a region field for Azure; it also handles budget and spending. It uses `TK.api` only.
- `ayarlar.js`: I removed the old key and budget code. It used the old ids like `GLM`/`Azure`.
- `layout.js` `loadSpend`: the spending badge now uses `exceeded`, `warning` and `stopOnExceed`.

**Things you should know**
- **Anthropic SDK 13.8 auth**: it sends the key as `Authorization: Bearer` with API version `2026-10-21`, not `x-api-key`. The tests check for this.
- **Anthropic web search + JSON**: when both are on, the schema goes into the system prompt instead of `output_config.format`, because citations can't be combined with the format option. The JSON is then parsed and repaired as usual.
- **Budget is not enforced inside the gateway**: Concepts/Production must call `IBudgetGuard.EnsureCanStartProductionAsync` before starting new work.
- **Shared files**: `TahaKonsept.slnx` and `Directory.Packages.props` (I added `Microsoft.Extensions.Logging.Abstractions`) had already been committed by another agent with my lines included. My commit also carries the edits to `ApiModules.cs`, `TahaKonsept.Api.csproj` and the Web Ayarlar files.

**Not verified** (no live calls were made):
- ElevenLabs model ids.
- Whether the GLM and Kimi chat endpoints accept the parameters I send (GLM `reasoning_effort` and web-search tool, the `json_object` + schema-in-prompt fallback used for both), and OpenAI's `xhigh`/`max` effort values.
- The key-test endpoints for fal, BFL and Z.ai.
- Whether Anthropic accepts the schema type arrays that .NET generates for nullable fields.

The first real key test or concept run will show whether any of these need adjusting.