using System.Text;
using System.Text.Json;
using TahaKonsept.Agent.Account.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;

namespace TahaKonsept.Agent.Account.Base;

/// <summary>
/// <see cref="IPairedAccountReader"/> tabanı: JWT erişim token'ının yük kısmını (imza doğrulamadan, yalnızca görüntüleme için)
/// çözüp <c>name</c> (yoksa <c>sub</c>) talebini okur (<see cref="ReadNameFromJwt"/>). Token kaynağı türetilen sınıftadır.
/// </summary>
public abstract class PairedAccountReaderBase : IPairedAccountReader
{
    /// <inheritdoc />
    public string? GetAccountName() => ReadNameFromJwt(GetAccessToken());

    /// <summary>JWT'den kullanıcı adını okur.</summary>
    /// <param name="jwt">Erişim token'ı.</param>
    /// <returns>Kullanıcı adı ya da <c>null</c>.</returns>
    public static string? ReadNameFromJwt(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            foreach (var claim in new[] { CustomClaimTypes.UserName, CustomClaimTypes.UserId })
            {
                if (doc.RootElement.TryGetProperty(claim, out var value) && value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    return value.GetString();
                }
            }

            return null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Kayıtlı erişim token'ı (yoksa <c>null</c>).</summary>
    /// <returns>Token ya da <c>null</c>.</returns>
    protected abstract string? GetAccessToken();
}
