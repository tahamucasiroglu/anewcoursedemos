namespace Konsept.Modules.Identity.Contracts.Abstract;

/// <summary>
/// Başka modüllerin kullanıcı hesabı hakkında okuyabildiği en az bilgi (Y-1). Günlük geçmişi gezilebilecek ilk günü hesabın açıldığı
/// günden alır (spec 0021 Req 6).
/// </summary>
public interface IUserAccountReader
{
    /// <summary>Hesabın açıldığı anı döndürür.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt anı (UTC); kullanıcı yoksa <c>null</c>.</returns>
    Task<DateTimeOffset?> GetRegisteredAtAsync(Guid userId, CancellationToken cancellationToken);
}
