using Konsept.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Üretimde konsol logu kapatılabilir (spec 0008 AC-5, review round 1 M-4, round 2 L-3): <c>Logging:Console=false</c>
/// iken host logu konsola düşmez; varsayılanda düşer. Konsol süreç genelinde olduğu için bu sınıf kendi koleksiyonunda
/// ve tek testte sırayla çalışır.
/// </summary>
[Collection(nameof(ConsoleLoggingTests))]
public sealed class ConsoleLoggingTests : IClassFixture<FastApiFactory>
{
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConsoleLoggingTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public ConsoleLoggingTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Bayrak kapalıyken işaretli log konsolda yoktur; açıkken (varsayılan) vardır.</summary>
    [Fact]
    public void ConsoleFlag_False_HostLogNotOnConsole()
    {
        var off = Capture("false");
        var on = Capture(null);

        off.Written.ShouldNotContain(off.Marker);
        on.Written.ShouldContain(on.Marker);
    }

    private (string Marker, string Written) Capture(string? flag)
    {
        var marker = Guid.NewGuid().ToString("N");
        var factory = _factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Logging:Directory", Path.Combine(_factory.LogDirectory, "console-" + marker));
            if (flag is not null)
            {
                b.UseSetting("Logging:Console", flag);
            }
        });
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            using var client = factory.CreateClient();
            var logger = factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Konsept.Api.Tests.Console");
#pragma warning disable CA1848 // Test.
            logger.LogWarning("konsol işareti {Marker}", marker);
#pragma warning restore CA1848
            return (marker, writer.ToString());
        }
        finally
        {
            Console.SetOut(original);
            factory.Dispose();
        }
    }
}
