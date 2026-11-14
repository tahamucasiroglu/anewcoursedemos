using Shouldly;
using TahaKonsept.Modules.Profiles.Application.Validation;
using TahaKonsept.Modules.Profiles.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Profiles.Tests.Application;

/// <summary>
/// <see cref="SaveProfileRequestValidator"/> testleri.
/// </summary>
public sealed class SaveProfileRequestValidatorTests
{
    private readonly SaveProfileRequestValidator _validator = new();

    /// <summary>Geçerli istek geçer.</summary>
    [Fact]
    public void Valid_request_passes() => _validator.Validate(ProfileTestContext.Request("Kanal")).IsValid.ShouldBeTrue();

    /// <summary>Hatalı alanlar reddedilir.</summary>
    [Fact]
    public void Invalid_fields_fail()
    {
        var request = ProfileTestContext.Request(" ") with
        {
            Language = "türkçe",
            Format = (VideoFormat)9,
            TargetDurationSec = 5,
            ChannelDescription = new string('a', 4001),
        };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.PropertyName).Distinct().OrderBy(n => n, StringComparer.Ordinal).ShouldBe(
            ["ChannelDescription", "Format", "Language", "Name", "TargetDurationSec"]);
    }
}
