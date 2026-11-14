namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// İzin adları (JWT <c>permission</c> claim değerleri). API'de aynı adlarla authorization policy'leri kayıtlıdır:
/// <c>[Authorize(Policy = PermissionNames.AdminAccess)]</c>. Yönetici (<see cref="RoleNames.Admin"/>) hepsine sahiptir.
/// </summary>
public static class PermissionNames
{
    /// <summary>Yapay zekâ modellerini kullanma.</summary>
    public const string ModelsUse = "models.use";

    /// <summary>Yayınlama özelliklerini kullanma.</summary>
    public const string PublishUse = "publish.use";

    /// <summary>Yönetim paneline erişim.</summary>
    public const string AdminAccess = "admin.access";

    /// <summary>Tanımlı tüm izinler.</summary>
    public static IReadOnlyList<string> All { get; } = [ModelsUse, PublishUse, AdminAccess];
}
