using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence;

/// <summary>AiGateway modülünün veritabanı bağlamı (<c>ai_gateway</c> şeması): kullanıcıların şifreli API key'leri ve harcama kayıtları.</summary>
public sealed class AiGatewayDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "ai_gateway";

    /// <summary>Initializes a new instance of the <see cref="AiGatewayDbContext"/> class.</summary>
    /// <param name="options">EF Core seçenekleri.</param>
    public AiGatewayDbContext(DbContextOptions<AiGatewayDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets API key satırları.</summary>
    public DbSet<UserApiKey> ApiKeys => Set<UserApiKey>();

    /// <summary>Gets harcama kayıtları (spec 0013).</summary>
    public DbSet<SpendRecord> SpendRecords => Set<SpendRecord>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;
}
