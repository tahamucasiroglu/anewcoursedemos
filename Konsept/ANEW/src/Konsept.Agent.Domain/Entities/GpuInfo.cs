namespace Konsept.Agent.Domain.Entities;

/// <summary>Tespit edilen ekran kartı (spec 0028 Req 5): ad, üretici ve ayrılmış bellek. Birden çok kart varsa en çok belleği olan.</summary>
/// <param name="Name">Kartın adı.</param>
/// <param name="Vendor">Üretici (<see cref="Nvidia"/>, <c>amd</c>, <c>intel</c> ya da <c>other</c>).</param>
/// <param name="VramBytes">Ayrılmış bellek (bayt).</param>
public sealed record GpuInfo(string Name, string Vendor, long VramBytes)
{
    /// <summary>NVIDIA üretici adı.</summary>
    public const string Nvidia = "nvidia";

    /// <summary>Gets ayrılmış bellek (MB).</summary>
    public int VramMb => (int)Math.Min(int.MaxValue, VramBytes / (1024 * 1024));
}
