using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Tests;

/// <summary>
/// Açık dosyayı taklit eden kalıcı silici (spec 0019 testleri): <see cref="Locked"/> adlı dosyada Windows'taki "başka işlem kullanıyor" hatasını
/// fırlatır, diğerlerini gerçekten siler.
/// </summary>
/// <param name="folder">Video klasörü.</param>
public sealed class LockingFileDeleter(string folder) : VideoFileDeleterBase(() => folder)
{
    /// <summary>Gets or sets açık sayılan dosya adı (<c>null</c> = hiçbiri).</summary>
    public string? Locked { get; set; }

    /// <inheritdoc />
    protected override void DeleteFile(string path)
    {
        if (Locked is not null && string.Equals(Path.GetFileName(path), Locked, StringComparison.Ordinal))
        {
            throw new IOException("Dosya başka bir işlem tarafından kullanılıyor.");
        }

        File.Delete(path);
    }
}
