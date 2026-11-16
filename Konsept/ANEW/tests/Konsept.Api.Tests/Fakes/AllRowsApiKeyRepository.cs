using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Kasıtlı olarak "sızdıran" key deposu (plan 0013, AC-3): listede kullanıcı süzgeci uygulamaz, tüm kullanıcıların satırlarını
/// döndürür. Kapının kendi sahiplik süzgecini (savunma derinliği) kanıtlamak için kullanılır.
/// </summary>
/// <param name="inner">Asıl depo.</param>
public sealed class AllRowsApiKeyRepository(InMemoryApiKeyRepository inner) : IApiKeyRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<UserApiKey>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(inner.Rows);

    /// <inheritdoc />
    public Task UpsertAsync(UserApiKey key, CancellationToken cancellationToken) => inner.UpsertAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid userId, string providerId, CancellationToken cancellationToken) =>
        inner.DeleteAsync(userId, providerId, cancellationToken);
}
