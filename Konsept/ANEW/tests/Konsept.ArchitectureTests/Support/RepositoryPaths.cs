namespace Konsept.ArchitectureTests.Support;

/// <summary>
/// Test çıktı klasöründen yukarı çıkarak repo içindeki <c>src/</c> klasörünü bulur.
/// </summary>
public static class RepositoryPaths
{
    /// <summary>Gets <c>src/</c> klasörünün tam yolu.</summary>
    public static string SourceDirectory
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Konsept.sln")))
            {
                dir = dir.Parent;
            }

            return dir is null
                ? throw new InvalidOperationException("Konsept.sln bulunamadı.")
                : Path.Combine(dir.FullName, "src");
        }
    }
}
