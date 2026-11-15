using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.DependencyInjection;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Infrastructure.DependencyInjection;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Tests.Support;

/// <summary>
/// Identity modülünü gerçek DI kaydıyla (EF InMemory, her örnek kendi veritabanı) kuran test ana makinesi.
/// <see cref="Clock"/>, <see cref="CurrentUser"/>, <see cref="Notifier"/> ve <see cref="SigningKey"/> testten erişilebilir.
/// Yardımcılar: <see cref="RunAsync{TService, TResult}"/> (yeni scope'ta servis çağırır), <see cref="SetupAdminAsync"/> (Admin rolünde kullanıcı +
/// giriş), <see cref="CreateUserAsync"/>, <see cref="RegisterAsync"/>. Yönetici tohumu ayarı yapıcıdan verilebilir (varsayılan: yapılandırılmamış).
/// </summary>
public sealed class IdentityTestHost : IAsyncDisposable
{
    /// <summary>Test yöneticisi kullanıcı adı.</summary>
    public const string AdminUserName = "taha";

    /// <summary>Test parolası.</summary>
    public const string Password = "Parola123!";

    private readonly ServiceProvider _provider;

    /// <summary>Servisleri kurar.</summary>
    /// <param name="adminSeed">Yönetici tohumu ayarı (verilmezse boş: tohum atlanır).</param>
    public IdentityTestHost(AdminSeedOptions? adminSeed = null)
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
        services.AddSingleton(adminSeed ?? new AdminSeedOptions());

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

    /// <summary>Admin rolünde test yöneticisini oluşturur, giriş yapar ve token + kimliği döner.</summary>
    /// <returns>Token yanıtı ve kullanıcı kimliği.</returns>
    public async Task<(TokenResponse Tokens, Guid UserId)> SetupAdminAsync()
    {
        var id = await CreateUserAsync(AdminUserName);
        await RunAsync<RoleManager<AppRole>, bool>(async r => (await r.RoleExistsAsync(RoleNames.Admin)) || (await r.CreateAsync(new AppRole(RoleNames.Admin))).Succeeded);
        await RunAsync<UserManager<AppUser>, bool>(async m => (await m.AddToRoleAsync((await m.FindByIdAsync(id.ToString()))!, RoleNames.Admin)).Succeeded);
        var login = await RunAsync<IAuthService, BuildingBlocks.Domain.Results.Result<TokenResponse>>(
            s => s.LoginAsync(new LoginRequest { UserName = AdminUserName, Password = Password }));
        return login.IsSuccess ? (login.Value, id) : throw new InvalidOperationException(login.Error.Message);
    }

    /// <summary>Kayıt servisiyle hesap açar.</summary>
    /// <param name="userName">Kullanıcı adı.</param>
    /// <param name="password">Parola (verilmezse <see cref="Password"/>).</param>
    /// <returns>Sonuç.</returns>
    public Task<BuildingBlocks.Domain.Results.Result<TokenResponse>> RegisterAsync(string userName, string? password = null) =>
        RunAsync<IAuthService, BuildingBlocks.Domain.Results.Result<TokenResponse>>(
            s => s.RegisterAsync(new RegisterRequest { UserName = userName, Password = password ?? Password }));

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
