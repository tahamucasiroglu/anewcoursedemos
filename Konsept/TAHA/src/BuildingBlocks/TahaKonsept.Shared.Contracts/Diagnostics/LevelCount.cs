namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Seviyeye göre günlük kaydı sayısı: <c>{ level: "Warning", count: 12 }</c>.
/// </summary>
public sealed record LevelCount
{
    /// <summary>Seviye.</summary>
    public LogLevelKind Level { get; init; }

    /// <summary>Kayıt sayısı.</summary>
    public int Count { get; init; }
}
