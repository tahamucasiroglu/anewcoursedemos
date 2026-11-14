using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Test kasası: kaydı doğrudan verilen depodan okur (kapsam açmadan).
/// </summary>
public sealed class RepositoryVault : ApiKeyVaultBase
{
    private readonly IProviderApiKeyRepository _repository;

    /// <summary>Kurucu.</summary>
    /// <param name="repository">Depo.</param>
    /// <param name="protector">Şifreleyici.</param>
    public RepositoryVault(IProviderApiKeyRepository repository, ISecretProtector protector)
        : base(protector)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    protected override Task<ProviderApiKey?> FindAsync(Guid userId, string platform, CancellationToken ct) =>
        _repository.FirstOrDefaultAsync(k => k.UserId == userId && k.Platform == platform, ct);
}
