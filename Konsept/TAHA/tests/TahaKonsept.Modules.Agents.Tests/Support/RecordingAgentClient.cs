using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Sunucu → ajan çağrılarını kaydeden sahte istemci. <see cref="OnPreviewSpeech"/> ile önizleme isteğine tepki verilebilir;
/// <see cref="ThrowOnSend"/> gönderim hatası benzetir.
/// </summary>
public sealed class RecordingAgentClient : IAgentClient
{
    /// <summary>Kaydedilen çağrılar (ad + argüman).</summary>
    public List<(string Method, object? Argument)> Calls { get; } = [];

    /// <summary>Önizleme isteği geldiğinde çalışır.</summary>
    public Action<SpeechPreviewCommand>? OnPreviewSpeech { get; set; }

    /// <summary>Doluysa her çağrı bu istisnayı fırlatır.</summary>
    public Exception? ThrowOnSend { get; set; }

    /// <summary>Aşama çalıştır.</summary>
    /// <param name="command">Komut.</param>
    /// <returns>Görev.</returns>
    public Task ExecuteStage(StageCommand command) => Record(nameof(ExecuteStage), command);

    /// <summary>İptal.</summary>
    /// <param name="jobId">İş.</param>
    /// <returns>Görev.</returns>
    public Task CancelJob(Guid jobId) => Record(nameof(CancelJob), jobId);

    /// <summary>Duraklat.</summary>
    /// <param name="jobId">İş.</param>
    /// <returns>Görev.</returns>
    public Task PauseJob(Guid jobId) => Record(nameof(PauseJob), jobId);

    /// <summary>Klasör aç.</summary>
    /// <param name="videoId">Video.</param>
    /// <returns>Görev.</returns>
    public Task OpenFolder(Guid videoId) => Record(nameof(OpenFolder), videoId);

    /// <summary>Sistem oynatıcısı.</summary>
    /// <param name="videoId">Video.</param>
    /// <returns>Görev.</returns>
    public Task OpenInSystemPlayer(Guid videoId) => Record(nameof(OpenInSystemPlayer), videoId);

    /// <summary>Video sil.</summary>
    /// <param name="videoId">Video.</param>
    /// <returns>Görev.</returns>
    public Task DeleteVideo(Guid videoId) => Record(nameof(DeleteVideo), videoId);

    /// <summary>Ses önizleme.</summary>
    /// <param name="command">Komut.</param>
    /// <returns>Görev.</returns>
    public Task PreviewSpeech(SpeechPreviewCommand command)
    {
        var task = Record(nameof(PreviewSpeech), command);
        OnPreviewSpeech?.Invoke(command);
        return task;
    }

    /// <summary>Yetenekleri yenile.</summary>
    /// <returns>Görev.</returns>
    public Task RefreshCapabilities() => Record(nameof(RefreshCapabilities), null);

    private Task Record(string method, object? argument)
    {
        if (ThrowOnSend is not null)
        {
            throw ThrowOnSend;
        }

        Calls.Add((method, argument));
        return Task.CompletedTask;
    }
}
