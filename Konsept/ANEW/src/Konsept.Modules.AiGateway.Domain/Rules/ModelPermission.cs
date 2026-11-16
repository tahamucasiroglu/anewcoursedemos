namespace Konsept.Modules.AiGateway.Domain.Rules;

/// <summary>
/// Model kullanma yetkisi kararı (spec 0013 Req 1–2, BR-13): çağıranın yetkileri arasında <c>model.use:*</c> ya da
/// <c>model.use:&lt;model&gt;</c> varsa model kullanılabilir. Karşılaştırma ordinal; yetki yoksa kapalı başarısızlık.
/// </summary>
public static class ModelPermission
{
    /// <summary>Yetki öneki (Identity'deki <c>Permissions.ModelUsePrefix</c> ile aynı değer).</summary>
    public const string Prefix = "model.use:";

    /// <summary>Tüm modeller.</summary>
    public const string All = Prefix + "*";

    /// <summary>Çağıran modeli kullanabilir mi.</summary>
    /// <param name="permissions">Çağıranın yetkileri.</param>
    /// <param name="modelId">Model kimliği.</param>
    /// <returns>Yetkiliyse <c>true</c>.</returns>
    public static bool Allows(IEnumerable<string>? permissions, string modelId) =>
        permissions is not null
        && permissions.Any(p => string.Equals(p, All, StringComparison.Ordinal) || string.Equals(p, Prefix + modelId, StringComparison.Ordinal));
}
