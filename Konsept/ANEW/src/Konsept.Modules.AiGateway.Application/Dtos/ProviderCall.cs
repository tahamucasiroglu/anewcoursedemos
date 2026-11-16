using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>
/// Kapının sağlayıcı istemcisine verdiği, kuralları uygulanmış tek çağrı: sağlayıcı, model, çözülmüş key (yalnızca AiGateway içinde,
/// Y-7; loglanmaz), efor, kırpılmış çıktı sınırı, sistem metni, istem ve görsel adedi.
/// </summary>
/// <param name="Provider">Sağlayıcı.</param>
/// <param name="Model">Model.</param>
/// <param name="ApiKey">Çağıranın çözülmüş key'i.</param>
/// <param name="Effort">Efor.</param>
/// <param name="MaxOutputTokens">Çıktı sınırı (metin).</param>
/// <param name="System">Sistem metni.</param>
/// <param name="Prompt">İstem.</param>
/// <param name="ImageCount">Görsel adedi.</param>
public sealed record ProviderCall(
    ProviderEntry Provider,
    ModelEntry Model,
    string ApiKey,
    string? Effort,
    int? MaxOutputTokens,
    string? System,
    string Prompt,
    int ImageCount)
{
    /// <inheritdoc />
    public override string ToString() => $"{Provider.Id}/{Model.Id}"; // Kayıt yazdırılırsa key görünmez.
}
