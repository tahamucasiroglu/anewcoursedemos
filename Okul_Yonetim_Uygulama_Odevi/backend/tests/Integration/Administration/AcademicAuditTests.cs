using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Spec 0008 AC-11: seviye, dönem, sınıf ve takvim kaydındaki her değişiklik audit'te (kim, ne, hangi kayıt, ne zaman,
/// IP); reddedilen işlem başarı olarak yazılmaz; aynı adla iki sınıf ya da çakışan iki dönem aynı anda eklenirse yalnızca
/// biri oluşur, öbürü kural mesajıyla reddedilir (okul başına sıra).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicAuditTests
{
    private readonly TestDatabase database;

    public AcademicAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task EveryAcademicChange_IsAudited_RejectedOnesAreNot()
    {
        DateTimeOffset now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: new FakeTimeProvider(now));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.77");
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);

        Guid level = await CreateGradeLevelAsync(client, token, "Hazırlık", 1);
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/grade-levels/{level}", token, new { name = "Hazırlık sınıfı", sortOrder = 1 });
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/grade-levels/{level}/deactivate", token);
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/grade-levels/{level}/activate", token);
        Guid term = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/terms/{term}", token, new { name = "Güz dönemi", startDate = "2026-09-01", endDate = "2027-01-22" });
        Guid classroom = await CreateClassroomAsync(client, token, term, a.BranchId, level, "H-A");
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{classroom}", token, new { gradeLevelId = level, name = "H-B" });
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/classrooms/{classroom}/deactivate", token);
        await StatusAsync(client, HttpMethod.Post, $"/api/admin/classrooms/{classroom}/activate", token);
        Guid entry = await CreateCalendarEntryAsync(client, token, term, "Tatil", "Holiday", "2026-10-29", "2026-10-29");
        await StatusAsync(client, HttpMethod.Put, $"/api/admin/calendar-entries/{entry}", token, new { title = "Bayram", kind = "Holiday", startDate = "2026-10-29", endDate = "2026-10-30" });
        await StatusAsync(client, HttpMethod.Delete, $"/api/admin/calendar-entries/{entry}", token);

        (await StatusAsync(client, HttpMethod.Post, "/api/admin/grade-levels", token, new { name = "HAZIRLIK SINIFI", sortOrder = 2 })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/terms", token, new { name = "Çakışan", startDate = "2027-01-01", endDate = "2027-02-01" })).ShouldBe(HttpStatusCode.BadRequest);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/classrooms", token, new { termId = term, branchId = a.BranchId, gradeLevelId = level, name = "h-b" })).ShouldBe(HttpStatusCode.BadRequest);

        AuditAction[] academic =
        [
            AuditAction.GradeLevelCreated, AuditAction.GradeLevelUpdated, AuditAction.GradeLevelDeactivated, AuditAction.GradeLevelActivated,
            AuditAction.TermCreated, AuditAction.TermUpdated,
            AuditAction.ClassroomCreated, AuditAction.ClassroomUpdated, AuditAction.ClassroomDeactivated, AuditAction.ClassroomActivated,
            AuditAction.CalendarEntryCreated, AuditAction.CalendarEntryUpdated, AuditAction.CalendarEntryDeleted,
        ];
        List<AuditEntry> entries = [.. (await QueryAsync(factory, c => c.AuditEntries.Where(e => e.TenantId == a.TenantId).ToListAsync())).Where(e => academic.Contains(e.Action))];
        entries.Select(e => e.Action).ShouldBe(academic, ignoreOrder: true, "her işlem bir kez; reddedilenler yazılmaz");
        entries.ShouldAllBe(e => e.ActorUserId == a.FounderId && e.IpAddress == "203.0.113.77" && e.OccurredAt == now && e.TargetId != null);
        entries.Single(e => e.Action == AuditAction.GradeLevelCreated).TargetId.ShouldBe(level);
        entries.Single(e => e.Action == AuditAction.TermUpdated).TargetId.ShouldBe(term);
        entries.Single(e => e.Action == AuditAction.ClassroomActivated).TargetId.ShouldBe(classroom);
        entries.Single(e => e.Action == AuditAction.CalendarEntryDeleted).TargetId.ShouldBe(entry);
        entries.Select(e => e.TargetType).Distinct().ShouldBe(["GradeLevel", "Term", "Classroom", "CalendarEntry"], ignoreOrder: true);
    }

    /// <summary>
    /// Review L-1: iki isteğin kural kontrolü kapıyla (<see cref="RaceGate"/>) kesin olarak çakıştırılır. İlk istek sıra
    /// içindeki okumada (sınıf adı kontrolü / dönem listesi) ikinci istek gönderildikten sonra en fazla 2 sn bekletilir.
    /// Okul başına sıra varken ikinci istek bu okumaya giremez, ilk istek bitince kuralı görüp reddedilir; sıra olmasaydı
    /// ikisi de "yok" görür ve iki kayıt oluşurdu.
    /// </summary>
    [Fact]
    public async Task SameClassroomOrOverlappingTermsAtOnce_OnlyOneIsCreated()
    {
        RaceGate classroomGate = new();
        RaceGate termGate = new();
        using OkulApiFactory factory = new(this.database.ConnectionString, configureServices: services =>
        {
            services.RemoveAll<IClassroomRepository>();
            services.AddScoped<ClassroomRepository>();
            services.AddScoped<IClassroomRepository>(sp => new GatedClassroomRepository(sp.GetRequiredService<ClassroomRepository>(), classroomGate));
            services.RemoveAll<ITermRepository>();
            services.AddScoped<TermRepository>();
            services.AddScoped<ITermRepository>(sp => new GatedTermRepository(sp.GetRequiredService<TermRepository>(), termGate));
        });
        using HttpClient client = factory.CreateClient();
        SchoolSeed a = await CreateSchoolAsync(factory);
        string token = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Guid level = await CreateGradeLevelAsync(client, token, "5. sınıf", 5);
        Guid term = await CreateTermAsync(client, token, "Güz", "2026-09-01", "2027-01-22");

        classroomGate.Enabled = true;
        var firstClassroom = CallAsync(client, HttpMethod.Post, "/api/admin/classrooms", token, new { termId = term, branchId = a.BranchId, gradeLevelId = level, name = "5-A" });
        await classroomGate.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var secondClassroom = CallAsync(client, HttpMethod.Post, "/api/admin/classrooms", token, new { termId = term, branchId = a.BranchId, gradeLevelId = level, name = "5-a" });
        var classrooms = await Task.WhenAll(firstClassroom, secondClassroom);
        classroomGate.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci istek sırayı beklemeden ad kontrolüne girdi");
        classrooms[0].Status.ShouldBe(HttpStatusCode.OK);
        classrooms[1].Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(classrooms[1].Body).ShouldBe("Bu adla bir sınıf zaten var");
        (await QueryAsync(factory, c => c.Classrooms.IgnoreQueryFilters().CountAsync(x => x.TermId == term))).ShouldBe(1);

        termGate.Enabled = true;
        var firstTerm = CallAsync(client, HttpMethod.Post, "/api/admin/terms", token, new { name = "Bahar", startDate = "2027-02-08", endDate = "2027-06-18" });
        await termGate.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var secondTerm = CallAsync(client, HttpMethod.Post, "/api/admin/terms", token, new { name = "Bahar 2", startDate = "2027-06-18", endDate = "2027-08-31" });
        var terms = await Task.WhenAll(firstTerm, secondTerm);
        termGate.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci istek sırayı beklemeden çakışma kontrolüne girdi");
        terms[0].Status.ShouldBe(HttpStatusCode.OK);
        terms[1].Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(terms[1].Body).ShouldBe("Bu tarihler başka bir dönemle çakışıyor");
        (await QueryAsync(factory, c => c.Terms.IgnoreQueryFilters().CountAsync(t => t.TenantId == a.TenantId))).ShouldBe(2);
    }
}
