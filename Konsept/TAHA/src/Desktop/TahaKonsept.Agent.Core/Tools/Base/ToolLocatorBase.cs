using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Tools.Base;

/// <summary>
/// Araç bulucu tabanı. Sıra: (1) <c>ToolsDir</c> altındaki aday göreli yollar (<see cref="GetLocalCandidates"/>; tek <c>*</c>
/// segmenti joker olabilir, en yeni ad önce), (2) PATH'te çalıştırılabilir adlar (<see cref="GetPathExecutableNames"/>),
/// (3) bilinen sabit konumlar (<see cref="GetWellKnownLocations"/>), (4) türetilmiş çözüm (<see cref="FindDerived"/>; ör. npm, node'un yanında).
/// </summary>
public abstract class ToolLocatorBase : IToolLocator
{
    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    protected ToolLocatorBase(IAgentPaths paths)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>Ajan klasörleri.</summary>
    protected IAgentPaths Paths { get; }

    /// <inheritdoc />
    public string? Find(ToolKind kind)
    {
        foreach (var relative in GetLocalCandidates(kind))
        {
            var found = ResolvePattern(Paths.ToolsDir, relative);
            if (found is not null)
            {
                return found;
            }
        }

        foreach (var name in GetPathExecutableNames(kind))
        {
            var found = SearchPath(name);
            if (found is not null)
            {
                return found;
            }
        }

        foreach (var location in GetWellKnownLocations(kind))
        {
            if (File.Exists(location))
            {
                return location;
            }
        }

        return FindDerived(kind);
    }

    /// <inheritdoc />
    public Result<string> Require(ToolKind kind)
    {
        var path = Find(kind);
        return path is not null
            ? Result.Success(path)
            : Error.NotFound("agent.tool_missing", GetMissingMessage(kind));
    }

    /// <summary><c>ToolsDir</c>'e göre aday göreli yollar (ör. <c>node/node.exe</c>, <c>python/*/python.exe</c>).</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Adaylar.</returns>
    protected abstract IEnumerable<string> GetLocalCandidates(ToolKind kind);

    /// <summary>PATH'te aranacak dosya adları.</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Adlar.</returns>
    protected abstract IEnumerable<string> GetPathExecutableNames(ToolKind kind);

    /// <summary>Bilinen sabit konumlar (ör. <c>C:\Windows\System32\nvidia-smi.exe</c>).</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Mutlak yollar.</returns>
    protected virtual IEnumerable<string> GetWellKnownLocations(ToolKind kind) => [];

    /// <summary>Başka bir araca göre türetilen yol (varsayılan: yok).</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Yol ya da <c>null</c>.</returns>
    protected virtual string? FindDerived(ToolKind kind) => null;

    /// <summary>Araç bulunamadığında kullanıcıya gösterilecek mesaj.</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Türkçe mesaj.</returns>
    protected abstract string GetMissingMessage(ToolKind kind);

    /// <summary>PATH değişkeninde dosya arar.</summary>
    /// <param name="fileName">Dosya adı.</param>
    /// <returns>Tam yol ya da <c>null</c>.</returns>
    protected static string? SearchPath(string fileName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        foreach (var dir in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim('"'), fileName);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            catch (ArgumentException)
            {
                // PATH'te geçersiz karakterli girdi; atla.
            }
        }

        return null;
    }

    /// <summary>Göreli deseni (tek <c>*</c> segment destekli) kökte çözer.</summary>
    /// <param name="root">Kök.</param>
    /// <param name="relativePattern">Göreli desen (<c>/</c> ayraçlı).</param>
    /// <returns>Var olan dosya ya da <c>null</c>.</returns>
    protected static string? ResolvePattern(string root, string relativePattern)
    {
        var segments = relativePattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return ResolveSegments(root, segments, 0);
    }

    private static string? ResolveSegments(string current, string[] segments, int index)
    {
        if (index == segments.Length)
        {
            return File.Exists(current) ? Path.GetFullPath(current) : null;
        }

        var segment = segments[index];
        if (segment != "*")
        {
            return ResolveSegments(Path.Combine(current, segment), segments, index + 1);
        }

        if (!Directory.Exists(current))
        {
            return null;
        }

        foreach (var dir in Directory.GetDirectories(current).OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var found = ResolveSegments(dir, segments, index + 1);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
