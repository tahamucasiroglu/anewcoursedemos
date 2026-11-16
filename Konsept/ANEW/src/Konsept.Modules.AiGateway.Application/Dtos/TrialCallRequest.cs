namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>Deneme çağrısı isteği (plan 0013 Risk 3): model ve isteğe bağlı efor; istem ve çıktı sınırı sunucuda sabittir.</summary>
public sealed record TrialCallRequest
{
    /// <summary>Gets model kimliği.</summary>
    public string? ModelId { get; init; }

    /// <summary>Gets efor; <c>null</c> → katalog varsayılanı.</summary>
    public string? Effort { get; init; }
}
