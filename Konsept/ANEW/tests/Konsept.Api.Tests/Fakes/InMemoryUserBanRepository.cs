using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Ban kayıtlarının bellek uygulaması (gerçek EF uygulaması entegrasyon testinde).</summary>
public sealed class InMemoryUserBanRepository : IUserBanRepository
{
    private readonly Lock _gate = new();
    private readonly List<UserBanRecord> _records = [];

    /// <summary>Gets tüm kayıtlar.</summary>
    public IReadOnlyList<UserBanRecord> Records
    {
        get
        {
            lock (_gate)
            {
                return [.. _records];
            }
        }
    }

    /// <summary>Gets or sets bir sonraki eklemede atılacak hata (yarıda kalan ban senaryosu, review r1 M-1).</summary>
    public Exception? FailNext { get; set; }

    /// <inheritdoc />
    public Task AddAsync(UserBanRecord record, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (FailNext is { } failure)
            {
                FailNext = null;
                throw failure;
            }

            _records.Add(record);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UserBanRecord>> ListForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<UserBanRecord>>([.. _records.Where(r => userIds.Contains(r.UserId)).OrderByDescending(r => r.At)]);
        }
    }
}
