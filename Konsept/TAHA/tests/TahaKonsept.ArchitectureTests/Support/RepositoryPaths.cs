namespace TahaKonsept.ArchitectureTests.Support;

/// <summary>
/// Depo kökünü (<c>TahaKonsept.slnx</c> bulunan klasör) ve <c>src/</c> klasörünü bulur.
/// </summary>
public static class RepositoryPaths
{
    private static readonly Lazy<string> RootLazy = new(FindRoot);

    /// <summary>Depo kökü (TAHA).</summary>
    public static string Root => RootLazy.Value;

    /// <summary><c>src/</c> klasörü.</summary>
    public static string Source => Path.Combine(Root, "src");

    /// <summary>Kökü göreli yola çevirir (mesajlar için).</summary>
    /// <param name="path">Mutlak yol.</param>
    /// <returns>Göreli yol.</returns>
    public static string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TahaKonsept.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("TahaKonsept.slnx bulunamadı.");
    }
}
