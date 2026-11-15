using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Oturum kullanıcısı (testler). Varsayılan kimlik kurucudan gelir; <see cref="Current"/> ile başka kullanıcıya geçilebilir
/// (kullanıcı ayrımı testleri).
/// </summary>
/// <param name="userId">Kullanıcı.</param>
public sealed class TestCurrentUser(Guid userId) : ICurrentUser
{
    /// <summary>Şu anki oturum kullanıcısı (testler değiştirebilir).</summary>
    public Guid Current { get; set; } = userId;

    /// <inheritdoc />
    public Guid? UserId => Current;

    /// <inheritdoc />
    public string? UserName => "taha";

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => [];

    /// <inheritdoc />
    public bool HasPermission(string permission) => true;

    /// <inheritdoc />
    public Guid GetRequiredUserId() => Current;
}
