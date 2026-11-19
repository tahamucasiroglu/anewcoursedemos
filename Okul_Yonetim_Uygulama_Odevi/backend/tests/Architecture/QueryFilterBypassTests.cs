using Shouldly;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Kurum filtresini delen okumalar izinli listede (Known traps T5, spec 0004): <c>IgnoreQueryFilters</c> uygulama
/// kodunda yalnızca kişinin kendi atamalarını/okullarını okuyan iki depoda geçebilir. Yeni bir yer eklenirse test kırmızı
/// olur ve gerekçe + review ister.
/// </summary>
public sealed class QueryFilterBypassTests
{
    private static readonly string[] AllowList =
    [
        "src/Persistence/Repositories/RoleAssignmentRepository.cs",
        "src/Persistence/Repositories/TenantRepository.cs",
    ];

    [Fact]
    public void IgnoreQueryFilters_IsUsedOnlyInAllowListedRepositories()
    {
        string backend = FindBackendRoot();
        string[] users = [.. Directory.EnumerateFiles(Path.Combine(backend, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => File.ReadLines(f).Any(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal) && l.Contains("IgnoreQueryFilters", StringComparison.Ordinal)))
            .Select(f => Path.GetRelativePath(backend, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

        users.ShouldBe(AllowList);
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
