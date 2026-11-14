using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// SHA-256 tabanlı aşama hash'leyici (varsayılan; singleton).
/// </summary>
public sealed class Sha256StageInputHasher : StageInputHasherBase
{
    /// <summary>SHA-256 özeti.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Özet.</returns>
    protected override string Digest(string text) => Sha256Hex(text);
}
