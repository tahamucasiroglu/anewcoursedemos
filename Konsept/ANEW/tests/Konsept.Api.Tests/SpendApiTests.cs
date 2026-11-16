using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Aylık harcama ucu (spec 0013 AC-14, AC-13 son kısmı): kullanıcının bu takvim ayındaki (İstanbul) tüm profillerine ait kayıtların
/// yuvarlanmamış toplamı; önceki ay ve başka kullanıcı girmez; hiç kayıt yoksa 0; key silinse de geçmiş kayıt toplama girer.
/// </summary>
public sealed class SpendApiTests : IClassFixture<AiGatewayApiFactory>
{
    private readonly AiGatewayApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="SpendApiTests"/> class.</summary>
    /// <param name="factory">AiGateway test fabrikası.</param>
    public SpendApiTests(AiGatewayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>AC-14: yalnızca kendi ve bu ayın kayıtları, yuvarlanmadan toplanır (1,234567 + 1,131 = 2,365567).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SpendApi_MonthTotal_OnlyOwnCurrentMonth()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await _factory.CreateUserClientAsync(ct, email);
        var userId = _factory.Users.Get(email).Id;
        var (start, _) = IstanbulMonth.Of(DateTimeOffset.UtcNow);

        (await client.GetFromJsonAsync<JsonElement>("/api/ai/spend/month", ct)).GetProperty("totalUsd").GetDecimal().ShouldBe(0m);

        await AddAsync(userId, start, 1.234567m, Guid.CreateVersion7());
        await AddAsync(userId, DateTimeOffset.UtcNow, 1.131m, Guid.CreateVersion7());
        await AddAsync(userId, start.AddSeconds(-1), 5m, null);
        await AddAsync(Guid.CreateVersion7(), DateTimeOffset.UtcNow, 7m, null);
        var month = await client.GetFromJsonAsync<JsonElement>("/api/ai/spend/month", ct);

        month.GetProperty("totalUsd").GetDecimal().ShouldBe(2.365567m);
        month.GetProperty("monthStartUtc").GetDateTimeOffset().ShouldBe(start);
    }

    private Task AddAsync(Guid userId, DateTimeOffset at, decimal cost, Guid? profile) =>
        _factory.Spend.AddAsync(
            new SpendRecord
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                OccurredAt = at,
                ProviderId = "anthropic",
                ModelId = "anthropic/claude-sonnet-4-5",
                ProfileId = profile,
                CostUsd = cost,
            },
            CancellationToken.None);
}
