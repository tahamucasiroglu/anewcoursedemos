using System.Collections;
using System.Globalization;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Common;

/// <summary>
/// Katkıcı sözlüklerindeki gevşek tipli değerleri okur: <see cref="ReadNumber(object)"/> (her sayısal tür/metin → decimal),
/// <see cref="ReadDailySeries"/> (sözlük ya da <see cref="DailyValue"/> listesi → verilen günleri boşluksuz kapsayan seri),
/// <see cref="ReadLevelCounts"/> (seviye sözlüğü → Information..Critical sayıları), <see cref="IsScalar"/> (Extra'ya uygun mu).
/// Okunamayan değer 0 / boş kabul edilir; hiçbir zaman istisna fırlatmaz.
/// </summary>
public static class StatsValueReader
{
    private static readonly LogLevelKind[] VisibleLevels =
        [LogLevelKind.Information, LogLevelKind.Warning, LogLevelKind.Error, LogLevelKind.Critical];

    /// <summary>Sayısal değeri okur.</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Sayı (okunamazsa 0).</returns>
    public static decimal ReadNumber(object? value)
    {
        try
        {
            return value switch
            {
                null => 0m,
                bool => 0m,
                string text => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m,
                IConvertible convertible => convertible.ToDecimal(CultureInfo.InvariantCulture),
                _ => 0m,
            };
        }
        catch (FormatException)
        {
            return 0m;
        }
        catch (InvalidCastException)
        {
            return 0m;
        }
        catch (OverflowException)
        {
            return 0m;
        }
    }

    /// <summary>Sözlükteki anahtarın sayısal değeri.</summary>
    /// <param name="stats">Birleşik istatistikler.</param>
    /// <param name="key">Anahtar.</param>
    /// <returns>Sayı (yoksa 0).</returns>
    public static decimal ReadNumber(IReadOnlyDictionary<string, object> stats, string key)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return stats.TryGetValue(key, out var value) ? ReadNumber(value) : 0m;
    }

    /// <summary>Sözlükteki anahtarın tam sayı değeri.</summary>
    /// <param name="stats">Birleşik istatistikler.</param>
    /// <param name="key">Anahtar.</param>
    /// <returns>Sayı (yoksa 0; int sınırına kırpılır).</returns>
    public static int ReadInt(IReadOnlyDictionary<string, object> stats, string key) =>
        (int)Math.Clamp(Math.Round(ReadNumber(stats, key)), int.MinValue, int.MaxValue);

    /// <summary>Günlük seriyi okur ve <paramref name="days"/> günlerini (eksikler 0) sırayla döndürür.</summary>
    /// <param name="value">Sözlük (gün → sayı) ya da <see cref="DailyValue"/> listesi.</param>
    /// <param name="days">Kapsanacak günler.</param>
    /// <returns>Seri.</returns>
    public static IReadOnlyList<DailyValue> ReadDailySeries(object? value, IReadOnlyList<DateOnly> days)
    {
        ArgumentNullException.ThrowIfNull(days);
        var map = new Dictionary<DateOnly, decimal>();
        switch (value)
        {
            case IEnumerable<DailyValue> points:
                foreach (var point in points)
                {
                    map[point.Date] = map.GetValueOrDefault(point.Date) + point.Value;
                }

                break;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (TryReadDate(entry.Key, out var date))
                    {
                        map[date] = map.GetValueOrDefault(date) + ReadNumber(entry.Value);
                    }
                }

                break;
        }

        return days.Select(d => new DailyValue { Date = d, Value = map.GetValueOrDefault(d) }).ToList();
    }

    /// <summary>Seviye sayılarını okur (Information, Warning, Error, Critical; eksikler 0).</summary>
    /// <param name="value">Sözlük (seviye ya da seviye adı → sayı).</param>
    /// <returns>Sayılar.</returns>
    public static IReadOnlyList<LevelCount> ReadLevelCounts(object? value)
    {
        var map = new Dictionary<LogLevelKind, int>();
        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                var level = entry.Key switch
                {
                    LogLevelKind kind => kind,
                    string name when Enum.TryParse<LogLevelKind>(name, true, out var parsed) => parsed,
                    _ => (LogLevelKind?)null,
                };
                if (level is { } l)
                {
                    map[l] = map.GetValueOrDefault(l) + (int)Math.Clamp(ReadNumber(entry.Value), 0, int.MaxValue);
                }
            }
        }

        return VisibleLevels.Select(l => new LevelCount { Level = l, Count = map.GetValueOrDefault(l) }).ToList();
    }

    /// <summary>Değer "Extra" alanına konabilecek basit bir değer mi (sayı, metin, bool, tarih).</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Basitse <c>true</c>.</returns>
    public static bool IsScalar(object? value) => value is string or bool or decimal or double or float or int or long or short or byte
        or uint or ulong or ushort or sbyte or DateTimeOffset or DateTime or DateOnly or Guid;

    private static bool TryReadDate(object? key, out DateOnly date)
    {
        switch (key)
        {
            case DateOnly d:
                date = d;
                return true;
            case DateTime dt:
                date = DateOnly.FromDateTime(dt);
                return true;
            case DateTimeOffset dto:
                date = DiagnosticsTime.LocalDate(dto);
                return true;
            case string text:
                return DateOnly.TryParse(text, CultureInfo.InvariantCulture, out date);
            default:
                date = default;
                return false;
        }
    }
}
