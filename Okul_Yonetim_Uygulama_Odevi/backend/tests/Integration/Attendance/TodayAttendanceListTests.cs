using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-3: sınıfın kayıtlı öğrencileri Türkçe harf sırasıyla (C/Ç, I/İ, O/Ö) gelir; bugün alınmadıysa hepsi "Var" ve
/// "kaydedilmedi"; başka sınıfın öğrencisi ve "ayrıldı" yapılan öğrenci listede yok; öğrencisi olmayan sınıfta liste boş
/// (ekranda "Bu sınıfta kayıtlı öğrenci yok", kaydet kapalı).
/// </summary>
[Trait("Category", "Integration")]
public sealed class TodayAttendanceListTests
{
    private readonly TestDatabase database;

    public TodayAttendanceListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task List_EnrolledStudentsInTurkishOrder_AllPresent_OthersAndLeftExcluded()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        foreach (string name in new[] { "Zeynep Ak", "Ömer Can", "İsmail Er", "Çağla Su", "Ozan Tek", "Işık Ay", "Cem Uz" })
        {
            await StudentAsync(client, c, name);
        }

        Guid otherClass = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-B");
        await StudentAsync(client, c, "Başka Sınıf", otherClass);
        Guid left = await StudentAsync(client, c, "Ayrılan Öğrenci");
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{left}/deactivate", c.Founder)).ShouldBe(HttpStatusCode.OK);

        var (status, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);

        status.ShouldBe(HttpStatusCode.OK);
        view.GetProperty("classroomName").GetString().ShouldBe("5-A");
        view.GetProperty("branchName").GetString().ShouldBe("Merkez");
        view.GetProperty("day").GetString().ShouldBe(Today);
        view.GetProperty("isSchoolDay").GetBoolean().ShouldBeTrue();
        view.GetProperty("isSaved").GetBoolean().ShouldBeFalse();
        view.GetProperty("savedTime").ValueKind.ShouldBe(JsonValueKind.Null);
        Names(view).ShouldBe(["Cem Uz", "Çağla Su", "Işık Ay", "İsmail Er", "Ozan Tek", "Ömer Can", "Zeynep Ak"]);
        view.GetProperty("students").EnumerateArray().ShouldAllBe(s => s.GetProperty("status").GetString() == "Present");
    }

    [Fact]
    public async Task ClassWithoutStudents_ReturnsEmptyList()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);

        var (status, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);

        status.ShouldBe(HttpStatusCode.OK);
        view.GetProperty("isSchoolDay").GetBoolean().ShouldBeTrue();
        view.GetProperty("students").GetArrayLength().ShouldBe(0);
    }

    private static string[] Names(JsonElement view) => [.. view.GetProperty("students").EnumerateArray().Select(s => s.GetProperty("fullName").GetString()!)];
}
