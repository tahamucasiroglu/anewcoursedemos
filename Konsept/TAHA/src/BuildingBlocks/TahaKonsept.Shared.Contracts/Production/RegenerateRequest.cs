using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Yeniden üret isteği (<c>POST api/production/videos/{id}/regenerate</c>): sıfırdan (yeniden kullanmadan) üretim; isteğe bağlı düzenlenmiş konsept metniyle.
/// </summary>
public sealed record RegenerateRequest
{
    /// <summary>Düzenlenmiş konsept (<c>null</c> = konseptin güncel hali).</summary>
    public ConceptDocument? Document { get; init; }

    /// <summary>Ayarlar (<c>null</c> = kullanıcının güncel ayarları).</summary>
    public ProductionSettingsDto? Settings { get; init; }
}
