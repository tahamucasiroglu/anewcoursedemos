using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Konsept.Agent.Tests;
using Konsept.Testing;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan arayüzünü pencere açmadan (headless) çalıştırır; kültür <c>tr-TR</c>.
/// </summary>
public static class TestAppBuilder
{
    /// <summary>Headless Avalonia uygulamasını kurar.</summary>
    /// <returns>Uygulama kurucusu.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    /// <summary>Modül başlatıcısı.</summary>
    [ModuleInitializer]
    internal static void Initialize() => TestCulture.Apply();
}
