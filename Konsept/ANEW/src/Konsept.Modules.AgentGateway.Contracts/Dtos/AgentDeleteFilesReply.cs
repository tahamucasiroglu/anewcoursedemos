namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Kalıcı silme cevabı (spec 0019 Req 4): silinemeyen dosya adları (açık dosya, izin yok vb.). Boşsa hepsi silindi; diskte zaten olmayan dosya
/// silinmiş sayılır.
/// </summary>
/// <param name="Remaining">Silinemeyenler.</param>
public sealed record AgentDeleteFilesReply(IReadOnlyList<string> Remaining);
