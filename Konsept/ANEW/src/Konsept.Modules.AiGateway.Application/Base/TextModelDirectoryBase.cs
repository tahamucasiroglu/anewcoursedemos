using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// <see cref="ITextModelDirectory"/> (spec 0005 Req 5): katalog servisinin kullanıcıya göre etiketlerinden yalnızca metin modelleri;
/// seçilebilir = <c>available</c> (yetkili + key geçerli). "Key yok", "Key okunamıyor" ve "Yetki yok" seçilemez; görsel ve
/// katalogda metin türünde olmayan her giriş (yerel, video motoru) listede yoktur.
/// </summary>
/// <param name="catalog">Katalog servisi.</param>
public abstract class TextModelDirectoryBase(ICatalogService catalog) : ITextModelDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TextModelInfo>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await catalog.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return [];
        }

        return [.. result.Value.Models
            .Where(m => !m.Local && string.Equals(m.Kind, CatalogModelDto.KindText, StringComparison.Ordinal))
            .Select(m => new TextModelInfo(
                m.Id,
                m.Name,
                m.EffortLevels,
                m.DefaultEffort,
                string.Equals(m.Availability, CatalogModelDto.Available, StringComparison.Ordinal)))];
    }
}
