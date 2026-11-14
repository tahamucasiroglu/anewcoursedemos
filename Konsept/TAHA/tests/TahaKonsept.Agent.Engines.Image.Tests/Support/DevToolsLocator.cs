namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Depo kökündeki (gitignore'lu) <c>.tools-dev</c> klasörünü bulur; gerçek ikili/model testleri buna bakar.</summary>
public static class DevToolsLocator
{
    /// <summary><c>.tools-dev</c> klasörü; depo kökü bulunamazsa <c>null</c>.</summary>
    /// <returns>Yol ya da <c>null</c>.</returns>
    public static string? FindToolsDevDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TahaKonsept.slnx")))
            {
                var candidate = Path.Combine(dir.FullName, ".tools-dev");
                return Directory.Exists(candidate) ? candidate : null;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
