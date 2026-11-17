namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Kalıcı silme isteği (spec 0019 Req 4): videonun tüm sürümlerinin ajan klasöründeki dosya adları (klasör yolu değil).</summary>
/// <param name="VideoId">Video (konsept).</param>
/// <param name="FileNames">Dosya adları.</param>
public sealed record AgentDeleteFilesRequest(Guid VideoId, IReadOnlyList<string> FileNames);
