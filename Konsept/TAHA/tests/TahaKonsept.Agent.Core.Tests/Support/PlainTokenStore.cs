using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Auth.Base;

namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>Şifrelemesiz token deposu (tabanın dosya mantığını DPAPI'siz test etmek için).</summary>
public sealed class PlainTokenStore : TokenStoreBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    public PlainTokenStore(IAgentPaths paths)
        : base(paths)
    {
    }

    /// <inheritdoc />
    protected override byte[] Protect(byte[] plain) => plain;

    /// <inheritdoc />
    protected override byte[] Unprotect(byte[] cipher) => cipher;
}
