using Konsept.Agent.Infrastructure.Base;

namespace Konsept.Agent.Tests;

/// <summary>Dosya eylemlerini kabuğu çağırmadan kaydeden alt sınıf (spec 0007 testleri; gerçek gezgin/oynatıcı açılmaz).</summary>
/// <param name="folder">Video klasörü.</param>
public sealed class RecordingFileActions(string folder) : VideoFileActionsBase(() => folder)
{
    /// <summary>Gets kaydedilen çağrılar (eylem, tam yol).</summary>
    public List<(string Action, string Path)> Calls { get; } = [];

    /// <summary>Gets or sets a value indicating whether kabuk hatası taklit edilir.</summary>
    public bool Fail { get; set; }

    /// <inheritdoc />
    protected override void RevealInFolder(string path) => Record("folder", path);

    /// <inheritdoc />
    protected override void OpenWithDefaultApp(string path) => Record("player", path);

    private void Record(string action, string path)
    {
        if (Fail)
        {
            throw new System.ComponentModel.Win32Exception(2);
        }

        Calls.Add((action, path));
    }
}
