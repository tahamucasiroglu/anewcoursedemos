using System.Net.Http.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Infrastructure.Persistence;
using Konsept.Modules.AiGateway.Infrastructure.Persistence.Concrete;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// AiGateway, gerçek PostgreSQL 17 ile (full profil): <c>ai_gateway.api_keys</c>'te düz key yok (AC-4), depo her işlemde kullanıcıyla
/// süzer (AC-9 — kasıtlı bozma bu testi kırar), eşzamanlı kayıt tek satır bırakır (AC-7). Sağlayıcı doğrulaması sahtedir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AiGatewayIntegrationTests : IClassFixture<IntegrationApiFactory>, IDisposable
{
    private readonly IntegrationApiFactory _database;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _keysDirectory = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "dp-int-" + Guid.NewGuid().ToString("N"));

    /// <summary>Initializes a new instance of the <see cref="AiGatewayIntegrationTests"/> class.</summary>
    /// <param name="database">Gerçek veritabanlı API fabrikası.</param>
    public AiGatewayIntegrationTests(IntegrationApiFactory database)
    {
        _database = database;
        _factory = database.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DataProtection:KeysDirectory", _keysDirectory);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IProviderKeyValidator>();
                services.AddSingleton<IProviderKeyValidator>(new FakeProviderKeyValidator());
            });
        });
    }

    /// <summary>AC-4: veritabanındaki satırda (şifreli değer + ipucu) girilen key'in hiçbir 8+ karakterlik parçası yoktur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ApiKeys_RealDatabase_NoPlainFragment()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var key = ApiKeyApiTests.NewKey("sk-ant-");

        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = key }, ct)).EnsureSuccessStatusCode();

        var userId = await UserIdAsync(email);
        var stored = (string)(await _database.Database.ScalarAsync(
            $"SELECT ciphertext || ' ' || hint || ' ' || validation FROM ai_gateway.api_keys WHERE user_id = '{userId}' AND provider_id = 'anthropic'"))!;
        stored.ShouldContain("verified");
        ApiKeyApiTests.ShouldNotLeak(key, stored);
    }

    /// <summary>
    /// AC-9 (kasıtlı bozma hedefi): gerçek depo yalnızca istenen kullanıcının satırlarını getirir ve siler; B'nin silmesi A'nın key'ini
    /// etkilemez, A'nın listesi B'nin ipucunu içermez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Repository_Ownership_RealDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        var emailA = AuthenticatedClient.NewEmail();
        var emailB = AuthenticatedClient.NewEmail();
        using var a = await AuthenticatedClient.CreateAsync(_factory, ct, emailA);
        using var b = await AuthenticatedClient.CreateAsync(_factory, ct, emailB);
        (await a.PutAsJsonAsync("/api/ai/keys/openai", new { apiKey = ApiKeyApiTests.NewKey("sk-") }, ct)).EnsureSuccessStatusCode();
        (await b.PutAsJsonAsync("/api/ai/keys/openai", new { apiKey = ApiKeyApiTests.NewKey("sk-") }, ct)).EnsureSuccessStatusCode();
        var idA = await UserIdAsync(emailA);
        var idB = await UserIdAsync(emailB);

        (await b.DeleteAsync(new Uri("/api/ai/keys/openai", UriKind.Relative), ct)).EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var repository = new ApiKeyRepository(scope.ServiceProvider.GetRequiredService<AiGatewayDbContext>());
        (await repository.ListAsync(idA, ct)).ShouldHaveSingleItem().UserId.ShouldBe(idA);
        (await repository.ListAsync(idB, ct)).ShouldBeEmpty();
        (await repository.DeleteAsync(idB, "openai", ct)).ShouldBeFalse();
        (await repository.ListAsync(idA, ct)).Count.ShouldBe(1);
    }

    /// <summary>AC-7/AC-14: aynı sağlayıcıya eşzamanlı iki kayıt tek satır bırakır (tekil indeks + tek ifadelik upsert).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConcurrentSave_SingleRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);

        var responses = await Task.WhenAll(
            client.PutAsJsonAsync("/api/ai/keys/kimi", new { apiKey = ApiKeyApiTests.NewKey("sk-") }, ct),
            client.PutAsJsonAsync("/api/ai/keys/kimi", new { apiKey = ApiKeyApiTests.NewKey("sk-") }, ct));

        responses.ShouldAllBe(r => r.IsSuccessStatusCode);
        var userId = await UserIdAsync(email);
        ((long)(await _database.Database.ScalarAsync($"SELECT count(*) FROM ai_gateway.api_keys WHERE user_id = '{userId}'"))!).ShouldBe(1);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _factory.Dispose();
        try
        {
            if (Directory.Exists(_keysDirectory))
            {
                Directory.Delete(_keysDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Geçici klasör.
        }
    }

    private async Task<Guid> UserIdAsync(string email) =>
        (Guid)(await _database.Database.ScalarAsync($"SELECT id FROM identity.users WHERE email = '{email}'"))!;
}
