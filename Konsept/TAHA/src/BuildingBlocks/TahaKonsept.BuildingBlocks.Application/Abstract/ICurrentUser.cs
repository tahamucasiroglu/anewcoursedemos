namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// İsteği yapan kullanıcı bilgisi (API'de JWT claim'lerinden okunur).
/// Önemli üyeler: <see cref="UserId"/>, <see cref="IsAuthenticated"/>, <see cref="HasPermission"/>, <see cref="GetRequiredUserId"/>.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Kullanıcı kimliği (oturum yoksa <c>null</c>).</summary>
    Guid? UserId { get; }

    /// <summary>Kullanıcı adı (oturum yoksa <c>null</c>).</summary>
    string? UserName { get; }

    /// <summary>Kimliği doğrulanmış mı.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Kullanıcının izinleri (<c>permission</c> claim'leri).</summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>Kullanıcı verilen izne sahip mi.</summary>
    /// <param name="permission">İzin adı (ör. <c>models.use</c>).</param>
    /// <returns>Sahipse <c>true</c>.</returns>
    bool HasPermission(string permission);

    /// <summary>Kullanıcı kimliğini döner; oturum yoksa <see cref="InvalidOperationException"/> fırlatır (programlama hatası).</summary>
    /// <returns>Kullanıcı kimliği.</returns>
    Guid GetRequiredUserId();
}
