using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Mesai saatleri deposu (spec 0011): kurum filtreli, öğretmen başına tek satır (izlenen — değiştirmek için).</summary>
public sealed class QuietHoursRepository : IQuietHoursRepository
{
    private readonly OkulDbContext context;

    public QuietHoursRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<QuietHours?> FindAsync(Guid teacherUserId, CancellationToken cancellationToken) =>
        this.context.QuietHours.FirstOrDefaultAsync(q => q.TeacherUserId == teacherUserId, cancellationToken);

    public void Add(QuietHours quietHours) => this.context.QuietHours.Add(quietHours);

    public void Remove(QuietHours quietHours) => this.context.QuietHours.Remove(quietHours);
}
