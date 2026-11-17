using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// Katalog listesi (spec 0004 Req 1–6): seed'in kendisi, değer eklemeden ya da değiştirmeden (AC-2); her model, isteği yapan
/// kullanıcının sağlayıcı satırına göre "Kullanılabilir" / "Key yok" / "Key okunamıyor" etiketini alır. Kullanıcının yetkisi
/// olmayan model, key durumundan önce "Yetki yok" etiketini alır (spec 0013 Req 3). Yerel girişler (spec 0006 Req 18) sağlayıcı satırına
/// bağlı değildir: key gerektirmez, yalnızca yetkiye göre "Kullanılabilir" / "Yetki yok" alır. Tür eşlemesi açıktır (B-89).
/// </summary>
/// <param name="catalog">Katalog.</param>
/// <param name="keys">Kullanıcının key durumları.</param>
/// <param name="permissions">Kullanıcının yetkileri.</param>
public abstract class CatalogServiceBase(ICatalogSource catalog, IApiKeyService keys, ICallerPermissions permissions) : ICatalogService
{
    /// <inheritdoc />
    public async Task<Result<CatalogDto>> GetAsync(CancellationToken cancellationToken)
    {
        var states = await keys.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!states.IsSuccess)
        {
            return Result<CatalogDto>.Failure(states.Error!);
        }

        var seed = catalog.Catalog;
        var byProvider = states.Value.ToDictionary(s => s.ProviderId, s => s.State, StringComparer.Ordinal);
        var providers = seed.Providers.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var models = seed.Models
            .Where(m => m.Local || providers.ContainsKey(m.ProviderId))
            .Select(m => ToDto(
                m,
                m.Local ? new ProviderEntry { Id = m.ProviderId, Name = seed.LocalProviderName } : providers[m.ProviderId],
                m.Local ? ApiKeyStatusDto.StateSaved : byProvider.GetValueOrDefault(m.ProviderId),
                ModelPermission.Allows(permissions.Current, m.Id)))
            .ToList();
        return Result<CatalogDto>.Success(new CatalogDto(seed.Version, [.. seed.Providers.Select(p => new CatalogProviderDto(p.Id, p.Name))], models));
    }

    private static CatalogModelDto ToDto(ModelEntry model, ProviderEntry provider, string? keyState, bool permitted) =>
        new(
            model.Id,
            provider.Id,
            provider.Name,
            model.Name,
            KindOf(model.Kind),
            model.EffortLevels,
            model.DefaultEffort,
            model.OutputTokenLimit,
            model.TimeoutSeconds,
            model.InputPricePerMillionUsd,
            model.OutputPricePerMillionUsd,
            model.PricePerImageUsd,
            !permitted ? CatalogModelDto.NoPermission : keyState switch
            {
                ApiKeyStatusDto.StateSaved => CatalogModelDto.Available,
                ApiKeyStatusDto.StateUnreadable => CatalogModelDto.Unreadable,
                _ => CatalogModelDto.NoKey,
            },
            model.Local,
            model.LanguageOnly,
            model.LicenseWarning,
            model.Component,
            model.MinVramGb);

    /// <summary>Katalog türünün API adı (B-89: her tür açıkça eşlenir; bilinmeyen tür metin sayılmaz).</summary>
    /// <param name="kind">Tür.</param>
    /// <returns>Ad.</returns>
    public static string KindOf(ModelKind kind) => kind switch
    {
        ModelKind.Text => CatalogModelDto.KindText,
        ModelKind.Image => CatalogModelDto.KindImage,
        ModelKind.Audio => CatalogModelDto.KindAudio,
        ModelKind.VideoEngine => CatalogModelDto.KindVideoEngine,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Bilinmeyen katalog türü."),
    };
}
