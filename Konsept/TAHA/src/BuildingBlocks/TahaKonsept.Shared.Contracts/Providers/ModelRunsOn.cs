namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Modelin çalıştığı yer: online sağlayıcılar sunucudan (Pi/API), yerel modeller masaüstü ajanında.
/// </summary>
public enum ModelRunsOn
{
    /// <summary>API sunucusundan çağrılan online sağlayıcı (API anahtarı gerekir).</summary>
    Server = 0,

    /// <summary>Kullanıcının bilgisayarında, masaüstü ajanında çalışır (ücretsiz, VRAM gerektirebilir).</summary>
    Agent = 1,
}
