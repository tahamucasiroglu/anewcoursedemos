using LeaveManager.Application.Abstractions;
using LeaveManager.Application.Health;

namespace LeaveManager.Application.Tests.Health;

public class GetSystemHealthTests
{
    private sealed class FakeVersionProvider(string version) : IAppVersionProvider
    {
        public string GetVersion() => version;
    }

    [Fact]
    public void Execute_WhenCalled_ReturnsHealthyStatus()
    {
        var useCase = new GetSystemHealth(new FakeVersionProvider("1.2.3"));

        var health = useCase.Execute();

        Assert.Equal("healthy", health.Status);
    }

    [Fact]
    public void Execute_WhenCalled_ReturnsVersionFromPort()
    {
        var useCase = new GetSystemHealth(new FakeVersionProvider("1.2.3"));

        var health = useCase.Execute();

        Assert.Equal("1.2.3", health.Version);
    }

    [Fact]
    public void Execute_Result_CarriesOnlyStatusAndVersion()
    {
        var properties = typeof(SystemHealth).GetProperties().Select(p => p.Name).Order().ToArray();

        Assert.Equal(["Status", "Version"], properties);
    }

    [Fact]
    public void Constructor_WithNullPort_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GetSystemHealth(null!));
    }
}
