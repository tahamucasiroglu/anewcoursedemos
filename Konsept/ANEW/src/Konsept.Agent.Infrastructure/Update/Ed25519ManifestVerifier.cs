using Konsept.Agent.Application.Abstract;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Konsept.Agent.Infrastructure.Update;

/// <summary>
/// Ed25519 imza doğrulayıcı (spec 0023 Req 6): açık anahtar (32 bayt, base64) ajana yayında gömülür (<c>AgentUpdatePublicKey</c>
/// AssemblyMetadata); özel anahtar repoda ve Pi'de durmaz. Anahtar yoksa ya da geçersizse doğrulayıcı "yapılandırılmamış" sayılır ve
/// güncelleme denetimi yapılmaz — hiçbir koşulda imzasız liste kabul edilmez.
/// </summary>
public sealed class Ed25519ManifestVerifier : IManifestVerifier
{
    private const int SignatureSize = 64;
    private readonly Ed25519PublicKeyParameters? _key;

    /// <summary>Initializes a new instance of the <see cref="Ed25519ManifestVerifier"/> class.</summary>
    /// <param name="publicKeyBase64">Açık anahtar (base64, 32 bayt); yoksa <c>null</c>.</param>
    public Ed25519ManifestVerifier(string? publicKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(publicKeyBase64))
        {
            return;
        }

        try
        {
            var raw = Convert.FromBase64String(publicKeyBase64.Trim());
            _key = raw.Length == Ed25519PublicKeyParameters.KeySize ? new Ed25519PublicKeyParameters(raw, 0) : null;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            _key = null;
        }
    }

    /// <inheritdoc />
    public bool IsConfigured => _key is not null;

    /// <inheritdoc />
    public bool Verify(byte[] data, byte[] signature)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        if (_key is null || signature.Length != SignatureSize)
        {
            return false;
        }

        var signer = new Ed25519Signer();
        signer.Init(forSigning: false, _key);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.VerifySignature(signature);
    }
}
