namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// İşin bekleyen indirmesi (spec 0027 Req 3, 5): ajanın kurabildiği eksik bileşenler ve kullanıcının onaylayıp onaylamadığı. İş
/// Ajan bekleniyor'dayken onaysız indirme işi bekletir; onaylı indirmede hat kurulumu ajana yaptırır. Kurulum bitince silinir.
/// </summary>
public sealed record PendingInstall
{
    /// <summary>Gets indirilecek bileşenler.</summary>
    public IReadOnlyList<InstallItem> Components { get; init; } = [];

    /// <summary>Gets a value indicating whether kullanıcı indirmeyi onayladı.</summary>
    public bool Approved { get; init; }

    /// <summary>Eksiklerin hepsi onaylanan bileşenlerin içinde mi.</summary>
    /// <param name="missing">Eksik bileşen kimlikleri.</param>
    /// <returns>Hepsi onaylıysa <c>true</c>.</returns>
    public bool Covers(IEnumerable<string> missing) =>
        Approved && missing.All(id => Components.Any(c => string.Equals(c.Id, id, StringComparison.Ordinal)));
}
