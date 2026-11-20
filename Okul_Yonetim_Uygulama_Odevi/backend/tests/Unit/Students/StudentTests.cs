using OkulYonetim.Domain.Students;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Students;

/// <summary>
/// Spec 0009 AC-3/AC-5 (varlık kuralları): öğrenci etkin oluşur, alanlar kırpılır; okul numarasının karşılaştırma biçimi harf
/// farkını yok sayar; boş numara tekillik dışında kalır; şube değişmez (yalnızca sınıf); ayrıldı/geri al kaydı silmez ve
/// denetim alanlarını günceller.
/// </summary>
public sealed class StudentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.CreateVersion7();

    [Fact]
    public void Create_TrimsFields_StartsActive_SetsAuditFields()
    {
        Guid tenant = Guid.CreateVersion7();
        Guid branch = Guid.CreateVersion7();

        Student student = Student.Create(tenant, branch, null, "  Ali Yılmaz ", new DateOnly(2015, 3, 1), " 12A ", Now, Actor);

        student.FullName.ShouldBe("Ali Yılmaz");
        student.SchoolNumber.ShouldBe("12A");
        student.SchoolNumberKey.ShouldBe("12a");
        student.IsActive.ShouldBeTrue();
        student.TenantId.ShouldBe(tenant);
        student.BranchId.ShouldBe(branch);
        student.ClassroomId.ShouldBeNull();
        student.HealthNoteCipher.ShouldBeNull();
        student.CreatedAt.ShouldBe(Now);
        student.CreatedBy.ShouldBe(Actor);
        student.UpdatedBy.ShouldBe(Actor);
    }

    [Theory]
    [InlineData("AB12", "ab12")]
    [InlineData("ab12", "ab12")]
    [InlineData(" Ab12 ", "ab12")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void NormalizeSchoolNumber_IgnoresCaseAndBlank(string? input, string? expected) =>
        Student.NormalizeSchoolNumber(input).ShouldBe(expected);

    [Fact]
    public void UpdateDetails_And_ChangeClassroom_KeepBranch()
    {
        Guid branch = Guid.CreateVersion7();
        Student student = Student.Create(Guid.CreateVersion7(), branch, null, "Ali", new DateOnly(2015, 3, 1), null, Now, Actor);
        Guid other = Guid.CreateVersion7();
        Guid classroom = Guid.CreateVersion7();

        student.UpdateDetails("Ali Can", new DateOnly(2015, 4, 2), "", Now.AddHours(1), other);
        student.ChangeClassroom(classroom, Now.AddHours(2), other);

        student.FullName.ShouldBe("Ali Can");
        student.BirthDate.ShouldBe(new DateOnly(2015, 4, 2));
        student.SchoolNumber.ShouldBeNull();
        student.SchoolNumberKey.ShouldBeNull();
        student.ClassroomId.ShouldBe(classroom);
        student.BranchId.ShouldBe(branch);
        student.UpdatedAt.ShouldBe(Now.AddHours(2));
        student.UpdatedBy.ShouldBe(other);
        student.CreatedBy.ShouldBe(Actor);

        student.ChangeClassroom(null, Now, other);
        student.ClassroomId.ShouldBeNull();
    }

    [Fact]
    public void MarkLeft_ThenReinstate_TogglesStatusOnly()
    {
        Student student = Student.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, "Ali", new DateOnly(2015, 3, 1), "7", Now, Actor);

        student.MarkLeft(Now.AddDays(1), Actor);
        student.IsActive.ShouldBeFalse();
        student.SchoolNumber.ShouldBe("7", "ayrılan öğrenci numarasını tutar (plan Risks 6)");

        student.Reinstate(Now.AddDays(2), Actor);
        student.IsActive.ShouldBeTrue();
        student.UpdatedAt.ShouldBe(Now.AddDays(2));
    }

    [Fact]
    public void SetHealth_StoresOnlyGivenCiphers()
    {
        Student student = Student.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, "Ali", new DateOnly(2015, 3, 1), null, Now, Actor);

        student.SetHealth("c1", null, Now, Actor);

        student.HealthNoteCipher.ShouldBe("c1");
        student.AllergyCipher.ShouldBeNull();
    }
}
