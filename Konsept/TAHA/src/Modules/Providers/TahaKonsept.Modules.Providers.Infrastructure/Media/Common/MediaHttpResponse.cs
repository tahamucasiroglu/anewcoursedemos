using System.Text;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Başarılı bir sağlayıcı HTTP yanıtının gövdesi (ham baytlar) ve içerik türü. JSON yanıtlar <see cref="MediaJson.Read{T}"/> ile,
/// ses/görsel yanıtları doğrudan <see cref="Body"/> olarak kullanılır.
/// </summary>
/// <param name="Body">Gövde baytları.</param>
/// <param name="ContentType">İçerik türü (<c>Content-Type</c> medya türü; yoksa null).</param>
public sealed record MediaHttpResponse(byte[] Body, string? ContentType)
{
    /// <summary>Gövdenin UTF-8 metni.</summary>
    public string Text => Encoding.UTF8.GetString(Body);
}
