using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>Ekran kartı tespiti (spec 0028 Req 5): en çok ayrılmış belleği olan kart; tespit edilemezse <c>null</c>. Gerçeği Windows kayıt defterinden.</summary>
public interface IGpuProbe
{
    /// <summary>Kartı tespit eder (istisna fırlatmaz).</summary>
    /// <returns>Kart ya da <c>null</c>.</returns>
    GpuInfo? Detect();
}
