namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptin yaşam döngüsü durumu: <see cref="Generated"/> (Üretilenler listesinde), <see cref="Approved"/> (Konseptten Video'da),
/// <see cref="Deleted"/> (Silinenler; sonsuza kadar saklanır, kalıcı silme ayrı işlemdir).
/// </summary>
public enum ConceptStatus
{
    /// <summary>Üretildi, henüz onaylanmadı.</summary>
    Generated = 0,

    /// <summary>Onaylandı; videoya dönüştürülebilir.</summary>
    Approved = 1,

    /// <summary>Silindi (Silinenler); konu önerilerinde negatif örnek.</summary>
    Deleted = 2,
}
