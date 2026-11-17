namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajanın araç denetimi cevabı: kurulu olmayan araçların kimlikleri (boşsa hepsi hazır) ve bunlardan ajanın kendi gömülü bileşen
/// listesinden indirip kurabildiklerinin bilgisi (spec 0027 Req 1). <see cref="Installable"/>'da olmayan eksik "kurulamaz" sayılır.
/// </summary>
/// <param name="Missing">Eksik araçlar.</param>
/// <param name="Installable">Eksiklerden ajanın kurabildikleri; eski ajanda <c>null</c>.</param>
/// <param name="Unsupported">Eksiklerden bu bilgisayarda çalışamayanlar (ör. ekran kartı gerekir; spec 0028 Req 6); eski ajanda <c>null</c>.</param>
public sealed record AgentToolCheckReply(
    IReadOnlyList<string> Missing,
    IReadOnlyList<AgentComponentInfo>? Installable = null,
    IReadOnlyList<AgentUnsupportedTool>? Unsupported = null)
{
    /// <summary>Ajan sözleşmeye/sürüme uymadığında eksik bildirilen kimlik (ajan güncellenmeli; kurulamaz).</summary>
    public const string AgentProtocol = "agent";
}
