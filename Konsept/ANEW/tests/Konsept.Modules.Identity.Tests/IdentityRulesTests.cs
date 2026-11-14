using System.Globalization;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Rules;
using Shouldly;

namespace Konsept.Modules.Identity.Tests;

/// <summary>
/// Identity iş kuralları, veritabanısız (spec 0002 AC-2, AC-5, AC-7, AC-8, AC-13 kural düzeyi).
/// </summary>
public sealed class IdentityRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// E-posta karşılaştırması büyük/küçük harften ve tr-TR kültüründen etkilenmez: <c>IREM</c> ile <c>irem</c> aynıdır
    /// (tr-TR'de <c>ToUpper()</c> "İREM" verirdi). Kültürden bağımsız eşleme Türkçe'ye özgü <c>ı</c>/<c>İ</c> harflerini
    /// <c>I</c>'ya katlamaz; bunlar ayrı adres sayılır (plan A-2).
    /// </summary>
    /// <param name="left">Bir e-posta.</param>
    /// <param name="right">Öbürü.</param>
    /// <param name="same">Aynı hesap mı.</param>
    [Theory]
    [InlineData("irem@x.com", "IREM@X.COM", true)]
    [InlineData("IREM@x.com", "irem@x.com", true)]
    [InlineData("  irem@x.com ", "Irem@X.com", true)]
    [InlineData("ırem@x.com", "irem@x.com", false)]
    [InlineData("İrem@x.com", "irem@x.com", false)]
    [InlineData("istanbul@x.com", "ISTANBUL@x.com", true)]
    [InlineData("irem@x.com", "iremx@x.com", false)]
    public void EmailNormalizer_TrTrCulture_CaseInsensitive(string left, string right, bool same)
    {
        CultureInfo.CurrentCulture.Name.ShouldBe("tr-TR");

        (EmailNormalizer.Normalize(left) == EmailNormalizer.Normalize(right)).ShouldBe(same);
    }

    /// <summary>Yanlış şifre dizisi: 2 hak, 1 hak, sonra 15 dakika kilit ve sayaç sıfır.</summary>
    [Fact]
    public void Lockout_WrongSequence_TwoOneThenLocked15Min()
    {
        var first = LockoutPolicy.Evaluate(0, null, T0);
        var second = LockoutPolicy.Evaluate(first.FailedCount, first.LockedUntil, T0.AddSeconds(5));
        var third = LockoutPolicy.Evaluate(second.FailedCount, second.LockedUntil, T0.AddSeconds(10));

        LockoutPolicy.RemainingAttempts(first.FailedCount).ShouldBe(2);
        LockoutPolicy.RemainingAttempts(second.FailedCount).ShouldBe(1);
        third.Applied.ShouldBeTrue();
        third.FailedCount.ShouldBe(0);
        third.LockedUntil.ShouldBe(T0.AddSeconds(10).AddMinutes(15));
        LockoutPolicy.IsLocked(third.LockedUntil, T0.AddSeconds(10)).ShouldBeTrue();
    }

    /// <summary>Kilitliyken deneme sayılmaz ve süre uzamaz; süre dolunca yeniden 3 hak (ilk hata "2 hak").</summary>
    [Fact]
    public void Lockout_WhileLocked_NotCountedNotExtended_FreshThreeAfterExpiry()
    {
        var lockedUntil = T0.AddMinutes(15);

        var during = LockoutPolicy.Evaluate(0, lockedUntil, T0.AddMinutes(14));
        var after = LockoutPolicy.Evaluate(0, lockedUntil, lockedUntil.AddSeconds(1));

        during.Applied.ShouldBeFalse();
        during.LockedUntil.ShouldBe(lockedUntil);
        during.FailedCount.ShouldBe(0);
        after.Applied.ShouldBeTrue();
        LockoutPolicy.RemainingAttempts(after.FailedCount).ShouldBe(2);
    }

    /// <summary>Kilitli kullanıcıda başarılı giriş uygulanmaz; 2 hatadan sonra başarılı giriş sayacı sıfırlar.</summary>
    [Fact]
    public void User_SuccessAfterTwoFailures_ResetsCounter_LockedRejectsSuccess()
    {
        var user = User.Register("Ayşe", "ayse@x.com", "hash", T0);
        user.ApplyFailedAttempt(T0);
        user.ApplyFailedAttempt(T0);

        user.ApplySuccessfulLogin(T0.AddSeconds(1)).ShouldBeTrue();
        user.FailedLoginCount.ShouldBe(0);
        user.LastLoginAt.ShouldBe(T0.AddSeconds(1));
        LockoutPolicy.RemainingAttempts(user.ApplyFailedAttempt(T0.AddSeconds(2)).FailedCount).ShouldBe(2);

        user.ApplyFailedAttempt(T0.AddSeconds(3));
        user.ApplyFailedAttempt(T0.AddSeconds(4));
        user.IsLocked(T0.AddSeconds(5)).ShouldBeTrue();
        user.ApplySuccessfulLogin(T0.AddSeconds(5)).ShouldBeFalse();
        user.LastLoginAt.ShouldBe(T0.AddSeconds(1));
    }

    /// <summary>Oturum son kullanımdan itibaren 14 gün geçerli; kullanıldıkça kayar; iptal edilen oturum geçersiz.</summary>
    [Fact]
    public void Session_SlidingExpiry_14Days()
    {
        var session = UserSession.Open(Guid.NewGuid(), "h", T0);

        session.IsActive(T0.AddDays(14)).ShouldBeTrue();
        session.IsActive(T0.AddDays(14).AddSeconds(1)).ShouldBeFalse();
        session.Touch(T0.AddDays(13));
        session.IsActive(T0.AddDays(26)).ShouldBeTrue();
        SessionPolicy.ShouldTouch(T0, T0.AddSeconds(59)).ShouldBeFalse();
        SessionPolicy.ShouldTouch(T0, T0.AddMinutes(1)).ShouldBeTrue();
        session.Revoke(T0.AddDays(13).AddHours(1));
        session.IsActive(T0.AddDays(13).AddHours(2)).ShouldBeFalse();
    }
}
