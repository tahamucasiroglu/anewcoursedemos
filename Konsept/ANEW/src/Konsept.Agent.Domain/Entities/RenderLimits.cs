namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Sahne render'ının kaynak sınırları (spec 0018 Req 1, L4): Job Object ile uygulanır; değerler <c>agentsettings.json</c>'dan gelir (plan 0018,
/// geliştirici PC'si ölçümü). Aşılırsa render ve tüm alt süreçleri sonlandırılır.
/// </summary>
/// <param name="SceneTimeout">Sahne başına duvar saati süresi.</param>
/// <param name="MemoryBytes">Render süreç ağacının toplam bellek sınırı.</param>
/// <param name="MaxProcesses">Aynı anda en çok süreç sayısı.</param>
public sealed record RenderLimits(TimeSpan SceneTimeout, long MemoryBytes, int MaxProcesses);
