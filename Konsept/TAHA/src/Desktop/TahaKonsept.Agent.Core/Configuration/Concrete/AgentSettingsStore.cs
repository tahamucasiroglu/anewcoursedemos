using System.Buffers.Text;
using System.Security.Cryptography;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Storage.Base;

namespace TahaKonsept.Agent.Core.Configuration.Concrete;

/// <summary>
/// <c>ConfigDir/settings.json</c> tabanlı ayar deposu. İlk okumada eksik alanları tamamlar: sunucu adresi
/// (<see cref="AgentOptions.DefaultServerBaseUrl"/>), port, 32 baytlık rastgele akış anahtarı (base64url) ve kurulum kimliği.
/// </summary>
public sealed class AgentSettingsStore : JsonFileStoreBase<AgentSettings>, IAgentSettingsStore
{
    /// <summary>Ayar dosyasının adı.</summary>
    public const string FileName = "settings.json";

    private readonly AgentOptions _options;
    private AgentSettings? _current;

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="options">Başlangıç seçenekleri.</param>
    public AgentSettingsStore(IAgentPaths paths, AgentOptions options)
        : base(Path.Combine(paths.ConfigDir, FileName))
    {
        _options = options;
    }

    /// <inheritdoc />
    public event EventHandler<AgentSettings>? Changed;

    /// <inheritdoc />
    public AgentSettings Current
    {
        get
        {
            lock (Gate)
            {
                return _current ??= LoadOrCreate();
            }
        }
    }

    /// <inheritdoc />
    public AgentSettings Update(Func<AgentSettings, AgentSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        AgentSettings updated;
        lock (Gate)
        {
            updated = Complete(change(Current));
            Write(updated);
            _current = updated;
        }

        Changed?.Invoke(this, updated);
        return updated;
    }

    /// <summary>Yeni rastgele akış anahtarı üretir (URL güvenli).</summary>
    /// <returns>Anahtar.</returns>
    public static string GenerateStreamKey() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    private AgentSettings LoadOrCreate()
    {
        var stored = Read();
        var completed = Complete(stored ?? new AgentSettings { VideoServerPort = _options.VideoServerPort });
        if (stored is null || completed != stored)
        {
            Write(completed);
        }

        return completed;
    }

    private AgentSettings Complete(AgentSettings settings) => settings with
    {
        ServerBaseUrl = string.IsNullOrWhiteSpace(settings.ServerBaseUrl) ? _options.DefaultServerBaseUrl : settings.ServerBaseUrl.Trim(),
        VideoServerPort = settings.VideoServerPort is > 0 and < 65536 ? settings.VideoServerPort : _options.VideoServerPort,
        StreamKey = string.IsNullOrWhiteSpace(settings.StreamKey) ? GenerateStreamKey() : settings.StreamKey,
        InstallId = settings.InstallId == Guid.Empty ? Guid.CreateVersion7() : settings.InstallId,
    };
}
