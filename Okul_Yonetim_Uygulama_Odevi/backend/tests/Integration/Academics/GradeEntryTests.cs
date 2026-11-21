using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-6: değerlendirmeyi açan öğretmen 0–100 tam sayı puanları tek seferde kaydeder, boş bırakılan "girilmedi" kalır;
/// -1, 101, ondalık ya da sayı olmayan değer o öğrenciyi (kimliğiyle) belirten alan hatası verir ve hiçbir puan kaydedilmez
/// (0 ve 100 kabul); kayıtlı puan düzeltilir ve silinir; liste "x / y puan girildi" sayısını doğru gösterir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GradeEntryTests
{
    private readonly TestDatabase database;

    public GradeEntryTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Save_ZeroHundredAndEmpty_CorrectAndClear_CountsRight()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid cem = await StudentAsync(client, w.Class, "Cem Er");
        Guid id = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        var (status, result, raw) = await SaveGradesAsync(client, w.Class.Teacher, id, (w.Ali, 0), (w.Ayse, "100"), (cem, null));
        status.ShouldBe(HttpStatusCode.OK, raw);
        result.GetProperty("gradedCount").GetInt32().ShouldBe(2);
        result.GetProperty("studentCount").GetInt32().ShouldBe(3);
        Dictionary<Guid, string?> scores = ByStudent((await GradesAsync(client, w.Class.Teacher, id)).Body, "score");
        scores[w.Ali].ShouldBe("0");
        scores[w.Ayse].ShouldBe("100");
        scores[cem].ShouldBeNull();
        JsonElement row = (await AssessmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body[0];
        row.GetProperty("gradedCount").GetInt32().ShouldBe(2);
        row.GetProperty("studentCount").GetInt32().ShouldBe(3);

        var (_, corrected, _) = await SaveGradesAsync(client, w.Class.Teacher, id, (w.Ali, 85), (w.Ayse, null), (cem, ""));
        corrected.GetProperty("gradedCount").GetInt32().ShouldBe(1);
        corrected.GetProperty("changed").GetInt32().ShouldBe(2);
        scores = ByStudent((await GradesAsync(client, w.Class.Teacher, id)).Body, "score");
        scores[w.Ali].ShouldBe("85");
        scores[w.Ayse].ShouldBeNull("silindi");
        (await AssessmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body[0].GetProperty("gradedCount").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(85.5)]
    [InlineData("abc")]
    [InlineData("85,5")]
    [InlineData("-1")]
    public async Task Save_OneInvalidScore_FieldErrorNamesStudent_NothingSaved(object invalid)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid id = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        var (status, problem, raw) = await SaveGradesAsync(client, w.Class.Teacher, id, (w.Ali, 90), (w.Ayse, invalid));

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain("Puan 0 ile 100 arasında tam sayı olmalı");
        raw.ShouldContain($"Grades[{w.Ayse:D}].Score");
        raw.ShouldNotContain(w.Ali.ToString("D"), customMessage: "yalnızca hatalı öğrenci belirtilir");
        ByStudent((await GradesAsync(client, w.Class.Teacher, id)).Body, "score").Values.ShouldAllBe(s => s == null, "hiçbir puan kaydedilmez");
    }
}
