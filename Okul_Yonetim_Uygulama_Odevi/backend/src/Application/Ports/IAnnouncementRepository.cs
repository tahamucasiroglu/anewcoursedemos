using OkulYonetim.Application.Features.Communication.Announcements;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Duyurular, alıcıları, kapsanan öğrenciler ve onay cevapları (spec 0011). Hepsi kurum filtreli; kimin neyi göreceği
/// (alıcı veli, yayınlayan, kurucu) çağıranın — <c>CommunicationGuard</c> ile — verdiği kişi kimliğiyle süzülür. Liste ve
/// sonuç okumaları izlemesiz projeksiyon (T7); öğrenci ve kişi adları kendi filtrelerinden geçerek eklenir.
/// </summary>
public interface IAnnouncementRepository
{
    Task<Announcement?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<string> AuthorNameAsync(Guid authorUserId, CancellationToken cancellationToken);

    /// <summary>Velinin bu duyurudaki alıcı satırı (izlenen — okundu yazmak için); alıcı değilse <c>null</c>.</summary>
    Task<AnnouncementRecipient?> FindRecipientAsync(Guid announcementId, Guid guardianUserId, CancellationToken cancellationToken);

    /// <summary>Öğrencinin bu duyurudaki onay cevabı (izlenen); yoksa <c>null</c>.</summary>
    Task<ConsentResponse?> FindConsentAsync(Guid announcementId, Guid studentId, CancellationToken cancellationToken);

    Task<bool> CoversStudentAsync(Guid announcementId, Guid studentId, CancellationToken cancellationToken);

    /// <summary>Kurucu ya da öğretmen listesi: <paramref name="authorUserId"/> verilirse yalnızca onun duyuruları.</summary>
    Task<IReadOnlyList<StaffAnnouncementRow>> ListForStaffAsync(Guid? authorUserId, int max, CancellationToken cancellationToken);

    /// <summary>Velinin alıcısı olduğu duyurular, yeniden eskiye, en fazla <paramref name="max"/>.</summary>
    Task<IReadOnlyList<FeedAnnouncementRow>> ListForGuardianAsync(Guid guardianUserId, int max, CancellationToken cancellationToken);

    /// <summary>Verilen duyuruların, verilen öğrencilerden kapsadıkları (izlemesiz).</summary>
    Task<IReadOnlyList<AnnouncementStudent>> ListCoveredAsync(IReadOnlyCollection<Guid> announcementIds, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid guardianUserId, CancellationToken cancellationToken);

    /// <summary>Yayın anı <paramref name="since"/> ya da sonrası olan duyurular, kapsam ve alıcı/okuyan sayılarıyla (0011 sonuç sayımı; spec 0017 kartı).</summary>
    Task<IReadOnlyList<AnnouncementReadRow>> ListPublishedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Verilen duyurulardan en az birini okumamış farklı alıcı veli sayısı (spec 0017 kartı).</summary>
    Task<int> CountUnreadGuardiansAsync(IReadOnlyCollection<Guid> announcementIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipientRow>> ListRecipientsAsync(Guid announcementId, CancellationToken cancellationToken);

    /// <summary>Alıcı velilerin, duyurunun kapsadığı ve bugün de bağlı oldukları çocukları (ad).</summary>
    Task<IReadOnlyList<RecipientChild>> ListRecipientChildrenAsync(Guid announcementId, CancellationToken cancellationToken);

    /// <summary>
    /// Kapsanan öğrencilerin onay durumu (cevapsız da dahil); <paramref name="studentIds"/> verilirse yalnızca onlar. Öğrenci adı
    /// kendi filtresinden; görünmeyen öğrencinin adı boş.
    /// </summary>
    Task<IReadOnlyList<ConsentChildView>> ListConsentAsync(Guid announcementId, IReadOnlyCollection<Guid>? studentIds, CancellationToken cancellationToken);

    void Add(Announcement announcement);

    void AddRecipient(AnnouncementRecipient recipient);

    void AddStudent(AnnouncementStudent student);

    void AddConsent(ConsentResponse response);
}
