using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Production.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Eksik bileşen indirme onayı (spec 0027): ajanın kurabildiği eksik bileşen işi Hata yapmaz, onay bekletir; Onayla → ajana kurdurulur ve iş
/// sürer; Reddet → açık mesajla Hata; kurulamayan eksik → yönlendirici mesajla Hata. Gerçek Production + AgentGateway, sahte ajan kanalı.
/// </summary>
public sealed partial class ProductionPipelineTests
{
    private static readonly AgentComponentInfo HyperFrames = new("hyperframes", "HyperFrames", 40_000_000, 115_000_000);

    /// <summary>AC-1: kurulabilir eksik → iş Ajan bekleniyor + bekleyen indirme; çağrı/harcama yok, sayaç artmaz, sıradaki başlamaz, kendiliğinden sürmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_Missing_WaitsForApproval_NoCalls()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        var first = Seed(profile, user, "Onay Bekleyen");
        var second = Seed(profile, user, "Sıradaki");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);

        await Eventually.WithinAsync(() => Task.FromResult(Job(first).Status == ProductionJobStatus.WaitingForAgent && Job(first).Progress?.Install is not null), Limit, ct);
        await Task.Delay(TimeSpan.FromSeconds(1), ct); // Tarama birkaç kez dürter: onaysız iş sürmemeli.

        Job(first).Status.ShouldBe(ProductionJobStatus.WaitingForAgent);
        Job(second).Status.ShouldBe(ProductionJobStatus.Queued);
        _factory.Provider.Calls.ShouldBeEmpty();
        _factory.Spend.Records.Where(r => r.UserId == user).ShouldBeEmpty();
        _factory.Agent.Count("InstallComponents").ShouldBe(0);
        var row = (await StateAsync(client, profile, ct)).GetProperty("jobs").EnumerateArray().First();
        row.GetProperty("status").GetString().ShouldBe("waiting_for_agent");
        var install = row.GetProperty("install");
        install.GetProperty("approved").GetBoolean().ShouldBeFalse();
        var item = install.GetProperty("components").EnumerateArray().Single();
        (item.GetProperty("id").GetString(), item.GetProperty("name").GetString(), item.GetProperty("downloadBytes").GetInt64(), item.GetProperty("installedBytes").GetInt64())
            .ShouldBe(("hyperframes", "HyperFrames", 40_000_000L, 115_000_000L));
        row.GetProperty("error").ValueKind.ShouldBe(JsonValueKind.Null);
        _factory.Events.Events.ShouldContain(e => e.UserId == user && e.Message.StartsWith("İndirme onayı bekleniyor: Onay Bekleyen — HyperFrames", StringComparison.Ordinal));
    }

    /// <summary>AC-2: Onayla → ajana yalnızca o kimlikle kurulum komutu gider, iş kendiliğinden tamamlanır, bekleyen indirme silinir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_Approve_InstallsThenCompletes()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        var concept = Seed(profile, user, "Onaylanan");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Progress?.Install is { Approved: false }), Limit, ct);

        using var approved = await client.PostAsync($"/api/production/jobs/{Job(concept).Id}/install/approve", null, ct);

        approved.StatusCode.ShouldBe(HttpStatusCode.OK);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Completed), Limit, ct);
        var install = _factory.Agent.Sent.Where(s => s.Method == "InstallComponents").ShouldHaveSingleItem();
        JsonSerializer.Deserialize<JsonElement>(install.Payload).GetProperty("components").EnumerateArray().Select(c => c.GetString()).ShouldBe(["hyperframes"]);
        Job(concept).Progress!.Install.ShouldBeNull();
        _factory.Agent.Count("InstallComponents").ShouldBe(1);
    }

    /// <summary>AC-3: Reddet → iş Hata (açık mesaj), kurulum komutu yok, sıradaki iş başlamaz, Günlük'te hata olayı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_Decline_FailsWithMessage_NoInstall()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        var first = Seed(profile, user, "Reddedilen");
        var second = Seed(profile, user, "Bekleyen İkinci");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(first).Progress?.Install is { Approved: false }), Limit, ct);

        using var declined = await client.PostAsync($"/api/production/jobs/{Job(first).Id}/install/decline", null, ct);

        declined.StatusCode.ShouldBe(HttpStatusCode.OK);
        var row = (await declined.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("jobs").EnumerateArray().First();
        (row.GetProperty("status").GetString(), row.GetProperty("error").GetString()).ShouldBe(("failed", "İndirme onaylanmadı: HyperFrames. Üretim durdu."));
        row.GetProperty("install").ValueKind.ShouldBe(JsonValueKind.Null);
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        Job(first).Status.ShouldBe(ProductionJobStatus.Failed);
        Job(second).Status.ShouldBe(ProductionJobStatus.Queued);
        _factory.Agent.Count("InstallComponents").ShouldBe(0);
        _factory.Provider.Calls.ShouldBeEmpty();
        using var lateApprove = await client.PostAsync($"/api/production/jobs/{Job(first).Id}/install/approve", null, ct); // Reddedilen onaylanamaz (review r1 L-1).
        lateApprove.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (Job(first).Status, Job(first).Progress!.Install, Job(first).Progress!.Error).ShouldBe((ProductionJobStatus.Failed, null, "İndirme onaylanmadı: HyperFrames. Üretim durdu."));
        _factory.Events.Events.ShouldContain(e => e.UserId == user && e.Message.Contains("İndirme onaylanmadı: HyperFrames", StringComparison.Ordinal)
            && e.Message.StartsWith("Video üretimi hata verdi: Reddedilen", StringComparison.Ordinal));
    }

    /// <summary>AC-4: kurulum başarısız → iş Hata, mesaj başarısız bileşeni sayar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_Failure_Fails()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        _factory.Agent.Install = (_, request, _) => Task.FromResult(new AgentInstallReply(false, request.Components));
        var concept = Seed(profile, user, "Kurulamayan");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Progress?.Install is { Approved: false }), Limit, ct);

        (await client.PostAsync($"/api/production/jobs/{Job(concept).Id}/install/approve", null, ct)).EnsureSuccessStatusCode();

        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);
        Job(concept).Progress!.Error.ShouldBe("İndirme ya da kurulum başarısız: HyperFrames. Ayrıntı ajan penceresinde (Kurulum ve bileşenler).");
        Job(concept).Progress!.Install.ShouldBeNull();
        _factory.Provider.Calls.ShouldBeEmpty();
    }

    /// <summary>AC-4: kurulum sırasında ajan koparsa iş Ajan bekleniyor ve onay korunur; yeni ajan gelince kurulum yeniden denenir ve iş tamamlanır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_AgentDrop_WaitsAndKeepsApproval()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, oldConn) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        _factory.Agent.Install = async (conn, request, token) =>
        {
            if (conn == oldConn)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IAgentPresenceService>().DisconnectAsync(conn, CancellationToken.None);
                throw new IOException("bağlantı koptu");
            }

            return await _factory.Agent.DefaultInstall(conn, request, token);
        };
        var concept = Seed(profile, user, "Kopan Kurulum");
        await EnqueueAndStartAsync(client, profile, [concept], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Progress?.Install is { Approved: false }), Limit, ct);

        (await client.PostAsync($"/api/production/jobs/{Job(concept).Id}/install/approve", null, ct)).EnsureSuccessStatusCode();
        await Eventually.WithinAsync(() => Task.FromResult(_factory.Agent.Count("InstallComponents") == 1 && Job(concept).Status == ProductionJobStatus.WaitingForAgent), Limit, ct);
        Job(concept).Progress!.Install.ShouldNotBeNull().Approved.ShouldBeTrue();
        await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        _factory.Provider.Calls.ShouldBeEmpty(); // Kopunca online aşamalara geçilmez (review r1 L-2).

        var newConn = await RegisterAgentAsync(user, ct);

        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Completed), Limit, ct);
        _factory.Agent.Sent.Count(s => s.Method == "InstallComponents" && s.ConnectionId == newConn).ShouldBe(1);
    }

    /// <summary>AC-5: ajanın kuramadığı yerel model eksikse iş online çağrı olmadan Hata, mesaj adı sayar ve başka model/ajan güncellemesi önerir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_NotInstallable_LocalModel_FailsWithGuidance()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: "local/supertonic-3");
        _factory.Agent.Missing = ["supertonic-3", "hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        var concept = Seed(profile, user, "Kurulamaz Model");
        await EnqueueAndStartAsync(client, profile, [concept], ct);

        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);

        Job(concept).Progress!.Error.ShouldBe(
            "Ajan kurulumu eksik: Supertonic 3. Bu ajan sürümü bunları indirip kuramıyor; Üretim ayarlarından başka bir model seçin ya da ajanı güncelleyin.");
        Job(concept).Progress!.Install.ShouldBeNull();
        _factory.Provider.Calls.ShouldBeEmpty();
        _factory.Agent.Count("InstallComponents").ShouldBe(0);
    }

    /// <summary>
    /// Spec 0028 AC-5: ekran kartı gerektiren yerel görsel modeli bu bilgisayarda desteklenmezse (ajan "desteklenmez" der) iş çağrısız Hata
    /// olur; mesaj modeli ve gereken belleği söyler; indirme önerilmez (kurulabilir diğer eksikler olsa da).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_GpuRequired_FailsWithMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null, image: "local/z-image-turbo");
        _factory.Agent.Missing = ["hyperframes", "z-image-turbo"];
        _factory.Agent.Installable = [HyperFrames];
        _factory.Agent.Unsupported = [new AgentUnsupportedTool("z-image-turbo", AgentUnsupportedTool.GpuRequired, 4096)];
        var concept = Seed(profile, user, "Kartsız Bilgisayar");
        await EnqueueAndStartAsync(client, profile, [concept], ct);

        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Status == ProductionJobStatus.Failed), Limit, ct);

        Job(concept).Progress!.Error.ShouldBe(
            "Z-Image-Turbo bu bilgisayarda çalışamaz: ekran kartı (en az 4 GB ayrılmış bellek) gerekir. Üretim ayarlarından başka bir görsel modeli seçin ya da görseli kapatın.");
        Job(concept).Progress!.Install.ShouldBeNull();
        _factory.Provider.Calls.ShouldBeEmpty();
        _factory.Agent.Count("InstallComponents").ShouldBe(0);
    }

    /// <summary>Spec 0028 Req 12: bekleyen indirmede her bileşenin lisansı da saklanır ve web'e gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_Pending_CarriesLicense()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames with { License = "Apache-2.0" }];
        var concept = Seed(profile, user, "Lisanslı");
        await EnqueueAndStartAsync(client, profile, [concept], ct);

        await Eventually.WithinAsync(() => Task.FromResult(Job(concept).Progress?.Install is not null), Limit, ct);

        var row = (await StateAsync(client, profile, ct)).GetProperty("jobs").EnumerateArray().First();
        row.GetProperty("install").GetProperty("components").EnumerateArray().Single().GetProperty("license").GetString().ShouldBe("Apache-2.0");
    }

    /// <summary>AC-6: Onayla/Reddet yalnızca sahibinin bekleyen indirmesinde: başkasının işi 404, bekleyen indirmesi olmayan iş 409.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_Approve_OtherUserOrNotPending_Rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, profile, user, _) = await SetupAsync(ct, audio: null);
        _factory.Agent.Missing = ["hyperframes"];
        _factory.Agent.Installable = [HyperFrames];
        var first = Seed(profile, user, "Sahipli");
        var second = Seed(profile, user, "Sırada Duran");
        await EnqueueAndStartAsync(client, profile, [first, second], ct);
        await Eventually.WithinAsync(() => Task.FromResult(Job(first).Progress?.Install is { Approved: false }), Limit, ct);
        var other = await _factory.CreateUserClientAsync(ct);

        using var foreignApprove = await other.PostAsync($"/api/production/jobs/{Job(first).Id}/install/approve", null, ct);
        using var foreignDecline = await other.PostAsync($"/api/production/jobs/{Job(first).Id}/install/decline", null, ct);
        using var notPending = await client.PostAsync($"/api/production/jobs/{Job(second).Id}/install/approve", null, ct);
        using var notPendingDecline = await client.PostAsync($"/api/production/jobs/{Job(second).Id}/install/decline", null, ct);

        (foreignApprove.StatusCode, foreignDecline.StatusCode).ShouldBe((HttpStatusCode.NotFound, HttpStatusCode.NotFound));
        (notPending.StatusCode, notPendingDecline.StatusCode).ShouldBe((HttpStatusCode.Conflict, HttpStatusCode.Conflict));
        Job(first).Progress!.Install.ShouldNotBeNull().Approved.ShouldBeFalse();
        Job(first).Status.ShouldBe(ProductionJobStatus.WaitingForAgent);
    }
}
