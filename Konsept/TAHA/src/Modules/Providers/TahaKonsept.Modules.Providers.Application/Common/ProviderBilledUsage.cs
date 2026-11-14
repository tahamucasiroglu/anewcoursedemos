using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Common;

/// <summary>
/// Sağlayıcının işi kabul edip ücretlendirdiği hâlde başarısız dönen çağrıların bilinen kullanımını hata üzerinde taşır
/// (ör. gönderilmiş ama sonucu indirilemeyen görsel işi, kullanım bilgisiyle gelen "refusal" yanıtı). <see cref="Attach"/> kullanımı
/// <see cref="Error.Details"/> içinde ayrılmış anahtara yazar; ölçen taban (<c>AiModelBase</c>) <see cref="Read"/> ile okuyup CallLog'a
/// maliyet olarak yazar ve <see cref="Strip"/> ile anahtarı çağırana dönmeden siler.
/// </summary>
public static class ProviderBilledUsage
{
    /// <summary>Ayrılmış ayrıntı anahtarı.</summary>
    public const string DetailKey = "providers.billed_usage";

    /// <summary>Hataya ücretlenmiş kullanımı ekler.</summary>
    /// <param name="error">Hata.</param>
    /// <param name="usage">Kullanım.</param>
    /// <returns>Kullanımı taşıyan hata.</returns>
    public static Error Attach(Error error, CallUsage usage)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(usage);
        var details = error.Details is null ? new Dictionary<string, string[]>() : new Dictionary<string, string[]>(error.Details);
        details[DetailKey] = [JsonSerializer.Serialize(usage)];
        return error with { Details = details };
    }

    /// <summary>Hatadaki ücretlenmiş kullanımı okur.</summary>
    /// <param name="error">Hata.</param>
    /// <returns>Kullanım (yoksa <see cref="CallUsage.Empty"/>).</returns>
    public static CallUsage Read(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error.Details is null || !error.Details.TryGetValue(DetailKey, out var values) || values.Length == 0)
        {
            return CallUsage.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<CallUsage>(values[0]) ?? CallUsage.Empty;
        }
        catch (JsonException)
        {
            return CallUsage.Empty;
        }
    }

    /// <summary>Ayrılmış anahtarı hatadan siler.</summary>
    /// <param name="error">Hata.</param>
    /// <returns>Temiz hata.</returns>
    public static Error Strip(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error.Details is null || !error.Details.ContainsKey(DetailKey))
        {
            return error;
        }

        var details = error.Details.Where(p => p.Key != DetailKey).ToDictionary(p => p.Key, p => p.Value);
        return error with { Details = details.Count == 0 ? null : details };
    }
}
