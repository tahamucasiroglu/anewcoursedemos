using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NArchitecture.Core.Localization.Abstraction;
using NSubstitute;
using OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;
using OkulYonetim.Application.Ports;
using Shouldly;

namespace OkulYonetim.Tests.Unit.SystemHealth;

/// <summary>Sistem durumu handler'ının birim testleri: çalışıyor, ulaşılamıyor ve 3 saniye zaman aşımı (spec 0001, AC-1/AC-2).</summary>
public sealed class GetSystemHealthQueryHandlerTests
{
    private readonly IDatabaseHealthProbe probe = Substitute.For<IDatabaseHealthProbe>();
    private readonly ILocalizationService localization = Substitute.For<ILocalizationService>();
    private readonly FakeTimeProvider time = new();

    public GetSystemHealthQueryHandlerTests()
    {
        this.localization.GetLocalizedAsync("Healthy", Arg.Any<ICollection<string>>(), "SystemHealth").Returns("Sistem çalışıyor");
        this.localization.GetLocalizedAsync("Unavailable", Arg.Any<ICollection<string>>(), "SystemHealth").Returns("Sistem şu anda hizmet dışı");
    }

    [Fact]
    public async Task Handle_DatabaseReachable_ReturnsHealthyWithTurkishMessage()
    {
        this.probe.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(true);

        SystemHealthResponse response = await this.CreateHandler().Handle(new GetSystemHealthQuery(), TestContext.Current.CancellationToken);

        response.Status.ShouldBe("healthy");
        response.Message.ShouldBe("Sistem çalışıyor");
    }

    [Fact]
    public async Task Handle_DatabaseUnreachable_ReturnsUnavailable()
    {
        this.probe.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(false);

        SystemHealthResponse response = await this.CreateHandler().Handle(new GetSystemHealthQuery(), TestContext.Current.CancellationToken);

        response.Status.ShouldBe("unavailable");
        response.Message.ShouldBe("Sistem şu anda hizmet dışı");
    }

    [Fact]
    public async Task Handle_ProbeTimesOut_ReturnsUnavailable()
    {
        TaskCompletionSource<bool> neverAnswers = new(TaskCreationOptions.RunContinuationsAsynchronously);
        this.probe.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(neverAnswers.Task);

        Task<SystemHealthResponse> pending = this.CreateHandler().Handle(new GetSystemHealthQuery(), TestContext.Current.CancellationToken);
        pending.IsCompleted.ShouldBeFalse();

        this.time.Advance(GetSystemHealthQueryHandler.DatabaseTimeout);
        SystemHealthResponse response = await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        response.Status.ShouldBe("unavailable");
    }

    [Fact]
    public async Task Handle_ProbeAnswersJustBeforeTimeout_ReturnsHealthy()
    {
        TaskCompletionSource<bool> slow = new(TaskCreationOptions.RunContinuationsAsynchronously);
        this.probe.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(slow.Task);

        Task<SystemHealthResponse> pending = this.CreateHandler().Handle(new GetSystemHealthQuery(), TestContext.Current.CancellationToken);
        this.time.Advance(GetSystemHealthQueryHandler.DatabaseTimeout - TimeSpan.FromMilliseconds(1));
        slow.SetResult(true);
        SystemHealthResponse response = await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        response.Status.ShouldBe("healthy");
    }

    private GetSystemHealthQueryHandler CreateHandler() =>
        new(this.probe, this.localization, this.time, NullLogger<GetSystemHealthQueryHandler>.Instance);
}
