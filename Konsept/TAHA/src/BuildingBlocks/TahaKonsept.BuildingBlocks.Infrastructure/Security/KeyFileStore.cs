using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Rastgele anahtarı dosyada (base64) saklar: yoksa üretir, varsa okur. Master key (<c>keys/master.key</c>) ve JWT anahtarı
/// (<c>keys/jwt.key</c>) için kullanılır.
/// <list type="bullet">
/// <item>Oluşturma atomiktir: anahtar önce aynı klasörde geçici dosyaya yazılır, sonra <see cref="File.Move(string, string, bool)"/> ile yerine
/// taşınır; eşzamanlı başlayan iki süreç (API + Web ilk çalıştırma) yarım yazılmış/boş dosya okumaz, yarışı kaybeden kazananın anahtarını kullanır.</item>
/// <item>Okuma, beklenen uzunlukta geçerli anahtar görene kadar kısa süre yeniden dener; olmazsa açıklayıcı hata verir.</item>
/// <item>Dosya yalnız geçerli kullanıcıya açıktır: Linux'ta 600, Windows'ta devralınmayan ACL (yalnız geçerli kullanıcı tam yetki);
/// önceden devralınan ACL ile oluşmuş dosyalar okunurken daraltılır.</item>
/// </list>
/// </summary>
public static class KeyFileStore
{
    private const int ReadAttempts = 40;
    private static readonly TimeSpan ReadRetryDelay = TimeSpan.FromMilliseconds(50);

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
            RestrictToCurrentUser(path);
            return ReadKey(path, sizeBytes);
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);

        var key = RandomNumberGenerator.GetBytes(sizeBytes);
        var temp = Path.Combine(directory, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = CreatePrivateFile(temp))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(Convert.ToBase64String(key));
            }

            File.Move(temp, path, overwrite: false);
            return key;
        }
        catch (IOException) when (File.Exists(path))
        {
            // Eşzamanlı başka bir süreç önce yerleştirdi; onunkini kullan.
            return ReadKey(path, sizeBytes);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    private static byte[] ReadKey(string path, int sizeBytes)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var key = Convert.FromBase64String(File.ReadAllText(path).Trim());
                if (key.Length == sizeBytes)
                {
                    return key;
                }
            }
            catch (Exception ex) when (ex is FormatException or IOException && attempt < ReadAttempts)
            {
                // Eski sürümün yazmakta olduğu dosya ya da geçici kilit: kısa bekleyip yeniden oku.
            }

            if (attempt >= ReadAttempts)
            {
                throw new InvalidOperationException($"Anahtar dosyası geçersiz: {path} ({sizeBytes} bayt beklenir). Dosyayı yedekleyip silin; yeni anahtar üretilir.");
            }

            Thread.Sleep(ReadRetryDelay);
        }
    }

    private static FileStream CreatePrivateFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return new FileInfo(path).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, CurrentUserOnly());
        }

        return new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
        });
    }

    private static void RestrictToCurrentUser(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var file = new FileInfo(path);
                if (file.GetAccessControl().AreAccessRulesProtected)
                {
                    return;
                }

                file.SetAccessControl(CurrentUserOnly());
            }
            else
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PrivilegeNotHeldException)
        {
            // Sahibi başka kullanıcı olan dosya: izin daraltılamadı, okuma yine denenir.
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static FileSecurity CurrentUserOnly()
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("Geçerli Windows kullanıcısı bulunamadı.");
        security.SetOwner(user);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
        return security;
    }
}
