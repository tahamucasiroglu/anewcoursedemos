using System.Globalization;

[assembly: AssemblyFixture(typeof(LeaveManager.Web.Tests.TurkishCultureFixture))]

namespace LeaveManager.Web.Tests;

/// <summary>Pins every test to tr-TR (docs/testing.md, Determinism).</summary>
public sealed class TurkishCultureFixture
{
    public TurkishCultureFixture()
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
