namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>
/// Ajan bilgisayarında çalışamayan araç/model (spec 0028 Req 6): kimlik, sebep kodu ve (ekran kartı gereksiniminde) gereken en az ayrılmış
/// bellek. Kurulabilir sayılmaz; iş çağrı yapılmadan açık mesajla durur.
/// </summary>
/// <param name="Id">Bileşen kimliği.</param>
/// <param name="Reason">Sebep: <see cref="GpuRequired"/>.</param>
/// <param name="MinVramMb">Gereken en az ayrılmış ekran kartı belleği (MB) ya da <c>null</c>.</param>
public sealed record AgentUnsupportedTool(string Id, string Reason, int? MinVramMb = null)
{
    /// <summary>Uygun ekran kartı yok.</summary>
    public const string GpuRequired = "gpu_required";
}
