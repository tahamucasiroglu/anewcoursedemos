using LeaveManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManager.Web.Tests;

[Trait("Category", "Integration")]
public sealed class DatabaseStartupTests(LeaveManagerWebFactory factory) : IClassFixture<LeaveManagerWebFactory>
{
    // AC-6 smoke test only: the open in-memory connection always connects. The proof that startup
    // creates a missing database file is the VERIFY observation of plan 0001, amendment A-2.
    [Fact]
    public async Task Startup_ResolvesDbContext_AndCanConnect()
    {
        using var client = factory.CreateClient(); // completes application startup

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LeaveManagerDbContext>();

        Assert.True(await context.Database.CanConnectAsync(TestContext.Current.CancellationToken));
    }
}
