using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Appointments;

/// <summary>
/// Öğretmen başına randevu sırası (spec 0016 plan Risks 3): müsaitlik açma, dilim kaldırma, talep, yanıt ve iptal aynı anahtarda
/// (<c>appointments:teacher:&lt;öğretmen&gt;</c>) sıraya girer; kararlar sıra içinde taze okumayla verilir. Her işlem tek anahtar alır
/// (iki anahtarlı sıra yok — kilit sırası sorunu doğmaz). Sıra 5 sn içinde gelmezse "Çok fazla istek".
/// </summary>
public static class AppointmentLock
{
    public const string KeyPrefix = "appointments:teacher:";

    public static string Key(Guid teacherUserId) => KeyPrefix + teacherUserId.ToString("N");

    /// <summary>Öğretmenin sırasına girer; sıra gelmezse Türkçe "Çok fazla istek" iş hatası.</summary>
    public static async Task<IExclusiveScope> EnterAsync(IUnitOfWork unitOfWork, AppointmentsGuard guard, Guid teacherUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(guard);
        try
        {
            return await unitOfWork.BeginExclusiveAsync(Key(teacherUserId), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw await guard.BusinessAsync(AppointmentsMessages.TooManyRequests).ConfigureAwait(false);
        }
    }
}
