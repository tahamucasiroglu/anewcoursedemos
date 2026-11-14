using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Core.Prerequisites;
using TahaKonsept.Agent.Headless.Abstract;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Agent.Headless.Base;

/// <summary>
/// <see cref="IHeadlessAgentRunner"/> akışı: adres → ön gereksinimler → çekirdek → eşleme döngüsü (kod dosyaya; onay beklenir;
/// süre dolarsa yeni kod) → oturum açıkken <see cref="IAgentAuthService.AuthorizationChanged"/> beklenir → iptalde çekirdek durdurulur.
/// Kod dosyası biçimi: 1. satır kullanıcı kodu, 2. satır doğrulama adresi. Eşlenince dosyaya <c>PAIRED</c> yazılır.
/// </summary>
public abstract class HeadlessAgentRunnerBase : IHeadlessAgentRunner
{
    private readonly IAgentHost _host;
    private readonly IAgentAuthService _auth;
    private readonly IAgentSettingsStore _settings;

    /// <summary>Oluşturur.</summary>
    /// <param name="host">Ajan çekirdeği.</param>
    /// <param name="auth">Oturum servisi.</param>
    /// <param name="settings">Ajan ayarları.</param>
    /// <param name="logger">Log.</param>
    protected HeadlessAgentRunnerBase(IAgentHost host, IAgentAuthService auth, IAgentSettingsStore settings, ILogger logger)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Log.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task<int> RunAsync(HeadlessLaunchOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);
        Logger.LogInformation("Penceresiz mod başlıyor (sunucu: {Server}).", options.ServerUrl ?? _settings.Current.ServerBaseUrl);
        if (!string.IsNullOrWhiteSpace(options.ServerUrl)
            && !string.Equals(_settings.Current.ServerBaseUrl, options.ServerUrl, StringComparison.OrdinalIgnoreCase))
        {
            _settings.Update(s => s with { ServerBaseUrl = options.ServerUrl });
        }

        if (!options.SkipPrerequisites && !await EnsurePrerequisitesAsync(ct).ConfigureAwait(false))
        {
            return 2;
        }

        var started = await _host.StartAsync(ct).ConfigureAwait(false);
        if (started.IsFailure)
        {
            Logger.LogWarning("Ajan çekirdeği uyarıyla başladı: {Error}", started.Error.Message);
        }

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!_auth.IsPaired)
                {
                    await PairOnceAsync(options.PairCodeOut, ct).ConfigureAwait(false);
                    continue;
                }

                await WaitForSignOutAsync(ct).ConfigureAwait(false);
                if (!ct.IsCancellationRequested)
                {
                    Logger.LogWarning("Oturum kapandı (sunucu yetkiyi kaldırdı); yeniden eşleme gerekiyor.");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Normal kapanış.
        }

        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await _host.StopAsync(stop.Token).ConfigureAwait(false);
        Logger.LogInformation("Penceresiz mod kapandı.");
        return 0;
    }

    /// <summary>Kodu dosyaya yazar (yol boşsa yazmaz).</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="content">İçerik.</param>
    protected static void WriteCodeFile(string? path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, content);
    }

    private async Task<bool> EnsurePrerequisitesAsync(CancellationToken ct)
    {
        if (_host.Prerequisites.AreRequiredInstalled())
        {
            Logger.LogInformation("Ön gereksinimler kurulu.");
            return true;
        }

        Logger.LogInformation("Eksik ön gereksinimler kuruluyor…");
        var progress = new Progress<PrerequisiteProgress>(p =>
        {
            if (!string.IsNullOrWhiteSpace(p.LogLine))
            {
                Logger.LogInformation("[Kurulum] {Step}: {Line}", p.StepName, p.LogLine);
            }
        });
        var installed = await _host.Prerequisites.InstallAsync(null, progress, ct).ConfigureAwait(false);
        if (installed.IsFailure)
        {
            Logger.LogError("Ön gereksinimler kurulamadı: {Error}", installed.Error.Message);
            return false;
        }

        return true;
    }

    private async Task PairOnceAsync(string? codeFile, CancellationToken ct)
    {
        var start = await _host.Pairing.StartAsync(ct).ConfigureAwait(false);
        if (start.IsFailure)
        {
            Logger.LogWarning("Eşleme başlatılamadı: {Error}; 10 sn sonra yeniden denenecek.", start.Error.Message);
            await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
            return;
        }

        DeviceStartResponse response = start.Value;
        WriteCodeFile(codeFile, response.UserCode + Environment.NewLine + response.VerificationUrl + Environment.NewLine);
        Logger.LogInformation("Eşleme kodu: {Code} — web'de onaylayın: {Url}", response.UserCode, response.VerificationUrl);
        var result = await _host.Pairing.WaitForApprovalAsync(response, ct).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            WriteCodeFile(codeFile, "PAIRED" + Environment.NewLine);
            Logger.LogInformation("Eşlendi.");
        }
        else
        {
            Logger.LogWarning("Eşleme tamamlanmadı: {Error}", result.Error.Message);
        }
    }

    private async Task WaitForSignOutAsync(CancellationToken ct)
    {
        var signedOut = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnChanged(object? sender, EventArgs e)
        {
            if (!_auth.IsPaired)
            {
                signedOut.TrySetResult();
            }
        }

        _auth.AuthorizationChanged += OnChanged;
        try
        {
            if (_auth.IsPaired)
            {
                await signedOut.Task.WaitAsync(ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _auth.AuthorizationChanged -= OnChanged;
        }
    }
}
