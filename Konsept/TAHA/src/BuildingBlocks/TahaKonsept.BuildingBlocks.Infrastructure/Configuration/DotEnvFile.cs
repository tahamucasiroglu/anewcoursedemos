namespace TahaKonsept.BuildingBlocks.Infrastructure.Configuration;

/// <summary>
/// Geliştirme kolaylığı: başlangıç klasöründen yukarı doğru ilk <c>.env</c> dosyasını bulur ve
/// <c>ANAHTAR=değer</c> satırlarını ortam değişkeni olarak yükler (zaten tanımlı olanları EZMEZ).
/// TAHA/.env git dışıdır; ör. <c>TAHAKONSEPT_DB_PASSWORD</c> buradan gelir. Üretimde (Pi) systemd ortamı kullanılır.
/// </summary>
public static class DotEnvFile
{
    /// <summary>En yakın üst klasördeki <c>.env</c> dosyasını yükler.</summary>
    /// <param name="startDirectory">Aramanın başladığı klasör (ör. ContentRoot).</param>
    /// <param name="maxDepth">En fazla kaç üst klasöre çıkılacağı.</param>
    /// <returns>Yüklenen dosyanın yolu; bulunamazsa <c>null</c>.</returns>
    public static string? LoadFromAncestors(string startDirectory, int maxDepth = 6)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);
        var directory = new DirectoryInfo(startDirectory);
        for (var depth = 0; directory is not null && depth <= maxDepth; depth++, directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                Load(candidate);
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Verilen <c>.env</c> dosyasını yükler.</summary>
    /// <param name="path">Dosya yolu.</param>
    public static void Load(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
