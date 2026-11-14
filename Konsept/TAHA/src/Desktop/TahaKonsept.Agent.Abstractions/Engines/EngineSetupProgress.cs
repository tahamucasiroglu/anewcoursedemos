namespace TahaKonsept.Agent.Abstractions.Engines;

/// <summary>Motor kurulumu sırasında raporlanan ilerleme: mesaj ve 0–100 yüzde (bilinmiyorsa <c>null</c>).</summary>
/// <param name="Message">Kullanıcıya gösterilecek mesaj.</param>
/// <param name="Percent">Yüzde (0–100) ya da <c>null</c>.</param>
public sealed record EngineSetupProgress(string Message, double? Percent);
