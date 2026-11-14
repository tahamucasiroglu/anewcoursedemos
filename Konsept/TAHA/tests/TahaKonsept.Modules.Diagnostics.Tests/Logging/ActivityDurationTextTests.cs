using Shouldly;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using Xunit;

namespace TahaKonsept.Modules.Diagnostics.Tests.Logging;

/// <summary>
/// <see cref="ActivityDurationText.Format"/> kısa Türkçe süre metinleri.
/// </summary>
public sealed class ActivityDurationTextTests
{
    /// <summary>Saniye / dakika / saat biçimleri.</summary>
    /// <param name="seconds">Süre (sn).</param>
    /// <param name="expected">Beklenen metin.</param>
    [Theory]
    [InlineData(0.25, "250 ms")]
    [InlineData(57, "57 sn")]
    [InlineData(180, "3 dk")]
    [InlineData(192, "3 dk 12 sn")]
    [InlineData(3600, "1 sa")]
    [InlineData(3900, "1 sa 5 dk")]
    [InlineData(-5, "0 ms")]
    public void Formats_durations(double seconds, string expected)
    {
        ActivityDurationText.Format(TimeSpan.FromSeconds(seconds)).ShouldBe(expected);
    }
}
