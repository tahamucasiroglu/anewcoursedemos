using LeaveManager.Infrastructure;
using LeaveManager.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

// First in the pipeline so every response — pages, static files, errors — carries the headers.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/NotFound");
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}");

await app.Services.InitializeDatabaseAsync();
await app.RunAsync();

/// <summary>Entry point; public so WebApplicationFactory can reference it.</summary>
public partial class Program
{
}
