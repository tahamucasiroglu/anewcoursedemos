namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Test başına geçici klasör; dispose edilince silinir.</summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Yeni geçici klasör oluşturur.</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tk-image-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Klasör yolu.</summary>
    public string Path { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
