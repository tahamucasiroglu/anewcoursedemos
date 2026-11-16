using System.Globalization;
using System.Text.RegularExpressions;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Modules.Monitoring.Domain.Rules;

/// <summary>
/// Aktivite türü etiketi (spec 0010 Req 5–6): olay kaydından yalnızca "ne tür işlem" çıkarılır. Olay metni, profil adı ve
/// başlık dışarı verilmez (BR-1); konsept üretiminde adet, kayıt metnindeki <c>n/m</c> sayısından okunan tek sayıdır.
/// </summary>
public static partial class ActivityKinds
{
    /// <summary>Bilinmeyen tür için etiket.</summary>
    public const string Other = "Diğer işlem";

    /// <summary>Olayın aktivite etiketini döndürür.</summary>
    /// <param name="type">Olay türü (kararlı kod).</param>
    /// <param name="level">Seviye.</param>
    /// <param name="endedAt">Süreli işlemin bitişi.</param>
    /// <param name="message">Olay metni (yalnızca adet için okunur, dönmez).</param>
    /// <returns>Türkçe etiket.</returns>
    public static string Describe(string type, EventLevel level, DateTimeOffset? endedAt, string? message)
    {
        var failed = level is EventLevel.Error or EventLevel.Critical;
        return type switch
        {
            "agent.connected" => "Ajan bağlandı",
            "agent.disconnected" => "Ajan bağlantısı koptu",
            "identity.registered" => "Kayıt oldu",
            "identity.login_succeeded" => "Giriş",
            "identity.logged_out" => "Çıkış",
            "identity.login_failed" => "Başarısız giriş",
            "identity.locked" => "Hesap geçici kilitlendi",
            "identity.agent_paired" => "Ajan eşleştirildi",
            "concepts.topics" => failed || level == EventLevel.Warning ? "Konu önerisi başarısız" : "Konu önerisi aldı",
            "concepts.generation" when failed => "Konsept üretimi başarısız",
            "concepts.generation" => CountOf(message) is { } count
                ? string.Format(CultureInfo.InvariantCulture, "Konsept üretti ({0})", count)
                : "Konsept üretti",
            "production.video" when failed => "Video üretimi hata verdi",
            "production.video" => endedAt is null ? "Video üretimi başladı" : "Video tamamlandı",
            "error_report.sent" => "Hata bildirdi",
            _ => Other,
        };
    }

    private static int? CountOf(string? message)
    {
        var match = message is null ? Match.Empty : CountPattern().Match(message);
        return match.Success && int.TryParse(match.Groups["n"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    [GeneratedRegex(@"(?<n>\d{1,6})/\d{1,6}", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex CountPattern();
}
