using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.DependencyInjection;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Infrastructure.DependencyInjection;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Tests.Support;

/// <summary>
/// Identity modülünü gerçek DI kaydıyla (EF InMemory, her örnek kendi veritabanı) kuran test ana makinesi.
/// <see cref="Clock"/>, <see cref="CurrentUser"/>, <see cref="Notifier"/> ve <see cref="SigningKey"/> testten erişilebilir.
/// Yardımcılar: <see cref="RunAsync{TService, TResult}"/> (yeni scope'ta servis çağırır), <see cref="SetupAdminAsync"/>, <see cref="CreateUserAsync"/>.
/// </summary>
public sealed class IdentityTestHost : IAsyncDisposable
{
    /// <summary>Test yöneticisi kullanıcı adı.</summary>
    public const string AdminUserName = "taha";

    /// <summary>Test parolası.</summary>
    public const string Password = "Parola123!";

    private readonly ServiceProvider _provider;

    /// <summary>Servisleri kurar.</summary>
    public IdentityTestHost()
    {
        var keyProvider = Substitute.For<IJwtSigningKeyProvider>();
        keyProvider.GetSigningKey().Returns(SigningKey);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock>(Clock);
        services.AddSingleton(JwtOptions);
        services.AddSingleton(keyProvider);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<ISessionRevocationNotifier>(Notifier);

        var databaseName = "identity-tests-" + Guid.NewGuid().ToString("N");
        services.AddIdentityInfrastructure(new ConfigurationBuilder().Build(), options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityApplication();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    /// <summary>Test saati.</summary>
    public FakeClock Clock { get; } = new();

    /// <summary>Oturumdaki kullanıcı (logout-all, me).</summary>
    public FakeCurrentUser CurrentUser { get; } = new();

    /// <summary>Kaydedici bildirimci.</summary>
    public RecordingSessionRevocationNotifier Notifier { get; } = new();

    /// <summary>JWT ayarları.</summary>
    public JwtOptions JwtOptions { get; } = new();

    /// <summary>JWT imza anahtarı.</summary>
    public byte[] SigningKey { get; } = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    /// <summary>Yeni scope'ta bir servisi çağırır.</summary>
    /// <typeparam name="TService">Servis türü.</typeparam>
    /// <typeparam name="TResult">Sonuç türü.</typeparam>
    /// <param name="call">Çağrı.</param>
    /// <returns>Sonuç.</returns>
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> call)
        where TService : notnull
    {
        await using var scope = _provider.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Kurulumu yapar ve yönetici kimliğini döner.</summary>
    /// <returns>Token yanıtı ve kullanıcı kimliği.</returns>
    public async Task<(TokenResponse Tokens, Guid UserId)> SetupAdminAsync()
    {
        var result = await RunAsync<IAuthService, BuildingBlocks.Domain.Results.Result<TokenResponse>>(
            s => s.SetupAsync(new SetupRequest { UserName = AdminUserName, Password = Password }));
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        var id = await RunAsync<UserManager<AppUser>, Guid>(async m => (await m.FindByNameAsync(AdminUserName))!.Id);
        return (result.Value, id);
    }

    /// <summary>Yönetici olmayan ek kullanıcı oluşturur (yasak testleri için).</summary>
    /// <param name="userName">Kullanıcı adı.</param>
    /// <returns>Kullanıcı kimliği.</returns>
    public Task<Guid> CreateUserAsync(string userName) =>
        RunAsync<UserManager<AppUser>, Guid>(async m =>
        {
            var user = new AppUser(userName, Clock.UtcNow);
            var created = await m.CreateAsync(user, Password);
            return created.Succeeded ? user.Id : throw new InvalidOperationException(string.Join(", ", created.Errors.Select(e => e.Description)));
        });

    /// <summary>Servis sağlayıcıyı kapatır.</summary>
    /// <returns>Görev.</returns>
    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
