namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// İmzalı sürüm listesinin doğrulayıcısı (spec 0023 Req 6): Ed25519 imzası, ajana gömülü açık anahtarla denetlenir.
/// </summary>
public interface IManifestVerifier
{
    /// <summary>Gets a value indicating whether ajana bir açık anahtar gömülü (geliştirme derlemesinde yok → güncelleme kapalı).</summary>
    bool IsConfigured { get; }

    /// <summary>İmzayı doğrular.</summary>
    /// <param name="data">Liste baytları.</param>
    /// <param name="signature">İmza.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    bool Verify(byte[] data, byte[] signature);
}
