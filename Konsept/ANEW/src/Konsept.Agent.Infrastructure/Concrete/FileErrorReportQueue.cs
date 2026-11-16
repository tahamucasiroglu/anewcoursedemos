using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>Bekleyen bildirimleri ajan klasöründeki <c>pending-reports.json</c> dosyasında tutar (içerik maskeli).</summary>
/// <param name="directory">Ajan klasörü.</param>
public sealed class FileErrorReportQueue(string directory) : ErrorReportQueueBase
{
    /// <summary>Dosya adı.</summary>
    public const string FileName = "pending-reports.json";

    private string FilePath => Path.Combine(directory, FileName);

    /// <inheritdoc />
    protected override string? Read() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

    /// <inheritdoc />
    protected override void Write(string text)
    {
        Directory.CreateDirectory(directory);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, text);
        File.Move(temp, FilePath, overwrite: true);
    }
}
