using Konsept.Modules.AgentGateway.Infrastructure;
using Konsept.Modules.AgentGateway.Presentation.Controllers;
using Konsept.Modules.Monitoring.Infrastructure;
using Konsept.Modules.Monitoring.Presentation.Controllers;
using Konsept.Shared.Infrastructure.Http;
using Konsept.Shared.Infrastructure.Logging;
using Konsept.Shared.Infrastructure.Versioning.Concrete;
using Konsept.Shared.Kernel.Abstract;

var builder = WebApplication.CreateBuilder(args);
builder.AddKonseptSerilog("api");

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
    .AddApplicationPart(typeof(AgentController).Assembly);
builder.Services.AddMonitoringModule(connectionString);
builder.Services.AddAgentGatewayModule();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();
app.MapAgentGateway();

if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
{
    await app.Services.MigrateMonitoringDatabaseAsync(CancellationToken.None);
}

await app.RunAsync();
