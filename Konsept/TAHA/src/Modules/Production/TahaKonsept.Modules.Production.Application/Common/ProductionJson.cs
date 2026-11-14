using System.Text.Json;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.Modules.Production.Application.Common;

/// <summary>
/// Production modülünün JSON yardımcıları: ortak sözleşme ayarlarıyla (<see cref="ContractJson.Options"/>; camelCase, enum metin)
/// serileştirme ve hataya dayanıklı okuma (<see cref="Read{T}"/> bozuk/boş JSON'da <c>null</c> döner).
/// </summary>
public static class ProductionJson
{
    /// <summary>Nesneyi JSON'a çevirir.</summary>
    /// <typeparam name="T">Tür.</typeparam>
    /// <param name="value">Değer.</param>
    /// <returns>JSON.</returns>
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);

    /// <summary>JSON'u okur; boş ya da bozuksa <c>null</c>.</summary>
    /// <typeparam name="T">Tür.</typeparam>
    /// <param name="json">JSON.</param>
    /// <returns>Değer ya da <c>null</c>.</returns>
    public static T? Read<T>(string? json)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, ContractJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
