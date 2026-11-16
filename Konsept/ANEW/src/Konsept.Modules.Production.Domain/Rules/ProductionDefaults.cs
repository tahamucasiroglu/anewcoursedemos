using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// Hiç seçim yapılmamış profilin varsayılanları (spec 0006 Req 2, 7; AC-3): motor HyperFrames, ses Supertonic 3, görsel ajan Z-Image-Turbo'yu
/// kurulu bildirdiyse Z-Image-Turbo yoksa "Kapalı — koddan çiz" (sayfa açıldığı anda belirlenir), altyazı açık, ilerleme çubuğu kapalı,
/// video kodu modeli seçilmemiş (plan 0006 Risk 3). Remotion hiçbir profilde varsayılan değildir.
/// </summary>
public static class ProductionDefaults
{
    /// <summary>Varsayılan video motoru.</summary>
    public const string Engine = "local/hyperframes";

    /// <summary>Varsayılan ses modeli.</summary>
    public const string Audio = "local/supertonic-3";

    /// <summary>Kuruluysa varsayılan görsel modeli.</summary>
    public const string Image = "local/z-image-turbo";

    /// <summary>Görsel varsayılanını belirleyen ajan bileşeni.</summary>
    public const string ImageComponent = "z-image-turbo";

    /// <summary>Profilin varsayılan ayarları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="installedComponents">Ajanın kurulu bildirdiği bileşenler.</param>
    /// <returns>Ayarlar.</returns>
    public static ProductionSettings For(Guid userId, Guid profileId, IReadOnlySet<string> installedComponents)
    {
        ArgumentNullException.ThrowIfNull(installedComponents);
        return new ProductionSettings
        {
            UserId = userId,
            ProfileId = profileId,
            Subtitles = true,
            ProgressBar = false,
            Engine = Engine,
            AudioModel = Audio,
            ImageModel = installedComponents.Contains(ImageComponent) ? Image : null,
        };
    }
}
