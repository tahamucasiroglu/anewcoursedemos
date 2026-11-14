using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>
/// Test ortamı yardımcıları: depo kökü (<c>TahaKonsept.slnx</c>'i arar), git dışı <c>.tools-dev/speech</c> klasörü
/// (canlı model ve portable uv), geçici klasör ve <see cref="SpeechEngineServices"/> üretimi.
/// </summary>
public static class TestEnvironment
{
    /// <summary>TAHA depo kökü.</summary>
    public static string RepositoryRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TahaKonsept.slnx")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new InvalidOperationException("TahaKonsept.slnx bulunamadı.");
        }
    }

    /// <summary>Git dışı geliştirme araçları klasörü: <c>.tools-dev/speech</c>.</summary>
    public static string SpeechToolsDir => Path.Combine(RepositoryRoot, ".tools-dev", "speech");

    /// <summary>Canlı testlerin model klasörü.</summary>
    public static string LiveModelsDir => Path.Combine(SpeechToolsDir, "models");

    /// <summary>Portable uv (yoksa <c>null</c>; TAHA_UV ortam değişkeni önceliklidir).</summary>
    public static string? UvPath
    {
        get
        {
            var fromEnv = Environment.GetEnvironmentVariable("TAHA_UV");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            {
                return fromEnv;
            }

            var local = Path.Combine(SpeechToolsDir, "uv", OperatingSystem.IsWindows() ? "uv.exe" : "uv");
            return File.Exists(local) ? local : null;
        }
    }

    /// <summary>Benzersiz geçici klasör oluşturur.</summary>
    /// <param name="prefix">Ön ek.</param>
    /// <returns>Yol.</returns>
    public static string NewTempDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), "taha-speech-tests", $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Servis kaydı üretir.</summary>
    /// <param name="paths">Klasörler.</param>
    /// <param name="uvPath">uv yolu (yoksa Require hata döner).</param>
    /// <param name="processes">Süreç çalıştırıcı (yoksa sahte).</param>
    /// <returns>Servisler.</returns>
    public static SpeechEngineServices CreateServices(TestAgentPaths paths, string? uvPath = null, IProcessRunner? processes = null)
    {
        var tools = Substitute.For<IToolLocator>();
        tools.Find(ToolKind.Uv).Returns(uvPath);
        tools.Find(ToolKind.Python).Returns((string?)null);
        tools.Require(ToolKind.Uv).Returns(uvPath is null
            ? Result.Failure<string>(Error.NotFound("tool.missing", "uv yok"))
            : Result.Success(uvPath));
        var http = Substitute.For<IHttpClientFactory>();
        http.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        return new SpeechEngineServices(paths, tools, processes ?? Substitute.For<IProcessRunner>(), http, NullLoggerFactory.Instance);
    }
}
