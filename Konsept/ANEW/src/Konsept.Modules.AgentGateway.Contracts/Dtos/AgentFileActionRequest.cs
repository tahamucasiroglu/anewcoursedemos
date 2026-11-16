namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Dosya eylemi isteği (spec 0007 Req 4): videonun klasörünü dosya gezgininde ya da videoyu varsayılan uygulamada aç.</summary>
/// <param name="VideoId">Video sürümü.</param>
/// <param name="FileName">Ajan klasöründeki dosya adı (klasör yolu değil).</param>
/// <param name="Action"><see cref="Folder"/> ya da <see cref="Player"/>.</param>
public sealed record AgentFileActionRequest(Guid VideoId, string FileName, string Action)
{
    /// <summary>Klasörde aç.</summary>
    public const string Folder = "folder";

    /// <summary>Sistem oynatıcısında aç.</summary>
    public const string Player = "player";
}
