using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// İş <i>Üretiliyor</i>'a geçerken sabitlenen değerler (spec 0006 Req 7, 20; spec 0017 Req 1): profilin o anki ayarları ve konseptin kaydındaki
/// dil/biçim/süre. Daha önce sabitlenmiş iş (Durduruldu/Hata'dan devam) ilk değerlerini korur. Ses kapalıysa altyazı uygulanmaz.
/// </summary>
public static class JobFixing
{
    /// <summary>İşin sabit ayarları.</summary>
    /// <param name="job">İş.</param>
    /// <param name="settings">Profilin güncel ayarları.</param>
    /// <param name="now">Zaman (UTC).</param>
    /// <returns>Sabit ayarlar.</returns>
    public static FixedJobSettings Fix(ProductionJob job, ProductionSettings settings, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(settings);
        return job.FixedSettings ?? new FixedJobSettings
        {
            Subtitles = settings.Subtitles && settings.AudioModel is not null,
            ProgressBar = settings.ProgressBar,
            CodeModel = settings.CodeModel,
            CodeEffort = settings.CodeEffort,
            Engine = settings.Engine,
            ImageModel = settings.ImageModel,
            AudioModel = settings.AudioModel,
            Language = job.Language,
            Format = job.Format,
            TargetDurationSeconds = job.TargetDurationSeconds,
            FixedAt = now,
        };
    }
}
