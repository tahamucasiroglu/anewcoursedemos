using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Updates.Abstract;

namespace TahaKonsept.Agent.Updates.Base;

/// <summary>
/// <see cref="IAgentUpdater"/> tabanı: hata yakalama ve loglama (güncelleme hatası uygulamayı asla düşürmez);
/// denetim/indirme/kurulum adımları türetilen sınıftadır.
/// </summary>
public abstract class AgentUpdaterBase : IAgentUpdater
{
    /// <summary>Oluşturur.</summary>
    /// <param name="logger">Log.</param>
    protected AgentUpdaterBase(ILogger logger) => Logger = logger;

    /// <inheritdoc />
    public bool IsUpdatePending { get; private set; }

    /// <summary>Log.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task<bool> CheckAndDownloadAsync(CancellationToken ct)
    {
        try
        {
            if (!CanUpdate())
            {
                Logger.LogInformation("Güncelleme denetimi atlandı (uygulama Velopack ile kurulmamış).");
                return false;
            }

            IsUpdatePending = await DownloadIfNewerAsync(ct).ConfigureAwait(false);
            return IsUpdatePending;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Logger.LogWarning("Güncelleme denetlenemedi: {Message}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public void ApplyOnExit()
    {
        if (!IsUpdatePending)
        {
            return;
        }

        try
        {
            ScheduleApply();
            Logger.LogInformation("İndirilen güncelleme kapanışta kurulacak.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Logger.LogWarning("Güncelleme kurulumu ayarlanamadı: {Message}", ex.Message);
        }
    }

    /// <summary>Uygulama güncellenebilir şekilde kurulu mu.</summary>
    /// <returns>Kuruluysa <c>true</c>.</returns>
    protected abstract bool CanUpdate();

    /// <summary>Yeni sürüm varsa indirir.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>İndirildiyse <c>true</c>.</returns>
    protected abstract Task<bool> DownloadIfNewerAsync(CancellationToken ct);

    /// <summary>İndirilen sürümün kapanıştan sonra kurulmasını ayarlar.</summary>
    protected abstract void ScheduleApply();
}
