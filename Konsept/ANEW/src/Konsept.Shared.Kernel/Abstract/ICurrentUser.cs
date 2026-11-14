namespace Konsept.Shared.Kernel.Abstract;

/// <summary>
/// İsteği yapan kullanıcı (plan 0002). Giriş yapılmamışsa kimlik alanları <c>null</c>'dır; servisler kullanıcıya göre
/// filtrelerken (BR-1) bunu kullanır.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Gets kullanıcı kimliği (<c>sub</c>); giriş yapılmamışsa <c>null</c>.</summary>
    Guid? UserId { get; }

    /// <summary>Gets oturum kimliği (<c>sid</c>); giriş yapılmamışsa <c>null</c>.</summary>
    Guid? SessionId { get; }

    /// <summary>Gets a value indicating whether istek kimliği doğrulanmış bir kullanıcıdan geliyor.</summary>
    bool IsAuthenticated { get; }
}
