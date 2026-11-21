using System.Runtime.CompilerServices;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Gerçek mesai saatleri deposunun sarmalayıcısı (spec 0016 review tur 1 R1-2): anahtar açıkken okuma veritabanı hatası gibi düşer — randevu
/// commit edildikten sonra öğretmen bildirimi kapısının mesai saatini okuması. Hata mesajı kişisel veri taşır (<see cref="Message"/>):
/// loga yalnızca hata türü ve kimlikler yazılmalı. Anahtar kapalıyken her şey olduğu gibi aktarılır.
/// </summary>
public sealed class ThrowingQuietHoursRepository : IQuietHoursRepository
{
    public const string Message = "bağlantı koptu: Ayşe Yılmaz +905551112233";

    private readonly IQuietHoursRepository inner;
    private readonly StrongBox<bool> fail;

    public ThrowingQuietHoursRepository(IQuietHoursRepository inner, StrongBox<bool> fail)
    {
        this.inner = inner;
        this.fail = fail;
    }

    public int Failures { get; private set; }

    public Task<QuietHours?> FindAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref this.fail.Value))
        {
            this.Failures++;
            throw new InvalidOperationException(Message);
        }

        return this.inner.FindAsync(teacherUserId, cancellationToken);
    }

    public void Add(QuietHours quietHours) => this.inner.Add(quietHours);

    public void Remove(QuietHours quietHours) => this.inner.Remove(quietHours);
}
