namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>İşin bekleyen indirmesi (spec 0027 Req 3, 5): onaylandı mı ve indirilecek bileşenler.</summary>
/// <param name="Approved">Kullanıcı onayladı mı.</param>
/// <param name="Components">Bileşenler.</param>
public sealed record PendingInstallDto(bool Approved, IReadOnlyList<InstallItemDto> Components);
