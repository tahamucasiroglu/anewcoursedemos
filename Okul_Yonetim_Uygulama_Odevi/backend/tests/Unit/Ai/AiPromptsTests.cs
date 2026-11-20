using OkulYonetim.Application.Features.Ai.Prompts;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 (AC-7, AC-11 dayanağı): sesli not talimatı derlemeye gömülüdür, boş değildir, kişisel veri yer tutucusu
/// taşımaz ve duman betiğinin okuduğu kaynak dosyayla birebir aynıdır (uygulama ile duman testi aynı talimatı kullanır).
/// </summary>
public sealed class AiPromptsTests
{
    [Fact]
    public void VoiceMessage_EmbeddedEqualsSourceFile()
    {
        string source = File.ReadAllText(Path.Combine(BackendRoot(), "src", "Application", "Features", "Ai", "Prompts", "voice-message.tr.txt"))
            .Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        AiPrompts.VoiceMessage.ShouldBe(source);
    }

    [Fact]
    public void VoiceMessage_PoliteTurkishInstruction_NoPersonalPlaceholders()
    {
        string prompt = AiPrompts.VoiceMessage;

        prompt.ShouldContain("kibar");
        prompt.ShouldContain("Sayın Veli");
        prompt.ShouldNotContain("{");
        AiPrompts.VoiceMessageMaxTokens.ShouldBeInRange(100, 1000);
    }

    private static string BackendRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("backend/Directory.Build.props bulunamadı");
    }
}
