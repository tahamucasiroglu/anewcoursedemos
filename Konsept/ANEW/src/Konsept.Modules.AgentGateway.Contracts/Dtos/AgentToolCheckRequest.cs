namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Üretimden önce ajana sorulan araçlar (spec 0017 Req 14): araç kimlikleri (ör. <c>node</c>, <c>ffmpeg</c>, <c>hyperframes</c>).</summary>
/// <param name="Tools">Gereken araçlar.</param>
public sealed record AgentToolCheckRequest(IReadOnlyList<string> Tools);
