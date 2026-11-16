namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Supertonic 3 lisansının istediği AI ses beyanı (spec 0017 Req 19, AC-22): video karesine gömülmez; videonun yanına not dosyası olarak
/// yazılır. Başka ses modeliyle ya da sessiz videoda not yoktur.
/// </summary>
public static class AiVoiceNotice
{
    /// <summary>Beyanı gerektiren ses modeli.</summary>
    public const string Model = "local/supertonic-3";

    /// <summary>Beyan metni.</summary>
    public const string Text = "Yapay zekâ ile üretilmiş ses: bu videonun seslendirmesi Supertonic 3 ile yapılmıştır.";

    /// <summary>Not gerekiyor mu.</summary>
    /// <param name="audioModel">Ses modeli.</param>
    /// <returns>Gerekiyorsa <c>true</c>.</returns>
    public static bool Applies(string? audioModel) => string.Equals(audioModel, Model, StringComparison.Ordinal);

    /// <summary>Videonun yanındaki not dosyasının yolu.</summary>
    /// <param name="videoPath">Video.</param>
    /// <returns>Not yolu.</returns>
    public static string FileFor(string videoPath) => Path.ChangeExtension(videoPath, ".ai-ses.txt");
}
