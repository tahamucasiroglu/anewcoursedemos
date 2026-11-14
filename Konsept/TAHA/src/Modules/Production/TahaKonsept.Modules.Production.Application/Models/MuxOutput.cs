namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// <c>Mux</c> aşamasının çıktısı: ajana kaydedilen video kimliği.
/// </summary>
public sealed record MuxOutput
{
    /// <summary>Video.</summary>
    public Guid VideoId { get; init; }
}
