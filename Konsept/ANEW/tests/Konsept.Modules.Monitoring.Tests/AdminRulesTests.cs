using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Shouldly;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>Admin paneli kuralları (spec 0010 Req 4–8; AC-4, AC-6, AC-7).</summary>
public sealed class AdminRulesTests
{
    /// <summary>Aralık yalnızca 7 / 30 / 90; başka ya da boş değer varsayılan 30'dur.</summary>
    /// <param name="requested">İstenen.</param>
    /// <param name="expected">Beklenen.</param>
    [Theory]
    [InlineData(7, 7)]
    [InlineData(30, 30)]
    [InlineData(90, 90)]
    [InlineData(null, 30)]
    [InlineData(0, 30)]
    [InlineData(365, 30)]
    [InlineData(-7, 30)]
    public void AdminRange_ParseDays_Only7_30_90(int? requested, int expected) =>
        AdminRange.ParseDays(requested).ShouldBe(expected);

    /// <summary>Aralığın günleri bugünle biter, eskiden yeniye sıralıdır ve tam gün sayısı kadardır.</summary>
    [Fact]
    public void AdminRange_DaysEndingAt_TodayInclusive()
    {
        var days = AdminRange.DaysEndingAt(new DateOnly(2026, 11, 16), 7);

        days.Count.ShouldBe(7);
        days[0].ShouldBe(new DateOnly(2026, 11, 10));
        days[^1].ShouldBe(new DateOnly(2026, 11, 16));
    }

    /// <summary>Kullanıcı araması 2 karakterden kısaysa uygulanmaz; ad ya da e-postada büyük/küçük harf duyarsız "içerir"dir.</summary>
    [Fact]
    public void AdminRange_Search_MinTwoChars_CaseInsensitiveContains()
    {
        AdminRange.ParseSearch(null).ShouldBeNull();
        AdminRange.ParseSearch(" a ").ShouldBeNull();
        AdminRange.ParseSearch(" ay ").ShouldBe("ay");
        AdminRange.Matches("AYŞE", "Ayşe Yılmaz", "a@ornek.test").ShouldBeTrue();
        AdminRange.Matches("ORNEK.TE", "Ali", "ali@ornek.test").ShouldBeTrue();
        AdminRange.Matches("ılmaz", "Ayşe YILMAZ", "x@y.z").ShouldBeTrue();
        AdminRange.Matches("veli", "Ayşe", "ayse@ornek.test").ShouldBeFalse();
        AdminRange.Matches("VELI", "Veli Demir", "veli@baska.test").ShouldBeTrue(); // review r1 M-3: tr-TR'de I≠i.
        AdminRange.Matches("GMAIL", "Ali", "ali@gmail.com").ShouldBeTrue();
        AdminRange.ParseDate("9999-12-31").ShouldBe(new DateOnly(9998, 12, 31)); // review r1 L-1 / r2 L-3: sınıra çekilir, taşmaz.
        AdminRange.ParseDate("1999-12-31").ShouldBe(new DateOnly(2000, 1, 1)); // filtre anlamı korunur (yok sayılmaz).
        AdminRange.ParsePage(int.MaxValue).ShouldBe(AdminRange.MaxPage);
        AdminRange.ParsePage(0).ShouldBe(1);
        AdminRange.ParsePage(null).ShouldBe(1);
        AdminRange.ParseDate("2026-11-16").ShouldBe(new DateOnly(2026, 11, 16));
        AdminRange.ParseDate("16.11.2026").ShouldBeNull();
    }

    /// <summary>
    /// Etiket yalnızca türden gelir; olay metni ya da içindeki başlık dönmez. Konsept üretiminde yalnızca adet sayısı okunur;
    /// video olayı başladı / tamamlandı / hata ayrılır; bilinmeyen tür "Diğer işlem".
    /// </summary>
    /// <param name="type">Tür.</param>
    /// <param name="level">Seviye.</param>
    /// <param name="ended">Bitiş var mı.</param>
    /// <param name="message">Metin.</param>
    /// <param name="expected">Etiket.</param>
    [Theory]
    [InlineData("agent.connected", EventLevel.Info, false, "Ajan bağlandı: GIZLI-PC (v1)", "Ajan bağlandı")]
    [InlineData("agent.disconnected", EventLevel.Warning, false, "Ajan bağlantısı koptu: GIZLI-PC", "Ajan bağlantısı koptu")]
    [InlineData("identity.login_succeeded", EventLevel.Info, false, "Giriş yapıldı", "Giriş")]
    [InlineData("identity.logged_out", EventLevel.Info, false, "Çıkış", "Çıkış")]
    [InlineData("identity.login_failed", EventLevel.Warning, false, "x", "Başarısız giriş")]
    [InlineData("identity.agent_paired", EventLevel.Info, false, "x", "Ajan eşleştirildi")]
    [InlineData("concepts.generation", EventLevel.Info, true, "Konsept üretimi: 3/5 tamamlandı", "Konsept üretti (3)")]
    [InlineData("concepts.generation", EventLevel.Warning, true, "Konsept üretimi: 4/5 tamamlandı, 1 hata", "Konsept üretti (4)")]
    [InlineData("concepts.generation", EventLevel.Error, true, "Konsept üretimi başarısız: 5", "Konsept üretimi başarısız")]
    [InlineData("production.video", EventLevel.Info, false, "Video üretimi başladı: Gizli Başlık", "Video üretimi başladı")]
    [InlineData("production.video", EventLevel.Info, true, "Video hazırlandı: Gizli Başlık 12/3", "Video tamamlandı")]
    [InlineData("production.video", EventLevel.Error, true, "Video üretimi hata verdi: Gizli Başlık", "Video üretimi hata verdi")]
    [InlineData("error_report.sent", EventLevel.Info, false, "Hata bildirimi gönderildi", "Hata bildirdi")]
    [InlineData("bilinmeyen.tur", EventLevel.Info, false, "Gizli içerik", "Diğer işlem")]
    public void ActivityKinds_LabelFromTypeOnly(string type, EventLevel level, bool ended, string message, string expected)
    {
        var label = ActivityKinds.Describe(type, level, ended ? DateTimeOffset.UnixEpoch : null, message);

        label.ShouldBe(expected);
        label.ShouldNotContain("Gizli");
        label.ShouldNotContain("GIZLI");
    }
}
