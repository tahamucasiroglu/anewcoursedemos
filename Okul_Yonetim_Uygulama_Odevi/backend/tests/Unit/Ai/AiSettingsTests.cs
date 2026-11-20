using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure;
using OkulYonetim.Infrastructure.Ai;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 AC-7 (ayar tarafı) ve C-2: varsayılan (Pi/üretim) ve örnek ayarlarda AI adresleri boştur; geliştirme ayarında
/// yalnızca <c>127.0.0.1</c> adresleri vardır. Yalnızca yerel adresler (localhost, geri döngü, özel ağ IPv4) kabul edilir;
/// boş ya da dış adres yazılırsa servis kaydı "kullanılamıyor" uygulamasıdır, yerel adreste gerçek adaptör kurulur.
/// </summary>
public sealed class AiSettingsTests
{
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Example.json")]
    public void DefaultSettings_AiAddressesEmpty(string file)
    {
        JsonElement ai = Ai(file);

        ai.GetProperty("TextGenerator").GetProperty("BaseUrl").GetString().ShouldBeNullOrEmpty();
        ai.GetProperty("SpeechToText").GetProperty("BaseUrl").GetString().ShouldBeNullOrEmpty();
    }

    [Fact]
    public void DevelopmentSettings_OnlyLoopbackAddresses()
    {
        JsonElement ai = Ai("appsettings.Development.json");

        new Uri(ai.GetProperty("TextGenerator").GetProperty("BaseUrl").GetString()!).Host.ShouldBe("127.0.0.1");
        ai.GetProperty("TextGenerator").GetProperty("Model").GetString().ShouldBe("gemma3:4b");
        new Uri(ai.GetProperty("SpeechToText").GetProperty("BaseUrl").GetString()!).Host.ShouldBe("127.0.0.1");
    }

    [Fact]
    public void AllSettingsFiles_NoNonLocalAiAddress()
    {
        foreach (string file in Directory.EnumerateFiles(WebApiFolder(), "appsettings*.json"))
        {
            using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(file));
            if (!settings.RootElement.TryGetProperty("Ai", out JsonElement ai))
            {
                continue;
            }

            foreach (string endpoint in new[] { "TextGenerator", "SpeechToText" })
            {
                string? url = ai.GetProperty(endpoint).GetProperty("BaseUrl").GetString();
                (string.IsNullOrEmpty(url) || AiOptions.IsLocalAddress(url)).ShouldBeTrue($"{Path.GetFileName(file)} {endpoint}: {url}");
            }
        }
    }

    [Theory]
    [InlineData("http://127.0.0.1:11434", true)]
    [InlineData("http://localhost:8765/", true)]
    [InlineData("http://192.168.1.50:11434", true)]
    [InlineData("http://10.0.0.7:8765", true)]
    [InlineData("http://172.16.4.2:8765", true)]
    [InlineData("http://172.32.0.1:8765", false)]
    [InlineData("http://8.8.8.8:11434", false)]
    [InlineData("https://api.openai.com/v1", false)]
    [InlineData("http://ollama.example.com:11434", false)]
    [InlineData("ftp://127.0.0.1", false)]
    [InlineData("127.0.0.1:11434", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalAddress_OnlyLocalOrPrivateNetwork(string? url, bool expected)
    {
        AiOptions.IsLocalAddress(url).ShouldBe(expected);
    }

    [Theory]
    [InlineData("", "gemma3:4b", "", typeof(UnavailableTextGenerator), typeof(UnavailableSpeechToText))]
    [InlineData("https://api.openai.com", "gemma3:4b", "https://speech.example.com", typeof(UnavailableTextGenerator), typeof(UnavailableSpeechToText))]
    [InlineData("http://127.0.0.1:11434", "", "http://127.0.0.1:8765", typeof(UnavailableTextGenerator), typeof(WhisperServiceSpeechToText))]
    [InlineData("http://127.0.0.1:11434", "gemma3:4b", "http://127.0.0.1:8765", typeof(OllamaTextGenerator), typeof(WhisperServiceSpeechToText))]
    public async Task Registration_UsesRealAdapterOnlyForLocalAddresses(string textUrl, string model, string speechUrl, Type text, Type speech)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:TextGenerator:BaseUrl"] = textUrl,
                ["Ai:TextGenerator:Model"] = model,
                ["Ai:SpeechToText:BaseUrl"] = speechUrl,
            })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IAiTextGenerator>().ShouldBeOfType(text);
        provider.GetRequiredService<ISpeechToText>().ShouldBeOfType(speech);
    }

    private static JsonElement Ai(string file)
    {
        using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(WebApiFolder(), file)));
        return settings.RootElement.GetProperty("Ai").Clone();
    }

    private static string WebApiFolder()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("backend/Directory.Build.props bulunamadı"), "src", "WebAPI");
    }
}
