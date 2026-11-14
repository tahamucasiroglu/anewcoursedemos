using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TahaKonsept.BuildingBlocks.Infrastructure.Security.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Catalog.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// Testlerde ortak nesneler: gerçek katalog, InMemory DbContext, sabit anahtarlı AES-GCM şifreleyici, model bağımlılıkları.
/// </summary>
public static class TestFixtures
{
    /// <summary>Gömülü katalog.</summary>
    public static EmbeddedModelCatalog Catalog { get; } = new();

    /// <summary>Katalogdan model.</summary>
    /// <param name="key">Anahtar.</param>
    public static ModelInfo Model(string key) => Catalog.Find(key) ?? throw new InvalidOperationException("Katalogda yok: " + key);

    /// <summary>Kimlik bilgisi.</summary>
    /// <param name="platform">Platform.</param>
    public static ProviderCredential Credential(string platform) => new(platform, "sk-test-1234567890", null);

    /// <summary>Model bağımlılıkları.</summary>
    /// <param name="recorder">Kaydedici.</param>
    public static AiModelDependencies Dependencies(RecordingCallRecorder recorder) => new(recorder, NullLogger.Instance);

    /// <summary>Sabit anahtarlı şifreleyici.</summary>
    public static AesGcmSecretProtector Protector() => new(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    /// <summary>Yeni InMemory DbContext.</summary>
    /// <param name="clock">Saat.</param>
    public static ProvidersDbContext Db(FixedClock? clock = null)
    {
        var options = new DbContextOptionsBuilder<ProvidersDbContext>()
            .UseInMemoryDatabase("providers-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ProvidersDbContext(options, clock ?? new FixedClock());
    }

    /// <summary>Basit metin isteği.</summary>
    /// <param name="maxTokens">Çıkış sınırı.</param>
    public static TextRequest Request(int maxTokens = 2000) => new()
    {
        Context = new CallContext(Guid.CreateVersion7(), "concept"),
        SystemPrompt = "Sen bir video konsepti yazarısın.",
        UserPrompt = "Bizans hakkında bir konsept yaz.",
        MaxOutputTokens = maxTokens,
    };
}
