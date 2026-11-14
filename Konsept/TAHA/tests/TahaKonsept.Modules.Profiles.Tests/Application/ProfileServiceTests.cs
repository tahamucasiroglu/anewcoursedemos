using Microsoft.EntityFrameworkCore;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Profiles.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using Xunit;

namespace TahaKonsept.Modules.Profiles.Tests.Application;

/// <summary>
/// <c>ProfileService</c> testleri: kullanıcı başına tek aktif profil (oluşturma, aktifleştirme, silme), kullanıcı ayrımı, güncelleme ve okuyucu.
/// </summary>
public sealed class ProfileServiceTests : IDisposable
{
    private readonly ProfileTestContext _ctx = new();

    /// <summary>İlk profil aktif olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task First_profile_becomes_active()
    {
        var result = await _ctx.Service.CreateAsync(ProfileTestContext.Request("Bizans"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeTrue();
        (await ActiveCountAsync(_ctx.UserA)).ShouldBe(1);
    }

    /// <summary>Yeni profil aktif olur, öncekiler pasifleşir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Creating_new_profile_deactivates_previous()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Bizans"), ct)).Value;
        var second = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Osmanlı"), ct)).Value;

        var list = (await _ctx.Service.ListAsync(ct)).Value;
        list.Count.ShouldBe(2);
        list.Single(p => p.Id == second.Id).IsActive.ShouldBeTrue();
        list.Single(p => p.Id == first.Id).IsActive.ShouldBeFalse();
        (await ActiveCountAsync(_ctx.UserA)).ShouldBe(1);
    }

    /// <summary>Aktifleştirme diğer profilleri pasifleştirir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Activate_is_exclusive()
    {
        var ct = TestContext.Current.CancellationToken;
        var a = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("A"), ct)).Value;
        await _ctx.Service.CreateAsync(ProfileTestContext.Request("B"), ct);
        await _ctx.Service.CreateAsync(ProfileTestContext.Request("C"), ct);

        var result = await _ctx.Service.ActivateAsync(a.Id, ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeTrue();
        var actives = await _ctx.Db.Profiles.AsNoTracking().Where(p => p.UserId == _ctx.UserA && p.IsActive).ToListAsync(ct);
        actives.Count.ShouldBe(1);
        actives[0].Id.ShouldBe(a.Id);
    }

    /// <summary>Aktifleştirme başka kullanıcının aktif profiline dokunmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Activation_does_not_affect_other_users()
    {
        var ct = TestContext.Current.CancellationToken;
        _ctx.UseUser(_ctx.UserB);
        var bProfile = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("B kanalı"), ct)).Value;

        _ctx.UseUser(_ctx.UserA);
        var a1 = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("A1"), ct)).Value;
        await _ctx.Service.CreateAsync(ProfileTestContext.Request("A2"), ct);
        await _ctx.Service.ActivateAsync(a1.Id, ct);

        (await ActiveCountAsync(_ctx.UserA)).ShouldBe(1);
        (await ActiveCountAsync(_ctx.UserB)).ShouldBe(1);
        (await _ctx.Reader.GetActiveAsync(_ctx.UserB, ct))!.Id.ShouldBe(bProfile.Id);
        (await _ctx.Reader.GetActiveAsync(_ctx.UserA, ct))!.Id.ShouldBe(a1.Id);
    }

    /// <summary>Başka kullanıcının profili görülemez, aktifleştirilemez, silinemez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Other_users_profile_is_not_found()
    {
        var ct = TestContext.Current.CancellationToken;
        _ctx.UseUser(_ctx.UserB);
        var bProfile = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("B kanalı"), ct)).Value;

        _ctx.UseUser(_ctx.UserA);
        (await _ctx.Service.GetAsync(bProfile.Id, ct)).Error.Type.ShouldBe(ErrorType.NotFound);
        (await _ctx.Service.ActivateAsync(bProfile.Id, ct)).Error.Type.ShouldBe(ErrorType.NotFound);
        (await _ctx.Service.DeleteAsync(bProfile.Id, ct)).Error.Type.ShouldBe(ErrorType.NotFound);
        (await _ctx.Service.ListAsync(ct)).Value.ShouldBeEmpty();
    }

    /// <summary>Aktif profil silinince en yeni kalan profil aktifleşir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Deleting_active_profile_activates_newest_remaining()
    {
        var ct = TestContext.Current.CancellationToken;
        await _ctx.Service.CreateAsync(ProfileTestContext.Request("Eski"), ct);
        var middle = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Orta"), ct)).Value;
        var newest = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Yeni"), ct)).Value;

        (await _ctx.Service.DeleteAsync(newest.Id, ct)).IsSuccess.ShouldBeTrue();

        var active = await _ctx.Reader.GetActiveAsync(_ctx.UserA, ct);
        active.ShouldNotBeNull();
        active.Id.ShouldBe(middle.Id);
        (await ActiveCountAsync(_ctx.UserA)).ShouldBe(1);
    }

    /// <summary>Pasif profil silinince aktif profil değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Deleting_inactive_profile_keeps_active()
    {
        var ct = TestContext.Current.CancellationToken;
        var old = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Eski"), ct)).Value;
        var current = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Güncel"), ct)).Value;

        await _ctx.Service.DeleteAsync(old.Id, ct);

        (await _ctx.Reader.GetActiveAsync(_ctx.UserA, ct))!.Id.ShouldBe(current.Id);
        (await _ctx.Service.ListAsync(ct)).Value.Count.ShouldBe(1);
    }

    /// <summary>Güncelleme alanları normalleştirerek yazar ve aktifliği korur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Update_normalizes_and_keeps_activation()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = (await _ctx.Service.CreateAsync(ProfileTestContext.Request("Kanal"), ct)).Value;
        var request = ProfileTestContext.Request("  Yeni Ad  ") with
        {
            Language = "EN",
            Format = VideoFormat.Horizontal,
            TargetDurationSec = 240,
            DefaultTone = "   ",
            DefaultAudience = " öğrenciler ",
        };

        var result = await _ctx.Service.UpdateAsync(created.Id, request, ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Yeni Ad");
        result.Value.Language.ShouldBe("en");
        result.Value.Format.ShouldBe(VideoFormat.Horizontal);
        result.Value.TargetDurationSec.ShouldBe(240);
        result.Value.DefaultTone.ShouldBeNull();
        result.Value.DefaultAudience.ShouldBe("öğrenciler");
        result.Value.IsActive.ShouldBeTrue();
        (await _ctx.Reader.GetAsync(created.Id, ct))!.Name.ShouldBe("Yeni Ad");
    }

    /// <summary>Profil yoksa okuyucu null döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Reader_returns_null_when_no_profile()
    {
        var ct = TestContext.Current.CancellationToken;
        (await _ctx.Reader.GetActiveAsync(_ctx.UserA, ct)).ShouldBeNull();
        (await _ctx.Reader.GetAsync(Guid.CreateVersion7(), ct)).ShouldBeNull();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();

    private Task<int> ActiveCountAsync(Guid userId) =>
        _ctx.Db.Profiles.AsNoTracking().CountAsync(p => p.UserId == userId && p.IsActive, TestContext.Current.CancellationToken);
}
