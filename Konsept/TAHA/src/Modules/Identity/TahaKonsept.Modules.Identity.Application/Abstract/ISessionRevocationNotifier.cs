namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// PUBLIC genişleme noktası: oturumlar iptal edildiğinde haber alır. Identity birden çok uygulamayı destekler
/// (<c>IEnumerable&lt;ISessionRevocationNotifier&gt;</c>, hepsi çağrılır; biri hata verirse loglanır, diğerleri sürer).
/// Varsayılan: <c>NoOpSessionRevocationNotifier</c>. Agents modülü kendi uygulamasını ekler
/// (<c>services.AddScoped&lt;ISessionRevocationNotifier, AgentSessionRevocationNotifier&gt;()</c>) ve ajanları hub'dan düşürür;
/// kolaylık için <c>SessionRevocationNotifierBase</c>'den türetip yalnızca gerekeni override edebilir.
/// </summary>
public interface ISessionRevocationNotifier
{
    /// <summary>Kullanıcının tüm oturumları (web + ajan) iptal edildi (logout-all, yasaklama).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnAllSessionsRevokedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Kullanıcının tek bir token ailesi iptal edildi (ör. ajan kaldırıldı ya da tekrar kullanım tespit edildi).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnTokenFamilyRevokedAsync(Guid userId, Guid familyId, CancellationToken ct = default);
}
