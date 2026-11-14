namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ses önizleme sonucu: ajanın yerel video sunucusundan çalınabilecek ses adresi ya da hata.
/// </summary>
public sealed record SpeechPreviewResult
{
    /// <summary>İstek kimliği.</summary>
    public Guid RequestId { get; init; }

    /// <summary>Başarılı mı.</summary>
    public bool Success { get; init; }

    /// <summary>Ajan yerel sunucusundaki ses yolu (ör. <c>/previews/{id}.wav</c>).</summary>
    public string? AudioPath { get; init; }

    /// <summary>Hata mesajı.</summary>
    public string? ErrorMessage { get; init; }
}
