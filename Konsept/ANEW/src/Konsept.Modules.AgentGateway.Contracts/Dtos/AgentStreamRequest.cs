namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Yerel akış isteği (spec 0017 Req 21): video ve ajan klasöründeki dosya adı.</summary>
/// <param name="VideoId">Video.</param>
/// <param name="FileName">Dosya adı (klasör yolu değil).</param>
public sealed record AgentStreamRequest(Guid VideoId, string FileName);
