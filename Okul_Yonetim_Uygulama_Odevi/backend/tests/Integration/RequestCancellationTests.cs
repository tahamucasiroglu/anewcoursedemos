using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration;

/// <summary>İstemcinin iptal ettiği istek hata sayılmaz (review round 1, L-1): Error yazılmaz, Information yazılır.</summary>
[Trait("Category", "Integration")]
public sealed class RequestCancellationTests
{
    private readonly TestDatabase database;

    public RequestCancellationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task ClientAbort_IsLoggedAsInformation_NotError()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, addThrowingEndpoint: true);
        using HttpClient client = factory.CreateClient();
        using CancellationTokenSource abort = new(TimeSpan.FromMilliseconds(300));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            client.GetAsync(new Uri(ThrowingEndpointStartupFilter.SlowPath, UriKind.Relative), abort.Token));

        string log = await WaitForAsync(factory, "İstek istemci tarafından iptal edildi");
        log.ShouldContain("[INF]");
        // İstek/hata hattından gelen Error satırı olmamalı (EF Core'un ilk şema kontrolü satırı bu testin konusu değil — T10).
        log.Split('\n')
            .ShouldNotContain(
                l => l.Contains("[ERR]", StringComparison.Ordinal)
                    && (l.Contains("TraceIdExceptionMiddleware", StringComparison.Ordinal) || l.Contains("RequestLoggingMiddleware", StringComparison.Ordinal)),
                "iptal hata sayılmamalı; log:" + Environment.NewLine + log);
    }

    private static async Task<string> WaitForAsync(OkulApiFactory factory, string text)
    {
        for (int i = 0; i < 50; i++)
        {
            string log = factory.ReadLog();
            if (log.Contains(text, StringComparison.Ordinal))
            {
                return log;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        string final = factory.ReadLog();
        final.ShouldContain(text);
        return final;
    }
}
