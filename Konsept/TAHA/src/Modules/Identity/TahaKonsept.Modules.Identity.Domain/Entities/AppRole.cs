using Microsoft.AspNetCore.Identity;

namespace TahaKonsept.Modules.Identity.Domain.Entities;

/// <summary>
/// ASP.NET Core Identity rolü (Guid v7 anahtar). Şimdilik yalnızca <c>Admin</c> rolü kullanılır.
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    /// <summary>EF için boş oluşturucu.</summary>
    public AppRole()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>Adıyla rol oluşturur.</summary>
    /// <param name="name">Rol adı.</param>
    public AppRole(string name)
        : base(name)
    {
        Id = Guid.CreateVersion7();
    }
}
