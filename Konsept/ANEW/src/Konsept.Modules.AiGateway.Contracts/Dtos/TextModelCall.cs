using Konsept.Modules.AiGateway.Contracts.Enums;

namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>
/// Metin modeli çağrısı (spec 0014): çağıran, model, rol (harcama kaydında amaç), profil, efor (yoksa katalog varsayılanı), sistem metni
/// ve istem. Çıktı sınırı verilmez: modelin katalog limiti uygulanır (AC-20; kodda sabit yok).
/// </summary>
public sealed record TextModelCall
{
    /// <summary>Gets çağıran.</summary>
    public required ModelCallerIdentity Caller { get; init; }

    /// <summary>Gets katalog model kimliği.</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets model rolü.</summary>
    public ModelRole Role { get; init; }

    /// <summary>Gets ilgili profil.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Gets efor; <c>null</c> → katalog varsayılanı.</summary>
    public string? Effort { get; init; }

    /// <summary>Gets sistem metni.</summary>
    public string? System { get; init; }

    /// <summary>Gets istem.</summary>
    public required string Prompt { get; init; }
}
