using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using LeaveManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeaveManager.Web.Tests;

/// <summary>
/// Hosts the app in the "Testing" environment on one open in-memory SQLite connection
/// (docs/conventions.md, Known traps 1): the database lives exactly as long as the factory.
/// </summary>
public class LeaveManagerWebFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected virtual string EnvironmentName => "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<LeaveManagerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LeaveManagerDbContext>>();
            services.AddDbContext<LeaveManagerDbContext>(options => options.UseSqlite(_connection));

            // Test-only endpoint that throws (spec 0001, Constraints: never in production code).
            services.AddControllersWithViews()
                .AddApplicationPart(typeof(ThrowingTestController).Assembly);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

/// <summary>Production environment; HTTPS base address so HTTPS redirection stays on (plan 0001, risk 4).</summary>
public sealed class ProductionWebFactory : LeaveManagerWebFactory
{
    public ProductionWebFactory()
    {
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    protected override string EnvironmentName => "Production";
}

internal static class Html
{
    public static async Task<IDocument> ParseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return await new HtmlParser().ParseDocumentAsync(content, TestContext.Current.CancellationToken);
    }
}
