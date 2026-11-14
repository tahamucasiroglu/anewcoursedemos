using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Base;

/// <summary>
/// <see cref="IProfileTemplateCatalog"/> ortak davranışı: şablonları bir kez yükler (<see cref="LoadTemplates"/>) ve bellekte tutar.
/// </summary>
public abstract class ProfileTemplateCatalogBase : IProfileTemplateCatalog
{
    private readonly Lazy<IReadOnlyList<ProfileTemplateDto>> _templates;

    /// <summary>Oluşturur.</summary>
    protected ProfileTemplateCatalogBase()
    {
        _templates = new Lazy<IReadOnlyList<ProfileTemplateDto>>(LoadTemplates, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Tüm şablonlar.</summary>
    /// <returns>Şablonlar.</returns>
    public IReadOnlyList<ProfileTemplateDto> GetAll() => _templates.Value;

    /// <summary>Anahtarla şablon.</summary>
    /// <param name="key">Anahtar.</param>
    /// <returns>Şablon ya da <c>null</c>.</returns>
    public ProfileTemplateDto? Find(string key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : _templates.Value.FirstOrDefault(t => string.Equals(t.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Şablonları kaynağından yükler (bir kez çağrılır).</summary>
    /// <returns>Şablonlar.</returns>
    protected abstract IReadOnlyList<ProfileTemplateDto> LoadTemplates();
}
