using Shouldly;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Concrete;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Application;

/// <summary>
/// <see cref="RandomPairingCodeGenerator"/>: kod alfabesi/uzunluğu, normalize + biçim, deterministik hash, benzersiz cihaz kodu.
/// </summary>
public sealed class PairingCodeGeneratorTests
{
    private readonly RandomPairingCodeGenerator _codes = new();

    /// <summary>Kullanıcı kodu 8 karakter ve alfabe içinde.</summary>
    [Fact]
    public void User_code_uses_alphabet()
    {
        for (var i = 0; i < 50; i++)
        {
            var code = _codes.CreateUserCode();
            code.Length.ShouldBe(PairingCodeGeneratorBase.UserCodeLength);
            code.All(c => PairingCodeGeneratorBase.UserCodeAlphabet.Contains(c, StringComparison.Ordinal)).ShouldBeTrue();
        }
    }

    /// <summary>Normalize tire/boşluk atar ve büyütür; biçim tire ekler.</summary>
    [Fact]
    public void Normalizes_and_formats()
    {
        _codes.NormalizeUserCode(" abcd-23ef ").ShouldBe("ABCD23EF");
        _codes.FormatUserCode("ABCD23EF").ShouldBe("ABCD-23EF");
    }

    /// <summary>Hash deterministik; cihaz kodları farklı.</summary>
    [Fact]
    public void Hash_is_deterministic_and_device_codes_unique()
    {
        var a = _codes.CreateDeviceCode();
        var b = _codes.CreateDeviceCode();

        a.ShouldNotBe(b);
        _codes.HashDeviceCode(a).ShouldBe(_codes.HashDeviceCode(a));
        _codes.HashDeviceCode(a).Length.ShouldBe(64);
    }
}
