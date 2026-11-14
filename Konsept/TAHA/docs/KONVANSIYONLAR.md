# Konvansiyonlar (F0 sonrası gerçek kod durumu)

> F0 doğrulama ajanının çıkardığı kopya kağıdı. Modül yazarken MIMARI.md ile birlikte okunur.

**Conventions cheat-sheet for the module agents**

*General*
- One type per file. File name equals type name (`Name{T}.cs` for generics). No nested types.
- Every type and public member needs a `/// <summary>`, including private and internal types.
- Folders `Abstract/` (interfaces), `Base/` (abstract classes), `Concrete/` (implementations). Entities go in `Domain/Entities/`, enums in `Domain/Enums/`.
- Namespaces follow folders.
- Packages: versions only in `Directory.Packages.props`; the csproj has `<PackageReference Include="X" />` with no version.
- Tests: xunit.v3 with an explicit `using Xunit;`, plus Shouldly and NSubstitute. Test csproj needs `<OutputType>Exe</OutputType>` and `<IsTestProject>true</IsTestProject>`.

*Domain* (`TahaKonsept.BuildingBlocks.Domain.*`)
- `AggregateRoot : EntityBase<Guid>, IAggregateRoot`, with `protected AggregateRoot()` (the id is `Guid.CreateVersion7()`) and `protected AggregateRoot(Guid id)`.
- `IAuditable` (`CreatedAtUtc`/`UpdatedAtUtc`) is filled in on save. Implement `IHasUser` (`UserId`) on every entity except Identity's.
- `Result`:
  - `Result.Success()`, `Result.Failure(error)`, `Result.Success<T>(v)`.
  - `Result<T>` converts implicitly from `T` and from `Error`, and has `.Map`.
- `Error(Code, Message, Type)` has factories `Error.Validation(code, msg, details?)`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Failure`. Services return `Result` and do not throw.

*Application* (`TahaKonsept.BuildingBlocks.Application.*`)
- `Abstract.ICurrentUser`: `UserId`, `UserName`, `IsAuthenticated`, `Permissions`, `HasPermission(p)`, `GetRequiredUserId()`.
- Other services in `Abstract`: `IClock.UtcNow`, `IUnitOfWork`, `ISecretProtector`, `IAppDataLocation`, `IJwtSigningKeyProvider`, `IApplicationService` (marker), `IDatabaseStatus`.
- `Abstract.IRepository<TEntity>` (where `TEntity : class, IEntity<Guid>, IAggregateRoot`):
  - Query: `Query()`, `QueryNoTracking()`, `GetByIdAsync(id, ct)`, `FirstOrDefaultAsync(pred, ct)`, `ListAsync(pred?, ct)`, `AnyAsync`, `CountAsync`, `GetPagedAsync(...)`.
  - Write: `AddAsync`, `Update`, `Remove`.
  - `UnitOfWork.SaveChangesAsync(ct)` commits.
- `Paging.PagedRequest`, `Paging.PagedResult<T>`; `Security.CustomClaimTypes` (`sub`, `name`, `permission`); `Security.JwtOptions`.
- A background service that needs the database must call `await databaseStatus.WaitUntilReadyAsync(ct)` first.

*Infrastructure* (`TahaKonsept.BuildingBlocks.Infrastructure.*`)
- `Persistence.Base.ModuleDbContextBase(DbContextOptions options, IClock clock)`: implement `public override string Schema => "concepts";`. It is also the `IUnitOfWork`, and it picks up `IEntityTypeConfiguration` classes from its own assembly automatically.
- `Persistence.Base.Repository<TEntity>(ModuleDbContextBase context)` is abstract. Write `ConceptRepository : Repository<Concept>, IConceptRepository`.
- `Persistence.Base.ModuleDesignTimeDbContextFactoryBase<TContext>`: implement `protected override TContext CreateContext(DbContextOptions<TContext> options, IClock clock)`. Name the class `<Module>DesignTimeDbContextFactory`.
- Register the context with `services.AddModuleDbContext<TContext>(configuration, "schema")`. This gives snake_case naming and keeps the migrations history in the module's own schema.
- Migrations go in `Infrastructure/Persistence/Migrations`, created with `dotnet ef migrations add Initial -p <Infra csproj> -s <Infra csproj> -o Persistence/Migrations`. Every DbContext needs at least one migration.
- Identity uses `IdentityDbContext`, so it can't derive from `ModuleDbContextBase`. It must set `HasDefaultSchema("identity")` and the same options itself.
- Other helpers:
  - `Security.Concrete.AesGcmSecretProtector` and `Time.Concrete.SystemClock`.
  - `Http.Base.ApiHttpClientBase` (desktop agent): override `PrepareRequestAsync` and `TryRecoverUnauthorizedAsync`.
  - `Logging.SerilogSetup.UseTahaSerilog("app")` and `AddTahaSerilog`.

*API layer* (`TahaKonsept.BuildingBlocks.Api.*`)
- `Controllers.Base.ApiControllerBase : ControllerBase` has `[Authorize]` by default. Add `[AllowAnonymous]` for login, setup and device start/token.
  - Helpers: `ToActionResult(Result)`, `ToActionResult<T>(Result<T>)`, `ToCreatedResult<T>`, `ToProblem(Error)`.
  - Actions are a single expression, e.g. `[HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await _service.GetAsync(id, ct));`
  - Use attribute routes such as `[Route("api/concepts")]`.
- Validation: write `XRequestValidator : AbstractValidator<XRequest>` and register it in the module's `RegisterServices` (e.g. `services.AddScoped<IValidator<XRequest>, XRequestValidator>()`). The global `ValidationFilter` runs it.

*Registering a module*
1. Projects go under `src/Modules/<M>/TahaKonsept.Modules.<M>.{Domain,Application,Infrastructure,Api}`. Add each to `TahaKonsept.slnx` under `/src/Modules/`.
2. References run Domain ← Application ← Infrastructure ← Api. Another module may only be used through its Application project's `Abstract` interfaces or through `Shared.Contracts`; the architecture tests read the csproj files to check this.
3. In the Api project, write `<M>Module : ModuleBase` (`Modules.Base`):
   - `Name => "Concepts"`.
   - `RegisterServices(services, config)` adds the DbContext, repositories, services and validators.
   - `public override Task MigrateAsync(IServiceProvider sp, CancellationToken ct) => MigrateDbContextAsync<ConceptsDbContext>(sp, ct);`
   - Agents only: `MapEndpoints(e) => e.MapHub<AgentHub>("/hubs/agent")`.
4. In `src/Hosts/TahaKonsept.Api/Modules/ApiModules.cs`, uncomment or add `new <M>Module()`; list order is migration order. Add a ProjectReference to the module's Api project in `TahaKonsept.Api.csproj`. Controllers are found automatically through the module's assembly.

*Shared.Contracts* (`TahaKonsept.Shared.Contracts.*`)
- Folders: `Production`, `Concepts`, `Agent`, `Agent.Pairing`, `Identity` (`TokenResponse`), `Common` (`HealthResponse`, `ContractJson`, `ProblemExtensionKeys`), `Diagnostics`.
- Enums are sent as strings and JSON is camelCase.
- Identity should add `LoginRequest`, `RefreshRequest`, `SetupRequest` and `SetupStatusResponse` here, then replace Web's temporary copies in `src/Hosts/TahaKonsept.Web/Models/Api/` (`IdentityLoginRequest {userName,password}`, `IdentityRefreshRequest {refreshToken}`, `IdentitySetupRequest {userName,password}`, `SetupStatusResponse {setupRequired}`).

*Adding a page and an Ajax endpoint in Web* (`src/Hosts/TahaKonsept.Web`)
- **Controller:** `Controllers/XController.cs : WebControllerBase` (`TahaKonsept.Web.Controllers.Base`). Login is required by default.
  - Page action: `[HttpGet] public IActionResult Index() => View();`
  - Ajax action, one line: `[HttpPost] public Task<IActionResult> Kaydet([FromBody] JsonElement body, CancellationToken ct) => ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/x", body, ct));`
  - Use typed DTOs from Shared.Contracts in place of `JsonElement` where they exist.
  - Helpers: `ApiUrl("api/x", ("status", s))` builds a query string; `Seg(id)` escapes a path segment. For a PUT on the same name, use a second method with `[ActionName("Ayarlar")]`.
- **View:** `Views/X/Index.cshtml` uses `_Layout`. Page scripts go in `@section Scripts { <script src="~/js/pages/x.js" asp-append-version="true"></script> }`. The nav link goes in `Views/Shared/_Layout.cshtml` with `asp-controller`.
- **JavaScript:** one IIFE per file, `(function (window, $) { var TK = window.TK = window.TK || {}; ... TK.x = {...}; })(window, jQuery);`
  - Make calls through `TK.api.get(url, query, opts)`, `post(url, body)`, `put`, `del(url)`. These return a jqXHR promise and add the antiforgery header `X-CSRF-TOKEN` automatically. Errors show as a toast unless `opts.silent`; a 401 redirects to login.
  - Live status uses `TK.poll(url, ms)`. Modals use `TK.modal.open({title, body, onClose})`.
  - `fetch` is banned, and a test scans for it.
- **Admin pages:** `Areas/Admin`, policy `AdminAccess` (`permission=admin.access` claim or the Admin role).

*How Web calls the API*
- Browser → MVC goes through `$.ajax` with the cookie.
- MVC → API goes through `IApiClient`: `GetAsync<T>`, `PostAsync<TReq,TRes>`, `PutAsync<TReq,TRes>`, `DeleteAsync<T>`, all returning `ApiCallResult<T>`.
  - It adds the bearer token from the encrypted cookie and refreshes it via `api/identity/refresh` before expiry or on a 401, then retries once.
  - A network failure becomes a 503.
  - API error bodies (ProblemDetails) are passed to the browser unchanged.
- Base URL is `Api:BaseUrl` (`http://127.0.0.1:7111`).
- The routes Web already expects are listed in the summary comment of each Web controller (e.g. `api/concepts/{id}/approve`, `api/production/queue/{start|pause|resume}`). Module APIs should implement exactly those routes, or update the strings in the Web controller.

*Database and environment*
- Port 5432 on the Pi (`appsettings.json`) and 5433 in Development through the tunnel.
- The password comes from `TAHAKONSEPT_DB_PASSWORD`; in Development, `TAHA/.env` is loaded automatically. `TAHAKONSEPT_DB` overrides the whole connection string.
- JWT uses `MapInboundClaims=false`, and SignalR reads the token from `?access_token`.

**Changed or added files** (under `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/`)
- `src/BuildingBlocks/TahaKonsept.BuildingBlocks.Application/Abstract/IDatabaseStatus.cs` (new)
- `src/BuildingBlocks/TahaKonsept.BuildingBlocks.Infrastructure/Persistence/Concrete/DatabaseStatus.cs` (new)
- `src/BuildingBlocks/TahaKonsept.BuildingBlocks.Infrastructure/Persistence/DatabaseConnectionString.cs` (added `Describe`)
- `src/BuildingBlocks/TahaKonsept.BuildingBlocks.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`
- `src/BuildingBlocks/TahaKonsept.Shared.Contracts/Common/HealthResponse.cs`
- `src/Hosts/TahaKonsept.Api/Database/ModuleMigrationHostedService.cs` (new)
- `src/Hosts/TahaKonsept.Api/Program.cs`
- `src/Hosts/TahaKonsept.Api/Endpoints/HealthEndpoints.cs`
- `src/Hosts/TahaKonsept.Api/Modules/ModuleMigrationExtensions.cs` (deleted)
- `tests/TahaKonsept.IntegrationTests/` (new: csproj, `Support/ApiFactory.cs`, `Support/ScriptedModule.cs`, `Api/HealthEndpointTests.cs`, `Api/ModuleMigrationHostedServiceTests.cs`)
- `TahaKonsept.slnx` (added the IntegrationTests project)

Nothing was committed.