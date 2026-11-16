namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Yetki adları (docs/security.md, yetki tabanlı politikalar). Bu spec yalnızca <see cref="AdminPanel"/>'i uygular;
/// <c>model.use:&lt;model&gt;</c> 0013'te, <c>publish.use</c> yayınlama gelince eklenir (spec 0011 Req 2).
/// </summary>
public static class Permissions
{
    /// <summary>Admin alanına giriş yetkisi.</summary>
    public const string AdminPanel = "admin.panel";

    /// <summary>Model kullanma yetkisinin öneki (spec 0013 Req 1–2): <c>model.use:&lt;model kimliği&gt;</c>.</summary>
    public const string ModelUsePrefix = "model.use:";

    /// <summary>Katalogdaki tüm modelleri kullanma yetkisi (varsayılan, spec 0013 Req 2).</summary>
    public const string ModelUseAll = ModelUsePrefix + "*";
}
