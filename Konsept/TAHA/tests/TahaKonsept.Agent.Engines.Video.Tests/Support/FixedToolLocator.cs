using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Her aracı sabit sahte yola çözen araç bulucu (<c>&lt;tür&gt;.exe</c>); süreç başlatmayan birim testleri için.</summary>
public sealed class FixedToolLocator : IToolLocator
{
    /// <inheritdoc />
    public string? Find(ToolKind kind) => kind.ToString().ToLowerInvariant() + ".exe";

    /// <inheritdoc />
    public Result<string> Require(ToolKind kind) => Find(kind)!;
}
