using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Belirteci ajan klasöründeki <c>agent-session.dat</c> dosyasında tutar (kullanıcı profili izinleri; şifreleme backlog'da,
/// plan 0012 Risk 5). Dosya içeriği hiçbir loga yazılmaz.
/// </summary>
/// <param name="directory">Ajan klasörü.</param>
public sealed class FileAgentTokenStore(string directory) : AgentTokenStoreBase
{
    /// <summary>Dosya adı.</summary>
    public const string FileName = "agent-session.dat";

    private string FilePath => Path.Combine(directory, FileName);

    /// <inheritdoc />
    protected override string? Load() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

    /// <inheritdoc />
    protected override void Store(string token)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, token);
    }

    /// <inheritdoc />
    protected override void Delete()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
