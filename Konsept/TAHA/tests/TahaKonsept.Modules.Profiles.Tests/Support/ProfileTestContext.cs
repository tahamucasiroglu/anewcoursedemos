using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Concrete;
using TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Concrete;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Tests.Support;

/// <summary>
/// Test düzeneği: EF InMemory <see cref="ProfilesDbContext"/>, ilerleyen sahte saat, değiştirilebilir oturum kullanıcısı
/// ve bunlarla kurulmuş <see cref="ProfileService"/> / <see cref="ProfileReader"/>. <see cref="Request"/> örnek istek üretir.
/// </summary>
public sealed class ProfileTestContext : IDisposable
{
    private DateTimeOffset _now = new(2026, 11, 14, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Düzeneği kurar (her örnek kendi veritabanını alır).</summary>
    public ProfileTestContext()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(_ => _now = _now.AddSeconds(1));
        CurrentUser = Substitute.For<ICurrentUser>();
        UseUser(UserA);

        var options = new DbContextOptionsBuilder<ProfilesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        Db = new ProfilesDbContext(options, clock);
        var repository = new ProfileRepository(Db);
        Service = new ProfileService(repository, new EmbeddedProfileTemplateCatalog(), CurrentUser);
        Reader = new ProfileReader(repository);
    }

    /// <summary>Birinci test kullanıcısı.</summary>
    public Guid UserA { get; } = Guid.CreateVersion7();

    /// <summary>İkinci test kullanıcısı.</summary>
    public Guid UserB { get; } = Guid.CreateVersion7();

    /// <summary>Sahte oturum kullanıcısı.</summary>
    public ICurrentUser CurrentUser { get; }

    /// <summary>DbContext.</summary>
    public ProfilesDbContext Db { get; }

    /// <summary>Test edilen servis.</summary>
    public ProfileService Service { get; }

    /// <summary>Test edilen okuyucu.</summary>
    public ProfileReader Reader { get; }

    /// <summary>Oturum kullanıcısını değiştirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    public void UseUser(Guid userId)
    {
        CurrentUser.UserId.Returns(userId);
        CurrentUser.GetRequiredUserId().Returns(userId);
        CurrentUser.IsAuthenticated.Returns(true);
    }

    /// <summary>Örnek kayıt isteği.</summary>
    /// <param name="name">Kanal adı.</param>
    /// <returns>İstek.</returns>
    public static SaveProfileRequest Request(string name) => new()
    {
        Name = name,
        Language = "tr",
        Format = VideoFormat.Vertical,
        TargetDurationSec = 60,
        ChannelDescription = name + " kanal içeriği",
    };

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => Db.Dispose();
}
