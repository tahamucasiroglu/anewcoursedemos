using Konsept.Modules.Identity.Contracts.Abstract;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>Hesap bilgisi okuyucusunun ortak mantığı: kullanıcıyı yükler, yalnızca kayıt anını dışarı verir.</summary>
public abstract class UserAccountReaderBase : IUserAccountReader
{
    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetRegisteredAtAsync(Guid userId, CancellationToken cancellationToken) =>
        await LoadRegisteredAtAsync(userId, cancellationToken).ConfigureAwait(false);

    /// <summary>Kullanıcının kayıt anını yükler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt anı ya da <c>null</c>.</returns>
    protected abstract Task<DateTimeOffset?> LoadRegisteredAtAsync(Guid userId, CancellationToken cancellationToken);
}
