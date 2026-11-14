using System.Text.Json;
using System.Text.Json.Serialization;

namespace TahaKonsept.Shared.Contracts.Common;

/// <summary>
/// API, Web ve ajan arasında ortak JSON ayarları: web varsayılanları (camelCase, büyük/küçük harf duyarsız) + enum'lar metin olarak.
/// MVC, SignalR (sunucu ve istemci) ve HTTP istemcileri <see cref="Apply"/> ile aynı ayarı kullanmalıdır.
/// </summary>
public static class ContractJson
{
    /// <summary>Paylaşılan, salt okunur ayar örneği.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Yeni bir ayar örneği üretir.</summary>
    /// <returns>Ayarlar.</returns>
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Apply(options);
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    /// <summary>Var olan ayarlara ortak kuralları uygular.</summary>
    /// <param name="options">Değiştirilecek ayarlar.</param>
    public static void Apply(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        if (!options.Converters.OfType<JsonStringEnumConverter>().Any())
        {
            options.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
