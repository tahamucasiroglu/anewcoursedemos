using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration;

/// <summary>Gerçek sunucu (Kestrel) ayarı: yanıtlara "Server" başlığı eklenmez (review round 1, L-4; spec 0001 gereksinim 1).</summary>
[Trait("Category", "Integration")]
public sealed class KestrelHardeningTests
{
    private readonly TestDatabase database;

    public KestrelHardeningTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public void Kestrel_DoesNotAddServerHeader()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);

        KestrelServerOptions options = factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        options.AddServerHeader.ShouldBeFalse();
    }
}
