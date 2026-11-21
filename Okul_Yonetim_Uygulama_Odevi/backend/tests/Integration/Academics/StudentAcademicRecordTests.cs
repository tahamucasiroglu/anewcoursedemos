using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-10: öğrenci bazlı okuma (spec 0014'ün kullanacağı uçsuz sorgu), verilen öğrenci ve tarih aralığı için aralıktaki
/// ödevleri (teslim tarihine göre) öğrencinin durumuyla ve notları (değerlendirme gününe göre) puanıyla döndürür; aralık dışı
/// kayıt, aynı sınıfın başka öğrencisinin durumu/puanı ve başka okulun kaydı dönmez. Sorgu, oturumdaki okulun filtresiyle
/// gerçek depolar üzerinden çalıştırılır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentAcademicRecordTests
{
    private readonly TestDatabase database;

    public StudentAcademicRecordTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Record_RangeAndStudentOnly_OtherSchoolInvisible()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        CommunicationWorld other = await WorldAsync(factory, client);
        Guid inRange1 = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Fen", "Bugün", null, "2026-11-20"));
        Guid inRange2 = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Matematik", "Hafta sonu", null, "2026-11-30"));
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Türkçe", "Aralık dışı", null, "2026-12-01"));
        (await SaveSubmissionsAsync(client, w.Class.Teacher, inRange1, (w.Ali, "Submitted"), (w.Ayse, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.OK);
        (await SaveSubmissionsAsync(client, w.Class.Teacher, inRange2, (w.Ali, "NotSubmitted"), (w.Ayse, "Submitted"))).Status.ShouldBe(HttpStatusCode.OK);
        Guid oldExam = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam("Türkçe", "Written", "Aralık dışı sınav", "2026-11-10"));
        Guid exam = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam("Matematik", "Oral", "Sözlü", "2026-11-19"));
        (await SaveGradesAsync(client, w.Class.Teacher, oldExam, (w.Ali, 40), (w.Ayse, 41))).Status.ShouldBe(HttpStatusCode.OK);
        (await SaveGradesAsync(client, w.Class.Teacher, exam, (w.Ali, 70), (w.Ayse, 30))).Status.ShouldBe(HttpStatusCode.OK);
        await AssignedAsync(client, other.Class.Teacher, other.Class.ClassroomId, Homework("Fen", "Başka okul", null, "2026-11-20"));

        StudentAcademicRecord record = await ReadAsync(factory, w.Class.School.TenantId, w.Ali, new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 30));

        record.Assignments.Select(a => (a.AssignmentId, a.Title, a.DueDate, a.Status)).ShouldBe(
        [
            (inRange1, "Bugün", new DateOnly(2026, 11, 20), "Submitted"),
            (inRange2, "Hafta sonu", new DateOnly(2026, 11, 30), "NotSubmitted"),
        ]);
        record.Grades.Select(g => (g.AssessmentId, g.Subject, g.Kind, g.Title, g.Date, g.Score)).ShouldBe(
        [
            (exam, "Matematik", "Oral", "Sözlü", new DateOnly(2026, 11, 19), (int?)70),
        ]);

        StudentAcademicRecord crossTenant = await ReadAsync(factory, other.Class.School.TenantId, w.Ali, new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 22));
        crossTenant.Assignments.ShouldBeEmpty("başka okulun oturumu bu okulun kaydını göremez");
        crossTenant.Grades.ShouldBeEmpty();
        StudentAcademicRecord otherStudent = await ReadAsync(factory, w.Class.School.TenantId, other.Ali, new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 22));
        otherStudent.Assignments.ShouldBeEmpty("başka okulun öğrencisi");
        (await ReadAsync(factory, w.Class.School.TenantId, w.Ali, new DateOnly(2026, 11, 30), new DateOnly(2026, 11, 1))).Assignments.ShouldBeEmpty("ters aralık");
    }

    private static async Task<StudentAcademicRecord> ReadAsync(OkulApiFactory factory, Guid tenantId, Guid studentId, DateOnly from, DateOnly to)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        DbContextOptions<OkulDbContext> options = scope.ServiceProvider.GetRequiredService<DbContextOptions<OkulDbContext>>();
        await using OkulDbContext context = new(options, new RecordSession(tenantId));
        StudentAcademicRecordHandler handler = new(new AssignmentRepository(context), new AssessmentRepository(context));
        return await handler.Handle(new GetStudentAcademicRecordQuery(studentId, from, to), TestContext.Current.CancellationToken);
    }

    /// <summary>Okul filtresinin okuduğu oturum (kurucu görünürlüğü: okulun bütün şubeleri) — kişi oturumu olmayan okuma gibi.</summary>
    private sealed record RecordSession(Guid? TenantId) : ICurrentSession
    {
        public bool IsAuthenticated => true;

        public Guid? UserId => null;

        public Guid? SessionId => null;

        public bool IsFounder => true;

        public bool IsPlatformAdmin => false;

        public IReadOnlyCollection<Guid> VisibleBranchIds => [];
    }
}
