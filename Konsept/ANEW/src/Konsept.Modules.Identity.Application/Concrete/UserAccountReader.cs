using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Hesap bilgisini kullanıcı deposundan okur (spec 0021 Req 6).</summary>
/// <param name="users">Kullanıcı deposu.</param>
public sealed class UserAccountReader(IUserRepository users) : UserAccountReaderBase
{
    /// <inheritdoc />
    protected override async Task<DateTimeOffset?> LoadRegisteredAtAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user?.RegisteredAt;
    }
}
