using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary><see cref="IModelDirectory"/> (spec 0006): katalog servisinin kullanıcıya göre etiketli listesi, tüm türler.</summary>
/// <param name="catalog">Katalog servisi.</param>
public abstract class ModelDirectoryBase(ICatalogService catalog) : IModelDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelInfo>?> ListAllAsync(CancellationToken cancellationToken)
    {
        var result = await catalog.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return null;
        }

        return [.. result.Value.Models.Select(m => new ModelInfo(
            m.Id, m.Name, m.Kind, m.Local, m.LanguageOnly, m.LicenseWarning, m.Component, m.EffortLevels, m.DefaultEffort, m.Availability, m.MinVramGb))];
    }
}
