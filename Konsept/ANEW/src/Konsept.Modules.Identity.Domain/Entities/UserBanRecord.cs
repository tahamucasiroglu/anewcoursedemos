using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Bir ban ya da ban kaldırma kaydı (spec 0024 Req 8): hedef kullanıcı, yapan admin, an (UTC) ve (ban için) sebep. Kayıtlar
/// silinmez; yalnızca admin uçlarında görünür.
/// </summary>
public sealed class UserBanRecord
{
    private UserBanRecord(Guid id, Guid userId, BanAction action, Guid adminId, DateTimeOffset at, string? reason)
    {
        Id = id;
        UserId = userId;
        Action = action;
        AdminId = adminId;
        At = at;
        Reason = reason;
    }

    /// <summary>Gets kayıt kimliği (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets hedef kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets tür.</summary>
    public BanAction Action { get; private set; }

    /// <summary>Gets işlemi yapan admin.</summary>
    public Guid AdminId { get; private set; }

    /// <summary>Gets an (UTC).</summary>
    public DateTimeOffset At { get; private set; }

    /// <summary>Gets sebep (yalnızca ban; isteğe bağlı).</summary>
    public string? Reason { get; private set; }

    /// <summary>Kayıt oluşturur.</summary>
    /// <param name="userId">Hedef kullanıcı.</param>
    /// <param name="action">Tür.</param>
    /// <param name="adminId">Yapan admin.</param>
    /// <param name="at">An.</param>
    /// <param name="reason">Sebep (ban kaldırmada yok sayılır).</param>
    /// <returns>Kayıt.</returns>
    public static UserBanRecord Create(Guid userId, BanAction action, Guid adminId, DateTimeOffset at, string? reason)
    {
        var utc = at.ToUniversalTime();
        var text = action == BanAction.Ban ? BanReason.Normalize(reason) : null;
        if (text is { Length: > BanReason.MaxLength })
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "Sebep en fazla 500 karakter.");
        }

        return new UserBanRecord(Guid.CreateVersion7(utc), userId, action, adminId, utc, text);
    }
}
