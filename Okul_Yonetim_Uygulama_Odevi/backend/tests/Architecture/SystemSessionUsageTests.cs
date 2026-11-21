using Shouldly;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Arka plan sistem oturumu (spec 0015, plan 0015 Risks 2): <c>CurrentSession.AuthenticateSystem</c> (kişisiz, bütün şubeleri gören okul
/// oturumu) uygulama kodunda yalnızca tanımlandığı dosyada ve hatırlatma çalışmasında (<c>OverdueReminderRunner</c>) geçebilir; yöntem
/// dışarı açık değildir (<c>internal</c>). Yeni bir kullanım test kırmızı olur ve gerekçe + review ister (T5 ile aynı kalıp).
/// </summary>
public sealed class SystemSessionUsageTests
{
    private static readonly string[] AllowList =
    [
        "src/Application/Features/Billing/Reminders/OverdueReminderRunner.cs",
        "src/Application/Features/Identity/Sessions/CurrentSession.cs",
    ];

    [Fact]
    public void AuthenticateSystem_IsUsedOnlyByReminderRunner()
    {
        string backend = FindBackendRoot();
        string[] users = [.. Directory.EnumerateFiles(Path.Combine(backend, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => File.ReadLines(f).Any(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal) && l.Contains("AuthenticateSystem", StringComparison.Ordinal)))
            .Select(f => Path.GetRelativePath(backend, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

        users.ShouldBe(AllowList);
    }

    [Fact]
    public void AuthenticateSystem_IsNotPublic()
    {
        System.Reflection.MethodInfo? method = typeof(Application.Features.Identity.Sessions.CurrentSession)
            .GetMethod("AuthenticateSystem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        method.ShouldNotBeNull();
        method.IsPublic.ShouldBeFalse();
    }

    private static string FindBackendRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("backend/Directory.Build.props bulunamadı");
    }
}
