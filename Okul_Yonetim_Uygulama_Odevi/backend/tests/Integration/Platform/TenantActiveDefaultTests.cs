using Microsoft.EntityFrameworkCore;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Platform;

/// <summary>
/// Spec 0007 (VERIFY V-3): göç, var olan okulları etkin bırakır — `tenants.is_active` sütununun veritabanı varsayılanı
/// true. Yanlışsa yayından sonra bütün okullar pasif görünür ve kimse giremez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TenantActiveDefaultTests
{
    private readonly TestDatabase database;

    public TenantActiveDefaultTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task IsActiveColumn_DefaultsToTrue_ForExistingRows()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        string? columnDefault = await QueryAsync(factory, c => c.Database
            .SqlQueryRaw<string>("SELECT column_default AS \"Value\" FROM information_schema.columns WHERE table_name = 'tenants' AND column_name = 'is_active'")
            .SingleAsync());

        columnDefault.ShouldBe("true");
    }
}
