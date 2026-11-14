namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>Test başına geçici klasör; atılınca silinir.</summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Yeni geçici klasör oluşturur.</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tk-agent-tests", Guid.NewGuid().ToString("N"));
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
            // Kilitli dosya kaldıysa işletim sistemi temizler.
        }
        catch (UnauthorizedAccessException)
        {
            // Aynı.
        }
    }
}
