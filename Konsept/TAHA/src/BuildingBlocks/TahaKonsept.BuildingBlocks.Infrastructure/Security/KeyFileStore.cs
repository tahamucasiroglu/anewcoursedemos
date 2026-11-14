using System.Security.Cryptography;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Rastgele anahtarı dosyada (base64) saklar: yoksa üretir, varsa okur. Linux'ta dosya izni 600 yapılır.
/// Master key (<c>keys/master.key</c>) ve JWT anahtarı (<c>keys/jwt.key</c>) için kullanılır.
/// </summary>
public static class KeyFileStore
{
    /// <summary>Anahtarı okur ya da üretip kaydeder.</summary>
    /// <param name="path">Anahtar dosyası yolu.</param>
    /// <param name="sizeBytes">Üretilecek anahtar uzunluğu (bayt).</param>
    /// <returns>Anahtar baytları.</returns>
    public static byte[] GetOrCreateKey(string path, int sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfLessThan(sizeBytes, 16);

        if (File.Exists(path))
        {
            return ReadKey(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var key = RandomNumberGenerator.GetBytes(sizeBytes);
        try
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(Convert.ToBase64String(key));
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            return key;
        }
        catch (IOException) when (File.Exists(path))
        {
            // Eşzamanlı başka bir süreç oluşturdu; onunkini kullan.
            return ReadKey(path);
        }
    }

    private static byte[] ReadKey(string path)
    {
        var text = File.ReadAllText(path).Trim();
        return Convert.FromBase64String(text);
    }
}
