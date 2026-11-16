namespace Konsept.Modules.AiGateway.Domain.Rules;

/// <summary>
/// Aylık harcamanın ay sınırı (spec 0013 Req 16): <c>Europe/Istanbul</c> takvim ayı; kayıtlar UTC'dir. Verilen anın İstanbul'daki
/// ayının ilk anı (dahil) ile sonraki ayın ilk anı (hariç) UTC olarak döner.
/// </summary>
public static class IstanbulMonth
{
    /// <summary>Saat dilimi kimliği (IANA).</summary>
    public const string TimeZoneId = "Europe/Istanbul";

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    /// <summary>Ayın UTC aralığı.</summary>
    /// <param name="now">Şimdiki an.</param>
    /// <returns>[başlangıç, bitiş) UTC.</returns>
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) Of(DateTimeOffset now)
    {
        var local = TimeZoneInfo.ConvertTime(now, Zone);
        var start = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return (ToUtc(start), ToUtc(start.AddMonths(1)));
    }

    private static DateTimeOffset ToUtc(DateTime local) => new(TimeZoneInfo.ConvertTimeToUtc(local, Zone), TimeSpan.Zero);
}
