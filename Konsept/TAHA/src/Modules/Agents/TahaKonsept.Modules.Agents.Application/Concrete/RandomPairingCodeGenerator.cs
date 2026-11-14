using System.Security.Cryptography;
using TahaKonsept.Modules.Agents.Application.Base;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Kriptografik rastgele (<see cref="RandomNumberGenerator"/>) eşleme kodu üretici.
/// </summary>
public sealed class RandomPairingCodeGenerator : PairingCodeGeneratorBase
{
    /// <summary>Rastgele bayt üretir.</summary>
    /// <param name="buffer">Tampon.</param>
    protected override void FillRandom(Span<byte> buffer) => RandomNumberGenerator.Fill(buffer);
}
