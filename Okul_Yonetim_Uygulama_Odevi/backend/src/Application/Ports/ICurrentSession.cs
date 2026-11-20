namespace OkulYonetim.Application.Ports;

/// <summary>
/// İsteğin oturumu: kim (kişi), hangi oturum, seçili kurum, kurucu mu ve görebildiği şubeler. Kimlik doğrulama
/// katmanı her istekte oturumu veritabanından doğrulayıp doldurur; kurum ve şube filtresi buradan okur (S1). Kurum ve
/// şube kimliği hiçbir zaman istemciden alınmaz.
/// </summary>
public interface ICurrentSession
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? SessionId { get; }

    Guid? TenantId { get; }

    bool IsFounder { get; }

    /// <summary>Platform yöneticisi ve bu oturum iki adımlı doğrulamayla açılmış (spec 0007).</summary>
    bool IsPlatformAdmin { get; }

    IReadOnlyCollection<Guid> VisibleBranchIds { get; }
}
