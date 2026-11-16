using System.Globalization;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Shouldly;

namespace Konsept.Modules.Identity.Tests;

/// <summary>
/// Rol ve yetki kuralları, veritabanısız (spec 0011 AC-1, AC-4 kural düzeyi): rol → yetki eşlemesi, ilk admin e-postası
/// karşılaştırması (tr-TR kültüründe) ve yeni hesabın varsayılan rolü.
/// </summary>
public sealed class RoleRulesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Admin rolü yalnızca <c>admin.panel</c>'i taşır; kullanıcı rolü hiçbir yetki taşımaz.</summary>
    [Fact]
    public void RolePermissions_AdminHasAdminPanel_UserHasNone()
    {
        RolePermissions.For(Role.Admin).ShouldBe(["admin.panel"]);
        RolePermissions.For(Role.User).ShouldBeEmpty();
        RolePermissions.For((Role)99).ShouldBeEmpty();
        Permissions.AdminPanel.ShouldBe("admin.panel");
    }

    /// <summary>
    /// Ayardaki e-posta büyük/küçük harften ve tr-TR kültüründen bağımsız eşleşir (0002 Req 2 kuralı: tr-TR'de
    /// <c>ToUpper()</c> "admin"i "ADMİN" yapardı); ayar boşsa ya da yalnızca boşluksa hiçbir e-posta admin değildir;
    /// başka bir e-posta asla eşleşmez.
    /// </summary>
    /// <param name="configured">Ayardaki e-posta.</param>
    /// <param name="email">Hesabın e-postası.</param>
    /// <param name="expected">Admin mi.</param>
    [Theory]
    [InlineData("admin@ornek.test", "admin@ornek.test", true)]
    [InlineData("admin@ornek.test", "ADMIN@ORNEK.TEST", true)]
    [InlineData("  Admin@Ornek.Test ", "admin@ornek.TEST", true)]
    [InlineData("ADMIN@ornek.test", "admin@ornek.test", true)]
    [InlineData("admin@ornek.test", "admın@ornek.test", false)]
    [InlineData("admin@ornek.test", "ADMİN@ornek.test", false)]
    [InlineData("admin@ornek.test", "baska@ornek.test", false)]
    [InlineData("admin@ornek.test", "admin@ornek.test.tr", false)]
    [InlineData("", "admin@ornek.test", false)]
    [InlineData("   ", "admin@ornek.test", false)]
    [InlineData(null, "admin@ornek.test", false)]
    [InlineData("", "", false)]
    public void AdminEmailPolicy_TrTr_CaseInsensitive(string? configured, string email, bool expected)
    {
        CultureInfo.CurrentCulture.Name.ShouldBe("tr-TR");

        AdminEmailPolicy.IsAdminEmail(configured, email).ShouldBe(expected);
    }

    /// <summary>Yeni hesap varsayılan olarak kullanıcı rolündedir ve yetki taşımaz.</summary>
    [Fact]
    public void Register_Default_RoleUser()
    {
        var user = User.Register("Ayşe", "ayse@ornek.test", "HASH", T0);

        user.Role.ShouldBe(Role.User);
        RolePermissions.For(user.Role).ShouldBeEmpty();
    }

    /// <summary>
    /// Senkron kuralı: eşleşen hesap admin olur, eşleşmeyen admin kullanıcıya döner, ayar boşsa (<c>null</c>) herkes
    /// kullanıcıdır; değişiklik yoksa <c>false</c> döner.
    /// </summary>
    [Fact]
    public void SyncAdminRole_PromotesMatching_DemotesOthers_EmptySettingNobody()
    {
        var match = User.Register("Admin", "Admin@Ornek.Test", "HASH", T0);
        var other = User.Register("Eski", "eski@ornek.test", "HASH", T0, Role.Admin);
        var normalized = AdminEmailPolicy.NormalizedAdminEmail("admin@ornek.test");

        match.SyncAdminRole(normalized).ShouldBeTrue();
        match.SyncAdminRole(normalized).ShouldBeFalse();
        other.SyncAdminRole(normalized).ShouldBeTrue();

        match.Role.ShouldBe(Role.Admin);
        other.Role.ShouldBe(Role.User);
        match.SyncAdminRole(AdminEmailPolicy.NormalizedAdminEmail(" ")).ShouldBeTrue();
        match.Role.ShouldBe(Role.User);
    }

    /// <summary>
    /// Spec 0013 AC-2: ayar yoksa her iki rol de tüm modellere yetkilidir (<c>model.use:*</c>); ayarla rol yalnızca listedeki modellere
    /// daraltılır; bilinmeyen rol hiçbir modele yetkili değildir. Model yetkileri rol yetkilerinden (<c>admin.panel</c>) ayrıdır.
    /// </summary>
    [Fact]
    public void RoleModelPermissions_DefaultAll_ConfiguredRestricts()
    {
        RoleModelPermissions.For(Role.User, [], []).ShouldBe(["model.use:*"]);
        RoleModelPermissions.For(Role.Admin, null, null).ShouldBe(["model.use:*"]);
        RoleModelPermissions.For(Role.User, ["openai/gpt-5-mini", "glm/glm-4.6"], []).ShouldBe(["model.use:openai/gpt-5-mini", "model.use:glm/glm-4.6"]);
        RoleModelPermissions.For(Role.Admin, ["openai/gpt-5-mini"], []).ShouldBe(["model.use:*"]);
        RoleModelPermissions.For((Role)99, [], []).ShouldBeEmpty();
        Permissions.ModelUseAll.ShouldBe("model.use:*");
    }
}
