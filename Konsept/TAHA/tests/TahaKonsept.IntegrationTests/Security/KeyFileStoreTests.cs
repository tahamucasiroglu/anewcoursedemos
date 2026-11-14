using System.Security.AccessControl;
using Shouldly;
using TahaKonsept.BuildingBlocks.Infrastructure.Security;
using Xunit;

namespace TahaKonsept.IntegrationTests.Security;

/// <summary>
/// Anahtar dosyası: eşzamanlı ilk oluşturmada herkes aynı tam uzunlukta anahtarı alır (boş/yarım dosya okunmaz), geçici dosya kalmaz,
/// Windows'ta dosya ACL'i devralınmaz (yalnız geçerli kullanıcı).
/// </summary>
public sealed class KeyFileStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "tk-keys-" + Guid.NewGuid().ToString("N"));

    /// <summary>Paralel çağrılar aynı anahtarı alır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concurrent_first_creation_returns_same_full_key()
    {
        var path = Path.Combine(_dir, "keys", "master.key");

        var keys = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => KeyFileStore.GetOrCreateKey(path, 32), TestContext.Current.CancellationToken)));

        keys.ShouldAllBe(k => k.Length == 32);
        keys.Select(Convert.ToBase64String).Distinct().Count().ShouldBe(1);
        KeyFileStore.GetOrCreateKey(path, 32).ShouldBe(keys[0]);
        Directory.GetFiles(Path.GetDirectoryName(path)!).ShouldBe([path]);
    }

    /// <summary>Windows'ta anahtar dosyası devralınan izinleri taşımaz.</summary>
    [Fact]
    public void Key_file_acl_is_not_inherited_on_windows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = Path.Combine(_dir, "jwt.key");
        KeyFileStore.GetOrCreateKey(path, 64);

        var security = new FileInfo(path).GetAccessControl();
        security.AreAccessRulesProtected.ShouldBeTrue();
        security.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)).Count.ShouldBe(1);
    }

    /// <summary>Geçici klasörü siler.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
