using System.Xml.Linq;

namespace TahaKonsept.ArchitectureTests.Support;

/// <summary>
/// <c>src/</c> altındaki bir csproj: adı, yolu ve proje referanslarının adları.
/// </summary>
/// <param name="Name">Proje adı (dosya adı, uzantısız).</param>
/// <param name="Path">Mutlak yol.</param>
/// <param name="References">Referans verilen proje adları.</param>
public sealed record ProjectFile(string Name, string Path, IReadOnlyList<string> References)
{
    /// <summary><c>src/</c> altındaki tüm projeleri okur (bin/obj hariç).</summary>
    /// <returns>Projeler.</returns>
    public static IReadOnlyList<ProjectFile> LoadAll()
    {
        if (!Directory.Exists(RepositoryPaths.Source))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(RepositoryPaths.Source, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !SourceFiles.IsExcluded(p))
            .Select(Load)
            .ToList();
    }

    private static ProjectFile Load(string path)
    {
        var document = XDocument.Load(path);
        var references = document
            .Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => System.IO.Path.GetFileNameWithoutExtension(i!.Replace('\\', '/')))
            .ToList();
        return new ProjectFile(System.IO.Path.GetFileNameWithoutExtension(path), path, references);
    }
}
