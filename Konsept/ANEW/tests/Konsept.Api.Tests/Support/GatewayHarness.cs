using System.Text.Json.Nodes;
using Konsept.Api.Tests.Fakes;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Concrete;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;
using Konsept.Modules.AiGateway.Infrastructure.Catalog;
using Konsept.Modules.AiGateway.Infrastructure.Security;
using Konsept.Shared.Kernel.Results;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Çağrı kapısı birim testleri için düzenek (plan 0013): sabit saat (<see cref="FakeTimeProvider"/>), bellek key ve harcama depoları,
/// geçici Data Protection anahtarlı gerçek key şifreleyici, sahte sağlayıcı ve gömülü katalog (istenirse değiştirilmiş). Gerçek
/// sağlayıcı çağrısı yoktur. <see cref="DriveAsync{T}"/> sahte saati çağrı bitene kadar adım adım ilerletir.
/// </summary>
public sealed class GatewayHarness
{
    /// <summary>Gets varsayılan çağıran (tüm modellere yetkili).</summary>
    public static readonly IReadOnlyCollection<string> AllModels = [ModelPermission.All];

    /// <summary>Initializes a new instance of the <see cref="GatewayHarness"/> class.</summary>
    public GatewayHarness()
    {
        Provider = new FakeProviderClient(Time);
        Protector = new DataProtectionApiKeyProtector(new EphemeralDataProtectionProvider());
    }

    /// <summary>Gets sabit saat.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Gets key deposu.</summary>
    public InMemoryApiKeyRepository Keys { get; } = new();

    /// <summary>Gets harcama deposu.</summary>
    public InMemorySpendRepository Spend { get; } = new();

    /// <summary>Gets sahte sağlayıcı.</summary>
    public FakeProviderClient Provider { get; }

    /// <summary>Gets key şifreleyici.</summary>
    public DataProtectionApiKeyProtector Protector { get; }

    /// <summary>Gömülü katalog, isteğe bağlı değişiklikle.</summary>
    /// <param name="mutate">JSON üzerinde değişiklik.</param>
    /// <returns>Katalog.</returns>
    public static CatalogSeed Seed(Action<JsonNode>? mutate = null)
    {
        var json = JsonNode.Parse(EmbeddedCatalogSource.ReadEmbeddedJson())!;
        mutate?.Invoke(json);
        return EmbeddedCatalogSource.Parse(json.ToJsonString());
    }

    /// <summary>Katalogdaki bir modelin JSON düğümü.</summary>
    /// <param name="seed">Katalog JSON'u.</param>
    /// <param name="id">Model kimliği.</param>
    /// <returns>Düğüm.</returns>
    public static JsonNode Model(JsonNode seed, string id) => seed["models"]!.AsArray().Single(m => (string?)m!["id"] == id)!;

    /// <summary>Kullanıcıya sağlayıcı key'i kaydeder.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="provider">Sağlayıcı.</param>
    /// <returns>Düz key.</returns>
    public string AddKey(Guid userId, string provider)
    {
        var key = "sk-test-" + Guid.NewGuid().ToString("N");
        Keys.UpsertAsync(
            new UserApiKey
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ProviderId = provider,
                Ciphertext = Protector.Protect(key),
                Hint = "sk-…" + key[^3..],
                Validation = KeyValidation.Verified,
                UpdatedAt = Time.GetUtcNow(),
            },
            CancellationToken.None).GetAwaiter().GetResult();
        return key;
    }

    /// <summary>Kapıyı kurar.</summary>
    /// <param name="seed">Katalog (verilmezse gömülü).</param>
    /// <param name="keys">Key deposu (verilmezse <see cref="Keys"/>).</param>
    /// <returns>Kapı.</returns>
    public AiCallGateway Gateway(CatalogSeed? seed = null, IApiKeyRepository? keys = null) =>
        new(new FixedCatalogSource(seed ?? Seed()), keys ?? Keys, Protector, Provider, Spend, Time, NullLogger<AiCallGateway>.Instance);

    /// <summary>Sonucu sahte saati ilerleterek bekler.</summary>
    /// <typeparam name="T">Sonuç tipi.</typeparam>
    /// <param name="task">Çağrı.</param>
    /// <param name="step">Her adımda ilerleme.</param>
    /// <returns>Sonuç.</returns>
    public async Task<T> DriveAsync<T>(Task<T> task, TimeSpan step)
    {
        var guard = 0;
        while (!task.IsCompleted)
        {
            await Task.Delay(2);
            Time.Advance(step);
            if (++guard > 5000)
            {
                throw new TimeoutException("Çağrı sahte saatle bitmedi.");
            }
        }

        return await task;
    }

    /// <summary>Kapıyı çağırır ve saati 250 ms adımlarla sürer.</summary>
    /// <param name="gateway">Kapı.</param>
    /// <param name="request">İstek.</param>
    /// <returns>Sonuç.</returns>
    public Task<Result<AiCallResult>> CallAsync(AiCallGateway gateway, AiCallRequest request) =>
        DriveAsync(gateway.CallAsync(request, CancellationToken.None), TimeSpan.FromMilliseconds(250));

    /// <summary>Metin isteği.</summary>
    /// <param name="userId">Çağıran.</param>
    /// <param name="modelId">Model.</param>
    /// <param name="permissions">Yetkiler (verilmezse tümü).</param>
    /// <returns>İstek.</returns>
    public static AiCallRequest Request(Guid userId, string modelId, IReadOnlyCollection<string>? permissions = null) => new()
    {
        Caller = new AiCaller(userId, permissions ?? AllModels),
        ModelId = modelId,
        Purpose = CallPurpose.ConceptGeneration,
        Prompt = "Kısa bir video konsepti öner.",
    };
}
