using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Ports;

/// <summary>Öğretmenlerin mesai saatleri (spec 0011): kurum filtreli, öğretmen başına tek satır; satır yoksa sessiz mod kapalı.</summary>
public interface IQuietHoursRepository
{
    Task<QuietHours?> FindAsync(Guid teacherUserId, CancellationToken cancellationToken);

    void Add(QuietHours quietHours);

    void Remove(QuietHours quietHours);
}
