using Konsept.Modules.AgentGateway.Infrastructure;
using Konsept.Modules.AgentGateway.Presentation.Controllers;
using Konsept.Modules.AiGateway.Infrastructure;
using Konsept.Modules.AiGateway.Presentation.Controllers;
using Konsept.Modules.Concepts.Infrastructure;
using Konsept.Modules.Concepts.Presentation.Controllers;
using Konsept.Modules.Identity.Infrastructure;
using Konsept.Modules.Identity.Presentation.Controllers;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Infrastructure;
using Konsept.Modules.Monitoring.Infrastructure.Logging;
using Konsept.Modules.Monitoring.Presentation.Controllers;
using Konsept.Modules.Production.Infrastructure;
using Konsept.Modules.Production.Presentation.Controllers;
using Konsept.Modules.Profiles.Infrastructure;
using Konsept.Modules.Profiles.Presentation.Controllers;
using Konsept.Shared.Infrastructure.Http;
using Konsept.Shared.Infrastructure.Logging;
using Konsept.Shared.Infrastructure.Versioning.Concrete;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.AddKonseptSerilog(LogSources.Api, "api", (logger, services) => logger.WriteToTechnicalLogDatabase(services, LogEntrySource.Api));

var connectionString = builder.Configuration.GetConnectionString("Konsept")
    ?? throw new InvalidOperationException("ConnectionStrings:Konsept ayarı eksik.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProductVersionProvider>(new AssemblyProductVersionProvider(typeof(Program).Assembly));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    if (context.ProblemDetails.Status is null or >= 500 && context.Exception is not null)
    {
        context.ProblemDetails.Title = "Beklenmeyen bir hata oluştu.";
        context.ProblemDetails.Detail = null;
    }
});
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationActionFilter>())
    .AddApplicationPart(typeof(SystemController).Assembly)
    .AddApplicationPart(typeof(AgentController).Assembly)
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(ApiKeysController).Assembly)
    .AddApplicationPart(typeof(ProfilesController).Assembly)
    .AddApplicationPart(typeof(ConceptsController).Assembly)
    .AddApplicationPart(typeof(ProductionController).Assembly);
builder.Services.AddScoped<ServiceKeyAuthorizationFilter>();
builder.Services.AddMonitoringModule(connectionString);
builder.Services.AddAgentGatewayModule();
builder.Services.AddIdentityModule(connectionString, builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddAiGatewayModule(connectionString, builder.Configuration);
builder.Services.AddProfilesModule(connectionString);
builder.Services.AddConceptsModule(connectionString);
builder.Services.AddProductionModule(connectionString);

// Default-deny (spec 0002 Req 11): açıkça [AllowAnonymous] olmayan her uç geçerli oturumlu JWT ister; iç uçlar servis
// anahtarı ister.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
    options.AddPolicy(AuthPolicies.InternalService, policy => policy.AddAuthenticationSchemes(AuthPolicies.ServiceKeyScheme).RequireAuthenticatedUser());

    // Yetki tabanlı politika (spec 0011): yetki claim'i token'dan değil, her istekte veritabanındaki rolden gelir
    // (SessionValidatingJwtEvents); kimliksiz istek 401, yetkisiz 403.
    options.AddPolicy(AuthPolicies.AdminPanel, policy => policy
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireClaim(AuthPolicies.PermissionClaim, AuthPolicies.AdminPanel));

    // Ajan hub'ı (spec 0012 Req 10): yalnızca eşleşmiş, iptal edilmemiş ajanın oturum belirteci; JWT kabul edilmez.
    options.AddPolicy(AuthPolicies.AgentHub, policy => policy
        .AddAuthenticationSchemes(AuthPolicies.AgentScheme)
        .RequireAuthenticatedUser()
        .RequireClaim(AuthPolicies.AgentDeviceClaim));
});

var app = builder.Build();

app.UseKonseptLogging();
app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapAgentGateway();
app.Services.WarnIfEphemeralSigningKey();

if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
{
    await app.Services.MigrateMonitoringDatabaseAsync(CancellationToken.None);
    await app.Services.MigrateIdentityDatabaseAsync(CancellationToken.None);
    await app.Services.MigrateAiGatewayDatabaseAsync(CancellationToken.None);
    await app.Services.MigrateProfilesDatabaseAsync(CancellationToken.None);
    await app.Services.MigrateConceptsDatabaseAsync(CancellationToken.None);
    await app.Services.MigrateProductionDatabaseAsync(CancellationToken.None);
}

await app.RunAsync();
