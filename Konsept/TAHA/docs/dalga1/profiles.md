The Profiles module, its API and the profile modal are built and committed in `f9b11ca`. All 19 of the module's tests pass, and so do all 17 architecture tests. The Profiles projects and the Api host build with 0 warnings. I applied the migration to the dev database (localhost:5433). In a short run on port 7191 the API applied the migration and answered `GET api/profiles/templates` with 401. That only shows the route exists: no logged-in request was made, because Identity is not registered yet. I stopped the host afterwards.

**Web build is failing, not from my files.** `TahaKonsept.Web.csproj` currently fails with 6 errors. They are all in `AccountServiceBase.cs` / `AccountService.cs`: `LoginRequest` and `SetupRequest` are ambiguous between `Web.Models` and `Shared.Contracts.Identity`. That is the Identity work in progress. My Web files raised no errors, but I could not confirm a clean Web build.

**Interfaces other modules will use** (Application project `TahaKonsept.Modules.Profiles.Application`, folder `Abstract/`):
```csharp
public interface IProfileReader {   // scoped; no logged-in user needed, works in background jobs
    Task<ProfileDto?> GetActiveAsync(Guid userId, CancellationToken ct = default);
    Task<ProfileDto?> GetAsync(Guid id, CancellationToken ct = default);
}
public interface IProfilePromptBuilder { string Build(ProfileDto profile); }   // singleton
```
- **Prompt output:** short Turkish lines. Example: `Kanal: …`, `Dil: Türkçe (tr); tüm metin ve seslendirme bu dilde.`, `Biçim: dikey 9:16; hedef ~60 sn.`, `Odak: …`, `Ton: … | Kitle: … | Anlatım: …`. Empty fields are left out, whitespace is collapsed, and the channel description is cut at a word boundary after 1200 characters.
- **Other services:** `IProfileService` (used by the API), `IProfileTemplateCatalog` (`GetAll()`, `Find(key)`), `IProfileRepository`. Each follows the interface → `Base/` → `Concrete/` pattern.

**Shared contracts** (`TahaKonsept.Shared.Contracts.Profiles`):
- `ProfileDto`: Id, Name, Language, Format, TargetDurationSec, ChannelDescription, DefaultTone, DefaultAudience, DefaultNarrativeStyle, TemplateKey, IsActive, CreatedAt, UpdatedAt.
- `ProfileTemplateDto`: Key, Category, Title, Name, Language, Format, TargetDurationSec, ChannelDescription, and the three default style fields.
- `SaveProfileRequest`: used for both create and update.
- `Format` reuses the existing `Shared.Contracts.Concepts.VideoFormat` (Vertical/Horizontal).

**Behaviour rules:**
- Each user has at most one active profile.
- A newly created profile becomes active.
- Deleting the active profile makes the newest remaining profile active.
- A profile belonging to another user returns 404.
- Input is checked by `SaveProfileRequestValidator`, with Turkish messages. Name is required (max 120), language must look like `tr`/`en`/`de`, duration is 15–1800 seconds, description max 4000.

**API routes** (`ProfilesController`, `[Route("api/profiles")]`):

| Route | Returns |
|---|---|
| `GET api/profiles` | 200, list of `ProfileDto` |
| `GET api/profiles/templates` | 200, list of `ProfileTemplateDto` |
| `GET api/profiles/{id}` | 200, `ProfileDto` |
| `POST api/profiles` | 201, `ProfileDto` |
| `PUT api/profiles/{id}` | 200, `ProfileDto` |
| `POST api/profiles/{id}/activate` | 200, `ProfileDto` |
| `DELETE api/profiles/{id}` | 204 |

**Templates:** the 8 required templates are in `Templates/profile-templates.json`, embedded in the Application assembly (not stored per user). Each has a sample channel name, language, format, duration, a Turkish channel description and suggested tone/audience/style.

**Database:** `ProfilesDbContext` uses schema `profiles` with one table, `profiles.profiles`. The migration `20261114024954_Initial` is in `Infrastructure/Persistence/Migrations`.

**Web changes:**
- `Controllers/ProfilController.cs`: added `Sil` (DELETE → `api/profiles/{id}`) and updated the doc comments. The body field is now `channelDescription`, not `content`.
- `wwwroot/js/core/profile.js`: rewritten. I kept it in `js/core/` because `_Layout` already loads it from there and `layout.js` depends on `TK.profile`, so I didn't touch `_Layout`.
  - ➕ opens a create modal with a template picker grouped by category. Picking a template fills all fields, and replaces the name only if it was empty or still the previous template's name.
  - The modal has a "Gelişmiş" section for tone, audience and narrative style.
  - ✏️ opens the edit modal, which has a "Sil" button with a confirmation.
  - The modal saves when closed by clicking outside, ✕, Esc or Kaydet. Calls go through `TK.api` only. Saving or deleting fires `tk:profile-saved`.
- The dropdown itself lives in `core/layout.js`, which I didn't own and left unchanged. It already loads the list, activates the chosen profile and fires `tk:profile` so pages reload their data.

**Shared files edited:**
- `TahaKonsept.slnx`: added 4 module projects and the test project.
- `src/Hosts/TahaKonsept.Api/Modules/ApiModules.cs`: `new ProfilesModule()` plus its using.
- `TahaKonsept.Api.csproj`: ProjectReference to `TahaKonsept.Modules.Profiles.Api`.

**Tests:** `tests/TahaKonsept.Modules.Profiles.Tests` uses EF InMemory and no real database. It covers:
- **Service:** only one active profile per user after create, activate and delete; users don't affect each other; another user's profile returns 404; update normalises the fields.
- **Prompt builder:** exact output text, empty fields left out, long descriptions shortened, durations written in minutes and seconds.
- **Templates and validation:** the 8 templates load with unique keys and each one passes the validator; a separate test checks that bad input is rejected.

**For the integrator:**
- Concepts and Production should inject `IProfileReader` and `IProfilePromptBuilder`. That needs a reference to `TahaKonsept.Modules.Profiles.Application`.
- There is no unique index in the database enforcing one active profile per user; the service enforces it. Adding a Postgres partial unique index would need a two-step save, because swapping the active flag in one save can trip it.

Main code is under `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/src/Modules/Profiles/`.