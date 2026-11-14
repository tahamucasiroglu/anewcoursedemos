using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Hazır profil şablonları kataloğu (kullanıcıya ait değil, sabit). <see cref="GetAll"/> tüm şablonlar, <see cref="Find"/> anahtarla şablon.
/// </summary>
public interface IProfileTemplateCatalog
{
    /// <summary>Tüm şablonlar (görüntüleme sırasıyla).</summary>
    /// <returns>Şablonlar.</returns>
    IReadOnlyList<ProfileTemplateDto> GetAll();

    /// <summary>Anahtarla şablon bulur (büyük/küçük harf duyarsız).</summary>
    /// <param name="key">Şablon anahtarı.</param>
    /// <returns>Şablon ya da <c>null</c>.</returns>
    ProfileTemplateDto? Find(string key);
}
