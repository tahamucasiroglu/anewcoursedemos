using Konsept.Modules.Identity.Application.Base;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Admin özeti iskeleti: henüz bölüm yok (spec 0011 Req 4).</summary>
public sealed class AdminOverviewService : AdminOverviewServiceBase
{
    /// <inheritdoc />
    protected override IReadOnlyList<string> Sections() => [];
}
