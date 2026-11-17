using System.Data.Common;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>
/// Kullanıcıların EF Core + PostgreSQL uygulaması. Sayaç işlemleri oku-değiştir-yaz değil tek ifadelik atomik SQL'dir
/// (plan Risk 3): satır kilidi eşzamanlı denemeleri sıralar, böylece hiçbir deneme kaybolmaz ve 3'ten fazla hak verilmez
/// (spec 0002 AC-6).
/// </summary>
public sealed class UserRepository : EfRepositoryBase<IdentityModuleDbContext>, IUserRepository
{
    private const string UniqueViolation = "23505";

    private const string FailedAttemptSql =
        "UPDATE identity.users SET " +
        "failed_login_count = CASE WHEN failed_login_count + 1 >= @max THEN 0 ELSE failed_login_count + 1 END, " +
        "locked_until = CASE WHEN failed_login_count + 1 >= @max THEN @lock_until ELSE locked_until END " +
        "WHERE id = @id AND (locked_until IS NULL OR locked_until <= @now) " +
        "RETURNING failed_login_count, locked_until";

    // Açılış rol senkronu (spec 0011, plan Risk 2) tek ifade: eşleşen hesap admin, diğer her hesap kullanıcı; parametre NULL
    // ise (ayar boş) eşitlik hiçbir satırda doğru olmaz, herkes kullanıcıdır. Yalnızca rolü değişen satırlar yazılır.
    private const string SyncAdminRoleSql =
        "UPDATE identity.users SET role = CASE WHEN normalized_email = @admin_email THEN 'admin' ELSE 'user' END " +
        "WHERE role <> CASE WHEN normalized_email = @admin_email THEN 'admin' ELSE 'user' END";

    /// <summary>Initializes a new instance of the <see cref="UserRepository"/> class.</summary>
    /// <param name="context">Identity veritabanı bağlamı.</param>
    public UserRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    /// <inheritdoc />
    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var entry = Context.Users.Add(user);
        try
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            entry.State = EntityState.Detached;
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<LockoutOutcome> RegisterFailedAttemptAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = Context.Database.GetDbConnection();
        await Context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = FailedAttemptSql;
                command.Parameters.Add(new NpgsqlParameter("max", LockoutPolicy.MaxFailedAttempts));
                command.Parameters.Add(new NpgsqlParameter("lock_until", (now + LockoutPolicy.LockDuration).ToUniversalTime()));
                command.Parameters.Add(new NpgsqlParameter("id", userId));
                command.Parameters.Add(new NpgsqlParameter("now", now.ToUniversalTime()));
                var outcome = await ReadOutcomeAsync(command, cancellationToken).ConfigureAwait(false);
                if (outcome is not null)
                {
                    return outcome;
                }
            }
        }
        finally
        {
            await Context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }

        // Satır güncellenmediyse hesap kilitli: hiçbir şey değişmedi (AC-7).
        var current = await FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return new LockoutOutcome(false, current?.FailedLoginCount ?? 0, current?.LockedUntil);
    }

    /// <inheritdoc />
    public async Task<bool> RegisterSuccessfulLoginAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var utc = now.ToUniversalTime();
        var updated = await Context.Users
            .Where(u => u.Id == userId && (u.LockedUntil == null || u.LockedUntil <= utc))
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.FailedLoginCount, 0).SetProperty(u => u.LastLoginAt, utc), cancellationToken)
            .ConfigureAwait(false);
        return updated == 1;
    }

    /// <inheritdoc />
    public Task<int> SyncAdminRoleAsync(string? normalizedAdminEmail, CancellationToken cancellationToken) =>
        Context.Database.ExecuteSqlRawAsync(
            SyncAdminRoleSql,
            [new NpgsqlParameter("admin_email", NpgsqlDbType.Varchar) { Value = (object?)normalizedAdminEmail ?? DBNull.Value }],
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
        await Context.Users.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> SetBannedAsync(Guid userId, DateTimeOffset? bannedAt, CancellationToken cancellationToken)
    {
        // Tek koşullu UPDATE (spec 0024 Req 3): eşzamanlı iki ban isteğinden yalnızca biri satırı değiştirir.
        var utc = bannedAt?.ToUniversalTime();
        var rows = utc is null
            ? Context.Users.Where(u => u.Id == userId && u.BannedAt != null)
            : Context.Users.Where(u => u.Id == userId && u.BannedAt == null);
        var updated = await rows.ExecuteUpdateAsync(set => set.SetProperty(u => u.BannedAt, utc), cancellationToken).ConfigureAwait(false);
        return updated == 1;
    }

    private static async Task<LockoutOutcome?> ReadOutcomeAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using (reader.ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            var count = reader.GetInt32(0);
            DateTimeOffset? lockedUntil = reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset>(1);
            return new LockoutOutcome(true, count, lockedUntil);
        }
    }
}
