using LeaveManager.Application;
using LeaveManager.Application.Health;
using LeaveManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Unexpected errors become ProblemDetails without stack trace or internals (docs/security.md, Input & output).
app.UseExceptionHandler();
app.UseStatusCodePages();

// Health is the second anonymous exception to "default deny" besides login (docs/security.md; spec 0001, R-2).
// The fallback authorization policy arrives with the login feature (plan 0001, R-6).
app.MapGet("/api/health", (GetSystemHealth useCase) => TypedResults.Ok(useCase.Execute()))
    .AllowAnonymous()
    .WithName("GetSystemHealth");

app.Run();
