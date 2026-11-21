using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NSubstitute;
using OkulYonetim.WebAPI.Errors;
using Shouldly;
using NArchitectureLogger = NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction.ILogger;

namespace OkulYonetim.Tests.Unit.Errors;

/// <summary>
/// T19 (spec 0012 review round 2, Düşük-2), şansa bağlı olmadan: birinci istek iş hatasında durum kodunu yazınca bekletilir
/// (<see cref="BlockingStatusResponseFeature"/>), bu arada ikinci istek başka bir beklenen hata (bulunamadı) ile tamamen
/// işlenir, sonra birinci bırakılır. Her yanıt kendi durum kodunu, JSON içerik türünü ve tam bir hata gövdesini taşır —
/// paylaşılan işleyicide birinci istek gövdesini ikinci isteğin yanıtına yazardı (biri iki gövde, öteki boş).
/// </summary>
public sealed class TraceIdExceptionMiddlewareTests
{
    private const string BusinessPath = "/is-hatasi";
    private const string BusinessMessage = "Birinci istek: iş hatası";
    private const string NotFoundMessage = "İkinci istek: kayıt bulunamadı";

    [Fact]
    public async Task ExpectedErrors_SecondRequestWhileFirstIsWriting_EachResponseOwnStatusAndSingleBody()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TraceIdExceptionMiddleware middleware = new(
            context => context.Request.Path == BusinessPath
                ? throw new BusinessException(BusinessMessage)
                : throw new NotFoundException(NotFoundMessage),
            new HttpContextAccessor(),
            Substitute.For<NArchitectureLogger>(),
            NullLogger<TraceIdExceptionMiddleware>.Instance);
        BlockingStatusResponseFeature blocking = new(StatusCodes.Status400BadRequest);
        DefaultHttpContext first = Context(BusinessPath, blocking);
        DefaultHttpContext second = Context("/bulunamadi", null);

        Task firstRun = Task.Run(() => middleware.Invoke(first), ct);
        await blocking.Entered.WaitAsync(BlockingStatusResponseFeature.Wait, ct);
        await middleware.Invoke(second);
        blocking.Release();
        await firstRun.WaitAsync(BlockingStatusResponseFeature.Wait, ct);

        first.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        first.Response.ContentType.ShouldBe(MediaTypeNames.Application.Json);
        Detail(Bodies(first).ShouldHaveSingleItem("birinci yanıtta tam bir gövde")).ShouldBe(BusinessMessage);
        second.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        second.Response.ContentType.ShouldBe(MediaTypeNames.Application.Json);
        Detail(Bodies(second).ShouldHaveSingleItem("ikinci yanıtta tam bir gövde")).ShouldBe(NotFoundMessage);
    }

    private static DefaultHttpContext Context(string path, IHttpResponseFeature? responseFeature)
    {
        DefaultHttpContext context = new();
        if (responseFeature is not null)
        {
            context.Features.Set(responseFeature);
        }

        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>Yanıt gövdesindeki üst düzey JSON değerleri (art arda yazılmış iki gövde iki değer, boş gövde sıfır).</summary>
    private static List<JsonElement> Bodies(DefaultHttpContext context)
    {
        byte[] bytes = ((MemoryStream)context.Response.Body).ToArray();
        List<JsonElement> values = [];
        Utf8JsonReader reader = new(bytes, new JsonReaderOptions { AllowMultipleValues = true });
        while (reader.Read())
        {
            values.Add(JsonElement.ParseValue(ref reader));
        }

        return values.Count > 0 || bytes.Length == 0
            ? values
            : throw new InvalidOperationException("JSON olmayan gövde: " + Encoding.UTF8.GetString(bytes));
    }

    private static string? Detail(JsonElement problem) =>
        problem.TryGetProperty("detail", out JsonElement detail) || problem.TryGetProperty("Detail", out detail) ? detail.GetString() : null;
}
