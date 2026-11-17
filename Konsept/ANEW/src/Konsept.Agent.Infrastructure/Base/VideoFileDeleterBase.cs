using Konsept.Agent.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Kalıcı silmenin ortak mantığı (spec 0019 Req 4, AC-6): ad kuralları <see cref="VideoFileActionsBase"/> ile aynıdır (yalnızca video klasöründeki
/// düz <c>.mp4</c> adı; yol, <c>..</c>, <c>:</c> akış adı yok). Kurala uymayan ad bu klasörde bir dosyayı gösteremez: hiçbir şey silinmez ve
/// silinmiş sayılır (klasör dışına asla dokunulmaz). Diskte olmayan dosya silinmiş sayılır. Silme hatası (açık dosya, izin yok) adı
/// "kalan"lara ekler; diğer dosyalar yine silinir — tekrar denemede yalnızca kalanlar gider.
/// </summary>
/// <param name="videoFolder">Video klasörünü veren kaynak.</param>
public abstract class VideoFileDeleterBase(Func<string> videoFolder) : IVideoFileDeleter
{
    /// <inheritdoc />
    public AgentDeleteFilesReply Delete(AgentDeleteFilesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var folder = videoFolder();
        var remaining = new List<string>();
        foreach (var name in (request.FileNames ?? []).Distinct(StringComparer.Ordinal))
        {
            if (!IsPlainVideoName(name))
            {
                continue;
            }

            var path = Path.GetFullPath(Path.Combine(folder, name));
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                DeleteFile(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                remaining.Add(name);
            }
        }

        return new AgentDeleteFilesReply(remaining);
    }

    /// <summary>Dosyayı siler.</summary>
    /// <param name="path">Tam yol (doğrulanmış, var).</param>
    protected abstract void DeleteFile(string path);

    private static bool IsPlainVideoName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal)
        && !name.Contains(':', StringComparison.Ordinal)
        && string.Equals(Path.GetExtension(name), ".mp4", StringComparison.OrdinalIgnoreCase);
}
