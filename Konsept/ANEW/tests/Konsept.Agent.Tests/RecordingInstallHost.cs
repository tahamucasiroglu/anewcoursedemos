using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Tests;

/// <summary>
/// Sahte Windows tarafı (spec 0023): kayıt defteri, kısayol ve süreç çağrılarını sırayla kaydeder; gerçek kayıt defterine, Başlat menüsüne ya da
/// kurulu ajana hiç dokunmaz. Kısayol "oluşturulunca" boş dosya yazar (kaldırmanın silmesi gözlenebilsin diye).
/// </summary>
public sealed class RecordingInstallHost : IInstallHost
{
    /// <summary>Gets çağrılar sırayla ("set Software\…|Ad=Değer", "delete-value …", "delete-key …", "shortcut …", "launch …", "stop …").</summary>
    public List<string> Calls { get; } = [];

    /// <summary>Gets yazılan değerler (alt anahtar|ad → değer).</summary>
    public Dictionary<string, object> Values { get; } = [];

    /// <summary>Gets başlatılan exe ve argümanlar.</summary>
    public List<(string Executable, string Arguments)> Launches { get; } = [];

    /// <inheritdoc />
    public void SetUserValue(string subKey, string name, object value)
    {
        Calls.Add($"set {subKey}|{name}");
        Values[subKey + "|" + name] = value;
    }

    /// <inheritdoc />
    public void DeleteUserValue(string subKey, string name)
    {
        Calls.Add($"delete-value {subKey}|{name}");
        Values.Remove(subKey + "|" + name);
    }

    /// <inheritdoc />
    public void DeleteUserKey(string subKey)
    {
        Calls.Add($"delete-key {subKey}");
        foreach (var key in Values.Keys.Where(k => k.StartsWith(subKey + "|", StringComparison.Ordinal)).ToList())
        {
            Values.Remove(key);
        }
    }

    /// <inheritdoc />
    public void CreateShortcut(string shortcutPath, string target)
    {
        Calls.Add($"shortcut {shortcutPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        File.WriteAllText(shortcutPath, target);
    }

    /// <inheritdoc />
    public void Launch(string executable, string arguments)
    {
        Calls.Add($"launch {executable} {arguments}");
        Launches.Add((executable, arguments));
    }

    /// <inheritdoc />
    public void StopRunning(string executable) => Calls.Add($"stop {executable}");
}
