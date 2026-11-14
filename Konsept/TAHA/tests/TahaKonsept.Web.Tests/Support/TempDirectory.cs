namespace TahaKonsept.Web.Tests.Support;

/// <summary>Test başına geçici klasör (<see cref="Path"/>); <see cref="Dispose"/> ile silinir. <see cref="Write"/> metin, <see cref="WriteBytes"/> ikili dosya yazar.</summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Yeni boş klasör açar.</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tk-web-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Klasörün mutlak yolu.</summary>
    public string Path { get; }

    /// <summary>Klasöre metin dosyası yazar.</summary>
    /// <param name="name">Dosya adı.</param>
    /// <param name="content">İçerik.</param>
    public void Write(string name, string content) => File.WriteAllText(System.IO.Path.Combine(Path, name), content);

    /// <summary>Klasöre verilen boyutta ikili dosya yazar.</summary>
    /// <param name="name">Dosya adı.</param>
    /// <param name="size">Bayt.</param>
    public void WriteBytes(string name, int size) => File.WriteAllBytes(System.IO.Path.Combine(Path, name), new byte[size]);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Geçici klasör; silinemezse işletim sistemi temizler.
        }
    }
}
