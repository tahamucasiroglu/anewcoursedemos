namespace Konsept.Agent.Domain.Entities;

/// <summary>Araç denetimi sonucu (spec 0028 Req 2, 6): eksik kimlikler (bağımlılıklar dahil, liste sırasında) ve bu bilgisayarda desteklenmeyenler.</summary>
/// <param name="Missing">Eksik araç/bileşen kimlikleri.</param>
/// <param name="Unsupported">Ekran kartı gereksinimi karşılanmayan (kurulsa da çalışamayacak) bileşenler.</param>
public sealed record ToolCheckResult(IReadOnlyList<string> Missing, IReadOnlyList<SetupComponent> Unsupported);
