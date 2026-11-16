using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Dosya eylemleri (spec 0007 Req 4, 9; AC-7, AC-8): Klasörde aç gezgine, Sistem oynatıcısında aç varsayılan uygulamaya video klasöründeki
/// dosyanın tam yoluyla gider; dosya yoksa "bulunamadı" ve kabuk çağrılmaz; yol/kaçış içeren ad ve bilinmeyen eylem reddedilir; kabuk hatası
/// "açılamadı" olur. Gerçek gezgin/oynatıcı açılmaz (kaydedici alt sınıf).
/// </summary>
public sealed class VideoFileActionsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "anewconcept-files-" + Guid.NewGuid().ToString("N"));

    /// <summary>Initializes a new instance of the <see cref="VideoFileActionsTests"/> class.</summary>
    public VideoFileActionsTests()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllBytes(Path.Combine(_folder, "kusatma.mp4"), [1, 2, 3]);
    }

    /// <summary>AC-8: iki eylem doğru kabuk çağrısına, dosyanın tam yoluyla gider.</summary>
    [Fact]
    public void Open_FolderAndPlayer_UseFullPath()
    {
        var actions = new RecordingFileActions(_folder);
        var video = Guid.NewGuid();

        actions.Open(new AgentFileActionRequest(video, "kusatma.mp4", AgentFileActionRequest.Folder)).ShouldBe(new AgentFileActionReply(true, null));
        actions.Open(new AgentFileActionRequest(video, "kusatma.mp4", AgentFileActionRequest.Player)).ShouldBe(new AgentFileActionReply(true, null));

        var path = Path.GetFullPath(Path.Combine(_folder, "kusatma.mp4"));
        actions.Calls.ShouldBe([("folder", path), ("player", path)]);
    }

    /// <summary>AC-7, H-1: dosya yoksa ya da ad yol/kaçış içeriyorsa "bulunamadı"; bilinmeyen eylem ve kabuk hatası "açılamadı"; kabuk çağrılmaz.</summary>
    [Fact]
    public void Open_Missing_PathEscape_UnknownAction_ShellError()
    {
        var actions = new RecordingFileActions(_folder);
        var video = Guid.NewGuid();
        var outside = Path.Combine(Path.GetTempPath(), "anewconcept-outside-" + Guid.NewGuid().ToString("N") + ".mp4");
        File.WriteAllBytes(outside, [1]);
        try
        {
            actions.Open(new AgentFileActionRequest(video, "yok.mp4", AgentFileActionRequest.Folder)).Error.ShouldBe(AgentFileActionReply.NotFound);
            actions.Open(new AgentFileActionRequest(video, "..\\" + Path.GetFileName(outside), AgentFileActionRequest.Player)).Error.ShouldBe(AgentFileActionReply.NotFound);
            actions.Open(new AgentFileActionRequest(video, outside, AgentFileActionRequest.Player)).Error.ShouldBe(AgentFileActionReply.NotFound);
            actions.Open(new AgentFileActionRequest(video, "kusatma.mp4", "delete")).Error.ShouldBe(AgentFileActionReply.Failed);
            actions.Calls.ShouldBeEmpty();

            actions.Fail = true;
            actions.Open(new AgentFileActionRequest(video, "kusatma.mp4", AgentFileActionRequest.Player)).ShouldBe(new AgentFileActionReply(false, AgentFileActionReply.Failed));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
