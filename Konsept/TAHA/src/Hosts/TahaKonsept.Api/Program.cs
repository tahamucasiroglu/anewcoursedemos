using Serilog;
using TahaKonsept.Api.Database;
using TahaKonsept.Api.Endpoints;
using TahaKonsept.Api.Modules;
using TahaKonsept.Api.Security;
using TahaKonsept.BuildingBlocks.Api.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Middleware;
using TahaKonsept.BuildingBlocks.Api.Modules.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Configuration;
using TahaKonsept.BuildingBlocks.Infrastructure.DependencyInjection;
using TahaKonsept.BuildingBlocks.Infrastructure.Logging;
using TahaKonsept.Shared.Contracts.Common;

// Taha Konsept API host'u (MIMARI §2, §4, §5). Modül listesi: Modules/ApiModules.cs.
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Geliştirmede TAHA/.env (git dışı) → TAHAKONSEPT_DB_PASSWORD vb.
    DotEnvFile.LoadFromAncestors(builder.Environment.ContentRootPath);
}

builder.Host.UseTahaSerilog("api");
builder.Host.UseSystemd();

builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddBuildingBlocksApi();

var mvc = builder.Services
    .AddControllers()
    .AddJsonOptions(options => ContractJson.Apply(options.JsonSerializerOptions));

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => ContractJson.Apply(options.PayloadSerializerOptions));

builder.Services.AddTahaJwtAuthentication();
builder.Services.AddAuthorization();

var modules = new ModuleRegistry(ApiModules.Create());
modules.RegisterServices(builder.Services, builder.Configuration, mvc);

// Migration'lar arka planda; DB yoksa Critical log + yeniden deneme, API yine de ayağa kalkar.
builder.Services.AddHostedService<ModuleMigrationHostedService>();

var app = builder.Build();

app.UseTahaExceptionHandling();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthEndpoints();
modules.MapEndpoints(app);

await app.RunAsync();

/// <summary>
/// API giriş noktası (top-level statements). <c>WebApplicationFactory&lt;Program&gt;</c> ile entegrasyon testleri için public partial.
/// </summary>
public partial class Program;
