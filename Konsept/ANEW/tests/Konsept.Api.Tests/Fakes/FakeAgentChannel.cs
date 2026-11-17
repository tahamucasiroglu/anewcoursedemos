using System.Text.Json;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Sahte ajan kanalı (plan 0017): gerçek SignalR yerine komutları bağlantı kimliğiyle kaydeder; araç denetimi, aşama ve akış cevapları
/// testten ayarlanır. Varsayılan: tüm araçlar kurulu, her aşama GPU'da başarılı, akış adresi verilir. Gerçek ajan, render ya da LLM yok.
/// </summary>
public sealed class FakeAgentChannel : IAgentChannel
{
    private readonly Lock _gate = new();
    private readonly List<(string ConnectionId, string Method, string Payload)> _sent = [];

    /// <summary>Initializes a new instance of the <see cref="FakeAgentChannel"/> class.</summary>
    public FakeAgentChannel()
    {
        Install = DefaultInstall;
    }

    /// <summary>Gets or sets eksik araçlar.</summary>
    public IReadOnlyList<string> Missing { get; set; } = [];

    /// <summary>Gets or sets araç denetiminden önce beklenecek görev (test sırası için); yoksa <c>null</c>.</summary>
    public Func<Task>? BeforeCheck { get; set; }

    /// <summary>Gets or sets ajanın kurabildiği bileşenler (spec 0027); araç denetimi eksiklerden bunları bildirir.</summary>
    public IReadOnlyList<AgentComponentInfo> Installable { get; set; } = [];

    /// <summary>Gets or sets kurulum komutunun cevabı (spec 0027); varsayılan: istenenleri kurar (eksiklerden çıkarır) ve başarı döner.</summary>
    public Func<string, AgentInstallRequest, CancellationToken, Task<AgentInstallReply>> Install { get; set; }

    /// <summary>Gets or sets aşama işleyicisi (bağlantı, komut, iptal).</summary>
    public Func<string, AgentStageCommand, CancellationToken, Task<AgentStageReply>> Stage { get; set; } = DefaultStage;

    /// <summary>Gets or sets akış işleyicisi.</summary>
    public Func<AgentStreamRequest, AgentStreamReply> Stream { get; set; } =
        r => new AgentStreamReply("http://127.0.0.1:47615/video/" + r.VideoId.ToString("N") + "?t=test", null);

    /// <summary>Gets or sets dosya eylemi işleyicisi (spec 0007; varsayılan: açıldı).</summary>
    public Func<AgentFileActionRequest, CancellationToken, Task<AgentFileActionReply>> FileAction { get; set; } = DefaultFileAction;

    /// <summary>Gets or sets kalıcı silme işleyicisi (spec 0019; varsayılan: hepsi silindi).</summary>
    public Func<AgentDeleteFilesRequest, CancellationToken, Task<AgentDeleteFilesReply>> DeleteFiles { get; set; } = DefaultDeleteFiles;

    /// <summary>Gets gönderilenler (bağlantı, metot, JSON yük).</summary>
    public IReadOnlyList<(string ConnectionId, string Method, string Payload)> Sent
    {
        get
        {
            lock (_gate)
            {
                return [.. _sent];
            }
        }
    }

    /// <summary>Varsayılan aşama: GPU'da başarılı; Supertonic seslendirmesinde AI ses notu.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">İptal.</param>
    /// <returns>Cevap.</returns>
    public static Task<AgentStageReply> DefaultStage(string connectionId, AgentStageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(new AgentStageReply(
            true, Device: "gpu", AiVoiceNotice: command.Stage == AgentStageCommand.Merge && command.AudioModel == "local/supertonic-3"));
    }

    /// <summary>Varsayılan dosya eylemi: açıldı.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal.</param>
    /// <returns>Cevap.</returns>
    public static Task<AgentFileActionReply> DefaultFileAction(AgentFileActionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new AgentFileActionReply(true, null));

    /// <summary>Varsayılan kalıcı silme: hepsi silindi.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal.</param>
    /// <returns>Cevap.</returns>
    public static Task<AgentDeleteFilesReply> DefaultDeleteFiles(AgentDeleteFilesRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new AgentDeleteFilesReply([]));

    /// <summary>Belirli bir metodun komut sayısı.</summary>
    /// <param name="method">Metot.</param>
    /// <param name="stage">Aşama (yalnızca <c>RunStage</c>).</param>
    /// <returns>Sayı.</returns>
    public int Count(string method, string? stage = null) =>
        Sent.Count(s => s.Method == method && (stage is null || s.Payload.Contains("\"stage\":\"" + stage + "\"", StringComparison.Ordinal)));

    /// <summary>Kayıtları temizler, varsayılanlara döner.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _sent.Clear();
        }

        Missing = [];
        BeforeCheck = null;
        Installable = [];
        Install = DefaultInstall;
        Stage = DefaultStage;
        FileAction = DefaultFileAction;
        DeleteFiles = DefaultDeleteFiles;
    }

    /// <inheritdoc />
    public Task<AgentPingReply> PingAsync(string connectionId, CancellationToken cancellationToken) =>
        Task.FromResult(new AgentPingReply("PC", "1.0.0"));

    /// <inheritdoc />
    public async Task<AgentToolCheckReply> CheckToolsAsync(string connectionId, AgentToolCheckRequest request, CancellationToken cancellationToken)
    {
        Record(connectionId, "CheckTools", request);
        if (BeforeCheck is { } wait)
        {
            await wait().ConfigureAwait(false);
        }

        var missing = Missing;
        return new AgentToolCheckReply([.. missing], [.. Installable.Where(c => missing.Contains(c.Id))]);
    }

    /// <summary>Kurulum komutu (spec 0027).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    public Task<AgentInstallReply> InstallComponentsAsync(string connectionId, AgentInstallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Record(connectionId, "InstallComponents", request);
        return Install(connectionId, request, cancellationToken);
    }

    /// <summary>Varsayılan kurulum: istenenler artık eksik değil.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başarı.</returns>
    public Task<AgentInstallReply> DefaultInstall(string connectionId, AgentInstallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Missing = [.. Missing.Where(id => !request.Components.Contains(id))];
        return Task.FromResult(new AgentInstallReply(true, []));
    }

    /// <inheritdoc />
    public Task<AgentStageReply> RunStageAsync(string connectionId, AgentStageCommand command, CancellationToken cancellationToken)
    {
        Record(connectionId, "RunStage", command);
        return Stage(connectionId, command, cancellationToken);
    }

    /// <inheritdoc />
    public Task CancelStageAsync(string connectionId, Guid jobId, CancellationToken cancellationToken)
    {
        Record(connectionId, "CancelStage", jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AgentStreamReply> OpenStreamAsync(string connectionId, AgentStreamRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Record(connectionId, "OpenStream", request);
        return Task.FromResult(Stream(request));
    }

    /// <inheritdoc />
    public Task<AgentFileActionReply> OpenFileAsync(string connectionId, AgentFileActionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Record(connectionId, "OpenFile", request);
        return FileAction(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentDeleteFilesReply> DeleteFilesAsync(string connectionId, AgentDeleteFilesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Record(connectionId, "DeleteFiles", request);
        return DeleteFiles(request, cancellationToken);
    }

    private void Record(string connectionId, string method, object payload)
    {
        lock (_gate)
        {
            _sent.Add((connectionId, method, JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
        }
    }
}
