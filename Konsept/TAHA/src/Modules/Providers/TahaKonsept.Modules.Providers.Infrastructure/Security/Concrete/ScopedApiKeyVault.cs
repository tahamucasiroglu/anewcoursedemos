using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Infrastructure.Security.Concrete;

/// <summary>
/// <see cref="ApiKeyVaultBase"/>'in tekil uygulaması: kaydı kendi DI kapsamında (ayrı DbContext) okur; paralel çağrılara güvenli.
/// </summary>
public sealed class ScopedApiKeyVault : ApiKeyVaultBase
{
    private readonly IServiceScopeFactory _scopes;

    /// <summary>Kurucu.</summary>
    /// <param name="scopes">Kapsam fabrikası.</param>
    /// <param name="protector">Şifreleyici.</param>
    public ScopedApiKeyVault(IServiceScopeFactory scopes, ISecretProtector protector)
        : base(protector)
    {
        _scopes = scopes;
    }

    /// <inheritdoc />
    protected override async Task<ProviderApiKey?> FindAsync(Guid userId, string platform, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProviderApiKeyRepository>();
        return await repository.FirstOrDefaultAsync(k => k.UserId == userId && k.Platform == platform, ct);
    }
}
