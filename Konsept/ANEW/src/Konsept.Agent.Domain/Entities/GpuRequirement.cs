namespace Konsept.Agent.Domain.Entities;

/// <summary>Bileşenin ekran kartı gereksinimi (spec 0028 Req 6): en az bu kadar ayrılmış ekran kartı belleği; yoksa model desteklenmez.</summary>
/// <param name="MinVramMb">En az ayrılmış bellek (MB).</param>
public sealed record GpuRequirement(int MinVramMb);
