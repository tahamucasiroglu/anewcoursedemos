namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Durdur isteği.</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Lane">Şerit (<c>topic</c> · <c>idea</c>).</param>
public sealed record StopJobRequest(Guid? ProfileId, string? Lane);
