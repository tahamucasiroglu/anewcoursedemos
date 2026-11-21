using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-10 (Excel'e aktarma biçimi): dosya UTF-8 BOM ile başlar, <c>text/csv; charset=utf-8</c> eki olarak iner, ayırıcı <c>;</c>, satır
/// sonu CRLF, ilk satır Türkçe başlıklar, tutar <c>1234,50</c>, tarih <c>GG.AA.YYYY</c>, <c>;</c> ya da <c>"</c> içeren ad tırnaklanır; satırlar
/// ekrandaki süzgeçle aynı ve sayfalamasız tamamı (55 tahsilat — ekranda iki sayfa), toplam satır(lar)ı sonda; dosya adı
/// <c>&lt;rapor&gt;-GG.AA.YYYY.csv</c>. Türkçe Excel'de açılış gözlemi VERIFY'da.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ReportExportTests
{
    private readonly TestDatabase database;

    public ReportExportTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Payments_FormatHeadersQuotingAllRowsAndTotals()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        Guid cagla = await CreateStudentAsync(client, w.Founder, NewStudent(w.BranchId, "Çağla Şimşek; Öz"));
        Guid can = await CreateStudentAsync(client, w.Founder, NewStudent(w.BranchId, "Ali \"Can\" Kaya"));
        Guid caglaPlan = await PlanAsync(client, w.Founder, PlanBody(cagla, w.FeeId, installmentCount: 1));
        Guid canPlan = await PlanAsync(client, w.Founder, PlanBody(can, w.FeeId, installmentCount: 1));
        await PaidAsync(client, w.Founder, caglaPlan, 1_234.50m, "2026-11-05");
        (Guid cancelled, _) = await PaidAsync(client, w.Founder, canPlan, 99m, "2026-11-06", "BankTransfer");
        await CancelPaymentAsync(client, w.Founder, cancelled);
        for (int i = 0; i < 53; i++)
        {
            await PaidAsync(client, w.Founder, canPlan, 10m, "2026-11-20", "CardPos");
        }

        ExportResponse file = await ExportAsync(client, w.Founder, "payments", "startDate=2026-11-01&endDate=2026-11-20");

        file.Status.ShouldBe(HttpStatusCode.OK, file.Text);
        file.ContentType.ShouldBe("text/csv; charset=utf-8");
        file.FileName.ShouldBe("tahsilat-raporu-20.11.2026.csv");
        file.Bytes.Take(3).ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF });
        file.Text.Split('\n').Length.ShouldBe(file.Text.Split("\r\n").Length, "her satır sonu CRLF");
        string[] lines = Lines(file.Bytes);
        lines[0].ShouldBe("Tarih;Makbuz no;Öğrenci;Şube;Tutar (TL);Yöntem;Durum;Kaydeden");
        lines[1].ShouldBe("05.11.2026;1;\"Çağla Şimşek; Öz\";Merkez;1234,50;Nakit;Geçerli;Ayşe Kurucu");
        lines[2].ShouldBe("06.11.2026;2;\"Ali \"\"Can\"\" Kaya\";Merkez;99,00;Havale/EFT;İptal;Ayşe Kurucu");
        lines[3].ShouldBe("20.11.2026;3;\"Ali \"\"Can\"\" Kaya\";Merkez;10,00;Kredi kartı (okul POS'u);Geçerli;Ayşe Kurucu");
        lines[^4].ShouldBe("Toplam (geçerli tahsilat);;;;1764,50;;;");
        lines[^3].ShouldBe("Toplam — Nakit;;;;1234,50;;;");
        lines[^2].ShouldBe("Toplam — Havale/EFT;;;;0,00;;;");
        lines[^1].ShouldBe("Toplam — Kredi kartı (okul POS'u);;;;530,00;;;");

        string query = "startDate=2026-11-01&endDate=2026-11-20";
        JsonElement page1 = (await PaymentsReportAsync(client, w.Founder, query)).Body;
        JsonElement page2 = (await PaymentsReportAsync(client, w.Founder, query + "&page=2")).Body;
        page1.GetProperty("totalCount").GetInt32().ShouldBe(55);
        long[] screen = [.. page1.GetProperty("items").EnumerateArray().Concat(page2.GetProperty("items").EnumerateArray()).Select(i => i.GetProperty("receiptNo").GetInt64())];
        long[] inFile = [.. lines[1..^4].Select(l => long.Parse(l.Split(';')[1], System.Globalization.CultureInfo.InvariantCulture))];
        inFile.ShouldBe(screen, "dosya = ekrandaki satırların sayfasız tamamı");
    }

    [Fact]
    public async Task AttendanceAndBalances_HeadersFileNamesAndTotalRows()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        await SavedAsync(client, w.School.Class.Teacher, w.School.Class.ClassroomId, [(w.Ali, "Absent"), (w.School.Ayse, "Late")]);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1));

        ExportResponse attendance = await ExportAsync(client, w.Founder, "attendance", "startDate=2026-11-01&endDate=2026-11-20");
        attendance.FileName.ShouldBe("devamsizlik-raporu-20.11.2026.csv");
        Lines(attendance.Bytes).ShouldBe(
        [
            "Öğrenci;Okul no;Şube;Sınıf;Yok (gün);Geç (gün)",
            "Ali Yılmaz;;Merkez;5-A;1;0",
            "Ayşe Kaya;;Merkez;5-A;0;1",
            "Toplam;;;;1;1",
        ]);

        ExportResponse balances = await ExportAsync(client, w.Founder, "balances");
        balances.FileName.ShouldBe("borc-gecikme-raporu-20.11.2026.csv");
        Lines(balances.Bytes).ShouldBe(
        [
            "Öğrenci;Şube;Sınıf;Plan;Net (TL);Ödenen (TL);Kalan (TL);Geciken (TL);En eski geciken vade",
            "Ali Yılmaz;Merkez;5-A;2026-2027 Eğitim Ücreti;10000,00;0,00;10000,00;0,00;",
            "Toplam;;;;10000,00;0,00;10000,00;0,00;",
        ]);
        Lines((await ExportAsync(client, w.Founder, "balances", "overdueOnly=true")).Bytes).Length.ShouldBe(2, "başlık ve toplam; geciken yok");
    }
}
