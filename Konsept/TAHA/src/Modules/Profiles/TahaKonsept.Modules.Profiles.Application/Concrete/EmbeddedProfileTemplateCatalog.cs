using System.Text.Json;
using TahaKonsept.Modules.Profiles.Application.Base;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Concrete;

/// <summary>
/// Şablonları derlemeye gömülü <c>Templates/profile-templates.json</c> dosyasından okuyan katalog (ortak JSON ayarlarıyla).
/// </summary>
public sealed class EmbeddedProfileTemplateCatalog : ProfileTemplateCatalogBase
{
    /// <summary>Gömülü kaynağın adı.</summary>
    public const string ResourceName = "TahaKonsept.Modules.Profiles.Application.Templates.profile-templates.json";

    /// <summary>Gömülü JSON'u okur.</summary>
    /// <returns>Şablonlar.</returns>
    protected override IReadOnlyList<ProfileTemplateDto> LoadTemplates()
    {
        using var stream = typeof(EmbeddedProfileTemplateCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Gömülü profil şablonları bulunamadı: {ResourceName}");
        return JsonSerializer.Deserialize<List<ProfileTemplateDto>>(stream, ContractJson.Options) ?? [];
    }
}
