using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence.Concrete;

/// <summary>
/// API key satırlarının EF Core + PostgreSQL uygulaması. Her sorgu <c>user_id</c> ile süzülür (BR-1). Yerine koyma tek ifadelik
/// <c>INSERT … ON CONFLICT</c>'tir: eşzamanlı iki kayıt da tek satır bırakır (spec 0004 AC-7, AC-14).
/// </summary>
public sealed class ApiKeyRepository : EfRepositoryBase<AiGatewayDbContext>, IApiKeyRepository
{
    /// <summary>Initializes a new instance of the <see cref="ApiKeyRepository"/> class.</summary>
    /// <param name="context">AiGateway veritabanı bağlamı.</param>
    public ApiKeyRepository(AiGatewayDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApiKey>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await Context.ApiKeys.AsNoTracking().Where(k => k.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task UpsertAsync(UserApiKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        var validation = key.Validation == KeyValidation.Verified ? "verified" : "unverified";
        var updatedAt = key.UpdatedAt.ToUniversalTime();
        return Context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO ai_gateway.api_keys (id, user_id, provider_id, ciphertext, hint, validation, updated_at)
            VALUES ({key.Id}, {key.UserId}, {key.ProviderId}, {key.Ciphertext}, {key.Hint}, {validation}, {updatedAt})
            ON CONFLICT (user_id, provider_id) DO UPDATE SET
                ciphertext = EXCLUDED.ciphertext, hint = EXCLUDED.hint, validation = EXCLUDED.validation, updated_at = EXCLUDED.updated_at
            """,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid userId, string providerId, CancellationToken cancellationToken) =>
        await Context.ApiKeys.Where(k => k.UserId == userId && k.ProviderId == providerId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;
}
