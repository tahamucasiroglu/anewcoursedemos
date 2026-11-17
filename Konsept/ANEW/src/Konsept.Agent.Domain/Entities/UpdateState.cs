namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Güncellemenin diskteki durumu (spec 0023 Req 9; <c>update-state.json</c>): kurulan ama henüz onaylanmamış sürüm, kaç kez onaysız açıldığı,
/// saklanan önceki sürüm ve geri dönülen sürüm (önceki sürüm açılınca kullanıcıya "Yeni sürüm başlatılamadı" der).
/// </summary>
/// <param name="PendingVersion">Kurulan, ilk açılışı onay bekleyen sürüm; yoksa <c>null</c>.</param>
/// <param name="PendingLaunches">Bekleyen sürümün onaysız açılış sayısı.</param>
/// <param name="PreviousVersion">Saklanan önceki çalışan sürüm.</param>
/// <param name="RolledBackFrom">Başlatılamayıp geri dönülen sürüm; bildirim gösterilince temizlenir.</param>
public sealed record UpdateState(string? PendingVersion = null, int PendingLaunches = 0, string? PreviousVersion = null, string? RolledBackFrom = null);
